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
    public class JobAgentSync : IJob
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
                    slog.action_identifier = Constants.Action_AgentSync;
                    slog.action_details = Constants.Tbl_cms_login + Constants.Is_Starting;
                    slog.action_failure = 0;
                    slog.action_failure_message = string.Empty;
                    slog.action_time = DateTime.Now.ToString();
                    DateTime startTime = DateTime.Now;
                    LocalDB.InsertSyncLog(slog);

                    logger.message = "Agent sync is running";
                    logger.Broadcast();

                    Database mysql = new Database();
                    QNEApi api = new QNEApi();
                    dynamic json = api.GetByName("Agents");
                    api.Message("Agent sync is running");
                    HashSet<string> queryList = new HashSet<string>();
                    int RecordCount = 0;
                    string query = "INSERT INTO cms_login(staff_code, name, email, contact_no, role_id, remark) VALUES ";

                    string updateQuery = " ON DUPLICATE KEY UPDATE staff_code = VALUES(staff_code), name = VALUES(name), email = VALUES(email), contact_no = VALUES(contact_no), role_id=VALUES(role_id), remark = VALUES(remark);";

                    try
                    {
                        foreach (var item in json)
                        {
                            RecordCount++;

                            int roleID = 2;

                            string Values = string.Format("('{0}','{1}','{2}','{3}','{4}','{5}')", item.staffCode, item.name, item.email, item.mobileNo, roleID, item.remarks);


                            queryList.Add(Values);

                            if (queryList.Count % 2000 == 0)
                            {
                                string tmp_query = query;
                                tmp_query += string.Join(", ", queryList);
                                tmp_query += updateQuery;

                                mysql.Insert(tmp_query);

                                queryList.Clear();
                                tmp_query = string.Empty;

                                logger.message = string.Format("{0} agent records is inserted", RecordCount);
                                logger.Broadcast();
                            }
                        }

                        if (queryList.Count > 0)
                        {
                            query = query + string.Join(", ", queryList) + updateQuery;

                            mysql.Insert(query);

                            logger.message = string.Format("{0} agent records is inserted", RecordCount);
                            logger.Broadcast();
                        }
                    }
                    catch (Exception ex)
                    {
                        api.Message("QNEAPI + JobAgentSync.cs ---> " + ex.Message);
                    }

                    api.Message("Agent sync finished");

                    slog.action_identifier = Constants.Action_AgentSync;
                    slog.action_details = Constants.Tbl_cms_login + Constants.Is_Finished + string.Format(" ({0}) records", RecordCount);
                    slog.action_failure = 0;
                    slog.action_failure_message = string.Empty;
                    slog.action_time = DateTime.Now.ToString();

                    LocalDB.InsertSyncLog(slog);

                    DateTime endTime = DateTime.Now;
                    TimeSpan ts = endTime - startTime;
                    Console.WriteLine("Completed in: " + ts.TotalSeconds.ToString("F"));

                    logger.message = "Agent sync finished in (" + ts.TotalSeconds.ToString("F") + " seconds)";
                    logger.Broadcast();
                });

                thread.Start();
                //thread.Join();

                //await Console.Out.WriteLineAsync("Done Agent Sync");
            }
            catch (ThreadAbortException e)
            {
                DpprException ex = new DpprException
                {
                    file_name = "JobAgentSync",
                    exception = e.Message,
                    time = DateTime.Now.ToString()
                };
                LocalDB.InsertException(ex);

                Console.WriteLine(Constants.Thread_Exception + e.Message);
            }
        }
    }
}