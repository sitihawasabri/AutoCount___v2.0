using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using AutoCount.Invoicing.Sales.SalesOrder;
using EasySales.Model;
using EasySales.Object;
using Google.Protobuf.WellKnownTypes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Org.BouncyCastle.Asn1;
using Quartz;
using Quartz.Logging;
using AutoCount.Stock.ItemPackage;
using AutoCount.Invoicing;
using AutoCount;

namespace EasySales.Job
{
    [DisallowConcurrentExecution]
    public class JobATCTransferSOSDKv2 : IJob
    {
        private ATC_Connection connection = null;
        private static readonly string LOCATION = "HQ";
        private string socket_OrderId = string.Empty;

        public void ExecuteSocket(string socket_OrderId)
        {
            this.socket_OrderId = socket_OrderId;
            Execute();
        }

        private string FormatAsRTF(string rtfString)
        {
            System.Windows.Forms.RichTextBox rtf = new System.Windows.Forms.RichTextBox();
            rtf.Text = rtfString;
            return rtf.Rtf;
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

                    logger.Broadcast("atcSdkSetting");

                    DpprUserSettings atcSdkSetting = LocalDB.GetParticularSetting(Constants.Setting_ATCv2);

                    bool isATCv2 = atcSdkSetting.setting == Constants.YES;

                    logger.Broadcast("isATCv2: " + isATCv2);

                    DpprSyncLog slog = new DpprSyncLog();
                    slog.action_identifier = Constants.Action_ATC_Transfer_SO;
                    slog.action_failure = 0;
                    slog.action_failure_message = string.Empty;
                    slog.action_time = DateTime.Now.ToString();

                    DateTime startTime = DateTime.Now;

                    LocalDB.InsertSyncLog(slog);

                    logger.message = "ATC Transfer SO (SDK) v2.0 is running";
                    logger.Broadcast();

                    List<DpprMySQLconfig> mysql_list = LocalDB.GetRemoteDatabaseConfig().Distinct().ToList();
                    List<DpprSQLServerconfig> mssql_list = LocalDB.GetRemoteSQLServerConfig();

                    mysql_list.Iterate<DpprMySQLconfig>((mysql_config, index) =>
                    {
                        DpprMySQLconfig mysqlconfig = mysql_list[index];
                        Database mysql = new Database();
                        mysql.Connect(index: index);

                        string targetDBname = string.Empty;
                        string order_status = "2";
                        string autoCount_sst = "false";
                        int enable_credit_control = 0;
                        string acc_no = "500-00000";
                        int insert_disc = 1;
                        int pickpack_app = 0;
                        int include_no_stock = 0;
                        int checking_module = 0;
                        int transfer_to_do = 0;

                        CheckBackendRule checkDB = new CheckBackendRule(mysql: mysql);
                        dynamic jsonRule = checkDB.CheckTablesExist().GetSettingByTableName("transfer_so_atc");

                        if (jsonRule.Count > 0)
                        {
                            foreach (var key in jsonRule)
                            {
                                if (key.mssql != null && key.mssql.GetType().ToString() == "Newtonsoft.Json.Linq.JArray")
                                {
                                    var mssql_rules = key.mssql;
                                    foreach (var db in mssql_rules)
                                    {
                                        targetDBname = db.name;
                                        order_status = db.order_status;
                                        autoCount_sst = db.autoCount_sst;

                                        if (db.enable_credit_control != null)
                                        {
                                            enable_credit_control = db.enable_credit_control;
                                        }

                                        if (db.acc_no != null)
                                        {
                                            acc_no = db.acc_no;
                                        }
                                        if (db.insert_disc != null)
                                        {
                                            insert_disc = db.insert_disc;
                                        }

                                        if (db.pickpack_app != null)
                                        {
                                            pickpack_app = db.pickpack_app;
                                        }

                                        if (db.checking_module != null)
                                        {
                                            checking_module = db.checking_module;
                                        }

                                        if (db.include_no_stock != null)
                                        {
                                            include_no_stock = db.include_no_stock;
                                        } 
                                        
                                        if (db.transfer_to_do != null)
                                        {
                                            transfer_to_do = db.transfer_to_do;
                                        }
                                    }
                                }
                            }

                            ATChandler autoCount = new ATChandler(isATCv2);
                            logger.Broadcast("Approaching triggerConnection");
                            if (isATCv2)
                            {
                                bool triggerConnection = AutoCountV2.TriggerConnection();
                                logger.Broadcast("triggerConnection: " + triggerConnection);
                            }

                            //logger.Broadcast("Getting User Session -> " + (AutoCountV2.TriggerConnection() == true).ToString());
                            this.connection = autoCount.PerformAuth();

                            //SELECT cms_order.*, CAST(cms_order.order_udf AS CHAR(10000) CHARACTER SET utf8) AS orderUdfJson, cms_login.staff_code FROM cms_order LEFT JOIN cms_login ON cms_order.salesperson_id = cms_login.login_id WHERE order_status = 1 AND cancel_status = 0 AND packing_status >= 1 AND doc_type = 'sales' AND order_fault = 0
                            string pickpack_query = " AND packing_status >= 1 ";
                            pickpack_query += checking_module == 1 ? " AND pack_confirmed = 1" : string.Empty;
                            string SOQuery = "SELECT cms_order.*,DATE_FORMAT(cms_order.order_date, '%d/%m/%Y %H:%s:%i') AS order_date_format,DATE_FORMAT(cms_order.delivery_date, '%d/%m/%Y %H:%s:%i') AS deli_date_format, cms_login.staff_code FROM cms_order LEFT JOIN cms_login ON cms_order.salesperson_id = cms_login.login_id WHERE order_status = " + order_status + " AND cancel_status = 0 AND doc_type = 'sales' AND order_fault = 0 ";
                            //order_id = "\"SO-SS-001\",\"SO-SS-003\",\"SO-SS-002\"";
                            socket_OrderId = socket_OrderId.Replace("\"", "\'");
                            string socketQuery = "AND order_id IN (" + socket_OrderId + ")";

                            SOQuery = SOQuery + (socket_OrderId != string.Empty ? socketQuery : "");

                            SOQuery += pickpack_app == 1 ? pickpack_query : "";

                            ArrayList queryResult = mysql.Select(SOQuery);
                            mysql.Message(SOQuery);

                            logger.Broadcast("Order Query: " + SOQuery);

                            logger.Broadcast("Total orders to be transferred: " + queryResult.Count);

                            SQLServer mssql = new SQLServer();
                            mssql.Connect(targetDBname);

                            if (queryResult.Count == 0)
                            {
                                logger.message = "No orders to be transferred";
                                logger.Broadcast();
                            }
                            else
                            {
                                logger.Broadcast("Try to login to AutoCount");
                                if (autoCount.PerformAuthInAutoCount())
                                {
                                    logger.Broadcast("Login with AutoCount is successful");
                                    logger.Broadcast("Inserting salesorder");

                                    for (int i = 0; i < queryResult.Count; i++)
                                    {
                                        int pickpack_counter = 0;
                                        int fault = 0;
                                        string orderId = string.Empty;

                                        Dictionary<string, string> cms_data = (Dictionary<string, string>)queryResult[i];
                                        logger.Broadcast("Salesorder data found");
                                        try
                                        {
                                            AutoCount.Invoicing.Sales.SalesOrder.SalesOrderCommand command =
        AutoCount.Invoicing.Sales.SalesOrder.SalesOrderCommand.Create(this.connection.userSession, this.connection.userSession.DBSetting);
                                            AutoCount.Invoicing.Sales.SalesOrder.SalesOrder addSalesOrder = command.AddNew();
                                            if (enable_credit_control == 0)
                                            {
                                                addSalesOrder.DisableCreditControl();
                                            }
                                            else
                                            {
                                                addSalesOrder.EnableCreditControl();
                                            }

                                            AutoCount.Invoicing.Sales.SalesOrder.SalesOrderDetail addOrderDetail;
                                            //SalesOrderCommand command = SalesOrderCommand.Create(this.connection.dBSetting);
                                            //SalesOrder addSalesOrder = command.AddNew();
                                            //SalesOrderDetail addOrderDetail;

                                            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-GB");
                                            orderId = cms_data["order_id"];
                                            logger.Broadcast("Trying to transfer order: " + orderId);
                                            addSalesOrder.DocNo = cms_data["order_id"];
                                            addSalesOrder.DocDate = Helper.ToDateTime(cms_data["order_date_format"]);
                                            addSalesOrder.DebtorCode = cms_data["cust_code"];
                                            addSalesOrder.DebtorName = cms_data["cust_company_name"];
                                            addSalesOrder.Agent = cms_data["staff_code"];
                                            if (cms_data["branch_code"] != "N/A")
                                            {
                                                addSalesOrder.BranchCode = cms_data["branch_code"];
                                            }
                                            addSalesOrder.InvAddr1 = cms_data["billing_address1"];
                                            addSalesOrder.InvAddr2 = cms_data["billing_address2"];
                                            addSalesOrder.InvAddr3 = cms_data["billing_address3"];
                                            addSalesOrder.InvAddr4 = cms_data["billing_address4"];
                                            addSalesOrder.Phone1 = cms_data["cust_tel"];
                                            addSalesOrder.Fax1 = cms_data["cust_fax"];

                                            addSalesOrder.DeliverAddr1 = cms_data["shipping_address1"];
                                            addSalesOrder.DeliverAddr2 = cms_data["shipping_address2"];
                                            addSalesOrder.DeliverAddr3 = cms_data["shipping_address3"];
                                            addSalesOrder.DeliverAddr4 = cms_data["shipping_address4"];
                                            addSalesOrder.DeliverPhone1 = cms_data["cust_tel"];
                                            addSalesOrder.DeliverFax1 = cms_data["cust_fax"];
                                            addSalesOrder.DeliverContact = cms_data["cust_fax"];
                                            addSalesOrder.SalesLocation = cms_data["warehouse_code"];
                                            addSalesOrder.Ref = cms_data["cust_reference"];
                                            //string order_delivery_note = cms_data["order_delivery_note"];
                                            //int noteLength = order_delivery_note.Length;
                                            //if(noteLength > 40)
                                            //{
                                            //    //insert some where else
                                            //}
                                            //else
                                            //{
                                            //    addSalesOrder.Remark1 = cms_data["order_delivery_note"];
                                            //}
                                            //string note = FormatAsRTF(order_delivery_note);
                                            //addSalesOrder.Note = note;
                                            addSalesOrder.Remark1 = cms_data["order_delivery_note"];
                                            addSalesOrder.ShipInfo = Helper.ToDateTime(cms_data["deli_date_format"]).ToString();

                                            // Let's get the total price 
                                            double net_total;
                                            ArrayList netotal_arr = mysql.Select("SELECT SUM(sub_total) AS sub_total FROM cms_order_item WHERE cancel_status = 0 AND packing_status = 1 AND isParent <> 1 AND order_id = '" + cms_data["order_id"] + "';");
                                            if (netotal_arr.Count > 0)
                                            {
                                                Dictionary<string, string> nettotal_map = (Dictionary<string, string>)netotal_arr[0];

                                                double.TryParse(nettotal_map["sub_total"], out net_total);

                                                if (autoCount_sst == "true")
                                                {
                                                    // TODO autoCount SST tax amount
                                                    double tax_amount = net_total * 0.05;
                                                    tax_amount += net_total;
                                                }
                                                nettotal_map.Clear();
                                            }
                                            else
                                            {
                                                net_total = 0;
                                            }
                                            netotal_arr.Clear();

                                            string itemQuery = "SELECT * from cms_order_item WHERE cancel_status = 0 AND order_id = '" + cms_data["order_id"] + "' ";
                                            if (pickpack_app == 1)
                                            {
                                                pickpack_query = " AND packing_status = 1 AND packed_qty <> 0 ";
                                                pickpack_query += checking_module == 1 ? " AND pack_confirmed_status = 1" : string.Empty;
                                                itemQuery += pickpack_query;
                                            }

                                            itemQuery += " ORDER BY order_item_id ASC";
                                            logger.Broadcast("Item Query ==> " + itemQuery);
                                            ArrayList allItemList = mysql.Select(itemQuery);
                                            //picked query = SELECT * FROM cms_order_item WHERE order_id = '" + orderId + "' AND cancel_status = 0 AND packing_status = 1 AND packed_qty <> 0

                                            //no stock query = SELECT * FROM cms_order_item WHERE packed_qty <> quantity AND packing_status <> 0 AND cancel_status = 0 AND order_id = '" + orderId + "'

                                            if (allItemList.Count > 0)
                                            {
                                                logger.Broadcast("Inserting normal item");

                                                foreach (Dictionary<string, string> item in allItemList)
                                                {
                                                    //logger.Broadcast("Trying to transfer order item: " + item["product_code"]);
                                                    decimal.TryParse(item["quantity"], out decimal quantity);
                                                    decimal.TryParse(item["packed_qty"], out decimal packedQty);

                                                    logger.Broadcast("Item Qty ==> " + quantity);
                                                    logger.Broadcast("Item Packed Qty ==> " + packedQty);
                                                    if (pickpack_app == 1)
                                                    {
                                                        quantity = packedQty;
                                                    }
                                                    logger.Broadcast("Qty ==> " + quantity);
                                                    decimal.TryParse(item["unit_price"], out decimal unitPrice);
                                                    decimal.TryParse(item["discount_amount"], out decimal discountAmount);
                                                    discountAmount = Math.Round(discountAmount, 2);
                                                    logger.Broadcast("['discount_amount']: " + discountAmount);

                                                    decimal disc_percent = 0;
                                                    if (discountAmount > 0)
                                                    {
                                                        disc_percent = discountAmount / (unitPrice * quantity) * 100;
                                                        logger.Broadcast("disc_percent: " + disc_percent);
                                                        disc_percent = Math.Round(disc_percent, 2);
                                                    }

                                                    decimal.TryParse(item["sub_total"], out decimal sub_total);

                                                    quantity = Math.Round(quantity, 2);
                                                    unitPrice = Math.Round(unitPrice, 2);
                                                    discountAmount = Math.Round(discountAmount, 2);
                                                    sub_total = Math.Round(sub_total, 2);

                                                    string discount = disc_percent + "%";
                                                    string product_code = item["product_code"];
                                                    //string uom_rate = item["uom_rate"];
                                                    string uom = item["unit_uom"];

                                                    addOrderDetail = addSalesOrder.AddDetail();
                                                    addOrderDetail.ItemCode = product_code;
                                                    string salespersonRemark = item["salesperson_remark"];
                                                    string furtherDescription = FormatAsRTF(salespersonRemark);
                                                    //Console.WriteLine(furtherDescription);
                                                    addOrderDetail.FurtherDescription = furtherDescription;
                                                    addOrderDetail.Description = item["product_name"];
                                                    addOrderDetail.Qty = quantity;

                                                    if(insert_disc == 1)
                                                    {
                                                        string disc_1 = item["disc_1"];
                                                        string discount_amount = item["discount_amount"];
                                                        if (disc_1 == discount_amount) //meaning RM
                                                        {
                                                            discount = disc_1;
                                                            discountAmount = Math.Round(discountAmount, 2);
                                                        }
                                                        else
                                                        {
                                                            disc_percent = Math.Round(disc_percent);
                                                            discount = disc_percent + "%";
                                                            discountAmount = Math.Round(discountAmount, 2);
                                                        }
                                                        /* Disc in % */
                                                        addOrderDetail.Discount = discount; //kian doesnt want to insert discount -- deployed already
                                                        logger.Broadcast("Discount: " + discount);
                                                        /* Disc in RM */
                                                        addOrderDetail.DiscountAmt = discountAmount;
                                                        logger.Broadcast("Discount Amount: " + discountAmount);
                                                    }
                                                    

                                                    //string stmtu = "SELECT UOM, Rate, ItemCode, Volume, Weight FROM ItemUOM WHERE ItemCode = '" + product_code + "' AND Rate = '"+ uom_rate + "'";
                                                    string stmtu = "SELECT UOM, Rate, ItemCode, Volume, Weight FROM ItemUOM where UOM = N\'" + item["unit_uom"] + "' and ItemCode = '" + product_code + "'";

                                                    ArrayList itemUom = mssql.Select(stmtu);
                                                    mysql.Message("get UOM from MSSQL Query ===> " + stmtu);

                                                    string weight = string.Empty;
                                                    string volume = string.Empty;

                                                    if (itemUom.Count > 0)
                                                    {
                                                        Dictionary<string, string> uomList = (Dictionary<string, string>)itemUom[0];

                                                        uom = uomList["UOM"];
                                                        uomList.Clear();
                                                    }
                                                    else
                                                    {
                                                        string msg = "No UOM found for this product code ===> [" + product_code + " | " + uom + "]";
                                                        mysql.Message(msg);
                                                        logger.Broadcast(msg);

                                                        //logger.Broadcast("Getting the latest uom for this item");
                                                        //string __stmtu = "SELECT UOM, Rate, ItemCode, Volume, Weight FROM ItemUOM where ItemCode = '" + product_code + "'";

                                                        //ArrayList __itemUom = mssql.Select(__stmtu);
                                                        //mysql.Message("check for latest uom for that itemCode due to deleted uom MSSQL Query ===> " + __stmtu);
                                                        ////check for latest uom for that itemCode
                                                        //for (int ix = 0; ix < __itemUom.Count; ix++)
                                                        //{
                                                        //    Dictionary<string, string> uomList = (Dictionary<string, string>)itemUom[ix];
                                                        //    string __rate = uomList["Rate"];
                                                        //    string __uom = uomList["UOM"];
                                                        //    string __itemCode = uomList["ItemCode"];

                                                        //    if (__rate == uom_rate) //pcs - 1, set - 1
                                                        //    {
                                                        //        uom = __uom;
                                                        //    }
                                                        //}
                                                        ////get uom rate from mysql, search in sql based on rate and product code bcs they might changed the product uom
                                                    }
                                                    itemUom.Clear();

                                                    addOrderDetail.UOM = uom;
                                                    addOrderDetail.UnitPrice = unitPrice;
                                                    addOrderDetail.SubTotal = sub_total;
                                                    addOrderDetail.Location = cms_data["warehouse_code"];
                                                    
                                                    item.Clear();
                                                }
                                            }
                                            else
                                            {
                                                if(pickpack_app == 0)
                                                {
                                                    string msg = "No items found for this order: [" + orderId + "]. Please check the internet connection.";
                                                    Database.Sanitize(ref msg);
                                                    mysql.Insert("UPDATE cms_order SET order_fault_message = '" + msg + "' WHERE order_id = '" + orderId + "'");
                                                    fault++;
                                                }
                                                else
                                                {
                                                    fault++;
                                                    logger.Broadcast("No picked items for this order. Proceed to no stock order");
                                                    goto noStockOrders;
                                                   
                                                }
                                                
                                            }

                                            allItemList.Clear();

                                            try
                                            {
                                                if(fault == 0)
                                                {
                                                    logger.Broadcast("Trying to save order");
                                                    addSalesOrder.Save();
                                                    logger.Broadcast("Sales Order [" + orderId + "] created");

                                                    if(pickpack_app == 0)
                                                    {
                                                        int.TryParse(order_status, out int int_order_status);
                                                        int updateOrderStatus = int_order_status + 1;
                                                        string updateStatusQuery = "UPDATE cms_order SET order_status = '" + updateOrderStatus + "' WHERE order_id = '" + cms_data["order_id"] + "'";

                                                        int failCounter = 0;
                                                    checkUpdateStatus:
                                                        bool updateStatus = mysql.Insert(updateStatusQuery);
                                                        mysql.Message(updateStatusQuery);
                                                        if (!updateStatus)
                                                        {
                                                            //order transferred to ATC but fail to update order_status in our db
                                                            //delay 2 seconds before try update status again
                                                            Task.Delay(2000);
                                                            failCounter++;
                                                            if (failCounter < 4)
                                                            {
                                                                goto checkUpdateStatus;
                                                            }
                                                        }
                                                    }
                                                    else
                                                    {
                                                        pickpack_counter++;
                                                        logger.Broadcast("Pickpack counter ==> " + pickpack_counter);
                                                    }
                                                }
                                                else
                                                {
                                                    if(pickpack_app == 0)
                                                    {
                                                        logger.Broadcast("Abort order: Fault (" + fault + "). Kindly check the order fault message.");
                                                        logger.Broadcast("Proceeding next order");
                                                        goto NextOrders;
                                                    }
                                                    else
                                                    {
                                                        logger.Broadcast("No picked items for this order. Proceed to no stock order");
                                                        goto noStockOrders;

                                                    }
                                                }
                                                
                                            }
                                            catch (Exception ex)
                                            {
                                                logger.Broadcast("Failed to transfer [" + orderId + "]: " + ex.Message);
                                                AutoCountV2.Message("Failed to transfer [" + orderId + "]: " + ex.Message);

                                                if (ex.Message == "Primary Key Error")
                                                {
                                                    int.TryParse(order_status, out int int_order_status);
                                                    int updateOrderStatus = int_order_status + 1;
                                                    string updateStatusQuery = "UPDATE cms_order SET order_status = '" + updateOrderStatus + "' WHERE order_id = '" + cms_data["order_id"] + "'";

                                                    int failCounter = 0;
                                                checkUpdateStatus:
                                                    bool updateStatus = mysql.Insert(updateStatusQuery);
                                                    mysql.Message(updateStatusQuery);
                                                    if (!updateStatus)
                                                    {
                                                        Task.Delay(2000);
                                                        failCounter++;
                                                        if (failCounter < 4)
                                                        {
                                                            goto checkUpdateStatus;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        logger.Broadcast("[" + orderId + "] already created. Kindly check in the AutoCount");
                                                        AutoCountV1.Message("[" + orderId + "] already created. Kindly check in the AutoCount");
                                                    }
                                                }
                                                else if (ex.Message.IndexOf("limit") != -1)
                                                {
                                                    //This customer account[J BEDS & SOFA] has exceeded the credit limit.
                                                    string msg = ex.Message;
                                                    Database.Sanitize(ref msg);
                                                    mysql.Insert("UPDATE cms_order SET order_fault_message = '" + msg + "' WHERE order_id = '" + orderId + "'");
                                                }
                                                else
                                                {
                                                    mysql.Insert("UPDATE cms_order SET order_fault_message = '" + ex.Message + "' WHERE order_id = '" + orderId + "'");
                                                }
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            logger.Broadcast("Failed to transfer: " + ex.Message);
                                            AutoCountV1.Message("Failed to transfer: " + ex.Message);

                                            logger.Broadcast("Failed to transfer [" + orderId + "]: " + ex.Message);
                                            AutoCountV2.Message("Failed to transfer [" + orderId + "]: " + ex.Message);

                                            if (ex.Message == "Primary Key Error")
                                            {
                                                int.TryParse(order_status, out int int_order_status);
                                                int updateOrderStatus = int_order_status + 1;
                                                string updateStatusQuery = "UPDATE cms_order SET order_status = '" + updateOrderStatus + "' WHERE order_id = '" + cms_data["order_id"] + "'";

                                                int failCounter = 0;
                                            checkUpdateStatus:
                                                bool updateStatus = mysql.Insert(updateStatusQuery);
                                                mysql.Message(updateStatusQuery);
                                                if (!updateStatus)
                                                {
                                                    Task.Delay(2000);
                                                    failCounter++;
                                                    if (failCounter < 4)
                                                    {
                                                        goto checkUpdateStatus;
                                                    }
                                                }
                                                else
                                                {
                                                    logger.Broadcast("[" + orderId + "] already created. Kindly check in the AutoCount");
                                                    AutoCountV1.Message("[" + orderId + "] already created. Kindly check in the AutoCount");
                                                }
                                            }
                                            else if (ex.Message.IndexOf("limit") != -1)
                                            {
                                                string msg = "This customer account [" + cms_data["cust_company_name"] + "] has exceeded the credit limit";
                                                mysql.Insert("UPDATE cms_order SET order_fault_message = '" + msg + "' WHERE order_id = '" + orderId + "'");
                                            }
                                            else
                                            {
                                                mysql.Insert("UPDATE cms_order SET order_fault_message = '" + ex.Message + "' WHERE order_id = '" + orderId + "'");
                                            }
                                        }

                                        noStockOrders:
                                        if (include_no_stock == 1 && pickpack_app == 1)
                                        {
                                            fault = 0;
                                            logger.Broadcast("Inserting no stock orders");
                                            //no stock query = SELECT * FROM cms_order_item WHERE packed_qty <> quantity AND packing_status <> 0 AND cancel_status = 0 AND order_id = '" + orderId + "'
                                            string noStockorderId = string.Empty;
                                            try
                                            {
                                                AutoCount.Invoicing.Sales.SalesOrder.SalesOrderCommand commandNoStock =
AutoCount.Invoicing.Sales.SalesOrder.SalesOrderCommand.Create(this.connection.userSession, this.connection.userSession.DBSetting);
                                                AutoCount.Invoicing.Sales.SalesOrder.SalesOrder addSalesOrderNoStock = commandNoStock.AddNew();
                                                if (enable_credit_control == 0)
                                                {
                                                    addSalesOrderNoStock.DisableCreditControl();
                                                }
                                                else
                                                {
                                                    addSalesOrderNoStock.EnableCreditControl();
                                                }

                                                AutoCount.Invoicing.Sales.SalesOrder.SalesOrderDetail addOrderDetailNoStock;

                                                Thread.CurrentThread.CurrentCulture = new CultureInfo("en-GB");
                                                noStockorderId = cms_data["order_id"] + "-1";
                                                logger.Broadcast("Trying to transfer order: " + noStockorderId);
                                                addSalesOrderNoStock.DocNo = noStockorderId;
                                                addSalesOrderNoStock.DocDate = Helper.ToDateTime(cms_data["order_date_format"]);
                                                addSalesOrderNoStock.DebtorCode = cms_data["cust_code"];
                                                addSalesOrderNoStock.DebtorName = cms_data["cust_company_name"];
                                                addSalesOrderNoStock.Agent = cms_data["staff_code"];
                                                if (cms_data["branch_code"] != "N/A")
                                                {
                                                    addSalesOrderNoStock.BranchCode = cms_data["branch_code"];
                                                }
                                                addSalesOrderNoStock.InvAddr1 = cms_data["billing_address1"];
                                                addSalesOrderNoStock.InvAddr2 = cms_data["billing_address2"];
                                                addSalesOrderNoStock.InvAddr3 = cms_data["billing_address3"];
                                                addSalesOrderNoStock.InvAddr4 = cms_data["billing_address4"];
                                                addSalesOrderNoStock.Phone1 = cms_data["cust_tel"];
                                                addSalesOrderNoStock.Fax1 = cms_data["cust_fax"];

                                                addSalesOrderNoStock.DeliverAddr1 = cms_data["shipping_address1"];
                                                addSalesOrderNoStock.DeliverAddr2 = cms_data["shipping_address2"];
                                                addSalesOrderNoStock.DeliverAddr3 = cms_data["shipping_address3"];
                                                addSalesOrderNoStock.DeliverAddr4 = cms_data["shipping_address4"];
                                                addSalesOrderNoStock.DeliverPhone1 = cms_data["cust_tel"];
                                                addSalesOrderNoStock.DeliverFax1 = cms_data["cust_fax"];
                                                addSalesOrderNoStock.DeliverContact = cms_data["cust_fax"];
                                                addSalesOrderNoStock.SalesLocation = cms_data["warehouse_code"];
                                                addSalesOrderNoStock.Ref = cms_data["cust_reference"];
                                                addSalesOrderNoStock.Remark1 = cms_data["order_delivery_note"];
                                                addSalesOrderNoStock.ShipInfo = Helper.ToDateTime(cms_data["deli_date_format"]).ToString();

                                                // Let's get the total price 
                                                double net_total_no_stock;
                                                string getNoStockItem = "SELECT SUM(sub_total) AS sub_total FROM cms_order_item WHERE cancel_status = 0 AND packing_status = 1 AND isParent <> 1 AND order_id = '" + cms_data["order_id"] + "';";
                                                getNoStockItem += checking_module == 1 ? " AND pack_confirmed_status = 1" : string.Empty;
                                                ArrayList netotal_arr_no_stock = mysql.Select(getNoStockItem);
                                                logger.Broadcast("getNoStockItem query === " + getNoStockItem);
                                                if (netotal_arr_no_stock.Count > 0)
                                                {
                                                    Dictionary<string, string> nettotal_map = (Dictionary<string, string>)netotal_arr_no_stock[0];

                                                    double.TryParse(nettotal_map["sub_total"], out net_total_no_stock);

                                                    if (autoCount_sst == "true")
                                                    {
                                                        double tax_amount = net_total_no_stock * 0.05;
                                                        tax_amount += net_total_no_stock;
                                                    }
                                                    nettotal_map.Clear();
                                                }
                                                else
                                                {
                                                    net_total_no_stock = 0;
                                                }
                                                netotal_arr_no_stock.Clear();

                                                ArrayList noStockItemList = mysql.Select("SELECT * FROM cms_order_item WHERE packed_qty <> quantity AND packing_status <> 0 AND cancel_status = 0 AND order_id = '" + cms_data["order_id"] + "'");

                                                if (noStockItemList.Count > 0)
                                                {
                                                    logger.Broadcast("Inserting normal item");

                                                    foreach (Dictionary<string, string> item in noStockItemList)
                                                    {
                                                        string product_code = item["product_code"];
                                                        string uom = item["unit_uom"];

                                                        logger.Broadcast("product_code: " + product_code);

                                                        
                                                        decimal.TryParse(item["quantity"], out decimal quantity);
                                                        decimal.TryParse(item["packed_qty"], out decimal packedQty);
                                                        decimal nostockQty = quantity - packedQty;
                                                        logger.Broadcast("Item Qty ==> " + quantity);
                                                        logger.Broadcast("Item Packed Qty ==> " + packedQty);
                                                        logger.Broadcast("nostockQty ==> " + nostockQty);
                                                        decimal.TryParse(item["unit_price"], out decimal unitPrice);
                                                        decimal.TryParse(item["discount_amount"], out decimal discountAmount);
                                                        discountAmount = Math.Round(discountAmount, 2);
                                                        logger.Broadcast("['discount_amount']: " + discountAmount);

                                                        decimal disc_percent = 0;
                                                        if (discountAmount > 0)
                                                        {
                                                            disc_percent = discountAmount / (unitPrice * nostockQty) * 100;
                                                            logger.Broadcast("disc_percent: " + disc_percent);
                                                            disc_percent = Math.Round(disc_percent, 2);
                                                        }

                                                        decimal.TryParse(item["sub_total"], out decimal sub_total);

                                                        nostockQty = Math.Round(nostockQty, 2);
                                                        unitPrice = Math.Round(unitPrice, 2);
                                                        discountAmount = Math.Round(discountAmount, 2);
                                                        sub_total = Math.Round(sub_total, 2);

                                                        string discount = disc_percent + "%";


                                                        addOrderDetailNoStock = addSalesOrderNoStock.AddDetail();
                                                        addOrderDetailNoStock.ItemCode = product_code;
                                                        string salespersonRemark = item["salesperson_remark"];
                                                        string furtherDescription = FormatAsRTF(salespersonRemark);
                                                        addOrderDetailNoStock.FurtherDescription = furtherDescription;
                                                        addOrderDetailNoStock.Description = item["product_name"];
                                                        addOrderDetailNoStock.Qty = nostockQty;

                                                        if (insert_disc == 1)
                                                        {
                                                            string disc_1 = item["disc_1"];
                                                            string discount_amount = item["discount_amount"];
                                                            if (disc_1 == discount_amount) //meaning RM
                                                            {
                                                                discount = disc_1;
                                                                discountAmount = Math.Round(discountAmount, 2);
                                                            }
                                                            else
                                                            {
                                                                disc_percent = Math.Round(disc_percent);
                                                                discount = disc_percent + "%";
                                                                discountAmount = Math.Round(discountAmount, 2);
                                                            }

                                                            addOrderDetailNoStock.Discount = discount;
                                                            logger.Broadcast("Discount: " + discount);
                                                            addOrderDetailNoStock.DiscountAmt = discountAmount;
                                                            logger.Broadcast("Discount Amount: " + discountAmount);
                                                        }

                                                        string stmtu = "SELECT UOM, Rate, ItemCode, Volume, Weight FROM ItemUOM where UOM = N\'" + item["unit_uom"] + "' and ItemCode = '" + product_code + "'";

                                                        ArrayList itemUom = mssql.Select(stmtu);
                                                        mysql.Message("get UOM from MSSQL Query ===> " + stmtu);

                                                        string weight = string.Empty;
                                                        string volume = string.Empty;

                                                        if (itemUom.Count > 0)
                                                        {
                                                            Dictionary<string, string> uomList = (Dictionary<string, string>)itemUom[0];

                                                            uom = uomList["UOM"];
                                                            uomList.Clear();
                                                        }
                                                        else
                                                        {
                                                            string msg = "No UOM found for this product code ===> [" + product_code + " | " + uom + "]";
                                                            mysql.Message(msg);
                                                            logger.Broadcast(msg);
                                                        }
                                                        itemUom.Clear();

                                                        addOrderDetailNoStock.UOM = uom;
                                                        addOrderDetailNoStock.UnitPrice = unitPrice;
                                                        addOrderDetailNoStock.SubTotal = sub_total;
                                                        addOrderDetailNoStock.Location = cms_data["warehouse_code"];

                                                        item.Clear();
                                                    }
                                                }
                                                else
                                                {
                                                    logger.Broadcast("No items for this no stock order");
                                                    logger.Broadcast("Proceeding next order");
                                                    fault++;
                                                }

                                                noStockItemList.Clear();

                                                try
                                                {
                                                    if (fault == 0)
                                                    {
                                                        logger.Broadcast("Trying to save no stock order");
                                                        addSalesOrderNoStock.Save();
                                                        logger.Broadcast("Sales Order [" + noStockorderId + "] created");

                                                        pickpack_counter++;
                                                        logger.Broadcast("Pickpack counter ==> " + pickpack_counter);
                                                    }
                                                    else
                                                    {
                                                        if (pickpack_counter > 0)
                                                        {
                                                            logger.Broadcast("Updating order status...");
                                                            int.TryParse(order_status, out int int_order_status);
                                                            int updateOrderStatus = int_order_status + 1;
                                                            string updateStatusQuery = "UPDATE cms_order SET order_status = '" + updateOrderStatus + "' WHERE order_id = '" + cms_data["order_id"] + "'";

                                                            int failCounter = 0;
                                                        checkUpdateStatus:
                                                            bool updateStatus = mysql.Insert(updateStatusQuery);
                                                            mysql.Message(updateStatusQuery);
                                                            if (!updateStatus)
                                                            {
                                                                Task.Delay(2000);
                                                                failCounter++;
                                                                if (failCounter < 4)
                                                                {
                                                                    goto checkUpdateStatus;
                                                                }
                                                            }
                                                        }

                                                        logger.Broadcast("Abort order: Fault (" + fault + "). Kindly check the order fault message.");
                                                        logger.Broadcast("Proceeding next order");
                                                        goto NextOrders;
                                                    }

                                                }
                                                catch (Exception ex)
                                                {
                                                    logger.Broadcast("L755: Failed to transfer [" + noStockorderId + "]: " + ex.Message);
                                                    
                                                    if (ex.Message == "Primary Key Error")
                                                    {
                                                        int.TryParse(order_status, out int int_order_status);
                                                        int updateOrderStatus = int_order_status + 1;
                                                        string updateStatusQuery = "UPDATE cms_order SET order_status = '" + updateOrderStatus + "' WHERE order_id = '" + cms_data["order_id"] + "'";

                                                        int failCounter = 0;
                                                    checkUpdateStatus:
                                                        bool updateStatus = mysql.Insert(updateStatusQuery);
                                                        mysql.Message(updateStatusQuery);
                                                        if (!updateStatus)
                                                        {
                                                            Task.Delay(2000);
                                                            failCounter++;
                                                            if (failCounter < 4)
                                                            {
                                                                goto checkUpdateStatus;
                                                            }
                                                        }
                                                        else
                                                        {
                                                            logger.Broadcast("[" + noStockorderId + "] already created. Kindly check in the AutoCount");
                                                        }
                                                    }
                                                    else if (ex.Message.IndexOf("limit") != -1)
                                                    {
                                                        string msg = ex.Message;
                                                        Database.Sanitize(ref msg);
                                                        mysql.Insert("UPDATE cms_order SET order_fault_message = '" + msg + "' WHERE order_id = '" + orderId + "'");
                                                    }
                                                    else
                                                    {
                                                        mysql.Insert("UPDATE cms_order SET order_fault_message = '" + ex.Message + "' WHERE order_id = '" + orderId + "'");
                                                    }
                                                }
                                            }
                                            catch (Exception ex)
                                            {
                                                logger.Broadcast("L795: Failed to transfer [" + noStockorderId + "]: " + ex.Message);

                                                if (ex.Message == "Primary Key Error")
                                                {
                                                    int.TryParse(order_status, out int int_order_status);
                                                    int updateOrderStatus = int_order_status + 1;
                                                    string updateStatusQuery = "UPDATE cms_order SET order_status = '" + updateOrderStatus + "' WHERE order_id = '" + cms_data["order_id"] + "'";

                                                    int failCounter = 0;
                                                checkUpdateStatus:
                                                    bool updateStatus = mysql.Insert(updateStatusQuery);
                                                    mysql.Message(updateStatusQuery);
                                                    if (!updateStatus)
                                                    {
                                                        Task.Delay(2000);
                                                        failCounter++;
                                                        if (failCounter < 4)
                                                        {
                                                            goto checkUpdateStatus;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        logger.Broadcast("[" + noStockorderId + "] already created. Kindly check in the AutoCount");
                                                    }
                                                }
                                                else if (ex.Message.IndexOf("limit") != -1)
                                                {
                                                    string msg = "This customer account [" + cms_data["cust_company_name"] + "] has exceeded the credit limit";
                                                    mysql.Insert("UPDATE cms_order SET order_fault_message = '" + msg + "' WHERE order_id = '" + orderId + "'");
                                                }
                                                else
                                                {
                                                    mysql.Insert("UPDATE cms_order SET order_fault_message = '" + ex.Message + "' WHERE order_id = '" + orderId + "'");
                                                }
                                            }
                                        }

                                        if (pickpack_counter > 0)
                                        {
                                            logger.Broadcast("Updating order status...");
                                            int.TryParse(order_status, out int int_order_status);
                                            int updateOrderStatus = int_order_status + 1;
                                            string updateStatusQuery = "UPDATE cms_order SET order_status = '" + updateOrderStatus + "' WHERE order_id = '" + cms_data["order_id"] + "'";

                                            int failCounter = 0;
                                        checkUpdateStatus:
                                            bool updateStatus = mysql.Insert(updateStatusQuery);
                                            mysql.Message(updateStatusQuery);
                                            if (!updateStatus)
                                            {
                                                Task.Delay(2000);
                                                failCounter++;
                                                if (failCounter < 4)
                                                {
                                                    goto checkUpdateStatus;
                                                }
                                            }
                                        }

                                        cms_data.Clear();
                                    NextOrders:
                                        Console.WriteLine("Next Orders");

                                        if (transfer_to_do == 1)
                                        {
                                            string order_id = "\'" + cms_data["order_id"] + "\'";
                                            new JobATCTransferDOSDK().TransferDO(order_id);
                                        }
                                    }
                                }
                                else
                                {
                                    logger.Broadcast("Login with AutoCount is failed");
                                    AutoCountV1.Message("Login with AutoCount is failed");
                                }
                            }

                            queryResult.Clear();
                        }
                        else
                        {
                            logger.Broadcast("Cannot connect to MYSQL Host at the moment. Kindly wait");
                            mysql.Message("Cannot connect to MYSQL Host at the moment. Kindly wait");
                        }
                    });
                    
                    mysql_list.Clear();
                    mssql_list.Clear();

                    slog.action_identifier = Constants.Action_ATC_Transfer_SO;
                    slog.action_failure = 0;
                    slog.action_failure_message = string.Empty;
                    slog.action_time = DateTime.Now.ToString();

                    DateTime endTime = DateTime.Now;
                    TimeSpan ts = endTime - startTime;
                    //Console.WriteLine("Completed in: " + ts.TotalSeconds.ToString("F"));

                    LocalDB.InsertSyncLog(slog);

                    logger.message = "Transfer SO (SDK) finished in (" + ts.TotalSeconds.ToString("F") + " seconds)";
                    logger.Broadcast();

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
                    file_name = "JobATCTransferSOSDK",
                    exception = e.Message,
                    time = DateTime.Now.ToString()
                };
                LocalDB.InsertException(ex);

                Console.WriteLine(Constants.Thread_Exception + e.Message);
            }
        }
    }
}