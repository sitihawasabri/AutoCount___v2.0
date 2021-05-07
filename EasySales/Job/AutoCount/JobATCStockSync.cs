using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using EasySales.Model;
using EasySales.Object;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Quartz;
using RestSharp;
using Ubiety.Dns.Core.Records;

namespace EasySales.Job
{
    [DisallowConcurrentExecution]
    public class JobATCStockSync : IJob
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
                    slog.action_identifier = Constants.Action_ATCStockSync;
                    slog.action_details = Constants.Tbl_cms_product + Constants.Is_Starting;
                    slog.action_failure = 0;
                    slog.action_failure_message = string.Empty;
                    slog.action_time = DateTime.Now.ToString();

                    DateTime startTime = DateTime.Now;

                    LocalDB.DBCleanup();
                    LocalDB.InsertSyncLog(slog);

                    logger.message = "ATC stock sync is running";
                    logger.Broadcast();

                    List<DpprMySQLconfig> mysql_list = LocalDB.GetRemoteDatabaseConfig().Distinct().ToList();
                    List<DpprSQLServerconfig> mssql_list = LocalDB.GetRemoteSQLServerConfig();

                    mysql_list.Iterate<DpprMySQLconfig>((mysql_config, index) =>
                    {
                        DpprMySQLconfig mysqlconfig = mysql_list[index];
                        Database mysql = new Database();
                        mysql.Connect(index: index);

                        CheckBackendRule checkDB = new CheckBackendRule(mysql: mysql);
                        dynamic jsonRule = checkDB.CheckTablesExist().GetSettingByTableName("cms_product_atc");

                        //Dictionary<string, string> cms_updated_time = mysql.GetUpdatedTime("cms_product");

                        ArrayList mssql_rule = new ArrayList();

                        string leftjoinquery = "left join dbo.ItemBalQty on dbo.Item.ItemCode = dbo.ItemBalQty.ItemCode";
                        string groupbyquery = "";

                        if (jsonRule != null)
                        {
                            foreach (var key in jsonRule)
                            {
                                if (key.mssql != null && key.mssql.GetType().ToString() == "Newtonsoft.Json.Linq.JArray")
                                {
                                    string query = "SELECT dbo.Item.ItemCode, dbo.ItemUOM.BarCode, MAX(CAST(FurtherDescription AS VARCHAR(MAX))) AS FurtherDescription,Description, DESC2,IsActive,SUM(BalQty) as BalQty, ItemGroup,ItemType from dbo.Item @leftjoinquery left join dbo.ItemUOM on dbo.ItemUOM.ItemCode = dbo.Item.ItemCode group by dbo.Item.ItemCode, Description,DESC2,IsActive,ItemGroup,ItemType, dbo.ItemUOM.BarCode ";
                                    //left join dbo.ItemUOM on dbo.ItemUOM.ItemCode = dbo.Item.ItemCode
                                    //@leftjoinquery = left join dbo.ItemBalQty on dbo.Item.ItemCode = dbo.ItemBalQty.ItemCode || left join dbo.ItemBatchBalQty on dbo.Item.ItemCode = dbo.ItemBatchBalQty.ItemCode

                                    var mssql_rules = key.mssql;
                                    foreach (var db in mssql_rules)
                                    {
                                        if (db.leftjoinquery != null)
                                        {
                                            leftjoinquery = db.leftjoinquery;
                                        }

                                        if (db.groupby != null)
                                        {
                                            groupbyquery = db.groupby;
                                        }

                                        query = query.Replace("@leftjoinquery", leftjoinquery);
                                        query = query.Replace("@groupbyquery", groupbyquery);

                                        string wh_join = string.Empty;
                                        string wh_select = string.Empty;
                                        ArrayList warehouse_qty_names = new ArrayList();
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
                                            CategoryField = db.category_field,
                                            Include = include,
                                            Exclude = exclude,
                                            Query = query
                                        };

                                        mssql_rule.Add(ATC_rule);
                                    }
                                }
                            }
                        }
                        else
                        {
                            throw new Exception("ATC Product sync requires backend rules");
                        }

                        ArrayList categoriesInMySQL = mysql.Select("SELECT * FROM cms_product_category");
                        Dictionary<string, string> categories = new Dictionary<string, string>();
                        for (int i = 0; i < categoriesInMySQL.Count; i++)
                        {
                            Dictionary<string, string> map = (Dictionary<string, string>)categoriesInMySQL[i];
                            categories.Add(map["categoryIdentifierId"], map["category_id"]);
                        }
                        categoriesInMySQL.Clear();

                        ArrayList productsInMySQL = mysql.Select("SELECT product_code FROM cms_product WHERE product_status = 1");
                        ArrayList activeProducts = new ArrayList();
                        for (int i = 0; i < productsInMySQL.Count; i++)
                        {
                            Dictionary<string, string> map = (Dictionary<string, string>)productsInMySQL[i];
                            activeProducts.Add(map["product_code"]);
                        }
                        productsInMySQL.Clear();

                        mssql_rule.Iterate<ATCRule>((database, idx) =>
                        {
                            SQLServer mssql = new SQLServer();
                            mssql.Connect(dbname: database.DBname);

                            logger.Broadcast(database.Query);

                            ArrayList queryResult = mssql.Select(database.Query);
                            if (queryResult.Count > 0)
                            {
                                logger.Broadcast("Products to be inserted: " + queryResult.Count);
                            }

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

                            ArrayList mysqlFieldList = new ArrayList(); /* get all mysql field column */
                            database.Include.Iterate<Dictionary<string, string>>((incDict, incindex) =>
                            {
                                string mysqlField = incDict["mysql"].ToString();
                                mysqlFieldList.Add(mysqlField);
                            });

                            HashSet<string> valueString = new HashSet<string>();
                            queryResult.Iterate<Dictionary<string, string>>((map, i) =>
                            {
                                string row = string.Empty;
                                database.Include.Iterate<Dictionary<string, string>>((include, inIdx) =>
                                {
                                    string nullfield = include["nullfield"];
                                    string find_mssql_field = include["mssql"];
                                    string corr_mysql_field = include["mysql"];

                                    bool NoMssqlField = true;
                                    bool addedToRow = false;

                                    if (corr_mysql_field == "category_id")
                                    {
                                        // get category id here
                                        string _categoryId = "0";
                                        string item = string.Empty;

                                        map.Iterate<KeyValuePair<string, string>>((mssql_fields, mssqIndx) =>
                                        {
                                            if (database.CategoryField == "ItemType")
                                            {
                                                if (mssql_fields.Key == "ItemType")
                                                {
                                                    item = mssql_fields.Value;
                                                }
                                            }

                                            if (database.CategoryField == "ItemGroup")
                                            {
                                                if (mssql_fields.Key == "ItemGroup")
                                                {
                                                    item = mssql_fields.Value;
                                                }
                                            }

                                        });

                                        if (string.IsNullOrEmpty(item) || !categories.TryGetValue(item, out _categoryId))
                                        {
                                            _categoryId = "0";
                                        }

                                        int.TryParse(_categoryId, out int CategoryId);

                                        row += inIdx == 0 ? "('" + CategoryId + "" : "','" + CategoryId;

                                        NoMssqlField = false;
                                        addedToRow = true;
                                    }

                                    if (find_mssql_field == "IsActive")
                                    {
                                        string status = string.Empty;

                                        map.Iterate<KeyValuePair<string, string>>((mssql_fields, mssqIndx) =>
                                        {
                                            if (mssql_fields.Key == "IsActive")
                                            {
                                                status = mssql_fields.Value;

                                                if (status == "F")
                                                {
                                                    status = "0";
                                                }
                                                else
                                                {
                                                    status = "1";
                                                }
                                            }
                                        });

                                        row += inIdx == 0 ? "('" + status + "" : "','" + status;

                                        NoMssqlField = false;
                                        addedToRow = true;
                                    }

                                    if (find_mssql_field == "FurtherDescription")
                                    {
                                        string desc = string.Empty;
                                        string FurDesc = string.Empty;

                                        map.Iterate<KeyValuePair<string, string>>((mssql_fields, mssqIndx) =>
                                        {
                                            if (mssql_fields.Key == "FurtherDescription")
                                            {
                                                desc = mssql_fields.Value;
                                                RichTextBox rtBox = new RichTextBox();
                                                rtBox.Rtf = desc;
                                                FurDesc = rtBox.Text;
                                                rtBox.Dispose();
                                            }
                                        });

                                        Database.Sanitize(ref FurDesc);
                                        row += inIdx == 0 ? "('" + FurDesc + "" : "','" + FurDesc;

                                        NoMssqlField = false;
                                        addedToRow = true;
                                    }

                                    if (corr_mysql_field == "sequence_no")
                                    {
                                        RecordCount++;
                                        row += inIdx == 0 ? "('" + RecordCount + "" : "','" + RecordCount;

                                        NoMssqlField = false;
                                        addedToRow = true;
                                    }

                                    if (corr_mysql_field == "product_code")
                                    {
                                        string productCode = string.Empty;

                                        map.Iterate<KeyValuePair<string, string>>((mssql_fields, mssqIndx) =>
                                        {
                                            if (mssql_fields.Key == "ItemCode")
                                            {
                                                productCode = mssql_fields.Value;
                                            }
                                        });

                                        row += inIdx == 0 ? "('" + productCode + "" : "','" + productCode;

                                        if (activeProducts.Contains(productCode))
                                        {
                                            int indexx = activeProducts.IndexOf(productCode);
                                            if (indexx != -1)
                                            {
                                                activeProducts.RemoveAt(indexx);
                                            }
                                        }

                                        NoMssqlField = false;
                                        addedToRow = true;
                                    }

                                    map.Iterate<KeyValuePair<string, string>>((mssql_fields, mssqIndx) =>
                                    {
                                        if (find_mssql_field.EcodeContains(mssql_fields.Key))
                                        {
                                            string tmp = string.Empty;
                                            tmp = LogicParser.Parse(mssql_fields.Key, find_mssql_field, map, nullfield)[mssql_fields.Key];


                                            for (int isql = 0; isql < mysqlFieldList.Count; isql++)
                                            {
                                                string eachField = mysqlFieldList[isql].ToString();
                                                if (!addedToRow)
                                                {
                                                    /* all field except itemcode/itemtype && find_mssql_field which has string/join dont insert here */
                                                    if (corr_mysql_field == eachField && LogicParser.IsCodeStr(find_mssql_field) == false)
                                                    {
                                                        Database.Sanitize(ref tmp);
                                                        row += inIdx == 0 ? "('" + tmp + "" : "','" + tmp;
                                                        addedToRow = true;
                                                    }
                                                }
                                            }

                                            if (!addedToRow)
                                            {
                                                Database.Sanitize(ref tmp);
                                                if (row.Contains(tmp) == false && corr_mysql_field != "category_id" && corr_mysql_field != "product_code")
                                                {
                                                    row += inIdx == 0 ? "('" + tmp + "" : "','" + tmp;
                                                    addedToRow = true;
                                                }
                                            }

                                            NoMssqlField = false;
                                        }
                                    });

                                    if (NoMssqlField)
                                    {
                                        string tmp = LogicParser.Parse(corr_mysql_field, find_mssql_field, map, nullfield)[corr_mysql_field];
                                        Database.Sanitize(ref tmp);
                                        row += inIdx == 0 ? "('" + tmp + "" : "','" + tmp;
                                    }
                                });

                                row += "')";

                                valueString.Add(row);

                                if (valueString.Count % 2000 == 0)
                                {
                                    string values = valueString.Join(",");

                                    insertQuery = insertQuery.ReplaceAll(values, "@values");

                                    mysql.Insert(insertQuery);
                                    mysql.Message("Stock Insert Query: " + insertQuery);

                                    insertQuery = insertQuery.ReplaceAll("@values", values);
                                    valueString.Clear();

                                    logger.message = string.Format("{0} stock records is inserted into " + mysqlconfig.config_database, RecordCount);
                                    logger.Broadcast();
                                }
                            });

                            if (valueString.Count > 0)
                            {
                                string values = valueString.Join(",");

                                insertQuery = insertQuery.ReplaceAll(values, "@values");

                                mysql.Insert(insertQuery);
                                mysql.Message("Stock Insert Query: " + insertQuery);

                                insertQuery = insertQuery.ReplaceAll("@values", values);
                                valueString.Clear();

                                logger.message = string.Format("{0} stock records is inserted into " + mysqlconfig.config_database, RecordCount);
                                logger.Broadcast();
                            }

                            if (activeProducts.Count > 0)
                            {
                                logger.Broadcast("Total product records to be deactivated: " + activeProducts.Count);

                                HashSet<string> deactivateId = new HashSet<string>();
                                for (int i = 0; i < activeProducts.Count; i++)
                                {
                                    string _id = activeProducts[i].ToString();
                                    deactivateId.Add(_id);
                                }

                                string ToBeDeactivate = "'" + string.Join("','", deactivateId) + "'";
                                Console.WriteLine(ToBeDeactivate);

                                string inactive = "UPDATE cms_product SET product_status = 0 WHERE product_code IN (" + ToBeDeactivate + ")";
                                mysql.Insert(inactive);

                                logger.Broadcast(activeProducts.Count + " product records deactivated");

                                activeProducts.Clear();
                                deactivateId.Clear();
                            }

                            categories.Clear();
                            activeProducts.Clear();

                            mysql.Insert("INSERT INTO cms_update_time(table_name, updated_at) VALUES('cms_product', NOW()) ON DUPLICATE KEY UPDATE updated_at = VALUES(updated_at)");

                            RecordCount = 0; /* reset count for the next database */
                            mysqlFieldList.Clear();
                            queryResult.Clear();
                        });
                        mssql_rule.Clear();
                        //cms_updated_time.Clear();
                    });

                    mysql_list.Clear();
                    mssql_list.Clear();

                    slog.action_identifier = Constants.Action_ATCStockSync;
                    slog.action_details = Constants.Tbl_cms_product + Constants.Is_Finished + string.Format(" ({0}) records", RecordCount);
                    slog.action_failure = 0;
                    slog.action_failure_message = string.Empty;
                    slog.action_time = DateTime.Now.ToString();

                    DateTime endTime = DateTime.Now;
                    TimeSpan ts = endTime - startTime;
                    //Console.WriteLine("Completed in: " + ts.TotalSeconds.ToString("F"));

                    LocalDB.InsertSyncLog(slog);

                    logger.message = "ATC stock sync finished in (" + ts.TotalSeconds.ToString("F") + " seconds)";
                    logger.Broadcast();

                });

                thread.Start();
                //thread.Join();

            }
            catch (ThreadAbortException e)
            {
                DpprException ex = new DpprException
                {
                    file_name = "JobATCStockSync",
                    exception = e.Message,
                    time = DateTime.Now.ToString()
                };
                LocalDB.InsertException(ex);

                Console.WriteLine(Constants.Thread_Exception + e.Message);
            }
        }
    }
}