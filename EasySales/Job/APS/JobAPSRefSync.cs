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
    class JobAPSRefSync : IJob
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
                    slog.action_identifier = Constants.Action_APSRefSync;
                    slog.action_details = Constants.Tbl_cms_mobile_module + Constants.Is_Starting;
                    slog.action_failure = 0;
                    slog.action_failure_message = string.Empty;
                    slog.action_time = DateTime.Now.ToString();

                    DateTime startTime = DateTime.Now;

                    LocalDB.InsertSyncLog(slog);

                    logger.message = "APS REF sync is running";
                    logger.Broadcast();

                    List<DpprMySQLconfig> mysql_list = LocalDB.GetRemoteDatabaseConfig().Distinct().ToList();
                    List<DpprSQLServerconfig> mssql_list = LocalDB.GetRemoteSQLServerConfig();

                    mysql_list.Iterate<DpprMySQLconfig>((mysql_config, index) =>
                    {
                        DpprMySQLconfig mysqlconfig = mysql_list[index];
                        Database mysql = new Database();
                        mysql.Connect(index: index);

                        CheckBackendRule checkDB = new CheckBackendRule(mysql: mysql);
                        dynamic jsonRule = checkDB.CheckTablesExist().GetSettingByTableName("app_cart_udf");

                        ArrayList mssql_rule = new ArrayList();

                        if (jsonRule != null)
                        {
                            foreach (var key in jsonRule)
                            {
                                if (key.mssql != null && key.mssql.GetType().ToString() == "Newtonsoft.Json.Linq.JArray")
                                {
                                    //later check again whats the refCodeID for PHONE CALL AND COUNTER
                                    string query = "SELECT intRefCodeID, charRefCode FROM sal_refcodetbl WHERE intRefCodeID IN (54,56)";

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
                            throw new Exception("APS REF sync requires backend rules");
                        }

                        mssql_rule.Iterate<APSRule>((database, idx) =>
                        {
                            SQLServer mssql = new SQLServer();
                            mssql.Connect(dbname: database.DBname);

                            Console.WriteLine(database.Query);

                            ArrayList queryResult = mssql.Select(database.Query);

                            string mysql_insert = string.Empty;
                            string mssql_insert = string.Empty;

                            string insertQuery = "UPDATE cms_mobile_module SET module = 'app_cart_udf', status = '@values' WHERE module = 'app_cart_udf'"; 

                            ArrayList mysqlFieldList = new ArrayList(); /* get all mysql field column */
                            database.Include.Iterate<Dictionary<string, string>>((incDict, incindex) =>
                            {
                                string mysqlField = incDict["mysql"].ToString();
                                mysqlFieldList.Add(mysqlField);
                            });

                            string Value = string.Empty;
                            HashSet<string> Values = new HashSet<string>();

                            queryResult.Iterate<Dictionary<string, string>>((map, i) =>
                            {
                                database.Include.Iterate<Dictionary<string, string>>((include, inIdx) =>
                                {
                                    string nullfield = include["nullfield"];
                                    string find_mssql_field = include["mssql"];
                                    string corr_mysql_field = include["mysql"];

                                    if (find_mssql_field == "value")
                                    {
                                        string charRefCode = string.Empty;
                                        string intRefCodeID = string.Empty;

                                        map.Iterate<KeyValuePair<string, string>>((mssql_fields, mssqIndx) =>
                                        {
                                            if (mssql_fields.Key == "charRefCode")
                                            {
                                                charRefCode = mssql_fields.Value;
                                            }
                                            
                                            if (mssql_fields.Key == "intRefCodeID")
                                            {
                                                intRefCodeID = mssql_fields.Value;
                                            }

                                        });

                                        string options = "{\"name\": \"" + charRefCode + "\",\"value\": \"" + intRefCodeID + "\"}";
                                        Values.Add(options);
                                    }
                                   
                                });
                            });

                            RecordCount++;
                            Value = "[{\"code\": \"refId\",\"name\": \"Reference ID\",\"options\": [" + string.Join(", ", Values) + "],\"type\": \"select\",\"required\": \"1\"}, {\"code\": \"orderPriority\",\"name\": \"Document Priority\",\"options\": [{\"name\": \"1 - Very Urgent\",\"value\": \"1\"}, {\"name\": \"2 - Urgent\",\"value\": \"2\"},{\"name\": \"3 - Normal\",\"value\": \"3\"},{\"name\": \"4 - Less Important\",\"value\": \"4\" }],\"type\": \"select\", \"required\": \"1\"}]";

                            if (RecordCount > 0)
                            {
                                insertQuery = insertQuery.ReplaceAll(Value, "@values");

                                mysql.Insert(insertQuery);

                                insertQuery = insertQuery.ReplaceAll("@values", Value);

                                logger.message = string.Format("{0} ref records is inserted into " + mysqlconfig.config_database, RecordCount);
                                logger.Broadcast();
                            }
                        });
                    });

                    slog.action_identifier = Constants.Action_APSRefSync;
                    slog.action_details = Constants.Tbl_cms_mobile_module + Constants.Is_Finished + string.Format(" ({0}) records", RecordCount);
                    slog.action_failure = 0;
                    slog.action_failure_message = string.Empty;
                    slog.action_time = DateTime.Now.ToString();

                    DateTime endTime = DateTime.Now;
                    TimeSpan ts = endTime - startTime;
                    Console.WriteLine("Completed in: " + ts.TotalSeconds.ToString("F"));

                    LocalDB.InsertSyncLog(slog);

                    logger.message = "APS ref sync finished in (" + ts.TotalSeconds.ToString("F") + " seconds)";
                    logger.Broadcast();

                });
                thread.Start();

            }
            catch (ThreadAbortException e)
            {
                DpprException ex = new DpprException
                {
                    file_name = "JobAPSRefSync",
                    exception = e.Message,
                    time = DateTime.Now.ToString()
                };
                LocalDB.InsertException(ex);

                Console.WriteLine(Constants.Thread_Exception + e.Message);
            }
        }
    }
}
