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

namespace EasySales.Job
{
    [DisallowConcurrentExecution]
    public class JobINVTransfer : IJob
    {
        public string invIdToTransfer = string.Empty;

        public void TransferInv(string invId)
        {
            this.invIdToTransfer = invId;
            Execute();
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
                    slog.action_identifier = Constants.Action_Transfer_INV;
                    slog.action_failure = 0;
                    slog.action_failure_message = string.Empty;
                    slog.action_time = DateTime.Now.ToString();

                    DateTime startTime = DateTime.Now;
                    logger.message = "----------------------------------------------------------------------------";
                    logger.Broadcast();
                    logger.message = "Transfer Invoice is running";
                    logger.Broadcast();
                    logger.message = "----------------------------------------------------------------------------";
                    logger.Broadcast();

                    dynamic hasUdf = new CheckBackendRule().CheckTablesExist().GetSettingByTableName("transfer_inv");
                    ArrayList functionList = new ArrayList();

                CHECKAGAIN:

                    SQLAccApi instance = SQLAccApi.getInstance();

                    dynamic ComServer = instance.GetComServer();

                    if (!instance.RPCAvailable())
                    {
                        goto CHECKAGAIN;
                    }

                    Database mysql = new Database();

                    dynamic BizObject, lMainDataSet, lDetailDataSet;
                    string order_validity_query, order_status_query;

                    dynamic BizObjectDO, lMainDO, lDetailDO;
                    string dockey = string.Empty;
                    string dtlkey = string.Empty;

                    int sql_multi_discount = 0;
                    int sql_template_package = 0;
                    int check_min_price = 0;
                    int order_status = 1;
                    int pickpack_link = 0;
                    int check_item_bal = 0;
                    int include_no_stock_so = 0;
                    int include_ext_no = 0;
                    int tax_include = 0;
                    int include_currency = 0;
                    int sync_stock_card = 0;
                    int sync_wh_stock = 0;

                    string location = string.Empty;
                    string transferable = string.Empty;
                    string tax_type = string.Empty;
                    string tax_rate = string.Empty;
                    string payment_method = string.Empty;
                    string do_to_inv = "0";
                    string converted_status = "1";
                    string show_disc_in = "%";

                    Dictionary<string, string> ItemTax = new Dictionary<string, string>();
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

                            dynamic _payment_method = _condition.payment_method;
                            if(_payment_method != null)
                            {
                                if (_payment_method != string.Empty)
                                {
                                    payment_method = _payment_method;
                                }
                            }
                            
                            dynamic _do_to_inv = _condition.do_to_inv;
                            if(_do_to_inv != null)
                            {
                                if (_do_to_inv != "0")
                                {
                                    do_to_inv = _do_to_inv;
                                }
                            }
                            
                            dynamic _converted_status = _condition.converted_status;
                            if(_converted_status != null)
                            {
                                if (_converted_status != "1")
                                {
                                    converted_status = _converted_status;
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

                            dynamic _tax_include = _condition.tax_include;
                            if (_tax_include != null)
                            {
                                if (_tax_include != 0)
                                {
                                    tax_include = _tax_include;
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
                            
                            dynamic _show_disc_in = _condition.show_disc_in;
                            if (_show_disc_in != null)
                            {
                                if (_show_disc_in != string.Empty)
                                {
                                    show_disc_in = _show_disc_in;
                                }
                            }
                        }
                    }
                    else
                    {
                        logger.Broadcast("Weak internet connection. Cannot retrieve orders from EasySales server.");
                        logger.Broadcast("Kindly check the internet connection");
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

                    string invQuery = "SELECT o.order_udf,o.order_reference,l.staff_code,o.salesperson_id, o.cust_id, o.order_id, o.cust_code, " + currency_query + "o.order_date, o.cust_company_name, o.cust_incharge_person, o.cust_tel, o.cust_fax, o.billing_address1, o.billing_address2, o.billing_address3, o.billing_address4, o.termcode, o.shipping_address1, o.shipping_address2, o.shipping_address3, o.shipping_address4, o.billing_state, o.delivery_date, o.grand_total, o.order_delivery_note, (SELECT GROUP_CONCAT(upload_image SEPARATOR ',') FROM cms_salesperson_uploads WHERE upload_bind_id = o.order_id GROUP BY upload_bind_id) AS image, o.gst_tax_amount, o.gst_amount, o.warehouse_code FROM cms_order AS o LEFT JOIN cms_customer AS c ON o.cust_id = c.cust_id LEFT JOIN cms_login AS l ON o.salesperson_id = l.login_id ";
                    string whereInvQuery = " WHERE o.doc_type = 'invoice' AND o.cancel_status = 0 AND " + order_status_query + order_validity_query + " AND order_fault = 0 ";
                    string whereInvToTransferQuery = " WHERE order_id = '" + invIdToTransfer + "';";
                    invQuery = invQuery + (invIdToTransfer != string.Empty ? whereInvToTransferQuery : whereInvQuery);

                    string doToInvOrderQuery = "SELECT cms_do.*, c.cust_id, c.termcode, c.cust_company_name, c.cust_incharge_person, c.cust_tel, c.cust_fax, c.billing_address1, c.billing_address2, c.billing_address3, c.billing_address4, c.termcode, c.shipping_address1, c.shipping_address2, c.shipping_address3, c.shipping_address4, c.billing_state FROM cms_do AS cms_do LEFT JOIN cms_customer AS c ON c.cust_code = cms_do.cust_code WHERE cms_do.transfer_status = 0 AND cms_do.packing_status = 1 AND cms_do.cancelled = 'F'";
                    invQuery = do_to_inv == "0" ? invQuery : doToInvOrderQuery;

                    ArrayList inv = mysql.Select(invQuery);
                    mysql.Message("Transfer INV Query: " + invQuery);

                    logger.Broadcast(invQuery);
                    logger.Broadcast("Invoices to transfer: " + inv.Count);

                    int roundingCount = 0;

                    if (inv.Count == 0)
                    {
                        logger.message = "No invoices to transfer";
                        logger.Broadcast();
                    }
                    else
                    {
                        int postCount = 0;
                        string orderId = string.Empty;
                        BizObject = ComServer.BizObjects.Find("SL_IV");

                        lMainDataSet = BizObject.DataSets.Find("MainDataSet");
                        lDetailDataSet = BizObject.DataSets.Find("cdsDocDetail");

                        for (int i = 0; i < inv.Count; i++)
                        {
                            BizObject.New();

                            string post_date, branch_name, Total;
                            double total;

                            Dictionary<string, string> invObj = (Dictionary<string, string>)inv[i];

                            string custCode = invObj["cust_code"];
                            //string agentCode = invObj["staff_code"];
                            string agentCode = do_to_inv == "0" ? invObj["staff_code"] : invObj["salesperson"];

                            ArrayList findCustomerSalesperson = mysql.Select("SELECT cms_login.login_id, cms_login.staff_code, cms_customer.cust_code,cms_customer.cust_company_name FROM cms_customer_salesperson LEFT JOIN cms_login ON cms_login.login_id = cms_customer_salesperson.salesperson_id LEFT JOIN cms_customer ON cms_customer_salesperson.customer_id = cms_customer.cust_id WHERE cust_code = '" + custCode + "'");
                            if (findCustomerSalesperson.Count > 0)
                            {
                                Dictionary<string, string> custSalesperson = (Dictionary<string, string>)findCustomerSalesperson[0];
                                agentCode = custSalesperson["staff_code"];
                            }

                            Total = do_to_inv == "0" ? invObj["grand_total"] : invObj["do_amount"];
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

                            int fault = 0;
                            if(do_to_inv == "0")
                            {
                                ArrayList orderFault = mysql.Select("SELECT order_fault FROM cms_order WHERE order_id = '" + invObj["order_id"] + "'");
                                Dictionary<string, string> getOrderFault = (Dictionary<string, string>)orderFault[0];

                                string _fault = getOrderFault["order_fault"];
                                int.TryParse(_fault, out fault);
                            }
                            
                            //post_date = Convert.ToDateTime(invObj["order_date"]).ToString("yyyy-MM-dd");
                            post_date = Convert.ToDateTime(do_to_inv == "0" ? invObj["order_date"] : invObj["do_date"]).ToString("yyyy-MM-dd");
                            logger.Broadcast("passing post_date : " + post_date);

                            string doCode = do_to_inv == "0" ? string.Empty : invObj["do_code"];
                            logger.Broadcast("passing doCode : " + doCode);
                            string convertedDoCode = doCode.Replace("DO", "IV");
                            orderId = do_to_inv == "0" ? invObj["order_id"] : convertedDoCode;
                            logger.Broadcast("passing orderId : " + orderId);
                            lMainDataSet.FindField("DocKey").value = -1;

                            if (include_ext_no == 1)
                            {
                                lMainDataSet.FindField("DocNo").value = "<<New>>";
                                if (invIdToTransfer != string.Empty)
                                {
                                    //string invId = invObj["order_reference"];
                                    //Console.WriteLine(invId);
                                    //invId = invId.Replace("SO", "INV");
                                    //Console.WriteLine("After replace: " + invId);
                                    //lMainDataSet.FindField("DocNo").value = invId;
                                    //lMainDataSet.FindField("DocNoEx").value = invIdToTransfer;
                                    lMainDataSet.FindField("DocNoEx").value = invObj["order_id"];
                                }
                                else
                                {
                                    if(do_to_inv == "0")
                                    {
                                        dynamic checkIV = ComServer.DBManager.NewDataSet("SELECT DOCNOEX FROM SL_IV WHERE DOCNOEX = '" + orderId + "'");
                                        checkIV.First();

                                        while (!checkIV.eof)
                                        {
                                            int updateOrderStatus = order_status + 1;

                                            mysql.Insert("INSERT INTO cms_order (order_id, order_status, order_remark) VALUES ('" + orderId + "','" + updateOrderStatus + "','Order already created in SQLAcc') ON DUPLICATE KEY UPDATE order_status = VALUES(order_status), order_remark = VALUES(order_remark)");

                                            logger.message = orderId + " already created in SQLAcc";
                                            logger.Broadcast();

                                            goto NextOrder;
                                        }
                                    }
                                    else
                                    {
                                        //dynamic checkDO = ComServer.DBManager.NewDataSet("SELECT DOCNOEX FROM SL_DO WHERE DOCNOEX = '" + orderId + "'");
                                        //checkDO.First();

                                        //while (!checkDO.eof)
                                        //{
                                        //    int updateOrderStatus = order_status + 1;

                                        //    mysql.Insert("INSERT INTO cms_do (do_code, transfer_status, order_fault_message) VALUES ('" + doCode + "','" + updateOrderStatus + "','Order already created in SQLAcc') ON DUPLICATE KEY UPDATE transfer_status = VALUES(transfer_status), order_fault_message = VALUES(order_fault_message)");

                                        //    logger.message = checkDO + " already created in SQLAcc";
                                        //    logger.Broadcast();

                                        //    goto NextOrder;
                                        //}
                                    }

                                    lMainDataSet.FindField("DocNoEx").value = do_to_inv == "0" ? invObj["order_id"] : doCode;
                                }
                            }
                            else
                            {
                                lMainDataSet.FindField("DocNo").value = orderId;
                            }
                            lMainDataSet.FindField("DocDate").value = post_date;
                            lMainDataSet.FindField("PostDate").value = post_date;
                            lMainDataSet.FindField("TaxDate").value = post_date;
                            lMainDataSet.FindField("Code").value = invObj["cust_code"];
                            lMainDataSet.FindField("CompanyName").value = invObj["cust_company_name"];
                            lMainDataSet.FindField("Address1").value = invObj["billing_address1"];
                            lMainDataSet.FindField("Address2").value = invObj["billing_address2"];
                            lMainDataSet.FindField("Address3").value = invObj["billing_address3"];
                            lMainDataSet.FindField("Address4").value = invObj["billing_address4"];
                            lMainDataSet.FindField("Phone1").value = invObj["cust_tel"];
                            lMainDataSet.FindField("Fax1").value = invObj["cust_fax"];
                            lMainDataSet.FindField("Attention").value = invObj["cust_incharge_person"];
                            lMainDataSet.FindField("Area").value = invObj["billing_state"];
                            lMainDataSet.FindField("Agent").value = agentCode;
                            lMainDataSet.FindField("Project").value = "----";
                            lMainDataSet.FindField("Terms").value = invObj["termcode"];

                            if (include_currency == 1 && do_to_inv == "0")
                            {
                                string currency = invObj["currency"];
                                if (currency != "RM")
                                {
                                    lMainDataSet.FindField("CurrencyCode").value = currency;
                                    lMainDataSet.FindField("CurrencyRate").value = invObj["currency_rate"];
                                }
                                else
                                {
                                    lMainDataSet.FindField("CurrencyRate").value = invObj["currency_rate"];
                                }

                            }
                            else
                            {
                                lMainDataSet.FindField("CurrencyCode").value = "----";
                                lMainDataSet.FindField("CurrencyRate").value = "1";
                            }

                            lMainDataSet.FindField("Shipper").value = "----";
                            lMainDataSet.FindField("Description").value = "INVOICE";
                            lMainDataSet.FindField("Cancelled").value = "F";
                            lMainDataSet.FindField("D_Amount").value = "0";
                            lMainDataSet.FindField("BranchName").value = branch_name;
                            lMainDataSet.FindField("DOCREF1").value = branch_name;
                            lMainDataSet.FindField("DAddress1").value = invObj["shipping_address1"];
                            lMainDataSet.FindField("DAddress2").value = invObj["shipping_address2"];
                            lMainDataSet.FindField("DAddress3").value = invObj["shipping_address3"];
                            lMainDataSet.FindField("DAddress4").value = invObj["shipping_address4"];
                            lMainDataSet.FindField("DAttention").value = "-";
                            lMainDataSet.FindField("DPhone1").value = "-";
                            lMainDataSet.FindField("DFax1").value = "-";
                            lMainDataSet.FindField("Transferable").value = transferable;   //based on backend rules
                            lMainDataSet.FindField("PrintCount").value = "0";
                            lMainDataSet.FindField("CHANGED").AsString = "F";
                            string image_url = string.Empty;
                            logger.Broadcast("GETTING IMG");
                            if (do_to_inv == "0")
                            {
                                image_url = invObj["image"] != string.Empty ? invObj["image"] : string.Empty;
                            }
                            string note = string.Empty;
                            note = do_to_inv == "0" ? (invObj["order_delivery_note"] + "\n\n\n" + image_url) : string.Empty;
                            mysql.Message("INV Note & Image:" + note);
                            lMainDataSet.FindField("Note").AsString = do_to_inv == "0" ? note : invObj["picker_note"];

                            string tax_query = string.Empty;
                            if (tax_include == 1)
                            {
                                tax_query = "  p.sst_code, p.sst_amount, ";
                            }

                            string disc_query = "";
                            if (sql_multi_discount != 0)
                            {
                                disc_query = "oi.disc_1,oi.disc_2,oi.disc_3,";
                            }
                            else
                            {
                                //follow aquatic code
                                disc_query = " oi.discount_method,oi.discount_amount,oi.disc_1,oi.disc_2,oi.disc_3, ";//"oi.discount_method,oi.discount_amount,"; //oi.disc_1 insert into discount column " oi.discount_method, oi.disc_1, ";//
                            }

                            string invItemQuery = "SELECT " + disc_query + " oi.product_code,oi.product_name,oi.quantity,oi.unit_uom,oi.unit_price,oi.sub_total,oi.salesperson_remark,p.product_id, " + tax_query + "  up.product_uom_rate AS uom_rate FROM cms_order_item AS oi LEFT JOIN cms_product p ON p.product_code = oi.product_code LEFT JOIN cms_product_uom_price_v2 up ON up.product_code = p.product_code AND up.product_uom = oi.unit_uom WHERE oi.cancel_status = 0 AND oi.order_id = '" + orderId + "' AND oi.isParent = 0 AND oi.order_item_validity = 2 ORDER BY oi.order_item_id ASC";
                            string doToInvQuery = "SELECT cms_do.ref_no AS dockey, dtl.item_code, dtl.ref_no AS dtlkey, dtl.do_code, dtl.item_name,dtl.quantity,dtl.uom,dtl.item_price,dtl.total_price,dtl.packed_by, dtl.packed_qty, dtl.location, dtl.picker_note,p.product_id FROM cms_do LEFT JOIN cms_do_details AS dtl ON cms_do.do_code = dtl.do_code LEFT JOIN cms_product p ON p.product_code = dtl.item_code LEFT JOIN cms_product_uom_price_v2 up ON up.product_code = p.product_code AND up.product_uom = dtl.uom WHERE dtl.active_status = 1 AND dtl.do_code = '" + doCode + "' AND dtl.packing_status = 1 ORDER BY dtl.id ASC";
                            invItemQuery = do_to_inv == "0" ? invItemQuery : doToInvQuery;
                            ArrayList invItems = mysql.Select(invItemQuery);
                            mysql.Message("invItemQuery:" + invItemQuery);

                            string itemCodeStr = string.Empty;
                            ArrayList cloudQtyQueryList = new ArrayList();
                            ArrayList onlyItemToSync = new ArrayList();

                            for (int idx = 0; idx < invItems.Count; idx++)
                            {
                                string uomrate, qty, sub_total, discount, del_date;
                                int sqty;
                                int sequence_no = 0;

                                string itemCode = string.Empty;

                                Dictionary<string, string> item = (Dictionary<string, string>)invItems[idx];

                                lDetailDataSet.Append();

                                del_date = Convert.ToDateTime(invObj[do_to_inv == "0"? "delivery_date": "do_date"]).ToString("yyyy-MM-dd");

                                uomrate = do_to_inv == "0" ? item["uom_rate"] : "1";
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
                                    lDetailDataSet.FindField("ItemCode").value = do_to_inv == "0" ? item["product_code"] : item["item_code"];
                                    itemCode = do_to_inv == "0" ? item["product_code"] : item["item_code"];
                                    onlyItemToSync.Add(itemCode);

                                    if (do_to_inv == "0")
                                    {
                                        //deducting cloud qty
                                        string whCode = invObj["warehouse_code"];
                                        string cloudQtyQuery = "SELECT * FROM cms_warehouse_stock WHERE product_code = '" + itemCode + "' AND wh_code = '" + whCode + "'";
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
                                            string updateWhStock = "UPDATE cms_warehouse_stock SET cloud_qty = cloud_qty - " + qty + " WHERE product_code = '" + itemCode + "' AND wh_code = '" + whCode + "'";
                                            cloudQtyQueryList.Add(updateWhStock);
                                            mysql.Message("updateWhStock: " + updateWhStock + " [ minus packedQtyNo: " + qty + "] ");
                                        }
                                    }
                                }
                                catch (Exception e)
                                {
                                    if (result.Count > 0)
                                    {
                                        Console.WriteLine(e.Message);
                                        string productCode = do_to_inv == "0" ? item["product_code"] : item["item_code"];
                                        string unitUom = do_to_inv == "0" ? item["unit_uom"] : item["uom"];

                                        Database.Sanitize(ref productCode);
                                        Database.Sanitize(ref unitUom);

                                        string updateOrderFault = string.Empty;
                                        if (do_to_inv == "0")
                                        {
                                            updateOrderFault = "UPDATE cms_order SET order_fault = '2', order_fault_message = 'Invalid Item code(" + productCode + "[" + unitUom + "])' WHERE order_id = '" + invObj["order_id"] + "'";
                                            logger.Broadcast("[" + invObj["order_id"] + "] Invalid Item code(" + productCode + "[" + unitUom + "]).");
                                        }
                                        else
                                        {
                                            updateOrderFault = "UPDATE cms_do SET order_fault = '2', order_fault_message = 'Invalid Item code(" + productCode + "[" + unitUom + "])' WHERE do_code = '" + invObj["do_code"] + "'";
                                            logger.Broadcast("[" + invObj["do_code"] + "] Invalid Item code(" + productCode + "[" + unitUom + "]).");
                                        }
                                        mysql.Insert(updateOrderFault);
                                    }
                                }

                                if (location != string.Empty)
                                {
                                    lDetailDataSet.FindField("Location").value = location;
                                }
                                else
                                {
                                    //string whCode = invObj["warehouse_code"];
                                    string whCode = do_to_inv == "0" ? invObj["warehouse_code"] : item["location"];
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
                                Console.WriteLine(do_to_inv == "0" ? item["salesperson_remark"] : item["picker_note"]);
                                lDetailDataSet.FindField("REMARK1").value = do_to_inv == "0" ? item["salesperson_remark"] : item["picker_note"]; //should be remarks from cms_do?

                                discount = "0%";

                                if (sql_multi_discount == 0 && do_to_inv == "0")
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
                                        //tomauto wants in RM...
                                        //teckhong wants in %...
                                        double.TryParse(item["discount_amount"], out double discount_amount);
                                        double.TryParse(item["quantity"], out double quantity);
                                        double.TryParse(item["unit_price"], out double unit_price);
                                        double _discount = (discount_amount * 100) / (quantity * unit_price);
                                        double checkDiscInRM = (_discount / 100) * (quantity * unit_price);
                                        //add condition, if want to show in RM then only show. If not show in %
                                        if (show_disc_in == "RM")
                                        {
                                            discount = checkDiscInRM.ToString();
                                            logger.Broadcast("Disc in RM: " + discount);
                                        }
                                        else //show in %
                                        {
                                            discount = Math.Round(_discount, 3) + "%";
                                            logger.Broadcast("Disc in %: " + discount);
                                        }

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

                                    if (sql_template_package == 1)
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
                                    if(do_to_inv == "0")
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
                                }

                                lDetailDataSet.FindField("Description").value = do_to_inv == "0" ? item["product_name"] : item["item_name"];

                                try
                                {
                                    lDetailDataSet.FindField("UOM").value = do_to_inv == "0" ? item["unit_uom"] : item["uom"];
                                }
                                catch (Exception)
                                {
                                    if (result.Count > 0)
                                    {
                                        string productCode = do_to_inv == "0" ? item["product_code"] : item["item_code"];
                                        string unitUom = do_to_inv == "0" ? item["unit_uom"] : item["uom"];

                                        Database.Sanitize(ref productCode);
                                        Database.Sanitize(ref unitUom);

                                        if(do_to_inv == "0")
                                        {
                                            mysql.Insert("UPDATE cms_order SET order_fault = '2', order_fault_message = 'Invalid UOM(" + productCode + "[" + unitUom + "])' WHERE order_id = '" + invObj["order_id"] + "'");
                                            logger.Broadcast("[" + invObj["order_id"] + "] Invalid UOM(" + productCode + "[" + unitUom + "]).");
                                        }
                                        else
                                        {
                                            mysql.Insert("UPDATE cms_do SET order_fault = '2', order_fault_message = 'Invalid UOM(" + productCode + "[" + unitUom + "])' WHERE do_code = '" + invObj["do_code"] + "'");
                                            logger.Broadcast("[" + invObj["do_code"] + "] Invalid UOM(" + productCode + "[" + unitUom + "]).");
                                        }
                                       
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
                                    //string _packed_qty = item["packed_qty"];
                                    string _packed_qty = item["packed_qty"];
                                    int.TryParse(_packed_qty, out int packed_qty);

                                    //string _unitPrice = item["unit_price"];
                                    string _unitPrice = do_to_inv == "0" ? item["unit_price"] : item["item_price"];
                                    double.TryParse(_unitPrice, out double unitPrice);
                                    double subTotalPacked = unitPrice * packed_qty;

                                    lDetailDataSet.FindField("QTY").value = packed_qty;
                                    lDetailDataSet.FindField("Amount").value = subTotalPacked;
                                    lDetailDataSet.FindField("LocalAmount").value = subTotalPacked;
                                }

                                if (tax_include == 1)
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

                                //lDetailDataSet.FindField("Rate").value = item["uom_rate"];
                                lDetailDataSet.FindField("Rate").value = do_to_inv == "0" ? item["uom_rate"] : "1";
                                lDetailDataSet.FindField("SQTY").value = sqty;
                                //lDetailDataSet.FindField("UnitPrice").value = item["unit_price"];
                                lDetailDataSet.FindField("UnitPrice").value = do_to_inv == "0" ? item["unit_price"] : item["item_price"];

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

                                if(do_to_inv == "1")
                                {
                                    //fromdocno && fromdoctype && fromdtlkey
                                    string DO_Dockey = invObj["ref_no"];
                                    string DTL_Dtlkey = item["dtlkey"];
                                    lDetailDataSet.FindField("FromDocType").value = "DO";
                                    lDetailDataSet.FindField("FromDockey").value = DO_Dockey;
                                    lDetailDataSet.FindField("FromDtlkey").value = DTL_Dtlkey;
                                }

                                lDetailDataSet.Post();

                                if(do_to_inv == "1")
                                {
                                    //change 'P' to 'D' in remark2 column
                                    BizObjectDO = ComServer.BizObjects.Find("SL_DO");
                                    lMainDO = BizObjectDO.DataSets.Find("MainDataSet");
                                    lDetailDO = BizObjectDO.DataSets.Find("cdsDocDetail");

                                    dockey = item["dockey"];
                                    dtlkey = item["dtlkey"];
                                    string docno = item["do_code"];

                                    if (Convert.IsDBNull(dockey) != null)
                                    {
                                        BizObjectDO.Params.Find("Dockey").Value = dockey;
                                        BizObjectDO.Open();
                                        BizObjectDO.Edit();
                                        lMainDO.FindField("DocNo").value = docno;

                                        if (lDetailDO.Locate("DtlKey", dtlkey, false, false))
                                        {
                                            lDetailDO.Edit();
                                            lDetailDO.FindField("Remark2").AsString = "DONE INVOICING";
                                            lDetailDO.Post();
                                        }
                                        BizObjectDO.Save();
                                        BizObjectDO.Close();
                                    }
                                }
                                

                                roundingCount = sequence_no + 1;
                            }

                            total = _total;
                            if(do_to_inv == "0")
                            {
                                string gstTaxAmount = invObj["gst_tax_amount"];
                                if (gstTaxAmount != "0")
                                {
                                    string _grandTotal = invObj["gst_amount"];
                                    Double.TryParse(_grandTotal, out double grandTotal);
                                    double difference = grandTotal - total;

                                    if (difference != 0)
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
                            }

                            lMainDataSet.FindField("DocAmt").value = total;
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


                            lMainDataSet.Post();

                            if(do_to_inv == "1")
                            {
                                string doDockey = invObj["ref_no"];
                                BizObjectDO = ComServer.BizObjects.Find("SL_DO");
                                lMainDO = BizObjectDO.DataSets.Find("MainDataSet");

                                BizObjectDO.Params.Find("Dockey").Value = doDockey;
                                BizObjectDO.Open();
                                BizObjectDO.Edit();
                                lMainDO.FindField("Dockey").value = doDockey;

                                lMainDO.Edit();
                                lMainDO.FindField("Note").AsString = ""; //clear all the remark from DO note
                                lMainDO.Post();

                                BizObjectDO.Save();
                                BizObjectDO.Close();
                            }

                            if(do_to_inv == "1")
                            {
                                string noStockItem = "SELECT cms_do.ref_no AS dockey, dtl.item_code, dtl.ref_no AS dtlkey, dtl.do_code, dtl.item_name,dtl.quantity,dtl.uom,dtl.item_price,dtl.total_price,dtl.packed_by, dtl.packed_qty, dtl.location, dtl.picker_note,p.product_id FROM cms_do LEFT JOIN cms_do_details AS dtl ON cms_do.do_code = dtl.do_code LEFT JOIN cms_product p ON p.product_code = dtl.item_code LEFT JOIN cms_product_uom_price_v2 up ON up.product_code = p.product_code AND up.product_uom = dtl.uom WHERE dtl.active_status = 1 AND dtl.do_code = '" + doCode + "' AND dtl.packing_status = 2 ORDER BY dtl.id ASC";
                                ArrayList noStockItemList = mysql.Select(noStockItem);

                                for(int ixx=0; ixx < noStockItemList.Count; ixx++)
                                {
                                    //put 'N' in remark2 column for no stock
                                    BizObjectDO = ComServer.BizObjects.Find("SL_DO");
                                    lMainDO = BizObjectDO.DataSets.Find("MainDataSet");
                                    lDetailDO = BizObjectDO.DataSets.Find("cdsDocDetail");

                                    Dictionary<string, string> each = (Dictionary<string, string>)noStockItemList[ixx];

                                    dockey = each["dockey"];
                                    dtlkey = each["dtlkey"];
                                    string docno = each["do_code"];

                                    if (Convert.IsDBNull(dockey) != null)
                                    {
                                        BizObjectDO.Params.Find("Dockey").Value = dockey;
                                        BizObjectDO.Open();
                                        BizObjectDO.Edit();
                                        lMainDO.FindField("DocNo").value = docno;

                                        if (lDetailDO.Locate("DtlKey", dtlkey, false, false))
                                        {
                                            lDetailDO.Edit();
                                            lDetailDO.FindField("Remark2").AsString = "NO STOCK"; //NO STOCK ITEM
                                            lDetailDO.Post();
                                        }
                                        BizObjectDO.Save();
                                        BizObjectDO.Close();
                                    }
                                }
                            }
                            
                            if (fault == 0)
                            {
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

                                    if (sync_wh_stock == 1)
                                    {
                                        new JobWhStockSync().ExecuteOnlyItem(onlyItemToSync);
                                    }

                                    postCount++;

                                    int updateOrderStatus = order_status + 1;

                                    int failCounter = 0;
                                checkUpdateStatus:
                                    //mysql.Insert("INSERT INTO cms_order (order_id, order_status) VALUES ('" + invObj["order_id"] + "','" + updateOrderStatus + "') ON DUPLICATE KEY UPDATE order_status = VALUES(order_status)");
                                    string updatedQuery = "UPDATE cms_order SET order_status = " + updateOrderStatus + " WHERE order_id = '" + orderId + "'";
                                    string updateDOStatus = "UPDATE cms_do SET transfer_status = " + converted_status + " WHERE do_code = '" + doCode + "'";

                                    string update = do_to_inv == "0" ? updatedQuery : updateDOStatus;
                                    string transferredOrderId = do_to_inv == "0" ? invObj["order_id"] : doCode;
                                    logger.Broadcast("[INVOICE] " + transferredOrderId + " is created");
                                    //logger.Broadcast("[INVOICE] " + invObj["order_id"] + " is created");
                                    mysql.Message(update);
                                    bool updated = mysql.Insert(update);
                                    if (!updated)
                                    {
                                        Task.Delay(2000);
                                        failCounter++;
                                        if (failCounter < 4)
                                        {
                                            goto checkUpdateStatus;
                                        }
                                    }

                                    if (sync_stock_card == 1)
                                    {
                                        string fieldToGet = include_ext_no == 1 ? "DOCNOEX = '" + invObj["order_id"] + "'" : "DOCNO = '" + invObj["order_id"] + "'";
                                        string stockCardQuery = "SELECT ST_TR.*, SL_IVDTL.UOM FROM ST_TR LEFT JOIN SL_IVDTL ON(ST_TR.DTLKEY = SL_IVDTL.DTLKEY) WHERE ST_TR.DOCTYPE = 'IV' AND ST_TR.DOCKEY = (SELECT DOCKEY FROM SL_IV WHERE " + fieldToGet + ")";
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

                                                logger.message = string.Format("[Invoice] : {0} stock card records is inserted", RecordCount);
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
                                                    file_name = "SQLAccounting + Transfer INV (Stock Card)",
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
                                    logger.Broadcast("[CATCH]: " + e.Message);
                                    if (result.Count > 0)
                                    {
                                        if (e.Message.IndexOf("duplicate") != -1)
                                        {
                                            string updateOrderFaultMsg = "UPDATE cms_order SET order_fault = '0', order_fault_message = 'Order ID duplicated' WHERE order_id = '" + orderId + "'";
                                            string updateDOFaultMsg = "UPDATE cms_do SET order_fault = '0', order_fault_message = 'Order ID duplicated' WHERE do_code = '" + doCode + "'";
                                            string update = do_to_inv == "0" ? updateOrderFaultMsg : updateDOFaultMsg;
                                            mysql.Insert(updateOrderFaultMsg);
                                            if(do_to_inv == "0")
                                            {
                                                logger.Broadcast("[" + invObj["order_id"] + "] Order ID duplicated.");
                                            }
                                            else
                                            {
                                                logger.Broadcast("[" + invObj["do_code"] + "] Order ID duplicated.");
                                            }
                                        }
                                        else if (e.Message.IndexOf("limit") != -1)
                                        {
                                            string updateOrderFaultMsg = "UPDATE cms_order SET order_fault = '4', order_fault_message = 'Customer credit limit exceeded' WHERE order_id = '" + orderId + "'";
                                            string updateDOFaultMsg = "UPDATE cms_do SET order_fault = '4', order_fault_message = 'Customer credit limit exceeded' WHERE do_code = '" + doCode + "'";
                                            string update = do_to_inv == "0" ? updateOrderFaultMsg : updateDOFaultMsg;
                                            mysql.Insert(update);
                                            if (do_to_inv == "0")
                                            {
                                                logger.Broadcast("[" + invObj["order_id"] + "] Customer credit limit exceeded.");
                                            }
                                            else
                                            {
                                                logger.Broadcast("[" + invObj["do_code"] + "] Customer credit limit exceeded.");
                                            }
                                        }
                                        else
                                        {
                                            string updateOrderFaultMsg = "UPDATE cms_order SET order_fault = '1', order_fault_message = 'Invalid Customer Code || " + e.Message + "' WHERE order_id = '" + orderId + "'";
                                            string updateDOFaultMsg = "UPDATE cms_do SET order_fault = '1', order_fault_message = 'Invalid Customer Code || " + e.Message + "' WHERE do_code = '" + doCode + "'";
                                            string update = do_to_inv == "0" ? updateOrderFaultMsg : updateDOFaultMsg;
                                            mysql.Insert(update);
                                            if (do_to_inv == "0")
                                            {
                                                logger.Broadcast("[" + invObj["order_id"] + "] Invalid Customer Code || " + e.Message + ".");
                                            }
                                            else
                                            {
                                                logger.Broadcast("[" + invObj["do_code"] + "] Invalid Customer Code || " + e.Message +".");
                                            }
                                        }
                                    }
                                }
                            }
                            BizObject.Close();
                        NextOrder:
                            Console.WriteLine("Next Orders");
                        }
                    }

                    ENDJOB:

                    slog.action_identifier = Constants.Action_Transfer_INV;
                    slog.action_failure = 0;
                    slog.action_details = "Finished";
                    slog.action_failure_message = string.Empty;
                    slog.action_time = DateTime.Now.ToString();

                    DateTime endTime = DateTime.Now;
                    TimeSpan ts = endTime - startTime;
                    Console.WriteLine("Completed in: " + ts.TotalSeconds.ToString("F"));

                    LocalDB.InsertSyncLog(slog);

                    logger.message = "Transfer Sales Invoices finished in (" + ts.TotalSeconds.ToString("F") + " seconds)";
                    logger.Broadcast();

                });

                thread.Start();
            }
            catch (ThreadAbortException e)
            {
                DpprException ex = new DpprException
                {
                    file_name = "JobTransferINV",
                    exception = e.Message,
                    time = DateTime.Now.ToString()
                };
                LocalDB.InsertException(ex);

                Console.WriteLine(Constants.Thread_Exception + e.Message);
            }
        }
    }
}

