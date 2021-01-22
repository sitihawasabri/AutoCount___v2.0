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
    public class JobBranchSync : IJob
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
                    slog.action_identifier = Constants.Action_BranchSync;                               /*check again */
                    slog.action_details = Constants.Tbl_cms_customer_branch + Constants.Is_Starting;    /*check again */
                    slog.action_failure = 0;
                    slog.action_failure_message = string.Empty;
                    slog.action_time = DateTime.Now.ToString();

                    DateTime startTime = DateTime.Now;

                    LocalDB.InsertSyncLog(slog);
                    logger.message = "----------------------------------------------------------------------------";
                    logger.Broadcast();
                    logger.message = "Branch sync is running";
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

                    Database _mysql = new Database();

                    Dictionary<string, string> customerList = new Dictionary<string, string>();
                    ArrayList customerFromDb = _mysql.Select("SELECT cust_code, cust_id FROM cms_customer;");

                    for (int i = 0; i < customerFromDb.Count; i++)
                    {
                        Dictionary<string, string> each = (Dictionary<string, string>)customerFromDb[i];
                        customerList.Add(each["cust_code"], each["cust_id"]);
                        /*using key (cust_code) to get value (cust_id) */
                    }

                    customerFromDb.Clear();

                    Dictionary<string, string> salespersonList = new Dictionary<string, string>();
                    ArrayList salespersonFromDb = _mysql.Select("SELECT login_id, staff_code FROM cms_login");

                    for (int i = 0; i < salespersonFromDb.Count; i++)
                    {
                        Dictionary<string, string> each = (Dictionary<string, string>)salespersonFromDb[i];
                        salespersonList.Add(each["staff_code"], each["login_id"]);
                        /*using key (staff_code) to get value (login_id) */
                    }
                    salespersonFromDb.Clear();

                    dynamic lMain, lRptVar, lbranch;
                    string Code, Agent, Latitude, Longitude, _Cust_code, Branch_code, Branch_name, Phone1, Phone2, Fax1, Address1, Address2, Address3, Address4, Contact;
                    string query, updateQuery, custSQL, custSQLupdate, query1;

                    HashSet<string> queryList = new HashSet<string>();
                    HashSet<string> latLongArray = new HashSet<string>();

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

                        query = "INSERT INTO cms_customer_branch(cust_id, agent_id, cust_code, branch_code, branch_name, branch_attn, branch_phone, branch_fax, billing_address1, billing_address2, billing_address3, billing_address4, billing_state, billing_postcode, billing_country, shipping_address1, shipping_address2, shipping_address3, shipping_address4, shipping_state, shipping_postcode, shipping_country, branch_area, branch_remark, branch_active) VALUES ";

                        updateQuery = " ON DUPLICATE KEY UPDATE cust_id = VALUES(cust_id), agent_id = VALUES(agent_id), branch_code = VALUES(branch_code), branch_name = VALUES(branch_name), branch_attn = VALUES(branch_attn), branch_phone = VALUES(branch_phone), branch_fax = VALUES(branch_fax), billing_address1 = VALUES(billing_address1), billing_address2 = VALUES(billing_address2), billing_address3 = VALUES(billing_address3), billing_address4 = VALUES(billing_address4), billing_state = VALUES(billing_state), billing_postcode = VALUES(billing_postcode), billing_country = VALUES(billing_country), shipping_address1 = VALUES(shipping_address1), shipping_address2 = VALUES(shipping_address2), shipping_address3 = VALUES(shipping_address3), shipping_address4 = VALUES(shipping_address4), shipping_state = VALUES(shipping_state), shipping_postcode = VALUES(shipping_postcode), shipping_country = VALUES(shipping_country), branch_area = VALUES(branch_area), branch_remark = VALUES(branch_remark), branch_active = VALUES(branch_active)";

                        custSQL = "INSERT INTO cms_customer (cust_code, latitude, longitude) VALUES ";
                        custSQLupdate = " ON DUPLICATE KEY UPDATE latitude=VALUES(latitude), longitude=VALUES(longitude)";

                        Code = lMain.FindField("CODE").AsString;

                        while (!lMain.eof)
                        {
                            Code = lMain.FindField("CODE").AsString;

                            string _custId = "0";

                            if (!customerList.TryGetValue(Code, out _custId))
                            {
                                _custId = "0";
                            }

                            int.TryParse(_custId, out int CustId);

                            int Branch_num = 0;

                            string AgentId = "0";

                            while (Code == lbranch.FindField("CODE").AsString)
                            {
                                RecordCount++;

                                if (AgentId == "0")
                                {
                                    Agent = lMain.FindField("AGENT").AsString;
                                    Agent = Agent.ToUpper();

                                    if (string.IsNullOrEmpty(Agent) || !salespersonList.TryGetValue(Agent, out AgentId))
                                    {
                                        AgentId = "0";
                                    }
                                }
                                int.TryParse(AgentId, out int _agentId);

                                Latitude = lbranch.FindField("GEOLAT").AsString;
                                Longitude = lbranch.FindField("GEOLONG").AsString;

                                Database.Sanitize(ref Code);
                                string latLong = string.Format("('{0}','{1}','{2}')", Code, Latitude, Longitude);
                                latLongArray.Add(latLong);
                                //latLongArray[] = "("{ _cust_code}","{ latitude}","{ longitude}")";  //insert above values in array

                                Branch_num++;                                                  // to make the branch code unique
                                Branch_code = lbranch.FindField("CODE").AsString;
                                //Branch_code = "-"+ Branch_num;                                     /*checkagain*/
                                Branch_code = Branch_code + "-" + Branch_num.ToString();             /*checkagain*/
                                Branch_name = "(" + Branch_code + ") " + lbranch.FindField("BRANCHNAME").AsString;
                                Phone1 = lbranch.FindField("PHONE1").AsString;
                                Phone2 = lbranch.FindField("PHONE2").AsString;
                                Fax1 = lbranch.FindField("FAX1").AsString;
                                Address1 = lbranch.FindField("ADDRESS1").AsString;
                                Address2 = lbranch.FindField("ADDRESS2").AsString;
                                Address3 = lbranch.FindField("ADDRESS3").AsString;
                                Address4 = lbranch.FindField("ADDRESS4").AsString;
                                Contact = lbranch.FindField("ATTENTION").AsString;

                                Database.Sanitize(ref Latitude);
                                Database.Sanitize(ref Longitude);
                                Database.Sanitize(ref Branch_code);
                                Database.Sanitize(ref Branch_name);
                                Database.Sanitize(ref Phone1);
                                Database.Sanitize(ref Phone2);
                                Database.Sanitize(ref Fax1);
                                Database.Sanitize(ref Address1);
                                Database.Sanitize(ref Address2);
                                Database.Sanitize(ref Address3);
                                Database.Sanitize(ref Address4);
                                Database.Sanitize(ref Contact);

                                string Values = string.Format("('{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}','{8}','{9}','{10}','{11}','{12}','{13}', '{14}','{15}','{16}','{17}','{18}','{19}','{20}','{21}', '{22}','{23}','{24}')", CustId, AgentId, Code, Branch_code, Branch_name, Contact, Phone1, Fax1, Address1, Address2, Address3, Address4, "", "", "", Address1, Address2, Address3, Address4, "", "", "", "", "", 1);

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

                                    logger.message = string.Format("{0} branch records is inserted", RecordCount);
                                    logger.Broadcast();
                                }

                                lbranch.Next();

                                if (lbranch.eof)
                                {
                                    break;
                                }
                            }
                            lMain.Next();
                        }

                        if (queryList.Count > 0)
                        {
                            query = query + string.Join(", ", queryList) + updateQuery;

                            Database mysql = new Database();
                            //mysql.Insert("UPDATE cms_customer_branch SET branch_active = '0' WHERE cust_code = '" + Code + "'");
                            mysql.Insert(query);

                            query = custSQL + string.Join(", ", latLongArray) + custSQLupdate;
                            mysql.Insert(query);
                            //mysql.Close();

                            logger.message = string.Format("{0} branch records is inserted", RecordCount);
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
                                file_name = "SQLAccounting + JobBranchSync",
                                exception = exc.Message,
                                time = DateTime.Now.ToString()
                            };
                            LocalDB.InsertException(exception);
                        }
                    }

                    slog.action_identifier = Constants.Action_BranchSync;
                    slog.action_details = Constants.Tbl_cms_customer_branch + Constants.Is_Finished + string.Format(" ({0}) records", RecordCount);
                    slog.action_failure = 0;
                    slog.action_failure_message = string.Empty;
                    slog.action_time = DateTime.Now.ToString();

                    DateTime endTime = DateTime.Now;
                    TimeSpan ts = endTime - startTime;
                    Console.WriteLine("Completed in: " + ts.TotalSeconds.ToString("F"));

                    LocalDB.InsertSyncLog(slog);

                    logger.message = "Branch sync finished in (" + ts.TotalSeconds.ToString("F") + " seconds)";
                    logger.Broadcast();
                });

                thread.Start();
                //thread.Join();

                //await Console.Out.WriteLineAsync("Branch Sync");
            }
            catch (ThreadAbortException e)
            {
                DpprException ex = new DpprException
                {
                    file_name = "JobBranchSync",
                    exception = e.Message,
                    time = DateTime.Now.ToString()
                };
                LocalDB.InsertException(ex);

                Console.WriteLine(Constants.Thread_Exception + e.Message);
            }
        }
    }
}