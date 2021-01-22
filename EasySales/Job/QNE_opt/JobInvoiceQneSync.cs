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
    public class JobInvoiceQneSync : IJob
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
                    slog.action_identifier = Constants.Action_InvoiceQneSync;
                    slog.action_details = Constants.Tbl_cms_invoice_qne + Constants.Is_Starting;
                    slog.action_failure = 0;
                    slog.action_failure_message = string.Empty;
                    slog.action_time = DateTime.Now.ToString();
                    DateTime startTime = DateTime.Now;
                    LocalDB.InsertSyncLog(slog);

                    logger.message = "Invoice QNE sync is running";
                    logger.Broadcast();

                    QNEApi api = new QNEApi();
                    dynamic json = api.GetByName("Invoices");
                    api.Message("Invoice QNE sync is running");
                    HashSet<string> queryList = new HashSet<string>();
                    int RecordCount = 0;
                    string query = "INSERT INTO cms_invoice(invoice_code, cust_code, invoice_date, invoice_amount, outstanding_amount, cancelled, invoice_due_date) VALUES ";

                    string updateQuery = " ON DUPLICATE KEY UPDATE invoice_code = VALUES(invoice_code), cust_code = VALUES(cust_code), invoice_date = VALUES(invoice_date), invoice_amount = VALUES(invoice_amount), outstanding_amount=VALUES(outstanding_amount), cancelled = VALUES(cancelled), invoice_due_date=VALUES(invoice_due_date);";

                    Database mysql = new Database();

                    try
                    {
                        foreach (var item in json)
                        {
                            RecordCount++;

                            char cancelled = 'F';

                            string inv_code = item.invoiceCode, cust_code = item.customer;

                            Database.Sanitize(ref inv_code);
                            Database.Sanitize(ref cust_code);

                            string Values = string.Format("('{0}','{1}','{2}','{3}','{4}','{5}','{6}')", inv_code, cust_code, item.invoiceDate, item.totalAmount, item.balance, cancelled, item.dueDate);

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

                                logger.message = string.Format("{0} QNE invoice records is inserted", RecordCount);
                                logger.Broadcast();
                            }
                        }

                        if (queryList.Count > 0)
                        {
                            query = query + string.Join(", ", queryList) + updateQuery;

                            mysql.Insert(query);

                            logger.message = string.Format("{0} QNE invoice records is inserted", RecordCount);
                            logger.Broadcast();
                        }
                    }
                    catch (Exception ex)
                    {
                        api.Message("QNEAPI + JobInvoiceSync.cs ---> " + ex.Message);
                    }
                    api.Message("Invoice QNE sync finished");
                    slog.action_identifier = Constants.Action_InvoiceQneSync;
                    slog.action_details = Constants.Tbl_cms_invoice_qne + Constants.Is_Finished + string.Format(" ({0}) records", RecordCount);
                    slog.action_failure = 0;
                    slog.action_failure_message = string.Empty;
                    slog.action_time = DateTime.Now.ToString();

                    DateTime endTime = DateTime.Now;
                    TimeSpan ts = endTime - startTime;
                    Console.WriteLine("Completed in: " + ts.TotalSeconds.ToString("F"));

                    LocalDB.InsertSyncLog(slog);

                    logger.message = "QNE invoice sync finished in (" + ts.TotalSeconds.ToString("F") + " seconds)";
                    logger.Broadcast();
                });

                thread.Start();
                //thread.Join();

                //await Console.Out.WriteLineAsync("Done QNE Invoice Sync");
            }
            catch (ThreadAbortException e)
            {
                DpprException ex = new DpprException
                {
                    file_name = "JobInvoiceQneSync",
                    exception = e.Message,
                    time = DateTime.Now.ToString()
                };
                LocalDB.InsertException(ex);

                Console.WriteLine(Constants.Thread_Exception + e.Message);
            }
        }
    }
}