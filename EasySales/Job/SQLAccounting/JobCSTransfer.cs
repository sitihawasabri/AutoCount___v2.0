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
using Newtonsoft.Json;

namespace EasySales.Job
{
    [DisallowConcurrentExecution]
    public class JobCSTransfer : IJob
    {
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

                    /**
                     * Here we will run SQLAccounting Codes
                     * */
                    DpprSyncLog slog = new DpprSyncLog();
                    slog.action_identifier = Constants.Action_Transfer_CS;
                    slog.action_details = "Starting";
                    slog.action_failure = 0;
                    slog.action_failure_message = string.Empty;
                    slog.action_time = DateTime.Now.ToString();

                    DateTime startTime = DateTime.Now;

                    //List<DpprSyncLog> list = LocalDB.checkJobRunning();
                    //if (list.Count > 0)
                    //{
                    //    DpprSyncLog value = list[0];
                    //    if (value.action_details == "Starting")
                    //    {
                    //        logger.message = "SQLACC Transfer Cash Sales is already running";
                    //        logger.Broadcast();
                    //        goto ENDJOB;
                    //    }
                    //}

                    LocalDB.InsertSyncLog(slog);
                    logger.message = "----------------------------------------------------------------------------";
                    logger.Broadcast();
                    logger.message = "Transfer Cash Sales is running";
                    logger.Broadcast();
                    logger.message = "----------------------------------------------------------------------------";
                    logger.Broadcast();

                    dynamic hasUdf = new CheckBackendRule().CheckTablesExist().GetSettingByTableName("transfer_cashsales");
                    ArrayList functionList = new ArrayList();

                CHECKAGAIN:

                    SQLAccApi instance = SQLAccApi.getInstance();

                    dynamic ComServer = instance.GetComServer();

                    if (!instance.RPCAvailable())
                    {
                        goto CHECKAGAIN;
                    }

                    Database mysql = new Database();

                    dynamic BizObject, lMainDataSet, lDetailDataSet, lRptVar;
                    string order_validity_query, order_status_query, disc_query, udf_query, item_validity_query, pickpack_query, check_bal_query;

                    int sql_multi_discount = 0;
                    int sql_template_package = 0;
                    int check_min_price = 0;
                    int order_status = 1;
                    int pickpack_link = 0;
                    int check_item_bal = 0;
                    int include_no_stock_so = 0;
                    int tax_include = 0;
                    int include_ext_no = 0;
                    int include_currency = 0;
                    int sync_stock_card = 0;
                    int sync_wh_stock = 0;

                    string location = string.Empty;
                    string transferable = string.Empty;
                    string tax_type = string.Empty;
                    string tax_rate = string.Empty;
                    string payment_method = string.Empty;
                    Dictionary<string, string> ItemTax = new Dictionary<string, string>();
                    string SST = getSST();

                    Dictionary<string, string> paymentMethodList = new Dictionary<string, string>();
                    Dictionary<string, string> finalPaymentMethodList = new Dictionary<string, string>();

