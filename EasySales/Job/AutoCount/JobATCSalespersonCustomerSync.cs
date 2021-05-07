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
    public class JobATCSalespersonCustomerSync : IJob
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
                    slog.action_identifier = Constants.Action_ATCSalespersonCustomerSync;                                  /*check again */
                    slog.action_details = Constants.Tbl_cms_customer_salesperson + Constants.Is_Starting;                   /*check again */
                    slog.action_failure = 0;
                    slog.action_failure_message = string.Empty;
                    slog.action_time = DateTime.Now.ToString();

                    DateTime startTime = DateTime.Now;

                    LocalDB.DBCleanup();
                    LocalDB.InsertSyncLog(slog);

                    logger.message = "ATC customer-agent sync is running";
                    logger.Broadcast();

                    List<DpprMySQLconfig> mysql_list = LocalDB.GetRemoteDatabaseConfig().Distinct().ToList();
                    List<DpprSQLServerconfig> mssql_list = LocalDB.GetRemoteSQLServerConfig();

                    mysql_list.Iterate<DpprMySQLconfig>((mysql_config, index) =>
                    {
                        DpprMySQLconfig mysqlconfig = mysql_list[index];
                        Database mysql = new Database();
                        mysql.Connect(index: index);

                        CheckBackendRule checkDB = new CheckBackendRule(mysql: mysql);
                        dynamic jsonRule = checkDB.CheckTablesExist().GetSettingByTableName("cms_customer_salesperson_atc");

                        //Dictionary<string, string> cms_updated_time = mysql.GetUpdatedTime("cms_customer_salesperson");

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
                                        string query = "SELECT UPPER(LTRIM(RTRIM(AccNo)))  AS AccNo, UPPER(LTRIM(RTRIM(SalesAgent))) AS SalesAgent FROM dbo.Debtor";

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
                            throw new Exception("ATC Customer-Agent sync requires backend rules");
                        }

                        ArrayList salespersonFromDb = mysql.Select("SELECT login_id, UPPER(TRIM(staff_code)) AS staff_code FROM cms_login");
                        Dictionary<string, string> salespersonList = new Dictionary<string, string>();

                        for (int i = 0; i < salespersonFromDb.Count; i++)
                        {
                            Dictionary<string, string> each = (Dictionary<string, string>)salespersonFromDb[i];
                            if (!salespersonList.ContainsKey(each["staff_code"]))
                            {
                                salespersonList.Add(each["staff_code"], each["login_id"]);
                            }
                        }
                        salespersonFromDb.Clear();

                        ArrayList customerFromDb = mysql.Select("SELECT cust_id, UPPER(TRIM(cust_code)) AS cust_code FROM cms_customer");
                        Dictionary<string, string> customerList = new Dictionary<string, string>();

                        for (int i = 0; i < customerFromDb.Count; i++)
                        {
                            Dictionary<string, string> each = (Dictionary<string, string>)customerFromDb[i];
                            if(!customerList.ContainsKey(each["cust_code"]))
                            {
                                customerList.Add(each["cust_code"], each["cust_id"]);
                            }
                        }
                        customerFromDb.Clear();

                        string getCustAgent = "SELECT salesperson_customer_id, salesperson_id, customer_id FROM cms_customer_salesperson WHERE active_status = 1 ";
                        Console.WriteLine("getCustAgentQuery: " + getCustAgent);
                        ArrayList custAgentFromDb = mysql.Select(getCustAgent);
                        Dictionary<string, string> custAgentList = new Dictionary<string, string>();

                        for (int i = 0; i < custAgentFromDb.Count; i++)
                        {
                            Dictionary<string, string> each = (Dictionary<string, string>)custAgentFromDb[i];
                            string custAgentId = each["customer_id"] + "(" + each["salesperson_id"] + ")";
                            if(!custAgentList.ContainsKey(each["salesperson_customer_id"]))
                            {
                                custAgentList.Add(each["salesperson_customer_id"], custAgentId);
                            }
                        }

                        Console.WriteLine("custAgentList.Count: " + custAgentList.Count);

                        mssql_rule.Iterate<ATCRule>((database, idx) =>
                        {
                            SQLServer mssql = new SQLServer();
                            mssql.Connect(dbname: database.DBname);

                            ArrayList queryResult = mssql.Select(database.Query);
                            if (queryResult.Count > 0)
                            {
                                logger.Broadcast("Cust-Agent to be inserted: " + queryResult.Count);
                            }
                            mssql.Message("Cust-Agent MSSQL Query ===> " + database.Query);

                            string mysql_insert = string.Empty;
                            string mssql_insert = string.Empty;

                            string insertQuery = "INSERT INTO cms_customer_salesperson (@columns) VALUES @values ON DUPLICATE KEY UPDATE @update_columns";
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
                                string salespersonId = string.Empty;
                                string salespersonCode = string.Empty;
                                string custId = string.Empty;
                                string custCode = string.Empty;

                                string row = string.Empty;
                                database.Include.Iterate<Dictionary<string, string>>((include, inIdx) =>
                                {
                                    string nullfield = include["nullfield"];
                                    string find_mssql_field = include["mssql"];
                                    string corr_mysql_field = include["mysql"];

                                    if (find_mssql_field == "AccNo")
                                    {
                                        map.Iterate<KeyValuePair<string, string>>((mssql_fields, mssqIndx) =>
                                        {
                                            if (mssql_fields.Key == "AccNo")
                                            {
                                                custCode = mssql_fields.Value;
                                                //Console.WriteLine("cust Code:" + custCode);
                                                if (string.IsNullOrEmpty(custCode) || !customerList.TryGetValue(custCode, out custId))
                                                {
                                                    custId = "0";
                                                }
                                            }
                                        });
                                    }

                                    if (find_mssql_field == "SalesAgent")
                                    {
                                        map.Iterate<KeyValuePair<string, string>>((mssql_fields, mssqIndx) =>
                                        {
                                            if (mssql_fields.Key == "SalesAgent")
                                            {
                                                salespersonCode = mssql_fields.Value;
                                                //Console.WriteLine("salespersonCode:" + salespersonCode);
                                                if (string.IsNullOrEmpty(salespersonCode) || !salespersonList.TryGetValue(salespersonCode, out salespersonId))
                                                {
                                                    salespersonId = "0";
                                                }
                                            }
                                        });
                                    }
                                });

                                string activeStatus = "1";
                                //Console.WriteLine("custId: " + custId + " salespersonId: " + salespersonId);
                                if (custId != "0" && salespersonId != "0")
                                {
                                    string uniqueKey = custId + "(" + salespersonId + ")";
                                    Console.WriteLine(uniqueKey);

                                    if (custAgentList.ContainsValue(uniqueKey))
                                    {
                                        var key = custAgentList.Where(pair => pair.Value == uniqueKey)
                                                    .Select(pair => pair.Key)
                                                    .FirstOrDefault();
                                        if (key != null)
                                        {
                                            custAgentList.Remove(key);
                                        }
                                    }
                                    row = "('" + custId + "','" + salespersonId + "', '" +activeStatus+ "')";
                                    //Console.WriteLine(row);
                                }

                                if (row != string.Empty)
                                {
                                    valueString.Add(row);
                                    //Console.WriteLine(row);
                                }

                                if (valueString.Count > 0 && valueString.Count % 2000 == 0)
                                {
                                    string values = valueString.Join(",");

                                    insertQuery = insertQuery.ReplaceAll(values, "@values");

                                    mysql.Insert(insertQuery);
                                    mysql.Message("JobATCSalespersonCustomerSync: " + insertQuery);

                                    insertQuery = insertQuery.ReplaceAll("@values", values);

                                    logger.message = string.Format("{0} customer-salesperson records is inserted into " + mysqlconfig.config_database, valueString.Count);
                                    logger.Broadcast();
                                    valueString.Clear();
                                }
                            });
                            

                            if (valueString.Count > 0)
                            {
                                string values = valueString.Join(",");

                                insertQuery = insertQuery.ReplaceAll(values, "@values");

                                mysql.Insert(insertQuery);
                                mysql.Message("JobATCSalespersonCustomerSync: " + insertQuery);

                                insertQuery = insertQuery.ReplaceAll("@values", values);

                                logger.message = string.Format("{0} customer-salesperson records is inserted into " + mysqlconfig.config_database, valueString.Count);
                                logger.Broadcast();
                                valueString.Clear();
                            }

                            if (custAgentList.Count > 0)
                            {
                                logger.Broadcast("Total cust-agent records to be deactivated: " + custAgentList.Count);

                                HashSet<string> deactivateId = new HashSet<string>();
                                for (int i = 0; i < custAgentList.Count; i++)
                                {
                                    string _id = custAgentList.ElementAt(i).Key;
                                    deactivateId.Add(_id);
                                }

                                string ToBeDeactivate = "'" + string.Join("','", deactivateId) + "'";
                                Console.WriteLine(ToBeDeactivate);

                                string inactive = "UPDATE cms_customer_salesperson SET active_status = 0 WHERE salesperson_customer_id IN (" + ToBeDeactivate + ")";
                                mysql.Message("Cust-Agent Deactivate Query ======> " + inactive);
                                mysql.Insert(inactive);

                                logger.Broadcast(custAgentList.Count + " cust-agent records deactivated");

                                custAgentList.Clear();
                            }
                            
                            mysql.Insert("INSERT INTO cms_update_time(table_name, updated_at) VALUES('cms_customer_salesperson', NOW()) ON DUPLICATE KEY UPDATE updated_at = VALUES(updated_at)");

                            salespersonList.Clear();
                            customerList.Clear();
                            custAgentList.Clear();

                            mysqlFieldList.Clear();
                            queryResult.Clear();
                        });
                        mssql_rule.Clear();
                        //cms_updated_time.Clear();
                    });

                    mysql_list.Clear();
                    mssql_list.Clear();

                    slog.action_identifier = Constants.Action_ATCSalespersonCustomerSync;
                    slog.action_details = Constants.Tbl_cms_customer_salesperson + Constants.Is_Finished + string.Format(" ({0}) records", RecordCount);
                    slog.action_failure = 0;
                    slog.action_failure_message = string.Empty;
                    slog.action_time = DateTime.Now.ToString();

                    DateTime endTime = DateTime.Now;
                    TimeSpan timespan = endTime - startTime;
                    //Console.WriteLine("Completed in: " + timespan.TotalSeconds.ToString("F"));

                    LocalDB.InsertSyncLog(slog);

                    logger.message = "ATC salesperson customer sync finished in (" + timespan.TotalSeconds.ToString("F") + " seconds)";
                    logger.Broadcast();

                });

                thread.Start();
                //thread.Join();

            }
            catch (ThreadAbortException e)
            {
                DpprException ex = new DpprException
                {
                    file_name = "JobATCSalespersonCustomerSync",
                    exception = e.Message,
                    time = DateTime.Now.ToString()
                };
                LocalDB.InsertException(ex);

                Console.WriteLine(Constants.Thread_Exception + e.Message);
            }
        }
    }
}
