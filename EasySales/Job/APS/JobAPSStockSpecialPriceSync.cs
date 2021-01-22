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

namespace EasySales.Job.APS
{
    [DisallowConcurrentExecution]
    public class JobAPSStockSpecialPriceSync : IJob
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
                    slog.action_identifier = Constants.Action_APSStockSpecialPriceSync;                                 
                    slog.action_details = Constants.Tbl_cms_product_price_v2 + Constants.Is_Starting;                   
                    slog.action_failure = 0;
                    slog.action_failure_message = string.Empty;
                    slog.action_time = DateTime.Now.ToString();

                    DateTime startTime = DateTime.Now;

                    LocalDB.InsertSyncLog(slog);

                    logger.message = "APS stock special price sync is running";
                    logger.Broadcast();

                    List<DpprMySQLconfig> mysql_list = LocalDB.GetRemoteDatabaseConfig().Distinct().ToList();
                    List<DpprSQLServerconfig> mssql_list = LocalDB.GetRemoteSQLServerConfig();

                    mysql_list.Iterate<DpprMySQLconfig>((mysql_config, index) =>
                    {
                        DpprMySQLconfig mysqlconfig = mysql_list[index];
                        Database mysql = new Database();
                        mysql.Connect(index: index);

                        CheckBackendRule checkDB = new CheckBackendRule(mysql: mysql);
                        dynamic jsonRule = checkDB.CheckTablesExist().GetSettingByTableName("cms_product_price_v2");

                        Dictionary<string, string> cms_updated_time = mysql.GetUpdatedTime("cms_product_price_v2");

                        ArrayList mssql_rule = new ArrayList();

                        if (jsonRule != null)
                        {
                            foreach (var key in jsonRule)
                            {
                                if (key.mssql != null && key.mssql.GetType().ToString() == "Newtonsoft.Json.Linq.JArray")
                                {
                                    string query = "SELECT DISTINCT stk.charItemCode as [ItemCode] , su.varStockUnitNm as [UOM], stk.decSp1Qty as [Sp1], stk.decSp2qty as [Sp2] , stk.decSp3QTy as [Sp3] , stk.decSp4Qty as [Sp4] , stk.decSp5Qty as [Sp5], stk.decsellingprice1 as [Price1], stk.decsellingprice2 as [Price2], stk.decsellingprice3 as [Price3], stk.decsellingprice4 as [Price4], stk.decsellingprice5 as [Price5], isnull(pmd.decSP4Qty, 0) as [Sp6], pmd.decSP4 as [Price6], pm.varTitle, pm.varRemarks from inv_stocktbl stk inner join inv_stockunittbl su on stk.intStockUnitID = su.intStockUnitID left outer join Sal_PromotionDetailsTbl pmd on stk.intInvID = pmd.intInvID left outer join Sal_PromotionTbl pm on pmd.intPromotionID = pm.intPromotionID and dtPromotionDate <= getdate() and dtPromotionEndDate >= getdate() WHERE stk.charItemCode != '' and stk.charItemCode not like '%deleted%' @andclause";

                                    string where_clause = " ";
                                    if (cms_updated_time.Count > 0)
                                    {
                                        string updated_at = cms_updated_time["updated_at"].ToString().MSSQLdate();
                                        where_clause = string.Format(" AND stk.dtModifyDate >='{0}';", updated_at);
                                    }

                                    query += where_clause;

                                    var mssql_rules = key.mssql;
                                    foreach (var db in mssql_rules)
                                    {
                                        string andclause = string.Empty;
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
                                        
                                        if (db.andclause != null)
                                        {
                                            andclause = db.andclause;
                                        }

                                        query = query.ReplaceAll(andclause, "@andclause");

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
                            throw new Exception("APS Stock Special Price sync requires backend rules");
                        }

                        mssql_rule.Iterate<APSRule>((database, idx) =>
                        {
                            SQLServer mssql = new SQLServer();
                            mssql.Connect(dbname: database.DBname);

                            Console.WriteLine(database.Query);

                            ArrayList queryResult = mssql.Select(database.Query);

                            string mysql_insert = string.Empty;
                            string mssql_insert = string.Empty;

                            string insertQuery = "INSERT INTO cms_product_price_v2 (@columns) VALUES @values ON DUPLICATE KEY UPDATE @update_columns";
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
                                for (int ixx = 1; ixx < 7; ixx++)
                                {
                                    RecordCount++;
                                    int qty = 0;
                                    string row = string.Empty;
                                    database.Include.Iterate<Dictionary<string, string>>((include, inIdx) =>
                                    {
                                        string nullfield = include["nullfield"];
                                        string find_mssql_field = include["mssql"];
                                        string corr_mysql_field = include["mysql"];

                                        bool NoMssqlField = true;
                                        bool addedToRow = false;

                                        if (corr_mysql_field == "product_price")
                                        {
                                            map.Iterate<KeyValuePair<string, string>>((mssql_fields, mssqIndx) =>
                                            {
                                                string priceNo = "Price" + ixx;
                                                string _price = string.Empty;
                                                double roundPrice = 0.00;

                                                if (mssql_fields.Key == priceNo)
                                                {
                                                    _price = mssql_fields.Value;

                                                    if (_price == "0.000000")//0.000000
                                                    {
                                                        _price = "0";
                                                    }

                                                    double.TryParse(_price, out double price);
                                                    roundPrice = Math.Round(price, 2);
                                                    row += inIdx == 0 ? "('" + roundPrice + "" : "','" + roundPrice;
                                                    NoMssqlField = false;
                                                    addedToRow = true;
                                                }
                                            });
                                            NoMssqlField = false;
                                            addedToRow = true;
                                        }

                                        if (corr_mysql_field == "quantity")
                                        {
                                            map.Iterate<KeyValuePair<string, string>>((mssql_fields, mssqIndx) =>
                                            {
                                                string quanNo = "Sp" + ixx;

                                                string _quantity = string.Empty;
                                                if (mssql_fields.Key == quanNo)
                                                {
                                                    _quantity = mssql_fields.Value;
                                                    int.TryParse(_quantity, out int quantity);
                                                    row += inIdx == 0 ? "('" + quantity + "" : "','" + quantity;
                                                    
                                                    qty = quantity;
                                                }
                                            });
                                            NoMssqlField = false;
                                            addedToRow = true;
                                        }

                                        if (find_mssql_field == "varTitle")
                                        {
                                            string priceCat = string.Empty;
                                            map.Iterate<KeyValuePair<string, string>>((mssql_fields, mssqIndx) =>
                                            {
                                                if (mssql_fields.Key == "varTitle")
                                                {
                                                    if (qty != 0)
                                                    {
                                                        if (ixx != 6)
                                                        {
                                                            priceCat = "SP " + ixx;
                                                            Database.Sanitize(ref priceCat);
                                                            row += inIdx == 0 ? "('" + priceCat + "" : "','" + priceCat;
                                                        }
                                                        else
                                                        {
                                                            priceCat = mssql_fields.Value;
                                                            Database.Sanitize(ref priceCat);
                                                            row += inIdx == 0 ? "('" + priceCat + "" : "','" + priceCat;
                                                        }
                                                    }
                                                    //else                      ##commented 10072020
                                                    //{
                                                    //    Database.Sanitize(ref priceCat);
                                                    //    row += inIdx == 0 ? "('" + priceCat + "" : "','" + priceCat;
                                                    //}
                                                }
                                            });
                                            NoMssqlField = false;
                                            addedToRow = true;
                                        }

                                        if (find_mssql_field == "varRemarks")
                                        {
                                            string remarks = string.Empty;
                                            
                                            map.Iterate<KeyValuePair<string, string>>((mssql_fields, mssqIndx) =>
                                            {
                                                if (mssql_fields.Key == "varRemarks")
                                                {
                                                    if (qty != 0)
                                                    {
                                                        if (ixx != 6)
                                                        {
                                                            remarks = "Price" + ixx;
                                                        }
                                                        else
                                                        {
                                                            remarks = mssql_fields.Value;
                                                        }
                                                    }
                                                    Database.Sanitize(ref remarks);
                                                    row += inIdx == 0 ? "('" + remarks + "" : "','" + remarks;
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
                                                    if(!addedToRow)
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
                                            if(!addedToRow)
                                            {
                                                string tmp = LogicParser.Parse(corr_mysql_field, find_mssql_field, map, nullfield)[corr_mysql_field];
                                                Database.Sanitize(ref tmp);
                                                row += inIdx == 0 ? "('" + tmp + "" : "','" + tmp;
                                            }
                                        }
                                    });

                                    row += "')";

                                    if(qty != 0)            //## added 10072020
                                    {
                                        valueString.Add(row);
                                    }
                                }

                                if (valueString.Count % 2000 == 0)
                                {
                                    string values = valueString.Join(",");

                                    insertQuery = insertQuery.ReplaceAll(values, "@values");

                                    mysql.Insert(insertQuery);
                                    mysql.Message("Special Price: " + insertQuery);

                                    insertQuery = insertQuery.ReplaceAll("@values", values);
                                    valueString.Clear();

                                    logger.message = string.Format("{0} stock special price records is inserted into " + mysqlconfig.config_database, RecordCount);
                                    logger.Broadcast();
                                }
                            });

                            if (valueString.Count > 0)
                            {
                                string values = valueString.Join(",");

                                insertQuery = insertQuery.ReplaceAll(values, "@values");

                                mysql.Insert(insertQuery);
                                mysql.Message("Special Price: " + insertQuery);

                                insertQuery = insertQuery.ReplaceAll("@values", values);
                                valueString.Clear();

                                logger.message = string.Format("{0} stock special price records is inserted into " + mysqlconfig.config_database, RecordCount);
                                logger.Broadcast();
                            }

                            if (cms_updated_time.Count > 0)
                            {
                                mysql.Insert("UPDATE cms_update_time SET updated_at = NOW() WHERE table_name = 'cms_product_price_v2'");
                            }
                            else
                            {
                                mysql.Insert("INSERT INTO cms_update_time(table_name, updated_at) VALUES('cms_product_price_v2', NOW())");
                            }

                            RecordCount = 0; /* reset count for the next database */
                        });
                    });

                    slog.action_identifier = Constants.Action_APSStockSpecialPriceSync;
                    slog.action_details = Constants.Tbl_cms_product_price_v2 + Constants.Is_Finished + string.Format(" ({0}) records", RecordCount);
                    slog.action_failure = 0;
                    slog.action_failure_message = string.Empty;
                    slog.action_time = DateTime.Now.ToString();

                    DateTime endTime = DateTime.Now;
                    TimeSpan ts = endTime - startTime;
                    Console.WriteLine("Completed in: " + ts.TotalSeconds.ToString("F"));

                    LocalDB.InsertSyncLog(slog);

                    logger.message = "APS stock special price sync finished in (" + ts.TotalSeconds.ToString("F") + " seconds)";
                    logger.Broadcast();

                });

                thread.Start();
                //thread.Join();

            }
            catch (ThreadAbortException e)
            {
                DpprException ex = new DpprException
                {
                    file_name = "JobAPSStockSpecialPriceSync",
                    exception = e.Message,
                    time = DateTime.Now.ToString()
                };
                LocalDB.InsertException(ex);

                Console.WriteLine(Constants.Thread_Exception + e.Message);
            }
        }
    }
}