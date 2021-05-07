using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
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
    public class JobATCWarehouseQtySync : IJob
    {
        private ArrayList onlyItem = new ArrayList();
        public void ExecuteOnlyItem(ArrayList onlyItem)
        {
            this.onlyItem = onlyItem;
            Execute();
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

                    int RecordCount = 0;
                    GlobalLogger logger = new GlobalLogger();

                    DpprSyncLog slog = new DpprSyncLog();
                    slog.action_identifier = Constants.Action_ATCWarehouseQtySync;                                 /*check again */
                    slog.action_details = Constants.Tbl_cms_warehouse_stock + Constants.Is_Starting;                   /*check again */
                    slog.action_failure = 0;
                    slog.action_failure_message = string.Empty;
                    slog.action_time = DateTime.Now.ToString();

                    DateTime startTime = DateTime.Now;

                    LocalDB.InsertSyncLog(slog);

                    logger.message = "ATC warehouse stock sync is running";
                    logger.Broadcast();

                    List<DpprMySQLconfig> mysql_list = LocalDB.GetRemoteDatabaseConfig().Distinct().ToList();
                    List<DpprSQLServerconfig> mssql_list = LocalDB.GetRemoteSQLServerConfig();

                    string deactivateWhQuery = "SELECT * FROM ItemBalQty";

                    mysql_list.Iterate<DpprMySQLconfig>((mysql_config, index) =>
                    {
                        DpprMySQLconfig mysqlconfig = mysql_list[index];
                        Database mysql = new Database();
                        mysql.Connect(index: index);

                        CheckBackendRule checkDB = new CheckBackendRule(mysql: mysql);
                        dynamic jsonRule = checkDB.CheckTablesExist().GetSettingByTableName("cms_warehouse_stock_atc");

                        //Dictionary<string, string> cms_updated_time = mysql.GetUpdatedTime("cms_warehouse_stock");

                        ArrayList mssql_rule = new ArrayList();

                        if (jsonRule != null)
                        {
                            foreach (var key in jsonRule)
                            {
                                if (key.mssql != null && key.mssql.GetType().ToString() == "Newtonsoft.Json.Linq.JArray")
                                {
                                    //string query = "SELECT * FROM ItemBalQty";
                                    string query = "SELECT * FROM ";

                                    var mssql_rules = key.mssql;
                                    foreach (var db in mssql_rules)
                                    {
                                        string table_name = db.table_name;
                                        string wh_date = string.Empty;
                                        string wh_null = string.Empty;
                                        string wh_join = string.Empty;
                                        string whereClause = string.Empty;
                                        ArrayList include = new ArrayList();
                                        ArrayList exclude = new ArrayList();
                                        ArrayList whList = new ArrayList();
                                        Dictionary<string, string> whListPair = new Dictionary<string, string>();

                                        query += " " + table_name + "";
                                        //query += " WHERE ItemCode = '@itemCode'";// AND UOM = '@uom'";

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
                                            Exclude = exclude,
                                            WarehousePairList = whListPair,
                                            Query = query
                                        };

                                        mssql_rule.Add(ATC_rule);
                                    }
                                }
                            }
                        }
                        else
                        {
                            throw new Exception("ATC Warehouse Stock sync requires backend rules");
                        }

                        //SELECT whbw.dtModifyDate, whhq.dtModifyDate, stk.intInvID, charlocation1, stk.charItemCode as [ItemCode], su.varStockUnitNm as [UOM], isnull(whhq.decQtyOnHand, 0) as [Qty1], isnull(whkl.decQtyOnHand, 0) as [Qty2], isnull(whbw.decQtyOnHand, 0) as [Qty3], isnull(whjb.decQtyOnHand, 0) as [Qty4], isnull(whpi.decQtyOnHand, 0) as [Qty5], isnull(whkj.decQtyOnHand, 0) as [Qty6], po.decQtyOnOrder as [POQty] from inv_stocktbl stk inner join inv_stockunittbl su on stk.intStockUnitID = su.intStockUnitID and stk.charItemCode != '' and stk.charItemCode not like '%deleted%' left outer join vwActiveInv_WarehouseStockTbl whhq on stk.intinvid = whhq.intinvid and whhq.intWarehouseID = 1 left outer join vwActiveInv_WarehouseStockTbl whkl on stk.intinvid = whkl.intinvid and whkl.intWarehouseID = 2 left outer join vwActiveInv_WarehouseStockTbl whbw on stk.intinvid = whbw.intinvid and whbw.intWarehouseID = 3 left outer join vwActiveInv_WarehouseStockTbl whjb on stk.intinvid = whjb.intinvid and whjb.intWarehouseID = 4 left outer join vwActiveInv_WarehouseStockTbl whpi on stk.intinvid = whpi.intinvid and whpi.intWarehouseID = 5 left outer join vwActiveInv_WarehouseStockTbl whkj on stk.intinvid = whkj.intinvid and whkj.intWarehouseID = 10003 left outer join Pur_POOffSetTbl po on stk.intinvid = po.intinvid WHERE stk.blnIsDelete = 'false' and whhq.dtModifyDate >= '2020-09-01' or whkl.dtModifyDate >= '2020-09-01' or whbw.dtModifyDate >= '2020-09-01' or whjb.dtModifyDate >= '2020-09-01' or whpi.dtModifyDate >= '2020-09-01' or whkj.dtModifyDate >= '2020-09-01'
                        //SELECT product_code, product_uom FROM cms_product_uom_price_v2 WHERE product_default_price = 1 AND active_status = 1
                        ArrayList baseUom = mysql.Select("SELECT product_code, product_uom FROM cms_product_uom_price_v2 WHERE product_default_price = 1 AND active_status = 1");

                        Dictionary<string, string> uniqueKeyList = new Dictionary<string, string>();
                        for (int i = 0; i < baseUom.Count; i++)
                        {
                            Dictionary<string, string> each = (Dictionary<string, string>)baseUom[i];
                            string product_code = each["product_code"].ToString();
                            string product_uom = each["product_uom"].ToString();
                            if(!uniqueKeyList.ContainsKey(product_code))
                            {
                                uniqueKeyList.Add(product_code, product_uom);
                            }
                        }
                        baseUom.Clear();

                        ArrayList itemFromDb = mysql.Select("SELECT product_code FROM cms_product WHERE product_status = 1");
                        ArrayList itemList = new ArrayList();
                        for (int i = 0; i < itemFromDb.Count; i++)
                        {
                            Dictionary<string, string> each = (Dictionary<string, string>)itemFromDb[i];
                            if(!itemList.Contains(each["product_code"]))
                            {
                                itemList.Add(each["product_code"]);
                            }
                        }
                        itemFromDb.Clear();

                        ArrayList warehouseItemInMySQL = mysql.Select("SELECT wh_code FROM cms_warehouse WHERE wh_status = 1");
                        ArrayList warehouseItemList = new ArrayList();

                        for (int i = 0; i < warehouseItemInMySQL.Count; i++)
                        {
                            Dictionary<string, string> each = (Dictionary<string, string>)warehouseItemInMySQL[i];
                            if(!warehouseItemList.Contains(each["wh_code"]))
                            {
                                warehouseItemList.Add(each["wh_code"]);
                            }
                        }
                        warehouseItemInMySQL.Clear();


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

                            ArrayList mysqlFieldList = new ArrayList(); /* get all mysql field column */
                            database.Include.Iterate<Dictionary<string, string>>((incDict, incindex) =>
                            {
                                string mysqlField = incDict["mysql"].ToString();
                                mysqlFieldList.Add(mysqlField);
                            });

                            if (this.onlyItem.Count > 0)
                            {
                                itemList = this.onlyItem;
                            }

                            HashSet<string> inProdCode = new HashSet<string>();
                            for (int i = 0; i < itemList.Count; i++)
                            {
                                string __prodCode = itemList[i].ToString();
                                inProdCode.Add(__prodCode);
                            }

                            string toBeSelected = "'" + string.Join("','", inProdCode) + "'";
                            Console.WriteLine(toBeSelected);

                            database.Query += " WHERE ItemCode IN (" + toBeSelected + ")";

                            //for (int ixx = 0; ixx < itemList.Count; ixx++)
                            //{
                            //string getWhQty = database.Query.Replace("@itemCode", productCode);
                            //if (uniqueKeyList.ContainsKey(productCode))
                            //{
                            //    var value = uniqueKeyList.Where(pair => pair.Key == productCode)
                            //                .Select(pair => pair.Value)
                            //                .FirstOrDefault();
                            //    uomName = value;
                            //    Console.WriteLine(value);
                            //}
                            //getWhQty = getWhQty.Replace("@uom", uomName);
                            //ArrayList queryResult = mssql.Select(getWhQty);
                            //logger.Broadcast(getWhQty);
                            //Console.WriteLine("queryResult.Count: " + queryResult.Count);

                            string productCode = string.Empty;
                            string uomName = string.Empty;

                            ArrayList queryResult = mssql.Select(database.Query);
                            if (queryResult.Count > 0)
                            {
                                logger.Broadcast("Warehouse Qty to be inserted: " + queryResult.Count);
                            }

                            HashSet<string> valueString = new HashSet<string>();
                            queryResult.Iterate<Dictionary<string, string>>((map, i) =>
                            {
                                string row = string.Empty;
                                string finalUOM = string.Empty;

                                database.Include.Iterate<Dictionary<string, string>>((include, inIdx) =>
                                {
                                    string nullfield = include["nullfield"];
                                    string find_mssql_field = include["mssql"];
                                    string corr_mysql_field = include["mysql"];

                                    bool NoMssqlField = true;
                                    bool addedToRow = false;

                                    if (find_mssql_field == "active_status")
                                    {
                                        string activeStatus = "1";

                                        map.Iterate<KeyValuePair<string, string>>((mssql_fields, mssqIndx) =>
                                        {
                                            if (mssql_fields.Key == "Location")
                                            {
                                                string location = mssql_fields.Value;

                                                if (warehouseItemList.Contains(location))
                                                {
                                                    activeStatus = "1";
                                                }
                                                Database.Sanitize(ref activeStatus);
                                                row += inIdx == 0 ? "('" + activeStatus + "" : "','" + activeStatus;
                                            }
                                        });

                                        NoMssqlField = false;
                                        addedToRow = true;
                                    }

                                    if (find_mssql_field == "DateTime.Now")
                                    {
                                        string updated_at = string.Empty;
                                        DateTime date = DateTime.Now;
                                        updated_at = date.ToString("s"); //2020-09-08 15:30:36

                                        Database.Sanitize(ref updated_at);

                                        if (row != string.Empty)
                                        {
                                            row += inIdx == 0 ? "('" + updated_at + "" : "','" + updated_at + "";
                                            addedToRow = true;
                                        }
                                    }

                                    if (find_mssql_field == "ItemCode")
                                    {
                                        map.Iterate<KeyValuePair<string, string>>((mssql_fields, mssqIndx) =>
                                        {
                                            if (mssql_fields.Key == "ItemCode")
                                            {
                                                productCode = mssql_fields.Value;

                                                Database.Sanitize(ref productCode);
                                                row += inIdx == 0 ? "('" + productCode + "" : "','" + productCode;
                                            }
                                        });

                                        NoMssqlField = false;
                                        addedToRow = true;
                                    }

                                    if (find_mssql_field == "UOM")
                                    {
                                        string UOMFROMMSSQL = string.Empty;

                                        map.Iterate<KeyValuePair<string, string>>((mssql_fields, mssqIndx) =>
                                        {
                                            if (mssql_fields.Key == "UOM")
                                            {
                                                UOMFROMMSSQL = mssql_fields.Value;

                                                if (uniqueKeyList.ContainsKey(productCode))
                                                {
                                                    var value = uniqueKeyList.Where(pair => pair.Key == productCode)
                                                                .Select(pair => pair.Value)
                                                                .FirstOrDefault();
                                                    uomName = value;
                                                    Console.WriteLine(value);
                                                }

                                                if (UOMFROMMSSQL == uomName)
                                                {
                                                    finalUOM = UOMFROMMSSQL;
                                                    Database.Sanitize(ref UOMFROMMSSQL);
                                                    row += inIdx == 0 ? "('" + UOMFROMMSSQL + "" : "','" + UOMFROMMSSQL;
                                                }
                                            }
                                        });

                                        NoMssqlField = false;
                                        addedToRow = true;
                                    }

                                    map.Iterate<KeyValuePair<string, string>>((mssql_fields, mssqIndx) =>
                                    {
                                        if (find_mssql_field.EcodeContains(mssql_fields.Key))
                                        {
                                            string tmp = string.Empty;

                                            tmp = LogicParser.Parse(mssql_fields.Key, find_mssql_field, map, nullfield)[mssql_fields.Key];

                                            //do looping for mysql field
                                            for (int isql = 0; isql < mysqlFieldList.Count; isql++)
                                            {
                                                string eachField = mysqlFieldList[isql].ToString();

                                                if (!addedToRow)
                                                {
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
                                                if (row.Contains(tmp) == false)
                                                {
                                                    Database.Sanitize(ref tmp);
                                                    row += inIdx == 0 ? "('" + tmp + "" : "','" + tmp;
                                                    addedToRow = true;
                                                }
                                            }

                                            NoMssqlField = false;
                                        }
                                    });

                                    if (NoMssqlField)
                                    {
                                        if (!addedToRow)
                                        {
                                            string tmp = LogicParser.Parse(corr_mysql_field, find_mssql_field, map, nullfield)[corr_mysql_field];
                                            Database.Sanitize(ref tmp);
                                            row += inIdx == 0 ? "('" + tmp + "" : "','" + tmp;
                                        }
                                    }
                                });

                                row += "')";
                                if (finalUOM != string.Empty)
                                {
                                    valueString.Add(row);
                                    RecordCount++;

                                    if (valueString.Count % 2000 == 0)
                                    {
                                        string values = valueString.Join(",");

                                        insertQuery = insertQuery.ReplaceAll(values, "@values");

                                        mysql.Insert(insertQuery);
                                        mysql.Message("ATC WH QTY Query: " + insertQuery);
                                        Thread.Sleep(5000);

                                        insertQuery = insertQuery.ReplaceAll("@values", values);
                                        valueString.Clear();

                                        logger.message = string.Format("{0} warehouse quantity records is inserted into " + mysqlconfig.config_database, RecordCount);
                                        logger.Broadcast();
                                    }
                                }

                            });

                            if (valueString.Count > 0)
                            {
                                string values = valueString.Join(",");

                                insertQuery = insertQuery.ReplaceAll(values, "@values");

                                mysql.Insert(insertQuery);
                                mysql.Message("ATC WH QTY Query: " + insertQuery);
                                Thread.Sleep(5000);
                                mysql.Insert("UPDATE cms_product p JOIN cms_warehouse_stock ws ON p.product_code = ws.product_code SET p.updated_at = ws.updated_at WHERE ws.updated_at > p.updated_at");
                                insertQuery = insertQuery.ReplaceAll("@values", values);

                                logger.message = string.Format("{0} warehouse quantity records is inserted into " + mysqlconfig.config_database, RecordCount);
                                logger.Broadcast();
                                valueString.Clear();
                            }

                            uniqueKeyList.Clear();
                            itemList.Clear();
                            warehouseItemList.Clear();

                            mysql.Insert("INSERT INTO cms_update_time(table_name, updated_at) VALUES('cms_warehouse_stock', NOW()) ON DUPLICATE KEY UPDATE updated_at = VALUES(updated_at)");

                            RecordCount = 0; /* reset count for the next database */
                            mysqlFieldList.Clear();
                            queryResult.Clear();
                        });
                        mssql_rule.Clear();
                        warehouseItemList.Clear();
                    });

                    slog.action_identifier = Constants.Action_ATCWarehouseQtySync;
                    slog.action_details = Constants.Tbl_cms_warehouse_stock + Constants.Is_Finished + string.Format(" ({0}) records", RecordCount);
                    slog.action_failure = 0;
                    slog.action_failure_message = string.Empty;
                    slog.action_time = DateTime.Now.ToString();

                    DateTime endTime = DateTime.Now;
                    TimeSpan ts = endTime - startTime;
                    Console.WriteLine("Completed in: " + ts.TotalSeconds.ToString("F"));

                    LocalDB.InsertSyncLog(slog);
                    logger.message = "ATC warehouse quantity sync finished in (" + ts.TotalSeconds.ToString("F") + " seconds)";
                    logger.Broadcast();

                });

                thread.Start();

            }
            catch (ThreadAbortException e)
            {
                DpprException ex = new DpprException
                {
                    file_name = "JobATCWarehouseQtySync",
                    exception = e.Message,
                    time = DateTime.Now.ToString()
                };
                LocalDB.InsertException(ex);

                Console.WriteLine(Constants.Thread_Exception + e.Message);
            }
        }
    }
}