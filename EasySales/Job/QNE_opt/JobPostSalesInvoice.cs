using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using System.ComponentModel;
using System.Threading;
using EasySales.Model;
using EasySales.Object;
using Quartz;
using Quartz.Impl;
using Quartz.Logging;
using RestSharp;
using EasySales.Object.QNE;
using System.Globalization;

namespace EasySales.Job
{
    [DisallowConcurrentExecution]
    public class JobPostSalesInvoice : IJob
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
                    slog.action_identifier = Constants.Action_PostSalesInvoices;
                    slog.action_details = "Starting";
                    slog.action_failure = 0;
                    slog.action_failure_message = string.Empty;
                    slog.action_time = DateTime.Now.ToString();
                    DateTime startTime = DateTime.Now;

                    List<DpprSyncLog> list = LocalDB.checkJobRunning();
                    if (list.Count > 0)
                    {
                        DpprSyncLog value = list[0];
                        if (value.action_details == "Starting")
                        {
                            logger.message = "QNE POST Sales Invoices is already running";
                            logger.Broadcast();
                            goto ENDJOB;
                        }
                    }

                    LocalDB.InsertSyncLog(slog);

                    logger.message = "POST Sales Invoice is running";
                    logger.Broadcast();

                    NotJobPostInvoice onlyInvoice = new NotJobPostInvoice(NotJobPostInvoice.Operation.Only_Invoice);
                    onlyInvoice.Execute();

                    slog.action_identifier = Constants.Action_PostSalesInvoices;
                    slog.action_details = "Finished";
                    slog.action_failure = 0;
                    slog.action_failure_message = string.Empty;
                    slog.action_time = DateTime.Now.ToString();

                    DateTime endTime = DateTime.Now;
                    TimeSpan ts = endTime - startTime;
                    Console.WriteLine("Completed in: " + ts.TotalSeconds.ToString("F"));

                    LocalDB.InsertSyncLog(slog);

                    logger.message = "POST Sales Invoices finished in (" + ts.TotalSeconds.ToString("F") + " seconds)";
                    logger.Broadcast();

                ENDJOB:
                    Console.WriteLine("ENDJOB");
                }
                );

                thread.Start();
                //thread.Join();

                //await Console.Out.WriteLineAsync("Done POST Sales Invoice");
            }
            catch (ThreadAbortException e)
            {
                DpprException ex = new DpprException
                {
                    file_name = "JobPostSalesInvoice",
                    exception = e.Message,
                    time = DateTime.Now.ToString()
                };
                LocalDB.InsertException(ex);

                Console.WriteLine(Constants.Thread_Exception + e.Message);
            }
        }
    }
}