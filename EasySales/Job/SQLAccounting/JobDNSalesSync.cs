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
    public class JobDNSalesSync : IJob
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
                    slog.action_identifier = Constants.Action_DebitNoteSync;                      /*check again */
                    slog.action_details = Constants.Tbl_cms_debitnote_sales + Constants.Is_Starting;    /*check again */
                    slog.action_failure = 0;
                    slog.action_failure_message = string.Empty;
                    slog.action_time = DateTime.Now.ToString();

                    DateTime startTime = DateTime.Now;

                    LocalDB.InsertSyncLog(slog);
                    logger.message = "----------------------------------------------------------------------------";
                    logger.Broadcast();
                    logger.message = "Sales Debit note sync is running";
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

                    dynamic lDataSet;
                    string lSQL, DOCNO, CODE, DOCDATE, DOCAMT, PAYMENTAMT, CANCELLED;
                    string query, updateQuery;
                    HashSet<string> queryList = new HashSet<string>();
                    Database _mysql = new Database();

                    Dictionary<string, string> salespersonList = new Dictionary<string, string>();
                    ArrayList salespersonFromDb = _mysql.Select("SELECT login_id, staff_code FROM cms_login");

                    for (int i = 0; i < salespersonFromDb.Count; i++)
                    {
                        Dictionary<string, string> each = (Dictionary<string, string>)salespersonFromDb[i];
                        salespersonList.Add(each["staff_code"], each["login_id"]);
                        /*using key (staff_code) to get value (login_id) */
                    }
                    salespersonFromDb.Clear();

                    Dictionary<string, string> cms_updated_time = _mysql.GetUpdatedTime1YearInterval("cms_debitnote_sales"); 
                    string updated_at = string.Empty;

                    if (cms_updated_time.Count > 0)
                    {
                        updated_at = cms_updated_time["updated_at"].ToString().MSSQLdate();
                    }

                    string getActiveDN = "SELECT dn_code FROM cms_debitnote_sales WHERE cancelled = 'F'";
                    if (cms_updated_time.Count > 0)
                    {
                        getActiveDN += " AND dn_date >= '" + updated_at + "'";
                    }
                    ArrayList inDBactivedn = _mysql.Select(getActiveDN);

                    logger.Broadcast("Active debit note in DB: " + inDBactivedn.Count);
                    ArrayList inDBdn = new ArrayList();
                    for (int i = 0; i < inDBactivedn.Count; i++)
                    {
                        Dictionary<string, string> each = (Dictionary<string, string>)inDBactivedn[i];
                        inDBdn.Add(each["dn_code"].ToString());
                    }
                    inDBactivedn.Clear();

                    lSQL = "SELECT DOCNO,CODE,DOCDATE,DOCAMT,LOCALDOCAMT,CANCELLED FROM SL_DN ";
                    if (cms_updated_time.Count > 0)
                    {
                        lSQL += " WHERE DOCDATE >= '" + updated_at + "'";
                    }

                    try
                    {
                        lDataSet = ComServer.DBManager.NewDataSet(lSQL);
                        
                        query = "INSERT INTO cms_debitnote_sales(dn_code, cust_code, dn_date, dn_amount, outstanding_amount, cancelled) VALUES ";
                        updateQuery = " ON DUPLICATE KEY UPDATE cancelled = VALUES(cancelled), dn_amount = VALUES(dn_amount), outstanding_amount = VALUES(outstanding_amount), cust_code = VALUES(cust_code);";

                        lDataSet.First();

                        while (!lDataSet.eof)
                        {
                            RecordCount++;

                            DOCNO = lDataSet.FindField("DOCNO").AsString;
                            if (inDBdn.Contains(DOCNO))
                            {
                                int index = inDBdn.IndexOf(DOCNO);
                                if (index != -1)
                                {
                                    inDBdn.RemoveAt(index);
                                }
                            }

                            CODE = lDataSet.FindField("CODE").AsString;
                            DOCDATE = lDataSet.FindField("DOCDATE").AsString;
                            DOCDATE = Convert.ToDateTime(DOCDATE).ToString("yyyy-MM-dd");
                            DOCAMT = lDataSet.FindField("LOCALDOCAMT").AsString;            //LOCALDOCAMT
                            CANCELLED = lDataSet.FindField("CANCELLED").AsString;

                            Double.TryParse(DOCAMT, out double docAmt);

                            Database.Sanitize(ref DOCNO);
                            Database.Sanitize(ref CODE);
                            Database.Sanitize(ref DOCDATE);
                            Database.Sanitize(ref DOCAMT);
                            Database.Sanitize(ref CANCELLED);

                            //string Values = string.Format("('{0}','{1}','{2}','{3}','{4}','{5}')", DOCNO, CODE, DOCDATE, DOCAMT, PAYMENTAMT, CANCELLED);
                            string Values = string.Format("('{0}','{1}','{2}','{3}','{4}','{5}')", DOCNO, CODE, DOCDATE, DOCAMT, 0, CANCELLED);

                            queryList.Add(Values);

                            if (queryList.Count % 2000 == 0)
                            {
                                string tmp_query = query;
                                tmp_query += string.Join(", ", queryList);
                                tmp_query += updateQuery;

                                Database mysql = new Database();
                                mysql.Insert(tmp_query);
                                mysql.Message(query);

                                queryList.Clear();
                                tmp_query = string.Empty;

                                logger.message = string.Format("{0} sales debit note records is inserted", RecordCount);
                                logger.Broadcast();
                            }

                            lDataSet.Next();
                        }

                        if (queryList.Count > 0)
                        {
                            query = query + string.Join(", ", queryList) + updateQuery;

                            Database mysql = new Database();
                            mysql.Insert(query);
                            mysql.Message(query);

                            logger.message = string.Format("{0} sales debit note records is inserted", RecordCount);
                            logger.Broadcast();
                        }

                        if (inDBdn.Count > 0)
                        {
                            logger.Broadcast("Total sales debit note records to be deactivated: " + inDBdn.Count);

                            string inactive = "UPDATE cms_debitnote_sales SET cancelled = '{0}' WHERE dn_code = '{1}'";

                            Database mysql = new Database();

                            for (int i = 0; i < inDBdn.Count; i++)
                            {
                                string _docno = inDBdn[i].ToString();

                                Database.Sanitize(ref _docno);
                                string _query = string.Format(inactive, 'T', _docno);

                                mysql.Insert(_query);
                            }

                            logger.Broadcast(inDBdn.Count + " sales debit note records deactivated");

                            inDBdn.Clear();
                        }

                        _mysql.Insert("INSERT INTO cms_update_time(table_name, updated_at) VALUES ('cms_debitnote_sales', NOW()) ON DUPLICATE KEY UPDATE table_name = VALUES(table_name), updated_at = VALUES(updated_at)");

                    }
                    catch
                    {
                        try
                        {
                            goto CHECKAGAIN;
                        }
                        catch (Exception exc)
                        {
                            DpprException exception = new DpprException()
                            {
                                file_name = "SQLAccounting + JobDNSync",
                                exception = exc.Message,
                                time = DateTime.Now.ToString()
                            };
                            LocalDB.InsertException(exception);
                        }
                    }
                    
                    slog.action_identifier = Constants.Action_DebitNoteSync;
                    slog.action_details = Constants.Tbl_cms_debitnote_sales + Constants.Is_Finished + string.Format(" ({0}) records", RecordCount);
                    slog.action_failure = 0;
                    slog.action_failure_message = string.Empty;
                    slog.action_time = DateTime.Now.ToString();

                    DateTime endTime = DateTime.Now;
                    TimeSpan ts = endTime - startTime;
                    Console.WriteLine("Completed in: " + ts.TotalSeconds.ToString("F"));

                    LocalDB.InsertSyncLog(slog);

                    logger.message = "Sales Debit note sync finished in (" + ts.TotalSeconds.ToString("F") + " seconds)";
                    logger.Broadcast();
                });

                thread.Start();
                //thread.Join();
            }
            catch (ThreadAbortException e)
            {
                DpprException ex = new DpprException
                {
                    file_name = "JobDNSync",
                    exception = e.Message,
                    time = DateTime.Now.ToString()
                };
                LocalDB.InsertException(ex);

                Console.WriteLine(Constants.Thread_Exception + e.Message);
            }
        }
    }
}