//SQLACC INVOICE TRANSFER BEFORE EDIT FOR SRRI EASWARI

//using EasySales.Model;
//using EasySales.Object;
//using Quartz;
//using Quartz.Impl.Matchers;
//using System;
//using System.Collections;
//using System.Collections.Generic;
//using System.ComponentModel;
//using System.Dynamic;
//using System.Linq;
//using System.Text;
//using System.Threading;
//using System.Threading.Tasks;
//using Newtonsoft.Json.Linq;

//namespace EasySales.Job
//{
//    [DisallowConcurrentExecution]
//    public class JobINVTransfer : IJob
//    {
//        public string invIdToTransfer = string.Empty;

//        public void transferInv(string invId)
//        {
//            this.invIdToTransfer = invId;
//            Execute();
//        }
//        private string getSST()
//        {
//            Database _mysql = new Database();
//            ArrayList moduleStatus = _mysql.Select("SELECT CAST(status AS CHAR(10000) CHARACTER SET utf8) as status FROM cms_mobile_module WHERE module = 'app_sst_percent'");

//            if (moduleStatus.Count > 0)
//            {
//                Dictionary<string, string> statusList = (Dictionary<string, string>)moduleStatus[0];
//                string status = statusList["status"].ToString();
//                return status;
//            }
//            return string.Empty;
//        }

