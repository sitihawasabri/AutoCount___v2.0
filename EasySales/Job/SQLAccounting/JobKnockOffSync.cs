using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EasySales.Model;
using EasySales.Object;
using Quartz;
using Quartz.Impl;
using Quartz.Logging;

namespace EasySales.Job
{
    [DisallowConcurrentExecution]
    class JobKnockOffSync : IJob
    {
        public void Execute()
        {
            this.Run();
        }
        public async Task Execute(IJobExecutionContext context)
        {
            this.Run();
        }

        public void Run()
        {
            try
            {
                Thread thread = new Thread(p =>
                {
                    Thread.CurrentThread.IsBackground = true;

                    int RecordCount = 0;
                    GlobalLogger logger = new GlobalLogger();

                    DpprSyncLog slog = new DpprSyncLog();
                    slog.action_identifier = Constants.Action_KnockOffSync;
                    slog.action_details = Constants.Tbl_cms_customer_ageing_ko + Constants.Is_Starting;
                    slog.action_failure = 0;
                    slog.action_failure_message = string.Empty;
                    slog.action_time = DateTime.Now.ToString();

                    DateTime startTime = DateTime.Now;

                    LocalDB.InsertSyncLog(slog);
                    logger.message = "----------------------------------------------------------------------------";
                    logger.Broadcast();
                    logger.message = "Knockoff sync is running";
                    logger.Broadcast();
                    logger.message = "----------------------------------------------------------------------------";
                    logger.Broadcast();

                /**
                 * Here we will run SQLAccounting Codes
                * */

                CHECKAGAIN:
                    SQLAccApi instance = SQLAccApi.getInstance();

                    dynamic ComServer = instance.GetComServer();

                    if (!instance.RPCAvailable())
                    {
                        goto CHECKAGAIN;
                    }

                    dynamic lDataSet, lDataSet2, lDataSet3;
                    string lSQLRcpt, lSQLCn, lSQLCf, DOCKEY, FROMDOCTYPE, FROMDOCKEY, TODOCTYPE, TODOCKEY, KOAMT, FROMDOCNO, TODOCNO, DOC_AGENT, DOCDATE;
                    string query, updateQuery;

                    HashSet<string> queryList = new HashSet<string>();
                    HashSet<string> queryListCn = new HashSet<string>();
                    HashSet<string> queryListCf = new HashSet<string>();
                    Database _mysql = new Database();

                    Dictionary<string, string> cms_updated_time = _mysql.GetUpdatedTime1YearInterval("cms_customer_ageing_ko"); 
                    string updated_at = string.Empty;

                    if (cms_updated_time.Count > 0)
                    {
                        updated_at = cms_updated_time["updated_at"].ToString().MSSQLdate();
                    }

                    Dictionary<string, string> salespersonList = new Dictionary<string, string>();
                    ArrayList salespersonFromDb = _mysql.Select("SELECT login_id, staff_code FROM cms_login");

                    for (int i = 0; i < salespersonFromDb.Count; i++)
                    {
                        Dictionary<string, string> each = (Dictionary<string, string>)salespersonFromDb[i];
                        salespersonList.Add(each["staff_code"], each["login_id"]);
                    }
                    salespersonFromDb.Clear();

                    Dictionary<string, string> uniqueKeyList = new Dictionary<string, string>();
                    string koInDb = "SELECT * FROM cms_customer_ageing_ko WHERE active_status = 1 ";
                    if (cms_updated_time.Count > 0)
                    {
                        koInDb += " AND doc_date >= '" + updated_at + "'";
                    }
                    ArrayList koFromDb = _mysql.Select(koInDb);

                    for (int i = 0; i < koFromDb.Count; i++)
                    {
                        Dictionary<string, string> each = (Dictionary<string, string>)koFromDb[i];
                        string id = each["id"];
                        string doc_code = each["doc_code"];
                        string doc_ko_ref = each["doc_ko_ref"];
                        string doc_ko_type = each["doc_ko_type"];

                        string unique = doc_code + doc_ko_ref + doc_ko_type;
                        uniqueKeyList.Add(id, unique);
                    }
                    koFromDb.Clear();

                    lSQLRcpt = "SELECT AR_KNOCKOFF.DOCKEY, AR_KNOCKOFF.FROMDOCTYPE, AR_KNOCKOFF.FROMDOCKEY, AR_KNOCKOFF.TODOCTYPE, AR_KNOCKOFF.TODOCKEY, AR_KNOCKOFF.LOCALKOAMT, AR_PM.DOCNO AS FROMDOCNO, AR_PM.DOCDATE,(CASE WHEN AR_KNOCKOFF.TODOCTYPE = 'IV' THEN AR_IV.DOCNO ELSE AR_DN.DOCNO END) AS TODOCNO,(CASE WHEN AR_KNOCKOFF.TODOCTYPE = 'IV' THEN AR_IV.AGENT ELSE AR_DN.AGENT END) AS DOC_AGENT FROM AR_KNOCKOFF LEFT JOIN AR_PM ON AR_PM.DOCKEY = AR_KNOCKOFF.FROMDOCKEY LEFT JOIN AR_IV ON AR_IV.DOCKEY = AR_KNOCKOFF.TODOCKEY LEFT JOIN AR_DN ON AR_DN.DOCKEY = AR_KNOCKOFF.TODOCKEY WHERE AR_KNOCKOFF.FROMDOCTYPE = 'PM' ";
                    lSQLCn = "SELECT AR_KNOCKOFF.DOCKEY, AR_KNOCKOFF.FROMDOCTYPE, AR_KNOCKOFF.FROMDOCKEY, AR_KNOCKOFF.TODOCTYPE, AR_KNOCKOFF.TODOCKEY, AR_KNOCKOFF.LOCALKOAMT, AR_CN.DOCNO AS FROMDOCNO, AR_CN.DOCDATE, (CASE WHEN AR_KNOCKOFF.TODOCTYPE = 'IV' THEN AR_IV.DOCNO ELSE AR_DN.DOCNO END) AS TODOCNO, (CASE WHEN AR_KNOCKOFF.TODOCTYPE = 'IV' THEN AR_IV.AGENT ELSE AR_DN.AGENT END) AS DOC_AGENT FROM AR_KNOCKOFF LEFT JOIN AR_CN ON AR_CN.DOCKEY = AR_KNOCKOFF.FROMDOCKEY LEFT JOIN AR_IV ON AR_IV.DOCKEY = AR_KNOCKOFF.TODOCKEY LEFT JOIN AR_DN ON AR_DN.DOCKEY = AR_KNOCKOFF.TODOCKEY WHERE AR_KNOCKOFF.FROMDOCTYPE = 'CN' ";
                    lSQLCf = "SELECT AR_KNOCKOFF.DOCKEY, AR_KNOCKOFF.FROMDOCTYPE, AR_KNOCKOFF.FROMDOCKEY, AR_KNOCKOFF.TODOCTYPE, AR_KNOCKOFF.TODOCKEY, AR_KNOCKOFF.LOCALKOAMT, AR_CF.DOCNO AS FROMDOCNO, AR_CF.DOCDATE, (CASE WHEN AR_KNOCKOFF.TODOCTYPE = 'CN' THEN AR_CN.DOCNO ELSE AR_PM.DOCNO END) AS TODOCNO, (CASE WHEN AR_KNOCKOFF.TODOCTYPE = 'CN' THEN AR_CN.AGENT ELSE AR_PM.AGENT END) AS DOC_AGENT FROM AR_KNOCKOFF LEFT JOIN AR_CF ON AR_CF.DOCKEY = AR_KNOCKOFF.FROMDOCKEY LEFT JOIN AR_PM ON AR_PM.DOCKEY = AR_KNOCKOFF.TODOCKEY LEFT JOIN AR_CN ON AR_CN.DOCKEY = AR_KNOCKOFF.TODOCKEY WHERE AR_KNOCKOFF.FROMDOCTYPE = 'CF' ";


                    if (cms_updated_time.Count > 0)
                    {
                        lSQLRcpt += " AND AR_PM.DOCDATE >= '" + updated_at + "'";
                        lSQLCn += " AND AR_CN.DOCDATE >= '" + updated_at + "'";
                        lSQLCf += " AND AR_CF.DOCDATE >= '" + updated_at + "'";
                    }
                    else
                    {
                        lSQLRcpt += "  ORDER BY DOCKEY ASC";
                        lSQLCn += "  ORDER BY DOCKEY ASC";
                        lSQLCf += "  ORDER BY DOCKEY ASC";
                    }

                    query = "INSERT INTO cms_customer_ageing_ko(doc_date, doc_code, doc_type, doc_ko_ref, doc_ko_type, doc_amount, active_status, salesperson_id) VALUES ";
                    updateQuery = " ON DUPLICATE KEY UPDATE doc_date = VALUES(doc_date), doc_type = VALUES(doc_type), doc_ko_ref = VALUES(doc_ko_ref), doc_ko_type = VALUES(doc_ko_type), doc_amount = VALUES(doc_amount),active_status=VALUES(active_status),salesperson_id=VALUES(salesperson_id);";

                    try
                    {
                        lDataSet = ComServer.DBManager.NewDataSet(lSQLRcpt);
                        lDataSet.First();

                        while (!lDataSet.eof)
                        {
                            RecordCount++;

                            DOCKEY = lDataSet.FindField("DOCKEY").AsString;
                            FROMDOCTYPE = lDataSet.FindField("FROMDOCTYPE").AsString;
                            FROMDOCKEY = lDataSet.FindField("FROMDOCKEY").AsString;
                            TODOCTYPE = lDataSet.FindField("TODOCTYPE").AsString;
                            TODOCKEY = lDataSet.FindField("TODOCKEY").AsString;
                            KOAMT = lDataSet.FindField("LOCALKOAMT").AsString; 
                            FROMDOCNO = lDataSet.FindField("FROMDOCNO").AsString;
                            TODOCNO = lDataSet.FindField("TODOCNO").AsString;
                            DOC_AGENT = lDataSet.FindField("DOC_AGENT").AsString; //doc_ko_type agent
                            DOCDATE = lDataSet.FindField("DOCDATE").AsString;
                            DOCDATE = Convert.ToDateTime(DOCDATE).ToString("yyyy-MM-dd");
                            string _AGENTID = "0";

                            if (DOC_AGENT != null)
                            {
                                if (!salespersonList.TryGetValue(DOC_AGENT, out _AGENTID))
                                {
                                    _AGENTID = "0";
                                }
                            }
                            else
                            {
                                _AGENTID = "0";
                            }

                            int.TryParse(_AGENTID, out int AGENTID);

                            if (FROMDOCTYPE == "PM")
                            {
                                FROMDOCTYPE = "OR";
                            }

                            string uniqueKey = FROMDOCNO + TODOCNO + TODOCTYPE;

                            if(uniqueKeyList.Count > 0)
                            {
                                if(uniqueKeyList.ContainsValue(uniqueKey))
                                {
                                    var key = uniqueKeyList.Where(pair => pair.Value == uniqueKey)
                                                        .Select(pair => pair.Key)
                                                        .FirstOrDefault();
                                    if (key != null)
                                    {
                                        uniqueKeyList.Remove(key);
                                    }
                                }
                            }

                            Database.Sanitize(ref DOCKEY);
                            Database.Sanitize(ref FROMDOCTYPE); //doc_type
                            Database.Sanitize(ref FROMDOCKEY);
                            Database.Sanitize(ref TODOCTYPE); //doc_ko_type
                            Database.Sanitize(ref TODOCKEY);
                            Database.Sanitize(ref KOAMT); //doc_amount
                            Database.Sanitize(ref FROMDOCNO); //doc_code
                            Database.Sanitize(ref TODOCNO); //doc_ko_ref
                            
                            string Values = string.Format("('{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}')", DOCDATE, FROMDOCNO, FROMDOCTYPE, TODOCNO, TODOCTYPE, KOAMT, 1, AGENTID);

                            queryList.Add(Values);

                            if (queryList.Count % 2000 == 0)
                            {
                                string tmp_query = query;
                                tmp_query += string.Join(", ", queryList);
                                tmp_query += updateQuery;

                                Database mysql = new Database();
                                mysql.Insert(tmp_query);

                                queryList.Clear();
                                tmp_query = string.Empty;

                                logger.message = string.Format("{0} knockoff records is inserted", RecordCount);
                                logger.Broadcast();
                            }

                            lDataSet.Next();
                        }

                        if (queryList.Count > 0)
                        {
                            string query1 = query + string.Join(", ", queryList) + updateQuery;

                            Database mysql = new Database();

                            mysql.Insert(query1);

                            logger.message = string.Format("{0} knockoff records is inserted", RecordCount);
                            logger.Broadcast();
                        }
                    }
                    catch (Exception ex)
                    {
                        try
                        {
                            Console.WriteLine(ex.Message);
                            goto CHECKAGAIN;
                            
                        }
                        catch (Exception exc)
                        {
                            DpprException exception = new DpprException()
                            {
                                file_name = "SQLAccounting + JobKnockOffSync",
                                exception = exc.Message,
                                time = DateTime.Now.ToString()
                            };
                            LocalDB.InsertException(exception);
                        }
                    }

                    try
                    {
                        lDataSet2 = ComServer.DBManager.NewDataSet(lSQLCn);

                        lDataSet2.First();

                        while (!lDataSet2.eof)
                        {
                            RecordCount++;

                            DOCKEY = lDataSet2.FindField("DOCKEY").AsString;
                            FROMDOCTYPE = lDataSet2.FindField("FROMDOCTYPE").AsString;
                            FROMDOCKEY = lDataSet2.FindField("FROMDOCKEY").AsString;
                            TODOCTYPE = lDataSet2.FindField("TODOCTYPE").AsString;
                            TODOCKEY = lDataSet2.FindField("TODOCKEY").AsString;
                            KOAMT = lDataSet2.FindField("LOCALKOAMT").AsString;
                            FROMDOCNO = lDataSet2.FindField("FROMDOCNO").AsString;
                            TODOCNO = lDataSet2.FindField("TODOCNO").AsString;
                            DOC_AGENT = lDataSet2.FindField("DOC_AGENT").AsString; //doc_ko_type agent
                            DOCDATE = lDataSet2.FindField("DOCDATE").AsString;
                            DOCDATE = Convert.ToDateTime(DOCDATE).ToString("yyyy-MM-dd");
                            string _AGENTID = "0";

                            if (DOC_AGENT != null)
                            {
                                if (!salespersonList.TryGetValue(DOC_AGENT, out _AGENTID))
                                {
                                    _AGENTID = "0";
                                }
                            }
                            else
                            {
                                _AGENTID = "0";
                            }

                            int.TryParse(_AGENTID, out int AGENTID);

                            if (FROMDOCTYPE == "PM")
                            {
                                FROMDOCTYPE = "OR";
                            }

                            string uniqueKey = FROMDOCNO + TODOCNO + TODOCTYPE;

                            if (uniqueKeyList.Count > 0)
                            {
                                if (uniqueKeyList.ContainsValue(uniqueKey))
                                {
                                    var key = uniqueKeyList.Where(pair => pair.Value == uniqueKey)
                                                        .Select(pair => pair.Key)
                                                        .FirstOrDefault();
                                    if (key != null)
                                    {
                                        uniqueKeyList.Remove(key);
                                    }
                                }
                            }

                            Database.Sanitize(ref DOCKEY);
                            Database.Sanitize(ref FROMDOCTYPE);
                            Database.Sanitize(ref FROMDOCKEY);
                            Database.Sanitize(ref TODOCTYPE);
                            Database.Sanitize(ref TODOCKEY);
                            Database.Sanitize(ref KOAMT);
                            Database.Sanitize(ref FROMDOCNO);
                            Database.Sanitize(ref TODOCNO);

                            string Values = string.Format("('{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}')", DOCDATE, FROMDOCNO, FROMDOCTYPE, TODOCNO, TODOCTYPE, KOAMT, 1, AGENTID);

                            queryListCn.Add(Values);

                            if (queryListCn.Count % 2000 == 0)
                            {
                                string tmp_query = query;
                                tmp_query += string.Join(", ", queryListCn);
                                tmp_query += updateQuery;

                                Database mysql = new Database();
                                mysql.Insert(tmp_query);

                                queryListCn.Clear();
                                tmp_query = string.Empty;

                                logger.message = string.Format("{0} knockoff records is inserted", RecordCount);
                                logger.Broadcast();
                            }

                            lDataSet2.Next();
                        }

                        if (queryListCn.Count > 0)
                        {
                            string query2 = query + string.Join(", ", queryListCn) + updateQuery;

                            Database mysql = new Database();
                            mysql.Insert(query2);

                            logger.message = string.Format("{0} knockoff records is inserted", RecordCount);
                            logger.Broadcast();
                        }

                        
                    }
                    catch (Exception ex)
                    {
                        try
                        {
                            Console.WriteLine(ex.Message);
                            goto CHECKAGAIN;
                        }
                        catch (Exception exc)
                        {
                            DpprException exception = new DpprException()
                            {
                                file_name = "SQLAccounting + JobKnockOffSync",
                                exception = exc.Message,
                                time = DateTime.Now.ToString()
                            };
                            LocalDB.InsertException(exception);
                        }
                    }

                    try
                    {
                        lDataSet3 = ComServer.DBManager.NewDataSet(lSQLCf);

                        lDataSet3.First();

                        while (!lDataSet3.eof)
                        {
                            RecordCount++;

                            DOCKEY = lDataSet3.FindField("DOCKEY").AsString;
                            FROMDOCTYPE = lDataSet3.FindField("FROMDOCTYPE").AsString;
                            FROMDOCKEY = lDataSet3.FindField("FROMDOCKEY").AsString;
                            TODOCTYPE = lDataSet3.FindField("TODOCTYPE").AsString;
                            TODOCKEY = lDataSet3.FindField("TODOCKEY").AsString;
                            KOAMT = lDataSet3.FindField("LOCALKOAMT").AsString;
                            FROMDOCNO = lDataSet3.FindField("FROMDOCNO").AsString;
                            TODOCNO = lDataSet3.FindField("TODOCNO").AsString;
                            DOC_AGENT = lDataSet3.FindField("DOC_AGENT").AsString; //doc_ko_type agent
                            DOCDATE = lDataSet3.FindField("DOCDATE").AsString;
                            DOCDATE = Convert.ToDateTime(DOCDATE).ToString("yyyy-MM-dd");
                            string _AGENTID = "0";

                            if (DOC_AGENT != null)
                            {
                                if (!salespersonList.TryGetValue(DOC_AGENT, out _AGENTID))
                                {
                                    _AGENTID = "0";
                                }
                            }
                            else
                            {
                                _AGENTID = "0";
                            }

                            int.TryParse(_AGENTID, out int AGENTID);

                            if (TODOCTYPE == "PM")
                            {
                                TODOCTYPE = "OR";
                            }

                            string uniqueKey = FROMDOCNO + TODOCNO + TODOCTYPE;

                            if (uniqueKeyList.Count > 0)
                            {
                                if (uniqueKeyList.ContainsValue(uniqueKey))
                                {
                                    var key = uniqueKeyList.Where(pair => pair.Value == uniqueKey)
                                                        .Select(pair => pair.Key)
                                                        .FirstOrDefault();
                                    if (key != null)
                                    {
                                        uniqueKeyList.Remove(key);
                                    }
                                }
                            }

                            Database.Sanitize(ref DOCKEY);
                            Database.Sanitize(ref FROMDOCTYPE);
                            Database.Sanitize(ref FROMDOCKEY);
                            Database.Sanitize(ref TODOCTYPE);
                            Database.Sanitize(ref TODOCKEY);
                            Database.Sanitize(ref KOAMT);
                            Database.Sanitize(ref FROMDOCNO);
                            Database.Sanitize(ref TODOCNO);

                            string Values = string.Format("('{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}')", DOCDATE, FROMDOCNO, FROMDOCTYPE, TODOCNO, TODOCTYPE, KOAMT, 1, AGENTID);

                            queryListCf.Add(Values);

                            if (queryListCf.Count % 2000 == 0)
                            {
                                string tmp_query = query;
                                tmp_query += string.Join(", ", queryListCf);
                                tmp_query += updateQuery;

                                Database mysql = new Database();
                                mysql.Insert(tmp_query);

                                queryListCf.Clear();
                                tmp_query = string.Empty;

                                logger.message = string.Format("{0} knockoff records is inserted", RecordCount);
                                logger.Broadcast();
                            }

                            lDataSet3.Next();
                        }

                        if (queryListCf.Count > 0)
                        {
                            string query3 = query + string.Join(", ", queryListCf) + updateQuery;

                            Database mysql = new Database();
                            mysql.Insert(query3);

                            logger.message = string.Format("{0} knockoff records is inserted", RecordCount);
                            logger.Broadcast();
                        }

                        _mysql.Insert("INSERT INTO cms_update_time(table_name, updated_at) VALUES('cms_customer_ageing_ko', NOW()) ON DUPLICATE KEY UPDATE table_name = VALUES(table_name), updated_at = VALUES(updated_at)");
                    }
                    catch (Exception ex)
                    {
                        try
                        {
                            Console.WriteLine(ex.Message);
                            goto CHECKAGAIN;
                        }
                        catch (Exception exc)
                        {
                            DpprException exception = new DpprException()
                            {
                                file_name = "SQLAccounting + JobKnockOffSync",
                                exception = exc.Message,
                                time = DateTime.Now.ToString()
                            };
                            LocalDB.InsertException(exception);
                        }
                    }

                    if (uniqueKeyList.Count > 0)
                    {
                        logger.Broadcast("Total KO records to be deactivated: " + uniqueKeyList.Count);
                        Database mysql = new Database();

                        HashSet<string> deactivateId = new HashSet<string>();
                        for (int i = 0; i < uniqueKeyList.Count; i++)
                        {
                            string _id = uniqueKeyList.ElementAt(i).Key;
                            deactivateId.Add(_id);
                        }

                        string ToBeDeactivate = "'" + string.Join("','", deactivateId) + "'";
                        Console.WriteLine(ToBeDeactivate);

                        string inactive = "UPDATE cms_customer_ageing_ko SET active_status = 0 WHERE id IN (" + ToBeDeactivate + ")";
                        mysql.Insert(inactive);

                        logger.Broadcast(uniqueKeyList.Count + " KO records deactivated");

                        uniqueKeyList.Clear();
                    }

                    slog.action_identifier = Constants.Action_KnockOffSync;
                    slog.action_details = Constants.Tbl_cms_customer_ageing_ko + Constants.Is_Finished + string.Format(" ({0}) records", RecordCount);
                    slog.action_failure = 0;
                    slog.action_failure_message = string.Empty;
                    slog.action_time = DateTime.Now.ToString();

                    DateTime endTime = DateTime.Now;
                    TimeSpan ts = endTime - startTime;
                    Console.WriteLine("Completed in: " + ts.TotalSeconds.ToString("F"));

                    LocalDB.InsertSyncLog(slog);

                    logger.message = "Knock off sync finished in (" + ts.TotalSeconds.ToString("F") + " seconds)";
                    logger.Broadcast();
                });

                thread.Start();
            }
            catch (ThreadAbortException e)
            {
                DpprException ex = new DpprException
                {
                    file_name = "JobKnockOffSync",
                    exception = e.Message,
                    time = DateTime.Now.ToString()
                };
                LocalDB.InsertException(ex);

                Console.WriteLine(Constants.Thread_Exception + e.Message);
            }
        }
    }
}
