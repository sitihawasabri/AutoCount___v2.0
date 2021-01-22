using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using EasySales.Model;
using EasySales.Object;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Org.BouncyCastle.Asn1;
using Quartz;


namespace EasySales.Job
{
    [DisallowConcurrentExecution]
    public class JobAPSTransferCN : IJob
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

                    int RecordCount = 0;
                    GlobalLogger logger = new GlobalLogger();

                    DpprSyncLog slog = new DpprSyncLog();
                    slog.action_identifier = Constants.Action_APS_Transfer_CN;
                    slog.action_failure = 0;
                    slog.action_failure_message = string.Empty;
                    slog.action_time = DateTime.Now.ToString();

                    DateTime startTime = DateTime.Now;

                    LocalDB.InsertSyncLog(slog);

                    logger.message = "APS Transfer CN is running";
                    logger.Broadcast();

                    List<DpprMySQLconfig> mysql_list = LocalDB.GetRemoteDatabaseConfig().Distinct().ToList();
                    List<DpprSQLServerconfig> mssql_list = LocalDB.GetRemoteSQLServerConfig();

                    mysql_list.Iterate<DpprMySQLconfig>((mysql_config, index) =>
                    {
                        Database mysql = new Database();
                        mysql.Connect(index: index);

                        string targetDBname = string.Empty;

                        CheckBackendRule checkDB = new CheckBackendRule(mysql: mysql);
                        dynamic jsonRule = checkDB.CheckTablesExist().GetSettingByTableName("transfer_cn");

                        ArrayList mssql_rule = new ArrayList();
                        if (jsonRule != null)
                        {
                            foreach (var key in jsonRule)
                            {
                                if (key.mssql != null && key.mssql.GetType().ToString() == "Newtonsoft.Json.Linq.JArray")
                                {
                                    var mssql_rules = key.mssql;
                                    foreach (var db in mssql_rules)
                                    {
                                        targetDBname = db.name;
                                    }
                                }
                            }
                        }
                        else
                        {
                            throw new Exception("APS Transfer CN sync requires backend rules");
                        }
                        
                        string CNQuery = "SELECT cms_order.cust_code, cms_order.order_date, cms_order.order_id, cms_order.order_delivery_note, cms_login.staff_code FROM cms_order LEFT JOIN cms_login ON cms_login.login_id = cms_order.salesperson_id WHERE order_status = 2 AND cancel_status = 0 AND doc_type = 'credit'";
                        ArrayList queryResult = mysql.Select(CNQuery);

                        if (queryResult.Count == 0)
                        {
                            logger.message = "No CN to transfer";
                            logger.Broadcast();
                        }
                        else
                        {
                            SQLServer mssql = new SQLServer();
                            mssql.Connect(targetDBname);

                            string order, custCode, orderId, orderDeliveryNote, staffCode, orderDate, salespersonQuery, customerQuery, runningQuery;

                            for (int i = 0; i < queryResult.Count; i++)
                            {
                                Dictionary<string, string> orderObj = (Dictionary<string, string>)queryResult[i];
                                ArrayList cnItems = mysql.Select("SELECT cms_order.cust_code, cms_order.order_date, cms_order.order_id, cms_order.order_delivery_note, cms_login.staff_code FROM cms_order LEFT JOIN cms_login ON cms_login.login_id = cms_order.salesperson_id WHERE order_id = '" + orderObj["orderId"] + "'");
                                //get cn items for related id

                                for (int ixx = 0; ixx < cnItems.Count; ixx++)
                                {
                                    Dictionary<string, string> objQuo = (Dictionary<string, string>)cnItems[ixx];

                                    string custId = "0";
                                    string salespersonId = "0";
                                    string runningId = "0";

                                    custCode = orderObj["cust_code"];
                                    orderId = orderObj["order_id"];
                                    orderDeliveryNote = orderObj["order_delivery_note"];
                                    staffCode = orderObj["staff_code"];
                                    orderDate = orderObj["order_date"];

                                    salespersonQuery = "SELECT intUserID FROM Adm_UserTbl WHERE charUserID = '" + orderObj["staff_code"] + "'";
                                    ArrayList salespersonList = mssql.Select(salespersonQuery);

                                    Dictionary<string, string> salespersonObj = (Dictionary<string, string>)salespersonList[0];
                                    salespersonId = salespersonObj["intUserID"];

                                    runningQuery = "SELECT MAX(intPendingID)+1 AS seq FROM Inv_PendingTbl";
                                    ArrayList runningList = mssql.Select(runningQuery);

                                    Dictionary<string, string> runningObj = (Dictionary<string, string>)runningList[0];
                                    runningId = runningObj["seq"];

                                    customerQuery = "SELECT intCustID FROM Sal_CustomerTbl WHERE charRef = '" + orderObj["cust_code"] + "'";
                                    ArrayList custIdList = mssql.Select(customerQuery);

                                    Dictionary<string, string> custObj = (Dictionary<string, string>)custIdList[0];
                                    custId = custObj["intCustID"];

                                    string cnQuery = "INSERT INTO Inv_PendingTbl(intPendingID, intCustID, dtPendingDate, dtPendingDateInitial, varTrxNo, varRemarks, varRefNo, charStatus, intAutoIncrementNo, varDeleteReason, blnIsDelete, intModifyBy, dtModifyDate, intCreatedBy, dtCreatedDate, intApproveBy) VALUES ";

                                    string Values = string.Format("('{0}', '{1}', '{2}', '{3}', '{4}', '{5}', '{6}', '{7}', '{8}', '{9}', '{10}', '{11}', '{12}', '{13}', '{14}', '{15}')", runningId, custId, orderDate, orderDate, orderId, orderDeliveryNote, "", "P", 1, "", "FALSE", salespersonId, orderDate, salespersonId, orderDate, 13);

                                    cnQuery = cnQuery + string.Join(", ", Values);
                                    mssql.Insert(cnQuery);

                                    string cnDetailsQuery = "SELECT cms_order_item.product_code, cms_order_item.quantity, cms_order_item.unit_price, cms_order.warehouse_code, cms_order.order_date FROM cms_order_item LEFT JOIN cms_order ON cms_order.order_id = cms_order_item.order_id";

                                    ArrayList cnDetailsItem = mysql.Select(cnDetailsQuery);

                                    for (int ix = 0; ix < cnDetailsItem.Count; ix++)
                                    {
                                        Dictionary<string, string> item = (Dictionary<string, string>)cnDetailsItem[ix];

                                        string productCode, qty, warehouseCode, orderItemDate, unitPrice, runningStmt;
                                        string _intPendingDetailsID = string.Empty;
                                        string productId = string.Empty;
                                        string productDesc = string.Empty;
                                        string productModel = string.Empty;

                                        productCode = item["product_code"];
                                        qty = item["quantity"];
                                        unitPrice = item["unit_price"];
                                        warehouseCode = item["warehouse_code"];
                                        orderItemDate = item["order_date"];

                                        int j = 0;
                                        j = ix + 1;

                                        string stmtProd = "SELECT intInvID, varDesc, varModel FROM Inv_StockTbl WHERE charItemCode ='" + productCode + "'";
                                        ArrayList productList = mssql.Select(stmtProd);

                                        Dictionary<string, string> objProd = (Dictionary<string, string>)productList[0];

                                        productId = objProd["intInvID"];
                                        productDesc = objProd["varDesc"];
                                        productModel = objProd["varModel"];

                                        productDesc = productDesc.Replace("'", "''");
                                        productModel = productModel.Replace("'", "''");

                                        runningStmt = "SELECT MAX(intPendingDetailsID) AS seq FROM Inv_PendingDetailsTbl";
                                        ArrayList runningStmtList = mssql.Select(runningStmt);

                                        Dictionary<string, string> runningStmtObj = (Dictionary<string, string>)runningStmtList[0];
                                        _intPendingDetailsID = runningStmtObj["seq"];

                                        int.TryParse(_intPendingDetailsID, out int intPendingDetailsID);
                                        intPendingDetailsID = intPendingDetailsID + j;

                                        string ss_sql, ss_sql2;
                                        ss_sql = "SET IDENTITY_INSERT Inv_PendingDetailsTbl ON; INSERT INTO Inv_PendingDetailsTbl(intPendingDetailsID, intPendingID, intInvID, varDesc, varModel, decOrderQty, decUnitPrice, intDODetailsNo, blnIsFaulty, varReason, dtExportDate, intExportBy, blnIsDelete, intWarehouseID, dtPendingDate, intAutoIncrementNo, bitIsREJ, bitIsREP, bitIsCN, bitIsKIV, bitIsRFB, bitIsEXC, bitIsDISC, varBatchNoForLBAUTO) VALUES ";
                                        ss_sql2 = ";SET IDENTITY_INSERT Inv_PendingDetailsTbl OFF;";

                                        string itemValues = string.Format("('{0}', '{1}', '{2}', '{3}', '{4}', '{5}', '{6}', '{7}', '{8}', '{9}', '{10}', '{11}', '{12}', '{13}', '{14}', '{15}', '{16}', '{17}', '{18}', '{19}', '{20}', '{21}', '{22}', '{23}')", intPendingDetailsID, runningId, productId, productDesc, productModel, qty, unitPrice, 0, "TRUE", "", "1900-01-01 00:00:00.000", 0, "FALSE", warehouseCode, orderDate, j, 0, 0, 0, 0, 0, 0, 0, "");

                                        string itemQuery = ss_sql + string.Join(", ", itemValues) + ss_sql2;
                                        mssql.Insert(itemQuery);
                                    }
                                }

                                mysql.Insert("UPDATE cms_order SET order_status = 3 WHERE order_id = '" + orderObj["orderId"] + "'");
                                //mysql.Close();
                            }
                        }
                    });

                    slog.action_identifier = Constants.Action_APS_Transfer_CN;
                    slog.action_failure = 0;
                    slog.action_failure_message = string.Empty;
                    slog.action_time = DateTime.Now.ToString();

                    DateTime endTime = DateTime.Now;
                    TimeSpan ts = endTime - startTime;
                    Console.WriteLine("Completed in: " + ts.TotalSeconds.ToString("F"));

                    LocalDB.InsertSyncLog(slog);

                    logger.message = "Transfer CN finished in (" + ts.TotalSeconds.ToString("F") + " seconds)";
                });

                thread.Start();
                //thread.Join();
            }
            catch (ThreadAbortException e)
            {
                DpprException ex = new DpprException
                {
                    file_name = "JobAPSTransferCN",
                    exception = e.Message,
                    time = DateTime.Now.ToString()
                };
                LocalDB.InsertException(ex);

                Console.WriteLine(Constants.Thread_Exception + e.Message);
            }
        }
    }
}
