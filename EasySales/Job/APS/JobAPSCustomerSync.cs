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
    public class JobAPSCustomerSync : IJob
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
                    slog.action_identifier = Constants.Action_APSCustomerSync;                                  /*check again */
                    slog.action_details = Constants.Tbl_cms_customer + Constants.Is_Starting;                   /*check again */
                    slog.action_failure = 0;
                    slog.action_failure_message = string.Empty;
                    slog.action_time = DateTime.Now.ToString();

                    DateTime startTime = DateTime.Now;

                    LocalDB.InsertSyncLog(slog);

                    logger.message = "APS customer sync is running";
                    logger.Broadcast();

                    List<DpprMySQLconfig> mysql_list = LocalDB.GetRemoteDatabaseConfig().Distinct().ToList();
                    List<DpprSQLServerconfig> mssql_list = LocalDB.GetRemoteSQLServerConfig();
                   

                    mysql_list.Iterate<DpprMySQLconfig>((mysql_config, index) =>
                    {
                        DpprMySQLconfig mysqlconfig = mysql_list[index];
                        Database mysql = new Database();
                        mysql.Connect(index: index);
                       
                        CheckBackendRule checkDB = new CheckBackendRule(mysql: mysql);
                        dynamic jsonRule = checkDB.CheckTablesExist().GetSettingByTableName("cms_customer");

                        Dictionary<string, string> cms_updated_time = mysql.GetUpdatedTime1MonthInterval("cms_customer");

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
                                        //string query = "SELECT cust.charRef , cust.varCustNm, varCustShortNm, cust.dtCreatedDate, cust.varOwner, cust.varOfficeEmail, cust.varOfficePhone, cust.varOfficeFax, IIF(cust.varCustCNNm = '' AND cust.charStatus = 'S', 'Suspended', IIF(cust.charStatus = 'A', 'Active', cust.varCustCNNm)) as varStatus , cust.charAddr, cust.charAddr1, cust.charAddr2, cust.varCity, cust.charPostCode, cust.charShipAddr1, cust.charShipAddr2, cust.charShipAddr3, cust.varShipCity, cust.charShipPostCode, cust.intPayTermID, cust.intAdtnPayTermID, usr.charUserID as varCollectChqSalMan, isnull(ts1.charUserID, 'N/A') as varTeam1Salesman, isnull(ts2.charUserID, 'N/A') as varTeam2Salesman, isnull(ts3.charUserID, 'N/A') as varTeam3Salesman , isnull(ts4.charUserID, 'N/A') as varTeam4Salesman, @pricegroup as [PriceGroup], grp.varCustGrpNm, @currentbalance as currentBalance from Sal_CustomerTbl cust inner join Adm_UserTbl usr on cust.intSalManID = usr.intUserID @leftjoinQuery left outer join(@selectquery";
                                        string query = "SELECT cust.charRef , cust.varCustNm, varCustShortNm, cust.dtCreatedDate, cust.varOwner, cust.varOfficeEmail, cust.varOfficePhone, cust.varOfficeFax, IIF(cust.varCustCNNm = '' AND cust.charStatus = 'S', 'Suspended', IIF(cust.charStatus = 'A', 'Active', cust.varCustCNNm)) as varStatus , cust.charAddr, cust.charAddr1, cust.charAddr2, cust.varCity, cust.charPostCode, cust.charShipAddr1, cust.charShipAddr2, cust.charShipAddr3, cust.varShipCity, cust.charShipPostCode, cust.intPayTermID, cust.intAdtnPayTermID, usr.charUserID as varCollectChqSalMan, @isnull  @pricegroup as [PriceGroup], grp.varCustGrpNm, @currentbalance as currentBalance from Sal_CustomerTbl cust inner join Adm_UserTbl usr on cust.intSalManID = usr.intUserID @leftjoinQuery left outer join(@selectquery";

                                        string where_clause = string.Empty;

                                        if(cms_updated_time.Count > 0)
                                        {
                                            string updated_at = cms_updated_time["updated_at"].ToString().MSSQLdate();
                                            where_clause = string.Format(" AND cust.dtModifyDate >= '{0}'", updated_at);
                                            query += where_clause;
                                        }

                                        string ts_join = string.Empty;
                                        string ts_join_query = string.Empty;
                                        string isnull_query = string.Empty;

                                        ArrayList include = new ArrayList();
                                        ArrayList exclude = new ArrayList();

                                        if(db.priceGroup != null)
                                        {
                                            string priceGroup = db.priceGroup;
                                            query = query.ReplaceAll(priceGroup, "@pricegroup");
                                        }
                                        
                                        if(db.currentBalance != null)
                                        {
                                            string currentBalance = db.currentBalance;
                                            query = query.ReplaceAll(currentBalance, "@currentbalance");
                                        }
                                        
                                        if(db.selectQuery != null)
                                        {
                                            string selectQuery = db.selectQuery;
                                            query = query.ReplaceAll(selectQuery, "@selectquery");
                                        }

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
                                            //isnull(ts1.charUserID, 'N/A') as varTeam1Salesman, isnull(ts2.charUserID, 'N/A') as varTeam2Salesman, isnull(ts3.charUserID, 'N/A') as varTeam3Salesman , isnull(ts4.charUserID, 'N/A') as varTeam4Salesman
                                            string isnull = " isnull(ts@tsid.charUserID, 'N/A') as varTeam@tsidSalesman, ";
                                            string tmp_join = " left outer join Adm_UserTbl ts@tsid on cust.intT@tsidSalesman = ts@tsid.intUserID";
                                            //string tmp_join_plus = " left outer join Sal_CustGroupTbl grp on cust.intCustGrpId = grp.intCustGrpId left outer join vwSal_CustomerPriceGroupForLBAUTOTbl pg on cust.intPriceGroupIDForLBAUTO = pg.intPriceGroupForLBID ";
                                            string tmp_join_plus = " left outer join Sal_CustGroupTbl grp on cust.intCustGrpId = grp.intCustGrpId  ";
                                            string tmp_join_add = " left outer join vwSal_CustomerPriceGroupForLBAUTOTbl pg on cust.intPriceGroupIDForLBAUTO = pg.intPriceGroupForLBID ";
                                            string tsid = string.Empty;
                                            //kl---> ts [1,2,3,4]
                                            //hq ---> ts [1,2,3,4]
                                            //zone---> ts [1,2,3,4]
                                            //ulsan---> ts [1]

                                            foreach (var ts in db.salesman)
                                            {
                                                tsid = "" + ts;

                                                string tmp = tmp_join.ReplaceAll(tsid, "@tsid");
                                                string tmpisnull = isnull.ReplaceAll(tsid, "@tsid");

                                                isnull_query += tmpisnull;
                                                ts_join_query += tmp;
                                            }

                                            if (tsid == "1")
                                            {
                                                ts_join_query += tmp_join_plus;
                                                ts_join_query = ts_join_query.ReplaceAll("intSalmanID", "intT1Salesman"); //ulsan only
                                            }
                                            else
                                            {
                                                ts_join_query += tmp_join_plus + tmp_join_add;
                                            }
                                            //ts_join_query += tmp_join_plus;
                                        }

                                        query = query.ReplaceAll(isnull_query, "@isnull");
                                        query = query.ReplaceAll(ts_join_query, "@leftjoinQuery");

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
                            throw new Exception("APS Customer sync requires backend rules");
                        }

                        mssql_rule.Iterate<APSRule>((database, idx) =>
                        {
                            SQLServer mssql = new SQLServer();
                            mssql.Connect(dbname: database.DBname);

                            ArrayList queryResult = mssql.Select(database.Query);
                            mssql.Message("[" + database.DBname + "] ---->" + database.Query);

                            string mysql_insert = string.Empty;
                            string mssql_insert = string.Empty;

                            string insertQuery = "INSERT INTO cms_customer (@columns) VALUES @values ON DUPLICATE KEY UPDATE @update_columns";
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

                                    if (find_mssql_field == "dtCreatedDate")
                                    {
                                        string createdDate = string.Empty;

                                        map.Iterate<KeyValuePair<string, string>>((mssql_fields, mssqIndx) =>
                                        {
                                            if (mssql_fields.Key == "dtCreatedDate")
                                            {
                                                createdDate = mssql_fields.Value; //"7/11/2012 9:31:06 AM"

                                                DateTime date = DateTime.Parse(createdDate);

                                                createdDate = date.ToString("s");
                                            }
                                        });

                                        Database.Sanitize(ref createdDate);

                                        row += inIdx == 0 ? "('" + createdDate + "" : "','" + createdDate;

                                        NoMssqlField = false;
                                        addedToRow = false;
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
                                                    if(eachField != "created_date")
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
                                                if (row.Contains(tmp) == false && corr_mysql_field != "created_date")
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
                                    mysql.Message("[" + mysqlconfig.config_database + "] ---->" + insertQuery);

                                    insertQuery = insertQuery.ReplaceAll("@values", values);
                                    valueString.Clear();

                                    logger.message = string.Format("{0} customer records is inserted into " + mysqlconfig.config_database, RecordCount);
                                    logger.Broadcast();
                                }

                            });

                            if (valueString.Count > 0)
                            {
                                string values = valueString.Join(",");

                                insertQuery = insertQuery.ReplaceAll(values, "@values");

                                mysql.Insert(insertQuery);
                                mysql.Message("[" + mysqlconfig.config_database + "] ---->" + insertQuery);

                                insertQuery = insertQuery.ReplaceAll("@values", values);
                                valueString.Clear();

                                logger.message = string.Format("{0} customer records is inserted into " + mysqlconfig.config_database, RecordCount);
                                logger.Broadcast();
                            }
                            RecordCount = 0; /* reset count for the next db */
                        });

                        if (cms_updated_time.Count > 0)
                        {
                            mysql.Insert("UPDATE cms_update_time SET updated_at = NOW() WHERE table_name = 'cms_customer'");
                        }
                        else
                        {
                            mysql.Insert("INSERT INTO cms_update_time(table_name, updated_at) VALUES('cms_customer', NOW())");
                        }

                    });

                    slog.action_identifier = Constants.Action_APSCustomerSync;
                    slog.action_details = Constants.Tbl_cms_customer + Constants.Is_Finished + string.Format(" ({0}) records", RecordCount);
                    slog.action_failure = 0;
                    slog.action_failure_message = string.Empty;
                    slog.action_time = DateTime.Now.ToString();


                    DateTime endTime = DateTime.Now;
                    TimeSpan timespan = endTime - startTime;
                    Console.WriteLine("Completed in: " + timespan.TotalSeconds.ToString("F"));

                    LocalDB.InsertSyncLog(slog);

                    logger.message = "APS customer sync finished in (" + timespan.TotalSeconds.ToString("F") + " seconds)";
                    logger.Broadcast();

                });

                thread.Start();
                //thread.Join();

            }
            catch (ThreadAbortException e)
            {
                DpprException ex = new DpprException
                {
                    file_name = "JobAPSCustomerSync",
                    exception = e.Message,
                    time = DateTime.Now.ToString()
                };
                LocalDB.InsertException(ex);

                Console.WriteLine(Constants.Thread_Exception + e.Message);
            }
        }
    }
}
