using EasySales.Model;
using EasySales.Object;
using Quartz;
using Quartz.Impl.Matchers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Microsoft.EntityFrameworkCore.Internal;
using System.Net.Sockets;

namespace EasySales.Job
{
    [DisallowConcurrentExecution]
    public class JobSoTransfer : IJob
    {
        private string socket_OrderId = string.Empty; //from socket transfer
        private static ArrayList SplitByTag(string remark, JArray tagArray)
        {
            ArrayList final = new ArrayList();
            if (String.IsNullOrEmpty(remark))
            {
                Dictionary<string, string> rObj = new Dictionary<string, string>();
                rObj.Add("remarks", remark);
                final.Add(rObj);
                return final;
            }

            var positions = new List<dynamic>();
            ArrayList replacements = new ArrayList();

            string ref_remark = remark.ToLower().Trim();

            foreach (JObject item in tagArray.Children())
            {
                string moduleName = item.GetValue("name").ToString();
                moduleName = "@" + moduleName;
                moduleName = moduleName.ToLower().Trim();

                int lastPos = 0;

                while ((lastPos = ref_remark.IndexOf(moduleName)) != -1)
                {
                    var builder = new StringBuilder(ref_remark);
                    builder.Remove(lastPos, moduleName.Length);
                    for (int j = 0; j < moduleName.Length; j++)
                    {
                        builder.Insert(lastPos + j, "^");
                    }
                    ref_remark = builder.ToString(); //"@desc fabric grey @po gaa18539-ida" //{^^^^^ fabric grey ^^^ gaa18539-ida}

                    dynamic pieces = new ExpandoObject();
                    pieces.Key = moduleName;
                    pieces.Position = lastPos;
                    pieces.Field = item.GetValue("field").ToString();
                    positions.Add(pieces);
                }
                replacements.Add(moduleName);
            }

            positions.Sort((a, b) => a.Position.CompareTo(b.Position));

            remark = remark.ToLower().Trim();
            for (int i = 0, size = positions.Count; i < size; i++)
            {
                dynamic obj = positions[i];
                int startPos = obj.Position;
                int endPos = remark.Length;
                if ((i + 1) != size)
                {
                    dynamic next = positions[i + 1];
                    endPos = next.Position;
                }
                string content = remark.Substring(startPos, (endPos - startPos));
                if (content != string.Empty)
                {
                    for (int j = 0; j < replacements.Count; j++)
                    {
                        string key = replacements[j].ToString();    //"@desc"       //"@po"
                        content = content.Replace(key, "");         //fabric grey   //" gaa18539-ida"
                    }
                    if (content != string.Empty)
                    {
                        Dictionary<string, string> map = new Dictionary<string, string>();
                        map.Add(obj.Field, content);
                        final.Add(map);
                    }
                }
            }
            return final;
        }

