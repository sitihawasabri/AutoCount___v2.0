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
    public class JobReceiptSync : IJob
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
                    slog.action_identifier = Constants.Action_ReceiptSync;
                    slog.action_details = Constants.Tbl_cms_receipt + Constants.Is_Starting;
                    slog.action_failure = 0;
                    slog.action_failure_message = string.Empty;
                    slog.action_time = DateTime.Now.ToString();
                    DateTime startTime = DateTime.Now;
                    LocalDB.InsertSyncLog(slog);

                    logger.message = "Receipt sync is running";
                    logger.Broadcast();

                    QNEApi api = new QNEApi();
                    dynamic json = api.GetByName("CustomerReceipts");
                    api.Message("Receipt sync is running");
                    HashSet<string> queryList = new HashSet<string>();
                    int RecordCount = 0;
                    string query = "INSERT INTO cms_receipt(receipt_code, cust_code, receipt_date, receipt_amount, receipt_knockoff_amount, cancelled) VALUES ";
                    
                    string updateQuery = " ON DUPLICATE KEY UPDATE receipt_code = VALUES(receipt_code), cust_code = VALUES(cust_code), receipt_date = VALUES(receipt_date), receipt_amount = VALUES(receipt_amount), receipt_knockoff_amount = VALUES(receipt_knockoff_amount), cancelled = VALUES(cancelled);";

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

                            string Values = string.Format("('{0}','{1}','{2}','{3}','{4}','{5}')", item.docCode, item.debtor, item.docDate, item.totalAmount, knockoffAmount, cancelled); //item.debtor.companyCode

                            queryList.Add(Values);

                            if (queryList.Count % 2000 == 0)
                            {
                                string tmp_query = query;
                                tmp_query += string.Join(", ", queryList);
                                tmp_query += updateQuery;

                                mysql.Insert(tmp_query);

                                queryList.Clear();
                                tmp_query = string.Empty;

                                logger.message = string.Format("{0} receipt records is inserted", RecordCount);
                                logger.Broadcast();
                            }
                        }

                        if (queryList.Count > 0)
                        {
                            query = query + string.Join(", ", queryList) + updateQuery;

                            mysql.Insert(query);

                            logger.message = string.Format("{0} receipt records is inserted", RecordCount);
                            logger.Broadcast();
                        }
                    }
                    catch (Exception ex)
                    {
                        api.Message("QNEAPI + JobReceiptSync.cs ---> " + ex.Message);
                    }
                    api.Message("Receipt sync finished");
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

                //await Console.Out.WriteLineAsync("Done receipt Sync");
            }
            catch (ThreadAbortException e)
            {
                DpprException ex = new DpprException
                {
                    file_name = "JobReceiptSync",
                    exception = e.Message,
                    time = DateTime.Now.ToString()
                };
                LocalDB.InsertException(ex);

                Console.WriteLine(Constants.Thread_Exception + e.Message);
            }
        }
    }
}