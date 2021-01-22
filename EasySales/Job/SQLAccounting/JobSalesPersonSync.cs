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
using Quartz.Impl.Matchers;
using System.Drawing;
using System.Reflection;

namespace EasySales.Job
{
    [DisallowConcurrentExecution]
    public class JobSalesPersonSync : IJob
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
                   // await context.Scheduler.PauseJobs(GroupMatcher<JobKey>.GroupContains(Constants.Job_Group_Sync));

                    Thread.CurrentThread.IsBackground = true;

                    int RecordCount = 0;
                    GlobalLogger logger = new GlobalLogger();

                    /**
                     * Here we will run SQLAccounting Codes
                     * */

                    DpprSyncLog slog = new DpprSyncLog();
                    slog.action_identifier = Constants.Action_AgentSync;
                    slog.action_details = Constants.Tbl_cms_login + Constants.Is_Starting;
                    slog.action_failure = 0;
                    slog.action_failure_message = string.Empty;
                    slog.action_time = DateTime.Now.ToString();

                    DateTime startTime = DateTime.Now;

                    LocalDB.InsertSyncLog(slog);
                    logger.message = "----------------------------------------------------------------------------";
                    logger.Broadcast();
                    logger.message = "Agent sync is running";
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

                    dynamic lRptVar, lMain, lbranch;
                    string Code, Description, Active;
                    string query, updateQuery;
                    HashSet<string> queryList = new HashSet<string>();

