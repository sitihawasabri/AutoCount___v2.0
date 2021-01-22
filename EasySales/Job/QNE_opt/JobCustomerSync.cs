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
    public class JobCustomerSync : IJob
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
                    slog.action_identifier = Constants.Action_CustomerSync;
                    slog.action_details = Constants.Tbl_cms_customer + Constants.Is_Starting;
                    slog.action_failure = 0;
                    slog.action_failure_message = string.Empty;
                    slog.action_time = DateTime.Now.ToString();
                    DateTime startTime = DateTime.Now;
                    LocalDB.InsertSyncLog(slog);

                    logger.message = "Customer sync is running";
                    logger.Broadcast();

                    QNEApi api = new QNEApi();
                    dynamic json = api.GetByName("Customers");
                    api.Message("Customer sync is running");
                    Database mysql = new Database();
                    HashSet<string> queryList = new HashSet<string>();
                    HashSet<string> creditLimitQueryList = new HashSet<string>();

                    int RecordCount = 0;

                    string query = "INSERT INTO cms_customer(cust_code, cust_company_name, cust_incharge_person, cust_email, cust_tel, cust_fax, billing_address1, billing_address2, billing_address3, billing_address4, termcode, customer_status, current_balance) VALUES ";
                    string updateQuery = " ON DUPLICATE KEY UPDATE cust_code = VALUES(cust_code), cust_company_name = VALUES(cust_company_name), cust_incharge_person = VALUES(cust_incharge_person), cust_email = VALUES(cust_email), cust_tel=VALUES(cust_tel), cust_fax = VALUES(cust_fax), billing_address1 = VALUES(billing_address1), billing_address2 = VALUES(billing_address2), billing_address3 = VALUES(billing_address3), billing_address4 = VALUES(billing_address4), termcode = VALUES(termcode), customer_status = VALUES(customer_status), current_balance = VALUES(current_balance);";

                    string creditLimitQuery = "INSERT INTO cms_customer(cust_code, credit_limit) VALUES ";

                    string creditLimitUpdateQuery = " ON DUPLICATE KEY UPDATE cust_code = VALUES(cust_code), credit_limit = VALUES(credit_limit);";

                    try
                    {
                        foreach (var item in json)
                        {
                            RecordCount++;

                            string status;

                            if (item.status == "ACTIVE")
                            {
                                status = "1";
                            }
                            else
                            {
                                status = "0";
                            }

                            int.TryParse(status, out int cust_status);
                            string company_code = item.companyCode.ToString(), company_name = item.companyName.ToString(), contact = item.contactPerson.ToString(), email = item.email.ToString(), phone_no = item.phoneNo1.ToString(), fax_no = item.faxNo1.ToString(), address1 = item.address1.ToString(), address2 = item.address2.ToString(), address3 = item.address3.ToString(), address4 = item.address4.ToString(), term = item.term.ToString(), currentBalance = item.currentBalance.ToString();

                            Database.Sanitize(ref company_code);
                            Database.Sanitize(ref company_name);
                            Database.Sanitize(ref contact);
                            Database.Sanitize(ref email);
                            Database.Sanitize(ref phone_no);
                            Database.Sanitize(ref fax_no);
                            Database.Sanitize(ref address1);
                            Database.Sanitize(ref address2);
                            Database.Sanitize(ref address3);
                            Database.Sanitize(ref address4);
                            Database.Sanitize(ref term);
                            Database.Sanitize(ref currentBalance);

                            string Values = string.Format("('{0}','{1}','{2}','{3}','{4}','{5}','{6}', '{7}', '{8}', '{9}', '{10}', '{11}', '{12}')", company_code, company_name, contact, email, phone_no, fax_no, address1, address2, address3, address4, term, cust_status, currentBalance);

                            queryList.Add(Values);

                            if (queryList.Count % 2000 == 0)
                            {
                                string tmp_query = query;
                                tmp_query += string.Join(", ", queryList);
                                tmp_query += updateQuery;

                                mysql.Insert(tmp_query);

                                queryList.Clear();
                                tmp_query = string.Empty;

                                logger.message = string.Format("{0} customer records is inserted", RecordCount);
                                logger.Broadcast();
                            }

                        }
                        if (queryList.Count > 0)
                        {
                            query = query + string.Join(", ", queryList) + updateQuery;

                            mysql.Insert(query);

                            logger.message = string.Format("{0} customer records is inserted", RecordCount);
                            logger.Broadcast();
                        }
                    }
                    catch (Exception ex)
                    {
                        api.Message("QNEAPI + JobCustomerSync.cs ---> " + ex.Message);
                    }

                    int CLCount = 0;

                    ArrayList customer = mysql.Select("SELECT cust_code, current_balance FROM cms_customer;");

                    dynamic jsonRule = new CheckBackendRule().CheckTablesExist().GetSettingByTableName("cms_customer");

                    Dictionary<string, string> codeStatusPair = new Dictionary<string, string>();

                    int check_credit_limit = 0;

                    if (jsonRule.Count > 0)
                    {
                        foreach (var condition in jsonRule)
                        {
                            dynamic _condition = condition.condition;

                            dynamic _check_credit_limit = _condition.check_credit_limit;
                            if(_check_credit_limit != null)
                            {
                                if (_check_credit_limit != 0)
                                {
                                    check_credit_limit = _check_credit_limit;
                                }
                            }
                        }
                    }
                    else
                    {
                        goto ENDJOB;
                    }

                    try
                    {
                        for (int i = 0; i < customer.Count; i++)
                        {
                            Dictionary<string, string> dict = (Dictionary<string, string>)customer[i];

                            dynamic jsonCreditLimit = api.GetByName("CreditControls/Find", new Parameter("companyCode", dict["cust_code"], ParameterType.QueryString));

                            foreach (var itemCL in jsonCreditLimit)
                            {
                                CLCount++;
                                string _creditLimit = "0";
                                string _current_balance;
                                string status;

                                if (itemCL.creditLimit != 0)
                                {
                                    _creditLimit = itemCL.creditLimit;
                                }
                                double.TryParse(_creditLimit, out double creditLimit);

                                string custCode = dict["cust_code"];
                                Database.Sanitize(ref custCode);

                                if (check_credit_limit != 0)
                                {
                                    _current_balance = dict["current_balance"];
                                    double.TryParse(_current_balance, out double current_balance);

                                    if (current_balance > creditLimit)
                                    {
                                        status = "0";
                                    }
                                    else
                                    {
                                        status = "1";
                                    }
                                    codeStatusPair.Add(custCode, status);
                                }

                                string Values = string.Format("('{0}','{1}')", custCode, creditLimit);

                                creditLimitQueryList.Add(Values);

                                if (creditLimitQueryList.Count % 2000 == 0)
                                {
                                    string tmp_query = creditLimitQuery;
                                    tmp_query += string.Join(", ", creditLimitQueryList);
                                    tmp_query += creditLimitUpdateQuery;

                                    mysql.Insert(tmp_query);

                                    creditLimitQueryList.Clear();
                                    tmp_query = string.Empty;

                                    logger.message = string.Format("{0} customer credit limit records is inserted", CLCount);
                                    logger.Broadcast();
                                }
                            }
                        }

                        if (codeStatusPair.Count > 0) //IF current balance > credit limit
                        {
                            string activeStatusQuery = "INSERT INTO cms_customer(cust_code, customer_status) VALUES ";
                            string updateActiveStatusQuery = " ON DUPLICATE KEY UPDATE cust_code = VALUES(cust_code), customer_status = VALUES(customer_status);";

                            for (int i = 0; i < codeStatusPair.Count; i++)
                            {
                                string _code = codeStatusPair.ElementAt(i).Key;

                                string _activeStatus = codeStatusPair.ElementAt(i).Value;
                                int.TryParse(_activeStatus, out int activeStatus);

                                Database.Sanitize(ref _code);

                                string _query = string.Format("('{0}','{1}')", _code, activeStatus);

                                mysql.Insert(activeStatusQuery + _query + updateActiveStatusQuery);
                            }
                            codeStatusPair.Clear();
                        }

                        if (creditLimitQueryList.Count > 0)
                        {
                            creditLimitQuery = creditLimitQuery + string.Join(", ", creditLimitQueryList) + creditLimitUpdateQuery;

                            mysql.Insert(creditLimitQuery);

                            logger.message = string.Format("{0} customer credit limit records is inserted", CLCount);
                            logger.Broadcast();
                        }
                    }
                    catch (Exception ex)
                    {
                        api.Message("QNEAPI [CreditControls/Find] + JobCustomerSync.cs  ---> " + ex.Message);
                    }

                    ENDJOB:

                    api.Message("Customer sync finished");

                    slog.action_identifier = Constants.Action_CustomerSync;
                    slog.action_details = Constants.Tbl_cms_customer + Constants.Is_Finished + string.Format(" ({0}) records", RecordCount);
                    slog.action_failure = 0;
                    slog.action_failure_message = string.Empty;
                    slog.action_time = DateTime.Now.ToString();

                    DateTime endTime = DateTime.Now;
                    TimeSpan ts = endTime - startTime;
                    Console.WriteLine("Completed in: " + ts.TotalSeconds.ToString("F"));

                    LocalDB.InsertSyncLog(slog);

                    logger.message = "Customer sync finished in (" + ts.TotalSeconds.ToString("F") + " seconds)";
                    logger.Broadcast();
                });
            
                thread.Start();
                //thread.Join();

                //await Console.Out.WriteLineAsync("Done Customer Sync");
            }catch (ThreadAbortException e)
            {
                DpprException ex = new DpprException
                {
                    file_name = "JobCustomerSync",
                    exception = e.Message,
                    time = DateTime.Now.ToString()
                };
                LocalDB.InsertException(ex);

                Console.WriteLine(Constants.Thread_Exception + e.Message);
            }
        }
    }
}