//        public void Execute()
//        {
//            this.Run();
//        }
//        public async Task Execute(IJobExecutionContext context)
//        {
//            this.Run();
//        }

//        public void Run()
//        {
//            try
//            {
//                Thread thread = new Thread(p =>
//                {
//                    Thread.CurrentThread.IsBackground = true;

//                    GlobalLogger logger = new GlobalLogger();

//                    /**
//                     * Here we will run SQLAccounting Codes
//                     * */
//                    DpprSyncLog slog = new DpprSyncLog();
//                    slog.action_identifier = Constants.Action_Transfer_INV;
//                    slog.action_failure = 0;
//                    slog.action_failure_message = string.Empty;
//                    slog.action_time = DateTime.Now.ToString();

//                    DateTime startTime = DateTime.Now;

//                    LocalDB.InsertSyncLog(slog);
//                    logger.message = "----------------------------------------------------------------------------";
//                    logger.Broadcast();
//                    logger.message = "Transfer Invoice is running";
//                    logger.Broadcast();
//                    logger.message = "----------------------------------------------------------------------------";
//                    logger.Broadcast();

//                    dynamic hasUdf = new CheckBackendRule().CheckTablesExist().GetSettingByTableName("transfer_inv");
//                    ArrayList functionList = new ArrayList();

