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
    class JobAPSDOSync : IJob
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
                    slog.action_identifier = Constants.Action_APSDOSync;
                    slog.action_details = Constants.Tbl_cms_do + Constants.Is_Starting;
                    slog.action_failure = 0;
                    slog.action_failure_message = string.Empty;
                    slog.action_time = DateTime.Now.ToString();

                    DateTime startTime = DateTime.Now;

                    LocalDB.InsertSyncLog(slog);

                    logger.message = "APS DO sync is running";
                    logger.Broadcast();

                    List<DpprMySQLconfig> mysql_list = LocalDB.GetRemoteDatabaseConfig().Distinct().ToList();
                    List<DpprSQLServerconfig> mssql_list = LocalDB.GetRemoteSQLServerConfig();

                    mysql_list.Iterate<DpprMySQLconfig>((mysql_config, index) =>
                    {
                        DpprMySQLconfig mysqlconfig = mysql_list[index];
                        Database mysql = new Database();
                        mysql.Connect(index: index);

                        CheckBackendRule checkDB = new CheckBackendRule(mysql: mysql);
                        dynamic jsonRule = checkDB.CheckTablesExist().GetSettingByTableName("cms_do");

                        Dictionary<string, string> cms_updated_time = mysql.GetUpdatedTime("cms_do");

                        ArrayList mssql_rule = new ArrayList();

                        if (jsonRule != null)
                        {
                            foreach (var key in jsonRule)
                            {
                                if (key.mssql != null && key.mssql.GetType().ToString() == "Newtonsoft.Json.Linq.JArray")
                                {
                                    //SELECT do.varTrxNo, do.dtCustDoDate, do.decDOAfterCalcAmt, do.blnIsDelete, cust.charRef, nest.intCreatedBy, sale.charUserID, do.varRemarks FROM vwActiveSal_DOTbl do left join Sal_DOTbl sdo on sdo.varTrxNo = do.varTrxNo left join Sal_CustomerTbl cust on do.intCustID = cust.intCustID left join vwSal_SalesmanTbl sale ON sale.intUserID = do.intCreatedBy left join(select soi.intCreatedBy, doi.varTrxNo from Sal_DOTbl doi inner join Sal_DODetailsTbl dod on doi.intDONo = dod.intDONo left outer join Sal_SODetailsTbl sod on dod.intSODetailsNo = sod.intSODetailsNo left outer join Sal_SOTbl soi on sod.intSONo = soi.intSONo group by doi.varTrxNo, soi.intCreatedBy ) nest on nest.varTrxNo = do.varTrxNo @dateQuery order by varTrxNo desc
                                    //string query = "SELECT varTrxNo, dtCustDoDate, decDOAfterCalcAmt, do.blnIsDelete, cust.charRef, do.intCreatedBy, sale.charUserID, do.varRemarks, do.varRefNo FROM vwActiveSal_DOTbl do inner join Sal_CustomerTbl cust on do.intCustID = cust.intCustID inner join vwSal_SalesmanTbl sale ON sale.intUserID = do.intCreatedBy @dateQuery order by varTrxNo desc";
                                    //string date_query = " where do.dtCreatedDate >= cast('@dateFrom' as Date) and do.dtCreatedDate <= cast('@dateTo' as Date) ";
                                    
                                    
                                    string query = "SELECT do.varTrxNo, do.dtCustDoDate, do.decDOAfterCalcAmt, do.blnIsDelete, cust.charRef, nest.intCreatedBy, sale.charUserID, do.varRemarks, do.varRefNo FROM vwActiveSal_DOTbl do left join Sal_DOTbl sdo on sdo.varTrxNo = do.varTrxNo left join Sal_CustomerTbl cust on do.intCustID = cust.intCustID left join(select soi.intCreatedBy, doi.varTrxNo from Sal_DOTbl doi inner join Sal_DODetailsTbl dod on doi.intDONo = dod.intDONo left outer join Sal_SODetailsTbl sod on dod.intSODetailsNo = sod.intSODetailsNo left outer join Sal_SOTbl soi on sod.intSONo = soi.intSONo group by doi.varTrxNo, soi.intCreatedBy) nest on nest.varTrxNo = do.varTrxNo left join vwSal_SalesmanTbl sale ON sale.intUserID = nest.intCreatedBy @dateQuery order by varTrxNo desc";
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
                                    }
                                    else
                                    {
                                        query = query.ReplaceAll("", "@dateQuery");
                                    }

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
                            throw new Exception("APS Delivery Order sync requires backend rules");
                        }

                        ArrayList salespersonFromDb = mysql.Select("SELECT login_id, UPPER(TRIM(staff_code)) AS staff_code FROM cms_login");
                        Dictionary<string, string> salespersonList = new Dictionary<string, string>();

                        for (int i = 0; i < salespersonFromDb.Count; i++)
                        {
                            Dictionary<string, string> each = (Dictionary<string, string>)salespersonFromDb[i];
                            salespersonList.Add(each["staff_code"], each["login_id"]);
                        }

                        mssql_rule.Iterate<APSRule>((database, idx) =>
                        {
                            SQLServer mssql = new SQLServer();
                            mssql.Connect(dbname: database.DBname);

                            //Console.WriteLine(database.Query);

                            ArrayList queryResult = mssql.Select(database.Query);
                            mssql.Message("DO Query [" + database.DBname + "] ---> " + database.Query);

                            string mysql_insert = string.Empty;
                            string mssql_insert = string.Empty;

                            string insertQuery = "INSERT INTO cms_do (@columns) VALUES @values ON DUPLICATE KEY UPDATE @update_columns";
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

                                    //custCode = obj["charRef"];
                                    //doAmt = obj["decDOAfterCalcAmt"];
                                    //remarks = obj["varRemarks"];
                                    //charUserId = obj["charUserId"];
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

                                        row += inIdx == 0 ? "('" + doCode + "" : "','" + doCode;

                                        NoMssqlField = false;
                                        addedToRow = true;
                                    }

                                    if (find_mssql_field == "charUserID")
                                    {
                                        string _salespersonName = string.Empty;
                                        string _salespersonId = "0";

                                        map.Iterate<KeyValuePair<string, string>>((mssql_fields, mssqIndx) =>
                                        {
                                            if (mssql_fields.Key == "charUserID")
                                            {
                                                _salespersonName = mssql_fields.Value;

                                                row += inIdx == 0 ? "('" + _salespersonName + "" : "','" + _salespersonName;

                                                if (string.IsNullOrEmpty(_salespersonName) || !salespersonList.TryGetValue(_salespersonName, out _salespersonId))
                                                {
                                                    _salespersonId = "0";
                                                }
                                            }
                                        });
                                        row += inIdx == 0 ? "('" + _salespersonId + "" : "','" + _salespersonId;

                                        NoMssqlField = false;
                                        addedToRow = true;
                                    }

                                    if (find_mssql_field == "salesperson")
                                    {
                                        //already insert the salesperson value in find_mssql_field == "charUserID" 
                                        NoMssqlField = false;
                                        addedToRow = true;
                                    }

                                    if (find_mssql_field == "dtCustDoDate")
                                    {
                                        string doDate = string.Empty;

                                        map.Iterate<KeyValuePair<string, string>>((mssql_fields, mssqIndx) =>
                                        {
                                            if (mssql_fields.Key == "dtCustDoDate")
                                            {
                                                doDate = mssql_fields.Value;
                                                doDate = Convert.ToDateTime(doDate).ToString("yyyy-MM-dd HH:mm:ss");
                                            }
                                        });

                                        row += inIdx == 0 ? "('" + doDate + "" : "','" + doDate;

                                        NoMssqlField = false;
                                        addedToRow = true;
                                    }

                                    if (find_mssql_field == "blnIsDelete")
                                    {
                                        string cancelled = string.Empty;

                                        map.Iterate<KeyValuePair<string, string>>((mssql_fields, mssqIndx) =>
                                        {
                                            if (mssql_fields.Key == "blnIsDelete")
                                            {
                                                cancelled = mssql_fields.Value;

                                                if (cancelled == "FALSE")
                                                {
                                                    cancelled = "F";
                                                }
                                                else
                                                {
                                                    cancelled = "T";
                                                }
                                            }
                                        });

                                        row += inIdx == 0 ? "('" + cancelled + "" : "','" + cancelled;

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

                                    logger.message = string.Format("{0} delivery order records is inserted into " + mysqlconfig.config_database, RecordCount);
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

                                logger.message = string.Format("{0} delivery order records is inserted into " + mysqlconfig.config_database, RecordCount);
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

                    //mysql.Insert("INSERT INTO cms_update_time(table_name) VALUES ('cms_do') ON DUPLICATE KEY UPDATE table_name = VALUES(table_name), updated_at = VALUES(updated_at)");

                    slog.action_identifier = Constants.Action_APSDOSync;
                    slog.action_details = Constants.Tbl_cms_do + Constants.Is_Finished + string.Format(" ({0}) records", RecordCount);
                    slog.action_failure = 0;
                    slog.action_failure_message = string.Empty;
                    slog.action_time = DateTime.Now.ToString();

                    DateTime endTime = DateTime.Now;
                    TimeSpan ts = endTime - startTime;
                    Console.WriteLine("Completed in: " + ts.TotalSeconds.ToString("F"));

                    LocalDB.InsertSyncLog(slog);
                    logger.message = "APS DO sync finished in (" + ts.TotalSeconds.ToString("F") + " seconds)";
                    logger.Broadcast();

                });

                thread.Start();
                //thread.Join();

            }
            catch (ThreadAbortException e)
            {
                DpprException ex = new DpprException
                {
                    file_name = "JobAPSDoSync",
                    exception = e.Message,
                    time = DateTime.Now.ToString()
                };
                LocalDB.InsertException(ex);

                Console.WriteLine(Constants.Thread_Exception + e.Message);
            }
        }
    }
}
