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
using AutoCount.Stock;
using System.Net;

namespace EasySales.Job
{
    [DisallowConcurrentExecution]
    public class JobATCTransferStockSDK : IJob
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

                    logger.Broadcast("atcSdkSetting");

                    DpprUserSettings atcSdkSetting = LocalDB.GetParticularSetting(Constants.Setting_ATCv2);

                    bool isATCv2 = atcSdkSetting.setting == Constants.YES;

                    logger.Broadcast("isATCv2: " + isATCv2);

                    DpprSyncLog slog = new DpprSyncLog();
                    slog.action_identifier = Constants.Action_ATC_Transfer_Stock;
                    slog.action_failure = 0;
                    slog.action_failure_message = string.Empty;
                    slog.action_time = DateTime.Now.ToString();

                    DateTime startTime = DateTime.Now;

                    LocalDB.InsertSyncLog(slog);

                    logger.message = "ATC Transfer Stock (SDK) is running";
                    logger.Broadcast();

                    List<DpprMySQLconfig> mysql_list = LocalDB.GetRemoteDatabaseConfig().Distinct().ToList();
                    List<DpprSQLServerconfig> mssql_list = LocalDB.GetRemoteSQLServerConfig();

                    ArrayList stockIdList = new ArrayList();

                    mysql_list.Iterate<DpprMySQLconfig>((mysql_config, index) =>
                    {
                        DpprMySQLconfig mysqlconfig = mysql_list[index];
                        Database mysql = new Database();
                        mysql.Connect(index: index);

                        string targetDBname = string.Empty;
                        string stStatus = string.Empty;
                        int sync_stock_card = 0;
                        int include_ext_no = 0;

                        CheckBackendRule checkDB = new CheckBackendRule(mysql: mysql);
                        dynamic jsonRule = checkDB.CheckTablesExist().GetSettingByTableName("transfer_stock_atc");

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
                                        stStatus = db.st_status;
                                    }
                                }
                            }

                            //this.connection = ATC_Configuration.Init_config();
                            //this.connection.dBSetting = AutoCountV1.PerformAuth(ref this.connection);

                            string STQuery = "SELECT *, DATE_FORMAT(st_date, '%d/%m/%Y %H:%s:%i') AS order_date_format FROM cms_stock_transfer WHERE st_status = " + stStatus + " AND cancel_status = 0 AND st_fault = 0 ";
                            //order_id = "\"SO-SS-001\",\"SO-SS-003\",\"SO-SS-002\"";
                            socket_OrderId = socket_OrderId.Replace("\"", "\'");
                            string socketQuery = "AND st_code IN (" + socket_OrderId + ")";

                            STQuery = STQuery + (socket_OrderId != string.Empty ? socketQuery : "");

                            ArrayList queryResult = mysql.Select(STQuery);
                            mysql.Message(STQuery);

                            logger.Broadcast("Total stock to be transferred: " + queryResult.Count);

                            ATChandler autoCount = new ATChandler(isATCv2);
                            this.connection = autoCount.PerformAuth();

                            logger.Broadcast("Succesfully getting the user session");
                            ArrayList onlyItemToSync = new ArrayList();

                            if (queryResult.Count == 0)
                            {
                                logger.message = "No stock to be transferred";
                                logger.Broadcast();
                            }
                            else
                            {
                                logger.Broadcast("Try to login to AutoCount");
                                if (AutoCountV1.PerformAuthInAutoCount(this.connection))
                                {
                                    logger.Broadcast("Login with AutoCount is successful");
                                    logger.Broadcast("Transferring Stock");

                                    for (int i = 0; i < queryResult.Count; i++)
                                    {
                                        string stCode = string.Empty;

                                        Dictionary<string, string> cms_data = (Dictionary<string, string>)queryResult[i];
                                        logger.Broadcast("Transfer Stock data found");
                                        try
                                        {
                                            AutoCount.Stock.StockTransfer.StockTransferCommand cmd =
        AutoCount.Stock.StockTransfer.StockTransferCommand.Create(this.connection.userSession, this.connection.userSession.DBSetting);
                                            AutoCount.Stock.StockTransfer.StockTransfer doc = cmd.AddNew();
                                            AutoCount.Stock.StockTransfer.StockTransferDetail dtl = null;

                                            stCode = cms_data["st_code"];
                                            //doc.DocNo = "<<New>>";
                                            doc.DocNo = stCode;
                                            doc.DocDate = Helper.ToDateTime(cms_data["order_date_format"]);
                                            doc.Description = "STOCK TRANSFER";
                                            doc.FromLocation = cms_data["from_location"];
                                            doc.ToLocation = cms_data["to_location"];
                                            doc.Reason = cms_data["note"];

                                            //Set to auto populate the item information from Item Maintenance
                                            //such as Item Description
                                            doc.EnableAutoLoadItemDetail = true;

                                            string normalItem = "SELECT * FROM cms_stock_transfer_dtl WHERE cancel_status = 0 AND st_code = '" + cms_data["st_code"] + "'";
                                            ArrayList allItem = mysql.Select(normalItem);

                                            if (allItem.Count > 0)
                                            {
                                                logger.Broadcast("Inserting normal item");

                                                foreach (Dictionary<string, string> cms_data_item in allItem)
                                                {
                                                    logger.Broadcast("Trying to transfer order item: " + cms_data_item["product_code"]);
                                                    decimal.TryParse(cms_data_item["quantity"], out decimal quantity);
                                                    decimal.TryParse(cms_data_item["unit_price"], out decimal unitPrice);

                                                    dtl = doc.AddDetail();
                                                    dtl.ItemCode = cms_data_item["product_code"];
                                                    onlyItemToSync.Add(cms_data_item["product_code"]);
                                                    dtl.UOM = cms_data_item["unit_uom"];
                                                    dtl.Qty = quantity;
                                                }
                                            }

                                            try
                                            {
                                                logger.Broadcast("Trying to create Stock Transfer");
                                                doc.Save();
                                                //log success
                                                stockIdList.Add(stCode);
                                                logger.Broadcast("Stock Transfer [" + stCode + "] created");
                                                int.TryParse(stStatus, out int int_order_status);
                                                int updateStStatus = int_order_status + 1;
                                                string updateStatusQuery = "UPDATE cms_stock_transfer SET st_status = '" + updateStStatus + "' WHERE st_code = '" + stCode + "'";

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

                                                new JobATCStockCardSync().ExecuteSyncTodayOnly("1");
                                                new JobATCWarehouseQtySync().ExecuteOnlyItem(onlyItemToSync);
                                            }
                                            catch (AutoCount.AppException ex)
                                            {
                                                //log error
                                                logger.Broadcast("Fail to create new Stock Transfer [" + stCode + "]: " + ex.Message);
                                                AutoCountV1.Message("Fail to create new Stock Transfer.\n" + ex.Message);

                                                if (ex.Message == "Primary Key Error")
                                                {
                                                    int.TryParse(stStatus, out int int_order_status);
                                                    int updateStStatus = int_order_status + 1;
                                                    string updateStatusQuery = "UPDATE cms_stock_transfer SET st_status = '" + updateStStatus + "' WHERE st_code = '" + stCode + "'";

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
                                                        logger.Broadcast("[" + stCode + "] already created. Kindly check in the AutoCount");
                                                        AutoCountV1.Message("[" + stCode + "] already created. Kindly check in the AutoCount");
                                                    }
                                                }
                                                else
                                                {
                                                    mysql.Insert("UPDATE cms_stock_transfer SET st_fault_message = '" + ex.Message + "' WHERE st_code = '" + stCode + "'");
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

                    slog.action_identifier = Constants.Action_ATC_Transfer_Stock;
                    slog.action_failure = 0;
                    slog.action_failure_message = string.Empty;
                    slog.action_time = DateTime.Now.ToString();

                    DateTime endTime = DateTime.Now;
                    TimeSpan ts = endTime - startTime;
                    //Console.WriteLine("Completed in: " + ts.TotalSeconds.ToString("F"));

                    LocalDB.InsertSyncLog(slog);

                    logger.message = "Transfer Stock (SDK) finished in (" + ts.TotalSeconds.ToString("F") + " seconds)";
                    logger.Broadcast();

                    Task.Delay(10000);

                    List<DpprMySQLconfig> _mysql_list = LocalDB.GetRemoteDatabaseConfig().Distinct().ToList();
                    DpprMySQLconfig _mysql_config = _mysql_list[0];
                    string _companyName = _mysql_config.config_database;
                    string companyName = _companyName.ReplaceAll("", "easysale_");
                    companyName = companyName.ReplaceAll("", "easyicec_");

                    companyName = companyName == "chillmjb" ? companyName = "chillm_jb" : companyName;
                    companyName = companyName == "chillmkdh" ? companyName = "chillm_kdh" : companyName;
                    companyName = companyName == "ibeauty" ? companyName = "insidergroup" : companyName;

                    for (int ixx = 0; ixx < stockIdList.Count; ixx++)
                    {
                        string stock_id = stockIdList[ixx].ToString();
                        var _url = string.Format("https://easysales.asia/esv2/webview/iManage/stkTransferNotf.php?stock_id={0}&client={1}", stock_id, companyName);
                        logger.Broadcast("API url: " + _url);
                        using (var webClient = new WebClient())
                        {
                            var response = webClient.DownloadString(_url);
                            logger.Broadcast("Notification sent to the salesman!");
                            Console.WriteLine(response);
                        }
                    }
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
                    file_name = "JobATCTransferStockSDK",
                    exception = e.Message,
                    time = DateTime.Now.ToString()
                };
                LocalDB.InsertException(ex);

                Console.WriteLine(Constants.Thread_Exception + e.Message);
            }
        }
    }
}