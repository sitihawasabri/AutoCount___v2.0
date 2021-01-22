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
    public class JobRcptSyncSQLAcc : IJob
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

                    /**
                     * Here we will run SQLAccounting Codes
                     * */

                    DpprSyncLog slog = new DpprSyncLog();
                    slog.action_identifier = Constants.Action_ReceiptSync;                      
                    slog.action_details = Constants.Tbl_cms_receipt + Constants.Is_Starting;    
                    slog.action_failure = 0;
                    slog.action_failure_message = string.Empty;
                    slog.action_time = DateTime.Now.ToString();

                    DateTime startTime = DateTime.Now;
                    LocalDB.InsertSyncLog(slog);
                    logger.message = "----------------------------------------------------------------------------";
                    logger.Broadcast();
                    logger.message = "Receipt sync is running";
                    logger.Broadcast();
                    logger.message = "----------------------------------------------------------------------------";
                    logger.Broadcast();

                CHECKAGAIN:
                    SQLAccApi instance = SQLAccApi.getInstance();

                    dynamic ComServer = instance.GetComServer();

                    if (!instance.RPCAvailable())
                    {
                        goto CHECKAGAIN;
                    }

                    Database _mysql = new Database();

                    dynamic jsonRule = new CheckBackendRule()
                                           .CheckTablesExist()
                                           .GetSettingByTableName("cms_receipt");
                    int postdate = 1;
                    if (jsonRule.Count > 0)
                    {
                        foreach (var rule in jsonRule)
                        {
                            dynamic _postdate = rule.post_date; //include postdate or not
                            if(_postdate != null)
                            {
                                if (_postdate != 1)
                                {
                                    postdate = _postdate;
                                }
                            }
                        }
                    }
                    else
                    {
                        goto ENDJOB;
                    }

                    Dictionary<string, string> salespersonList = new Dictionary<string, string>();
                    ArrayList salespersonFromDb = _mysql.Select("SELECT login_id, staff_code FROM cms_login");

                    for (int i = 0; i < salespersonFromDb.Count; i++)
                    {
                        Dictionary<string, string> each = (Dictionary<string, string>)salespersonFromDb[i];
                        salespersonList.Add(each["staff_code"], each["login_id"]);
                    }
                    salespersonFromDb.Clear();

                    dynamic lDataSet;
                    string lSQL, DOCNO, CODE, KOAMT, DOCAMT, CANCELLED, AGENT, DATEFIELD, CHEQUE_NO, DESC, POSTDATED, DOCKEY, CURRENCYRATE, BOUNCEDDATE;
                    string query, updateQuery;
                    HashSet<string> queryList = new HashSet<string>();

                    Dictionary<string, string> cms_updated_time = _mysql.GetUpdatedTime1YearInterval("cms_receipt"); //{[updated_at, 28/06/2020 5:38:23 PM]}
                    string updated_at = string.Empty;

                    if (cms_updated_time.Count > 0)
                    {
                        updated_at = cms_updated_time["updated_at"].ToString().MSSQLdate();
                    }

                    string getActiveRcpt = "SELECT receipt_code FROM cms_receipt WHERE cancelled = 'F'";

                    if (cms_updated_time.Count > 0)
                    {
                        getActiveRcpt += " AND receipt_date >= '" + updated_at + "'";
                    }

                    ArrayList inDBactiveRcpt = _mysql.Select(getActiveRcpt);

                    logger.Broadcast("Active receipt in DB: " + inDBactiveRcpt.Count);
                    ArrayList inDBrcpt = new ArrayList();
                    for (int i = 0; i < inDBactiveRcpt.Count; i++)
                    {
                        Dictionary<string, string> each = (Dictionary<string, string>)inDBactiveRcpt[i];
                        inDBrcpt.Add(each["receipt_code"].ToString());
                    }
                    inDBactiveRcpt.Clear();

                    //[old query] lSQL = "SELECT AR_PM.DOCKEY, AR_PM.DESCRIPTION, AR_PM.DOCAMT, AR_PM.AGENT, AR_PM.DOCNO, AR_PM.CODE, AR_PM.DOCDATE, AR_PM.POSTDATE, AR_PM.CHEQUENUMBER, AR_PM.CANCELLED, KO.KOAMT, IIF(AR_PM.DOCDATE <> AR_PM.POSTDATE,'P','N') AS POSTDATED FROM AR_PM LEFT JOIN(SELECT SUM(AR_KNOCKOFF.KOAMT) AS KOAMT, AR_KNOCKOFF.FROMDOCKEY FROM AR_KNOCKOFF WHERE AR_KNOCKOFF.FROMDOCTYPE= 'PM' GROUP BY AR_KNOCKOFF.FROMDOCKEY) KO ON KO.FROMDOCKEY = AR_PM.DOCKEY ";
                    //lSQL = "SELECT AR_PM.DOCKEY, AR_PM.DESCRIPTION, AR_PM.DOCAMT, AR_PM.CURRENCYRATE, AR_PM.AGENT, AR_PM.DOCNO, AR_PM.CODE, AR_PM.DOCDATE, AR_PM.POSTDATE, AR_PM.CHEQUENUMBER, AR_PM.CANCELLED, KO.KOAMT, IIF(AR_PM.DOCDATE <> AR_PM.POSTDATE,'P','N') AS POSTDATED FROM AR_PM LEFT JOIN(SELECT SUM(AR_KNOCKOFF.KOAMT) AS KOAMT, AR_KNOCKOFF.FROMDOCKEY FROM AR_KNOCKOFF WHERE AR_KNOCKOFF.FROMDOCTYPE= 'PM' GROUP BY AR_KNOCKOFF.FROMDOCKEY) KO ON KO.FROMDOCKEY = AR_PM.DOCKEY "; //ko not local amount 30092020
                    lSQL = "SELECT AR_PM.DOCKEY, AR_PM.DESCRIPTION, AR_PM.DOCAMT, AR_PM.CURRENCYRATE, AR_PM.AGENT, AR_PM.DOCNO, AR_PM.CODE, AR_PM.DOCDATE, AR_PM.POSTDATE, AR_PM.CHEQUENUMBER, AR_PM.CANCELLED, AR_PM.BOUNCEDDATE, KO.KOAMT, IIF(AR_PM.DOCDATE <> AR_PM.POSTDATE,'P','N') AS POSTDATED FROM AR_PM LEFT JOIN(SELECT SUM(AR_KNOCKOFF.LOCALKOAMT) AS KOAMT, AR_KNOCKOFF.FROMDOCKEY FROM AR_KNOCKOFF WHERE AR_KNOCKOFF.FROMDOCTYPE= 'PM' GROUP BY AR_KNOCKOFF.FROMDOCKEY) KO ON KO.FROMDOCKEY = AR_PM.DOCKEY "; //ko local amount 07102020
                    if (cms_updated_time.Count > 0)
                    {
                        lSQL += " WHERE AR_PM.DOCDATE >= '" + updated_at + "' ORDER BY POSTDATED DESC";
                    }
                    else
                    {
                        lSQL += " ORDER BY POSTDATED DESC";
                    }

                    try
                    {
                        lDataSet = ComServer.DBManager.NewDataSet(lSQL);

                        dynamic lKODataSet;
                        string KO_TODOCKEY, KO_KOAMT, KO_KOLOCALKOAMT;
                        string lSQLKO = "SELECT * FROM AR_KNOCKOFF WHERE TODOCTYPE = 'PM' ORDER BY TODOCKEY ASC";
                        lKODataSet = ComServer.DBManager.NewDataSet(lSQLKO);

                        var KOAMTLIST = new List<KeyValuePair<string, string>>(); //if use dictionary, cannot add same key value

                        lKODataSet.First();

                        while (!lKODataSet.eof)
                        {
                            KO_TODOCKEY = lKODataSet.FindField("TODOCKEY").AsString;
                            KO_KOLOCALKOAMT = lKODataSet.FindField("LOCALKOAMT").AsString; //just added 30092020

                            KOAMTLIST.Add(new KeyValuePair<string, string>(KO_TODOCKEY, KO_KOLOCALKOAMT));

                            lKODataSet.Next();
                        }

                        query = "INSERT INTO cms_receipt(receipt_code, cust_code, salesperson_id, receipt_date, receipt_amount, receipt_knockoff_amount, cheque_no, receipt_desc, cancelled) VALUES ";
                        updateQuery = " ON DUPLICATE KEY UPDATE receipt_date = VALUES(receipt_date), cust_code = VALUES(cust_code), cancelled = VALUES(cancelled), receipt_amount = VALUES(receipt_amount), receipt_knockoff_amount = VALUES(receipt_knockoff_amount),salesperson_id = VALUES(salesperson_id), cheque_no = VALUES(cheque_no), receipt_desc = VALUES(receipt_desc) ";

                        lDataSet.First();

                        while (!lDataSet.eof)
                        {
                            RecordCount++;
                            //if p - insert postdate ELSE n - insert docdate 

                            DOCKEY = lDataSet.FindField("DOCKEY").AsString;
                            DOCNO = lDataSet.FindField("DOCNO").AsString;

                            if (inDBrcpt.Contains(DOCNO))
                            {
                                int index = inDBrcpt.IndexOf(DOCNO);
                                if (index != -1)
                                {
                                    inDBrcpt.RemoveAt(index);
                                }
                            }

                            CODE = lDataSet.FindField("CODE").AsString;
                            KOAMT = lDataSet.FindField("KOAMT").AsString; //ko local amount 07102020

                            if (KOAMT == null)
                            {
                                KOAMT = "0";
                            }

                            double.TryParse(KOAMT, out double DOUBLE_KOAMT);

                            foreach (var keyValue in KOAMTLIST)
                            {
                                if (keyValue.Key == DOCKEY)
                                {
                                    string ko_amount = keyValue.Value;
                                    double.TryParse(ko_amount, out double DOUBLE_KO_KOAMT);
                                    DOUBLE_KOAMT = DOUBLE_KO_KOAMT + DOUBLE_KOAMT;
                                }
                            }

                            KOAMT = DOUBLE_KOAMT.ToString();

                            POSTDATED = lDataSet.FindField("POSTDATED").AsString;

                            if(postdate == 1)
                            {
                                if (POSTDATED == "P")
                                {
                                    DATEFIELD = lDataSet.FindField("POSTDATE").AsString;
                                    DATEFIELD = Convert.ToDateTime(DATEFIELD).ToString("yyyy-MM-dd");
                                }
                                else
                                {
                                    DATEFIELD = lDataSet.FindField("DOCDATE").AsString;
                                    DATEFIELD = Convert.ToDateTime(DATEFIELD).ToString("yyyy-MM-dd");
                                }
                            }
                            else
                            {
                                DATEFIELD = lDataSet.FindField("DOCDATE").AsString;
                                DATEFIELD = Convert.ToDateTime(DATEFIELD).ToString("yyyy-MM-dd");
                            }
                            
                            CHEQUE_NO = lDataSet.FindField("CHEQUENUMBER").AsString;
                            DESC = lDataSet.FindField("DESCRIPTION").AsString;

                            DOCAMT = lDataSet.FindField("DOCAMT").AsString;
                            double.TryParse(DOCAMT, out double docAmt);

                            CURRENCYRATE = lDataSet.FindField("CURRENCYRATE").AsString;
                            double.TryParse(CURRENCYRATE, out double currencyRate);

                            if (currencyRate != 1)
                            {
                                docAmt = docAmt * currencyRate;
                            }

                            CANCELLED = lDataSet.FindField("CANCELLED").AsString;
                            BOUNCEDDATE = lDataSet.FindField("BOUNCEDDATE").AsString;

                            if(BOUNCEDDATE != null)
                            {
                                CANCELLED = "T";
                            }

                            //string AGENTID = "0";
                            //foreach (var keyValue in koAgentIdList)
                            //{
                            //    if (keyValue.Key == DOCNO)
                            //    {
                            //        AGENTID = keyValue.Value;
                            //        break;
                            //    }
                            //}

                            AGENT = lDataSet.FindField("AGENT").AsString;

                            string _AGENTID = "0";

                            if (!salespersonList.TryGetValue(AGENT, out _AGENTID))
                            {
                                _AGENTID = "0";
                            }

                            int.TryParse(_AGENTID, out int AGENTID);

                            Database.Sanitize(ref DOCNO);
                            Database.Sanitize(ref CODE);
                            Database.Sanitize(ref KOAMT);
                            Database.Sanitize(ref DATEFIELD);
                            Database.Sanitize(ref DOCAMT);
                            Database.Sanitize(ref CHEQUE_NO);
                            Database.Sanitize(ref DESC);
                            Database.Sanitize(ref CANCELLED);

                            string Values = string.Format("('{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}','{8}')", DOCNO, CODE, AGENTID, DATEFIELD, docAmt, KOAMT, CHEQUE_NO, DESC, CANCELLED);

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

                                logger.message = string.Format("{0} receipt records is inserted", RecordCount);
                                logger.Broadcast();
                            }

                            lDataSet.Next();
                        }

                        if (queryList.Count > 0)
                        {
                            query = query + string.Join(", ", queryList) + updateQuery;

                            Database mysql = new Database();
                            mysql.Insert(query);

                            logger.message = string.Format("{0} receipt records is inserted", RecordCount);
                            logger.Broadcast();
                        }

                        if (inDBrcpt.Count > 0)
                        {
                            logger.Broadcast("Total receipt records to be deactivated: " + inDBrcpt.Count);

                            //string inactive = "INSERT INTO cms_receipt (receipt_code, cancelled) VALUES ";
                            //string inactive_duplicate = "ON DUPLICATE KEY UPDATE cancelled=VALUES(cancelled);";

                            string inactive = "UPDATE cms_receipt SET cancelled = '{0}' WHERE receipt_code = '{1}'";

                            Database mysql = new Database();

                            for (int i = 0; i < inDBrcpt.Count; i++)
                            {
                                string _docno = inDBrcpt[i].ToString();

                                Database.Sanitize(ref _docno);
                                string _query = string.Format(inactive, 'T', _docno);

                                mysql.Insert(_query);
                            }

                            logger.Broadcast(inDBrcpt.Count + " receipt records deactivated");

                            inDBrcpt.Clear();
                        }

                        _mysql.Insert("INSERT INTO cms_update_time(table_name, updated_at) VALUES ('cms_receipt', NOW()) ON DUPLICATE KEY UPDATE table_name = VALUES(table_name), updated_at = VALUES(updated_at)");
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
                                file_name = "SQLAccounting + JobReceiptSync",
                                exception = exc.Message,
                                time = DateTime.Now.ToString()
                            };
                            LocalDB.InsertException(exception);
                        }
                    }

                    ENDJOB:

                    slog.action_identifier = Constants.Action_ReceiptSync;
                    slog.action_details = Constants.Tbl_cms_receipt + Constants.Is_Finished + string.Format(" ({0}) records", RecordCount);
                    slog.action_failure = 0;
                    slog.action_failure_message = string.Empty;
                    slog.action_time = DateTime.Now.ToString();

                    DateTime endTime = DateTime.Now;
                    TimeSpan ts = endTime - startTime;
                    Console.WriteLine("Completed in: " + ts.TotalSeconds.ToString("F"));

                    LocalDB.InsertSyncLog(slog);

                    logger.message = "Receipt sync finished in (" + ts.TotalSeconds.ToString("F") + " seconds)";
                    logger.Broadcast();
                });

                thread.Start();
                //thread.Join();
            }
            catch (ThreadAbortException e)
            {
                DpprException ex = new DpprException
                {
                    file_name = "JobReceiptSyncSQL",
                    exception = e.Message,
                    time = DateTime.Now.ToString()
                };
                LocalDB.InsertException(ex);

                Console.WriteLine(Constants.Thread_Exception + e.Message);
            }
        }
    }
}