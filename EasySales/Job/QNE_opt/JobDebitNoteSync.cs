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
    public class JobDebitNoteSync : IJob
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
                    slog.action_identifier = Constants.Action_DebitNoteSync;
                    slog.action_details = Constants.Tbl_cms_debitnote + Constants.Is_Starting;
                    slog.action_failure = 0;
                    slog.action_failure_message = string.Empty;
                    slog.action_time = DateTime.Now.ToString();
                    DateTime startTime = DateTime.Now;
                    LocalDB.InsertSyncLog(slog);

                    logger.message = "Debit note sync is running";
                    logger.Broadcast();

                    Database mysql = new Database();
                    QNEApi api = new QNEApi();
                    dynamic json = api.GetByName("supplierDNs");
                    api.Message("Debit note sync is running");
                    HashSet<string> queryList = new HashSet<string>();
                    int RecordCount = 0;
                    string query = "INSERT INTO cms_debitnote(dn_code, dn_date, dn_amount, cancelled) VALUES ";
                    string updateQuery = " ON DUPLICATE KEY UPDATE dn_code = VALUES(dn_code), dn_date = VALUES(dn_date), dn_amount = VALUES(dn_amount), cancelled=VALUES(cancelled);";

                    try
                    {
                        foreach (var item in json)
                        {
                            RecordCount++;

                            char cancelled = 'F';

                            string Values = string.Format("('{0}','{1}','{2}','{3}')", item.dnCode, item.dnDate, item.totalAmount, cancelled);
                            //,'{5}' , item.outstanding amount,  item.customer

                            queryList.Add(Values);

                            if (queryList.Count % 2000 == 0)
                            {
                                string tmp_query = query;
                                tmp_query += string.Join(", ", queryList);
                                tmp_query += updateQuery;

                                mysql.Insert(tmp_query);

                                queryList.Clear();
                                tmp_query = string.Empty;

                                logger.message = string.Format("{0} debit note records is inserted", RecordCount);
                                logger.Broadcast();
                            }
                        }
                        if (queryList.Count > 0)
                        {
                            query = query + string.Join(", ", queryList) + updateQuery;

                            mysql.Insert(query);

                            logger.message = string.Format("{0} debit note records is inserted", RecordCount);
                            logger.Broadcast();
                        }
                    }
                    catch (Exception ex)
                    {
                        api.Message(ex.Message + "[JobDebitNoteSync.cs]");
                    }
                    api.Message("Debit note sync finished");
                    slog.action_identifier = Constants.Action_DebitNoteSync;
                    slog.action_details = Constants.Tbl_cms_debitnote + Constants.Is_Finished + string.Format(" ({0}) records", RecordCount);
                    slog.action_failure = 0;
                    slog.action_failure_message = string.Empty;
                    slog.action_time = DateTime.Now.ToString();

                    DateTime endTime = DateTime.Now;
                    TimeSpan ts = endTime - startTime;
                    Console.WriteLine("Completed in: " + ts.TotalSeconds.ToString("F"));

                    LocalDB.InsertSyncLog(slog);

                    logger.message = "Debit note sync finished in (" + ts.TotalSeconds.ToString("F") + " seconds)";
                    logger.Broadcast();
                });

                thread.Start();
                //thread.Join();

                //await Console.Out.WriteLineAsync("Done debit note Sync");
            }
            catch (ThreadAbortException e)
            {
                DpprException ex = new DpprException
                {
                    file_name = "JobDebitNoteSync",
                    exception = e.Message,
                    time = DateTime.Now.ToString()
                };
                LocalDB.InsertException(ex);

                Console.WriteLine(Constants.Thread_Exception + e.Message);
            }
        }
    }
}