//                CHECKAGAIN:

//                    SQLAccApi instance = SQLAccApi.getInstance();

//                    dynamic ComServer = instance.GetComServer();

//                    if (!instance.RPCAvailable())
//                    {
//                        goto CHECKAGAIN;
//                    }

//                    Database mysql = new Database();

//                    dynamic BizObject, lMainDataSet, lDetailDataSet;
//                    string order_validity_query, order_status_query;

//                    int sql_multi_discount = 0;
//                    int sql_template_package = 0;
//                    int check_min_price = 0;
//                    int order_status = 1;
//                    int pickpack_link = 0;
//                    int check_item_bal = 0;
//                    int include_no_stock_so = 0;
//                    int include_ext_no = 0;
//                    int tax_include = 0;

//                    string location = string.Empty;
//                    string transferable = string.Empty;
//                    string tax_type = string.Empty;
//                    string tax_rate = string.Empty;
//                    string payment_method = string.Empty;
//                    Dictionary<string, string> ItemTax = new Dictionary<string, string>();
//                    string SST = getSST();

//                    if (hasUdf.Count > 0)
//                    {
//                        foreach (var condition in hasUdf)
//                        {
//                            dynamic _condition = condition.condition;

//                            dynamic _check_min_price = _condition.check_min_price;
//                            if (_check_min_price != null)
//                            {
//                                if (_check_min_price != 0)
//                                {
//                                    check_min_price = _check_min_price;
//                                }
//                            }

