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
    class JobAPSTransferQuotation : IJob
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
                    slog.action_identifier = Constants.Action_APS_Transfer_Quotation;
                    slog.action_failure = 0;
                    slog.action_failure_message = string.Empty;
                    slog.action_time = DateTime.Now.ToString();

                    DateTime startTime = DateTime.Now;

                    LocalDB.InsertSyncLog(slog);

                    logger.message = "APS transfer quotation is running";
                    logger.Broadcast();

                    List<DpprMySQLconfig> mysql_list = LocalDB.GetRemoteDatabaseConfig().Distinct().ToList();
                    List<DpprSQLServerconfig> mssql_list = LocalDB.GetRemoteSQLServerConfig();

                    mysql_list.Iterate<DpprMySQLconfig>((mysql_config, index) =>
                    {
                        Database mysql = new Database();
                        mysql.Connect(index: index);

                        CheckBackendRule checkDB = new CheckBackendRule(mysql: mysql);
                        dynamic jsonRule = checkDB.CheckTablesExist().GetSettingByTableName("transfer_quotation");

                        ArrayList mssql_rule = new ArrayList();
                        if (jsonRule != null)
                        {
                            foreach (var key in jsonRule)
                            {
                                if (key.mssql != null && key.mssql.GetType().ToString() == "Newtonsoft.Json.Linq.JArray")
                                {
                                    string query = "SELECT cms_order.order_id, cms_order_item.cancel_status, cms_order_item.product_code, cms_order_item.quantity, cms_order_item.salesperson_remark, cms_login.staff_code, cms_order.order_date, cms_order.internal_updated_at FROM cms_order_item LEFT JOIN cms_order ON cms_order.order_id = cms_order_item.order_id LEFT JOIN cms_login ON cms_login.login_id = cms_order.salesperson_id WHERE cms_order_item.cancel_status = 0 AND cms_order.order_status = 1 AND doc_type = 'quotation'";
                                    //string query = "SELECT cms_order.order_id, cms_order_item.product_code, cms_order_item.quantity, cms_order_item.salesperson_remark, cms_login.staff_code, cms_order.order_date, cms_order.internal_updated_at FROM cms_order_item LEFT JOIN cms_order ON cms_order.order_id = cms_order_item.order_id LEFT JOIN cms_login ON cms_login.login_id = cms_order.salesperson_id WHERE doc_type = 'quotation'";
                                    //SELECT cms_order_item.product_code, cms_order_item.quantity, cms_order_item.salesperson_remark, cms_login.staff_code, cms_order.order_date, cms_order.internal_updated_at FROM cms_order_item LEFT JOIN cms_order ON cms_order.order_id = cms_order_item.order_id LEFT JOIN cms_login ON cms_login.login_id = cms_order.salesperson_id WHERE doc_type = 'quotation'
                                    //get all quotation order

                                    var mssql_rules = key.mssql;
                                    foreach (var db in mssql_rules)
                                    {
                                        APSRule aps_rule = new APSRule()
                                        {
                                            DBname = db.name,
                                            Query = query
                                        };

                                        mssql_rule.Add(aps_rule);
                                    }
                                }
                            }
                        }
                        else
                        {
                            throw new Exception("APS Transfer Quotation sync requires backend rules");
                        }

                        mssql_rule.Iterate<APSRule>((database, idx) =>
                        {
                            SQLServer mssql = new SQLServer();
                            mssql.Connect(dbname: database.DBname);

                            Console.WriteLine(database.Query);

                            ArrayList queryResult = mysql.Select(database.Query);

                            if (queryResult.Count == 0)
                            {
                                logger.message = "No Quotation to transfer";
                                logger.Broadcast();
                            }
                            else
                            {
                                string productCode, quantity, salespersonRemark, staffCode, orderDate, internalUpdatedAt;
                                HashSet<string> valueString = new HashSet<string>();

                                for (int i = 0; i < queryResult.Count; i++)
                                {
                                    Dictionary<string, string> obj = (Dictionary<string, string>)queryResult[i];

                                    //ArrayList quotationItems = mysql.Select("SELECT cms_order_item.product_code, cms_order_item.quantity, cms_order_item.salesperson_remark, cms_login.staff_code, cms_order.order_date, cms_order.internal_updated_at FROM cms_order_item LEFT JOIN cms_order ON cms_order.order_id = cms_order_item.order_id LEFT JOIN cms_login ON cms_login.login_id = cms_order.salesperson_id WHERE cms_order_item.order_id = '" + obj["order_id"] + "'");
                                    ArrayList quotationItems = mysql.Select("SELECT cms_order_item.cancel_status, cms_order_item.product_code, cms_order_item.quantity, cms_order_item.salesperson_remark, cms_login.staff_code, cms_order.order_date, cms_order.internal_updated_at FROM cms_order_item LEFT JOIN cms_order ON cms_order.order_id = cms_order_item.order_id LEFT JOIN cms_login ON cms_login.login_id = cms_order.salesperson_id WHERE cms_order_item.order_id = '" + obj["order_id"] + "' AND cms_order_item.cancel_status = 0");
                                    //get quotation order items for related id

                                    for (int ixx = 0; ixx < quotationItems.Count; ixx++)
                                    {
                                        Dictionary<string, string> objQuo = (Dictionary<string, string>)quotationItems[ixx];

                                        productCode = objQuo["product_code"];
                                        quantity = objQuo["quantity"];
                                        salespersonRemark = objQuo["salesperson_remark"];
                                        staffCode = objQuo["staff_code"];
                                        orderDate = objQuo["order_date"];
                                        orderDate = Convert.ToDateTime(orderDate).ToString("yyyy-MM-dd HH:mm:ss");
                                        internalUpdatedAt = objQuo["internal_updated_at"];
                                        internalUpdatedAt = Convert.ToDateTime(internalUpdatedAt).ToString("yyyy-MM-dd HH:mm:ss");

                                        int j;
                                        j = i + 1;//"PT-8043A"

                                        ArrayList productList = mssql.Select("SELECT intInvID FROM Inv_StockTbl WHERE charItemCode ='" + productCode + "'");

                                        string productId = "";
                                        Dictionary<string, string> eachproduct = (Dictionary<string, string>)productList[0];
                                        productId = eachproduct["intInvID"];

                                        ArrayList salespersonList = mssql.Select("SELECT intUserID FROM Adm_UserTbl WHERE charUserID = '" + staffCode + "'");

                                        string salespersonId = "";
                                        Dictionary<string, string> eachsalesperson = (Dictionary<string, string>)salespersonList[0];
                                        salespersonId = eachsalesperson["intUserID"];

                                        ArrayList runningList = mssql.Select("SELECT MAX(intPOBasketNo) AS seq FROM Pur_POBasketTbl");

                                        string runningId = "";
                                        Dictionary<string, string> eachrunning = (Dictionary<string, string>)runningList[0];//{[seq, 1481]}
                                        runningId = eachrunning["seq"];

                                        runningId = runningId + j; //runningid = 481, + j (1) become 4811 not 482

                                        string Values = string.Format("('{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}','{8}','{9}','{10}')", runningId, quantity, productId, salespersonRemark, "A", salespersonId, orderDate, salespersonId, internalUpdatedAt, "FALSE", j);

                                        //      $values[]= "(".$running_id.",".$quantity.",".$product_id.",'".$salesperson_remark."','A',".$salesperson_id.",'".$order_date."',".$salesperson_id.",'".$internal_updated_at."','FALSE',".$j.")";

                                        valueString.Add(Values);//"('14811','10','10069083','','A','476','17/01/2020 12:00:00 AM','476','05/05/2020 9:41:30 AM','FALSE','1',)"
                                    }
                                    
                                    string ss_sql = "INSERT INTO Pur_POBasketTbl (intPOBasketNo,decQty,intInvID,varRemarks,charStatus,intCreatedBy,dtCreatedDate,intModifyBy,dtModifyDate,blnIsDelete,intAutoIncrementNo) VALUES " +string.Join(", ", valueString);

                                    Console.Write(ss_sql);

                                    //mssql.Insert(ss_sql);
                                    //mysql.Insert("UPDATE cms_order SET order_status = 2 WHERE order_id = '" + obj["orderId"] + "'");

                                    //      if ($ssdb->query($ss_sql)){
                                    //$mysql->Execute("UPDATE cms_order SET order_status = 2 WHERE order_id = '".$data['orderId']."'");
                                    //      sqlsrv_free_stmt($sth);

                                    //      if (empty($data['salespersonId']))
                                    //      {
                                    //          return getCurrencyOrderAndSalespersonCustomerInOneTime($data);
                                    //      }
                                    //      else
                                    //      {
                                    //          return searchOrders($data);
                                    //      }
                                    //      }
                                }
                                
                            }

                        });
                    });

                    slog.action_identifier = Constants.Action_APSDOSync;
                    slog.action_details = Constants.Tbl_cms_do + Constants.Is_Finished + string.Format(" ({0}) records", RecordCount);
                    slog.action_failure = 0;
                    slog.action_failure_message = string.Empty;
                    slog.action_time = DateTime.Now.ToString();

                    DateTime endTime = DateTime.Now;
                    TimeSpan ts = endTime - startTime;
                    Console.WriteLine("Completed in: " + ts.TotalSeconds.ToString("F"));

                    LocalDB.InsertSyncLog(slog);
                    logger.message = "APS transfer quotation finished in (" + ts.TotalSeconds.ToString("F") + " seconds)";
                    logger.Broadcast();

                });

                thread.Start();

            }
            catch (ThreadAbortException e)
            {
                DpprException ex = new DpprException
                {
                    file_name = "JobAPSTransferQuotationSync",
                    exception = e.Message,
                    time = DateTime.Now.ToString()
                };
                LocalDB.InsertException(ex);

                Console.WriteLine(Constants.Thread_Exception + e.Message);
            }
        }
    }
}