        private string getSST()
        {
            Database _mysql = new Database();
            ArrayList moduleStatus = _mysql.Select("SELECT CAST(status AS CHAR(10000) CHARACTER SET utf8) as status FROM cms_mobile_module WHERE module = 'app_sst_percent'");

            if (moduleStatus.Count > 0)
            {
                Dictionary<string, string> statusList = (Dictionary<string, string>)moduleStatus[0];
                string status = statusList["status"].ToString();
                return status;
            }
            return string.Empty;
        }
        public void ExecuteSocket(string socket_OrderId)
        {
            this.socket_OrderId = socket_OrderId;
            Execute();
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
                //await context.Scheduler.PauseJobs(GroupMatcher<JobKey>.GroupContains(Constants.Job_Group_Sync)); /*ask julfi*/
                GlobalLogger logger = new GlobalLogger();
                Thread thread = new Thread(p =>
                {
                    Thread.CurrentThread.IsBackground = true;
                    //GlobalLogger logger = new GlobalLogger();

                    /**
                     * Here we will run SQLAccounting Codes
                     * */
                    DpprSyncLog slog = new DpprSyncLog();
                    slog.action_identifier = Constants.Action_Transfer_SO;
                    slog.action_details = "Starting";
                    slog.action_failure = 0;
                    slog.action_failure_message = string.Empty;
                    slog.action_time = DateTime.Now.ToString();

                    DateTime startTime = DateTime.Now;
                    List<DpprSyncLog> list = LocalDB.checkJobRunning();
                    //if (list.Count > 0)
                    //{
                    //    DpprSyncLog value = list[0];
                    //    if (value.action_details == "Starting")
                    //    {
                    //        logger.message = "SQLACC Transfer Sales Order is already running";
                    //        logger.Broadcast();
                    //        goto ENDJOB;
                    //    }
                    //}

                    LocalDB.InsertSyncLog(slog);
                    logger.message = "----------------------------------------------------------------------------";
                    logger.Broadcast();
                    logger.message = "Transfer Sales Orders is running";
                    logger.Broadcast();
                    logger.message = "----------------------------------------------------------------------------";
                    logger.Broadcast();

                    dynamic hasUdf = new CheckBackendRule().CheckTablesExist().GetSettingByTableName("cms_order");
                    ArrayList functionList = new ArrayList();

                    dynamic appRemark = new CheckBackendRule().getAppRemark();

                CHECKAGAIN:

                    SQLAccApi instance = SQLAccApi.getInstance();

                    dynamic ComServer = instance.GetComServer();

                    if (!instance.RPCAvailable())
                    {
                        goto CHECKAGAIN;
                    }

                    Database mysql = new Database();

                    dynamic BizObject, lMainDataSet, lDetailDataSet, lRptVar;
                    string order_validity_query, order_status_query, disc_query, udf_query, item_validity_query, pickpack_query, check_bal_query, currency_query;

                    int sql_multi_discount = 0;
                    int sql_template_package = 0;
                    int check_min_price = 0;
                    int order_status = 1;
                    int pickpack_link = 0;
                    int check_item_bal = 0;
                    int order_approved = 0;
                    int include_no_stock_so = 0;
                    int include_ext_no = 0;
                    int include_currency = 0;
                    int include_do = 0;
                    int check_balqty_do = 0;
                    int include_invoice = 0;
                    int checking_package = 0;
                    int find_cust_agent = 1;

                    string location = string.Empty;
                    string transferable = "T";
                    string tax_type = string.Empty;
                    string tax_rate = string.Empty;
                    string mysql_field = string.Empty;
                    string sqlacc_field = string.Empty;
                    string fieldname = string.Empty;
                    string separator = string.Empty;
                    
                    string refUdf = "empty";
                    Dictionary<string, string> ItemTax = new Dictionary<string, string>();
                    Dictionary<string, string> FieldNameList = new Dictionary<string, string>();
                    Dictionary<string, string> DtlFieldNameList = new Dictionary<string, string>();
                    Dictionary<string, string> FieldSeparatorList = new Dictionary<string, string>();
                    Dictionary<string, string> DtlFieldSeparatorList = new Dictionary<string, string>();
                    Dictionary<string, string> projectList = new Dictionary<string, string>();
                    Dictionary<string, string> finalProjectList = new Dictionary<string, string>();
                    string SST = getSST();

                    if (hasUdf.Count > 0)
                    {
                        foreach (var condition in hasUdf)
                        {
                            dynamic _condition = condition.condition;

                            dynamic _check_min_price = _condition.check_min_price;
                            if (_check_min_price != null)
                            {
                                if (_check_min_price != 0)
                                {
                                    check_min_price = _check_min_price;
                                }
                            }

                            dynamic _sql_multi_discount = _condition.sql_multi_discount;
                            if (_sql_multi_discount != null)
                            {
                                if (_sql_multi_discount != 0)
                                {
                                    sql_multi_discount = _sql_multi_discount;
                                }
                            }
                            
                            dynamic _sql_template_package = _condition.sql_template_package;
                            if (_sql_template_package != null)
                            {
                                if (_sql_template_package != 0)
                                {
                                    sql_template_package = _sql_template_package;
                                }
                            }
                            
                            dynamic _order_status = _condition.order_status;
                            if (_order_status != null)
                            {
                                if (_order_status != 1)
                                {
                                    order_status = _order_status;
                                }
                            }
                            

                            dynamic _check_item_bal = _condition.check_item_bal;
                            if (_check_item_bal != null)
                            {
                                if (_check_item_bal != 0)
                                {
                                    check_item_bal = _check_item_bal;
                                }
                            }
                            
                            dynamic _order_approved = _condition.order_approved;
                            if (_order_approved != null)
                            {
                                if (_order_approved != 0)
                                {
                                    order_approved = _order_approved;
                                }
                            }

                            dynamic _pickpack_link = _condition.pickpack_link;
                            if (_pickpack_link != null)
                            {
                                if (_pickpack_link != 0)
                                {
                                    pickpack_link = _pickpack_link;
                                }
                            }
                            

                            dynamic _include_no_stock_so = _condition.include_no_stock_so;
                            if (_include_no_stock_so != null)
                            {
                                if (_include_no_stock_so != 0)
                                {
                                    include_no_stock_so = _include_no_stock_so;
                                }
                            }
                            

                            dynamic _location = _condition.location;
                            if (_location != null)
                            {
                                if (_location != string.Empty)
                                {
                                    location = _location;
                                }
                            }
                            

                            dynamic _transferable = _condition.transferable;
                            if (_transferable != null)
                            {
                                if (_transferable != string.Empty)
                                {
                                    transferable = _transferable;
                                }
                            }
                            

                            dynamic _tax = _condition.tax;
                            if (_tax != null)
                            {
                                foreach (var taxdtl in _tax)
                                {
                                    dynamic _taxtype = taxdtl.name;
                                    if (_taxtype != string.Empty)
                                    {
                                        tax_type = _taxtype;
                                    }

                                    dynamic _taxrate = taxdtl.rate;
                                    if (_taxrate != string.Empty)
                                    {
                                        tax_rate = _taxrate;
                                    }

                                    ItemTax.Add(tax_type, tax_rate);
                                }
                            }

                            dynamic _include_currency = _condition.include_currency;
                            if (_include_currency != null)
                            {
                                if (_include_currency != string.Empty)
                                {
                                    include_currency = _include_currency;
                                }
                            }

                            dynamic _include_ext_no = _condition.include_ext_no;
                            if (_include_ext_no != null)
                            {
                                if (_include_ext_no != string.Empty)
                                {
                                    include_ext_no = _include_ext_no;
                                }
                            }
                            
                            dynamic _include_do = _condition.include_do;
                            if (_include_do != null)
                            {
                                if (_include_do != string.Empty)
                                {
                                    include_do = _include_do;
                                }
                            }
                            
                            dynamic _check_balqty_do = _condition.check_balqty_do; //oasisqi no need, aquatic need
                            if (_check_balqty_do != null)
                            {
                                if (_check_balqty_do != string.Empty)
                                {
                                    check_balqty_do = _check_balqty_do;
                                }
                            }
                            
                            dynamic _include_invoice = _condition.include_invoice;
                            if (_include_invoice != null)
                            {
                                if (_include_invoice != string.Empty)
                                {
                                    include_invoice = _include_invoice;
                                }
                            }
                            
                            dynamic _checking_package = _condition.checking_package;
                            if (_checking_package != null)
                            {
                                if (_checking_package != string.Empty)
                                {
                                    checking_package = _checking_package;
                                }
                            }
                            
                            dynamic _find_cust_agent = _condition.find_cust_agent;
                            if (_find_cust_agent != null)
                            {
                                if (_find_cust_agent != string.Empty)
                                {
                                    find_cust_agent = _find_cust_agent;
                                }
                            }

                            dynamic _field = _condition.order_field;
                            if (_field != null)
                            {
                                foreach (var field in _field)
                                {
                                    dynamic _mysql = field.mysql;
                                    if (_mysql != string.Empty)
                                    {
                                        mysql_field = _mysql;
                                    }

                                    dynamic _sqlacc = field.sqlacc;
                                    if (_sqlacc != string.Empty)
                                    {
                                        sqlacc_field = _sqlacc;
                                    }

                                    FieldNameList.Add(mysql_field, sqlacc_field);
                                }
                            }

                            dynamic _field_separator = _condition.order_field_separator;
                            if (_field_separator != null)
                            {
                                foreach (var field in _field_separator)
                                {
                                    dynamic _fieldname = field.fieldname;
                                    if (_fieldname != string.Empty)
                                    {
                                        fieldname = _fieldname;
                                    }

                                    dynamic _separator = field.separator;
                                    separator = _separator;

                                    FieldSeparatorList.Add(fieldname, separator);
                                }
                            }

                            dynamic _dtl_field = _condition.order_dtl_field;
                            if (_dtl_field != null)
                            {
                                foreach (var field in _dtl_field)
                                {
                                    dynamic _mysql = field.mysql;
                                    if (_mysql != string.Empty)
                                    {
                                        mysql_field = _mysql;
                                    }

                                    dynamic _sqlacc = field.sqlacc;
                                    if (_sqlacc != string.Empty)
                                    {
                                        sqlacc_field = _sqlacc;
                                    }

                                    DtlFieldNameList.Add(mysql_field, sqlacc_field);
                                }
                            }

                            dynamic _dtl_field_separator = _condition.order_dtl_field_separator;
                            if (_dtl_field_separator != null)
                            {
                                foreach (var field in _dtl_field_separator)
                                {
                                    dynamic _fieldname = field.fieldname;
                                    if (_fieldname != string.Empty)
                                    {
                                        fieldname = _fieldname;
                                    }

                                    dynamic _separator = field.separator;
                                    separator = _separator;

                                    DtlFieldSeparatorList.Add(fieldname, separator);
                                }
                            }

                            dynamic _project = _condition.project;
                            if (_project != null)
                            {
                                if (_project.Count > 0)
                                {
                                    foreach (var item in _project)
                                    {
                                        projectList = item.ToObject<Dictionary<string, string>>();
                                        finalProjectList = finalProjectList.Union(projectList).ToDictionary(k => k.Key, v => v.Value);
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        goto ENDJOB;
                    }

                    order_validity_query = "";
                    if (check_min_price != 0)
                    {
                        order_validity_query = " AND o.order_validity = 2 ";
                    }

                    order_status_query = " order_status = 1 ";
                    if (order_status != 1)
                    {
                        order_status_query = " order_status = " + order_status + "";
                    }

                    check_bal_query = "";
                    if (check_item_bal != 0)
                    {
                        check_bal_query = " AND o.order_approved = " + check_item_bal + ""; //weiwo - if item qty of order is more than balqty in sqlacc, reject the order
                    }

                    if(order_approved != 0)
                    {
                        check_bal_query = " AND o.order_approved = " + order_approved + ""; // 1 - approved, 2 - rejected
                    }

                    pickpack_query = "";
                    if (pickpack_link != 0)
                    {
                        pickpack_query = " AND packing_status = 1 "; // AND pack_confirmed = 1
                    }
                    
                    currency_query = "";
                    if (include_currency != 0)
                    {
                        currency_query = "  c.currency, c.currency_rate, ";
                    }

                    string orderQuery = "SELECT CAST(o.order_udf AS CHAR(10000) CHARACTER SET utf8) AS orderUdfJson, l.staff_code,o.salesperson_id, o.cust_id, o.order_id, o.cust_code, " + currency_query + " o.order_date, o.cust_company_name, o.warehouse_code, o.cust_incharge_person, o.cust_tel, o.cust_fax, o.billing_address1, o.billing_address2, o.billing_address3, o.billing_address4, o.termcode, b.shipping_address1, b.shipping_address2, b.shipping_address3, b.shipping_address4, o.billing_state, o.delivery_date, o.grand_total, o.gst_tax_amount, o.gst_amount, o.order_delivery_note, (SELECT GROUP_CONCAT(upload_image SEPARATOR ',') FROM cms_salesperson_uploads WHERE upload_bind_id = o.order_id GROUP BY upload_bind_id) AS image FROM cms_order AS o LEFT JOIN cms_customer AS c ON o.cust_id = c.cust_id LEFT JOIN cms_login AS l ON o.salesperson_id = l.login_id LEFT JOIN cms_customer_branch AS b ON b.branch_code = o.branch_code WHERE " + order_status_query + " AND cancel_status = 0 " + order_validity_query + "AND doc_type = 'sales' AND order_fault = 0 " + check_bal_query + " " + pickpack_query ;
                    socket_OrderId = socket_OrderId.Replace("\"", "\'");
                    string socketQuery = "AND order_id IN (" + socket_OrderId + ")";

                    orderQuery = orderQuery + (socket_OrderId != string.Empty ? socketQuery : "");

                    //string orderQuery = "SELECT CAST(o.order_udf AS CHAR(10000) CHARACTER SET utf8) AS orderUdfJson, l.staff_code,o.salesperson_id, o.cust_id, o.order_id, o.cust_code, " + currency_query  + " o.order_date, o.cust_company_name, o.warehouse_code, o.cust_incharge_person, o.cust_tel, o.cust_fax, o.billing_address1, o.billing_address2, o.billing_address3, o.billing_address4, o.termcode, b.shipping_address1, b.shipping_address2, b.shipping_address3, b.shipping_address4, o.billing_state, o.delivery_date, o.grand_total, o.gst_tax_amount, o.gst_amount, o.order_delivery_note, (SELECT GROUP_CONCAT(upload_image SEPARATOR ',') FROM cms_salesperson_uploads WHERE upload_bind_id = o.order_id GROUP BY upload_bind_id) AS image FROM cms_order AS o LEFT JOIN cms_customer AS c ON o.cust_id = c.cust_id LEFT JOIN cms_login AS l ON o.salesperson_id = l.login_id LEFT JOIN cms_customer_branch AS b ON b.branch_code = o.branch_code WHERE " + order_status_query + " AND cancel_status = 0 " + order_validity_query + "AND doc_type = 'sales' AND order_fault = 0 " + check_bal_query + " " + pickpack_query;

                    ArrayList orders = mysql.Select(orderQuery);
                    mysql.Message(orderQuery);

                    logger.Broadcast(orderQuery);
                    logger.Broadcast("Order(s) to insert: " + orders.Count);

                    if (orders.Count == 0)
                    {
                        logger.message = "No orders to insert";
                        logger.Broadcast();
                    }
                    else
                    {
                        int postCount = 0;
                        string orderId = string.Empty;
                        try
                        {
                            BizObject = ComServer.BizObjects.Find("SL_SO");

                            lMainDataSet = BizObject.DataSets.Find("MainDataSet");
                            lDetailDataSet = BizObject.DataSets.Find("cdsDocDetail");

                            for (int i = 0; i < orders.Count; i++)
                            {
                                BizObject.New();

                                string post_date, branch_name, Total;
                                double total;

                                Dictionary<string, string> orderObj = (Dictionary<string, string>)orders[i];

                                string custCode = orderObj["cust_code"];
                                string agentCode = orderObj["staff_code"];

                                if(find_cust_agent == 1)
                                {
                                    ArrayList findCustomerSalesperson = mysql.Select("SELECT cms_login.login_id, cms_login.staff_code, cms_customer.cust_code,cms_customer.cust_company_name FROM cms_customer_salesperson LEFT JOIN cms_login ON cms_login.login_id = cms_customer_salesperson.salesperson_id LEFT JOIN cms_customer ON cms_customer_salesperson.customer_id = cms_customer.cust_id WHERE cust_code = '" + custCode + "' AND cms_customer_salesperson.active_status = 1");
                                    if (findCustomerSalesperson.Count > 0)
                                    {
                                        Dictionary<string, string> custSalesperson = (Dictionary<string, string>)findCustomerSalesperson[0];
                                        agentCode = custSalesperson["staff_code"];
                                    }
                                }

                                Total = orderObj["grand_total"];
                                double.TryParse(Total, out double _total);
                                total = _total * 1.00;

                                string _branch_name;

                                ArrayList branchDb = mysql.Select("SELECT branch_name FROM cms_customer_branch");

                                for (int iX = 0; iX < branchDb.Count; iX++)
                                {
                                    Dictionary<string, string> branchObj = (Dictionary<string, string>)branchDb[iX];
                                    _branch_name = branchObj["branch_name"];
                                }

                                _branch_name = "";

                                if (_branch_name == "")
                                {
                                    branch_name = "";
                                }
                                else
                                {
                                    branch_name = _branch_name;
                                }

                                ArrayList result = mysql.Select("SHOW COLUMNS FROM `cms_order` LIKE 'order_fault'");

                                ArrayList orderFault = mysql.Select("SELECT order_fault FROM cms_order WHERE order_id = '" + orderObj["order_id"] + "'");
                                Dictionary<string, string> getOrderFault = (Dictionary<string, string>)orderFault[0];

                                string _fault = getOrderFault["order_fault"];
                                int.TryParse(_fault, out int fault);

                                post_date = Convert.ToDateTime(orderObj["order_date"]).ToString("yyyy-MM-dd");

                                string clientSOID = LogicParser.client_ID();
                                orderId = orderObj["order_id"];
                                string orderRunningId = string.Empty;

                                if(clientSOID.Contains("agentId"))
                                {
                                    clientSOID = clientSOID.Replace("agentId", agentCode); //for aquatic SO-agentId-orderRunningId
                                    Console.WriteLine("clientSOID:" + clientSOID);
                                }
                                if(clientSOID.Contains("orderRunningId"))
                                {
                                    int index = orderId.LastIndexOf("-");
                                    if (index > 0)
                                        orderRunningId = orderId.Substring(index + 1);
                                    Console.WriteLine(orderRunningId);

                                    clientSOID = clientSOID.Replace("orderRunningId", orderRunningId);
                                    Console.WriteLine("clientSOID:" + clientSOID);
                                }

                                lMainDataSet.FindField("DocKey").value = -1;

                                if (include_ext_no == 1)
                                {
                                    if (clientSOID != string.Empty)
                                    {
                                        lMainDataSet.FindField("DocNo").value = clientSOID;
                                    }
                                    else
                                    {
                                        lMainDataSet.FindField("DocNo").value = "<<New>>";
                                    }

                                    lMainDataSet.FindField("DocNoEx").value = orderObj["order_id"];
                                }
                                else
                                {
                                    lMainDataSet.FindField("DocNo").value = orderObj["order_id"];
                                }

                                lMainDataSet.FindField("DocDate").value = post_date;
                                lMainDataSet.FindField("PostDate").value = post_date;
                                lMainDataSet.FindField("TaxDate").value = post_date;
                                lMainDataSet.FindField("Code").value = orderObj["cust_code"];
                                lMainDataSet.FindField("CompanyName").value = orderObj["cust_company_name"];
                                lMainDataSet.FindField("Address1").value = orderObj["billing_address1"];
                                lMainDataSet.FindField("Address2").value = orderObj["billing_address2"];
                                lMainDataSet.FindField("Address3").value = orderObj["billing_address3"];
                                lMainDataSet.FindField("Address4").value = orderObj["billing_address4"];
                                lMainDataSet.FindField("Phone1").value = orderObj["cust_tel"];
                                lMainDataSet.FindField("Fax1").value = orderObj["cust_fax"];
                                lMainDataSet.FindField("Attention").value = orderObj["cust_incharge_person"];
                                lMainDataSet.FindField("Area").value = orderObj["billing_state"];
                                lMainDataSet.FindField("Agent").value = agentCode;
                                //lMainDataSet.FindField("Project").value = "----";

                                string warehouse_code = orderObj["warehouse_code"];
                                string project = "----";

                                if (finalProjectList.Count > 0)
                                {
                                    if (finalProjectList.ContainsKey(warehouse_code))
                                    {
                                        project = finalProjectList.Where(pair => pair.Key == warehouse_code)
                                                            .Select(pair => pair.Value)
                                                            .FirstOrDefault();
                                        Console.WriteLine(project);
                                        lMainDataSet.FindField("Project").value = project;
                                    }
                                    else
                                    {
                                        lMainDataSet.FindField("Project").value = "----";
                                    }
                                }
                                else
                                {
                                    lMainDataSet.FindField("Project").value = "----";
                                }

                                lMainDataSet.FindField("Terms").value = orderObj["termcode"];

                                if (include_currency == 1)
                                {
                                    string currency = orderObj["currency"];
                                    if (currency != "RM")
                                    {
                                        lMainDataSet.FindField("CurrencyCode").value = currency;
                                        lMainDataSet.FindField("CurrencyRate").value = orderObj["currency_rate"];
                                    }
                                    else
                                    {
                                        lMainDataSet.FindField("CurrencyRate").value = orderObj["currency_rate"];
                                    }

                                }
                                else
                                {
                                    lMainDataSet.FindField("CurrencyCode").value = "----";
                                    lMainDataSet.FindField("CurrencyRate").value = "1";
                                }

                                lMainDataSet.FindField("Shipper").value = "----";
                                lMainDataSet.FindField("Description").value = "Sales Order";
                                lMainDataSet.FindField("Cancelled").value = "F";
                                //lMainDataSet.FindField("DocAmt").value = total;           //commented these 2 lines bcs we are inserting the total after calculated the details subtotal
                                //lMainDataSet.FindField("LocalDocAmt").value = total;
                                lMainDataSet.FindField("D_Amount").value = "0";
                                lMainDataSet.FindField("D_BankCharge").value = "0";
                                lMainDataSet.FindField("BranchName").value = branch_name;

                                lMainDataSet.FindField("DOCREF1").value = branch_name;
                                lMainDataSet.FindField("DOCREF4").value = "APP";            //for srri easwari

                                lMainDataSet.FindField("DAttention").value = "-";
                                lMainDataSet.FindField("DPhone1").value = "-";
                                lMainDataSet.FindField("DFax1").value = "-";
                                lMainDataSet.FindField("Transferable").value = transferable;   //based on backend rules
                                lMainDataSet.FindField("PrintCount").value = "0";
                                lMainDataSet.FindField("CHANGED").AsString = "F";

                                if (FieldNameList.Count > 0)
                                {
                                    string mysql_column = string.Empty;
                                    string sqlacc_column = string.Empty;

                                    for (int idx = 0; idx < FieldNameList.Count; idx++) 
                                    {
                                        mysql_column = FieldNameList.ElementAt(idx).Key;
                                        sqlacc_column = FieldNameList.ElementAt(idx).Value;

                                        if(mysql_column.Contains("orderUdfJson")) 
                                        {
                                            string field_separator = "";
                                            string orderUdf = mysql_column.Replace("orderUdfJson_", "");

                                            string[] words = orderUdf.Split('_');
                                            string orderUdftest = orderObj["orderUdfJson"].ToString();
                                            JArray remarkJArraytest = orderUdftest.IsJArray() ? (JArray)JToken.Parse(orderUdftest) : new JArray();
                                            string udfString = string.Empty;

                                            foreach (var word in words)
                                            {
                                                if (FieldSeparatorList.Count > 0)
                                                {
                                                    if (FieldSeparatorList.ContainsKey(word))
                                                    {
                                                        Dictionary<string, string> value = (Dictionary<string, string>)FieldSeparatorList;
                                                        field_separator = value[word];
                                                    }
                                                }

                                                string refIdtest = LogicParser.filterOrderUDFbyKey(remarkJArraytest, word);

                                                if (refIdtest != string.Empty)
                                                {
                                                    udfString += field_separator + refIdtest;
                                                }
                                            }
                                            
                                            lMainDataSet.FindField(sqlacc_column).AsString = udfString;
                                        }
                                        else if (mysql_column.Contains("+"))
                                        {
                                            string[] words = mysql_column.Split('+');
                                            string udfString = string.Empty;
                                            string field_separator = "";

                                            foreach (var word in words)
                                            {
                                                if (FieldSeparatorList.Count > 0)
                                                {
                                                    if (FieldSeparatorList.ContainsKey(word))
                                                    {
                                                        Dictionary<string, string> value = (Dictionary<string, string>)FieldSeparatorList;
                                                        field_separator = value[word];
                                                    }
                                                }

                                                string getValue = orderObj[word];
                                                if (getValue != string.Empty)
                                                {
                                                    udfString += field_separator + getValue;
                                                }
                                            }

                                            lMainDataSet.FindField(sqlacc_column).AsString = udfString;
                                        }
                                        else if (mysql_column.Contains("createdsalesperson"))
                                        {
                                            string agent = mysql_column.Replace("createdsalesperson.", "");
                                            ArrayList agentName = mysql.Select("SELECT l.staff_code, l.name FROM cms_login AS l LEFT JOIN cms_order AS o ON l.login_id = o.salesperson_id WHERE o.salesperson_id =  " + orderObj["salesperson_id"] + "");
                                            Dictionary<string, string> item = (Dictionary<string, string>)agentName[0];
                                            lMainDataSet.FindField(sqlacc_column).AsString = item[agent];
                                        }
                                        else
                                        {
                                            if(mysql_column.Contains("date"))
                                            {
                                                string date = Convert.ToDateTime(orderObj[mysql_column]).ToString("dd/MM/yyyy"); //samfah delivery date
                                                lMainDataSet.FindField(sqlacc_column).AsString = date;
                                            }
                                            else
                                            {
                                                lMainDataSet.FindField(sqlacc_column).AsString = orderObj[mysql_column];
                                            }
                                            
                                        }
                                    }
                                }

                                string orderUdft = orderObj["orderUdfJson"].ToString();
                                JArray remarkJArrayt = orderUdft.IsJArray() ? (JArray)JToken.Parse(orderUdft) : new JArray();
                                Console.WriteLine(remarkJArrayt);
                                Console.WriteLine(remarkJArrayt.ToString());

                                string shipping_add = orderObj["shipping_address1"];
                                if (shipping_add != string.Empty && remarkJArrayt.ToString() == "[]")
                                {
                                    lMainDataSet.FindField("DAddress1").value = orderObj["shipping_address1"];
                                    lMainDataSet.FindField("DAddress2").value = orderObj["shipping_address2"];
                                    lMainDataSet.FindField("DAddress3").value = orderObj["shipping_address3"];
                                    lMainDataSet.FindField("DAddress4").value = orderObj["shipping_address4"];
                                }

                                //lMainDataSet.Post(); //posting after inserting all item details

                                disc_query = "";
                                if (sql_multi_discount != 0)
                                {
                                    disc_query = "oi.disc_1,oi.disc_2,oi.disc_3,";
                                }
                                else
                                {
                                    //follow aquatic code
                                    disc_query = " oi.discount_method,oi.discount_amount,oi.disc_1,oi.disc_2,oi.disc_3, ";//"oi.discount_method,oi.discount_amount,"; //oi.disc_1 insert into discount column " oi.discount_method, oi.disc_1, ";//
                                }

                                udf_query = "";
                                if (sql_template_package != 0)
                                {
                                    udf_query = ", IF(parent_code IS NULL OR parent_code = '','F','T') AS udf_istemplate";
                                }

                                item_validity_query = "";
                                if (check_min_price != 0)
                                {
                                    item_validity_query = " AND oi.order_item_validity = 2 ";
                                }

                                pickpack_query = "";
                                if (pickpack_link != 0)
                                {
                                    pickpack_query = " AND packing_status > 0 AND packed_qty <> 0 ";
                                }

                                string orderItemQuery = "SELECT " + disc_query + " p.product_remark, oi.product_code, oi.parent_code, oi.product_name, oi.quantity, oi.unit_uom, oi.unit_price, oi.sub_total, oi.salesperson_remark, oi.packed_qty, p.product_id, up.product_uom_rate AS uom_rate " + udf_query + " FROM cms_order_item AS oi LEFT JOIN cms_product p ON p.product_code = oi.product_code LEFT JOIN cms_product_uom_price_v2 up ON up.product_code = p.product_code AND up.product_uom = oi.unit_uom AND up.active_status = 1 WHERE oi.cancel_status = 0 AND oi.order_id = '" + orderObj["order_id"] + "' " + item_validity_query + " " + pickpack_query + " AND(oi.isParent = 0 OR parent_code <> 'PACKAGE')  ORDER BY oi.order_item_id ASC"; //added oi.isParent = 0 15102020 AND oi.isParent = 0
                                mysql.Message("orderItemQuery: " + orderItemQuery);
                                ArrayList orderItems = mysql.Select(orderItemQuery);

                                ArrayList pckgParentCodeList = new ArrayList();
                                if (checking_package == 1)
                                {
                                    string checkingPackage = "SELECT isParent, parent_code, product_code FROM cms_order_item WHERE parent_code = 'PACKAGE' AND order_id = '" + orderObj["order_id"] + "' AND cancel_status = 0";
                                    ArrayList packageParent = mysql.Select(checkingPackage);
                                    
                                    if (packageParent.Count > 0)
                                    {
                                        for (int ipckg = 0; ipckg < packageParent.Count; ipckg++)
                                        {
                                            Dictionary<string, string> pckg = (Dictionary<string, string>)packageParent[ipckg];
                                            string pckgParentCode = pckg["product_code"];
                                            pckgParentCodeList.Add(pckgParentCode);
                                        }
                                    }
                                }

                                int roundingCount = 0;
                                string itemCodeStr = string.Empty;
                                ArrayList cloudQtyQueryList = new ArrayList();

                                for (int idx = 0; idx < orderItems.Count; idx++)
                                {

                                    string uomrate, qty, sub_total, discount, del_date;
                                    int sqty;
                                    int sequence_no = 0;
                                    double converted_disc;

                                    string itemCode = string.Empty;

                                    Dictionary<string, string> item = (Dictionary<string, string>)orderItems[idx];

                                    lDetailDataSet.Append();

                                    del_date = Convert.ToDateTime(orderObj["delivery_date"]).ToString("yyyy-MM-dd");

                                    uomrate = item["uom_rate"];
                                    qty = item["quantity"];
                                    int.TryParse(uomrate, out int Uomrate);
                                    int.TryParse(qty, out int Qty);

                                    sqty = Qty * Uomrate;

                                    sequence_no++;

                                    lDetailDataSet.FindField("DtlKey").value = -1;
                                    lDetailDataSet.FindField("DocKey").value = -1;
                                    //lDetailDataSet.FindField("Seq").value = sequence_no;

                                    try
                                    {
                                        lDetailDataSet.FindField("ItemCode").value = item["product_code"];
                                        itemCode = item["product_code"];

                                        string whCode = orderObj["warehouse_code"];
                                        string cloudQtyQuery = "SELECT * FROM cms_warehouse_stock WHERE product_code = '" + itemCode + "' AND wh_code = " + whCode;
                                        ArrayList checkCloudQty = mysql.Select(cloudQtyQuery);

                                        int cloud_qty = -1;
                                        if (checkCloudQty.Count > 0)
                                        {
                                            Dictionary<string, string> objCloudQty = (Dictionary<string, string>)checkCloudQty[0];
                                            string _cloud_qty = objCloudQty["cloud_qty"];
                                            int.TryParse(_cloud_qty, out cloud_qty);
                                            mysql.Message("cloudQtyQuery [picked]: " + cloudQtyQuery + " --- [" + cloud_qty + "] ");
                                        }

                                        if (cloud_qty > 0)
                                        {
                                            string updateWhStock = "UPDATE cms_warehouse_stock SET cloud_qty = cloud_qty - " + qty + " WHERE product_code = '" + itemCode + "' AND wh_code = " + whCode;
                                            cloudQtyQueryList.Add(updateWhStock);
                                            mysql.Message("updateWhStock: " + updateWhStock + " [ minus packedQtyNo: " + qty + "] ");
                                        }

                                        int balanceQty = 0;
                                        if (check_item_bal == 1) //weiwo but they dont use anymore
                                        {
                                            lRptVar = ComServer.DBManager.NewDataSet("SELECT CODE, BALSQTY FROM ST_ITEM WHERE CODE ='" + itemCode + "'");
                                            lRptVar.First();

                                            while (!lRptVar.eof)
                                            {
                                                string _balanceQty = lRptVar.FindField("BALSQTY").value;
                                                int.TryParse(_balanceQty, out balanceQty);
                                                lRptVar.Next();
                                            }

                                            if (balanceQty < Qty)
                                            {
                                                fault++;

                                                if (itemCodeStr != string.Empty)
                                                {
                                                    itemCodeStr += ", " + itemCode;
                                                }
                                                else
                                                {
                                                    itemCodeStr = itemCode;
                                                }
                                                string msg = "Not enough stock for this item(s): " + itemCodeStr;
                                                mysql.Insert("UPDATE cms_order SET order_approved = '2', order_comment = '" + msg + "' WHERE order_id = '" + orderObj["order_id"] + "'");
                                            }
                                        }
                                    }
                                    catch (Exception e)
                                    {
                                        if (result.Count > 0)
                                        {
                                            Console.WriteLine(e.Message);
                                            string productCode = item["product_code"];
                                            string unitUom = item["unit_uom"];

                                            Database.Sanitize(ref productCode);
                                            Database.Sanitize(ref unitUom);

                                            mysql.Insert("UPDATE cms_order SET order_fault = '2', order_fault_message = 'Invalid Item code(" + productCode + "[" + unitUom + "])' WHERE order_id = '" + orderObj["order_id"] + "'");
                                        }
                                    }

                                    if (location != string.Empty)
                                    {
                                        lDetailDataSet.FindField("Location").value = location;
                                    }
                                    else
                                    {
                                        //location based on warehouse_code
                                        string whCode = orderObj["warehouse_code"];
                                        if (whCode != string.Empty)
                                        {
                                            lDetailDataSet.FindField("Location").value = whCode;
                                        }
                                        else
                                        {
                                            lDetailDataSet.FindField("Location").value = "----";
                                        }
                                    }

                                    //lDetailDataSet.FindField("Project").value = "----";

                                    string item_warehouse_code = orderObj["warehouse_code"];
                                    string item_project = "----";

                                    if (finalProjectList.Count > 0)
                                    {
                                        if (finalProjectList.ContainsKey(item_warehouse_code))
                                        {
                                            item_project = finalProjectList.Where(pair => pair.Key == item_warehouse_code)
                                                                .Select(pair => pair.Value)
                                                                .FirstOrDefault();
                                            Console.WriteLine(project);
                                            lDetailDataSet.FindField("Project").value = item_project;
                                        }
                                        else
                                        {
                                            lDetailDataSet.FindField("Project").value = "----";
                                        }
                                    }
                                    else
                                    {
                                        lDetailDataSet.FindField("Project").value = "----";
                                    }


                                    //lDetailDataSet.FindField("REMARK1").value = item["salesperson_remark"]; //comment for samfah bcs insert in desc2
                                    //INSERT THIS BACKEND RULES FOR NEXT CLIENT 
                                    //"order_dtl_field": [
                                    //{ "mysql":"salesperson_remark", "sqlacc":"REMARK1"}
                                    //],
                                    //"order_dtl_field_separator":[
                                    //{ "fieldname":"salesperson_remark", "separator":""}	
                                    //]
                                    discount = "0%";
                                    if (hasUdf != null)
                                    {
                                        dynamic splitted_remark = SplitByTag(item["salesperson_remark"], appRemark);
                                        //{[DESCRIPTION,  fabric grey ]}
                                        //{[REMARK1,  gaa18539-ida]}

                                        Type splitRemarkType = splitted_remark.GetType();
                                        if (splitRemarkType.Name == "ArrayList")
                                        {
                                            foreach (var value in splitted_remark)
                                            {
                                                foreach (var keyValue in value)
                                                {
                                                    if (keyValue.Key == "DESCRIPTION")
                                                    {
                                                        item["product_name"] = string.IsNullOrEmpty(keyValue.Value) ? item["product_name"] : (item["product_name"] + " - " + keyValue.Value);
                                                    }

                                                    if (keyValue.Key == "REMARK1")
                                                    {
                                                        lDetailDataSet.FindField("REMARK1").value = keyValue.Value;
                                                    }
                                                }
                                            }
                                        }
                                        else
                                        {
                                            item["product_name"] = string.IsNullOrEmpty(item["salesperson_remark"]) ? item["product_name"] : (item["product_name"] + " - " + item["salesperson_remark"]);
                                        }

                                        if (sql_multi_discount == 0)
                                        {
                                            //if (item["discount_method"] == "PercentDiscountType")
                                            //{
                                            //    discount = item["discount_amount"] + "%";
                                            //}
                                            //else
                                            //{
                                            //    //double disc_1;
                                            //    //double.TryParse(item["disc_1"], out disc_1);
                                            //    //string _discount_amount = item["disc_1"];

                                            //    string _discount_amount = item["discount_amount"];
                                            //    string _quantity = item["quantity"];
                                            //    string _unit_price = item["unit_price"];

                                            //    double.TryParse(_discount_amount, out double discount_amount);
                                            //    double.TryParse(_quantity, out double quantity);
                                            //    double.TryParse(_unit_price, out double unit_price);

                                            //    if (unit_price == 0 || discount_amount == 0)
                                            //    {
                                            //        discount = "0%";
                                            //    }
                                            //    else
                                            //    {
                                            //        converted_disc = (discount_amount * 100) / (quantity * unit_price);
                                            //        discount = Math.Round(converted_disc, 3) + "%";
                                            //        //discount = disc_1 + "%";
                                            //    }
                                            //}

                                            if (item["disc_1"] != item["discount_amount"] && item["disc_1"] != "0")
                                            {
                                                if(item["disc_1"] != string.Empty)
                                                {
                                                    discount = item["disc_1"] + "%";
                                                }
                                                else
                                                {
                                                    discount = "0%";
                                                }
                                                
                                            }
                                            else if (item["disc_1"] == "0" && item["discount_amount"] != "0")
                                            {
                                                //tomauto wants in RM...
                                                double.TryParse(item["discount_amount"], out double discount_amount);
                                                double.TryParse(item["quantity"], out double quantity);
                                                double.TryParse(item["unit_price"], out double unit_price);
                                                double _discount = (discount_amount * 100) / (quantity * unit_price);
                                                double checkDiscInRM = (_discount / 100) * (quantity * unit_price);
                                                if (checkDiscInRM == discount_amount)
                                                {
                                                    discount = discount_amount.ToString();
                                                }
                                                else
                                                {
                                                    discount = Math.Round(_discount, 3) + "%";
                                                }
                                                
                                                //discount = discount + "%";
                                                mysql.Message("[item['disc_1'] == '0' && item['discount_amount'] != '0'] discount: " + discount);
                                            }
                                            else if (item["disc_1"] != "0" && item["discount_amount"] != "0")
                                            {
                                                double.TryParse(item["discount_amount"], out double discount_amount);
                                                double.TryParse(item["quantity"], out double quantity);
                                                double.TryParse(item["unit_price"], out double unit_price);
                                                double _discount = (discount_amount * 100) / (quantity * unit_price);
                                                //discount = _discount + "%";
                                                discount = Math.Round(_discount, 3) + "%";
                                                Console.WriteLine("discount:" + discount);
                                                mysql.Message("[item['disc_1'] != '0' && item['discount_amount'] != '0'] discount: " + discount);

                                            }
                                            else
                                            {
                                                discount = item["discount_amount"] + "%";
                                            }

                                            if(sql_template_package == 1)
                                            {
                                                if (item["udf_istemplate"] == "T")
                                                {
                                                    if (item["disc_1"] != string.Empty)
                                                    {
                                                        discount = item["disc_1"] + "%";
                                                    }
                                                    if (item["disc_2"] != string.Empty)
                                                    {
                                                        discount += "+" + item["disc_2"] + "%";
                                                    }
                                                    if (item["disc_3"] != string.Empty)
                                                    {
                                                        discount += "+" + item["disc_3"] + "%";
                                                    }
                                                }
                                            }
                                        }
                                        else
                                        {
                                            string disc_1, disc_2, disc_3;

                                            disc_1 = item["disc_1"];
                                            disc_2 = item["disc_2"];
                                            disc_3 = item["disc_3"];

                                            if (float.Parse(disc_1) > 0 || float.Parse(disc_2) > 0 || float.Parse(disc_3) > 0)
                                            {
                                                discount = float.Parse(disc_1) + "%+" + float.Parse(disc_2) + "%+" + float.Parse(disc_3) + "%";
                                            }
                                            discount = discount.Replace("+0%", ""); //"2%+3%+4%"
                                        }

                                        if (sql_template_package != 0)
                                        {
                                            lDetailDataSet.FindField("UDF_ISTEMPLATE").AsString = item["udf_istemplate"];
                                            lDetailDataSet.FindField("UDF_DeliDate_Remark").AsString = del_date;

                                            if (item["udf_istemplate"] == "T")
                                            {
                                                lDetailDataSet.FindField("UDF_TemplateCode").AsString = item["parent_code"];
                                            }

                                            float udf_m3, udf_totalM3, udf_packing;
                                            string udf_remark;

                                            udf_m3 = 0;
                                            udf_totalM3 = 0;
                                            udf_packing = 0;

                                            udf_remark = item["product_remark"];
                                            string[] exploaded = udf_remark.Split('|');

                                            for (int kkk = 0; kkk < exploaded.Length; kkk++)
                                            {
                                                string needle = exploaded[kkk];
                                                if (needle.Contains("M3:") != false)
                                                {
                                                    string m3 = needle.Split(':')[1];
                                                    float totalM3 = float.Parse(m3) * float.Parse(item["quantity"]);

                                                    udf_m3 = float.Parse(m3);
                                                    udf_totalM3 = totalM3;
                                                }
                                                if (needle.Contains("PACKING:") != false)
                                                {
                                                    string packing = needle.Split(':')[1];
                                                    udf_packing = float.Parse(packing);
                                                }
                                            }

                                            lDetailDataSet.FindField("UDF_Packing").value = udf_packing;
                                            lDetailDataSet.FindField("UDF_M3").value = udf_m3;
                                            lDetailDataSet.FindField("UDF_TotalM3").value = udf_totalM3;
                                        }
                                    }

                                    lDetailDataSet.FindField("Description").value = item["product_name"];

                                    try
                                    {
                                        lDetailDataSet.FindField("UOM").value = item["unit_uom"];
                                    }
                                    catch (Exception)
                                    {
                                        if (result.Count > 0)
                                        {
                                            string productCode = item["product_code"];
                                            string unitUom = item["unit_uom"];

                                            Database.Sanitize(ref productCode);
                                            Database.Sanitize(ref unitUom);

                                            mysql.Insert("UPDATE cms_order SET order_fault = '2', order_fault_message = 'Invalid UOM(" + productCode + "[" + unitUom + "])' WHERE order_id = '" + orderObj["order_id"] + "'");
                                        }
                                    }

                                    if (pickpack_link == 0)
                                    {
                                        sub_total = item["sub_total"];

                                        lDetailDataSet.FindField("QTY").value = qty;
                                        lDetailDataSet.FindField("Amount").value = sub_total;
                                        lDetailDataSet.FindField("LocalAmount").value = sub_total;
                                    }
                                    else
                                    {
                                        //get packed_qty
                                        string _packed_qty = item["packed_qty"];
                                        int.TryParse(_packed_qty, out int packed_qty);

                                        string _unitPrice = item["unit_price"];
                                        double.TryParse(_unitPrice, out double unitPrice);
                                        double subTotalPacked = unitPrice * packed_qty;

                                        lDetailDataSet.FindField("QTY").value = packed_qty;
                                        lDetailDataSet.FindField("Amount").value = subTotalPacked;
                                        lDetailDataSet.FindField("LocalAmount").value = subTotalPacked;
                                    }

                                    if (ItemTax.Count > 0) //pluto
                                    {
                                        string _taxtype = string.Empty;
                                        string _taxrate = string.Empty;

                                        string taxtype = string.Empty;
                                        string taxrate = string.Empty;

                                        for (int itax = 0; itax < ItemTax.Count; itax++)
                                        {
                                            _taxtype = ItemTax.ElementAt(itax).Key;
                                            _taxrate = ItemTax.ElementAt(itax).Value;

                                            if (_taxrate == SST)
                                            {
                                                taxtype = _taxtype;
                                                taxrate = _taxrate;
                                                break;
                                            }
                                        }

                                        lDetailDataSet.FindField("Tax").value = taxtype;
                                        lDetailDataSet.FindField("TaxRate").value = taxrate + "%";

                                        double.TryParse(taxrate, out double doubleTaxRate);
                                        string _unitPrice = item["unit_price"];
                                        double.TryParse(_unitPrice, out double unitPrice);

                                        double _taxAmt = unitPrice * (doubleTaxRate / 100);
                                        string taxAmt = _taxAmt.ToString();
                                        lDetailDataSet.FindField("LocalTaxAmt").value = 0;
                                        //lDetailDataSet.FindField("TaxInclusive").value = "T"; #meaning the tax is in the price already

                                        int.TryParse(qty, out int _qty);
                                        _taxAmt = _taxAmt * _qty;
                                        _total = _total + _taxAmt;

                                        sub_total = item["sub_total"];
                                        double.TryParse(sub_total, out double _subtotal);
                                        _subtotal = _subtotal + _taxAmt;
                                        sub_total = _subtotal.ToString();

                                        lDetailDataSet.FindField("Amount").value = sub_total;
                                        lDetailDataSet.FindField("LocalAmount").value = sub_total;
                                    }
                                    else
                                    {
                                        lDetailDataSet.FindField("Tax").value = "";
                                        lDetailDataSet.FindField("TaxRate").value = "";
                                        lDetailDataSet.FindField("TaxAmt").value = 0;
                                        lDetailDataSet.FindField("LocalTaxAmt").value = 0;
                                        lDetailDataSet.FindField("TaxInclusive").value = 0;
                                    }

                                    lDetailDataSet.FindField("Rate").value = item["uom_rate"];
                                    lDetailDataSet.FindField("SQTY").value = sqty;
                                    lDetailDataSet.FindField("UnitPrice").value = item["unit_price"];

                                    if (discount != "0%")
                                    {
                                        lDetailDataSet.FindField("Disc").value = discount;
                                    }
                                    else
                                    {
                                        lDetailDataSet.FindField("Disc").value = "";
                                    }

                                    lDetailDataSet.FindField("DeliveryDate").value = del_date;

                                    if(pckgParentCodeList.Count > 0)
                                    {
                                        string childCode = item["product_code"];
                                        string parentCode = item["parent_code"];

                                        if (pckgParentCodeList.Contains(childCode))
                                        {
                                            lDetailDataSet.FindField("Printable").value = "T";
                                        }
                                        else
                                        {
                                            if (pckgParentCodeList.Contains(parentCode))
                                            {
                                                lDetailDataSet.FindField("Printable").value = "F";
                                            }
                                            else
                                            {
                                                lDetailDataSet.FindField("Printable").value = "T";
                                            }
                                            
                                        }
                                    }
                                    else
                                    {
                                        lDetailDataSet.FindField("Printable").value = "T";
                                    }
                                    
                                    lDetailDataSet.FindField("Transferable").value = transferable;

                                    if (DtlFieldNameList.Count > 0)
                                    {
                                        string mysql_column = string.Empty;
                                        string sqlacc_column = string.Empty;

                                        for (int iDx = 0; iDx < DtlFieldNameList.Count; iDx++)
                                        {
                                            mysql_column = DtlFieldNameList.ElementAt(iDx).Key;
                                            sqlacc_column = DtlFieldNameList.ElementAt(iDx).Value;

                                            if (mysql_column.Contains("+"))
                                            {
                                                string[] words = mysql_column.Split('+');
                                                string udfString = string.Empty;
                                                string field_separator = "";

                                                foreach (var word in words)
                                                {
                                                    if (DtlFieldSeparatorList.Count > 0)
                                                    {
                                                        if (DtlFieldSeparatorList.ContainsKey(word))
                                                        {
                                                            Dictionary<string, string> value = (Dictionary<string, string>)DtlFieldSeparatorList;
                                                            field_separator = value[word];
                                                        }
                                                    }

                                                    string getValue = orderObj[word];
                                                    if (getValue != string.Empty)
                                                    {
                                                        udfString += field_separator + getValue;
                                                    }
                                                }

                                                lDetailDataSet.FindField(sqlacc_column).AsString = udfString;
                                            }
                                            else
                                            {
                                                Console.WriteLine(sqlacc_column + mysql_column);
                                                lDetailDataSet.FindField(sqlacc_column).AsString = item[mysql_column];
                                            }
                                        }
                                    }

                                    lDetailDataSet.Post();

                                    roundingCount = sequence_no + 1;
                                }

                                total = _total;
                                if (ItemTax.Count > 0)
                                {
                                    string gstTaxAmount = orderObj["gst_tax_amount"];
                                    if (gstTaxAmount != "0")
                                    {
                                        string _grandTotal = orderObj["gst_amount"];
                                        Double.TryParse(_grandTotal, out double grandTotal);
                                        double difference = grandTotal - total;

                                        ArrayList rounding = mysql.Select("SELECT p.*, uom.* FROM cms_product AS p LEFT JOIN cms_product_uom_price_v2 AS uom ON p.product_code = uom.product_code WHERE p.product_code = 'RTN5Cents'");
                                        Dictionary<string, string> roundingObj = (Dictionary<string, string>)rounding[0];
                                        lDetailDataSet.Append();

                                        lDetailDataSet.FindField("DtlKey").value = -1;
                                        lDetailDataSet.FindField("DocKey").value = -1;
                                        lDetailDataSet.FindField("Seq").value = roundingCount;
                                        lDetailDataSet.FindField("ItemCode").value = roundingObj["product_code"];
                                        lDetailDataSet.FindField("Location").value = "----";
                                        lDetailDataSet.FindField("Project").value = "----";
                                        lDetailDataSet.FindField("UOM").value = roundingObj["product_uom"];
                                        lDetailDataSet.FindField("QTY").value = 1;
                                        lDetailDataSet.FindField("Amount").value = difference;
                                        lDetailDataSet.FindField("LocalAmount").value = difference;
                                        lDetailDataSet.FindField("Tax").value = "";
                                        lDetailDataSet.FindField("TaxRate").value = "";
                                        lDetailDataSet.FindField("TaxAmt").value = 0;
                                        lDetailDataSet.FindField("LocalTaxAmt").value = 0;
                                        lDetailDataSet.FindField("TaxInclusive").value = 0;
                                        lDetailDataSet.FindField("Rate").value = roundingObj["product_uom_rate"];
                                        lDetailDataSet.FindField("UnitPrice").value = difference;
                                        lDetailDataSet.FindField("Disc").value = "";
                                        lDetailDataSet.FindField("Printable").value = "T";
                                        lDetailDataSet.FindField("Transferable").value = "T";
                                        lDetailDataSet.Post();
                                        total = total + difference;
                                    }
                                }

                                lMainDataSet.FindField("DocAmt").value = total;
                                if (include_currency == 1)
                                {
                                    //lMainDataSet.FindField("LocalDocAmt").value = total;
                                    //no need to insert as SQLAccounting will calculate by itself
                                }
                                else
                                {
                                    lMainDataSet.FindField("LocalDocAmt").value = total;
                                }

                                if(orderItems.Count > 0)
                                {
                                    lMainDataSet.Post();

                                    if (fault == 0)
                                    {
                                        int updateOrderStatus = order_status + 1;
                                        try
                                        {
                                            BizObject.Save();

                                            //deducting the cloud qty
                                            if (cloudQtyQueryList.Count > 0)
                                            {
                                                for (int ixx = 0; ixx < cloudQtyQueryList.Count; ixx++)
                                                {
                                                    string query = cloudQtyQueryList[ixx].ToString();
                                                    mysql.Insert(query);
                                                }
                                                cloudQtyQueryList.Clear();
                                            }

                                            postCount++;

                                            if (include_no_stock_so == 0)
                                            {
                                                mysql.Insert("INSERT INTO cms_order (order_id, order_status) VALUES ('" + orderObj["order_id"] + "','" + updateOrderStatus + "') ON DUPLICATE KEY UPDATE order_status = VALUES(order_status)");
                                            }
                                            logger.message = orderObj["order_id"] + " created";
                                            logger.Broadcast();
                                        }
                                        catch (Exception e)
                                        {
                                            if (result.Count > 0)
                                            {
                                                if (e.Message.IndexOf("duplicate") != -1)
                                                {
                                                    postCount++;
                                                    //mysql.Insert("UPDATE cms_order SET order_fault = '3', order_fault_message = 'Order ID duplicated' WHERE order_id = '" + orderObj["order_id"] + "'");
                                                    mysql.Insert("INSERT INTO cms_order (order_id, order_status) VALUES ('" + orderObj["order_id"] + "','" + updateOrderStatus + "') ON DUPLICATE KEY UPDATE order_status = VALUES(order_status)");
                                                }
                                                else if (e.Message.IndexOf("limit") != -1)
                                                {
                                                    mysql.Insert("UPDATE cms_order SET order_fault = '4', order_fault_message = 'Customer credit limit exceeded' WHERE order_id = '" + orderObj["order_id"] + "'");
                                                }
                                                else if (e.Message.IndexOf("customer") != -1)
                                                {
                                                    mysql.Insert("UPDATE cms_order SET order_fault = '1', order_fault_message = 'Invalid Customer Code' WHERE order_id = '" + orderObj["order_id"] + "'");
                                                }
                                                else
                                                {
                                                    mysql.Insert("UPDATE cms_order SET order_fault = '5', order_fault_message = '" + e.Message + "' WHERE order_id = '" + orderObj["order_id"] + "'");
                                                }
                                            }
                                        }
                                    }
                                    BizObject.Close();
                                }

                                //do no stock insertion here

                                if (include_no_stock_so == 1)
                                {
                                    BizObject.New();

                                    string noStockSOId = orderObj["order_id"] + "-1";
                                    lMainDataSet.FindField("DocKey").value = -1;
                                    lMainDataSet.FindField("DocNo").value = noStockSOId;
                                    lMainDataSet.FindField("DocDate").value = post_date;
                                    lMainDataSet.FindField("PostDate").value = post_date;
                                    lMainDataSet.FindField("TaxDate").value = post_date;
                                    lMainDataSet.FindField("Code").value = orderObj["cust_code"];
                                    lMainDataSet.FindField("CompanyName").value = orderObj["cust_company_name"];
                                    lMainDataSet.FindField("Address1").value = orderObj["billing_address1"];
                                    lMainDataSet.FindField("Address2").value = orderObj["billing_address2"];
                                    lMainDataSet.FindField("Address3").value = orderObj["billing_address3"];
                                    lMainDataSet.FindField("Address4").value = orderObj["billing_address4"];
                                    lMainDataSet.FindField("Phone1").value = orderObj["cust_tel"];
                                    lMainDataSet.FindField("Fax1").value = orderObj["cust_fax"];
                                    lMainDataSet.FindField("Attention").value = orderObj["cust_incharge_person"];
                                    lMainDataSet.FindField("Area").value = orderObj["billing_state"];
                                    lMainDataSet.FindField("Agent").value = agentCode;                  //orderObj["staff_code"];
                                    lMainDataSet.FindField("Project").value = "----";
                                    lMainDataSet.FindField("Terms").value = orderObj["termcode"];
                                    lMainDataSet.FindField("CurrencyCode").value = "----";
                                    lMainDataSet.FindField("CurrencyRate").value = "1";
                                    lMainDataSet.FindField("Shipper").value = "----";
                                    lMainDataSet.FindField("Description").value = "Sales Order";
                                    lMainDataSet.FindField("Cancelled").value = "F";
                                    lMainDataSet.FindField("DocAmt").value = total;
                                    lMainDataSet.FindField("LocalDocAmt").value = total;
                                    
                                    lMainDataSet.FindField("D_Amount").value = "0";
                                    lMainDataSet.FindField("D_BankCharge").value = "0";
                                    lMainDataSet.FindField("BranchName").value = branch_name;

                                    lMainDataSet.FindField("DOCREF1").value = branch_name;

                                    lMainDataSet.FindField("DAddress1").value = orderObj["shipping_address1"];
                                    lMainDataSet.FindField("DAddress2").value = orderObj["shipping_address2"];
                                    lMainDataSet.FindField("DAddress3").value = orderObj["shipping_address3"];
                                    lMainDataSet.FindField("DAddress4").value = orderObj["shipping_address4"];
                                    lMainDataSet.FindField("DAttention").value = "-";
                                    lMainDataSet.FindField("DPhone1").value = "-";
                                    lMainDataSet.FindField("DFax1").value = "-";
                                    lMainDataSet.FindField("Transferable").value = transferable;
                                    lMainDataSet.FindField("PrintCount").value = "0";
                                    lMainDataSet.FindField("CHANGED").AsString = "F";
                                    lMainDataSet.FindField("NOTE").AsString = orderObj["order_delivery_note"] + "\n\n -- \n\n" + orderObj["image"];

                                    lMainDataSet.Post();

                                    disc_query = "";
                                    if (sql_multi_discount != 0)
                                    {
                                        disc_query = "oi.disc_1,oi.disc_2,oi.disc_3,";
                                    }
                                    else
                                    {
                                        disc_query = "oi.discount_method,oi.discount_amount,";
                                    }

                                    udf_query = "";
                                    if (sql_template_package != 0)
                                    {
                                        udf_query = ", IF(parent_code IS NULL OR parent_code = '','F','T') AS udf_istemplate";
                                    }

                                    item_validity_query = "";
                                    if (check_min_price != 0)
                                    {
                                        item_validity_query = " AND oi.order_item_validity = 2 ";
                                    }

                                    pickpack_query = "";
                                    if (pickpack_link != 0)
                                    {
                                        pickpack_query = " AND packed_qty <> quantity  "; //AND pack_confirmed_status = 1
                                    }

                                    string noStockItemQuery = "SELECT " + disc_query + " p.product_remark, oi.packed_qty, oi.product_code, oi.parent_code, oi.product_name, oi.quantity, oi.unit_uom, oi.unit_price, oi.sub_total, oi.salesperson_remark, p.product_id, up.product_uom_rate AS uom_rate " + udf_query + " FROM cms_order_item AS oi LEFT JOIN cms_product p ON p.product_code = oi.product_code LEFT JOIN cms_product_uom_price_v2 up ON up.product_code = p.product_code AND up.product_uom = oi.unit_uom WHERE oi.cancel_status = 0 AND oi.order_id = '" + orderObj["order_id"] + "' " + item_validity_query + " " + pickpack_query + " ORDER BY oi.order_item_id DESC";
                                    ArrayList noStockOrderItems = mysql.Select(noStockItemQuery);

                                    if (noStockOrderItems.Count != 0)
                                    {
                                        string itemCodeStr1 = string.Empty;

                                        for (int idx = 0; idx < noStockOrderItems.Count; idx++)
                                        {
                                            string uomrate, qty, discount, del_date;
                                            int sqty;
                                            int sequence_no = 0;
                                            double converted_disc;

                                            string itemCode = string.Empty;

                                            Dictionary<string, string> item = (Dictionary<string, string>)noStockOrderItems[idx];

                                            lDetailDataSet.Append();

                                            del_date = Convert.ToDateTime(orderObj["delivery_date"]).ToString("yyyy-MM-dd");

                                            uomrate = item["uom_rate"];
                                            qty = item["quantity"];
                                            int.TryParse(uomrate, out int Uomrate);
                                            int.TryParse(qty, out int Qty);

                                            sqty = Qty * Uomrate;

                                            sequence_no++;

                                            lDetailDataSet.FindField("DtlKey").value = -1;
                                            lDetailDataSet.FindField("DocKey").value = -1;

                                            try
                                            {
                                                lDetailDataSet.FindField("ItemCode").value = item["product_code"];
                                                itemCode = item["product_code"];

                                                int balanceQty = 0;
                                                if (check_item_bal == 1)
                                                {
                                                    lRptVar = ComServer.DBManager.NewDataSet("SELECT CODE, BALSQTY FROM ST_ITEM WHERE CODE ='" + itemCode + "'");
                                                    lRptVar.First();

                                                    while (!lRptVar.eof)
                                                    {
                                                        string _balanceQty = lRptVar.FindField("BALSQTY").value;
                                                        int.TryParse(_balanceQty, out balanceQty);
                                                        lRptVar.Next();
                                                    }

                                                    if (balanceQty < Qty)
                                                    {
                                                        fault++;

                                                        if (itemCodeStr1 != string.Empty)
                                                        {
                                                            itemCodeStr1 += ", " + itemCode;
                                                        }
                                                        else
                                                        {
                                                            itemCodeStr1 = itemCode;
                                                        }
                                                        string msg = "Not enough stock for this item(s): " + itemCodeStr1;
                                                        mysql.Insert("UPDATE cms_order SET order_approved = '2', order_comment = '" + msg + "' WHERE order_id = '" + orderObj["order_id"] + "'");
                                                    }
                                                }
                                            }
                                            catch (Exception e)
                                            {
                                                if (result.Count > 0)
                                                {
                                                    Console.WriteLine(e.Message);
                                                    string productCode = item["product_code"];
                                                    string unitUom = item["unit_uom"];

                                                    Database.Sanitize(ref productCode);
                                                    Database.Sanitize(ref unitUom);

                                                    mysql.Insert("UPDATE cms_order SET order_fault = '2', order_fault_message = 'Invalid Item code(" + productCode + "[" + unitUom + "])' WHERE order_id = '" + orderObj["order_id"] + "'");
                                                }
                                            }

                                            if (location != string.Empty)
                                            {
                                                lDetailDataSet.FindField("Location").value = location;
                                            }
                                            else
                                            {
                                                string whCode = orderObj["warehouse_code"];
                                                if (whCode != string.Empty)
                                                {
                                                    if (whCode == "HQ")
                                                    {
                                                        lDetailDataSet.FindField("Location").value = "----";
                                                    }
                                                    else
                                                    {
                                                        lDetailDataSet.FindField("Location").value = whCode;
                                                    }
                                                }
                                                else
                                                {
                                                    lDetailDataSet.FindField("Location").value = "----";
                                                }
                                            }
                                            lDetailDataSet.FindField("Project").value = "----";
                                            lDetailDataSet.FindField("REMARK1").value = item["salesperson_remark"];

                                            discount = "0%";

                                            if (hasUdf != null)
                                            {
                                                dynamic splitted_remark = SplitByTag(item["salesperson_remark"], appRemark);

                                                Type splitRemarkType = splitted_remark.GetType();
                                                if (splitRemarkType.Name == "ArrayList")
                                                {
                                                    foreach (var value in splitted_remark)
                                                    {
                                                        foreach (var keyValue in value)
                                                        {
                                                            if (keyValue.Key == "DESCRIPTION")
                                                            {
                                                                item["product_name"] = string.IsNullOrEmpty(keyValue.Value) ? item["product_name"] : (item["product_name"] + " - " + keyValue.Value);
                                                            }

                                                            if (keyValue.Key == "REMARK1")
                                                            {
                                                                lDetailDataSet.FindField("REMARK1").value = keyValue.Value;
                                                            }
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    item["product_name"] = string.IsNullOrEmpty(item["salesperson_remark"]) ? item["product_name"] : (item["product_name"] + " - " + item["salesperson_remark"]);
                                                }

                                                if (sql_multi_discount == 0)
                                                {
                                                    if (item["disc_1"] != item["discount_amount"] && item["disc_1"] != "0")
                                                    {
                                                        if (item["disc_1"] != string.Empty)
                                                        {
                                                            discount = item["disc_1"] + "%";
                                                        }
                                                        else
                                                        {
                                                            discount = "0%";
                                                        }
                                                    }
                                                    else if (item["disc_1"] == "0" && item["discount_amount"] != "0")
                                                    {
                                                        double.TryParse(item["discount_amount"], out double discount_amount);
                                                        double.TryParse(item["quantity"], out double quantity);
                                                        double.TryParse(item["unit_price"], out double unit_price);
                                                        double _discount = (discount_amount * 100) / (quantity * unit_price);
                                                        discount = _discount + "%";
                                                        Console.WriteLine("discount:" + discount);
                                                    }
                                                    else if (item["disc_1"] != "0" && item["discount_amount"] != "0")
                                                    {
                                                        double.TryParse(item["discount_amount"], out double discount_amount);
                                                        double.TryParse(item["quantity"], out double quantity);
                                                        double.TryParse(item["unit_price"], out double unit_price);
                                                        double _discount = (discount_amount * 100) / (quantity * unit_price);
                                                        discount = _discount + "%";
                                                        Console.WriteLine("discount:" + discount);
                                                    }
                                                    else
                                                    {
                                                        discount = item["discount_amount"] + "%";
                                                    }
                                                }
                                                else
                                                {
                                                    string disc_1, disc_2, disc_3;

                                                    disc_1 = item["disc_1"];
                                                    disc_2 = item["disc_2"];
                                                    disc_3 = item["disc_3"];

                                                    if (float.Parse(disc_1) > 0 || float.Parse(disc_2) > 0 || float.Parse(disc_3) > 0)
                                                    {
                                                        discount = float.Parse(disc_1) + "%+" + float.Parse(disc_2) + "%+" + float.Parse(disc_3) + "%";
                                                    }
                                                    discount = discount.Replace("+0%", "");
                                                }

                                                if (sql_template_package != 0)
                                                {
                                                    lDetailDataSet.FindField("UDF_ISTEMPLATE").AsString = item["udf_istemplate"];
                                                    lDetailDataSet.FindField("UDF_DeliDate_Remark").AsString = del_date;

                                                    if (item["udf_istemplate"] == "T")
                                                    {
                                                        lDetailDataSet.FindField("UDF_TemplateCode").AsString = item["parent_code"];
                                                    }

                                                    float udf_m3, udf_totalM3, udf_packing;
                                                    string udf_remark;

                                                    udf_m3 = 0;
                                                    udf_totalM3 = 0;
                                                    udf_packing = 0;

                                                    udf_remark = item["product_remark"];
                                                    string[] exploaded = udf_remark.Split('|');

                                                    for (int kkk = 0; kkk < exploaded.Length; kkk++)
                                                    {
                                                        string needle = exploaded[kkk];
                                                        if (needle.Contains("M3:") != false)
                                                        {
                                                            string m3 = needle.Split(':')[1];
                                                            float totalM3 = float.Parse(m3) * float.Parse(item["quantity"]);

                                                            udf_m3 = float.Parse(m3);
                                                            udf_totalM3 = totalM3;
                                                        }
                                                        if (needle.Contains("PACKING:") != false)
                                                        {
                                                            string packing = needle.Split(':')[1];
                                                            udf_packing = float.Parse(packing);
                                                        }
                                                    }

                                                    lDetailDataSet.FindField("UDF_Packing").value = udf_packing;
                                                    lDetailDataSet.FindField("UDF_M3").value = udf_m3;
                                                    lDetailDataSet.FindField("UDF_TotalM3").value = udf_totalM3;
                                                }
                                            }

                                            lDetailDataSet.FindField("Description").value = item["product_name"];

                                            try
                                            {
                                                lDetailDataSet.FindField("UOM").value = item["unit_uom"];
                                            }
                                            catch (Exception)
                                            {
                                                if (result.Count > 0)
                                                {
                                                    string productCode = item["product_code"];
                                                    string unitUom = item["unit_uom"];

                                                    Database.Sanitize(ref productCode);
                                                    Database.Sanitize(ref unitUom);

                                                    mysql.Insert("UPDATE cms_order SET order_fault = '2', order_fault_message = 'Invalid UOM(" + productCode + "[" + unitUom + "])' WHERE order_id = '" + orderObj["order_id"] + "'");
                                                }
                                            }

                                            string _qty = item["quantity"];
                                            string _packedqty = item["packed_qty"];
                                            int.TryParse(_qty, out int totalQty);
                                            int.TryParse(_packedqty, out int packedQty);

                                            int noStockQty = totalQty - packedQty;

                                            string _unitPrice = item["unit_price"];
                                            string _subTotal = item["sub_total"];

                                            double.TryParse(_unitPrice, out double unitPrice);
                                            double.TryParse(_subTotal, out double subTotal);

                                            double subTotalNoStock = unitPrice * noStockQty;

                                            lDetailDataSet.FindField("QTY").value = noStockQty;
                                            lDetailDataSet.FindField("Rate").value = item["uom_rate"];
                                            lDetailDataSet.FindField("SQTY").value = sqty;
                                            lDetailDataSet.FindField("UnitPrice").value = item["unit_price"];

                                            if (discount != "0%")
                                            {
                                                lDetailDataSet.FindField("Disc").value = discount;
                                            }
                                            else
                                            {
                                                lDetailDataSet.FindField("Disc").value = "";
                                            }

                                            lDetailDataSet.FindField("DeliveryDate").value = del_date;

                                            lDetailDataSet.FindField("Tax").value = "";
                                            lDetailDataSet.FindField("TaxRate").value = "";
                                            lDetailDataSet.FindField("TaxAmt").value = 0;
                                            lDetailDataSet.FindField("LocalTaxAmt").value = 0;
                                            lDetailDataSet.FindField("TaxInclusive").value = 0;

                                            lDetailDataSet.FindField("Amount").value = subTotalNoStock;
                                            lDetailDataSet.FindField("LocalAmount").value = subTotalNoStock;
                                            lDetailDataSet.FindField("Printable").value = "T";
                                            lDetailDataSet.FindField("Transferable").value = transferable;

                                            lDetailDataSet.Post();
                                        }

                                        if (fault == 0)
                                        {
                                            try
                                            {
                                                BizObject.Save();

                                                postCount++;
                                                int updateOrderStatus = order_status + 1;
                                                logger.Broadcast(noStockSOId + " [NO STOCK SO] created");
                                            }
                                            catch (Exception e)
                                            {
                                                if (result.Count > 0)
                                                {
                                                    if (e.Message.IndexOf("duplicate") != -1)
                                                    {
                                                        //mysql.Insert("UPDATE cms_order SET order_fault = '3', order_fault_message = 'Order ID duplicated' WHERE order_id = '" + orderObj["order_id"] + "'");
                                                    }
                                                    else if (e.Message.IndexOf("limit") != -1)
                                                    {
                                                        mysql.Insert("UPDATE cms_order SET order_fault = '4', order_fault_message = 'Customer credit limit exceeded' WHERE order_id = '" + orderObj["order_id"] + "'");
                                                    }
                                                    else
                                                    {
                                                        mysql.Insert("UPDATE cms_order SET order_fault = '1', order_fault_message = 'Invalid Customer Code' WHERE order_id = '" + orderObj["order_id"] + "'");
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    BizObject.Close();
                                }

                                if (postCount > 0)
                                {
                                    int updateOrderStatus = order_status + 1;
                                    string updateOrderStatusQuery = "INSERT INTO cms_order (order_id, order_status) VALUES ('" + orderId + "','" + updateOrderStatus + "') ON DUPLICATE KEY UPDATE order_status = VALUES(order_status)";
                                    mysql.Insert(updateOrderStatusQuery);

                                    if (clientSOID != string.Empty)
                                    {
                                        if(orderRunningId == string.Empty)
                                        {
                                            mysql.Insert("UPDATE cms_setting SET so_running_id = so_running_id + 1");
                                        }

                                        string updateOrderRef = "UPDATE cms_order SET order_reference = '" + clientSOID + "' WHERE order_id = '" + orderObj["order_id"] + "'";
                                        mysql.Insert(updateOrderRef);
                                    }

                                    postCount = 0;
                                }

                                //transfer delivery order here
                                if (include_do == 1)
                                {
                                    dynamic lRptVarDO, lRptVarDODTL;
                                    string del_date;
                                    post_date = orderObj["order_date"];
                                    post_date = Convert.ToDateTime(orderObj["order_date"]).ToString("yyyy-MM-dd");
                                    del_date = orderObj["delivery_date"];
                                    del_date = Convert.ToDateTime(orderObj["delivery_date"]).ToString("yyyy-MM-dd");

                                    double totalDO;
                                    Total = orderObj["grand_total"];
                                    double.TryParse(Total, out totalDO);
                                    totalDO = totalDO * 1.00;

                                    string DO_ID;
                                    dynamic balQTY, Qty;

                                    string SO_ID = clientSOID != string.Empty ? clientSOID : orderObj["order_id"];
                                    Console.WriteLine("SO_ID:" + SO_ID);
                                    DO_ID = SO_ID.Replace("SO", "DO");
                                    Console.WriteLine("DO_ID:" + DO_ID);
                                    int valid = 0;

                                    string getSO = "SELECT SO.QTY,I.BALSQTY FROM SL_SODTL SO LEFT JOIN ST_ITEM I ON SO.ItemCode = I.Code WHERE SO.Dockey=(SELECT DocKey FROM SL_SO WHERE ";
                                    //if include ext no get from ext no if not get from docno
                                    if(include_ext_no == 1)
                                    {
                                        getSO += " DocNoEx= ";
                                    }
                                    else
                                    {
                                        getSO += " DocNo= ";
                                    }
                                    getSO = getSO + (clientSOID != string.Empty ? "'" + clientSOID + "')" : "'" + orderObj["order_id"] + "')");
                                    Console.WriteLine(getSO);
                                    mysql.Message("getSO:" + getSO);
                                    lRptVarDO = ComServer.DBManager.NewDataSet(getSO);
                                    lRptVarDO.First();

                                    if(check_balqty_do == 1) //aquatic need, oasis no need to check
                                    {
                                        while (!lRptVarDO.eof)
                                        {
                                            balQTY = lRptVarDO.FindField("BALSQTY").AsFloat;
                                            Qty = lRptVarDO.FindField("QTY").AsFloat;
                                            if (balQTY >= Qty)
                                            {
                                                Qty = Qty;
                                            }
                                            else
                                            {
                                                Qty = balQTY;
                                            }
                                            if (Qty > 0)
                                            {
                                                valid += 1;
                                            }
                                            lRptVarDO.Next();
                                        }
                                    }
                                    else
                                    {
                                        valid = 1;
                                    }
                                    

                                    if (valid > 0)
                                    {
                                        dynamic BizObjectDO, lMainDataSetDO, lDetailDataSetDO;
                                        BizObjectDO = ComServer.BizObjects.Find("SL_DO");

                                        lMainDataSetDO = BizObjectDO.DataSets.Find("MainDataSet");
                                        lDetailDataSetDO = BizObjectDO.DataSets.Find("cdsDocDetail");

                                        BizObjectDO.New();

                                        lMainDataSetDO.FindField("DocKey").value = -1;
                                        if (include_ext_no == 1)
                                        {
                                            lMainDataSetDO.FindField("DocNo").value = "<<New>>";//DO_ID;
                                            lMainDataSetDO.FindField("DocNoEx").value = orderObj["order_id"];
                                        }
                                        else
                                        {
                                            lMainDataSetDO.FindField("DocNo").value = DO_ID;
                                        }

                                        lMainDataSetDO.FindField("DocDate").value = post_date;
                                        lMainDataSetDO.FindField("PostDate").value = post_date;
                                        lMainDataSetDO.FindField("TaxDate").value = post_date;
                                        lMainDataSetDO.FindField("Code").value = orderObj["cust_code"];
                                        lMainDataSetDO.FindField("CompanyName").value = orderObj["cust_company_name"];
                                        lMainDataSetDO.FindField("Address1").value = orderObj["billing_address1"];
                                        lMainDataSetDO.FindField("Address2").value = orderObj["billing_address2"];
                                        lMainDataSetDO.FindField("Address3").value = orderObj["billing_address3"];
                                        lMainDataSetDO.FindField("Address4").value = orderObj["billing_address4"];
                                        lMainDataSetDO.FindField("Phone1").value = orderObj["cust_tel"];
                                        lMainDataSetDO.FindField("Description").value = "Delivery Order";

                                        lMainDataSetDO.Post();

                                        //string getSODTL = "SELECT SO.*,I.BALSQTY FROM SL_SODTL SO LEFT JOIN ST_ITEM I ON SO.ItemCode = I.Code WHERE SO.Dockey=(SELECT DocKey FROM SL_SO WHERE DocNo= ";
                                        string getSODTL = "SELECT SO.*,I.BALSQTY FROM SL_SODTL SO LEFT JOIN ST_ITEM I ON SO.ItemCode = I.Code WHERE SO.Dockey=(SELECT DocKey FROM SL_SO WHERE ";
                                        if (include_ext_no == 1)
                                        {
                                            getSODTL += " DocNoEx= ";
                                        }
                                        else
                                        {
                                            getSODTL += " DocNo= ";
                                        }
                                        getSODTL = getSODTL + (clientSOID != string.Empty ? "'" + clientSOID + "')" : "'" + orderObj["order_id"] + "')");
                                        //if include ext no get from ext no if not get from docno
                                        Console.WriteLine(getSODTL);
                                        lRptVarDODTL = ComServer.DBManager.NewDataSet(getSODTL);
                                        lRptVarDODTL.First();
                                        while (!lRptVarDODTL.eof)
                                        {
                                            double amount = 0;
                                            if (check_balqty_do == 1)
                                            {
                                                balQTY = lRptVarDODTL.FindField("BALSQTY").AsFloat;
                                                Qty = lRptVarDODTL.FindField("QTY").AsFloat;

                                                if (balQTY >= Qty)
                                                {
                                                    Qty = Qty;
                                                }
                                                else
                                                {
                                                    if (balQTY >= 0)
                                                    {
                                                        Qty = balQTY;
                                                    }
                                                    else
                                                    {
                                                        Qty = 0;
                                                    }
                                                }

                                                amount = Qty * lRptVarDODTL.FindField("UnitPrice").AsFloat;
                                            }
                                            else
                                            {
                                                Qty = lRptVarDODTL.FindField("QTY").AsFloat;
                                                amount = Qty * lRptVarDODTL.FindField("UnitPrice").AsFloat;
                                            }

                                            if (Qty > 0)
                                            {
                                                lDetailDataSetDO.Append();
                                                lDetailDataSetDO.FindField("DtlKey").value = -1;
                                                lDetailDataSetDO.FindField("DtlKey").value = -1;
                                                lDetailDataSetDO.FindField("ItemCode").value = lRptVarDODTL.FindField("ItemCode").AsString;
                                                lDetailDataSetDO.FindField("Description").value = lRptVarDODTL.FindField("Description").AsString;
                                                //lDetail.FindField("Account").AsString     = "500-000" "If you wanted override the Sales Account Code
                                                lDetailDataSetDO.FindField("UOM").value = lRptVarDODTL.FindField("UOM").AsString;
                                                lDetailDataSetDO.FindField("Qty").value = Qty;
                                                lDetailDataSetDO.FindField("DISC").value = lRptVarDODTL.FindField("Disc").AsString;
                                                lDetailDataSetDO.FindField("Tax").value = lRptVarDODTL.FindField("Tax").AsString;
                                                lDetailDataSetDO.FindField("TaxRate").value = lRptVarDODTL.FindField("TaxRate").AsString;
                                                lDetailDataSetDO.FindField("TaxInclusive").value = lRptVarDODTL.FindField("TaxInclusive").Value;
                                                lDetailDataSetDO.FindField("UnitPrice").value = lRptVarDODTL.FindField("UnitPrice").AsFloat;
                                                lDetailDataSetDO.FindField("Amount").value = amount;
                                                lDetailDataSetDO.FindField("TaxAmt").value = lRptVarDODTL.FindField("TaxAmt").AsFloat;
                                                lDetailDataSetDO.FindField("FromDocType").value = "SO";
                                                lDetailDataSetDO.FindField("FromDockey").value = lRptVarDODTL.FindField("DocKey").AsFloat;
                                                lDetailDataSetDO.FindField("FromDtlkey").value = lRptVarDODTL.FindField("DtlKey").AsFloat;
                                                lDetailDataSetDO.Post();
                                            }

                                            lRptVarDODTL.Next();
                                        }

                                        BizObjectDO.Save();
                                        BizObjectDO.Close();
                                    }

                                    if(valid > 0)
                                    {
                                        int updateOrderStatus = order_status + 1;
                                        logger.Broadcast(DO_ID + " created");
                                        string update_status = "UPDATE cms_order SET order_status = '" + updateOrderStatus + "', order_status_last_update_by = 'admin' WHERE order_id = '" + orderObj["order_id"] + "'";
                                        mysql.Insert(update_status);
                                    }
                                }

                                //transfer invoice here
                                if (include_invoice == 1)
                                {
                                    new JobINVTransfer().TransferInv(orderObj["order_id"]);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                            DpprException exception = new DpprException()
                            {
                                file_name = "SQLAccounting + TransferSO",
                                exception = ex.Message,
                                time = DateTime.Now.ToString()
                            };
                            LocalDB.InsertException(exception);
                        }
                        
                    }

                    slog.action_identifier = Constants.Action_Transfer_SO;
                    slog.action_failure = 0;
                    slog.action_details = "Finished";
                    slog.action_failure_message = string.Empty;
                    slog.action_time = DateTime.Now.ToString();

                    DateTime endTime = DateTime.Now;
                    TimeSpan ts = endTime - startTime;
                    Console.WriteLine("Completed in: " + ts.TotalSeconds.ToString("F"));

                    LocalDB.InsertSyncLog(slog);

                    logger.message = "Transfer SO finished in (" + ts.TotalSeconds.ToString("F") + " seconds)";
                    logger.Broadcast();
                ENDJOB:
                    Console.WriteLine("ENDJOB");
                });

                thread.Start();
                if (socket_OrderId != string.Empty)
                {
                    thread.Join();
                }
            }
            catch (ThreadAbortException e)
            {
                DpprException ex = new DpprException
                {
                    file_name = "JobTransferSO",
                    exception = e.Message,
                    time = DateTime.Now.ToString()
                };
                LocalDB.InsertException(ex);

                Console.WriteLine(Constants.Thread_Exception + e.Message);
            }
        }
    }
}