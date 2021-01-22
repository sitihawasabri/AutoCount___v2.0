using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
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
    public class JobCNSync : IJob
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
                    slog.action_identifier = Constants.Action_CreditNoteSync;                      
                    slog.action_details = Constants.Tbl_cms_creditnote + Constants.Is_Starting;    
                    slog.action_failure = 0;
                    slog.action_failure_message = string.Empty;
                    slog.action_time = DateTime.Now.ToString();

                    DateTime startTime = DateTime.Now;

                    LocalDB.InsertSyncLog(slog);
                    logger.message = "----------------------------------------------------------------------------";
                    logger.Broadcast();
                    logger.message = "Credit Note sync is running";
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

                    dynamic lDataSet;
                    string lSQL, DOCKEY, DOCNO, CODE, KOKOAMT, DOCDATE, DOCAMT, CANCELLED, AGENT, KO_TODOCKEY, KO_KOAMT;
                    string query, updateQuery;

                    HashSet<string> queryList = new HashSet<string>();
                    Database _mysql = new Database();

                    Dictionary<string, string> cms_updated_time = _mysql.GetUpdatedTime1YearInterval("cms_creditnote"); //{[updated_at, 28/06/2020 5:38:23 PM]}
                    string updated_at = string.Empty;

                    if (cms_updated_time.Count > 0)
                    {
                        updated_at = cms_updated_time["updated_at"].ToString().MSSQLdate();
                    }

                    string getActiveCN = "SELECT cn_code FROM cms_creditnote WHERE cancelled = 'F'";
                    if (cms_updated_time.Count > 0)
                    {
                        getActiveCN += " AND cn_date >= '" + updated_at + "'";
                    }
                    ArrayList inDBactivecn = _mysql.Select(getActiveCN);

                    logger.Broadcast("Active credit note in DB: " + inDBactivecn.Count);
                    ArrayList inDBcn = new ArrayList();
                    for (int i = 0; i < inDBactivecn.Count; i++)
                    {
                        Dictionary<string, string> each = (Dictionary<string, string>)inDBactivecn[i];
                        string cnCode = each["cn_code"].ToString();
                        if (!inDBcn.Contains(cnCode))
                        {
                            inDBcn.Add(cnCode);
                        }
                    }
                    inDBactivecn.Clear();

                    Dictionary<string, string> salespersonList = new Dictionary<string, string>();
                    ArrayList salespersonFromDb = _mysql.Select("SELECT login_id, staff_code FROM cms_login");

                    for (int i = 0; i < salespersonFromDb.Count; i++)
                    {
                        Dictionary<string, string> each = (Dictionary<string, string>)salespersonFromDb[i];
                        salespersonList.Add(each["staff_code"], each["login_id"]);
                        /*using key (staff_code) to get value (login_id) */
                    }
                    salespersonFromDb.Clear();

                    lSQL = "SELECT AR_CN.DOCKEY, AR_CN.DOCNO,AR_CN.CODE,AR_CN.AGENT,KO.KOAMT,AR_CN.DOCDATE,AR_CN.LOCALDOCAMT,AR_CN.CANCELLED FROM AR_CN LEFT JOIN(SELECT SUM(AR_KNOCKOFF.LOCALKOAMT) AS KOAMT, AR_KNOCKOFF.FROMDOCKEY FROM AR_KNOCKOFF WHERE AR_KNOCKOFF.FROMDOCTYPE = 'CN' GROUP BY AR_KNOCKOFF.FROMDOCKEY) KO  ON KO.FROMDOCKEY = AR_CN.DOCKEY ";
                    
                    if (cms_updated_time.Count > 0)
                    {
                        lSQL += " WHERE AR_CN.DOCDATE >='" + updated_at + "' ORDER BY DOCKEY ASC";
                    }
                    else
                    {
                        lSQL += " ORDER BY DOCKEY ASC";
                    }

                    Console.WriteLine(lSQL);

                    try
                    {
                        lDataSet = ComServer.DBManager.NewDataSet(lSQL);

                        dynamic lKODataSet;
                        string lSQLKO = "SELECT TODOCKEY, LOCALKOAMT FROM AR_KNOCKOFF WHERE TODOCTYPE = 'CN' ORDER BY TODOCKEY ASC";
                        lKODataSet = ComServer.DBManager.NewDataSet(lSQLKO);
                        var KOAMTLIST = new List<KeyValuePair<string, string>>(); //if use dictionary, cannot add same key value

                        lKODataSet.First();

                        while (!lKODataSet.eof)
                        {
                            KO_TODOCKEY = lKODataSet.FindField("TODOCKEY").AsString;
                            KO_KOAMT = lKODataSet.FindField("LOCALKOAMT").AsString;

                            KOAMTLIST.Add(new KeyValuePair<string, string>(KO_TODOCKEY, KO_KOAMT));

                            lKODataSet.Next();
                        }

                        query = "INSERT INTO cms_creditnote(cn_code, cust_code, cn_knockoff_amount, cn_date, cn_amount, cancelled, salesperson_id) VALUES ";
                        updateQuery = " ON DUPLICATE KEY UPDATE cust_code = VALUES(cust_code), cancelled = VALUES(cancelled), cn_amount = VALUES(cn_amount),cn_knockoff_amount = VALUES(cn_knockoff_amount), cn_date = VALUES(cn_date), salesperson_id = VALUES(salesperson_id) "; //cust_code

                        lDataSet.First();

                        while (!lDataSet.eof)
                        {
                            RecordCount++;

                            DOCKEY = lDataSet.FindField("DOCKEY").AsString;
                            DOCNO = lDataSet.FindField("DOCNO").AsString;

                            if (inDBcn.Contains(DOCNO))
                            {
                                int index = inDBcn.IndexOf(DOCNO);
                                if (index != -1)
                                {
                                    inDBcn.RemoveAt(index);
                                }
                            }

                            CODE = lDataSet.FindField("CODE").AsString;
                            KOKOAMT = lDataSet.FindField("KOAMT").AsString; //LOCALKOAMT

                            if (KOKOAMT == null)
                            {
                                KOKOAMT = "0";
                            }

                            double.TryParse(KOKOAMT, out double DOUBLE_KOAMT);

                            foreach (var keyValue in KOAMTLIST)
                            {
                                if (keyValue.Key == DOCKEY) 
                                {
                                    string ko_amt = keyValue.Value;
                                    double.TryParse(ko_amt, out double DOUBLE_KO_KOAMT);
                                    DOUBLE_KOAMT = DOUBLE_KO_KOAMT + DOUBLE_KOAMT; //if CN was knockoff with CF, add the amount to current amount
                                }
                            }

                            KOKOAMT = DOUBLE_KOAMT.ToString();
                            DOCDATE = lDataSet.FindField("DOCDATE").AsString;
                            DOCDATE = Convert.ToDateTime(DOCDATE).ToString("yyyy-MM-dd");
                            DOCAMT = lDataSet.FindField("LOCALDOCAMT").AsString;            //LOCALDOCAMT
                            CANCELLED = lDataSet.FindField("CANCELLED").AsString;
                            AGENT = lDataSet.FindField("AGENT").AsString;

                            string _AGENTID = "0";

                            if (_AGENTID == "0")
                            {
                                AGENT = lDataSet.FindField("AGENT").AsString;
                                if (AGENT != null)
                                {
                                    AGENT = AGENT.ToUpper();

                                    if (string.IsNullOrEmpty(AGENT) || !salespersonList.TryGetValue(AGENT, out _AGENTID))
                                    {
                                        _AGENTID = "0";
                                    }
                                }
                                else
                                {
                                    _AGENTID = "0";
                                }

                            }
                            int.TryParse(_AGENTID, out int AGENTID);

                            Database.Sanitize(ref DOCNO);
                            Database.Sanitize(ref CODE);
                            Database.Sanitize(ref KOKOAMT);
                            Database.Sanitize(ref DOCDATE);
                            Database.Sanitize(ref DOCAMT);
                            Database.Sanitize(ref CANCELLED);

                            string Values = string.Format("('{0}','{1}','{2}','{3}','{4}','{5}','{6}')", DOCNO, CODE, KOKOAMT, DOCDATE, DOCAMT, CANCELLED, AGENTID);

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

                                logger.message = string.Format("{0} credit note records is inserted", RecordCount);
                                logger.Broadcast();
                            }

                            lDataSet.Next();
                        }

                        if (queryList.Count > 0)
                        {
                            query = query + string.Join(", ", queryList) + updateQuery;

                            Database mysql = new Database();
                            mysql.Insert(query);

                            logger.message = string.Format("{0} credit note records is inserted", RecordCount);
                            logger.Broadcast();
                        }

                        if (inDBcn.Count > 0)
                        {
                            logger.Broadcast("Total credit note records to be deactivated: " + inDBcn.Count);

                            string inactive = "UPDATE cms_creditnote SET cancelled = '{0}' WHERE cn_code = '{1}'";

                            Database mysql = new Database();

                            for (int i = 0; i < inDBcn.Count; i++)
                            {
                                string _docno = inDBcn[i].ToString();

                                Database.Sanitize(ref _docno);
                                string _query = string.Format(inactive, 'T', _docno);

                                mysql.Insert(_query);
                            }

                            logger.Broadcast(inDBcn.Count + " credit note records deactivated");

                            inDBcn.Clear();
                        }

                        _mysql.Insert("INSERT INTO cms_update_time(table_name, updated_at) VALUES ('cms_creditnote', NOW()) ON DUPLICATE KEY UPDATE table_name = VALUES(table_name), updated_at = VALUES(updated_at)");

                    }
                    catch
                    {
                        try
                        {
                            instance.KillSQLAccounting();
                            goto CHECKAGAIN;
                        }
                        catch (Exception exc)
                        {
                            DpprException exception = new DpprException()
                            {
                                file_name = "SQLAccounting + JobCNSync",
                                exception = exc.Message,
                                time = DateTime.Now.ToString()
                            };
                            LocalDB.InsertException(exception);
                        }
                    }


                    slog.action_identifier = Constants.Action_CreditNoteSync;
                    slog.action_details = Constants.Tbl_cms_creditnote + Constants.Is_Finished + string.Format(" ({0}) records", RecordCount);
                    slog.action_failure = 0;
                    slog.action_failure_message = string.Empty;
                    slog.action_time = DateTime.Now.ToString();

                    DateTime endTime = DateTime.Now;
                    TimeSpan ts = endTime - startTime;
                    Console.WriteLine("Completed in: " + ts.TotalSeconds.ToString("F"));

                    LocalDB.InsertSyncLog(slog);

                    logger.message = "Credit note sync finished in (" + ts.TotalSeconds.ToString("F") + " seconds)";
                    logger.Broadcast();
                });

                thread.Start();
            }
            catch (ThreadAbortException e)
            {
                DpprException ex = new DpprException
                {
                    file_name = "JobCNSync",
                    exception = e.Message,
                    time = DateTime.Now.ToString()
                };
                LocalDB.InsertException(ex);

                Console.WriteLine(Constants.Thread_Exception + e.Message);
            }
        }
    }
}