//                            dynamic _sql_multi_discount = _condition.sql_multi_discount;
//                            if (_sql_multi_discount != null)
//                            {
//                                if (_sql_multi_discount != 0)
//                                {
//                                    sql_multi_discount = _sql_multi_discount;
//                                }
//                            }

//                            dynamic _sql_template_package = _condition.sql_template_package;
//                            if (_sql_template_package != null)
//                            {
//                                if (_sql_template_package != 0)
//                                {
//                                    sql_template_package = _sql_template_package;
//                                }
//                            }

//                            dynamic _order_status = _condition.order_status;
//                            if (_order_status != null)
//                            {
//                                if (_order_status != 1)
//                                {
//                                    order_status = _order_status;
//                                }
//                            }


//                            dynamic _check_item_bal = _condition.check_item_bal;
//                            if (_check_item_bal != null)
//                            {
//                                if (_check_item_bal != 0)
//                                {
//                                    check_item_bal = _check_item_bal;
//                                }
//                            }


//                            dynamic _pickpack_link = _condition.pickpack_link;
//                            if (_pickpack_link != null)
//                            {
//                                if (_pickpack_link != 0)
//                                {
//                                    pickpack_link = _pickpack_link;
//                                }
//                            }


//                            dynamic _include_no_stock_so = _condition.include_no_stock_so;
//                            if (_include_no_stock_so != null)
//                            {
//                                if (_include_no_stock_so != 0)
//                                {
//                                    include_no_stock_so = _include_no_stock_so;
//                                }
//                            }


//                            dynamic _location = _condition.location;
//                            if (_location != null)
//                            {
//                                if (_location != string.Empty)
//                                {
//                                    location = _location;
//                                }
//                            }


//                            dynamic _transferable = _condition.transferable;
//                            if (_transferable != null)
//                            {
//                                if (_transferable != string.Empty)
//                                {
//                                    transferable = _transferable;
//                                }
//                            }

//                            dynamic _payment_method = _condition.payment_method;
//                            if (_payment_method != null)
//                            {
//                                if (_payment_method != string.Empty)
//                                {
//                                    payment_method = _payment_method;
//                                }
//                            }


