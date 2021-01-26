//extern alias ATCARAP;
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
using AutoCount.Invoicing.Sales.CashSale;
using AutoCount.ARAP.ARPayment;

namespace EasySales.Job
{
    [DisallowConcurrentExecution]
    public class JobATCTransferCSSDK : IJob
    {
        private ATC_Connection connection = null;
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

                    DpprUserSettings atcSdkSetting = LocalDB.GetParticularSetting(Constants.Setting_ATCv2);
                    bool isATCv2 = atcSdkSetting.setting == Constants.YES;

                    logger.Broadcast("isATCv2: " + isATCv2);

                    DpprSyncLog slog = new DpprSyncLog();
                    slog.action_identifier = Constants.Action_ATC_Transfer_CS;
                    slog.action_failure = 0;
                    slog.action_failure_message = string.Empty;
                    slog.action_time = DateTime.Now.ToString();

                    DateTime startTime = DateTime.Now;

                    LocalDB.InsertSyncLog(slog);

                    logger.message = "ATC Transfer CS (SDK) is running";
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
                        string order_id_format = "<<New>>";

                        CheckBackendRule checkDB = new CheckBackendRule(mysql: mysql);
                        dynamic jsonRule = checkDB.CheckTablesExist().GetSettingByTableName("transfer_cs_atc");

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
                                    }
                                }
                            }

                            //this.connection = ATC_Configuration.Init_config();
                            //this.connection.dBSetting = AutoCountV1.PerformAuth(ref this.connection);

                            string SOQuery = "SELECT o.*,DATE_FORMAT(o.order_date,'%d/%m/%Y %H:%s:%i') AS order_date_format,l.staff_code,cms_customer_branch.branch_name FROM cms_order AS o LEFT JOIN cms_customer AS c ON o.cust_id = c.cust_id LEFT JOIN cms_login AS l ON o.salesperson_id = l.login_id LEFT JOIN cms_customer_branch ON cms_customer_branch.branch_code = o.branch_code WHERE o.order_status = " + order_status + " AND cancel_status = 0 AND doc_type = 'cash' AND order_fault = 0 ";
                            //order_id = "\"SO-SS-001\",\"SO-SS-003\",\"SO-SS-002\"";
                            socket_OrderId = socket_OrderId.Replace("\"", "\'");
                            string socketQuery = "AND order_id IN (" + socket_OrderId + ")";

                            SOQuery = SOQuery + (socket_OrderId != string.Empty ? socketQuery : "");

                            ArrayList queryResult = mysql.Select(SOQuery);
                            mysql.Message(SOQuery);

                            logger.Broadcast("Total CS to be transferred: " + queryResult.Count);

                            ATChandler autoCount = new ATChandler(isATCv2);
                            logger.Broadcast("Getting User Session -> " + (AutoCountV2.TriggerConnection() == true).ToString());
                            this.connection = autoCount.PerformAuth();
                            logger.Broadcast("Succesfully getting the user session");

                            if (queryResult.Count == 0)
                            {
                                logger.message = "No CS to be transferred";
                                logger.Broadcast();
                            }
                            else
                            {
                                if (autoCount.PerformAuthInAutoCount())
                                {
                                    logger.Broadcast("Login with AutoCount is successful");
                                    logger.Broadcast("Inserting Cash Sales");

                                    for (int i = 0; i < queryResult.Count; i++)
                                    {
                                        string cash_id = string.Empty;

                                        Dictionary<string, string> cms_data = (Dictionary<string, string>)queryResult[i];
                                        //CashSaleCommand command = CashSaleCommand.Create(this.connection.dBSetting);
                                        //CashSale addCashDoc = command.AddNew();
                                        //CashSaleDetail addCashDocDetail;

                                        dynamic addCashDoc = autoCount.NewCashSales();

                                        Thread.CurrentThread.CurrentCulture = new CultureInfo("en-GB");
                                        cash_id = cms_data["order_id"];

                                        string order_id = order_id_format == "<<New>>" ? order_id_format : cms_data["order_id"];
                                        addCashDoc.DocNo = order_id;
                                        addCashDoc.RefDocNo = order_id_format == "<<New>>" ? cms_data["order_id"] : "";
                                        addCashDoc.DocDate = Helper.ToDateTime(cms_data["order_date_format"]);

                                        addCashDoc.DebtorCode = cms_data["cust_code"];
                                        addCashDoc.DebtorName = cms_data["cust_company_name"];
                                        addCashDoc.Agent = cms_data["staff_code"];


                                        addCashDoc.InvAddr1 = cms_data["billing_address1"];
                                        addCashDoc.InvAddr2 = cms_data["billing_address2"];
                                        addCashDoc.InvAddr3 = cms_data["billing_address3"];
                                        addCashDoc.InvAddr4 = cms_data["billing_address4"];

                                        addCashDoc.Phone1 = cms_data["cust_tel"];
                                        addCashDoc.Fax1 = cms_data["cust_fax"];

                                        addCashDoc.DeliverAddr1 = cms_data["shipping_address1"];
                                        addCashDoc.DeliverAddr2 = cms_data["shipping_address2"];
                                        addCashDoc.DeliverAddr3 = cms_data["shipping_address3"];
                                        addCashDoc.DeliverAddr4 = cms_data["shipping_address4"];
                                        addCashDoc.DeliverPhone1 = cms_data["cust_tel"];
                                        addCashDoc.DeliverFax1 = cms_data["cust_fax"];
                                        addCashDoc.DeliverContact = cms_data["cust_fax"];
                                        addCashDoc.SalesLocation = cms_data["warehouse_code"];
                                        addCashDoc.Ref = cms_data["cust_reference"];
                                        addCashDoc.IsRoundAdj = false;

                                        ArrayList itemList = mysql.Select("SELECT oi.order_item_id, oi.disc_1, oi.disc_2, oi.disc_3 , oi.order_id, oi.ipad_item_id, oi.product_id, oi.salesperson_remark, oi.quantity, oi.editted_quantity, oi.unit_price, oi.unit_uom,oi.attribute_remark,oi.optional_remark, oi.discount_method, oi.discount_amount, oi.sub_total, oi.sequence_no, oi.uom_id, oi.packing_status, oi.packed_by, oi.cancel_status, oi.updated_at, cms_product.*, up.product_uom_rate, up.product_min_price FROM cms_order_item AS oi LEFT JOIN cms_product ON oi.product_code = cms_product.product_code LEFT JOIN cms_product_uom_price_v2 up ON up.product_code = oi.product_code AND up.product_uom = oi.unit_uom WHERE cancel_status = 0 AND  order_id = '" + cash_id + "'");

                                        logger.Broadcast("ItemList: " + itemList.Count);
                                        decimal paymentAmount = 0;

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

                                                paymentAmount = paymentAmount + sub_total;

                                                quantity = Math.Round(quantity, 2);
                                                unitPrice = Math.Round(unitPrice, 2);
                                                discountAmount = Math.Round(discountAmount, 2);
                                                sub_total = Math.Round(sub_total, 2);

                                                string discount = discountAmount + "%";

                                                dynamic addCashDocDetail = autoCount.NewCashSalesDetails(addCashDoc);

                                                //addCashDocDetail = addCashDoc.AddDetail();
                                                addCashDocDetail.AccNo = "500-0000";
                                                addCashDocDetail.ItemCode = item["product_code"];
                                                addCashDocDetail.FurtherDescription = item["product_remark"];
                                                addCashDocDetail.Description = item["product_name"];
                                                addCashDocDetail.Desc2 = item["salesperson_remark"];
                                                addCashDocDetail.Qty = quantity;
                                                addCashDocDetail.Discount = discount;
                                                addCashDocDetail.UOM = item["unit_uom"];
                                                addCashDocDetail.UnitPrice = unitPrice;
                                                addCashDocDetail.SubTotal = sub_total;
                                                addCashDocDetail.Location = cms_data["warehouse_code"];
                                                //addCashDocDetail.ProjNo = "HQ"; ///cms_data["proj_no"];
                                                addCashDocDetail.DiscountAmt = discountAmount;

                                                double cloud_qty = 0;
                                                double.TryParse(item["product_uom_rate"], out cloud_qty);
                                                cloud_qty = cloud_qty * (double)quantity;

                                                wh_stk_adj.Add("UPDATE cms_warehouse_stock SET cloud_qty = cloud_qty - " + cloud_qty + " WHERE product_code = '" + item["product_code"] + "' AND wh_code = '" + cms_data["warehouse_code"] + "'");
                                            }

                                            // Payment
                                            logger.Broadcast("Payment Section");
                                            addCashDoc.PaymentMode = 1;
                                            addCashDoc.CCApprovalCode = DBNull.Value.ToString(); //DBNULL error
                                            logger.Broadcast("Passing DBNULL");
                                            addCashDoc.CashSalePayment.ARPayment.ClearDetails();

                                            if (isATCv2)
                                            {
                                                addCashDoc.CashSalePayment = AutoCount.Invoicing.Sales.SalesPayment.Create(addCashDoc.ReferPaymentDocKey, addCashDoc.DocKey, "CS", this.connection.userSession, this.connection.userSession.DBSetting);
                                            }
                                            else
                                            {
                                                addCashDoc.CashSalePayment = AutoCount.Invoicing.Sales.SalesPayment.Create(
                                                                            addCashDoc.ReferPaymentDocKey, addCashDoc.DocKey,
                                                                            AutoCount.Document.DocumentType.CashSale, this.connection.userSession, connection.dBSetting);
                                            }

                                            addCashDoc.CashSalePayment.DebtorCode = addCashDoc.DebtorCode;
                                            addCashDoc.CashSalePayment.CurrencyCode = addCashDoc.CurrencyCode;
                                            addCashDoc.CashSalePayment.DocDate = addCashDoc.DocDate;

                                            if (isATCv2)
                                            {
                                                logger.Broadcast("insert atcv2 payment here");
                                                logger.Broadcast("commented bcs 2 dlls files");
                                                //ARPaymentDTLEntity payDtl = addCashDoc.CashSalePayment.ARPayment.NewDetail();
                                                ////AutoCount.ARAP.ARPayment.ARPaymentDTLEntity payDtl = addCashDoc.CashSalePayment.ARPayment.NewDetail();
                                                //payDtl.PaymentMethod = "CASH";
                                                //payDtl.PaymentAmt = paymentAmount;

                                                //if (addCashDoc.CashSalePayment.PaymentAmt > 0)
                                                //{
                                                //    addCashDoc.ReferPaymentDocKey = addCashDoc.CashSalePayment.DocKey;
                                                //}
                                            }
                                            else
                                            {
                                                AutoCount.ARAP.ARPayment.ARPaymentDTLEntity payDtl = addCashDoc.CashSalePayment.ARPayment.NewDetail();
                                                //"CASH" must be maintained
                                                //in General Maintenance | Payment Method Maintenance
                                                payDtl.PaymentMethod = "CASH";
                                                payDtl.PaymentAmt = paymentAmount;

                                                if (addCashDoc.CashSalePayment.PaymentAmt > 0)
                                                {
                                                    addCashDoc.ReferPaymentDocKey = addCashDoc.CashSalePayment.DocKey;
                                                }
                                            }

                                            try
                                            {
                                                addCashDoc.Save();
                                                string order_reference_id = addCashDoc.DocNo;
                                                logger.Broadcast(cash_id + " created");
                                                int.TryParse(order_status, out int int_order_status);
                                                int updateOrderStatus = int_order_status + 1;
                                                string updateStatusQuery = "UPDATE cms_order SET order_status = '" + updateOrderStatus + "', order_reference = '" + order_reference_id + "' WHERE order_id = '" + cms_data["order_id"] + "'";

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
                                                mysql.Insert("INSERT INTO cms_order (order_id, order_reference) VALUES ('" + cash_id + "','" + order_reference_id + "') ON DUPLICATE KEY UPDATE order_reference = VALUES(order_reference);");
                                                for (int ixx = 0; ixx < wh_stk_adj.Count; ixx++)
                                                {
                                                    mysql.Insert(wh_stk_adj[ixx].ToString());
                                                }

                                                new JobATCStockCardSync().ExecuteSyncTodayOnly("1");

                                            }
                                            catch (Exception ex)
                                            {
                                                logger.Broadcast("Transfer CS catch: " + ex.Message);
                                                autoCount.Message("Transfer CS catch: " + ex.Message);
                                            }
                                        }
                                    }
                                }
                            }
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

                    logger.message = "Transfer CS (SDK) finished in (" + ts.TotalSeconds.ToString("F") + " seconds)";
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
                    file_name = "JobATCTransferCSSDK",
                    exception = e.Message,
                    time = DateTime.Now.ToString()
                };
                LocalDB.InsertException(ex);

                Console.WriteLine(Constants.Thread_Exception + e.Message);
            }
        }
    }
}