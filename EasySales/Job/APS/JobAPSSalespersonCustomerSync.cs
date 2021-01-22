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
    public class JobAPSSalespersonCustomerSync : IJob
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
                    slog.action_identifier = Constants.Action_APSSalespersonCustomerSync;                                  /*check again */
                    slog.action_details = Constants.Tbl_cms_customer_salesperson + Constants.Is_Starting;                   /*check again */
                    slog.action_failure = 0;
                    slog.action_failure_message = string.Empty;
                    slog.action_time = DateTime.Now.ToString();

                    DateTime startTime = DateTime.Now;

                    LocalDB.InsertSyncLog(slog);

                    logger.message = "APS salesperson customer sync is running";
                    logger.Broadcast();

                    //{"mysql":"customer_id", "mssql":"charRef"},{"mysql":"salesperson_id", "mssql":"team"}

                    List<DpprMySQLconfig> mysql_list = LocalDB.GetRemoteDatabaseConfig().Distinct().ToList();
                    List<DpprSQLServerconfig> mssql_list = LocalDB.GetRemoteSQLServerConfig();
                    string customerDbQuery = "SELECT cust_id, UPPER(TRIM(cust_code)) AS cust_code FROM cms_customer";

                    mysql_list.Iterate<DpprMySQLconfig>((mysql_config, index) =>
                    {
                        DpprMySQLconfig mysqlconfig = mysql_list[index];
                        Database mysql = new Database();
                        mysql.Connect(index: index);

                        CheckBackendRule checkDB = new CheckBackendRule(mysql: mysql);
                        dynamic jsonRule = checkDB.CheckTablesExist().GetSettingByTableName("cms_customer_salesperson");

                        Dictionary<string, string> cms_updated_time = mysql.GetUpdatedTime("cms_customer");
                        string cms_updated_at = string.Empty;
                        ArrayList mssql_rule = new ArrayList();

                        if (jsonRule != null)
                        {
                            foreach (var key in jsonRule)
                            {
                                if (key.mssql != null && key.mssql.GetType().ToString() == "Newtonsoft.Json.Linq.JArray")
                                {
                                    string query = "SELECT cust.charRef , cust.varCustNm ,usr.charUserid as varColSalMan @teamsalesman from Sal_CustomerTbl cust inner join Adm_UserTbl usr on cust.intSalManID = usr.intUserID @leftjoin";

                                    //@teamsalesman = ", isnull(ts1.charUserID, '0') as varTeam1Salesman , isnull(ts2.charUserID, '0') as varTeam2Salesman , isnull(ts3.charUserID, '0') as varTeam3Salesman , isnull(ts4.charUserID, '0') as varTeam4Salesman "
                                    //@leftjoin = " left outer join Adm_UserTbl ts1 on cust.intT1Salesman = ts1.intUserID left outer join Adm_UserTbl ts2 on cust.intT2Salesman = ts2.intUserID left outer join Adm_UserTbl ts3 on cust.intT3Salesman = ts3.intUserID left outer join Adm_UserTbl ts4 on cust.intT4Salesman = ts4.intUserID "
                                    if (cms_updated_time.Count > 0)
                                    {
                                        cms_updated_at = cms_updated_time["updated_at"].ToString().MSSQLdate();
                                        customerDbQuery = string.Format("SELECT cust_id, UPPER(TRIM(cust_code)) AS cust_code FROM cms_customer WHERE updated_at >= '{0}'", cms_updated_at);
                                    }

                                    var mssql_rules = key.mssql;
                                    foreach (var db in mssql_rules)
                                    {
                                        string ts_query = string.Empty;
                                        string ts_query_plus = string.Empty;
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

                                        if (db.salesman.GetType().ToString() == "Newtonsoft.Json.Linq.JArray" && db.salesman.Count > 0)
                                        {
                                            //, isnull(ts1.charUserID, '0') as varTeam1Salesman 
                                            string tmp_join = " , isnull(ts@tsid.charUserID, '0') as varTeam@tsidSalesman";
                                            string tmp_join_plus = " left outer join Adm_UserTbl ts@tsid on cust.intT@tsidSalesman = ts@tsid.intUserID ";

                                            //kl---> ts [1,2,3,4]
                                            //hq ---> ts [1,2,3,4]
                                            //zone---> ts [1,2,3,4]
                                            //ulsan---> ts []

                                            foreach (var ts in db.salesman)
                                            {
                                                string tsid = "" + ts;

                                                string tmp = tmp_join.ReplaceAll(tsid, "@tsid");

                                                ts_query += tmp;
                                            }

                                            foreach (var ts in db.salesman)
                                            {
                                                string tsid = "" + ts;

                                                string tmp2 = tmp_join_plus.ReplaceAll(tsid, "@tsid");

                                                ts_query_plus += tmp2;
                                            }

                                        }

                                        query = query.ReplaceAll(ts_query, "@teamsalesman");
                                        query = query.ReplaceAll(ts_query_plus, "@leftjoin");

                                        Console.WriteLine(db.loopcount);

                                        APSRule aps_rule = new APSRule()
                                        {
                                            DBname = db.name,
                                            loopCount = db.loopcount,
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
                            throw new Exception("APS Customer-Salesperson sync requires backend rules");
                        }

                        ArrayList salespersonFromDb = mysql.Select("SELECT login_id, UPPER(TRIM(staff_code)) AS staff_code FROM cms_login");
                        Dictionary<string, string> salespersonList = new Dictionary<string, string>();

                        for (int i = 0; i < salespersonFromDb.Count; i++)
                        {
                            Dictionary<string, string> each = (Dictionary<string, string>)salespersonFromDb[i];
                            salespersonList.Add(each["staff_code"], each["login_id"]);
                        }

                        ArrayList customerFromDb = mysql.Select(customerDbQuery);
                        Dictionary<string, string> customerList = new Dictionary<string, string>();

                        for (int i = 0; i < customerFromDb.Count; i++)
                        {
                            Dictionary<string, string> each = (Dictionary<string, string>)customerFromDb[i];
                            customerList.Add(each["cust_code"], each["cust_id"]);
                        }

                        Console.WriteLine("customerList.Count: "+ customerList.Count);

                        string getCustAgent = "SELECT salesperson_customer_id, salesperson_id, customer_id FROM cms_customer_salesperson WHERE active_status = 1 ";
                        getCustAgent += cms_updated_time.Count > 0 ? " AND customer_id IN (SELECT cust_id FROM cms_customer WHERE updated_at >= '"+ cms_updated_at + "')" : "";
                        Console.WriteLine("getCustAgentQuery: " + getCustAgent);
                        ArrayList custAgentFromDb = mysql.Select(getCustAgent);
                        Dictionary<string, string> custAgentList = new Dictionary<string, string>();

                        for (int i = 0; i < custAgentFromDb.Count; i++)
                        {
                            Dictionary<string, string> each = (Dictionary<string, string>)custAgentFromDb[i];
                            string custAgentId = each["customer_id"] + "(" + each["salesperson_id"] + ")";
                            Console.WriteLine(custAgentId);
                            custAgentList.Add(each["salesperson_customer_id"], custAgentId);
                        }

                        Console.WriteLine("custAgentList.Count: " + custAgentList.Count);

                        mssql_rule.Iterate<APSRule>((database, idx) =>
                        {
                            SQLServer mssql = new SQLServer();
                            mssql.Connect(dbname: database.DBname);

                            logger.Broadcast(database.Query);
                            Console.WriteLine(database.Query);

                            ArrayList queryResult = mssql.Select(database.Query);

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
                                for(int ixx = 0; ixx < database.loopCount; ixx++)
                                {
                                    Console.WriteLine(ixx +"<"+ database.loopCount);
                                    string row = string.Empty;
                                    database.Include.Iterate<Dictionary<string, string>>((include, inIdx) =>
                                    {
                                        string nullfield = include["nullfield"];
                                        string find_mssql_field = include["mssql"];
                                        string corr_mysql_field = include["mysql"];
                                        bool addedToRow = false;
                                        string team = string.Empty;

                                        if (ixx == 0)
                                        {
                                            map.Iterate<KeyValuePair<string, string>>((mssql_fields, mssqIndx) =>           
                                            {
                                                if (mssql_fields.Key == "varColSalMan")
                                                {
                                                    team = mssql_fields.Value;
                                                    addedToRow = true;
                                                }
                                            });
                                        }
                                    
                                        if (ixx == 1)
                                        {
                                            map.Iterate<KeyValuePair<string, string>>((mssql_fields, mssqIndx) =>           
                                            {
                                                if (mssql_fields.Key == "varTeam1Salesman")
                                                {
                                                    team = mssql_fields.Value;
                                                    addedToRow = true;
                                                }
                                            });
                                        }
                                        
                                        if (ixx == 2)
                                        {
                                            map.Iterate<KeyValuePair<string, string>>((mssql_fields, mssqIndx) =>           
                                            {
                                                if (mssql_fields.Key == "varTeam2Salesman")
                                                {
                                                    team = mssql_fields.Value;
                                                    Console.WriteLine("varTeam2Salesman: " + team);
                                                    addedToRow = true;
                                                }
                                            });
                                        }
                                        
                                        if (ixx == 3)
                                        {
                                            map.Iterate<KeyValuePair<string, string>>((mssql_fields, mssqIndx) =>           
                                            {
                                                if (mssql_fields.Key == "varTeam3Salesman")
                                                {
                                                    team = mssql_fields.Value;
                                                    Console.WriteLine("varTeam3Salesman: " + team);
                                                    addedToRow = true;
                                                }
                                            });
                                        }
                        
                                        if (ixx == 4)
                                        {
                                            map.Iterate<KeyValuePair<string, string>>((mssql_fields, mssqIndx) =>          
                                            {
                                                if (mssql_fields.Key == "varTeam4Salesman")
                                                {
                                                    team = mssql_fields.Value;
                                                    Console.WriteLine("varTeam4Salesman: " + team);
                                                    addedToRow = true;
                                                }
                                            });
                                        }

                                        if (find_mssql_field == "charRef")
                                        {
                                            string _custId = string.Empty;

                                            map.Iterate<KeyValuePair<string, string>>((mssql_fields, mssqIndx) =>          
                                            {
                                                if (mssql_fields.Key == "charRef")
                                                {
                                                    _custId = mssql_fields.Value;
                                                    Console.WriteLine("cust Code:" + _custId);
                                                    if (string.IsNullOrEmpty(_custId) || !customerList.TryGetValue(_custId, out _custId))
                                                    {
                                                        _custId = "0";
                                                    }
                                                }
                                            });

                                            string _salespersonId = string.Empty;

                                            if (string.IsNullOrEmpty(team) || !salespersonList.TryGetValue(team, out _salespersonId))
                                            {
                                                _salespersonId = "0";
                                            }

                                            Console.WriteLine("custId: " + _custId + " salespersonId: " + _salespersonId);
                                            if (_custId != "0" && _salespersonId != "0")
                                            {
                                                string uniqueKey = _custId + "(" + _salespersonId + ")";
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
                                                
                                                //row = "('" + _custId + "','" + _salespersonId + "')";
                                                row = "('" + _custId + "','" + _salespersonId + "'";
                                                Console.WriteLine(row);
                                            }
                                            addedToRow = true;
                                        }

                                        if (find_mssql_field == "DateTime.Now")
                                        {
                                            string updated_at = string.Empty;
                                            DateTime date = DateTime.Now;
                                            updated_at = date.ToString("s"); //2020-09-08 15:30:36

                                            Database.Sanitize(ref updated_at);

                                            if(row != string.Empty)
                                            {
                                                //row += inIdx == 0 ? "('" + updated_at + "" : ",'" + updated_at + "')";
                                                row += inIdx == 0 ? "('" + updated_at + "" : ",'" + updated_at;
                                            }
                                            addedToRow = true;
                                        }

                                        if (corr_mysql_field == "active_status")
                                        {
                                            if (row != string.Empty)
                                            {
                                                row += inIdx == 0 ? "('1" : "','1";
                                            }
                                            addedToRow = true;
                                        }

                                    });

                                    if(row != string.Empty)
                                    {
                                        row += "')";
                                        valueString.Add(row);
                                        Console.WriteLine(row);
                                    }
                                }

                                if (valueString.Count > 0 && valueString.Count % 2000 == 0)
                                {
                                    string values = valueString.Join(",");

                                    insertQuery = insertQuery.ReplaceAll(values, "@values");

                                    mysql.Insert(insertQuery);
                                    mysql.Message("JobAPSSalespersonCustomerSync: [" + mysqlconfig.config_database + "]--->" + insertQuery);

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
                                mysql.Message("JobAPSSalespersonCustomerSync: [" + mysqlconfig.config_database + "]--->" + insertQuery);

                                insertQuery = insertQuery.ReplaceAll("@values", values);

                                logger.message = string.Format("{0} customer-salesperson records is inserted into " + mysqlconfig.config_database, valueString.Count);
                                logger.Broadcast();
                                valueString.Clear();
                            }

                            if(custAgentList.Count > 0)
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
                                mysql.Message("Cust-Agent Deactivate Query: " + inactive);
                                mysql.Insert(inactive);

                                logger.Broadcast(custAgentList.Count + " cust-agent records deactivated");

                                custAgentList.Clear();
                            }

                            if (cms_updated_time.Count > 0)
                            {
                                mysql.Insert("UPDATE cms_update_time SET updated_at = NOW() WHERE table_name = 'cms_customer_salesperson'");
                            }
                            else
                            {
                                mysql.Insert("INSERT INTO cms_update_time(table_name, updated_at) VALUES('cms_customer_salesperson', NOW())");
                            }
                        });
                    });

                    slog.action_identifier = Constants.Action_APSSalespersonCustomerSync;
                    slog.action_details = Constants.Tbl_cms_customer_salesperson + Constants.Is_Finished + string.Format(" ({0}) records", RecordCount);
                    slog.action_failure = 0;
                    slog.action_failure_message = string.Empty;
                    slog.action_time = DateTime.Now.ToString();

                    DateTime endTime = DateTime.Now;
                    TimeSpan timespan = endTime - startTime;
                    Console.WriteLine("Completed in: " + timespan.TotalSeconds.ToString("F"));

                    LocalDB.InsertSyncLog(slog);

                    logger.message = "APS salesperson customer sync finished in (" + timespan.TotalSeconds.ToString("F") + " seconds)";
                    logger.Broadcast();

                });

                thread.Start();
                //thread.Join();

            }
            catch (ThreadAbortException e)
            {
                DpprException ex = new DpprException
                {
                    file_name = "JobAPSSalespersonCustomerSync",
                    exception = e.Message,
                    time = DateTime.Now.ToString()
                };
                LocalDB.InsertException(ex);

                Console.WriteLine(Constants.Thread_Exception + e.Message);
            }
        }
    }
}
