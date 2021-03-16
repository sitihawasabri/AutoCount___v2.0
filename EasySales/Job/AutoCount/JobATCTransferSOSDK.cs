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
using AutoCount.Data;

namespace EasySales.Job
{
    [DisallowConcurrentExecution]
    public class JobATCTransferSOSDK : IJob
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

        private bool CreateNonExistItemPackage(string packageCode, DBSetting dBSetting)
        {
            ItemPackageCommand cmd = ItemPackageCommand.Create(connection.userSession, dBSetting);
            ItemPackage itemPackage = cmd.Edit(packageCode);
            ItemPackageDetail dtl;

            GlobalLogger logger = new GlobalLogger();

            if (itemPackage == null)
            {
                try
                {
                    //itemPackage.Save();
                    //Log Item Package Created
                    logger.Broadcast("Package NOT found: " + packageCode);
                    return false;
                }
                catch
                {
                    //Log error
                    logger.Broadcast("Package NOT found: " + packageCode);
                    return false;
                }

            }
            else
            {
                logger.Broadcast("Package found: " + packageCode);
                return true;
            }
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

                    DpprSyncLog slog = new DpprSyncLog();
                    slog.action_identifier = Constants.Action_ATC_Transfer_SO;
                    slog.action_failure = 0;
                    slog.action_failure_message = string.Empty;
                    slog.action_time = DateTime.Now.ToString();

                    DateTime startTime = DateTime.Now;

                    LocalDB.InsertSyncLog(slog);

                    logger.message = "ATC Transfer SO (SDK) is running";
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

                        CheckBackendRule checkDB = new CheckBackendRule(mysql: mysql);
                        dynamic jsonRule = checkDB.CheckTablesExist().GetSettingByTableName("transfer_so_atc");

                        ArrayList mssql_rule = new ArrayList();
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
                                    }
                                }
                            }

                            this.connection = ATC_Configuration.Init_config();
                            this.connection.dBSetting = AutoCountV1.PerformAuth(ref this.connection);

                            string SOQuery = "SELECT cms_order.*,DATE_FORMAT(cms_order.order_date, '%d/%m/%Y %H:%s:%i') AS order_date_format, cms_login.staff_code FROM cms_order LEFT JOIN cms_login ON cms_order.salesperson_id = cms_login.login_id WHERE order_status = " + order_status + " AND cancel_status = 0 AND doc_type = 'sales' AND order_fault = 0 ";
                            //order_id = "\"SO-SS-001\",\"SO-SS-003\",\"SO-SS-002\"";
                            socket_OrderId = socket_OrderId.Replace("\"", "\'");
                            string socketQuery = "AND order_id IN (" + socket_OrderId + ")";

                            SOQuery = SOQuery + (socket_OrderId != string.Empty ? socketQuery : "");

                            ArrayList queryResult = mysql.Select(SOQuery);
                            mysql.Message(SOQuery);

                            logger.Broadcast("Total orders to be transferred: " + queryResult.Count);

                            if (queryResult.Count == 0)
                            {
                                logger.message = "No orders to be transferred";
                                logger.Broadcast();
                            }
                            else
                            {
                                logger.Broadcast("Try to login to AutoCount");
                                if (AutoCountV1.PerformAuthInAutoCount(this.connection))
                                {
                                    logger.Broadcast("Login with AutoCount is successful");
                                    logger.Broadcast("Inserting salesorder");

                                    for (int i = 0; i < queryResult.Count; i++)
                                    {
                                        string orderId = string.Empty;

                                        Dictionary<string, string> cms_data = (Dictionary<string, string>)queryResult[i];
                                        logger.Broadcast("Salesorder data found");
                                        try
                                        {
                                            SalesOrderCommand command = SalesOrderCommand.Create(connection.userSession, this.connection.dBSetting);
                                            SalesOrder addSalesOrder = command.AddNew();
                                            SalesOrderDetail addOrderDetail;
                                            InvoicingPackageDetailRecord packageDetail; //if not using default package item

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
                                            addSalesOrder.SalesLocation = LOCATION;
                                            addSalesOrder.Ref = cms_data["cust_reference"];


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

                                            ArrayList allItemList = mysql.Select("SELECT * from cms_order_item WHERE cancel_status = 0 AND order_id = '" + cms_data["order_id"] + "'");

                                            ArrayList allParentList = mysql.Select("SELECT * from cms_order_item WHERE cancel_status = 0 AND order_id = '" + cms_data["order_id"] + "' and isParent = 1");
                                            ArrayList parentKeyList = new ArrayList();
                                            for (int icount = 0; icount < allParentList.Count; icount++)
                                            {
                                                Dictionary<string, string> parentUnique = (Dictionary<string, string>)allParentList[icount];
                                                string ipad_item_id = parentUnique["ipad_item_id"];
                                                string product_code = parentUnique["product_code"];
                                                string parent_code = parentUnique["parent_code"];
                                                string unique_key = ipad_item_id + "^" + product_code;
                                                parentKeyList.Add(unique_key);
                                            }

                                            //ArrayList packageItemDtl = new ArrayList();
                                            for (int ix = 0; ix < allItemList.Count; ix++)
                                            {
                                                Dictionary<string, string> itemUnique = (Dictionary<string, string>)allItemList[ix];
                                                string _ipad_item_id = itemUnique["ipad_item_id"];
                                                int.TryParse(_ipad_item_id, out int ipad_item_id);
                                                string parent_code = itemUnique["parent_code"];
                                                for (int ixx = 0; ixx < parentKeyList.Count; ixx++)
                                                {
                                                    string _key = parentKeyList[ixx].ToString();
                                                    string[] keys = _key.Split('^');
                                                    int.TryParse(keys[0], out int key);
                                                    //Console.WriteLine("key: " + key);
                                                    //Console.WriteLine("keys[1]: " + keys[1]);
                                                    //Console.WriteLine("keys[0]: " + keys[0]);
                                                    if (parent_code == keys[1] && ipad_item_id > key)
                                                    {

                                                        //Console.WriteLine("parent_code: " + itemUnique["parent_code"]);
                                                        //Console.WriteLine("_key: " + _key);
                                                        itemUnique["parent_code"] = _key;
                                                    }
                                                }
                                            }

                                            Dictionary<string, string> remainingOrderItem = new Dictionary<string, string>();
                                            string ipadItemId = string.Empty;
                                            string prodCode = string.Empty;

                                            foreach (Dictionary<string, string> each in allItemList)
                                            {
                                                ipadItemId = each["ipad_item_id"];
                                                prodCode = each["product_code"];
                                                remainingOrderItem.Add(ipadItemId, prodCode);
                                            }

                                            if (allItemList.Count > 0)
                                            {
                                                //transfer package first
                                                bool noError = true;
                                                string packageQuery = "SELECT * FROM cms_order_item WHERE order_id = '" + cms_data["order_id"] + "' AND isParent <> 0 AND parent_code <> '' AND parent_code <> 'FOC'";
                                                //package code - get package code, qty, unit_price, subtotal
                                                ArrayList packageItemParentList = mysql.Select(packageQuery);
                                                if (packageItemParentList.Count > 0)
                                                {
                                                    logger.Broadcast("Inserting package item");
                                                }
                                                for (int ix = 0; ix < packageItemParentList.Count; ix++)
                                                {
                                                    Dictionary<string, string> parentItem = (Dictionary<string, string>)packageItemParentList[ix];
                                                    string packageCode = parentItem["product_code"]; //package code from mysql
                                                    logger.Broadcast("Package Code: " + packageCode);
                                                    ipadItemId = parentItem["ipad_item_id"];
                                                    string removeParent = ipadItemId + "^" + packageCode;
                                                    if (remainingOrderItem.ContainsValue(packageCode))
                                                    {
                                                        for (int ixx = 0; ixx < remainingOrderItem.Count; ixx++)
                                                        {
                                                            string key = remainingOrderItem.ElementAt(ixx).Key;
                                                            string value = remainingOrderItem.ElementAt(ixx).Value;

                                                            if (ipadItemId == key)
                                                            {
                                                                remainingOrderItem.Remove(key); //remove ipad_item_id of item package parent item
                                                                break;
                                                            }
                                                        }
                                                    }

                                                    if (noError && CreateNonExistItemPackage(packageCode, this.connection.dBSetting))
                                                    {
                                                        //Add a new Item Package
                                                        //Apply default sub items in Item Package Maintenance
                                                        //bcs default so no need to insert sub items

                                                        logger.Broadcast("Transferring package to AutoCount..");
                                                        addOrderDetail = addSalesOrder.AddPackage(packageCode);

                                                        Decimal.TryParse(parentItem["quantity"], out decimal qty);
                                                        Decimal.TryParse(parentItem["sub_total"], out decimal subtotal);
                                                        Decimal.TryParse(parentItem["unit_price"], out decimal unit_price);
                                                        qty = Math.Round(qty, 2);
                                                        unit_price = Math.Round(unit_price, 2);
                                                        subtotal = Math.Round(subtotal, 2);

                                                        addOrderDetail.Qty = qty;
                                                        addOrderDetail.SubTotal = subtotal;
                                                        addOrderDetail.UnitPrice = unit_price;
                                                        string temp_remark = parentItem["salesperson_remark"];
                                                        string temp_furdesc = FormatAsRTF(temp_remark);
                                                        //Console.WriteLine(temp_furdesc);
                                                        addOrderDetail.FurtherDescription = temp_furdesc;

                                                        //Remove all default sub items that are maintained in Item Package Maintenance
                                                        addSalesOrder.DeletePackageDetailByDtlKey(addOrderDetail.DtlKey);

                                                        for (int ixx = 0; ixx < allItemList.Count; ixx++)
                                                        {
                                                            Dictionary<string, string> itemchild = (Dictionary<string, string>)allItemList[ixx];

                                                            string parent_code = itemchild["parent_code"];
                                                            //Console.WriteLine("parent_code: " + parent_code);

                                                            string[] parent_code_key = parent_code.Split('^');
                                                            //Console.WriteLine("parent_code_key[0]: " + parent_code_key[0]);
                                                            //Console.WriteLine("ipadItemId: " + ipadItemId);
                                                            if (ipadItemId == parent_code_key[0]) //only insert item with same (ipad_item_id+product_code) only
                                                            {
                                                                logger.Broadcast("Trying to transfer package item: " + itemchild["product_code"]);
                                                                packageDetail = addSalesOrder.AddPackageDetail(addOrderDetail.DtlKey);
                                                                packageDetail.ItemCode = itemchild["product_code"];

                                                                Decimal.TryParse(itemchild["quantity"], out decimal item_qty);
                                                                Decimal.TryParse(itemchild["unit_price"], out decimal item_unit_price);

                                                                item_qty = Math.Round(item_qty, 2);
                                                                item_unit_price = Math.Round(item_unit_price, 2);

                                                                packageDetail.Qty = item_qty;
                                                                packageDetail.UnitPrice = item_unit_price;
                                                                string temp_item_remark = itemchild["salesperson_remark"];
                                                                string temp_item_furdesc = FormatAsRTF(temp_item_remark);
                                                                //Console.WriteLine(temp_item_furdesc);
                                                                packageDetail.FurtherDescription = temp_item_furdesc;

                                                                string itemProductCode = itemchild["product_code"];
                                                                string child_ipadItemId = itemchild["ipad_item_id"];
                                                                if (remainingOrderItem.ContainsValue(itemProductCode))
                                                                {
                                                                    for (int idx = 0; idx < remainingOrderItem.Count; idx++)
                                                                    {
                                                                        string key = remainingOrderItem.ElementAt(idx).Key;
                                                                        string value = remainingOrderItem.ElementAt(idx).Value;

                                                                        if (child_ipadItemId == key)
                                                                        {
                                                                            //Console.WriteLine("remove key:" + key);
                                                                            //Console.WriteLine("remove value:" + value);
                                                                            remainingOrderItem.Remove(key); //remove ipad_item_id of item package child item
                                                                            break;
                                                                        }
                                                                    }
                                                                }
                                                            }

                                                        }
                                                    }
                                                    else
                                                    {
                                                        noError = false;
                                                        logger.Broadcast("Error getting the package");
                                                    }
                                                }
                                                packageItemParentList.Clear();

                                                HashSet<string> normalOrderItemId = new HashSet<string>();
                                                for (int iDx = 0; iDx < remainingOrderItem.Count; iDx++)
                                                {
                                                    string _id = remainingOrderItem.ElementAt(iDx).Key;
                                                    normalOrderItemId.Add(_id);
                                                }
                                                remainingOrderItem.Clear();

                                                string orderItemIn = "'" + string.Join("','", normalOrderItemId) + "'";
                                                normalOrderItemId.Clear();

                                                string normalItem = "SELECT * FROM cms_order_item WHERE order_id = '" + cms_data["order_id"] + "' AND ipad_item_id IN (" + orderItemIn + ")";
                                                ArrayList allNormalOrderItem = mysql.Select(normalItem);

                                                ArrayList getParentFOCItem = mysql.Select("SELECT product_code, parent_code FROM cms_order_item WHERE order_id = '" + cms_data["order_id"] + "' AND parent_code = 'FOC' AND cancel_status = 0");
                                                ArrayList parentProdCodeList = new ArrayList();
                                                for (int ix = 0; ix < getParentFOCItem.Count; ix++)
                                                {
                                                    Dictionary<string, string> each = (Dictionary<string, string>)getParentFOCItem[ix];
                                                    string parentProdCode = each["product_code"];
                                                    parentProdCodeList.Add(parentProdCode);
                                                }

                                                ArrayList focItemList = new ArrayList();


                                                logger.Broadcast("[FOC] parentProdCodeList.Count: " + parentProdCodeList.Count);

                                                if (noError && allNormalOrderItem.Count > 0)
                                                {
                                                    logger.Broadcast("Inserting normal item");

                                                    foreach (Dictionary<string, string> item in allNormalOrderItem)
                                                    {
                                                        logger.Broadcast("Trying to transfer order item: " + item["product_code"]);
                                                        decimal.TryParse(item["quantity"], out decimal quantity);
                                                        decimal.TryParse(item["unit_price"], out decimal unitPrice);
                                                        decimal.TryParse(item["discount_amount"], out decimal discountAmount);

                                                        if (discountAmount > 0)
                                                        {
                                                            discountAmount = discountAmount / (unitPrice * quantity) * 100;
                                                        }

                                                        decimal.TryParse(item["sub_total"], out decimal sub_total);

                                                        string orderItemId = item["order_item_id"];
                                                        if (focItemList.Contains(orderItemId))
                                                        {
                                                            //skip FOC items - no need to insert new row
                                                            goto NextItem;
                                                        }

                                                        quantity = Math.Round(quantity, 2);
                                                        unitPrice = Math.Round(unitPrice, 2);
                                                        discountAmount = Math.Round(discountAmount, 2);
                                                        sub_total = Math.Round(sub_total, 2);

                                                        string discount = discountAmount + "%";

                                                        addOrderDetail = addSalesOrder.AddDetail();
                                                        string itemCode = item["product_code"];
                                                        addOrderDetail.ItemCode = itemCode;
                                                        string salespersonRemark = item["salesperson_remark"];
                                                        string furtherDescription = FormatAsRTF(salespersonRemark);
                                                        //Console.WriteLine(furtherDescription);
                                                        addOrderDetail.FurtherDescription = furtherDescription;
                                                        addOrderDetail.Description = item["product_name"];
                                                        addOrderDetail.Qty = quantity;
                                                        //addOrderDetail.Discount = discount; //kian doesnt want to insert discount -- deployed already
                                                        addOrderDetail.UOM = item["unit_uom"];
                                                        addOrderDetail.UnitPrice = unitPrice;
                                                        addOrderDetail.SubTotal = sub_total;
                                                        addOrderDetail.Location = LOCATION;
                                                        addOrderDetail.DiscountAmt = discountAmount;

                                                        if (parentProdCodeList.Contains(itemCode))
                                                        {
                                                            //get FOC item
                                                            ArrayList getFOC = mysql.Select("SELECT order_item_id, product_code, quantity FROM cms_order_item WHERE order_id = '" + cms_data["order_id"] + "' AND parent_code = '" + itemCode + "' AND cancel_status = 0");

                                                            for (int ixx = 0; ixx < getFOC.Count; ixx++)
                                                            {
                                                                Dictionary<string, string> each = (Dictionary<string, string>)getFOC[ixx];
                                                                string qty = each["quantity"];
                                                                string product_code = each["product_code"];
                                                                string order_item_id = each["order_item_id"];

                                                                if (product_code == itemCode)
                                                                {
                                                                    logger.Broadcast("[" + product_code + "] FOC qty ===> " + qty);
                                                                    decimal.TryParse(qty, out decimal __qty);
                                                                    addOrderDetail.FOCQty = __qty;

                                                                    focItemList.Add(order_item_id);
                                                                }
                                                            }
                                                        }

                                                    NextItem:
                                                        Console.WriteLine("Next Item");
                                                    }
                                                }

                                                allNormalOrderItem.Clear();
                                            }
                                            allParentList.Clear();
                                            parentKeyList.Clear();
                                            allItemList.Clear();

                                            try
                                            {
                                                logger.Broadcast("Trying to save order");
                                                addSalesOrder.Save();
                                                logger.Broadcast("Sales Order [" + orderId + "] created");
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
                                            catch (AutoCount.AppException ex)
                                            {
                                                //Console.WriteLine("0");
                                                //Console.WriteLine(ex.Message);

                                                logger.Broadcast("Failed to transfer [" + orderId + "]: " + ex.Message);
                                                AutoCountV1.Message("Failed to transfer [" + orderId + "]: " + ex.Message);

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
                                                        //order transferred to ATC but fail to update order_status in our db
                                                        //delay 2 seconds before try update status again
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
                                                    string msg = "This customer account ["+ cms_data["cust_company_name"] +"] has exceeded the credit limit";
                                                    mysql.Insert("UPDATE cms_order SET order_fault_message = '" + msg + "' WHERE order_id = '" + orderId + "'");
                                                }
                                                else
                                                {
                                                    mysql.Insert("UPDATE cms_order SET order_fault_message = '" + ex.Message + "' WHERE order_id = '" + orderId + "'");
                                                }
                                            }
                                        }
                                        catch (AutoCount.AppException ex)
                                        {
                                            //Console.WriteLine("0");
                                            //Console.WriteLine(ex.Message);
                                            logger.Broadcast("Failed to transfer: " + ex.Message);
                                            AutoCountV1.Message("Failed to transfer: " + ex.Message);
                                        }
                                    }
                                }
                                else
                                {
                                    //Console.WriteLine("Login with AutoCount is failed");
                                    logger.Broadcast("Login with AutoCount is failed");
                                    AutoCountV1.Message("Login with AutoCount is failed");
                                }
                            }

                            queryResult.Clear();
                        }
                        else
                        {
                            //throw new Exception("ATC Transfer SO (SDK) sync requires backend rules");
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