//                            dynamic _tax = _condition.tax;
//                            if (_tax != null)
//                            {
//                                foreach (var taxdtl in _tax)
//                                {
//                                    dynamic _taxtype = taxdtl.name;
//                                    if (_taxtype != string.Empty)
//                                    {
//                                        tax_type = _taxtype;
//                                    }

//                                    dynamic _taxrate = taxdtl.rate;
//                                    if (_taxrate != string.Empty)
//                                    {
//                                        tax_rate = _taxrate;
//                                    }

//                                    ItemTax.Add(tax_type, tax_rate);
//                                }
//                            }

//                            dynamic _tax_include = _condition.tax_include;
//                            if (_tax_include != null)
//                            {
//                                if (_tax_include != 0)
//                                {
//                                    tax_include = _tax_include;
//                                }

//                            }

//                            dynamic _include_ext_no = _condition.include_ext_no;
//                            if (_include_ext_no != null)
//                            {
//                                if (_include_ext_no != string.Empty)
//                                {
//                                    include_ext_no = _include_ext_no;
//                                }
//                            }
//                        }
//                    }
//                    else
//                    {
//                        goto ENDJOB;
//                    }

//                    order_validity_query = "";
//                    if (check_min_price != 0)
//                    {
//                        order_validity_query = " AND o.order_validity = 2 ";
//                    }

//                    order_status_query = " o.order_status = 1 ";
//                    if (order_status != 1)
//                    {
//                        order_status_query = " o.order_status = " + order_status + "";
//                    }

//                    string invQuery = "SELECT o.order_udf,o.order_reference,l.staff_code,o.salesperson_id, o.cust_id, o.order_id, o.cust_code, o.order_date, o.cust_company_name, o.cust_incharge_person, o.cust_tel, o.cust_fax, o.billing_address1, o.billing_address2, o.billing_address3, o.billing_address4, o.termcode, o.shipping_address1, o.shipping_address2, o.shipping_address3, o.shipping_address4, o.billing_state, o.delivery_date, o.grand_total, o.gst_tax_amount, o.gst_amount, o.warehouse_code FROM cms_order AS o LEFT JOIN cms_customer AS c ON o.cust_id = c.cust_id LEFT JOIN cms_login AS l ON o.salesperson_id = l.login_id ";
//                    string whereInvQuery = " WHERE o.doc_type = 'invoice' AND o.cancel_status = 0 AND " + order_status_query + order_validity_query + " AND order_fault = 0 ";
//                    string whereInvToTransferQuery = " WHERE order_id = '" + invIdToTransfer + "';";
//                    invQuery = invQuery + (invIdToTransfer != string.Empty ? whereInvToTransferQuery : whereInvQuery);
//                    ArrayList inv = mysql.Select(invQuery);

//                    logger.Broadcast(invQuery);
//                    logger.Broadcast("Invoices to transfer: " + inv.Count);

//                    int roundingCount = 0;

//                    if (inv.Count == 0)
//                    {
//                        logger.message = "No invoices to transfer";
//                        logger.Broadcast();
//                    }
//                    else
//                    {
//                        int postCount = 0;
//                        string orderId = string.Empty;
//                        BizObject = ComServer.BizObjects.Find("SL_IV");

//                        lMainDataSet = BizObject.DataSets.Find("MainDataSet");
//                        lDetailDataSet = BizObject.DataSets.Find("cdsDocDetail");

//                        for (int i = 0; i < inv.Count; i++)
//                        {
//                            BizObject.New();

//                            string post_date, branch_name, Total;
//                            double total;

//                            Dictionary<string, string> invObj = (Dictionary<string, string>)inv[i];

//                            string custCode = invObj["cust_code"];
//                            string agentCode = invObj["staff_code"];

//                            ArrayList findCustomerSalesperson = mysql.Select("SELECT cms_login.login_id, cms_login.staff_code, cms_customer.cust_code,cms_customer.cust_company_name FROM cms_customer_salesperson LEFT JOIN cms_login ON cms_login.login_id = cms_customer_salesperson.salesperson_id LEFT JOIN cms_customer ON cms_customer_salesperson.customer_id = cms_customer.cust_id WHERE cust_code = '" + custCode + "'");
//                            if (findCustomerSalesperson.Count > 0)
//                            {
//                                Dictionary<string, string> custSalesperson = (Dictionary<string, string>)findCustomerSalesperson[0];
//                                agentCode = custSalesperson["staff_code"];
//                            }

//                            Total = invObj["grand_total"];
//                            double.TryParse(Total, out double _total);
//                            total = _total * 1.00;

//                            string _branch_name;

//                            ArrayList branchDb = mysql.Select("SELECT branch_name FROM cms_customer_branch");

//                            for (int iX = 0; iX < branchDb.Count; iX++)
//                            {
//                                Dictionary<string, string> branchObj = (Dictionary<string, string>)branchDb[iX];
//                                _branch_name = branchObj["branch_name"];
//                            }

//                            _branch_name = "";

//                            if (_branch_name == "")
//                            {
//                                branch_name = "";
//                            }
//                            else
//                            {
//                                branch_name = _branch_name;
//                            }

//                            ArrayList result = mysql.Select("SHOW COLUMNS FROM `cms_order` LIKE 'order_fault'");

//                            ArrayList orderFault = mysql.Select("SELECT order_fault FROM cms_order WHERE order_id = '" + invObj["order_id"] + "'");
//                            Dictionary<string, string> getOrderFault = (Dictionary<string, string>)orderFault[0];

//                            string _fault = getOrderFault["order_fault"];
//                            int.TryParse(_fault, out int fault);

//                            post_date = Convert.ToDateTime(invObj["order_date"]).ToString("yyyy-MM-dd");

//                            orderId = invObj["order_id"];
//                            lMainDataSet.FindField("DocKey").value = -1;

//                            if (include_ext_no == 1)
//                            {
//                                lMainDataSet.FindField("DocNo").value = "<<New>>";
//                                if (invIdToTransfer != string.Empty)
//                                {
//                                    string invId = invObj["order_reference"];
//                                    Console.WriteLine(invId);
//                                    invId = invId.Replace("SO", "INV");
//                                    Console.WriteLine("After replace: " + invId);
//                                    lMainDataSet.FindField("DocNo").value = invId;
//                                    lMainDataSet.FindField("DocNoEx").value = invIdToTransfer;
//                                }
//                                else
//                                {
//                                    lMainDataSet.FindField("DocNoEx").value = invObj["order_id"];
//                                }
//                            }
//                            else
//                            {
//                                lMainDataSet.FindField("DocNo").value = invObj["order_id"];
//                            }

//                            lMainDataSet.FindField("DocDate").value = post_date;
//                            lMainDataSet.FindField("PostDate").value = post_date;
//                            lMainDataSet.FindField("TaxDate").value = post_date;
//                            lMainDataSet.FindField("Code").value = invObj["cust_code"];
//                            lMainDataSet.FindField("CompanyName").value = invObj["cust_company_name"];
//                            lMainDataSet.FindField("Address1").value = invObj["billing_address1"];
//                            lMainDataSet.FindField("Address2").value = invObj["billing_address2"];
//                            lMainDataSet.FindField("Address3").value = invObj["billing_address3"];
//                            lMainDataSet.FindField("Address4").value = invObj["billing_address4"];
//                            lMainDataSet.FindField("Phone1").value = invObj["cust_tel"];
//                            lMainDataSet.FindField("Fax1").value = invObj["cust_fax"];
//                            lMainDataSet.FindField("Attention").value = invObj["cust_incharge_person"];
//                            lMainDataSet.FindField("Area").value = invObj["billing_state"];
//                            lMainDataSet.FindField("Agent").value = agentCode;                              //invObj["staff_code"];
//                            lMainDataSet.FindField("Project").value = "----";
//                            lMainDataSet.FindField("Terms").value = invObj["termcode"];
//                            lMainDataSet.FindField("CurrencyCode").value = "----";
//                            lMainDataSet.FindField("CurrencyRate").value = "1";
//                            lMainDataSet.FindField("Shipper").value = "----";
//                            lMainDataSet.FindField("Description").value = "INVOICE";
//                            lMainDataSet.FindField("Cancelled").value = "F";
//                            lMainDataSet.FindField("D_Amount").value = "0";
//                            lMainDataSet.FindField("BranchName").value = branch_name;
//                            lMainDataSet.FindField("DOCREF1").value = branch_name;
//                            lMainDataSet.FindField("DAddress1").value = invObj["shipping_address1"];
//                            lMainDataSet.FindField("DAddress2").value = invObj["shipping_address2"];
//                            lMainDataSet.FindField("DAddress3").value = invObj["shipping_address3"];
//                            lMainDataSet.FindField("DAddress4").value = invObj["shipping_address4"];
//                            lMainDataSet.FindField("DAttention").value = "-";
//                            lMainDataSet.FindField("DPhone1").value = "-";
//                            lMainDataSet.FindField("DFax1").value = "-";
//                            lMainDataSet.FindField("Transferable").value = transferable;   //based on backend rules
//                            lMainDataSet.FindField("PrintCount").value = "0";
//                            lMainDataSet.FindField("CHANGED").AsString = "F";

