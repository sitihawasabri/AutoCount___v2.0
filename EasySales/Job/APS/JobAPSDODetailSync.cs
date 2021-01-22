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
    class JobAPSDODetailSync : IJob
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
                    slog.action_identifier = Constants.Action_APSDODetailSync;
                    slog.action_details = Constants.Tbl_cms_do_details + Constants.Is_Starting;
                    slog.action_failure = 0;
                    slog.action_failure_message = string.Empty;
                    slog.action_time = DateTime.Now.ToString();

                    DateTime startTime = DateTime.Now;

                    LocalDB.InsertSyncLog(slog);

                    logger.message = "APS DO details sync is running";
                    logger.Broadcast();

                    //{"mysql":"do_code", "mssql":"varTrxNo"},{"mysql":"item_code", "mssql":"charItemCode"},{"mysql":"item_name", "mssql":"varDesc"},{"mysql":"item_price", "mssql":"decUnitPrice"},{"mysql":"quantity", "mssql":"decOrderQty"},{"mysql":"total_price", "mssql":"totalPrice"},{"mysql":"uom", "mssql":"varStockUnitNm"},{"mysql":"discount", "mssql":"decDiscPercent1"}

                    //ArrayList deliveryOrderDetail = mssql.Select("SELECT varTrxNo, charItemCode, dod.varDesc, dod.varModel, decOrderQty, varStockUnitNm, decUnitPrice, decDiscPercent1, decAfterCalcUnitPrice, dod.decAfterCalcUnitPrice * dod.decOrderQty as totalPrice FROM vwActiveSal_DOTbl do INNER JOIN Sal_DODetailsTbl dod ON do.intDONo = dod.intDONo INNER JOIN Inv_StockTbl stk ON dod.intInvID = stk.intInvID WHERE do.dtCreatedDate >= CAST('" +dateFrom+ "' as Date) AND do.dtCreatedDate <= CAST('" +dateTo+ "' as Date) ORDER BY varTrxNo DESC");

                    //query = "INSERT INTO cms_do_details(do_code, item_code, item_name, item_price, quantity, total_price, uom, discount) VALUES ";
                    //    updateQuery = " ON DUPLICATE KEY UPDATE quantity = VALUES(quantity), total_price = VALUES(total_price), item_price = VALUES(item_price)";

                    //        doCode = obj["varTrxNo"];
                    //        doCode = doCode.Replace("\\", "\\\\");

                    //        itemCode = obj["charItemCode"];
                    //        itemCode = itemCode.Replace("'", "\'");
                            
                    //        itemName = obj["varDesc"];
                    //        itemName = itemName.Replace("'", "\'");

                    //        itemPrice = obj["decUnitPrice"];
                    //        qty = obj["decOrderQty"];
                    //        totalPrice = obj["totalPrice"];
                    //        uom = obj["varStockUnitNm"];
                    //        discount = obj["decDiscPercent1"] + "%";

                    List<DpprMySQLconfig> mysql_list = LocalDB.GetRemoteDatabaseConfig().Distinct().ToList();
                    List<DpprSQLServerconfig> mssql_list = LocalDB.GetRemoteSQLServerConfig();

                    mysql_list.Iterate<DpprMySQLconfig>((mysql_config, index) =>
                    {
                        DpprMySQLconfig mysqlconfig = mysql_list[index];
                        Database mysql = new Database();
                        mysql.Connect(index: index);

                        CheckBackendRule checkDB = new CheckBackendRule(mysql: mysql);
                        dynamic jsonRule = checkDB.CheckTablesExist().GetSettingByTableName("cms_do_details");

                        Dictionary<string, string> cms_updated_time = mysql.GetUpdatedTime("cms_do");

                        ArrayList mssql_rule = new ArrayList();

                        if (jsonRule != null)
                        {
                            foreach (var key in jsonRule)
                            {
                                if (key.mssql != null && key.mssql.GetType().ToString() == "Newtonsoft.Json.Linq.JArray")
                                {
                                    string query = "SELECT varTrxNo, charItemCode, dod.varDesc, dod.varModel, decOrderQty, varStockUnitNm, decUnitPrice, decDiscPercent1, decAfterCalcUnitPrice, dod.decAfterCalcUnitPrice * dod.decOrderQty as totalPrice FROM vwActiveSal_DOTbl do INNER JOIN Sal_DODetailsTbl dod ON do.intDONo = dod.intDONo INNER JOIN Inv_StockTbl stk ON dod.intInvID = stk.intInvID @dateQuery ORDER BY varTrxNo DESC";
                                    string date_query = " WHERE do.dtCreatedDate >= CAST('@dateFrom' as Date) AND do.dtCreatedDate <= CAST('@dateTo' as Date) ";

                                    string updated_at = string.Empty;
                                    DateTime updatedAtDateTime = DateTime.Now;

                                    /* if no date, sync all */
                                    if (cms_updated_time.Count > 0)
                                    {
                                        updated_at = cms_updated_time["updated_at"].ToString().MSSQLdate();
                                        updatedAtDateTime = Convert.ToDateTime(updated_at);

                                        int startMonths = Convert.ToInt32(-12);
                                        int endMonths = Convert.ToInt32(+3);

                                        DateTime _dateTo = updatedAtDateTime.AddMonths(endMonths);
                                        DateTime _dateFrom = updatedAtDateTime.AddMonths(startMonths);

                                        string dateFrom = _dateFrom.ToShortDateString().MSSQLdate();
                                        string dateTo = _dateTo.ToShortDateString().MSSQLdate();

                                        date_query = date_query.ReplaceAll(dateFrom, "@dateFrom");
                                        date_query = date_query.ReplaceAll(dateTo, "@dateTo");

                                        query = query.ReplaceAll(date_query, "@dateQuery");
                                        Console.WriteLine(query);
                                    }
                                    else
                                    {
                                        query = query.ReplaceAll("", "@dateQuery");
                                    }

                                    //int startMonths = Convert.ToInt32(+6);
                                    //int endMonths = Convert.ToInt32(-6);

                                    //DateTime _dateTo = updatedAtDateTime.AddMonths(startMonths);
                                    //DateTime _dateFrom = updatedAtDateTime.AddMonths(endMonths);

                                    //string dateFrom = _dateFrom.ToShortDateString().MSSQLdate();
                                    //string dateTo = _dateTo.ToShortDateString().MSSQLdate();

                                    //query = query.ReplaceAll(dateFrom, "@dateFrom");
                                    //query = query.ReplaceAll(dateTo, "@dateTo");

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
                            throw new Exception("APS Delivery Order Details sync requires backend rules");
                        }

                        mssql_rule.Iterate<APSRule>((database, idx) =>
                        {
                            SQLServer mssql = new SQLServer();
                            mssql.Connect(dbname: database.DBname);
                            mssql.Message("DO Dtl Query [" + database.DBname + "] ---> " + database.Query);

                            Console.WriteLine(database.Query);

                            ArrayList queryResult = mssql.Select(database.Query);

                            string mysql_insert = string.Empty;
                            string mssql_insert = string.Empty;

                            string insertQuery = "INSERT INTO cms_do_details (@columns) VALUES @values ON DUPLICATE KEY UPDATE @update_columns";
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

                                    //itemPrice = obj["decUnitPrice"];
                                    //qty = obj["decOrderQty"];
                                    //totalPrice = obj["totalPrice"];
                                    //uom = obj["varStockUnitNm"];
                                    //discount = obj["decDiscPercent1"] + "%";

                                    if (find_mssql_field == "varTrxNo")
                                    {
                                        string doCode = string.Empty;

                                        map.Iterate<KeyValuePair<string, string>>((mssql_fields, mssqIndx) =>
                                        {
                                            if (mssql_fields.Key == "varTrxNo")
                                            {
                                                doCode = mssql_fields.Value;
                                                doCode = doCode.Replace("\\", "\\\\");
                                            }
                                        });
                                        Database.Sanitize(ref doCode);
                                        row += inIdx == 0 ? "('" + doCode + "" : "','" + doCode;

                                        NoMssqlField = false;
                                        addedToRow = true;
                                    }
                                    
                                    if (find_mssql_field == "charItemCode")
                                    {
                                        string itemCode = string.Empty;

                                        map.Iterate<KeyValuePair<string, string>>((mssql_fields, mssqIndx) =>
                                        {
                                            if (mssql_fields.Key == "charItemCode")
                                            {
                                                itemCode = mssql_fields.Value;
                                                itemCode = itemCode.Replace("\\", "\\\\");
                                            }
                                        });
                                        Database.Sanitize(ref itemCode);
                                        row += inIdx == 0 ? "('" + itemCode + "" : "','" + itemCode;

                                        NoMssqlField = false;
                                        addedToRow = true;
                                    }
                                    
                                    if (find_mssql_field == "varDesc")
                                    {
                                        string itemName = string.Empty;

                                        map.Iterate<KeyValuePair<string, string>>((mssql_fields, mssqIndx) =>
                                        {
                                            if (mssql_fields.Key == "varDesc")
                                            {
                                                itemName = mssql_fields.Value;
                                                itemName = itemName.Replace("'", "\'");
                                            }
                                        });
                                        Database.Sanitize(ref itemName);
                                        row += inIdx == 0 ? "('" + itemName + "" : "','" + itemName;

                                        NoMssqlField = false;
                                        addedToRow = true;
                                    }
                                    
                                    if (find_mssql_field == "decDiscPercent1")
                                    {
                                        string discount = string.Empty;

                                        map.Iterate<KeyValuePair<string, string>>((mssql_fields, mssqIndx) =>
                                        {
                                            if (mssql_fields.Key == "decDiscPercent1")
                                            {
                                                discount = mssql_fields.Value + "%";
                                            }
                                        });

                                        row += inIdx == 0 ? "('" + discount + "" : "','" + discount;

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

                                RecordCount++;
                                valueString.Add(row);

                                if (valueString.Count % 2000 == 0)
                                {
                                    string values = valueString.Join(",");

                                    insertQuery = insertQuery.ReplaceAll(values, "@values");

                                    mysql.Insert(insertQuery);

                                    insertQuery = insertQuery.ReplaceAll("@values", values);
                                    valueString.Clear();

                                    logger.message = string.Format("{0} delivery order details records is inserted into " + mysqlconfig.config_database, RecordCount);
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

                                logger.message = string.Format("{0} delivery order details records is inserted into " + mysqlconfig.config_database, RecordCount);
                                logger.Broadcast();
                            }

                            if (cms_updated_time.Count > 0)
                            {
                                mysql.Insert("UPDATE cms_update_time SET updated_at = NOW() WHERE table_name = 'cms_do'");
                            }
                            else
                            {
                                mysql.Insert("INSERT INTO cms_update_time(table_name, updated_at) VALUES('cms_do', NOW())");
                            }
                          
                        });
                    });

                    slog.action_identifier = Constants.Action_APSDODetailSync;
                    slog.action_details = Constants.Tbl_cms_do_details + Constants.Is_Finished + string.Format(" ({0}) records", RecordCount);
                    slog.action_failure = 0;
                    slog.action_failure_message = string.Empty;
                    slog.action_time = DateTime.Now.ToString();

                    DateTime endTime = DateTime.Now;
                    TimeSpan ts = endTime - startTime;
                    Console.WriteLine("Completed in: " + ts.TotalSeconds.ToString("F"));

                    LocalDB.InsertSyncLog(slog);

                    logger.message = "APS DO details sync finished in (" + ts.TotalSeconds.ToString("F") + " seconds)";
                    logger.Broadcast();

                });

                thread.Start();
                //thread.Join();

            }
            catch (ThreadAbortException e)
            {
                DpprException ex = new DpprException
                {
                    file_name = "JobAPSDoDetailSync",
                    exception = e.Message,
                    time = DateTime.Now.ToString()
                };
                LocalDB.InsertException(ex);

                Console.WriteLine(Constants.Thread_Exception + e.Message);
            }
        }
    }
}
