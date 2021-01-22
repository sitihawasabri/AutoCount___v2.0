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
    public class JobATCReadyStock : IJob
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
                    slog.action_identifier = Constants.Action_ATCReadyStockSync;                                  /*check again */
                    slog.action_details = Constants.Tbl_cms_product + Constants.Is_Starting;                   /*check again */
                    slog.action_failure = 0;
                    slog.action_failure_message = string.Empty;
                    slog.action_time = DateTime.Now.ToString();

                    DateTime startTime = DateTime.Now;

                    LocalDB.InsertSyncLog(slog);

                    logger.message = "ATC ready stock sync is running";
                    logger.Broadcast();

                    List<DpprMySQLconfig> mysql_list = LocalDB.GetRemoteDatabaseConfig().Distinct().ToList();
                    List<DpprSQLServerconfig> mssql_list = LocalDB.GetRemoteSQLServerConfig();

                    mysql_list.Iterate<DpprMySQLconfig>((mysql_config, index) =>
                    {
                        DpprMySQLconfig mysqlconfig = mysql_list[index];
                        Database mysql = new Database();
                        mysql.Connect(index: index);

                        CheckBackendRule checkDB = new CheckBackendRule(mysql: mysql);
                        dynamic jsonRule = checkDB.CheckTablesExist().GetSettingByTableName("ready_stock_atc");

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
                            throw new Exception("ATC Ready Stock sync requires backend rules");
                        }

                        mssql_rule.Iterate<ATCRule>((database, idx) =>
                        {
                            SQLServer mssql = new SQLServer();
                            mssql.Connect(dbname: database.DBname);

                            string mysql_insert = string.Empty;
                            string mssql_insert = string.Empty;

                            string insertQuery = "INSERT INTO cms_product (@columns) VALUES @values ON DUPLICATE KEY UPDATE @update_columns";
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

                            //Console.WriteLine("Trying to read ready stock from AutoCount");

                            

                            StockStatusHelper ssHelper = new StockStatusHelper(connection.userSession);
                            StockStatusCriteria crit = ssHelper.Criteria;

                            crit.LocationFilter.Type = AutoCount.SearchFilter.FilterType.ByRange;

                            crit.LocationFilter.From = "HQ";

                            crit.LocationFilter.To = "HQ";

                            ssHelper.Inquire();

                            DataSet ds = ssHelper.ResultDataSet;
                            this.resultTable = ds.Tables["Detail"];

                            if (!this.resultTable.Columns.Contains("ReadyStock"))
                            {
                                this.resultTable.Columns.Add("ReadyStock", typeof(decimal), "AvailableQtyAfterCSGN - POQty");
                            }

                            Dictionary<string, string> valueList = new Dictionary<string, string>();
                            HashSet<string> queryList = new HashSet<string>();
                            foreach (DataRow row in this.resultTable.Rows)
                            {
                                //double.TryParse(row["AvailableQtyAfterCSGN"].ToString(), out double currentQty);
                                //string _currentQty = currentQty.ToString();

                                double.TryParse(row["AvailableQtyAfterCSGN"].ToString(), out double AvailableQtyAfterCSGN);
                                double.TryParse(row["CSGNBalQty"].ToString(), out double CSGNBalQty);
                                double.TryParse(row["POQty"].ToString(), out double POQty);
                                double.TryParse(row["OnHandQty"].ToString(), out double OnHandQty);
                                double.TryParse(row["SOQty"].ToString(), out double SOQty);
                                double readyStockQty = AvailableQtyAfterCSGN - POQty;
                                string _readyStockQty = readyStockQty.ToString();

                                double qty = OnHandQty - CSGNBalQty - SOQty; //KIAN Calculation
                                string kian_qty = qty.ToString();
                                string _AvailableQtyAfterCSGN = qty.ToString();
                                string _POQty = POQty.ToString();
                                string _SOQty = SOQty.ToString();

                                string valueQuery = String.Empty;
                                string code = row["ItemCode"].ToString();
                                code = code.Replace("'", @"\'");

                                //Console.WriteLine("ItemCode: " + row["ItemCode"]);
                                //Console.WriteLine("AvailableQty: " + row["AvailableQty"]);
                                //Console.WriteLine("OnHandQty: " + row["OnHandQty"]);
                                //Console.WriteLine("SOQty: " + row["SOQty"]);
                                //Console.WriteLine("AvailableQtyAfterCSGN: " + row["AvailableQtyAfterCSGN"]);
                                //Console.WriteLine("CSGNBalQty: " + row["CSGNBalQty"]);
                                //Console.WriteLine("qty: " + kian_qty);

                                valueList.Add("code", code);
                                valueList.Add("kian_qty", kian_qty);
                                valueList.Add("AvailableQtyAfterCSGN", _AvailableQtyAfterCSGN);
                                valueList.Add("POQty", _POQty);
                                valueList.Add("SOQty", _SOQty);
                                valueList.Add("readyStockQty", _readyStockQty);
                                //valueList.Add("currentQty", _currentQty);

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
                            mysql.Message("JobATCReadyStock ----> " +insertQuery);

                            if (isUpdated)
                            {
                                logger.message = string.Format("{0} ready stock records is inserted", queryList.Count);
                                logger.Broadcast();
                            }
                            else
                            {
                                logger.message = string.Format("{0} ready stock records is inserted", queryList.Count);
                                logger.Broadcast();
                            }
                            queryList.Clear();
                        });
                        mssql_rule.Clear();
                    });

                    mysql_list.Clear();
                    mssql_list.Clear();

                    slog.action_identifier = Constants.Action_ATCReadyStockSync;
                    slog.action_details = Constants.Tbl_cms_product + Constants.Is_Finished + string.Format(" ({0}) records", RecordCount);
                    slog.action_failure = 0;
                    slog.action_failure_message = string.Empty;
                    slog.action_time = DateTime.Now.ToString();


                    DateTime endTime = DateTime.Now;
                    TimeSpan timespan = endTime - startTime;
                    Console.WriteLine("Completed in: " + timespan.TotalSeconds.ToString("F"));

                    LocalDB.InsertSyncLog(slog);

                    logger.message = "ATC ready stock sync finished in (" + timespan.TotalSeconds.ToString("F") + " seconds)";
                    logger.Broadcast();

                });

                thread.Start();
                //thread.Join();

            }
            catch (ThreadAbortException e)
            {
                DpprException ex = new DpprException
                {
                    file_name = "JobATCReadyStockSync",
                    exception = e.Message,
                    time = DateTime.Now.ToString()
                };
                LocalDB.InsertException(ex);

                Console.WriteLine(Constants.Thread_Exception + e.Message);
            }
        }
    }
}