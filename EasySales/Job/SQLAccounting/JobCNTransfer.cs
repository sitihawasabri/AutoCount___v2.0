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
    public class JobCNTransfer : IJob
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

                    /**
                     * Here we will run SQLAccounting Codes
                     * */
                    DpprSyncLog slog = new DpprSyncLog();
                    slog.action_identifier = Constants.Action_Transfer_CN;
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
                    //        logger.message = "SQLACC Transfer Credit Note is already running";
                    //        logger.Broadcast();
                    //        goto ENDJOB;
                    //    }
                    //}

                    LocalDB.InsertSyncLog(slog);
                    logger.message = "----------------------------------------------------------------------------";
                    logger.Broadcast();
                    logger.message = "Transfer Credit Note is running";
                    logger.Broadcast();
                    logger.message = "----------------------------------------------------------------------------";
                    logger.Broadcast();

                    dynamic hasUdf = new CheckBackendRule().CheckTablesExist().GetSettingByTableName("transfer_creditnote");
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

                    int check_min_price = 0;
                    int order_status = 1;
                    int include_currency = 0;
                    int include_ext_no = 0;
                    int sync_stock_card = 0;
                    int sync_wh_stock = 0;
                    int sql_multi_discount = 0;

                    string location = string.Empty;
                    string transferable = string.Empty;
                    string tax_type = string.Empty;
                    string tax_rate = string.Empty;
                    string payment_method = string.Empty;
                    string show_disc_in = "%";

                    if (hasUdf.Count > 0)
                    {
                        foreach (var condition in hasUdf)
                        {
                            dynamic _condition = condition.condition;

                            dynamic _order_status = _condition.order_status;
                            if (_order_status != null)
                            {
                                if (_order_status != 1)
                                {
                                    order_status = _order_status;
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

                            dynamic _sql_multi_discount = _condition.sql_multi_discount;
                            if (_sql_multi_discount != null)
                            {
                                if (_sql_multi_discount != 0)
                                {
                                    sql_multi_discount = _sql_multi_discount;
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

                    string orderQuery = "SELECT o.order_udf,o.order_reference,l.staff_code,o.salesperson_id, o.cust_id, o.order_id, o.cust_code, " + currency_query + "o.order_date, o.cust_company_name, o.cust_incharge_person, o.cust_tel, o.cust_fax, o.billing_address1, o.billing_address2, o.billing_address3, o.billing_address4, o.termcode, o.shipping_address1, o.shipping_address2, o.shipping_address3, o.shipping_address4, o.billing_state, o.warehouse_code, o.gst_amount, o.gst_tax_amount, o.delivery_date, o.order_delivery_note, (SELECT SUM(unit_price * quantity) FROM cms_order_item WHERE order_id = o.order_id AND cancel_status = 0) AS grand_total, (SELECT GROUP_CONCAT(upload_image SEPARATOR ',') FROM cms_salesperson_uploads WHERE upload_bind_id = o.order_id GROUP BY upload_bind_id) AS image FROM cms_order AS o LEFT JOIN cms_customer AS c ON o.cust_id = c.cust_id LEFT JOIN cms_login AS l ON o.salesperson_id = l.login_id WHERE cancel_status = 0 AND doc_type = 'credit' AND " + order_status_query + order_validity_query + "";

                    ArrayList orders = mysql.Select(orderQuery);
                    mysql.Message("Transfer CN Query: " + orderQuery);

                    logger.Broadcast(orderQuery);
                    logger.Broadcast("Credit note to transfer: " + orders.Count);

                    int roundingCount = 0;

                    if (orders.Count == 0)
                    {
                        logger.message = "No credit note to insert";
                        logger.Broadcast();
                    }
                    else
                    {
                        string orderId = string.Empty;
                        BizObject = ComServer.BizObjects.Find("SL_CN");

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
                                dynamic checkCN = ComServer.DBManager.NewDataSet("SELECT DOCNOEX FROM SL_CN WHERE DOCNOEX = '"+orderId+"'");
                                checkCN.First();

                                while (!checkCN.eof)
                                {
                                    int updateOrderStatus = order_status + 1;

                                    mysql.Insert("INSERT INTO cms_order (order_id, order_status, order_remark) VALUES ('" + orderObj["order_id"] + "','" + updateOrderStatus + "','Order already created in SQLAcc') ON DUPLICATE KEY UPDATE order_status = VALUES(order_status), order_remark = VALUES(order_remark)");

                                    logger.message = orderId + " already created in SQLAcc";
                                    logger.Broadcast();

                                    goto NextOrder;
                                }
                                
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
                            lMainDataSet.FindField("Description").value = "Sales Returned";
                            lMainDataSet.FindField("Cancelled").value = "F";
                            lMainDataSet.FindField("BranchName").value = branch_name;
                            lMainDataSet.FindField("DOCREF1").value = branch_name;
                            lMainDataSet.FindField("DAddress1").value = orderObj["shipping_address1"];
                            lMainDataSet.FindField("DAddress2").value = orderObj["shipping_address2"];
                            lMainDataSet.FindField("DAddress3").value = orderObj["shipping_address3"];
                            lMainDataSet.FindField("DAddress4").value = orderObj["shipping_address4"];
                            lMainDataSet.FindField("DAttention").value = "-";
                            lMainDataSet.FindField("DPhone1").value = "-";
                            lMainDataSet.FindField("DFax1").value = "-";
                            string image_url = orderObj["image"] != string.Empty ? orderObj["image"] : string.Empty;
                            string note = orderObj["order_delivery_note"] + "\n\n\n" + image_url;
                            mysql.Message("CN Note & Image:" + note);
                            lMainDataSet.FindField("NOTE").AsString = note;


                            string disc_query = "";
                            if (sql_multi_discount != 0)
                            {
                                disc_query = "oi.disc_1,oi.disc_2,oi.disc_3,";
                            }
                            else
                            {
                                disc_query = " oi.discount_method,oi.discount_amount,oi.disc_1,oi.disc_2,oi.disc_3, ";
                            }

                            string orderItemQuery = "SELECT " + disc_query + " oi.product_code,oi.product_name,oi.quantity,oi.unit_uom,oi.unit_price,oi.sub_total,oi.salesperson_remark,p.product_id, up.product_uom_rate AS uom_rate FROM cms_order_item AS oi LEFT JOIN cms_product p ON p.product_code = oi.product_code LEFT JOIN cms_product_uom_price_v2 up ON up.product_code = p.product_code AND up.product_uom = oi.unit_uom WHERE oi.cancel_status = 0 AND oi.order_id = '" + orderObj["order_id"] + "' AND oi.isParent = 0 AND oi.order_item_validity = 2 ORDER BY oi.order_item_id ASC";

                            ArrayList orderItems = mysql.Select(orderItemQuery);

                            string itemCodeStr = string.Empty;
                            ArrayList cloudQtyQueryList = new ArrayList();
                            ArrayList onlyItemToSync = new ArrayList();

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

                                    //deducting cloud qty
                                    string whCode = orderObj["warehouse_code"];
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
                                
                                sub_total = item["sub_total"];

                                lDetailDataSet.FindField("QTY").value = qty;
                                lDetailDataSet.FindField("Amount").value = sub_total;
                                lDetailDataSet.FindField("LocalAmount").value = sub_total;

                                lDetailDataSet.FindField("Tax").value = "";
                                lDetailDataSet.FindField("TaxRate").value = "";
                                lDetailDataSet.FindField("TaxAmt").value = 0;
                                lDetailDataSet.FindField("LocalTaxAmt").value = 0;
                                lDetailDataSet.FindField("TaxInclusive").value = 0;
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

                                lDetailDataSet.Post();
                                roundingCount = sequence_no + 1;
                            }

                            total = _total;
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

                                    int updateOrderStatus = order_status + 1;
                                    
                                    mysql.Insert("INSERT INTO cms_order (order_id, order_status) VALUES ('" + orderObj["order_id"] + "','" + updateOrderStatus + "') ON DUPLICATE KEY UPDATE order_status = VALUES(order_status)");
                                   
                                    logger.message = orderObj["order_id"] + " created";
                                    logger.Broadcast();

                                    if (sync_stock_card == 1)
                                    {
                                        string fieldToGet = include_ext_no == 1 ? "DOCNOEX = '" + orderObj["order_id"] + "'" : "DOCNO = '" + orderObj["order_id"] + "'";
                                        string stockCardQuery = "SELECT ST_TR.*, SL_CNDTL.UOM FROM ST_TR LEFT JOIN SL_CNDTL ON(ST_TR.DTLKEY = SL_CNDTL.DTLKEY) WHERE ST_TR.DOCTYPE = 'CN' AND ST_TR.DOCKEY = (SELECT DOCKEY FROM SL_CN WHERE " + fieldToGet + ")";
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

                                                logger.message = string.Format("[Credit Note] : {0} stock card records is inserted", RecordCount);
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
                                                    file_name = "SQLAccounting + Transfer CN (Stock Card)",
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
                                    if (result.Count > 0)
                                    {
                                        if (e.Message.IndexOf("duplicate") != -1)
                                        {
                                            mysql.Insert("UPDATE cms_order SET order_fault = '0', order_fault_message = 'Order ID duplicated' WHERE order_id = '" + orderObj["order_id"] + "'");
                                            logger.Broadcast("[" + orderObj["order_id"] + "] Order ID duplicated.");
                                        }
                                        else if (e.Message.IndexOf("limit") != -1)
                                        {
                                            mysql.Insert("UPDATE cms_order SET order_fault = '4', order_fault_message = 'Customer credit limit exceeded' WHERE order_id = '" + orderObj["order_id"] + "'");
                                            logger.Broadcast("[" + orderObj["order_id"] + "] Customer credit limit exceeded.");
                                        }
                                        else if(e.Message.IndexOf("customer") != -1)
                                        {
                                            mysql.Insert("UPDATE cms_order SET order_fault = '1', order_fault_message = 'Invalid Customer Code' WHERE order_id = '" + orderObj["order_id"] + "'");
                                            logger.Broadcast("[" + orderObj["order_id"] + "] Invalid Customer Code.");
                                        }
                                        else
                                        {
                                            mysql.Insert("UPDATE cms_order SET order_fault = '1', order_fault_message = '"+e.Message+"' WHERE order_id = '" + orderObj["order_id"] + "'");
                                            logger.Broadcast("[" + orderObj["order_id"] + "] " + e.Message + "");
                                        }
                                    }
                                }
                            }
                            BizObject.Close();
                        NextOrder:
                            Console.WriteLine("Next Order");
                        }
                    }

                    ENDJOB:

                    slog.action_identifier = Constants.Action_Transfer_CN;
                    slog.action_failure = 0;
                    slog.action_details = "Finished";
                    slog.action_failure_message = string.Empty;
                    slog.action_time = DateTime.Now.ToString();

                    DateTime endTime = DateTime.Now;
                    TimeSpan ts = endTime - startTime;
                    Console.WriteLine("Completed in: " + ts.TotalSeconds.ToString("F"));

                    LocalDB.InsertSyncLog(slog);

                    logger.message = "Transfer CN finished in (" + ts.TotalSeconds.ToString("F") + " seconds)";
                    logger.Broadcast();

                });

                thread.Start();
            }
            catch (ThreadAbortException e)
            {
                DpprException ex = new DpprException
                {
                    file_name = "JobTransferCN",
                    exception = e.Message,
                    time = DateTime.Now.ToString()
                };
                LocalDB.InsertException(ex);

                Console.WriteLine(Constants.Thread_Exception + e.Message);
            }
        }
    }
}