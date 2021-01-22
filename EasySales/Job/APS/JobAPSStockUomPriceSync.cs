using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EasySales.Model;
using EasySales.Object;
using Quartz;
using System.Linq;
using Microsoft.EntityFrameworkCore.Internal;

namespace EasySales.Job
{
    [DisallowConcurrentExecution]
    public class JobAPSStockUomPriceSync : IJob
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
                    slog.action_identifier = Constants.Action_APSStockUomPriceSync;                                 
                    slog.action_details = Constants.Tbl_cms_product_uom_price_v2 + Constants.Is_Starting;           
                    slog.action_failure = 0;
                    slog.action_failure_message = string.Empty;
                    slog.action_time = DateTime.Now.ToString();

                    DateTime startTime = DateTime.Now;

                    LocalDB.InsertSyncLog(slog);

                    logger.message = "APS stock UOM price sync is running";
                    logger.Broadcast();

                    List<DpprMySQLconfig> mysql_list = LocalDB.GetRemoteDatabaseConfig().Distinct().ToList();
                    List<DpprSQLServerconfig> mssql_list = LocalDB.GetRemoteSQLServerConfig();

                    mysql_list.Iterate<DpprMySQLconfig>((mysql_config, index) =>
                    {
                        DpprMySQLconfig mysqlconfig = mysql_list[index];
                        Database mysql = new Database();
                        mysql.Connect(index: index);

                        CheckBackendRule checkDB = new CheckBackendRule(mysql: mysql);
                        dynamic jsonRule = checkDB.CheckTablesExist().GetSettingByTableName("cms_product_uom_price_v2");

                        Dictionary<string, string> cms_updated_time = mysql.GetUpdatedTime1MonthInterval("cms_product_uom_price_v2");

                        ArrayList mssql_rule = new ArrayList();

                        if (jsonRule != null)
                        {
                            foreach (var key in jsonRule)
                            {
                                if (key.mssql != null && key.mssql.GetType().ToString() == "Newtonsoft.Json.Linq.JArray")
                                {
                                    string query = "SELECT stk.charItemCode as [ItemCode] ,stk.decsellingprice1 as [Price1], stk.varModel as [Model], su.varStockUnitNm as [UOM], stk.decMinimumSP as [MinPrice] from inv_stocktbl stk inner join inv_stockunittbl su on stk.intStockUnitID = su.intStockUnitID where stk.charItemCode != '' and stk.charItemCode not like '%deleted%' and stk.blnIsDelete = 'False' ";

                                    string and_clause = " order by stk.varModel asc;";
                                    if (cms_updated_time.Count > 0)
                                    {
                                        string updated_at = cms_updated_time["updated_at"].ToString().MSSQLdate();
                                        and_clause = string.Format(" AND stk.dtModifyDate >='{0}' order by stk.varModel asc;", updated_at);
                                    }

                                    query += and_clause;

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

                                        APSRule aps_rule = new APSRule()
                                        {
                                            DBname = db.name,
                                            Include = include,
                                            Exclude = exclude,
                                            Query = query
                                        };

                                        mssql_rule.Add(aps_rule);
                                    }
                                }
                            }
                        }
                        else
                        {
                            throw new Exception("APS Product UOM price sync requires backend rules");
                        }

                        mssql_rule.Iterate<APSRule>((database, idx) =>
                        {
                            SQLServer mssql = new SQLServer();
                            mssql.Connect(dbname: database.DBname);
                            //logger.Broadcast(database.Query);
                            Console.WriteLine(database.Query);

                            ArrayList queryResult = mssql.Select(database.Query);

                            string mysql_insert = string.Empty;
                            string mssql_insert = string.Empty;

                            string insertQuery = "INSERT INTO cms_product_uom_price_v2 (@columns) VALUES @values ON DUPLICATE KEY UPDATE @update_columns";
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

                                    if (find_mssql_field == "ItemCode")
                                    {
                                        string itemCode = string.Empty;
                                        string _productId = string.Empty;

                                        map.Iterate<KeyValuePair<string, string>>((mssql_fields, mssqIndx) =>
                                        {
                                            if (mssql_fields.Key == "ItemCode")
                                            {
                                                itemCode = mssql_fields.Value;
                                                RecordCount++;
                                            }
                                        });
                                        Database.Sanitize(ref itemCode);
                                        row += inIdx == 0 ? "('" + itemCode + "" : "','" + itemCode;

                                        NoMssqlField = false;
                                        addedToRow = true;
                                    }

                                    if (find_mssql_field == "Price1")
                                    {
                                        string _price = string.Empty;
                                        map.Iterate<KeyValuePair<string, string>>((mssql_fields, mssqIndx) =>
                                        {
                                            if (mssql_fields.Key == "Price1")
                                            {
                                                _price = mssql_fields.Value;

                                                if (_price == "0.00")
                                                {
                                                    _price = "0";
                                                }
                                            }
                                        });

                                        double.TryParse(_price, out double price);
                                        double roundPrice = Math.Round(price, 2);

                                        row += inIdx == 0 ? "('" + roundPrice + "" : "','" + roundPrice;

                                        NoMssqlField = false;
                                        addedToRow = true;
                                    }

                                    if (find_mssql_field == "MinPrice")
                                    {
                                        string _minPrice = string.Empty;
                                        map.Iterate<KeyValuePair<string, string>>((mssql_fields, mssqIndx) =>
                                        {
                                            if (mssql_fields.Key == "MinPrice")
                                            {
                                                _minPrice = mssql_fields.Value;

                                                if (_minPrice == null || _minPrice == "0.00")
                                                {
                                                    _minPrice = "0";
                                                }
                                            }
                                        });

                                        double.TryParse(_minPrice, out double minPrice);
                                        double roundMinPrice = Math.Round(minPrice, 2);

                                        row += inIdx == 0 ? "('" + roundMinPrice + "" : "','" + roundMinPrice;
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
                                            row += inIdx == 0 ? "('" + updated_at + "" : "','" + updated_at;
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
                                            //logger.Broadcast(mssql_fields.Key + "----------" + tmp);

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

                                    insertQuery = insertQuery.ReplaceAll("@values", values);
                                    valueString.Clear();

                                    logger.message = string.Format("{0} stock uom price records is inserted into " + mysqlconfig.config_database, RecordCount);
                                    logger.Broadcast();
                                }
                            });

                            if (valueString.Count > 0)
                            {
                                string values = valueString.Join(",");

                                insertQuery = insertQuery.ReplaceAll(values, "@values");

                                mysql.Insert(insertQuery);

                                insertQuery = insertQuery.ReplaceAll("@values", values);
                                valueString.Clear();

                                logger.message = string.Format("{0} stock uom price records is inserted into " + mysqlconfig.config_database, RecordCount);
                                logger.Broadcast();
                            }

                            if (cms_updated_time.Count > 0)
                            {
                                mysql.Insert("UPDATE cms_update_time SET updated_at = NOW() WHERE table_name = 'cms_product_uom_price_v2'");
                            }
                            else
                            {
                                mysql.Insert("INSERT INTO cms_update_time(table_name, updated_at) VALUES('cms_product_uom_price_v2', NOW())");
                            }

                            //add the checking --- deactivate old uom
                            ArrayList activeUOM = new ArrayList();
                            ArrayList activeUOMCount = mysql.Select("SELECT COUNT(*) AS active_uom FROM cms_product_uom_price_v2 WHERE active_status = 1;");
                            int activeInDBCount = 0;
                            if (activeUOMCount.Count > 0)
                            {
                                Dictionary<string, string> getCount = (Dictionary<string, string>)activeUOMCount[0];
                                string _activeInDB = getCount["active_uom"];
                                int.TryParse(_activeInDB, out activeInDBCount);
                            }
                            else
                            {
                                goto ENDJOB;
                            }

                            int offset = 0;
                        RUNANOTHERBATCH:
                            ArrayList tmpActiveUOM = mysql.Select("SELECT * FROM cms_product_uom_price_v2 WHERE active_status = 1 LIMIT 3000 OFFSET " + offset + ";");
                            if (tmpActiveUOM.Count > 0)
                            {
                                activeUOM.AddRange(tmpActiveUOM);
                            }

                            if (offset < activeInDBCount)
                            {
                                offset = activeUOM.Count;
                                Task.Delay(3000);
                                goto RUNANOTHERBATCH;
                            }

                            Dictionary<string, string> checkActiveUom = new Dictionary<string, string>();
                            List<HashSet<string>> chunkArrays = new List<HashSet<string>>();
                            HashSet<string> tmp1 = new HashSet<string>();

                            for (int i = 0; i < activeUOM.Count; i++)
                            {
                                Dictionary<string, string> each = (Dictionary<string, string>)activeUOM[i];
                                string id = each["product_uom_price_id"];
                                string code = each["product_code"];
                                string uom = each["product_uom"];
                                string unique = code + uom;
                                string uniqueLowercase = unique.ToLower();
                                checkActiveUom.Add(id, uniqueLowercase);
                            }

                            for (int i = 0; i < activeUOM.Count; i++)
                            {
                                Dictionary<string, string> each = (Dictionary<string, string>)activeUOM[i];
                                string prodCode = each["product_code"].ToString();
                                /* no need to sanitize, result from mysql already sanitized */
                                tmp1.Add(prodCode);

                                if (tmp1.Count % 3000 == 0)
                                {
                                    chunkArrays.Add(tmp1);
                                    tmp1 = new HashSet<string>();
                                }
                            }

                            if (tmp1.Count > 0)
                            {
                                chunkArrays.Add(tmp1);
                            }

                            for (int idd = 0; idd < chunkArrays.Count; idd++)
                            {
                                HashSet<string> productCodeList = (HashSet<string>)chunkArrays[idd];
                                string whereNotInCode = "'" + string.Join("','", productCodeList) + "'";

                                string checkNotInCode = "SELECT stk.dtModifyDate, stk.dtCreatedDate, stk.charItemCode as [ItemCode] ,stk.decsellingprice1 as [Price1], stk.varModel as [Model], su.varStockUnitNm as [UOM], stk.decMinimumSP as [MinPrice] from inv_stocktbl stk inner join inv_stockunittbl su on stk.intStockUnitID = su.intStockUnitID where stk.blnIsDelete = 'False' AND stk.charItemCode IN (" + whereNotInCode + ");";
                                // it will return the items exists in mssql

                                ArrayList activeInMSSQL = mssql.Select(checkNotInCode);
                                if (activeInMSSQL.Count > 0)
                                {
                                    for (int imssql = 0; imssql < activeInMSSQL.Count; imssql++)
                                    {
                                        Dictionary<string, string> pair = (Dictionary<string, string>)activeInMSSQL[imssql];
                                        string _code = pair["ItemCode"];
                                        string _uom = pair["UOM"];
                                        string _unique = _code + _uom;
                                        string _uniqueLowercase = _unique.ToLower();

                                        if (checkActiveUom.Count > 0)
                                        {
                                            if (checkActiveUom.ContainsValue(_uniqueLowercase))
                                            {
                                                var key = checkActiveUom.Where(_pair => _pair.Value == _uniqueLowercase)
                                                            .Select(_pair => _pair.Key)
                                                            .FirstOrDefault();
                                                //Console.WriteLine("key:" + key);
                                                if (key != null)
                                                {
                                                    checkActiveUom.Remove(key);
                                                    //Console.WriteLine("uniqueDict.Count after removed same key: " + checkActiveUom.Count);
                                                }
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    goto ENDJOB;
                                }
                            }

                            if (checkActiveUom.Count > 0)/* the remaining codes in ArrayList activeProducts, are needed to be deactived - set 0 */
                            {
                                //Console.WriteLine("checkActiveProducts.Count: " + checkActiveUom.Count);
                                logger.message = string.Format("{0} uom records to be deactivated in " + mysqlconfig.config_database, checkActiveUom.Count);
                                logger.Broadcast();

                                HashSet<string> idToBeDeactivateList = new HashSet<string>();
                                for (int ix = 0; ix < checkActiveUom.Count; ix++)
                                {
                                    string id = checkActiveUom.ElementAt(ix).Key;
                                    idToBeDeactivateList.Add(id);
                                }

                                string idToBeDeactivate = "'" + string.Join("','", idToBeDeactivateList) + "'";
                                //Console.WriteLine(idToBeDeactivate);
                                mysql.Message("[" + mysqlconfig.config_database + "] idToBeDeactivate: " + idToBeDeactivate);

                                string inactive = "UPDATE cms_product_uom_price_v2 SET active_status = 0 WHERE product_uom_price_id IN (" + idToBeDeactivate + ")";
                                mysql.Insert(inactive);
                                logger.message = string.Format("{0} uom records deactivated in " + mysqlconfig.config_database, checkActiveUom.Count);
                                logger.Broadcast();

                                checkActiveUom.Clear();
                            }
                            ENDJOB:
                            RecordCount = 0; /* reset count for the next database */
                        });
                    }); 

                    slog.action_identifier = Constants.Action_APSStockUomPriceSync;
                    slog.action_details = Constants.Tbl_cms_product_uom_price_v2 + Constants.Is_Finished + string.Format(" ({0}) records", RecordCount);
                    slog.action_failure = 0;
                    slog.action_failure_message = string.Empty;
                    slog.action_time = DateTime.Now.ToString();

                    DateTime endTime = DateTime.Now;
                    TimeSpan ts = endTime - startTime;
                    Console.WriteLine("Completed in: " + ts.TotalSeconds.ToString("F"));

                    LocalDB.InsertSyncLog(slog);
                    logger.message = "APS stock UOM price sync finished in (" + ts.TotalSeconds.ToString("F") + " seconds)";
                    logger.Broadcast();

                });

                thread.Start();
                //thread.Join();

            }
            catch (ThreadAbortException e)
            {
                DpprException ex = new DpprException
                {
                    file_name = "JobAPSStockUomPriceSync",
                    exception = e.Message,
                    time = DateTime.Now.ToString()
                };
                LocalDB.InsertException(ex);

                Console.WriteLine(Constants.Thread_Exception + e.Message);
            }
        }
    }
}