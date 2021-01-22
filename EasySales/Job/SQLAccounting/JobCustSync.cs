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
    public class JobCustSync : IJob
    {
        public string QuotedStr(string str)
        {
            return str.Replace("'", "''");
        }
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

                    DpprSyncLog slog = new DpprSyncLog();
                    slog.action_identifier = Constants.Action_CustomerSync;
                    slog.action_details = Constants.Tbl_cms_customer + Constants.Is_Starting;
                    slog.action_failure = 0;
                    slog.action_failure_message = string.Empty;
                    slog.action_time = DateTime.Now.ToString();

                    DateTime startTime = DateTime.Now;

                    LocalDB.InsertSyncLog(slog);
                    logger.message = "----------------------------------------------------------------------------";
                    logger.Broadcast();
                    logger.message = "Customer sync is running";
                    logger.Broadcast();
                    logger.message = "----------------------------------------------------------------------------";
                    logger.Broadcast();

                /**
                * Here we will run SQLAccounting Codes
                * */

                CHECKAGAIN:
                    SQLAccApi instance = SQLAccApi.getInstance();

                    dynamic ComServer = instance.GetComServer();

                    if (!instance.RPCAvailable())
                    {
                        goto CHECKAGAIN;
                    }

                    dynamic lRptVar, lMain, lbranch;
                    string query, updateQuery;
                    string Code, Companyname, Creationdate, Area, Currentbalance, Status, Creditlimit, Creditterm, Email, DEmail, Remark, Dfax1, DMobile, Dphone1, Daddress1, Daddress2, Daddress3, Daddress4, Dcontact, Selling_price_type, Branch, Address1, Address2, Address3, Address4, CodeFromBranch, CurrencyCode, ConversionRate;

                    dynamic custRule = new CheckBackendRule()
                                            .CheckTablesExist()
                                            .GetSettingByTableName("cms_customer");
                    ArrayList ruleList = new ArrayList();

                    int suspended_status = 1;
                    int disable_bad_debt_cust = 0;
                    int control_type = 0;

                    if (custRule.Count > 0)
                    {
                        foreach (var include in custRule)
                        {
                            dynamic _include = include.include;

                            dynamic _suspended_status = _include.suspended_status;
                            if (_suspended_status != null)
                            {
                                if (_suspended_status != 1)
                                {
                                    suspended_status = _suspended_status;
                                }
                            }

                            dynamic _disable_bad_debt_cust = _include.disable_bad_debt_cust;
                            if (_disable_bad_debt_cust != null)
                            {
                                if (_disable_bad_debt_cust != 0)
                                {
                                    disable_bad_debt_cust = _disable_bad_debt_cust;
                                }
                            }

                            dynamic _control_type = _include.control_type;
                            if(_control_type != null)
                            {
                                if (_control_type != 0)
                                {
                                    control_type = _control_type;
                                }
                            }
                        }
                    }
                    else
                    {
                        goto ENDJOB;
                    }

                    HashSet<string> queryList = new HashSet<string>();

                    query = "INSERT INTO cms_customer(cust_code, cust_company_name, cust_incharge_person, cust_reference, cust_email, cust_tel, cust_fax, billing_address1, billing_address2, billing_address3, billing_address4, billing_state, shipping_address1, shipping_address2, shipping_address3, shipping_address4, current_balance, termcode, customer_status, created_date, selling_price_type, cust_remark, credit_limit, currency, currency_rate) VALUES ";

                    updateQuery = " ON DUPLICATE KEY UPDATE cust_company_name = VALUES(cust_company_name), cust_incharge_person = VALUES(cust_incharge_person), cust_reference = VALUES(cust_reference), cust_email = VALUES(cust_email), cust_tel = VALUES(cust_tel), cust_fax = VALUES(cust_fax), billing_address1 = VALUES(billing_address1), billing_address2 = VALUES(billing_address2), billing_address3 = VALUES(billing_address3), billing_address4 = VALUES(billing_address4), billing_state = VALUES(billing_state), shipping_address1 = VALUES(shipping_address1), shipping_address2 = VALUES(shipping_address2), shipping_address3 = VALUES(shipping_address3), shipping_address4 = VALUES(shipping_address4),current_balance = VALUES(current_balance), termcode = VALUES(termcode), customer_status = VALUES(customer_status), created_date = VALUES(created_date), selling_price_type = VALUES(selling_price_type), cust_remark = VALUES(cust_remark), credit_limit = VALUES(credit_limit), currency = VALUES(currency), currency_rate = VALUES(currency_rate);";

                    //lMain = ComServer.DBManager.NewDataSet("SELECT * FROM AR_CUSTOMER ORDER BY CODE ASC");

                    try
                    {
                        lMain = ComServer.DBManager.NewDataSet("SELECT C.*, CURR.CODE, CURR.SELLINGRATE FROM AR_CUSTOMER AS C LEFT JOIN CURRENCY AS CURR ON CURR.CODE = C.CURRENCYCODE ORDER BY C.CODE ASC");
                        lbranch = ComServer.DBManager.NewDataSet("SELECT * FROM AR_CUSTOMERBRANCH WHERE CODE IN(SELECT CODE FROM AR_CUSTOMER) ORDER BY CODE ASC");
                        lMain.First();
                        lbranch.First();

                        Database _mysql = new Database();
                        ArrayList branchList = new ArrayList();
                        ArrayList custList = new ArrayList();

                        ArrayList custListToDeactivate = new ArrayList();
                        ArrayList custListFromDb = _mysql.Select("SELECT cust_code, customer_status FROM cms_customer WHERE customer_status = 1");

                        for (int i = 0; i < custListFromDb.Count; i++)
                        {
                            Dictionary<string, string> each = (Dictionary<string, string>)custListFromDb[i];
                            custListToDeactivate.Add(each["cust_code"]);
                        }
                        custListFromDb.Clear();

                        string branchQuery = string.Empty;

                        while (!lMain.eof)
                        {
                            RecordCount++;

                            Code = lMain.FindField("CODE").AsString;
                            custList.Add(Code);

                            if (custListToDeactivate.Contains(Code))
                            {
                                int index = custListToDeactivate.IndexOf(Code);
                                if (index != -1)
                                {
                                    custListToDeactivate.RemoveAt(index);
                                }
                            }

                            //Dfax1 = ""; //changed to DMobile bcs nobody use fax
                            DMobile = ""; //changed to DMobile bcs nobody use fax
                            Dphone1 = "";
                            Daddress1 = "";
                            Daddress2 = "";
                            Daddress3 = "";
                            Daddress4 = "";
                            Dcontact = "";
                            DEmail = "";

                            while (!lbranch.eof)
                            {
                                CodeFromBranch = lbranch.FindField("CODE").AsString;
                                Branch = lbranch.FindField("BRANCHNAME").AsString;

                                if (Branch != string.Empty && Code == CodeFromBranch)
                                {
                                    //Dfax1 = lbranch.FindField("FAX1").AsString;
                                    DMobile = lbranch.FindField("MOBILE").AsString;
                                    Dphone1 = lbranch.FindField("PHONE1").AsString;

                                    Daddress1 = lbranch.FindField("ADDRESS1").AsString;
                                    Daddress2 = lbranch.FindField("ADDRESS2").AsString;
                                    Daddress3 = lbranch.FindField("ADDRESS3").AsString;
                                    Daddress4 = lbranch.FindField("ADDRESS4").AsString;
                                    Dcontact = lbranch.FindField("ATTENTION").AsString;
                                    DEmail = lbranch.FindField("EMAIL").AsString;
                                    break;
                                }

                                lbranch.Next();

                                if (lbranch.eof)
                                {
                                    lbranch.First();
                                }
                            }

                            Companyname = lMain.FindField("COMPANYNAME").AsString;
                            Creationdate = lMain.FindField("CREATIONDATE").AsString;
                            Creationdate = Convert.ToDateTime(Creationdate).ToString("yyyy-MM-dd");
                            Currentbalance = lMain.FindField("OUTSTANDING").AsString;
                            Status = lMain.FindField("STATUS").AsString;
                            Creditlimit = lMain.FindField("CREDITLIMIT").AsString;
                            Creditterm = lMain.FindField("CREDITTERM").AsString;
                            CurrencyCode = lMain.FindField("CURRENCYCODE").AsString;
                            if (CurrencyCode == "----")
                            {
                                CurrencyCode = "RM";
                            }
                            ConversionRate = lMain.FindField("SELLINGRATE").AsString;

                            Area = lMain.FindField("AREA").AsString;

                            Remark = lMain.FindField("REMARK").AsString;

                            Selling_price_type = lMain.FindField("PRICETAG").AsString;

                            int activeValue = 1;

                            /* "S" - SUSPENDED 
                             * "I" - INACTIVE 
                             * "A" - ACTIVE 
                             * "P" - PROSPECT
                             * "N" - PENDING */

                            if (Status == "I")
                            {
                                activeValue = 0;
                            }
                            else if (Status == "S")
                            {
                                activeValue = suspended_status;
                            }
                            else
                            {
                                activeValue = 1;
                            }

                            if (Currentbalance == null)
                            {
                                Currentbalance = "0";
                            }

                            Database.Sanitize(ref Code);
                            Database.Sanitize(ref Companyname);
                            Database.Sanitize(ref Creationdate);
                            Database.Sanitize(ref Area);
                            Database.Sanitize(ref Currentbalance);
                            Database.Sanitize(ref Status);
                            Database.Sanitize(ref Creditlimit);
                            Database.Sanitize(ref Creditterm);
                            Database.Sanitize(ref DEmail);
                            Database.Sanitize(ref Remark);
                            Database.Sanitize(ref DMobile); //insert into cust_fax column
                            Database.Sanitize(ref Dphone1);
                            Database.Sanitize(ref Daddress1);
                            Database.Sanitize(ref Daddress2);
                            Database.Sanitize(ref Daddress3);
                            Database.Sanitize(ref Daddress4);
                            Database.Sanitize(ref Dcontact);
                            Database.Sanitize(ref Selling_price_type);
                            Database.Sanitize(ref CurrencyCode);
                            Database.Sanitize(ref ConversionRate);

                            double.TryParse(Currentbalance, out double _currentBalance);

                            string Values = string.Format("('{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}','{8}', '{9}','{10}','{11}','{12}','{13}','{14}','{15}','{16}','{17}','{18}','{19}','{20}','{21}','{22}','{23}','{24}')", Code, Companyname, Dcontact, Remark, DEmail, Dphone1, DMobile, Daddress1, Daddress2, Daddress3, Daddress4, Area, Daddress1, Daddress2, Daddress3, Daddress4, _currentBalance, Creditterm, activeValue, Creationdate, Selling_price_type, Remark, Creditlimit, CurrencyCode, ConversionRate);

                            queryList.Add(Values);

                            if (queryList.Count % 500 == 0)
                            {
                                string tmp_query = query;
                                tmp_query += string.Join(", ", queryList);
                                tmp_query += updateQuery;

                                Database mysql = new Database();
                                int failCounter = 0;
                            INSERTAGAIN:
                                bool inserted = mysql.Insert(tmp_query);
                                if (!inserted)
                                {
                                    Task.Delay(2000);
                                    failCounter++;
                                    if (failCounter < 4)
                                    {
                                        goto INSERTAGAIN;
                                    }
                                    else
                                    {
                                        mysql.Message("Failed to insert this query: ---> " + tmp_query);
                                    }
                                }
                                else
                                {
                                    logger.message = string.Format("{0} customer records is inserted", RecordCount);
                                    logger.Broadcast();
                                }

                                tmp_query = string.Empty;
                                queryList.Clear();
                            }

                            lMain.Next();
                        }

                        if (queryList.Count > 0)
                        {
                            query = query + string.Join(", ", queryList) + updateQuery;

                            Database mysql = new Database();

                            //mysql.Insert("INSERT INTO cms_customer (cust_code,customer_zone) SELECT c.cust_code, cz.zone_id FROM cms_customer c INNER JOIN cms_customer_salesperson cs ON c.cust_id = cs.customer_id LEFT JOIN cms_login l ON l.login_id = cs.salesperson_id LEFT JOIN cms_customer_zone cz ON l.staff_code = cz.zone_name ON DUPLICATE KEY UPDATE cust_code = VALUES(cust_code), customer_zone = VALUES(customer_zone);");
                            int failCounter = 0;
                        INSERTAGAIN:
                            bool inserted = mysql.Insert(query);
                            if (!inserted)
                            {
                                Task.Delay(2000);
                                failCounter++;
                                if (failCounter < 4)
                                {
                                    goto INSERTAGAIN;
                                }
                                else
                                {
                                    mysql.Message("Failed to insert this query: ---> " + query);
                                }
                            }
                            else
                            {
                                logger.message = string.Format("{0} customer records is inserted", RecordCount);
                                logger.Broadcast();
                            }
                        };

                        //change customer_status = 0 if the customer no longer exists in SQLACC
                        if (custListToDeactivate.Count > 0)
                        {
                            logger.Broadcast("Total customers to be deactivated: " + custListToDeactivate.Count);

                            string inactive = "UPDATE cms_customer SET customer_status = '{0}' WHERE cust_code = '{1}'";

                            Database mysql = new Database();
                            int deactivateCount = 0;
                            for (int i = 0; i < custListToDeactivate.Count; i++)
                            {
                                string custCode = custListToDeactivate[i].ToString();

                                Database.Sanitize(ref custCode);
                                string _query = string.Format(inactive, '0', custCode);

                                int failCounter = 0;
                                INSERTAGAIN:
                                bool inserted = mysql.Insert(_query);
                                if (!inserted)
                                {
                                    Task.Delay(2000);
                                    failCounter++;
                                    if (failCounter < 4)
                                    {
                                        goto INSERTAGAIN;
                                    }
                                    else
                                    {
                                        mysql.Message("Failed to insert this query: ---> " + _query);
                                    }
                                }
                                else
                                {
                                    deactivateCount++;
                                }
                            }

                            logger.Broadcast(deactivateCount + " customers deactivated");

                            custListToDeactivate.Clear();
                        }

                        if (disable_bad_debt_cust == 1)
                        {
                            RecordCount = 0;
                            Database mysql = new Database();
                            ArrayList checkCust = mysql.Select("SELECT IF(current_balance > credit_limit, 0 , 1) AS `status`, cust_code FROM cms_customer;");

                            Dictionary<string, string> custStatusList = new Dictionary<string, string>();

                            for (int i = 1; i < checkCust.Count; i++)
                            {
                                Dictionary<string, string> each = (Dictionary<string, string>)checkCust[i];
                                custStatusList.Add(each["cust_code"], each["status"]);
                            }

                            dynamic lCust;
                            string insert, update;
                            string code, status, controltype;
                            int int_status, int_ctrl_type;
                            HashSet<string> custStatusQueryList = new HashSet<string>();

                            insert = "INSERT INTO cms_customer (cust_code, customer_status) VALUES ";
                            update = " ON DUPLICATE KEY UPDATE cust_code = VALUES(cust_code), customer_status = VALUES(customer_status)";

                            lCust = ComServer.DBManager.NewDataSet("SELECT controltype,iif(controltype=4,0,1) as status, code FROM AR_CUSTOMERCRCTRL");

                            lCust.First();

                            while (!lCust.eof)
                            {
                                code = lCust.FindField("CODE").AsString;
                                int_status = lCust.FindField("STATUS").value;
                                int_ctrl_type = lCust.FindField("CONTROLTYPE").value;

                                status = int_status.ToString();
                                controltype = int_ctrl_type.ToString();

                                if (control_type == 1)
                                {
                                    if (controltype == "8")
                                    {
                                        for (int i = 1; i < custStatusList.Count; i++)
                                        {
                                            string _code = custStatusList.ElementAt(i).Key;
                                            string _status = custStatusList.ElementAt(i).Value;

                                            if (code == _code)
                                            {
                                                status = _status;
                                                break;
                                            }
                                        }
                                    }
                                }

                                Database.Sanitize(ref code);
                                Database.Sanitize(ref status);

                                RecordCount++;
                                string Values = string.Format("('{0}','{1}')", code, status);

                                custStatusQueryList.Add(Values);

                                if (custStatusQueryList.Count % 500 == 0)
                                {
                                    string tmp_query = insert;
                                    tmp_query += string.Join(", ", custStatusQueryList);
                                    tmp_query += update;

                                    mysql.Insert(tmp_query);

                                    custStatusQueryList.Clear();
                                    tmp_query = string.Empty;

                                    logger.message = string.Format("{0} bad debt customer records is updated", RecordCount);
                                    logger.Broadcast();
                                }

                                lCust.Next();
                            }

                            if (custStatusQueryList.Count > 0)
                            {
                                insert = insert + string.Join(", ", custStatusQueryList) + update;
                                mysql.Insert(insert);

                                logger.message = string.Format("{0} bad debt customer records is updated", RecordCount);
                                logger.Broadcast();
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
                                file_name = "SQLAccounting + JobCustomerSync",
                                exception = exc.Message,
                                time = DateTime.Now.ToString()
                            };
                            LocalDB.InsertException(exception);
                        }
                    }

                    ENDJOB:

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

            }
            catch (ThreadAbortException e)
            {
                DpprException ex = new DpprException
                {
                    file_name = "JobCustSync",
                    exception = e.Message,
                    time = DateTime.Now.ToString()
                };
                LocalDB.InsertException(ex);

                Console.WriteLine(Constants.Thread_Exception + e.Message);
            }
        }
    }
}