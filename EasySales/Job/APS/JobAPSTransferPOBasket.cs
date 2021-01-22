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

namespace EasySales.Job.APS
{
    public class JobAPSTransferPOBasket : IJob
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
                    slog.action_identifier = Constants.Action_APS_Transfer_PO_Basket;
                    slog.action_failure = 0;
                    slog.action_failure_message = string.Empty;
                    slog.action_time = DateTime.Now.ToString();

                    DateTime startTime = DateTime.Now;

                    LocalDB.InsertSyncLog(slog);

                    logger.message = "APS Transfer PO Basket is running";
                    logger.Broadcast();

                    List<DpprMySQLconfig> mysql_list = LocalDB.GetRemoteDatabaseConfig().Distinct().ToList();
                    List<DpprSQLServerconfig> mssql_list = LocalDB.GetRemoteSQLServerConfig();

                    mysql_list.Iterate<DpprMySQLconfig>((mysql_config, index) =>
                    {
                        Database mysql = new Database();
                        mysql.Connect(index: index);

                        string targetDBname = string.Empty;

                        CheckBackendRule checkDB = new CheckBackendRule(mysql: mysql);
                        dynamic jsonRule = checkDB.CheckTablesExist().GetSettingByTableName("transfer_po_basket");

                        ArrayList mssql_rule = new ArrayList();
                        if (jsonRule != null)
                        {
                            foreach (var key in jsonRule)
                            {
                                if (key.mssql != null && key.mssql.GetType().ToString() == "Newtonsoft.Json.Linq.JArray")
                                {
                                    //string query = "SELECT * FROM cms_product_purchase_request LEFT JOIN cms_login ON cms_login.login_id = cms_product_purchase_request.salesperson_id";
                                    //string query = "SELECT cms_product_purchase_request.product_code, cms_product_purchase_request.quantity, cms_login.staff_code, cms_product_purchase_request.updated_at FROM cms_product_purchase_request LEFT JOIN cms_login ON cms_login.login_id = cms_product_purchase_request.salesperson_id";
                                    
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
                            throw new Exception("APS Transfer PO Basket requires backend rules");
                        }

                        string POQuery = "SELECT cms_product_purchase_request.product_code, cms_product_purchase_request.quantity, cms_login.staff_code, cms_product_purchase_request.updated_at FROM cms_product_purchase_request LEFT JOIN cms_login ON cms_login.login_id = cms_product_purchase_request.salesperson_id";

                        ArrayList queryResult = mysql.Select(POQuery);

                        if (queryResult.Count == 0)
                        {
                            logger.message = "No PO Basket to transfer";
                            logger.Broadcast();
                        }
                        else
                        {
                            SQLServer mssql = new SQLServer();
                            mssql.Connect(targetDBname);

                            for (int i = 0; i < queryResult.Count; i++)
                            {
                                int j = 0;
                                string product_code, quantity, salesperson_remark, staff_code, order_date, internal_updated_at, product_id, salesperson_id, ss_sql;
                                Dictionary<string, string> obj = (Dictionary<string, string>)queryResult[i];

                                product_code = obj["product_code"];
                                quantity = obj["quantity"];
                                salesperson_remark = "";    //obj["salesperson_remark"];
                                staff_code = obj["staff_code"];
                                order_date = obj["updated_at"];
                                internal_updated_at = obj["updated_at"];
                                j = i + 1;

                                ArrayList product = mssql.Select("SELECT intInvID FROM Inv_StockTbl WHERE charItemCode ='" + product_code + "'");
                                product_id = "";
                                Dictionary<string, string> productObj = (Dictionary<string, string>)product[0];
                                product_id = productObj["intInvID"];//"10172768"

                                ArrayList salesperson = mssql.Select("SELECT intUserID FROM Adm_UserTbl WHERE charUserID = '" + staff_code + "'");
                                salesperson_id = "";
                                Dictionary<string, string> salespersonObj = (Dictionary<string, string>)salesperson[0];
                                salesperson_id = salespersonObj["intUserID"];//"571"

                                ArrayList exist = mssql.Select("SELECT * FROM Pur_POBasketTbl WHERE intInvID = " + product_id + " AND intCreatedBy = " + salesperson_id + " AND intModifyBy = " + salesperson_id);

                                if (exist.Count > 0)
                                {
                                    internal_updated_at = Convert.ToDateTime(internal_updated_at).ToString("yyyy-MM-dd HH:mm:ss"); //check
                                    ss_sql = "UPDATE Pur_POBasketTbl SET decQty = " + quantity + ", dtModifyDate = '" + internal_updated_at + "' WHERE intInvID = " + product_id + " AND intCreatedBy = " + salesperson_id + " AND intModifyBy = " + salesperson_id;

                                    mssql.Insert(ss_sql);
                                }
                                else
                                {
                                    string running_id;
                                    ArrayList running = mssql.Select("SELECT MAX(intPOBasketNo) AS seq FROM Pur_POBasketTbl");

                                    Dictionary<string, string> number = (Dictionary<string, string>)running[0];
                                    running_id = number["seq"];

                                    running_id = running_id + j;

                                    string Values = string.Format("('{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}','{8}','{9}','{10}',)", running_id, quantity, product_id, salesperson_remark, "A", salesperson_id, order_date, salesperson_id, internal_updated_at, "FALSE", 1);
                                    // values[]= "(".running_id.",".quantity.",".product_id.",'".salesperson_remark."','A',".salesperson_id.",'".order_date."',".salesperson_id.",'".internal_updated_at."','FALSE',1)";

                                    ss_sql = "INSERT INTO Pur_POBasketTbl (intPOBasketNo,decQty,intInvID,varRemarks,charStatus,intCreatedBy,dtCreatedDate,intModifyBy,dtModifyDate,blnIsDelete,intAutoIncrementNo) VALUES " + string.Join(", ", Values);
                                    mssql.Insert(ss_sql);
                                }
                            }
                        }
                    });

                    slog.action_identifier = Constants.Action_APS_Transfer_PO_Basket;
                    slog.action_failure = 0;
                    slog.action_failure_message = string.Empty;
                    slog.action_time = DateTime.Now.ToString();

                    DateTime endTime = DateTime.Now;
                    TimeSpan ts = endTime - startTime;
                    Console.WriteLine("Completed in: " + ts.TotalSeconds.ToString("F"));

                    LocalDB.InsertSyncLog(slog);

                    logger.message = "Transfer PO Basket finished in (" + ts.TotalSeconds.ToString("F") + " seconds)";
                    logger.Broadcast();

                });

                thread.Start();
            }
            catch (ThreadAbortException e)
            {
                DpprException ex = new DpprException
                {
                    file_name = "JobAPSTransferPOBasket",
                    exception = e.Message,
                    time = DateTime.Now.ToString()
                };
                LocalDB.InsertException(ex);

                Console.WriteLine(Constants.Thread_Exception + e.Message);
            }
        }
    }
}
