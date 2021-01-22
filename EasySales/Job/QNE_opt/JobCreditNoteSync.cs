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
using RestSharp;

namespace EasySales.Job
{
    [DisallowConcurrentExecution]
    public class JobCreditNoteSync : IJob
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

                    GlobalLogger logger = new GlobalLogger();

                    DpprSyncLog slog = new DpprSyncLog();
                    slog.action_identifier = Constants.Action_CreditNoteSync;
                    slog.action_details = Constants.Tbl_cms_creditnote + Constants.Is_Starting;
                    slog.action_failure = 0;
                    slog.action_failure_message = string.Empty;
                    slog.action_time = DateTime.Now.ToString();
                    DateTime startTime = DateTime.Now;
                    LocalDB.InsertSyncLog(slog);

                    logger.message = "Credit note sync is running";
                    logger.Broadcast();

                    QNEApi api = new QNEApi();
                    dynamic json = api.GetByName("SalesCNs");
                    api.Message("Credit note sync is running");
                    HashSet<string> queryList = new HashSet<string>();
                    int RecordCount = 0;
                    string query = "INSERT INTO cms_creditnote(cn_code, cust_code, cn_date, cn_amount, cancelled, cn_knockoff_amount) VALUES ";

                    string updateQuery = " ON DUPLICATE KEY UPDATE cn_code = VALUES(cn_code), cust_code = VALUES(cust_code), cn_date = VALUES(cn_date), cn_amount = VALUES(cn_amount), cancelled = VALUES(cancelled), cn_knockoff_amount = VALUES(cn_knockoff_amount);";

                    Database mysql = new Database();

                    try
                    {
                        foreach (var item in json)
                        {
                            RecordCount++;

                            double knockoffAmount = 0.00;
                            string cancelled = "F";

                            if (item.isCancelled == true)
                            {
                                cancelled = "T";
                            }

                            string Values = string.Format("('{0}','{1}','{2}','{3}','{4}','{5}')", item.cnCode, item.customer, item.cnDate, item.totalAmount, cancelled, knockoffAmount);

                            queryList.Add(Values);

                            //if (RecordCount % 10000 == 0)
                            //{
                            //    mysql.KillSleepyProcesses();
                            //}

                            if (queryList.Count % 2000 == 0)
                            {
                                string tmp_query = query;
                                tmp_query += string.Join(", ", queryList);
                                tmp_query += updateQuery;

                                mysql.Insert(tmp_query);

                                queryList.Clear();
                                tmp_query = string.Empty;

                                logger.message = string.Format("{0} credit note records is inserted", RecordCount);
                                logger.Broadcast();
                            }
                        }

                        if (queryList.Count > 0)
                        {
                            query = query + string.Join(", ", queryList) + updateQuery;

                            mysql.Insert(query);

                            logger.message = string.Format("{0} credit note records is inserted", RecordCount);
                            logger.Broadcast();
                        }
                    }
                    catch (Exception ex)
                    {
                        api.Message("QNEAPI + JobCreditNoteSync.cs ---> " + ex.Message);
                    }
                    api.Message("Credit note sync finished");
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
               //thread.Join();

                //await Console.Out.WriteLineAsync("Done credit note Sync");
            }
            catch (ThreadAbortException e)
            {
                DpprException ex = new DpprException
                {
                    file_name = "JobCreditNoteSync",
                    exception = e.Message,
                    time = DateTime.Now.ToString()
                };
                LocalDB.InsertException(ex);

                Console.WriteLine(Constants.Thread_Exception + e.Message);
            }
        }
    }
}