                    if (hasUdf.Count > 0)
                    {
                        foreach (var condition in hasUdf)
                        {
                            dynamic _condition = condition.condition;

                            dynamic _check_min_price = _condition.check_min_price;
                            if (_check_min_price != 0)
                            {
                                check_min_price = _check_min_price;
                            }

                            dynamic _sql_multi_discount = _condition.sql_multi_discount;
                            if (_sql_multi_discount != 0)
                            {
                                sql_multi_discount = _sql_multi_discount;
                            }
                            dynamic _sql_template_package = _condition.sql_template_package;
                            if (_sql_template_package != 0)
                            {
                                sql_template_package = _sql_template_package;
                            }
                            dynamic _order_status = _condition.order_status;
                            if (_order_status != 1)
                            {
                                order_status = _order_status;
                            }

                            dynamic _check_item_bal = _condition.check_item_bal;
                            if (_check_item_bal != 0)
                            {
                                check_item_bal = _check_item_bal;
                            }

                            dynamic _pickpack_link = _condition.pickpack_link;
                            if (_pickpack_link != 0)
                            {
                                pickpack_link = _pickpack_link;
                            }

                            dynamic _include_no_stock_so = _condition.include_no_stock_so;
                            if (_include_no_stock_so != 0)
                            {
                                include_no_stock_so = _include_no_stock_so;
                            }

                            dynamic _location = _condition.location;
                            if (_location != string.Empty)
                            {
                                location = _location;
                            }

                            dynamic _transferable = _condition.transferable;
                            if (_transferable != string.Empty)
                            {
                                transferable = _transferable;
                            }
                            
                            dynamic _payment_method = _condition.payment_method;
                            if (_payment_method != null)
                            {
                                if (_payment_method.Count > 0)
                                {
                                    foreach(var item in _payment_method)
                                    {
                                        paymentMethodList = item.ToObject<Dictionary<string, string>>();
                                        finalPaymentMethodList = finalPaymentMethodList.Union(paymentMethodList).ToDictionary(k => k.Key, v => v.Value);
                                    }
                                }                            
                            }
                            
                            dynamic _tax_include = _condition.tax_include;
                            if (_tax_include != 0)
                            {
                                tax_include = _tax_include;
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

                            dynamic _include_ext_no = _condition.include_ext_no;
                            if (_include_ext_no != null)
                            {
                                if (_include_ext_no != string.Empty)
                                {
                                    include_ext_no = _include_ext_no;
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
                            
                            dynamic _sync_stock_card = _condition.sync_stock_card;
                            if (_sync_stock_card != null)
                            {
                                if (_sync_stock_card != 0)
                                {
                                    sync_stock_card = _sync_stock_card;
                                }
                            }

                            dynamic _sync_wh_stock = _condition.sync_wh_stock;
                            if (_sync_wh_stock != null)
                            {
                                if (_sync_wh_stock != 0)
                                {
                                    sync_wh_stock = _sync_wh_stock;
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

                    order_status_query = " o.order_status = 1 ";
                    if (order_status != 1)
                    {
                        order_status_query = " o.order_status = " + order_status + "";
                    }

                    string currency_query = "";
                    if (include_currency != 0)
                    {
                        currency_query = "  c.currency, c.currency_rate, ";
                    }

                    string orderQuery = "SELECT o.order_udf,o.order_reference,l.staff_code,o.salesperson_id, o.cust_id, o.order_id, o.cust_code, " + currency_query + "o.order_date, o.cust_company_name, o.cust_incharge_person, o.cust_tel, o.cust_fax, o.billing_address1, o.billing_address2, o.billing_address3, o.billing_address4, o.termcode, o.shipping_address1, o.shipping_address2, o.shipping_address3, o.shipping_address4, o.billing_state, o.warehouse_code, o.gst_amount, o.gst_tax_amount, o.delivery_date, o.order_delivery_note, (SELECT SUM(unit_price * quantity) FROM cms_order_item WHERE order_id = o.order_id AND cancel_status = 0) AS grand_total, (SELECT GROUP_CONCAT(upload_image SEPARATOR ',') FROM cms_salesperson_uploads WHERE upload_bind_id = o.order_id GROUP BY upload_bind_id) AS image FROM cms_order AS o LEFT JOIN cms_customer AS c ON o.cust_id = c.cust_id LEFT JOIN cms_login AS l ON o.salesperson_id = l.login_id WHERE cancel_status = 0 AND doc_type = 'cash' AND " + order_status_query + order_validity_query + "";

                    ArrayList orders = mysql.Select(orderQuery);

                    logger.Broadcast(orderQuery);
                    logger.Broadcast("Cash sales to transfer: " + orders.Count);

                    int roundingCount = 0;
                    
                    if (orders.Count == 0)
                    {
                        logger.message = "No cash sales to insert";
                        logger.Broadcast();
                    }
                    else
                    {
                        int postCount = 0;
                        string orderId = string.Empty;
                        BizObject = ComServer.BizObjects.Find("SL_CS");

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

                            ArrayList findCustomerSalesperson = mysql.Select("SELECT cms_login.login_id, cms_login.staff_code, cms_customer.cust_code,cms_customer.cust_company_name FROM cms_customer_salesperson LEFT JOIN cms_login ON cms_login.login_id = cms_customer_salesperson.salesperson_id LEFT JOIN cms_customer ON cms_customer_salesperson.customer_id = cms_customer.cust_id WHERE cust_code = '" + custCode + "'");
                            if (findCustomerSalesperson.Count > 0)
                            {
                                Dictionary<string, string> custSalesperson = (Dictionary<string, string>)findCustomerSalesperson[0];
                                agentCode = custSalesperson["staff_code"];
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

                            orderId = orderObj["order_id"];
                            lMainDataSet.FindField("DocKey").value = -1;
                            if (include_ext_no == 1)
                            {
                                lMainDataSet.FindField("DocNo").value = "<<New>>";
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
                            lMainDataSet.FindField("Agent").value = agentCode;                              //orderObj["staff_code"];
                            lMainDataSet.FindField("Project").value = "----";
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
                            lMainDataSet.FindField("Description").value = "Cash Order";
                            lMainDataSet.FindField("Cancelled").value = "F";
                            lMainDataSet.FindField("D_Amount").value = "0";
                            lMainDataSet.FindField("BranchName").value = branch_name;
                            lMainDataSet.FindField("DOCREF1").value = branch_name;
                            lMainDataSet.FindField("DAddress1").value = orderObj["shipping_address1"];
                            lMainDataSet.FindField("DAddress2").value = orderObj["shipping_address2"];
                            lMainDataSet.FindField("DAddress3").value = orderObj["shipping_address3"];
                            lMainDataSet.FindField("DAddress4").value = orderObj["shipping_address4"];
                            lMainDataSet.FindField("DAttention").value = "-";
                            lMainDataSet.FindField("DPhone1").value = "-";
                            lMainDataSet.FindField("DFax1").value = "-";
                            lMainDataSet.FindField("Transferable").value = transferable;   //based on backend rules
                            lMainDataSet.FindField("PrintCount").value = "0";
                            lMainDataSet.FindField("CHANGED").AsString = "F";

                            string image_url = orderObj["image"] != string.Empty ? orderObj["image"] : string.Empty;
                            string note = orderObj["order_delivery_note"] + "\n\n\n" + image_url; 
                            mysql.Message("CS Note & Image:" + note);
                            lMainDataSet.FindField("NOTE").AsString = note;

                            string tax_query = string.Empty;
                            if(tax_include == 1)
                            {
                                tax_query = "  p.sst_code, p.sst_amount, ";
                            }
                            //string orderItemQuery = "SELECT " + disc_query + " p.product_remark, oi.product_code, oi.parent_code, oi.product_name, oi.quantity, oi.unit_uom, oi.unit_price, oi.sub_total, oi.salesperson_remark, oi.packed_qty, p.product_id, up.product_uom_rate AS uom_rate " + udf_query + " FROM cms_order_item AS oi LEFT JOIN cms_product p ON p.product_code = oi.product_code LEFT JOIN cms_product_uom_price_v2 up ON up.product_code = p.product_code AND up.product_uom = oi.unit_uom WHERE oi.cancel_status = 0 AND oi.order_id = '" + orderObj["order_id"] + "' " + item_validity_query + " " + pickpack_query + " ORDER BY oi.order_item_id DESC";
                            string orderItemQuery = "SELECT oi.product_code,oi.product_name,oi.quantity,oi.unit_uom,oi.unit_price,oi.sub_total,oi.salesperson_remark,p.product_id, "+ tax_query +" up.product_uom_rate AS uom_rate FROM cms_order_item AS oi LEFT JOIN cms_product p ON p.product_code = oi.product_code LEFT JOIN cms_product_uom_price_v2 up ON up.product_code = p.product_code AND up.product_uom = oi.unit_uom WHERE oi.cancel_status = 0 AND oi.order_id = '" + orderObj["order_id"] + "' AND oi.isParent = 0 AND oi.order_item_validity = 2 ORDER BY oi.order_item_id ASC";

                            ArrayList orderItems = mysql.Select(orderItemQuery);
                            ArrayList onlyItemToSync = new ArrayList();

                            string itemCodeStr = string.Empty;
                            ArrayList cloudQtyQueryList = new ArrayList();

                            for (int idx = 0; idx < orderItems.Count; idx++)
                            {
                                string uomrate, qty, sub_total, discount, del_date;
                                int sqty;
                                int sequence_no = 0;

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
                                lDetailDataSet.FindField("Seq").value = sequence_no;

                                try
                                {
                                    lDetailDataSet.FindField("ItemCode").value = item["product_code"];
                                    itemCode = item["product_code"];
                                    onlyItemToSync.Add(itemCode);


                                    string whCode = orderObj["warehouse_code"];
                                    string cloudQtyQuery = "SELECT * FROM cms_warehouse_stock WHERE product_code = '" + itemCode + "' AND wh_code = '" + whCode + "'";
                                    ArrayList checkCloudQty = mysql.Select(cloudQtyQuery);
                                    mysql.Message("cloudQtyQuery: " + cloudQtyQuery);

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
                                        string updateWhStock = "UPDATE cms_warehouse_stock SET cloud_qty = cloud_qty - " + qty + " WHERE product_code = '" + itemCode + "' AND wh_code = '" + whCode + "'";
                                        cloudQtyQueryList.Add(updateWhStock);
                                        mysql.Message("updateWhStock: " + updateWhStock + " [ minus packedQtyNo: " + qty + "] ");
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

                                if(tax_include == 1)
                                {
                                    if (ItemTax.Count > 0) //pluto
                                    {
                                        string taxtype = string.Empty;
                                        string taxrate = string.Empty;

                                        string _taxtype = string.Empty;
                                        string _taxrate = string.Empty;
                                        for (int itax = 0; itax < ItemTax.Count; itax++)
                                        {
                                            _taxtype = ItemTax.ElementAt(itax).Key;
                                            _taxrate = ItemTax.ElementAt(itax).Value;

                                            //if (_taxrate == SST)
                                            //{
                                                taxtype = _taxtype;
                                                taxrate = _taxrate;
                                            //    break;
                                            //}
                                        }

                                        string sstCode, sstAmt;
                                        sstCode = item["sst_code"];
                                        sstAmt = item["sst_amount"];

                                        if (sstCode != string.Empty)
                                        {
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

                                            sub_total = item["sub_total"];
                                            double.TryParse(sub_total, out double _subtotal);
                                            double _taxAmt = 0.00;
                                            _subtotal = _subtotal + _taxAmt;
                                            sub_total = _subtotal.ToString();

                                            lDetailDataSet.FindField("Amount").value = sub_total;
                                            lDetailDataSet.FindField("LocalAmount").value = sub_total;
                                        }
                                    }
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


                                lDetailDataSet.FindField("Printable").value = "T";
                                lDetailDataSet.FindField("Transferable").value = transferable;

                                lDetailDataSet.Post();
                                roundingCount = sequence_no + 1;
                            }

                            total = _total;
                            string gstTaxAmount = orderObj["gst_tax_amount"];
                            if (gstTaxAmount != "0")
                            {
                                string _grandTotal = orderObj["gst_amount"];
                                Double.TryParse(_grandTotal, out double grandTotal);
                                double difference = grandTotal - total;

                                if(difference != 0)
                                {
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

                            if (total > 0)
                            {
                                string cust_area = orderObj["billing_state"];
                                string payment_into = string.Empty;
                                if(finalPaymentMethodList.Count > 1)
                                {
                                    if (finalPaymentMethodList.ContainsKey(cust_area))
                                    {
                                        payment_into = finalPaymentMethodList.Where(pair => pair.Key == cust_area)
                                                            .Select(pair => pair.Value)
                                                            .FirstOrDefault();
                                        Console.WriteLine(payment_into);
                                        lMainDataSet.FindField("P_PAYMENTMETHOD").AsString = payment_into;
                                    }
                                    else
                                    {
                                        mysql.Insert("UPDATE cms_order SET order_fault = '0', order_fault_message = 'Payment Method is not correct for this customer. Kindly add the correct method in transfer_cashsales backend rule.' WHERE order_id = '" + orderObj["order_id"] + "'");
                                        logger.Broadcast("[" + orderObj["order_id"] + "] Payment Method is not correct for this customer.");
                                        logger.Broadcast("[" + orderObj["order_id"] + "] Kindly add the correct method in transfer_cashsales backend rule.");
                                        goto nextOrder;
                                    }
                                }
                                else
                                {
                                    if (finalPaymentMethodList.Count > 0)
                                    {
                                        payment_into = finalPaymentMethodList.ElementAt(0).Value;
                                        lMainDataSet.FindField("P_PAYMENTMETHOD").AsString = payment_into;
                                        Console.WriteLine(payment_into);
                                    }
                                }
                                lMainDataSet.FindField("P_CHEQUENUMBER").value = "";
                                lMainDataSet.FindField("P_AMOUNT").value = total;
                            }
                            try
                            {
                                lMainDataSet.Post();
                            }
                            catch (Exception exx)
                            {
                                Console.WriteLine(exx.Message);
                            }

                            if (fault == 0)
                            {
                                try
                                {
                                    BizObject.Save();

                                    if (cloudQtyQueryList.Count > 0)
                                    {
                                        for (int ixx = 0; ixx < cloudQtyQueryList.Count; ixx++)
                                        {
                                            string query = cloudQtyQueryList[ixx].ToString();
                                            mysql.Insert(query); //deducting clouq qty
                                        }
                                        cloudQtyQueryList.Clear();
                                    }

                                    if (sync_wh_stock == 1)
                                    {
                                        new JobWhStockSync().ExecuteOnlyItem(onlyItemToSync);
                                    }

                                    postCount++;

                                    int updateOrderStatus = order_status + 1;
                                    if (include_no_stock_so == 0)
                                    {
                                        mysql.Insert("INSERT INTO cms_order (order_id, order_status) VALUES ('" + orderObj["order_id"] + "','" + updateOrderStatus + "') ON DUPLICATE KEY UPDATE order_status = VALUES(order_status)");
                                    }
                                    logger.message = orderObj["order_id"] + " created";
                                    logger.Broadcast();

                                    if (sync_stock_card == 1)
                                    {
                                        string fieldToGet = include_ext_no == 1 ? "DOCNOEX = '" + orderObj["order_id"] + "'" : "DOCNO = '" + orderObj["order_id"] + "'";
                                        string stockCardQuery = "SELECT ST_TR.*, SL_CSDTL.UOM FROM ST_TR LEFT JOIN SL_CSDTL ON(ST_TR.DTLKEY = SL_CSDTL.DTLKEY) WHERE ST_TR.DOCTYPE = 'CS' AND ST_TR.DOCKEY = (SELECT DOCKEY FROM SL_CS WHERE " + fieldToGet + ")";
                                        Console.WriteLine("stockCardQuery: " + stockCardQuery);

                                        string insertQuery = "INSERT INTO cms_stock_card(stock_dtl_key, product_code, location, unit_uom, doc_date, doc_type, doc_no, doc_key, dtl_key, quantity, unit_price, refer_to) VALUES ";
                                        string insertUpdateQuery = " ON DUPLICATE KEY UPDATE product_code = VALUES(product_code), location = VALUES(location), unit_uom = VALUES(unit_uom),doc_date = VALUES(doc_date), doc_type = VALUES(doc_type), doc_no = VALUES(doc_no), doc_key = VALUES(doc_key), dtl_key = VALUES(dtl_key), quantity = VALUES(quantity), unit_price = VALUES(unit_price), refer_to = VALUES(refer_to) ";

                                        dynamic lStockCardDt;
                                        string TRANSNO, ITEMCODE, LOCATION, DEFUOM_ST, POSTDATE, DOCTYPE, DOCNO, DOCKEY, DTLKEY, QTY, PRICE, REFTO;
                                        int RecordCount = 0;
                                        HashSet<string> queryList = new HashSet<string>();

                                        try
                                        {
                                            lStockCardDt = ComServer.DBManager.NewDataSet(stockCardQuery);
                                            lStockCardDt.First();

                                            while (!lStockCardDt.eof)
                                            {
                                                RecordCount++;
                                                TRANSNO = lStockCardDt.FindField("TRANSNO").AsString;
                                                ITEMCODE = lStockCardDt.FindField("ITEMCODE").AsString;
                                                LOCATION = lStockCardDt.FindField("LOCATION").AsString;
                                                DEFUOM_ST = lStockCardDt.FindField("UOM").AsString;
                                                POSTDATE = lStockCardDt.FindField("POSTDATE").AsString;
                                                POSTDATE = Convert.ToDateTime(POSTDATE).ToString("yyyy-MM-dd");
                                                DOCTYPE = lStockCardDt.FindField("DOCTYPE").AsString;
                                                DOCNO = lStockCardDt.FindField("DOCNO").AsString;
                                                DOCKEY = lStockCardDt.FindField("DOCKEY").AsString;
                                                DTLKEY = lStockCardDt.FindField("DTLKEY").AsString;
                                                QTY = lStockCardDt.FindField("QTY").AsString;
                                                PRICE = lStockCardDt.FindField("PRICE").AsString;
                                                REFTO = lStockCardDt.FindField("REFTO").AsString;

                                                Database.Sanitize(ref TRANSNO);
                                                Database.Sanitize(ref ITEMCODE);
                                                Database.Sanitize(ref LOCATION);
                                                Database.Sanitize(ref DEFUOM_ST);
                                                Database.Sanitize(ref POSTDATE);
                                                Database.Sanitize(ref DOCTYPE);
                                                Database.Sanitize(ref DOCNO);
                                                Database.Sanitize(ref DOCKEY);
                                                Database.Sanitize(ref DTLKEY);
                                                Database.Sanitize(ref QTY);
                                                Database.Sanitize(ref PRICE);
                                                Database.Sanitize(ref REFTO);

                                                string Values = string.Format("('{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}','{8}','{9}','{10}','{11}')", TRANSNO, ITEMCODE, LOCATION, DEFUOM_ST, POSTDATE, DOCTYPE, DOCNO, DOCKEY, DTLKEY, QTY, PRICE, REFTO);

                                                queryList.Add(Values);

                                                lStockCardDt.Next();
                                            }

                                            if (queryList.Count > 0)
                                            {
                                                string tmp_query = insertQuery;
                                                tmp_query += string.Join(", ", queryList);
                                                tmp_query += insertUpdateQuery;

                                                mysql.Insert(tmp_query);

                                                queryList.Clear();
                                                tmp_query = string.Empty;

                                                logger.message = string.Format("[Cash Sales] : {0} stock card records is inserted", RecordCount);
                                                logger.Broadcast();
                                            }

                                            new JobStockCardSync().ExecuteSyncTodayOnly("1");
                                        }
                                        catch (Exception exx)
                                        {
                                            try
                                            {
                                                Console.WriteLine(exx.Message);
                                                goto CHECKAGAIN;
                                            }
                                            catch (Exception exc)
                                            {
                                                DpprException exception = new DpprException()
                                                {
                                                    file_name = "SQLAccounting + Transfer CS (Stock Card)",
                                                    exception = exc.Message,
                                                    time = DateTime.Now.ToString()
                                                };
                                                LocalDB.InsertException(exception);
                                            }
                                        }
                                    }
                                }
                                catch (Exception e)
                                {
                                    logger.Broadcast("[" + orderObj["order_id"] + "] " + e.Message);
                                    if (result.Count > 0)
                                    {
                                        if (e.Message.IndexOf("duplicate") != -1)
                                        {
                                            mysql.Insert("UPDATE cms_order SET order_fault = '0', order_fault_message = 'Order ID duplicated' WHERE order_id = '" + orderObj["order_id"] + "'");
                                            logger.Broadcast("[" + orderObj["order_id"] + "] Order ID duplicated.");
                                            //if(include_ext_no == 1)
                                            //{
                                            //    mysql.Insert("UPDATE cms_order SET order_fault = '0', order_fault_message = '" + e.Message + "' WHERE order_id = '" + orderObj["order_id"] + "'");
                                            //}
                                            //else
                                            //{
                                            //    mysql.Insert("UPDATE cms_order SET order_fault = '0', order_fault_message = '" + e.Message + "' WHERE order_id = '" + orderObj["order_id"] + "'");
                                            //    int updateOrderStatus = order_status + 1;
                                            //    if (include_no_stock_so == 0)
                                            //    {
                                            //        mysql.Insert("INSERT INTO cms_order (order_id, order_status) VALUES ('" + orderObj["order_id"] + "','" + updateOrderStatus + "') ON DUPLICATE KEY UPDATE order_status = VALUES(order_status)");
                                            //    }
                                            //    logger.message = orderObj["order_id"] + " already created";
                                            //    logger.Broadcast();
                                            //}
                                        }
                                        else if (e.Message.IndexOf("limit") != -1)
                                        {
                                            mysql.Insert("UPDATE cms_order SET order_fault = '4', order_fault_message = 'Customer credit limit exceeded' WHERE order_id = '" + orderObj["order_id"] + "'");
                                            logger.Broadcast("[" + orderObj["order_id"] + "] Customer credit limit exceeded.");
                                        }
                                        else if (e.Message.IndexOf("customer") != -1)
                                        {
                                            mysql.Insert("UPDATE cms_order SET order_fault = '1', order_fault_message = 'Invalid Customer Code' WHERE order_id = '" + orderObj["order_id"] + "'");
                                            logger.Broadcast("[" + orderObj["order_id"] + "] Invalid Customer Code.");
                                        }
                                        else
                                        {
                                            mysql.Insert("UPDATE cms_order SET order_fault = '1', order_fault_message = '" + e.Message + "' WHERE order_id = '" + orderObj["order_id"] + "'");
                                            logger.Broadcast("[" + orderObj["order_id"] + "] " + e.Message + "");
                                        }
                                    }
                                }
                            }
                            BizObject.Close();

                            if (postCount > 0)
                            {
                                int updateOrderStatus = order_status + 1;
                                string updateOrderStatusQuery = "INSERT INTO cms_order (order_id, order_status) VALUES ('" + orderId + "','" + updateOrderStatus + "') ON DUPLICATE KEY UPDATE order_status = VALUES(order_status)";
                                mysql.Insert(updateOrderStatusQuery);
                                postCount = 0;
                            }
                        nextOrder:
                            logger.Broadcast("Next Order");
                        }
                    }

                    ENDJOB:

                    slog.action_identifier = Constants.Action_Transfer_CS;
                    slog.action_failure = 0;
                    slog.action_details = "Finished";
                    slog.action_failure_message = string.Empty;
                    slog.action_time = DateTime.Now.ToString();

                    DateTime endTime = DateTime.Now;
                    TimeSpan ts = endTime - startTime;
                    Console.WriteLine("Completed in: " + ts.TotalSeconds.ToString("F"));

                    LocalDB.InsertSyncLog(slog);

                    logger.message = "Transfer CS finished in (" + ts.TotalSeconds.ToString("F") + " seconds)";
                    logger.Broadcast();

                });

                thread.Start();
            }
            catch (ThreadAbortException e)
            {
                DpprException ex = new DpprException
                {
                    file_name = "JobTransferCS",
                    exception = e.Message,
                    time = DateTime.Now.ToString()
                };
                LocalDB.InsertException(ex);

                Console.WriteLine(Constants.Thread_Exception + e.Message);
            }
        }
    }
}