//                            string tax_query = string.Empty;
//                            if (tax_include == 1)
//                            {
//                                tax_query = "  p.sst_code, p.sst_amount, ";
//                            }

//                            string invItemQuery = "SELECT oi.product_code,oi.product_name,oi.quantity,oi.unit_uom,oi.unit_price,oi.sub_total,oi.salesperson_remark,p.product_id, " + tax_query + "  up.product_uom_rate AS uom_rate FROM cms_order_item AS oi LEFT JOIN cms_product p ON p.product_code = oi.product_code LEFT JOIN cms_product_uom_price_v2 up ON up.product_code = p.product_code AND up.product_uom = oi.unit_uom WHERE oi.cancel_status = 0 AND oi.order_id = '" + invObj["order_id"] + "' AND oi.isParent = 0 AND oi.order_item_validity = 2 ORDER BY oi.order_item_id ASC";

//                            ArrayList invItems = mysql.Select(invItemQuery);

//                            string itemCodeStr = string.Empty;

//                            for (int idx = 0; idx < invItems.Count; idx++)
//                            {
//                                string uomrate, qty, sub_total, discount, del_date;
//                                int sqty;
//                                int sequence_no = 0;

//                                string itemCode = string.Empty;

//                                Dictionary<string, string> item = (Dictionary<string, string>)invItems[idx];

//                                lDetailDataSet.Append();

//                                del_date = Convert.ToDateTime(invObj["delivery_date"]).ToString("yyyy-MM-dd");

//                                uomrate = item["uom_rate"];
//                                qty = item["quantity"];
//                                int.TryParse(uomrate, out int Uomrate);
//                                int.TryParse(qty, out int Qty);

//                                sqty = Qty * Uomrate;

//                                sequence_no++;

//                                lDetailDataSet.FindField("DtlKey").value = -1;
//                                lDetailDataSet.FindField("DocKey").value = -1;
//                                lDetailDataSet.FindField("Seq").value = sequence_no;

//                                try
//                                {
//                                    lDetailDataSet.FindField("ItemCode").value = item["product_code"];
//                                    itemCode = item["product_code"];
//                                }
//                                catch (Exception e)
//                                {
//                                    if (result.Count > 0)
//                                    {
//                                        Console.WriteLine(e.Message);
//                                        string productCode = item["product_code"];
//                                        string unitUom = item["unit_uom"];

//                                        Database.Sanitize(ref productCode);
//                                        Database.Sanitize(ref unitUom);

//                                        mysql.Insert("UPDATE cms_order SET order_fault = '2', order_fault_message = 'Invalid Item code(" + productCode + "[" + unitUom + "])' WHERE order_id = '" + invObj["order_id"] + "'");
//                                    }
//                                }

//                                if (location != string.Empty)
//                                {
//                                    lDetailDataSet.FindField("Location").value = location;
//                                }
//                                else
//                                {
//                                    string whCode = invObj["warehouse_code"];
//                                    if (whCode != string.Empty)
//                                    {
//                                        if (whCode == "HQ")
//                                        {
//                                            lDetailDataSet.FindField("Location").value = "----";
//                                        }
//                                        else
//                                        {
//                                            lDetailDataSet.FindField("Location").value = whCode;
//                                        }
//                                    }
//                                    else
//                                    {
//                                        lDetailDataSet.FindField("Location").value = "----";
//                                    }
//                                }

//                                lDetailDataSet.FindField("Project").value = "----";
//                                lDetailDataSet.FindField("REMARK1").value = item["salesperson_remark"];

//                                discount = "0%";

//                                lDetailDataSet.FindField("Description").value = item["product_name"];

//                                try
//                                {
//                                    lDetailDataSet.FindField("UOM").value = item["unit_uom"];
//                                }
//                                catch (Exception)
//                                {
//                                    if (result.Count > 0)
//                                    {
//                                        string productCode = item["product_code"];
//                                        string unitUom = item["unit_uom"];

//                                        Database.Sanitize(ref productCode);
//                                        Database.Sanitize(ref unitUom);

//                                        mysql.Insert("UPDATE cms_order SET order_fault = '2', order_fault_message = 'Invalid UOM(" + productCode + "[" + unitUom + "])' WHERE order_id = '" + invObj["order_id"] + "'");
//                                    }
//                                }

//                                if (pickpack_link == 0)
//                                {
//                                    sub_total = item["sub_total"];

//                                    lDetailDataSet.FindField("QTY").value = qty;
//                                    lDetailDataSet.FindField("Amount").value = sub_total;
//                                    lDetailDataSet.FindField("LocalAmount").value = sub_total;
//                                }
//                                else
//                                {
//                                    //get packed_qty
//                                    string _packed_qty = item["packed_qty"];
//                                    int.TryParse(_packed_qty, out int packed_qty);

//                                    string _unitPrice = item["unit_price"];
//                                    double.TryParse(_unitPrice, out double unitPrice);
//                                    double subTotalPacked = unitPrice * packed_qty;

//                                    lDetailDataSet.FindField("QTY").value = packed_qty;
//                                    lDetailDataSet.FindField("Amount").value = subTotalPacked;
//                                    lDetailDataSet.FindField("LocalAmount").value = subTotalPacked;
//                                }

//                                if (tax_include == 1)
//                                {
//                                    if (ItemTax.Count > 0) //pluto
//                                    {
//                                        string taxtype = string.Empty;
//                                        string taxrate = string.Empty;

//                                        string _taxtype = string.Empty;
//                                        string _taxrate = string.Empty;
//                                        for (int itax = 0; itax < ItemTax.Count; itax++)
//                                        {
//                                            _taxtype = ItemTax.ElementAt(itax).Key;
//                                            _taxrate = ItemTax.ElementAt(itax).Value;

//                                            //if (_taxrate == SST)
//                                            //{
//                                            taxtype = _taxtype;
//                                            taxrate = _taxrate;
//                                            //    break;
//                                            //}
//                                        }

//                                        string sstCode, sstAmt;
//                                        sstCode = item["sst_code"];
//                                        sstAmt = item["sst_amount"];

//                                        if (sstCode != string.Empty)
//                                        {
//                                            lDetailDataSet.FindField("Tax").value = taxtype;
//                                            lDetailDataSet.FindField("TaxRate").value = taxrate + "%";

//                                            double.TryParse(taxrate, out double doubleTaxRate);
//                                            string _unitPrice = item["unit_price"];
//                                            double.TryParse(_unitPrice, out double unitPrice);

//                                            double _taxAmt = unitPrice * (doubleTaxRate / 100);
//                                            string taxAmt = _taxAmt.ToString();
//                                            lDetailDataSet.FindField("LocalTaxAmt").value = 0;
//                                            //lDetailDataSet.FindField("TaxInclusive").value = "T"; #meaning the tax is in the price already

//                                            int.TryParse(qty, out int _qty);
//                                            _taxAmt = _taxAmt * _qty;
//                                            _total = _total + _taxAmt;

//                                            sub_total = item["sub_total"];
//                                            double.TryParse(sub_total, out double _subtotal);
//                                            _subtotal = _subtotal + _taxAmt;
//                                            sub_total = _subtotal.ToString();

//                                            lDetailDataSet.FindField("Amount").value = sub_total;
//                                            lDetailDataSet.FindField("LocalAmount").value = sub_total;
//                                        }
//                                        else
//                                        {
//                                            lDetailDataSet.FindField("Tax").value = "";
//                                            lDetailDataSet.FindField("TaxRate").value = "";

//                                            sub_total = item["sub_total"];
//                                            double.TryParse(sub_total, out double _subtotal);
//                                            double _taxAmt = 0.00;
//                                            _subtotal = _subtotal + _taxAmt;
//                                            sub_total = _subtotal.ToString();

