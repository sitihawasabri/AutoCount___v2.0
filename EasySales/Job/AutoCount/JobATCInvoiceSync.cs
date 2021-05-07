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
    class JobATCInvoiceSync : IJob
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
                    slog.action_identifier = Constants.Action_ATCInvoiceSync;
                    slog.action_details = Constants.Tbl_cms_invoice + Constants.Is_Starting;
                    slog.action_failure = 0;
                    slog.action_failure_message = string.Empty;
                    slog.action_time = DateTime.Now.ToString();

                    DateTime startTime = DateTime.Now;

                    LocalDB.DBCleanup();
                    LocalDB.InsertSyncLog(slog);

                    logger.message = "ATC invoice sync is running";
                    logger.Broadcast();

                    List<DpprMySQLconfig> mysql_list = LocalDB.GetRemoteDatabaseConfig().Distinct().ToList();
                    List<DpprSQLServerconfig> mssql_list = LocalDB.GetRemoteSQLServerConfig();

                    mysql_list.Iterate<DpprMySQLconfig>((mysql_config, index) =>
                    {
                        DpprMySQLconfig mysqlconfig = mysql_list[index];
                        Database mysql = new Database();
                        mysql.Connect(index: index);

                        CheckBackendRule checkDB = new CheckBackendRule(mysql: mysql);
                        dynamic jsonRule = checkDB.CheckTablesExist().GetSettingByTableName("cms_invoice_atc");

                        Dictionary<string, string> cms_updated_time = mysql.GetUpdatedTime("cms_invoice"); 

                        ArrayList mssql_rule = new ArrayList();

                        if (jsonRule != null)
                        {
                            foreach (var key in jsonRule)
                            {
                                if (key.mssql != null && key.mssql.GetType().ToString() == "Newtonsoft.Json.Linq.JArray")
                                {
                                    string query = "SELECT SourceType,DocNo, DebtorCode, DocDate, NetTotal, Outstanding, Cancelled, DueDate, SalesAgent FROM dbo.ARinvoice WHERE JournalType = 'SALES' AND DebtorCode IS NOT NULL ORDER BY docdate";
                                    
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
                            throw new Exception("ATC Invoice sync requires backend rules");
                        }

                        ArrayList salespersonFromDb = mysql.Select("SELECT login_id, UPPER(TRIM(staff_code)) AS staff_code FROM cms_login");
                        Dictionary<string, string> salespersonList = new Dictionary<string, string>();

                        for (int i = 0; i < salespersonFromDb.Count; i++)
                        {
                            Dictionary<string, string> each = (Dictionary<string, string>)salespersonFromDb[i];
                            salespersonList.Add(each["staff_code"], each["login_id"]);
                        }
                        salespersonFromDb.Clear();

                        mssql_rule.Iterate<ATCRule>((database, idx) =>
                        {
                            SQLServer mssql = new SQLServer();
                            mssql.Connect(dbname: database.DBname);

                            //Console.WriteLine(database.Query);

                            ArrayList queryResult = mssql.Select(database.Query);
                            if(queryResult.Count > 0)
                            {
                                logger.Broadcast("Invoice to be inserted: " + queryResult.Count);
                                mysql.Insert("UPDATE cms_invoice SET cancelled = 'T'"); //deactivate all first
                            }

                            string mysql_insert = string.Empty;
                            string mssql_insert = string.Empty;

                            string insertQuery = "INSERT INTO cms_invoice (@columns) VALUES @values ON DUPLICATE KEY UPDATE @update_columns";
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

                                    if (find_mssql_field == "DocDate")
                                    {
                                        RecordCount++;
                                        string docDate = string.Empty;

                                        map.Iterate<KeyValuePair<string, string>>((mssql_fields, mssqIndx) =>
                                        {
                                            if (mssql_fields.Key == "DocDate")
                                            {
                                                docDate = mssql_fields.Value;
                                                docDate = Convert.ToDateTime(docDate).ToString("yyyy-MM-dd HH:mm:ss");
                                            }
                                        });

                                        row += inIdx == 0 ? "('" + docDate + "" : "','" + docDate;

                                        NoMssqlField = false;
                                        addedToRow = true;
                                    }
                                    
                                    if (find_mssql_field == "DueDate")
                                    {
                                        string dueDate = string.Empty;

                                        map.Iterate<KeyValuePair<string, string>>((mssql_fields, mssqIndx) =>
                                        {
                                            if (mssql_fields.Key == "DueDate")
                                            {
                                                dueDate = mssql_fields.Value;
                                                dueDate = Convert.ToDateTime(dueDate).ToString("yyyy-MM-dd HH:mm:ss");
                                            }
                                        });

                                        row += inIdx == 0 ? "('" + dueDate + "" : "','" + dueDate;

                                        NoMssqlField = false;
                                        addedToRow = true;
                                    }

                                    if (find_mssql_field == "SalesAgent")
                                    {
                                        string salespersonId = string.Empty;

                                        map.Iterate<KeyValuePair<string, string>>((mssql_fields, mssqIndx) =>
                                        {
                                            if (mssql_fields.Key == "SalesAgent")
                                            {
                                                string salespersonCode = mssql_fields.Value;

                                                //Console.WriteLine("salespersonCode:" + salespersonCode);
                                                if (string.IsNullOrEmpty(salespersonCode) || !salespersonList.TryGetValue(salespersonCode, out salespersonId))
                                                {
                                                    salespersonId = "0";
                                                }
                                            }
                                        });

                                        row += inIdx == 0 ? "('" + salespersonId + "" : "','" + salespersonId;

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
                                //Console.WriteLine(row);

                                valueString.Add(row);

                                if (valueString.Count % 2000 == 0)
                                {
                                    string values = valueString.Join(",");

                                    insertQuery = insertQuery.ReplaceAll(values, "@values");

                                    mysql.Insert(insertQuery);
                                    mysql.Message("Invoice Query ====> " + insertQuery);

                                    insertQuery = insertQuery.ReplaceAll("@values", values);
                                    valueString.Clear();

                                    logger.message = string.Format("{0} invoice records is inserted into " + mysqlconfig.config_database, RecordCount);
                                    logger.Broadcast();
                                }
                            });

                            if (valueString.Count > 0)
                            {
                                string values = valueString.Join(",");

                                insertQuery = insertQuery.ReplaceAll(values, "@values");

                                mysql.Insert(insertQuery);
                                mysql.Message("Invoice Query ====> " + insertQuery);

                                insertQuery = insertQuery.ReplaceAll("@values", values);
                                valueString.Clear();

                                logger.message = string.Format("{0} invoice records is inserted into " + mysqlconfig.config_database, RecordCount);
                                logger.Broadcast();
                            }
                            
                            mysql.Insert("INSERT INTO cms_update_time(table_name, updated_at) VALUES('cms_invoice', NOW()) ON DUPLICATE KEY UPDATE updated_at = VALUES(updated_at)");

                            RecordCount = 0; /* reset count */
                            queryResult.Clear();
                            mysqlFieldList.Clear();
                            salespersonList.Clear();
                        });
                        mssql_rule.Clear();
                        cms_updated_time.Clear();
                    });

                    mysql_list.Clear();
                    mssql_list.Clear();

                    slog.action_identifier = Constants.Action_ATCInvoiceSync;
                    slog.action_details = Constants.Tbl_cms_invoice + Constants.Is_Finished + string.Format(" ({0}) records", RecordCount);
                    slog.action_failure = 0;
                    slog.action_failure_message = string.Empty;
                    slog.action_time = DateTime.Now.ToString();

                    DateTime endTime = DateTime.Now;
                    TimeSpan ts = endTime - startTime;
                    //Console.WriteLine("Completed in: " + ts.TotalSeconds.ToString("F"));

                    LocalDB.InsertSyncLog(slog);
                    logger.message = "ATC invoice sync finished in (" + ts.TotalSeconds.ToString("F") + " seconds)";
                    logger.Broadcast();

                });

                thread.Start();
                //thread.Join();

            }
            catch (ThreadAbortException e)
            {
                DpprException ex = new DpprException
                {
                    file_name = "JobATCInvoiceSync",
                    exception = e.Message,
                    time = DateTime.Now.ToString()
                };
                LocalDB.InsertException(ex);

                Console.WriteLine(Constants.Thread_Exception + e.Message);
            }
        }
    }
}