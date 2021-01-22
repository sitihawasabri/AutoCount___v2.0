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
    class JobAPSInvoiceDetailSync :IJob
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
                    slog.action_identifier = Constants.Action_APSInvoiceDetailSync;
                    slog.action_details = Constants.Tbl_cms_invoice_details + Constants.Is_Starting;
                    slog.action_failure = 0;
                    slog.action_failure_message = string.Empty;
                    slog.action_time = DateTime.Now.ToString();

                    DateTime startTime = DateTime.Now;

                    LocalDB.InsertSyncLog(slog);

                    logger.message = "APS invoice detail sync is running";
                    logger.Broadcast();

                    //ArrayList invoicedetail = mssql.Select("SELECT do.varTrxNo, doT.charItemCodePrint, doT.varModel, doT.decUnitPrice, doT.decOrderQty, doT.decUnitPrice*doT.decOrderQty as totalPrice, doT.varStockUnitNm, 0 as discount from Sal_DODetailsTbl doT inner join Sal_DOTbl do on doT.intDONo = do.intDONo where do.dtCreatedDate >= cast('" + dateFrom + "' as Date) and do.dtCreatedDate <= cast('" + dateTo + "' as Date) order by varTrxNo desc");

                    //query = "INSERT INTO cms_invoice_details(invoice_code, item_code, item_name, item_price, quantity, total_price, uom, discount) VALUES ";
                    //updateQuery = " ON DUPLICATE KEY UPDATE quantity = VALUES(quantity), total_price = VALUES(total_price), item_price = VALUES(item_price)";

                    //nvoiceCode = obj["varTrxNo"];
                    //itemCode = obj["charItemCodePrint"];
                    //itemName = obj["varModel"];
                    //itemPrice = obj["decUnitPrice"];
                    //quantity = obj["decOrderQty"];
                    //totalPrice = obj["totalPrice"];
                    //uom = obj["varStockUnitNm"];
                    //discount = obj["discount"];

                    //{"mysql":"invoice_code", "mssql":"varTrxNo"},{"mysql":"item_code", "mssql":"charItemCodePrint"},{"mysql":"item_name", "mssql":"varModel"},{"mysql":"item_price", "mssql":"decUnitPrice"},{"mysql":"quantity", "mssql":"decOrderQty"},{"mysql":"total_price", "mssql":"totalPrice"},{"mysql":"uom", "mssql":"varStockUnitNm"},{"mysql":"discount", "mssql":"discount"}
                    //exclude: invoice_code, item_code, item_name

                    List<DpprMySQLconfig> mysql_list = LocalDB.GetRemoteDatabaseConfig().Distinct().ToList();
                    List<DpprSQLServerconfig> mssql_list = LocalDB.GetRemoteSQLServerConfig();

                    mysql_list.Iterate<DpprMySQLconfig>((mysql_config, index) =>
                    {
                        DpprMySQLconfig mysqlconfig = mysql_list[index];
                        Database mysql = new Database();
                        mysql.Connect(index: index);

                        CheckBackendRule checkDB = new CheckBackendRule(mysql: mysql);
                        dynamic jsonRule = checkDB.CheckTablesExist().GetSettingByTableName("cms_invoice_details");

                        Dictionary<string, string> cms_updated_time = mysql.GetUpdatedTime("cms_invoice_details");

                        ArrayList mssql_rule = new ArrayList();

                        if (jsonRule != null)
                        {
                            foreach (var key in jsonRule)
                            {
                                if (key.mssql != null && key.mssql.GetType().ToString() == "Newtonsoft.Json.Linq.JArray")
                                {
                                    //string query = "SELECT do.varTrxNo, doT.charItemCodePrint, doT.varModel, doT.decUnitPrice, doT.decOrderQty, doT.decUnitPrice*doT.decOrderQty as totalPrice, doT.varStockUnitNm, 0 as discount from Sal_DODetailsTbl doT inner join Sal_DOTbl do on doT.intDONo = do.intDONo where do.dtCreatedDate >= cast('@dateFrom' as Date) and do.dtCreatedDate <= cast('@dateTo' as Date) order by varTrxNo desc";
                                    string query = "SELECT do.varTrxNo, doT.charItemCodePrint, doT.varModel, doT.decUnitPrice, doT.decOrderQty, doT.decUnitPrice*doT.decOrderQty as totalPrice, doT.varStockUnitNm, 0 as discount from Sal_DODetailsTbl doT inner join Sal_DOTbl do on doT.intDONo = do.intDONo @dateQuery order by varTrxNo desc";
                                    string date_query = " where do.dtCreatedDate >= cast('@dateFrom' as Date) and do.dtCreatedDate <= cast('@dateTo' as Date) ";

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
                            throw new Exception("APS Invoice Details sync requires backend rules");
                        }

                        mssql_rule.Iterate<APSRule>((database, idx) =>
                        {
                            SQLServer mssql = new SQLServer();
                            mssql.Connect(dbname: database.DBname);

                            Console.WriteLine(database.Query);

                            ArrayList queryResult = mssql.Select(database.Query);
                            mssql.Message("Invoice Dtl Query [" + database.DBname + "] ---> " + database.Query);

                            string mysql_insert = string.Empty;
                            string mssql_insert = string.Empty;

                            string insertQuery = "INSERT INTO cms_invoice_details (@columns) VALUES @values ON DUPLICATE KEY UPDATE @update_columns";
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

                            Console.WriteLine(insertQuery);

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
                                            addedToRow = true;
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

                                    logger.message = string.Format("{0} invoice details records is inserted into " + mysqlconfig.config_database, RecordCount);
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

                                logger.message = string.Format("{0} invoice details records is inserted into " + mysqlconfig.config_database, RecordCount);
                                logger.Broadcast();
                            }

                            if (cms_updated_time.Count > 0)
                            {
                                mysql.Insert("UPDATE cms_update_time SET updated_at = NOW() WHERE table_name = 'cms_invoice_details'");
                            }
                            else
                            {
                                mysql.Insert("INSERT INTO cms_update_time(table_name, updated_at) VALUES('cms_invoice_details', NOW())");
                            }

                            RecordCount = 0; /* reset count */
                        });
                    });

                    slog.action_identifier = Constants.Action_APSInvoiceDetailSync;
                    slog.action_details = Constants.Tbl_cms_invoice_details + Constants.Is_Finished + string.Format(" ({0}) records", RecordCount);
                    slog.action_failure = 0;
                    slog.action_failure_message = string.Empty;
                    slog.action_time = DateTime.Now.ToString();

                    DateTime endTime = DateTime.Now;
                    TimeSpan ts = endTime - startTime;
                    Console.WriteLine("Completed in: " + ts.TotalSeconds.ToString("F"));

                    LocalDB.InsertSyncLog(slog);

                    logger.message = "APS invoice detail sync finished in (" + ts.TotalSeconds.ToString("F") + " seconds)";
                    logger.Broadcast();

                });

                thread.Start();
                //thread.Join();

            }
            catch (ThreadAbortException e)
            {
                DpprException ex = new DpprException
                {
                    file_name = "JobAPSInvoiceDetailSync",
                    exception = e.Message,
                    time = DateTime.Now.ToString()
                };
                LocalDB.InsertException(ex);

                Console.WriteLine(Constants.Thread_Exception + e.Message);
            }
        }
    }
}