//                                            lDetailDataSet.FindField("Amount").value = sub_total;
//                                            lDetailDataSet.FindField("LocalAmount").value = sub_total;
//                                        }
//                                    }
//                                }
//                                else
//                                {
//                                    lDetailDataSet.FindField("Tax").value = "";
//                                    lDetailDataSet.FindField("TaxRate").value = "";
//                                    lDetailDataSet.FindField("TaxAmt").value = 0;
//                                    lDetailDataSet.FindField("LocalTaxAmt").value = 0;
//                                    lDetailDataSet.FindField("TaxInclusive").value = 0;
//                                }

//                                lDetailDataSet.FindField("Rate").value = item["uom_rate"];
//                                lDetailDataSet.FindField("SQTY").value = sqty;
//                                lDetailDataSet.FindField("UnitPrice").value = item["unit_price"];

//                                if (discount != "0%")
//                                {
//                                    lDetailDataSet.FindField("Disc").value = discount;
//                                }
//                                else
//                                {
//                                    lDetailDataSet.FindField("Disc").value = "";
//                                }

//                                lDetailDataSet.FindField("DeliveryDate").value = del_date;


//                                lDetailDataSet.FindField("Printable").value = "T";
//                                lDetailDataSet.FindField("Transferable").value = transferable;

//                                lDetailDataSet.Post();
//                                roundingCount = sequence_no + 1;
//                            }

//                            total = _total;
//                            string gstTaxAmount = invObj["gst_tax_amount"];
//                            if (gstTaxAmount != "0")
//                            {
//                                string _grandTotal = invObj["gst_amount"];
//                                Double.TryParse(_grandTotal, out double grandTotal);
//                                double difference = grandTotal - total;

//                                if (difference != 0)
//                                {
//                                    ArrayList rounding = mysql.Select("SELECT p.*, uom.* FROM cms_product AS p LEFT JOIN cms_product_uom_price_v2 AS uom ON p.product_code = uom.product_code WHERE p.product_code = 'RTN5Cents'");
//                                    Dictionary<string, string> roundingObj = (Dictionary<string, string>)rounding[0];
//                                    lDetailDataSet.Append();

//                                    lDetailDataSet.FindField("DtlKey").value = -1;
//                                    lDetailDataSet.FindField("DocKey").value = -1;
//                                    lDetailDataSet.FindField("Seq").value = roundingCount;
//                                    lDetailDataSet.FindField("ItemCode").value = roundingObj["product_code"];
//                                    lDetailDataSet.FindField("Location").value = "----";
//                                    lDetailDataSet.FindField("Project").value = "----";
//                                    lDetailDataSet.FindField("UOM").value = roundingObj["product_uom"];
//                                    lDetailDataSet.FindField("QTY").value = 1;
//                                    lDetailDataSet.FindField("Amount").value = difference;
//                                    lDetailDataSet.FindField("LocalAmount").value = difference;
//                                    lDetailDataSet.FindField("Tax").value = "";
//                                    lDetailDataSet.FindField("TaxRate").value = "";
//                                    lDetailDataSet.FindField("TaxAmt").value = 0;
//                                    lDetailDataSet.FindField("LocalTaxAmt").value = 0;
//                                    lDetailDataSet.FindField("TaxInclusive").value = 0;
//                                    lDetailDataSet.FindField("Rate").value = roundingObj["product_uom_rate"];
//                                    lDetailDataSet.FindField("UnitPrice").value = difference;
//                                    lDetailDataSet.FindField("Disc").value = "";
//                                    lDetailDataSet.FindField("Printable").value = "T";
//                                    lDetailDataSet.FindField("Transferable").value = "T";
//                                    lDetailDataSet.Post();
//                                    total = total + difference;
//                                }
//                            }

//                            lMainDataSet.FindField("DocAmt").value = total;
//                            lMainDataSet.FindField("LocalDocAmt").value = total;

//                            lMainDataSet.Post();

//                            if (fault == 0)
//                            {
//                                try
//                                {
//                                    BizObject.Save();

//                                    postCount++;

//                                    int updateOrderStatus = order_status + 1;

//                                    int failCounter = 0;
//                                checkUpdateStatus:
//                                    //mysql.Insert("INSERT INTO cms_order (order_id, order_status) VALUES ('" + invObj["order_id"] + "','" + updateOrderStatus + "') ON DUPLICATE KEY UPDATE order_status = VALUES(order_status)");
//                                    string updatedQuery = "UPDATE cms_order SET order_status = " + updateOrderStatus + " WHERE order_id = '" + orderId + "'";
//                                    logger.Broadcast("[INVOICE] " + orderId + " is created");
//                                    bool updated = mysql.Insert(updatedQuery);
//                                    if (!updated)
//                                    {
//                                        Task.Delay(2000);
//                                        failCounter++;
//                                        if (failCounter < 4)
//                                        {
//                                            goto checkUpdateStatus;
//                                        }
//                                    }
//                                }
//                                catch (Exception e)
//                                {
//                                    if (result.Count > 0)
//                                    {
//                                        if (e.Message.IndexOf("duplicate") != -1)
//                                        {
//                                            mysql.Insert("UPDATE cms_order SET order_fault = '0', order_fault_message = 'Order ID duplicated' WHERE order_id = '" + invObj["order_id"] + "'");
//                                            //if (include_ext_no == 1)
//                                            //{
//                                            //    mysql.Insert("UPDATE cms_order SET order_fault = '0', order_fault_message = '" + e.Message + "' WHERE order_id = '" + invObj["order_id"] + "'");
//                                            //}
//                                            //else
//                                            //{
//                                            //    mysql.Insert("UPDATE cms_order SET order_fault = '0', order_fault_message = '" + e.Message + "' WHERE order_id = '" + invObj["order_id"] + "'");
//                                            //    int updateOrderStatus = order_status + 1;
//                                            //    if (include_no_stock_so == 0)
//                                            //    {
//                                            //        mysql.Insert("INSERT INTO cms_order (order_id, order_status) VALUES ('" + invObj["order_id"] + "','" + updateOrderStatus + "') ON DUPLICATE KEY UPDATE order_status = VALUES(order_status)");
//                                            //    }
//                                            //    logger.message = invObj["order_id"] + " already created";
//                                            //    logger.Broadcast();
//                                            //}
//                                        }
//                                        else if (e.Message.IndexOf("limit") != -1)
//                                        {
//                                            mysql.Insert("UPDATE cms_order SET order_fault = '4', order_fault_message = 'Customer credit limit exceeded' WHERE order_id = '" + invObj["order_id"] + "'");
//                                        }
//                                        else
//                                        {
//                                            mysql.Insert("UPDATE cms_order SET order_fault = '1', order_fault_message = 'Invalid Customer Code || " + e.Message + "' WHERE order_id = '" + invObj["order_id"] + "'");
//                                        }
//                                    }
//                                }
//                            }
//                            BizObject.Close();

//                            if (postCount > 0)
//                            {
//                                int updateOrderStatus = order_status + 1;
//                                string updateOrderStatusQuery = "INSERT INTO cms_order (order_id, order_status) VALUES ('" + orderId + "','" + updateOrderStatus + "') ON DUPLICATE KEY UPDATE order_status = VALUES(order_status)";
//                                mysql.Insert(updateOrderStatusQuery);
//                                postCount = 0;
//                            }
//                        }
//                    }

//                ENDJOB:

//                    slog.action_identifier = Constants.Action_Transfer_INV;
//                    slog.action_failure = 0;
//                    slog.action_failure_message = string.Empty;
//                    slog.action_time = DateTime.Now.ToString();

//                    DateTime endTime = DateTime.Now;
//                    TimeSpan ts = endTime - startTime;
//                    Console.WriteLine("Completed in: " + ts.TotalSeconds.ToString("F"));

//                    LocalDB.InsertSyncLog(slog);

//                    logger.message = "Transfer Sales Invoices finished in (" + ts.TotalSeconds.ToString("F") + " seconds)";
//                    logger.Broadcast();

//                });

//                thread.Start();
//            }
//            catch (ThreadAbortException e)
//            {
//                DpprException ex = new DpprException
//                {
//                    file_name = "JobTransferINV",
//                    exception = e.Message,
//                    time = DateTime.Now.ToString()
//                };
//                LocalDB.InsertException(ex);

//                Console.WriteLine(Constants.Thread_Exception + e.Message);
//            }
//        }
//    }
//}