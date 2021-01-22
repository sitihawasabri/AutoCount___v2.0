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
    public class JobStockCategoriesSync : IJob
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
                    slog.action_identifier = Constants.Action_StockCategoriesSync;
                    slog.action_details = Constants.Tbl_cms_product_category + Constants.Is_Starting;
                    slog.action_failure = 0;
                    slog.action_failure_message = string.Empty;
                    slog.action_time = DateTime.Now.ToString();
                    DateTime startTime = DateTime.Now;
                    LocalDB.InsertSyncLog(slog);

                    logger.message = "Stock categories sync is running";
                    logger.Broadcast();

                    QNEApi api = new QNEApi();
                    dynamic json = api.GetByName("StockCategories");
                    api.Message("Stock categories sync is running");
                    HashSet<string> queryList = new HashSet<string>();
                    int RecordCount = 0;
                    string query = "INSERT INTO cms_product_category(categoryIdentifierId, category_name, sequence_no, category_status) VALUES ";

                    string updateQuery = " ON DUPLICATE KEY UPDATE categoryIdentifierId = VALUES(categoryIdentifierId), category_name = VALUES(category_name), sequence_no = VALUES(sequence_no), category_status = VALUES(category_status);";

                    Database mysql = new Database();

                    ArrayList exCode = new ArrayList();

                    try
                    {
                        foreach (var item in json)
                        {
                            RecordCount++;
                            int activeValue = 0;

                            String categoryCode = item.categoryCode,
                                   description = item.description;

                            if (item.isActive == true)
                            {
                                activeValue = 1;
                            }

                            Database.Sanitize(ref categoryCode);
                            Database.Sanitize(ref description);

                            string Values = string.Format("('{0}','{1}','{2}','{3}')", categoryCode, description, RecordCount, activeValue);

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

                                logger.message = string.Format("{0} stock categories records is inserted", RecordCount);
                                logger.Broadcast();
                            }
                        }

                        if (queryList.Count > 0)
                        {
                            query = query + string.Join(", ", queryList) + updateQuery;

                            mysql.Insert(query);

                            logger.message = string.Format("{0} stock categories records is inserted", RecordCount);
                            logger.Broadcast();
                        }
                    }
                    catch (Exception ex)
                    {
                        api.Message("QNEAPI + JobStockCategoriesSync.cs ---> " + ex.Message);
                    }
                    api.Message("Stock categories sync finished");
                    slog.action_identifier = Constants.Action_StockCategoriesSync;
                    slog.action_details = Constants.Tbl_cms_product_category + Constants.Is_Finished + string.Format(" ({0}) records", RecordCount);
                    slog.action_failure = 0;
                    slog.action_failure_message = string.Empty;
                    slog.action_time = DateTime.Now.ToString();

                    DateTime endTime = DateTime.Now;
                    TimeSpan ts = endTime - startTime;
                    Console.WriteLine("Completed in: " + ts.TotalSeconds.ToString("F"));

                    LocalDB.InsertSyncLog(slog);

                    logger.message = "Stock categories sync finished in (" + ts.TotalSeconds.ToString("F") + " seconds)";
                    logger.Broadcast();
                });

                thread.Start();
                //thread.Join();

                //await Console.Out.WriteLineAsync("Done Stock Categories Sync");
            }
            catch (ThreadAbortException e)
            {
                DpprException ex = new DpprException
                {
                    file_name = "JobStockCategoriesSync",
                    exception = e.Message,
                    time = DateTime.Now.ToString()
                };
                LocalDB.InsertException(ex);

                Console.WriteLine(Constants.Thread_Exception + e.Message);
            }
        }
    }
}