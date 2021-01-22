using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using AutoCount.Stock.StockStatus;
using EasySales.Model;
using EasySales.Object;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Quartz;
using RestSharp;
using System.Text;
using System.Data;

namespace EasySales.Job
{
    [DisallowConcurrentExecution]
    public class JobATCWHReadyStock : IJob
    {
        private static ATC_Connection connection = null;
        private System.Data.DataTable resultTable;

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
                    slog.action_identifier = Constants.Action_ATCWHReadyStockSync;                                  /*check again */
                    slog.action_details = Constants.Tbl_cms_warehouse_stock + Constants.Is_Starting;                   /*check again */
                    slog.action_failure = 0;
                    slog.action_failure_message = string.Empty;
                    slog.action_time = DateTime.Now.ToString();

                    DateTime startTime = DateTime.Now;

                    LocalDB.InsertSyncLog(slog);

                    logger.message = "ATC warehouse ready stock sync is running";
                    logger.Broadcast();

                    List<DpprMySQLconfig> mysql_list = LocalDB.GetRemoteDatabaseConfig().Distinct().ToList();
                    List<DpprSQLServerconfig> mssql_list = LocalDB.GetRemoteSQLServerConfig();

                    mysql_list.Iterate<DpprMySQLconfig>((mysql_config, index) =>
                    {
                        DpprMySQLconfig mysqlconfig = mysql_list[index];
                        Database mysql = new Database();
                        mysql.Connect(index: index);

                        CheckBackendRule checkDB = new CheckBackendRule(mysql: mysql);
                        dynamic jsonRule = checkDB.CheckTablesExist().GetSettingByTableName("wh_ready_stock_atc");

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
                                        ArrayList include = new ArrayList();
                                        ArrayList exclude = new ArrayList();

                                        if (db.include.GetType().ToString() == "Newtonsoft.Json.Linq.JArray" && db.include.Count > 0)
                                        {
                                            foreach (var item in db.include)
                                            {
                                                Dictionary<string, string> pair = new Dictionary<string, string>
                                                {
                                                    { "mysql", item.mysql.ToString() },
                                                    { "mssql", item.mssql.ToString() },
                                                    { "nullfield", item.nullfield.ToString() }
                                                };
                                                include.Add(pair);
                                            }
                                        }

                                        if (db.exclude.GetType().ToString() == "Newtonsoft.Json.Linq.JArray" && db.exclude.Count > 0)
                                        {
                                            foreach (var item in db.exclude)
                                            {
                                                Dictionary<string, string> pair = new Dictionary<string, string>
                                                {
                                                    { "mysql", item.ToString() }
                                                };
                                                exclude.Add(pair);
                                            }
                                        }

                                        ATCRule ATC_rule = new ATCRule()
                                        {
                                            DBname = db.name,
                                            Include = include,
                                            Exclude = exclude
                                        };

                                        mssql_rule.Add(ATC_rule);
                                    }
                                }
                            }
                        }
                        else
                        {
                            throw new Exception("ATC Warehouse Ready Stock sync requires backend rules");
                        }

                        mssql_rule.Iterate<ATCRule>((database, idx) =>
                        {
                            SQLServer mssql = new SQLServer();
                            mssql.Connect(dbname: database.DBname);

                            string mysql_insert = string.Empty;
                            string mssql_insert = string.Empty;

                            string insertQuery = "INSERT INTO cms_warehouse_stock (@columns) VALUES @values ON DUPLICATE KEY UPDATE @update_columns";
                            string columns = string.Empty;
                            string update_columns = string.Empty;

                            database.Include.Iterate<Dictionary<string, string>>((include, inIdx) =>
                            {
                                bool add = true;
                                database.Exclude.Iterate<Dictionary<string, string>>((exclude, exIdx) =>
                                {
                                    if (exclude["mysql"] == include["mysql"])
                                    {
                                        add = false;
                                    }
                                });

                                columns += include["mysql"];

                                if (add)
                                {
                                    update_columns += (include["mysql"] + "=VALUES(" + include["mysql"] + ")");
                                    if (inIdx != database.Include.Count - 1)
                                    {
                                        update_columns += ",";
                                    }
                                }

                                if (inIdx != database.Include.Count - 1)
                                {
                                    columns += ",";
                                }
                            });

                            insertQuery = insertQuery.ReplaceAll(columns, "@columns");
                            insertQuery = insertQuery.ReplaceAll(update_columns, "@update_columns");

                            connection = ATC_Configuration.Init_config();
                            connection.dBSetting = AutoCountV1.PerformAuth(ref connection);

                            StockStatusHelper ssHelper = new StockStatusHelper(connection.userSession);
                            StockStatusCriteria crit = ssHelper.Criteria;

                            crit.LocationFilter.Type = AutoCount.SearchFilter.FilterType.ByRange;

                            ssHelper.Inquire();

                            DataSet ds = ssHelper.ResultDataSet;
                            this.resultTable = ds.Tables["Detail"];

                            Dictionary<string, string> valueList = new Dictionary<string, string>();
                            HashSet<string> queryList = new HashSet<string>();
                            foreach (DataRow row in this.resultTable.Rows)
                            {
                                double.TryParse(row["AvailableQtyAfterCSGN"].ToString(), out double afterCnsg);
                                double.TryParse(row["POQty"].ToString(), out double POQty);
                                double readyStockQty = afterCnsg - POQty;

                                string valueQuery = string.Empty;
                                string itemCode = row["ItemCode"].ToString();
                                string wh_code = row["Location"].ToString();
                                string uom_name = row["UOM"].ToString();

                                itemCode = itemCode.Replace("'", @"\'");
                                string _POQty = POQty.ToString();
                                string _readyStockQty = POQty.ToString();

                                Database.Sanitize(ref itemCode);
                                Database.Sanitize(ref uom_name);

                                valueList.Add("Location", wh_code);
                                valueList.Add("ItemCode", itemCode);
                                valueList.Add("POQty", _POQty);
                                valueList.Add("UOM", uom_name);
                                valueList.Add("readyStockQty", _readyStockQty);

                                database.Include.Iterate<Dictionary<string, string>>((include, inIdx) =>
                                {
                                    string fieldname = include["mssql"];

                                    valueList.Iterate<KeyValuePair<string, string>>((field, ixx) =>
                                    {
                                        if (field.Key == fieldname)
                                        {
                                            string value = string.Empty;
                                            value = field.Value;
                                            valueQuery += ixx == 0 ? "('" + value + "" : "','" + value;
                                        }
                                    });

                                });

                                valueQuery += "')";
                                Console.WriteLine(valueQuery);

                                queryList.Add(valueQuery);
                                valueList.Clear();
                            }

                            string values = queryList.Join(",");
                            insertQuery = insertQuery.ReplaceAll(values, "@values");
                            bool isUpdated = mysql.Insert(insertQuery);
                            mysql.Message("JobATCWHReadyStock ----> " + insertQuery);

                            if (isUpdated)
                            {
                                logger.message = string.Format("{0} warehouse ready stock records is inserted", queryList.Count);
                                logger.Broadcast();
                            }
                            else
                            {
                                logger.message = string.Format("{0} warehouse ready stock records is inserted", queryList.Count);
                                logger.Broadcast();
                            }
                            queryList.Clear();
                        });
                        mssql_rule.Clear();
                    });

                    mysql_list.Clear();
                    mssql_list.Clear();

                    slog.action_identifier = Constants.Action_ATCWHReadyStockSync;
                    slog.action_details = Constants.Tbl_cms_warehouse_stock + Constants.Is_Finished + string.Format(" ({0}) records", RecordCount);
                    slog.action_failure = 0;
                    slog.action_failure_message = string.Empty;
                    slog.action_time = DateTime.Now.ToString();


                    DateTime endTime = DateTime.Now;
                    TimeSpan timespan = endTime - startTime;
                    Console.WriteLine("Completed in: " + timespan.TotalSeconds.ToString("F"));

                    LocalDB.InsertSyncLog(slog);

                    logger.message = "ATC warehouse ready stock sync finished in (" + timespan.TotalSeconds.ToString("F") + " seconds)";
                    logger.Broadcast();

                });

                thread.Start();
                //thread.Join();

            }
            catch (ThreadAbortException e)
            {
                DpprException ex = new DpprException
                {
                    file_name = "JobATCWHReadyStockSync",
                    exception = e.Message,
                    time = DateTime.Now.ToString()
                };
                LocalDB.InsertException(ex);

                Console.WriteLine(Constants.Thread_Exception + e.Message);
            }
        }
    }
}