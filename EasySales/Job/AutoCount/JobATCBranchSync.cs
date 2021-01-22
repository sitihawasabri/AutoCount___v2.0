using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
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

namespace EasySales.Job
{
    [DisallowConcurrentExecution]
    public class JobATCBranchSync : IJob
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
                    slog.action_identifier = Constants.Action_ATCBranchSync;                                  /*check again */
                    slog.action_details = Constants.Tbl_cms_customer_branch + Constants.Is_Starting;                   /*check again */
                    slog.action_failure = 0;
                    slog.action_failure_message = string.Empty;
                    slog.action_time = DateTime.Now.ToString();

                    DateTime startTime = DateTime.Now;

                    LocalDB.InsertSyncLog(slog);

                    logger.message = "ATC branch sync is running";
                    logger.Broadcast();

                    List<DpprMySQLconfig> mysql_list = LocalDB.GetRemoteDatabaseConfig().Distinct().ToList();
                    List<DpprSQLServerconfig> mssql_list = LocalDB.GetRemoteSQLServerConfig();

                    mysql_list.Iterate<DpprMySQLconfig>((mysql_config, index) =>
                    {
                        DpprMySQLconfig mysqlconfig = mysql_list[index];
                        Database mysql = new Database();
                        mysql.Connect(index: index);
                       
                        CheckBackendRule checkDB = new CheckBackendRule(mysql: mysql);
                        dynamic jsonRule = checkDB.CheckTablesExist().GetSettingByTableName("cms_customer_branch_atc");

                        Dictionary<string, string> cms_updated_time = mysql.GetUpdatedTime("cms_customer_branch");

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
                                        string query = "SELECT UPPER(LTRIM(RTRIM([Branch].[AccNo]))) AS [AccNo],UPPER(LTRIM(RTRIM(CASE WHEN [Branch].[SalesAgent] IS NOT NULL THEN [Branch].[SalesAgent] ELSE [Debtor].[SalesAgent] END))) AS [SalesAgent], [Debtor].[CompanyName], [Branch].[BranchCode], [Branch].[BranchName], [Branch].[Phone1], [Branch].[Fax1], [Branch].[IsActive], [Branch].[Contact], [Branch].[AreaCode], [Branch].[Address1], [Branch].[Address2] , [Branch].[Address3], [Branch].[Address4], [Branch].[PostCode]FROM [dbo].[Branch] JOIN [dbo].[Debtor] ON [Debtor].[AccNo] = [Branch].[AccNo]";

                                        string ts_join = string.Empty;
                                        string ts_join_query = string.Empty;
                                        string isnull_query = string.Empty;

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
                            throw new Exception("ATC Branch sync requires backend rules");
                        }

                        ArrayList salespersonFromDb = mysql.Select("SELECT login_id, UPPER(TRIM(staff_code)) AS staff_code FROM cms_login");
                        Dictionary<string, string> salespersonList = new Dictionary<string, string>();

                        for (int i = 0; i < salespersonFromDb.Count; i++)
                        {
                            Dictionary<string, string> each = (Dictionary<string, string>)salespersonFromDb[i];
                            salespersonList.Add(each["staff_code"], each["login_id"]);
                        }
                        salespersonFromDb.Clear();

                        ArrayList customerFromDb = mysql.Select("SELECT cust_id, UPPER(TRIM(cust_code)) AS cust_code FROM cms_customer");
                        Dictionary<string, string> customerList = new Dictionary<string, string>();

                        for (int i = 0; i < customerFromDb.Count; i++)
                        {
                            Dictionary<string, string> each = (Dictionary<string, string>)customerFromDb[i];
                            customerList.Add(each["cust_code"], each["cust_id"]);
                        }
                        customerFromDb.Clear();

                        //Console.WriteLine("customerList.Count: " + customerList.Count);

                        mssql_rule.Iterate<ATCRule>((database, idx) =>
                        {
                            SQLServer mssql = new SQLServer();
                            mssql.Connect(dbname: database.DBname);

                            ArrayList queryResult = mssql.Select(database.Query);
                            //Console.WriteLine(database.Query);

                            string mysql_insert = string.Empty;
                            string mssql_insert = string.Empty;

                            string insertQuery = "INSERT INTO cms_customer_branch (@columns) VALUES @values ON DUPLICATE KEY UPDATE @update_columns";
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
                            //Console.WriteLine(insertQuery);

                            ArrayList mysqlFieldList = new ArrayList();
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

                                    if (find_mssql_field == "cust_id")
                                    {
                                        string cust_id = string.Empty;

                                        map.Iterate<KeyValuePair<string, string>>((mssql_fields, mssqIndx) =>
                                        {
                                            if (mssql_fields.Key == "AccNo")
                                            {
                                                string accNo = mssql_fields.Value;

                                                if (string.IsNullOrEmpty(accNo) || !customerList.TryGetValue(accNo, out cust_id))
                                                {
                                                    cust_id = "0";
                                                }
                                            }
                                        });

                                        row += inIdx == 0 ? "('" + cust_id + "" : "','" + cust_id;

                                        NoMssqlField = false;
                                        addedToRow = true;
                                    }

                                    if (find_mssql_field == "agent_id")
                                    {
                                        string agent_id = "0";

                                        map.Iterate<KeyValuePair<string, string>>((mssql_fields, mssqIndx) =>
                                        {
                                            if (mssql_fields.Key == "SalesAgent")
                                            {
                                                string SalesAgent = mssql_fields.Value;

                                                if (string.IsNullOrEmpty(SalesAgent) || !salespersonList.TryGetValue(SalesAgent, out agent_id))
                                                {
                                                    agent_id = "0";
                                                }
                                            }
                                        });

                                        row += inIdx == 0 ? "('" + agent_id + "" : "','" + agent_id;

                                        NoMssqlField = false;
                                        addedToRow = true;
                                    }

                                    map.Iterate<KeyValuePair<string, string>>((mssql_fields, mssqIndx) =>
                                    {
                                        if (find_mssql_field.EcodeContains(mssql_fields.Key))
                                        {
                                            string tmp = LogicParser.Parse(mssql_fields.Key, find_mssql_field, map, nullfield)[mssql_fields.Key];

                                            //do looping for mysql field
                                            for (int isql = 0; isql < mysqlFieldList.Count; isql++)
                                            {
                                                string eachField = mysqlFieldList[isql].ToString();

                                                /* all field except find_mssql_field which has string/join dont insert here */
                                                if (corr_mysql_field == eachField && LogicParser.IsCodeStr(find_mssql_field) == false) 
                                                {
                                                    if(!addedToRow)
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

                                valueString.Add(row);

                                RecordCount++;

                                if (valueString.Count % 2000 == 0)
                                {
                                    string values = valueString.Join(",");

                                    insertQuery = insertQuery.ReplaceAll(values, "@values");

                                    mysql.Insert(insertQuery);
                                    mysql.Message(insertQuery);

                                    insertQuery = insertQuery.ReplaceAll("@values", values);
                                    valueString.Clear();

                                    logger.message = string.Format("{0} branch records is inserted into " + mysqlconfig.config_database, RecordCount);
                                    logger.Broadcast();
                                }

                            });

                            if (valueString.Count > 0)
                            {
                                string values = valueString.Join(",");

                                insertQuery = insertQuery.ReplaceAll(values, "@values");

                                mysql.Insert(insertQuery);
                                mysql.Message(insertQuery);

                                insertQuery = insertQuery.ReplaceAll("@values", values);
                                valueString.Clear();

                                logger.message = string.Format("{0} branch records is inserted into " + mysqlconfig.config_database, RecordCount);
                                logger.Broadcast();
                            }
                            RecordCount = 0; /* reset count for the next db */
                            queryResult.Clear();
                            mysqlFieldList.Clear();
                        });
                        mssql_rule.Clear();

                        if (cms_updated_time.Count > 0)
                        {
                            mysql.Insert("UPDATE cms_update_time SET updated_at = NOW() WHERE table_name = 'cms_customer_branch'");
                        }
                        else
                        {
                            mysql.Insert("INSERT INTO cms_update_time(table_name, updated_at) VALUES('cms_customer_branch', NOW())");
                        }
                        cms_updated_time.Clear();
                    });

                    mysql_list.Clear();
                    mssql_list.Clear();

                    slog.action_identifier = Constants.Action_ATCBranchSync;
                    slog.action_details = Constants.Tbl_cms_customer_branch + Constants.Is_Finished + string.Format(" ({0}) records", RecordCount);
                    slog.action_failure = 0;
                    slog.action_failure_message = string.Empty;
                    slog.action_time = DateTime.Now.ToString();


                    DateTime endTime = DateTime.Now;
                    TimeSpan timespan = endTime - startTime;
                    //Console.WriteLine("Completed in: " + timespan.TotalSeconds.ToString("F"));

                    LocalDB.InsertSyncLog(slog);

                    logger.message = "ATC branch sync finished in (" + timespan.TotalSeconds.ToString("F") + " seconds)";
                    logger.Broadcast();

                });

                thread.Start();
                //thread.Join();

            }
            catch (ThreadAbortException e)
            {
                DpprException ex = new DpprException
                {
                    file_name = "JobATCBranchSync",
                    exception = e.Message,
                    time = DateTime.Now.ToString()
                };
                LocalDB.InsertException(ex);

                Console.WriteLine(Constants.Thread_Exception + e.Message);
            }
        }
    }
}
