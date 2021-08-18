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
using EasySales.Model;
using EasySales.Object;
using Google.Protobuf.WellKnownTypes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Org.BouncyCastle.Asn1;
using Quartz;
using Quartz.Logging;
using AutoCount.Invoicing.Sales;
using AutoCount.Invoicing.Sales.Invoice;

namespace EasySales.Job
{
    [DisallowConcurrentExecution]
    public class JobATCTransferINVSDK : IJob
    {
        private ATC_Connection connection = null;
        public string invIdToTransfer = string.Empty;
        public void TransferINV(string invId)
        {
            this.invIdToTransfer = invId;
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
                    slog.action_identifier = Constants.Action_ATC_Transfer_INV;
                    slog.action_failure = 0;
                    slog.action_failure_message = string.Empty;
                    slog.action_time = DateTime.Now.ToString();

                    DateTime startTime = DateTime.Now;

                    LocalDB.InsertSyncLog(slog);

                    logger.message = "ATC Transfer INV (SDK) is running";
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
                        string order_id_format = "<<New>>";
                        int enable_credit_control = 0;
                        string acc_no = "500-00000";

                        CheckBackendRule checkDB = new CheckBackendRule(mysql: mysql);
                        dynamic jsonRule = checkDB.CheckTablesExist().GetSettingByTableName("transfer_inv_atc");

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
                                        targetDBname = db.name != null ? db.name : targetDBname;
                                        order_status = db.order_status != null ? db.order_status : order_status;
                                        order_id_format = db.order_id_format != null ? db.order_id_format : order_id_format;

                                        if (db.enable_credit_control != null)
                                        {
                                            enable_credit_control = db.enable_credit_control;
                                        }

                                        if (db.acc_no != null)
                                        {
                                            acc_no = db.acc_no;
                                        }
                                    }
                                }
                            }

                            logger.Broadcast("Acc No: " + acc_no);

                            string SOQuery = "SELECT o.*,DATE_FORMAT(o.order_date,'%d/%m/%Y %H:%s:%i') AS order_date_format,l.staff_code,cms_customer_branch.branch_name FROM cms_order AS o LEFT JOIN cms_customer AS c ON o.cust_id = c.cust_id LEFT JOIN cms_login AS l ON o.salesperson_id = l.login_id LEFT JOIN cms_customer_branch ON cms_customer_branch.branch_code = o.branch_code WHERE o.order_status = " + order_status + " AND cancel_status = 0 AND doc_type = 'invoice' AND order_fault = 0 ";
                            string whereDOToTransferQuery = " AND order_id IN (" + invIdToTransfer + ")";
                            SOQuery += invIdToTransfer != string.Empty ? whereDOToTransferQuery : string.Empty;

                            //socket_OrderId = socket_OrderId.Replace("\"", "\'");
                            //string socketQuery = "AND order_id IN (" + socket_OrderId + ")";
                            //SOQuery = SOQuery + (socket_OrderId != string.Empty ? socketQuery : "");

                            ArrayList queryResult = mysql.Select(SOQuery);
                            mysql.Message(SOQuery);

                            logger.Broadcast("Total INV to be transferred: " + queryResult.Count);

                            ATChandler autoCount = new ATChandler(isATCv2);
                            logger.Broadcast("Approaching triggerConnection");
                            if(isATCv2)
                            {
                                bool triggerConnection = AutoCountV2.TriggerConnection();
                                logger.Broadcast("triggerConnection: " + triggerConnection);
                            }
                            this.connection = autoCount.PerformAuth();

                            logger.Broadcast("Succesfully getting the user session");

                            ArrayList onlyItemToSync = new ArrayList();

                            if (queryResult.Count == 0)
                            {
                                logger.message = "No INV to be transferred";
                                logger.Broadcast();
                            }
                            else
                            {
                                logger.Broadcast("Login to AutoCount now");

                                if (autoCount.PerformAuthInAutoCount())
                                {
                                    logger.Broadcast("Login with AutoCount is successful");
                                    logger.Broadcast("Inserting Sales Invoices");

                                    for (int i = 0; i < queryResult.Count; i++)
                                    {
                                        string inv_id = string.Empty;

                                        Dictionary<string, string> cms_data = (Dictionary<string, string>)queryResult[i];

                                        Thread.CurrentThread.CurrentCulture = new CultureInfo("en-GB");
                                        inv_id = cms_data["order_id"];

                                        logger.Broadcast("inv id: " + inv_id);
                                        logger.Broadcast("Creating invoices now...");
                                        logger.Broadcast("Dynamic ATC");

                                        dynamic doc = autoCount.NewInvoice();
                                        if (enable_credit_control == 0)
                                        {
                                            doc.DisableCreditControl();
                                        }
                                        else
                                        {
                                            doc.EnableCreditControl();
                                        }

                                        doc.DebtorCode = cms_data["cust_code"];
                                        logger.Broadcast("Cust Code: " + cms_data["cust_code"]);
                                        logger.Broadcast("Passing cust_code");
                                        string order_id = order_id_format == "<<New>>" ? order_id_format : cms_data["order_id"];
                                        doc.RefDocNo = order_id_format == "<<New>>" ? cms_data["order_id"] : "";
                                        doc.DocNo = order_id;
                                        doc.DocDate = Helper.ToDateTime(cms_data["order_date_format"]);
                                        doc.Description = "Sales Invoices";
                                        doc.CurrencyRate = 1;
                                        doc.Agent = cms_data["staff_code"];
                                        logger.Broadcast("Inserting Sales Location ---> " + cms_data["warehouse_code"]);
                                        doc.SalesLocation = cms_data["warehouse_code"];

                                        string invItemQuery = "SELECT oi.order_item_id, oi.disc_1, oi.disc_2, oi.disc_3 , oi.order_id, oi.ipad_item_id, oi.product_id, oi.salesperson_remark, oi.quantity, oi.editted_quantity, oi.unit_price, oi.unit_uom,oi.attribute_remark,oi.optional_remark, oi.discount_method, oi.discount_amount, oi.sub_total, oi.sequence_no, oi.uom_id, oi.packing_status, oi.packed_by, oi.cancel_status, oi.updated_at, cms_product.*, up.product_uom_rate, up.product_min_price FROM cms_order_item AS oi LEFT JOIN cms_product ON oi.product_code = cms_product.product_code LEFT JOIN cms_product_uom_price_v2 up ON up.product_code = oi.product_code AND up.product_uom = oi.unit_uom WHERE cancel_status = 0 AND  order_id = '" + inv_id + "'";
                                        ArrayList itemList = mysql.Select(invItemQuery);

                                        ArrayList getParentFOCItem = mysql.Select("SELECT product_code, parent_code FROM cms_order_item WHERE order_id = '" + inv_id + "' AND parent_code = 'FOC' AND cancel_status = 0");
                                        ArrayList parentProdCodeList = new ArrayList();
                                        for (int ix = 0; ix < getParentFOCItem.Count; ix++)
                                        {
                                            Dictionary<string, string> each = (Dictionary<string, string>)getParentFOCItem[ix];
                                            string parentProdCode = each["product_code"];
                                            parentProdCodeList.Add(parentProdCode);
                                        }

                                        logger.Broadcast("Invoice Item Query: " + invItemQuery);
                                        logger.Broadcast("ItemListCount: " + itemList.Count);

                                        logger.Broadcast("parentProdCodeList.Count: " + parentProdCodeList.Count);

                                        ArrayList focItemList = new ArrayList();

                                        if (itemList.Count > 0)
                                        {
                                            ArrayList wh_stk_adj = new ArrayList();
                                            foreach (Dictionary<string, string> item in itemList)
                                            {
                                                decimal.TryParse(item["quantity"], out decimal quantity);
                                                decimal.TryParse(item["unit_price"], out decimal unitPrice);
                                                decimal.TryParse(item["discount_amount"], out decimal discountAmount);

                                                if (discountAmount > 0)
                                                {
                                                    discountAmount = discountAmount / (unitPrice * quantity) * 100;
                                                }

                                                decimal.TryParse(item["sub_total"], out decimal sub_total);

                                                quantity = Math.Round(quantity, 2);
                                                unitPrice = Math.Round(unitPrice, 2);
                                                discountAmount = Math.Round(discountAmount, 2);
                                                sub_total = Math.Round(sub_total, 2);

                                                string discount = discountAmount + "%";

                                                string orderItemId = item["order_item_id"];
                                                if (focItemList.Contains(orderItemId))
                                                {
                                                    //skip FOC items
                                                    goto NextItem;
                                                }

                                                logger.Broadcast("Inserting the items");
                                                dynamic dtl = autoCount.NewInvoiceDetails(doc);
                                                dtl.AccNo = acc_no;
                                                string itemCode = item["product_code"];
                                                dtl.ItemCode = itemCode;
                                                if(!onlyItemToSync.Contains(itemCode))
                                                {
                                                    onlyItemToSync.Add(item["product_code"]);
                                                }

                                                logger.Broadcast("Product code: " + item["product_code"]);
                                                logger.Broadcast("Passing product code");
                                                dtl.Description = item["product_name"];
                                                dtl.Desc2 = item["salesperson_remark"];
                                                dtl.Qty = quantity;
                                                dtl.Discount = discount;
                                                dtl.UOM = item["unit_uom"];
                                                dtl.UnitPrice = unitPrice;
                                                dtl.SubTotal = sub_total;
                                                dtl.Location = cms_data["warehouse_code"];
                                                dtl.DiscountAmt = discountAmount;
                                                string temp_remark = item["salesperson_remark"];
                                                string temp_furdesc = FormatAsRTF(temp_remark);
                                                dtl.FurtherDescription = temp_furdesc;

                                                double cloud_qty = 0;
                                                double.TryParse(item["product_uom_rate"], out cloud_qty);
                                                cloud_qty = cloud_qty * (double)quantity;

                                                string DOID = string.Empty;

                                                if (invIdToTransfer != string.Empty)
                                                {
                                                    dtl.FromDocType = "DO";
                                                    dtl.FromDocNo = DOID;
                                                }

                                                if (parentProdCodeList.Contains(itemCode))
                                                {
                                                    //get FOC item
                                                    ArrayList getFOC = mysql.Select("SELECT order_item_id, product_code, quantity FROM cms_order_item WHERE order_id = '" + inv_id + "' AND parent_code = '" + itemCode + "' AND cancel_status = 0");

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
                                                            dtl.FOCQty = __qty;

                                                            focItemList.Add(order_item_id);
                                                        }
                                                    }
                                                }

                                                wh_stk_adj.Add("UPDATE cms_warehouse_stock SET cloud_qty = cloud_qty - " + cloud_qty + " WHERE product_code = '" + item["product_code"] + "' AND wh_code = '" + cms_data["warehouse_code"] + "'");

                                            NextItem:
                                                Console.WriteLine("Next Item");
                                            }

                                            try
                                            {
                                                logger.Broadcast("Trying to save doc");
                                                doc.Save();
                                                logger.Broadcast("Save successfully");
                                                string order_reference_id = doc.DocNo;
                                                logger.Broadcast(inv_id + " created");
                                                int.TryParse(order_status, out int int_order_status);
                                                int updateOrderStatus = int_order_status + 1;
                                                string updateStatusQuery = "UPDATE cms_order SET order_status = '" + updateOrderStatus + "', order_reference = '" + order_reference_id + "' WHERE order_id = '" + cms_data["order_id"] + "'";

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

                                                mysql.Insert("INSERT INTO cms_order (order_id, order_reference) VALUES ('" + inv_id + "','" + order_reference_id + "') ON DUPLICATE KEY UPDATE order_reference = VALUES(order_reference);");
                                                for (int ixx = 0; ixx < wh_stk_adj.Count; ixx++)
                                                {
                                                    mysql.Insert(wh_stk_adj[ixx].ToString());
                                                }
                                               
                                                new JobATCWarehouseQtySync().ExecuteOnlyItem(onlyItemToSync);
                                            }
                                            catch(AutoCount.AppException ex)
                                            {
                                                logger.Broadcast("Transfer INV catch: " + ex.Message);
                                                autoCount.Message("Transfer INV catch: " + ex.Message);

                                                string str = ex.Message;
                                                Database.Sanitize(ref str);
                                                if (str.Contains("already exist"))
                                                {
                                                    int.TryParse(order_status, out int int_order_status);
                                                    int updateOrderStatus = int_order_status + 1;
                                                    mysql.Insert("UPDATE cms_order SET order_status = '" + updateOrderStatus + "' WHERE order_id = '" + inv_id + "'");
                                                }
                                                else
                                                {
                                                    mysql.Insert("UPDATE cms_order SET order_fault_message = '" + str + "' WHERE order_id = '" + inv_id + "'");
                                                    //AutoCount.AppMessage.ShowMessage(ex.Message);
                                                }
                                            }
                                        }
                                    }
                                    new JobATCStockCardSync().ExecuteSyncTodayOnly("1");
                                }
                            }
                        }
                        else
                        {
                            logger.Broadcast("Cannot connect to MYSQL Host at the moment. Kindly wait");
                            mysql.Message("Cannot connect to MYSQL Host at the moment. Kindly wait");
                        }
                    });

                    mysql_list.Clear();
                    mssql_list.Clear();

                    slog.action_identifier = Constants.Action_ATC_Transfer_INV;
                    slog.action_failure = 0;
                    slog.action_failure_message = string.Empty;
                    slog.action_time = DateTime.Now.ToString();

                    DateTime endTime = DateTime.Now;
                    TimeSpan ts = endTime - startTime;

                    LocalDB.InsertSyncLog(slog);

                    logger.message = "Transfer INV (SDK) finished in (" + ts.TotalSeconds.ToString("F") + " seconds)";
                    logger.Broadcast();

                });

                thread.Start();
                //if (socket_OrderId != string.Empty)
                //{
                //    thread.Join();
                //}
            }
            catch (ThreadAbortException e)
            {
                DpprException ex = new DpprException
                {
                    file_name = "JobATCTransferINVSDK",
                    exception = e.Message,
                    time = DateTime.Now.ToString()
                };
                LocalDB.InsertException(ex);

                Console.WriteLine(Constants.Thread_Exception + e.Message);
            }
        }
    }
}