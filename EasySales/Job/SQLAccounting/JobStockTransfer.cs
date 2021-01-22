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
using System.Net;

namespace EasySales.Job
{
    [DisallowConcurrentExecution]
    public class JobStockTransfer : IJob
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
                    slog.action_identifier = Constants.Action_Transfer_Stock;
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
                    //        logger.message = "SQLACC Transfer Stock is already running";
                    //        logger.Broadcast();
                    //        goto ENDJOB;
                    //    }
                    //}

                    LocalDB.InsertSyncLog(slog);
                    logger.message = "----------------------------------------------------------------------------";
                    logger.Broadcast();
                    logger.message = "Transfer Stock is running";
                    logger.Broadcast();
                    logger.message = "----------------------------------------------------------------------------";
                    logger.Broadcast();

                    dynamic hasUdf = new CheckBackendRule().CheckTablesExist().GetSettingByTableName("transfer_stock");
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
                    int st_status = 1;
                    int include_ext_no = 0;
                    int sync_stock_card = 0;
                    int sync_wh_stock = 0;
                    string payment_method = string.Empty;

                    if (hasUdf.Count > 0)
                    {
                        foreach (var condition in hasUdf)
                        {
                            dynamic _condition = condition.condition;

                            dynamic _st_status = _condition.st_status;
                            if (_st_status != 1)
                            {
                                st_status = _st_status;
                            }

                            dynamic _payment_method = _condition.payment_method;
                            if (_payment_method != string.Empty)
                            {
                                payment_method = _payment_method;
                            }

                            dynamic _include_ext_no = _condition.include_ext_no;
                            if (_include_ext_no != null)
                            {
                                if (_include_ext_no != string.Empty)
                                {
                                    include_ext_no = _include_ext_no;
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

                    string orderQuery = "SELECT st.*, c.cust_company_name, DATE_FORMAT(st.st_date, '%d/%m/%Y %H:%s:%i') AS order_date_format FROM cms_stock_transfer AS st LEFT JOIN cms_customer AS c ON st.cust_code = c.cust_code WHERE st_status = " +st_status+ " AND cancel_status = 0 AND st_fault = 0 ";

                    ArrayList orders = mysql.Select(orderQuery);

                    logger.Broadcast(orderQuery);
                    logger.Broadcast("Stock to transfer: " + orders.Count);

                    ArrayList stockIdList = new ArrayList();

                    int fault = 0;

                    if (orders.Count == 0)
                    {
                        logger.message = "No stock to transfer";
                        logger.Broadcast();
                    }
                    else
                    {
                        int postCount = 0;
                        string stCode = string.Empty;
                        BizObject = ComServer.BizObjects.Find("ST_XF");

                        lMainDataSet = BizObject.DataSets.Find("MainDataSet");
                        lDetailDataSet = BizObject.DataSets.Find("cdsDocDetail");

                        ArrayList onlyItemToSync = new ArrayList();

                        for (int i = 0; i < orders.Count; i++)
                        {
                            BizObject.New();

                            string post_date, Total;
                            double total;

                            Dictionary<string, string> orderObj = (Dictionary<string, string>)orders[i];
                            Total = orderObj["total"];
                            double.TryParse(Total, out double _total);
                            total = _total * 1.00;

                            post_date = Convert.ToDateTime(orderObj["st_date"]).ToString("yyyy-MM-dd");

                            stCode = orderObj["st_code"];
                            lMainDataSet.FindField("DocKey").value = -1;
                            if (include_ext_no == 1)
                            {
                                lMainDataSet.FindField("DocNo").value = "<<New>>";
                                lMainDataSet.FindField("DocNoEx").value = stCode;
                            }
                            else
                            {
                                lMainDataSet.FindField("DocNo").value = stCode;
                            }

                            lMainDataSet.FindField("DocDate").value = post_date;
                            lMainDataSet.FindField("PostDate").value = post_date;
                            lMainDataSet.FindField("Code").AsString = orderObj["cust_code"];
                            lMainDataSet.FindField("CompanyName").AsString = orderObj["cust_company_name"];
                            lMainDataSet.FindField("Description").AsString = "Stock Transfer";

                            string orderItemQuery = "SELECT * FROM cms_stock_transfer_dtl WHERE cancel_status = 0 AND st_code = '" + stCode + "'";

                            ArrayList orderItems = mysql.Select(orderItemQuery);

                            string itemCodeStr = string.Empty;

                            for (int idx = 0; idx < orderItems.Count; idx++)
                            {
                                string uomrate, qty, sub_total, discount, del_date;
                                int sqty;
                                int sequence_no = 0;

                                string itemCode = string.Empty;

                                Dictionary<string, string> item = (Dictionary<string, string>)orderItems[idx];

                                lDetailDataSet.Append();

                                qty = item["quantity"];
                                int.TryParse(qty, out int Qty);

                                lDetailDataSet.FindField("DtlKey").value = -1;
                                lDetailDataSet.FindField("DocKey").value = -1;
                                lDetailDataSet.FindField("DtlKey").value = -1;
                                
                                try
                                {
                                    lDetailDataSet.FindField("ItemCode").value = item["product_code"];
                                    itemCode = item["product_code"];
                                    onlyItemToSync.Add(itemCode);
                                }
                                catch (Exception e)
                                {
                                    Console.WriteLine(e.Message);
                                    string productCode = item["product_code"];
                                    string unitUom = item["unit_uom"];

                                    Database.Sanitize(ref productCode);
                                    Database.Sanitize(ref unitUom);

                                    mysql.Insert("UPDATE cms_stock_transfer SET st_fault = '2', st_fault_message = 'Invalid Item code(" + itemCode + ")' WHERE st_code = '" + stCode + "'");
                                    logger.Broadcast("["+ stCode + "] Invalid Item code(" + itemCode + ")");

                                    fault++;
                                }

                                try
                                {
                                    lDetailDataSet.FindField("UOM").value = item["unit_uom"];
                                }
                                catch (Exception)
                                {
                                    string productCode = item["product_code"];
                                    string unitUom = item["unit_uom"];

                                    Database.Sanitize(ref productCode);
                                    Database.Sanitize(ref unitUom);

                                    mysql.Insert("UPDATE cms_stock_transfer SET st_fault = '2', st_fault_message = 'Invalid UOM(" + unitUom + ")' WHERE st_code = '" + stCode + "'");
                                    logger.Broadcast("[" + stCode + "] Invalid UOM(" + unitUom + ")");

                                    fault++;
                                }

                                lDetailDataSet.FindField("Description").AsString = item["product_name"];
                                lDetailDataSet.FindField("QTY").value = item["quantity"];
                                lDetailDataSet.FindField("FromLocation").AsString = item["from_location"];
                                lDetailDataSet.FindField("ToLocation").AsString = item["to_location"];
                                //lDetailDataSet.FindField("REMARK1").value = item["salesperson_remark"];

                                sub_total = item["sub_total"];

                                lDetailDataSet.FindField("QTY").value = qty;
                                lDetailDataSet.FindField("Amount").value = sub_total;
                                lDetailDataSet.FindField("LocalAmount").value = sub_total;
                                lDetailDataSet.FindField("UnitPrice").value = item["unit_price"];

                                lDetailDataSet.Post();
                            }

                            total = _total;
                            lMainDataSet.Post();

                            if (fault == 0)
                            {
                                try
                                {
                                    BizObject.Save();

                                    stockIdList.Add(stCode); //successfully transfer to SQLAcc

                                    int updateStStatus = st_status + 1;
                                    string updateOrderStatusQuery = "UPDATE cms_stock_transfer SET st_status = '" + updateStStatus + "' WHERE st_code = '" + stCode + "'";
                                    mysql.Insert(updateOrderStatusQuery);
                                    logger.message = stCode + " created";
                                    logger.Broadcast();

                                    BizObject.Close();

                                    if (sync_stock_card == 1)
                                    {
                                        string fieldToGet = include_ext_no == 1 ? "DOCNOEX = '" + orderObj["st_code"] + "'" : "DOCNO = '" + orderObj["st_code"] + "'";
                                        string stockCardQuery = "SELECT ST_TR.*, ST_XFDTL.UOM FROM ST_TR LEFT JOIN ST_XFDTL ON(ST_TR.DTLKEY = ST_XFDTL.DTLKEY) WHERE ST_TR.DOCTYPE = 'XF' AND ST_TR.DOCKEY = (SELECT DOCKEY FROM ST_XF WHERE " + fieldToGet + ")";
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

                                                logger.message = string.Format("[Stock Transfer] : {0} stock card records is inserted", RecordCount);
                                                logger.Broadcast();
                                            }

                                            new JobStockCardSync().ExecuteSyncTodayOnly("1");
                                        }
                                        catch (Exception exx)
                                        {
                                            try
                                            {
                                                Console.WriteLine(exx.Message);
                                                logger.Broadcast("[Sync Stock Card] " + exx.Message);
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

                                                logger.Broadcast("[Sync Stock Card] " + exc.Message);
                                            }
                                        }
                                    }

                                    if (sync_wh_stock == 1)
                                    {
                                        //new JobWhStockSync().Execute();
                                        new JobWhStockSync().ExecuteOnlyItem(onlyItemToSync);
                                    }

                                }
                                catch (Exception e)
                                {   
                                    if (e.Message.IndexOf("duplicate") != -1)
                                    {
                                        mysql.Insert("UPDATE cms_stock_transfer SET st_fault = '0', st_fault_message = 'Order ID duplicated' WHERE st_code = '" + stCode + "'");
                                        logger.Broadcast("[Duplicate] Stock Transfer is already in SQLAcc.");
                                    }
                                    else if (e.Message.IndexOf("limit") != -1)
                                    {
                                        mysql.Insert("UPDATE cms_stock_transfer SET st_fault = '4', st_fault_message = 'Customer credit limit exceeded' WHERE st_code = '" + stCode + "'");
                                        logger.Broadcast("[Credit Limit] Customer credit limit exceeded");
                                    }
                                    else if (e.Message.IndexOf("customer") != -1)
                                    {
                                        mysql.Insert("UPDATE cms_stock_transfer SET st_fault = '1', st_fault_message = 'Invalid Customer Code' WHERE st_code = '" + stCode + "'");
                                        logger.Broadcast("Invalid Customer Code");
                                    }
                                    else
                                    {
                                        mysql.Insert("UPDATE cms_stock_transfer SET st_fault = '1', st_fault_message = '" + e.Message + "' WHERE st_code = '" + stCode + "'");
                                        logger.Broadcast("[FAILED!] " + e.Message +"");
                                    }
                                }
                            }
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

                    Task.Delay(10000);

                    List<DpprMySQLconfig> mysql_list = LocalDB.GetRemoteDatabaseConfig().Distinct().ToList();
                    DpprMySQLconfig mysql_config = mysql_list[0];
                    string _companyName = mysql_config.config_database;
                    string companyName = _companyName.ReplaceAll("", "easysale_");
                    companyName = companyName.ReplaceAll("", "easyicec_");

                    if (companyName == "ibeauty")
                    {
                        companyName = "insidergroup";
                    }
                    else
                    {
                        companyName = companyName;
                    }

                    for (int i = 0; i < stockIdList.Count; i++)
                    {
                        string stock_id = stockIdList[i].ToString();
                        var _url = string.Format("https://easysales.asia/esv2/webview/iManage/stkTransferNotf.php?stock_id={0}&client={1}", stock_id, companyName);
                        logger.Broadcast("API url: " + _url);
                        using (var webClient = new WebClient())
                        {
                            var response = webClient.DownloadString(_url);
                            logger.Broadcast("Notification sent to the salesman!");
                            Console.WriteLine(response);
                        }
                    }

                    LocalDB.InsertSyncLog(slog);

                    logger.message = "Transfer Stock finished in (" + ts.TotalSeconds.ToString("F") + " seconds)";
                    logger.Broadcast();

                });

                thread.Start();
            }
            catch (ThreadAbortException e)
            {
                DpprException ex = new DpprException
                {
                    file_name = "JobTransferStock",
                    exception = e.Message,
                    time = DateTime.Now.ToString()
                };
                LocalDB.InsertException(ex);

                Console.WriteLine(Constants.Thread_Exception + e.Message);
            }
        }
    }
}