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
    public class JobCustomerAgentSync : IJob
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
                    slog.action_identifier = Constants.Action_CustomerAgentSync;
                    slog.action_details = Constants.Tbl_cms_customer_salesperson + Constants.Is_Starting;
                    slog.action_failure = 0;
                    slog.action_failure_message = string.Empty;
                    slog.action_time = DateTime.Now.ToString();
                    DateTime startTime = DateTime.Now;
                    LocalDB.InsertSyncLog(slog);

                    logger.message = "Customer Agent sync is running";
                    logger.Broadcast();

                    QNEApi api = new QNEApi();
                    dynamic json = api.GetByName("Customers");
                    api.Message("Customer Agent sync is running");
                    HashSet<string> queryList = new HashSet<string>();
                    int RecordCount = 0;
                    string query = "INSERT INTO cms_customer_salesperson(salesperson_id, customer_id, active_status) VALUES ";

                    string updateQuery = " ON DUPLICATE KEY UPDATE salesperson_id = VALUES(salesperson_id), customer_id = VALUES(customer_id), active_status = VALUES(active_status);";

                    Database mysql = new Database();

                    ArrayList salespersonFromDb = mysql.Select("SELECT login_id, staff_code FROM cms_login;");
                    Dictionary<string, string> salespersonList = new Dictionary<string, string>();

                    for (int i = 0; i < salespersonFromDb.Count; i++)
                    {
                        Dictionary<string, string> each = (Dictionary<string, string>)salespersonFromDb[i];
                        salespersonList.Add(each["staff_code"], each["login_id"]);
                    }

                    ArrayList customerFromDb = mysql.Select("SELECT cust_id, cust_code FROM cms_customer;");
                    Dictionary<string, string> customerList = new Dictionary<string, string>();

                    for (int i = 0; i < customerFromDb.Count; i++)
                    {
                        Dictionary<string, string> each = (Dictionary<string, string>)customerFromDb[i];
                        customerList.Add(each["cust_code"], each["cust_id"]);
                    }

                    var custAgentlist = new List<KeyValuePair<string, string>>(); /* can keep same key value */

                    ArrayList inDBactiveCustAgent = mysql.Select("SELECT * FROM cms_customer_salesperson WHERE active_status = 1;");

                    for (int i = 0; i < inDBactiveCustAgent.Count; i++) 
                    {
                        Dictionary<string, string> each = (Dictionary<string, string>)inDBactiveCustAgent[i];
                        custAgentlist.Add(new KeyValuePair<string, string>(each["salesperson_id"], each["customer_id"]));
                    }
                    inDBactiveCustAgent.Clear();

                    try
                    {
                        foreach (var item in json)
                        {
                            RecordCount++;
                            string Values = string.Empty;

                            string _loginId = "0";
                            string _custId = "0";
                            string status;

                            if (item.status == "ACTIVE")
                            {
                                status = "1";
                            }
                            else
                            {
                                status = "0";
                            }

                            if (string.IsNullOrEmpty(item.salesPerson.ToString()) || !salespersonList.TryGetValue(item.salesPerson.ToString(), out _loginId))
                            {
                                _loginId = "0";
                            }

                            if (string.IsNullOrEmpty(item.companyCode.ToString()) || !customerList.TryGetValue(item.companyCode.ToString(), out _custId))
                            {
                                _custId = "0";
                            }

                            int.TryParse(_loginId, out int LoginId);
                            int.TryParse(_custId, out int CustomerId);
                            int.TryParse(status, out int ActiveStatus);

                            if (custAgentlist.Contains(new KeyValuePair<string, string>(_loginId, _custId)))
                            {
                                int index = custAgentlist.IndexOf(new KeyValuePair<string, string>(_loginId, _custId));
                                if (index != -1)
                                {
                                    custAgentlist.RemoveAt(index);
                                }
                            }
                            Console.WriteLine(custAgentlist.Count);

                            if (CustomerId != 0 && LoginId != 0)
                            {
                                Values = string.Format("('{0}','{1}','{2}')", LoginId, CustomerId, ActiveStatus);
                                queryList.Add(Values);
                            }

                            //string Values = string.Format("('{0}','{1}','{2}')", LoginId, CustomerId, ActiveStatus);
                            //queryList.Add(Values);

                            if (queryList.Count > 0 && queryList.Count % 2000 == 0) //if still got issue, insert at the end only not by batch
                            {
                                string tmp_query = query;
                                tmp_query += string.Join(", ", queryList);
                                tmp_query += updateQuery;

                                mysql.Insert(tmp_query);

                                queryList.Clear();
                                tmp_query = string.Empty;

                                logger.message = string.Format("{0} Customer Agent records is inserted", RecordCount);
                                logger.Broadcast();
                            }
                        }

                        Console.WriteLine("RecordCount: " + RecordCount);

                        if (queryList.Count > 0)
                        {
                            query = query + string.Join(", ", queryList) + updateQuery;

                            //mysql.Insert("UPDATE cms_customer_salesperson SET active_status = 0"); //deactivated first || if got issue enabled this
                            mysql.Insert(query);

                            logger.message = string.Format("{0} Customer Agent records is inserted", RecordCount);
                            logger.Broadcast();


                            if (custAgentlist.Count > 0)
                            {
                                logger.Broadcast("Cust-Agent to be deactivated: " + custAgentlist.Count);

                                string inactive = "INSERT INTO cms_customer_salesperson(salesperson_id, customer_id, active_status) VALUES ";
                                string inactive_duplicate = " ON DUPLICATE KEY UPDATE salesperson_id = VALUES(salesperson_id), customer_id = VALUES(customer_id), active_status = VALUES(active_status);";

                                for (int i = 0; i < custAgentlist.Count; i++)
                                {
                                    string loginId = custAgentlist[i].Key.ToString();
                                    string custId = custAgentlist[i].Value.ToString();
                                    string _query = string.Format("('{0}','{1}',0)", loginId, custId);
                                    mysql.Insert(inactive + _query + inactive_duplicate);
                                    mysql.Message("cust-agent deactivated query: " + inactive + _query + inactive_duplicate);
                                }

                                logger.message = custAgentlist + " Customer-Agent records have been deactivated";
                                logger.Broadcast();

                                custAgentlist.Clear();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        api.Message("QNEAPI + JobCustomerAgentSync.cs ---> " + ex.Message);
                    }

                    api.Message("Customer Agent sync finished");
                    slog.action_identifier = Constants.Action_CustomerAgentSync;
                    slog.action_details = Constants.Tbl_cms_customer_salesperson + Constants.Is_Finished + string.Format(" ({0}) records", RecordCount);
                    slog.action_failure = 0;
                    slog.action_failure_message = string.Empty;
                    slog.action_time = DateTime.Now.ToString();

                    DateTime endTime = DateTime.Now;
                    TimeSpan ts = endTime - startTime;
                    Console.WriteLine("Completed in: " + ts.TotalSeconds.ToString("F"));

                    LocalDB.InsertSyncLog(slog);
                    logger.message = "Customer Agent sync finished in (" + ts.TotalSeconds.ToString("F") + " seconds)";
                    logger.Broadcast();
                });

                thread.Start();
                //thread.Join();

                //await Console.Out.WriteLineAsync("Done Customer Agent Sync");
            }
            catch (ThreadAbortException e)
            {
                DpprException ex = new DpprException
                {
                    file_name = "JobCustomerAgentSync",
                    exception = e.Message,
                    time = DateTime.Now.ToString()
                };
                LocalDB.InsertException(ex);

                Console.WriteLine(Constants.Thread_Exception + e.Message);
            }
        }
    }
}