                    try
                    {
                        lRptVar = ComServer.RptObjects.Find("Common.Agent.RO");

                        lRptVar.CalculateReport();

                        lMain = lRptVar.DataSets.Find("cdsMain");

                        query = "INSERT INTO cms_login(staff_code, name, login_status) VALUES ";

                        updateQuery = " ON DUPLICATE KEY UPDATE staff_code = VALUES(staff_code), name = VALUES(name), login_status = VALUES(login_status);";

                        lMain.First();

                        while (!lMain.eof)
                        {
                            RecordCount++;

                            Code = lMain.FindField("CODE").AsString;
                            Description = lMain.FindField("DESCRIPTION").AsString;
                            Active = lMain.FindField("ISACTIVE").AsString;

                            int activeValue = 1;

                            if (Active == "F")
                            {
                                activeValue = 0;
                            }

                            Database.Sanitize(ref Code);
                            Database.Sanitize(ref Description);
                            Database.Sanitize(ref Active);

                            string Values = string.Format("('{0}','{1}','{2}')", Code, Description, activeValue);

                            queryList.Add(Values);

                            if (queryList.Count % 2000 == 0)
                            {
                                string tmp_query = query;
                                tmp_query += string.Join(", ", queryList);
                                tmp_query += updateQuery;

                                Database mysql = new Database();
                                mysql.Insert(tmp_query);
                                //mysql.Close();

                                queryList.Clear();
                                tmp_query = string.Empty;

                                logger.message = string.Format("{0} salesperson records is inserted", RecordCount);
                                logger.Broadcast();
                            }
                            lMain.Next();
                        }

                        if (queryList.Count > 0)
                        {
                            query = query + string.Join(", ", queryList) + updateQuery;

                            Database mysql = new Database();
                            mysql.Insert(query);
                            //mysql.Close();

                            logger.message = string.Format("{0} salesperson records is inserted", RecordCount);
                            logger.Broadcast();
                        }
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
                                file_name = "SQLAccounting + JobCust-AgentSync",
                                exception = exc.Message,
                                time = DateTime.Now.ToString()
                            };
                            LocalDB.InsertException(exception);
                        }
                    }

                    logger.message = "----------------------------------------------------------------------------";
                    logger.Broadcast();
                    logger.message = "Customer - Agent  sync is running";
                    logger.Broadcast();
                    logger.message = "----------------------------------------------------------------------------";
                    logger.Broadcast();

                    Database _mysql = new Database();
                    dynamic backendRule = new CheckBackendRule().CheckTablesExist().GetSettingByTableName("cms_customer_salesperson");
                    var CustSPGroup = new List<KeyValuePair<string, string>>();

                    //string cust_code = string.Empty;
                    //string sp = string.Empty;

                    if (backendRule.Count > 0)
                    {
                        foreach(var rule in backendRule)
                        {
                            Dictionary<string, ArrayList> dictObj = rule.ToObject<Dictionary<string, ArrayList>>();
                            foreach (string cust_code in dictObj.Keys)
                            {
                                foreach(string sp in dictObj[cust_code])
                                {
                                    CustSPGroup.Add(new KeyValuePair<string, string>(cust_code, sp));
                                }
                            }
                        }
                    }

                    string _Code, Agent;
                    string sql, sqlquery;

                    HashSet<string> _queryList = new HashSet<string>();

                    sql = "INSERT INTO cms_customer_salesperson(customer_id, salesperson_id, active_status) VALUES ";
                    sqlquery = " ON DUPLICATE KEY UPDATE salesperson_id = VALUES(salesperson_id), active_status = VALUES(active_status)";

                    Dictionary<string, string> salespersonList = new Dictionary<string, string>();
                    ArrayList salespersonFromDb = _mysql.Select("SELECT login_id, staff_code FROM cms_login");

                    for (int i = 0; i < salespersonFromDb.Count; i++)
                    {
                        Dictionary<string, string> each = (Dictionary<string, string>)salespersonFromDb[i];
                        salespersonList.Add(each["staff_code"], each["login_id"]);
                    }
                    salespersonFromDb.Clear();

                    Dictionary<string, string> customerList = new Dictionary<string, string>();
                    ArrayList customerFromDb = _mysql.Select("SELECT cust_code, cust_id FROM cms_customer;"); //get only cust_code and cust_id

                    for (int i = 0; i < customerFromDb.Count; i++)
                    {
                        Dictionary<string, string> each = (Dictionary<string, string>)customerFromDb[i];
                        customerList.Add(each["cust_code"], each["cust_id"]);
                    }

                    customerFromDb.Clear();

                    try
                    {
                        lRptVar = ComServer.RptObjects.Find("Customer.RO");

                        lRptVar.Params.Find("SelectDate").Value = false;
                        lRptVar.Params.Find("AllAgent").Value = true;
                        lRptVar.Params.Find("AllArea").Value = true;
                        lRptVar.Params.Find("AllCompany").Value = true;
                        lRptVar.Params.Find("AllCurrency").Value = true;
                        lRptVar.Params.Find("AllTerms").Value = true;
                        lRptVar.Params.Find("AllCompanyCategory").Value = true;
                        lRptVar.CalculateReport();

                        lMain = lRptVar.DataSets.Find("cdsMain");
                        lbranch = lRptVar.DataSets.Find("cdsBranch");

                        lMain.First();
                        lbranch.First();

                        RecordCount = 0;

                        while (!lMain.eof)
                        {
                            RecordCount++;
                            string Values = string.Empty;

                            _Code = lMain.FindField("CODE").AsString;

                            while (_Code == lbranch.FindField("CODE").AsString)
                            {
                                lbranch.Next();
                                if (lbranch.eof)
                                {
                                    break;
                                }
                            }

                            Agent = lMain.FindField("AGENT").AsString;

                            string _custId = "0";

                            if (!customerList.TryGetValue(_Code, out _custId))
                            {
                                _custId = "0";
                            }

                            int.TryParse(_custId, out int CustId);

                            string _AgentId = "0";

                            if (!salespersonList.TryGetValue(Agent, out _AgentId))
                            {
                                _AgentId = "0";
                            }

                            int.TryParse(_AgentId, out int AgentId);

                            int activeValue = 0;

                            if (CustId != 0 && AgentId != 0)
                            {
                                activeValue = 1;
                            }

                            //if cust_id = 0, dont insert
                            if (CustId != 0)
                            {
                                Values = string.Format("('{0}','{1}','{2}')", CustId, AgentId, activeValue);
                                _queryList.Add(Values);
                            }

                            lMain.Next();

                        }

                        if (_queryList.Count > 0)
                        {
                            _mysql.Insert("UPDATE cms_customer_salesperson SET active_status = 0");

                            sql = sql + string.Join(", ", _queryList) + sqlquery;

                            Database mysql = new Database();
                            mysql.Insert(sql);

                            mysql.Insert("INSERT INTO cms_customer(cust_code, customer_zone) SELECT c.cust_code, cz.zone_id FROM cms_customer c INNER JOIN cms_customer_salesperson cs ON c.cust_id = cs.customer_id AND cs.active_status = 1 LEFT JOIN cms_login l ON l.login_id = cs.salesperson_id LEFT JOIN cms_customer_zone cz ON l.staff_code = cz.zone_name ON DUPLICATE KEY UPDATE cust_code = VALUES(cust_code), customer_zone = VALUES(customer_zone);");

                            logger.message = string.Format("{0} customer_salesperson records is inserted", RecordCount);
                            logger.Broadcast();
                        }

                        if(CustSPGroup.Count > 0) //aquatic
                        {
                            Database mysql = new Database();
                            for (int i = 0; i < CustSPGroup.Count; i++)
                            {
                                string custId = string.Empty;
                                string cust_code = CustSPGroup.ElementAt(i).Key;
                                if (!customerList.TryGetValue(cust_code, out custId))
                                {
                                    custId = "0";
                                }
                                string agentId = CustSPGroup.ElementAt(i).Value;

                                string update = "UPDATE cms_customer_salesperson SET active_status = 1 WHERE customer_id = " + custId + " AND salesperson_id = " + agentId + "";
                                if (custId != "0")
                                {
                                    bool updated = mysql.Insert(update);
                                }
                                
                            }
                        }
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
                                file_name = "SQLAccounting + JobCust-AgentSync",
                                exception = exc.Message,
                                time = DateTime.Now.ToString()
                            };
                            LocalDB.InsertException(exception);
                        }
                    }

                    slog.action_identifier = Constants.Action_AgentSync;
                    slog.action_details = Constants.Tbl_cms_login + Constants.Is_Finished + string.Format(" ({0}) records", RecordCount);
                    slog.action_failure = 0;
                    slog.action_failure_message = string.Empty;
                    slog.action_time = DateTime.Now.ToString();

                    DateTime endTime = DateTime.Now;
                    TimeSpan ts = endTime - startTime;
                    Console.WriteLine("Completed in: " + ts.TotalSeconds.ToString("F"));

                    LocalDB.InsertSyncLog(slog);

                    logger.message = "Salesperson sync finished in (" + ts.TotalSeconds.ToString("F") + " seconds)";
                    logger.Broadcast();

                    //await context.Scheduler.ResumeJobs(GroupMatcher<JobKey>.GroupContains(Constants.Job_Group_Sync));
                });

                thread.Start();
                //thread.Join();

                //await Console.Out.WriteLineAsync("Salesperson Sync");
            }
            catch (ThreadAbortException e)
            { 
                DpprException ex = new DpprException
                {
                    file_name = "JobSalesPersonSync",
                    exception = e.Message,
                    time = DateTime.Now.ToString()
                };
                LocalDB.InsertException(ex);

                Console.WriteLine(Constants.Thread_Exception + e.Message);
            }
        }
    }
}