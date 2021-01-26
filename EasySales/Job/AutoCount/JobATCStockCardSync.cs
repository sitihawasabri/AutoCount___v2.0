using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using EasySales.Model;
using EasySales.Object;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Quartz;
using RestSharp;
using Ubiety.Dns.Core.Records;

namespace EasySales.Job
{
    [DisallowConcurrentExecution]
    public class JobATCStockCardSync : IJob
    {
        private string sync_today_only = "0";
        public void ExecuteSyncTodayOnly(string sync_today_only)
        {
            GlobalLogger logger = new GlobalLogger();
            this.sync_today_only = sync_today_only;
            logger.Broadcast("sync_today_only:" + this.sync_today_only);
            Execute();
        }
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
                    slog.action_identifier = Constants.Action_ATCStockCardSync;
                    slog.action_details = "cms_stock_card";
                    slog.action_failure = 0;
                    slog.action_failure_message = string.Empty;
                    slog.action_time = DateTime.Now.ToString();

                    DateTime startTime = DateTime.Now;

                    LocalDB.InsertSyncLog(slog);

                    logger.message = "ATC stock card sync is running";
                    logger.Broadcast();

                    List<DpprMySQLconfig> mysql_list = LocalDB.GetRemoteDatabaseConfig().Distinct().ToList();
                    List<DpprSQLServerconfig> mssql_list = LocalDB.GetRemoteSQLServerConfig();

                    mysql_list.Iterate<DpprMySQLconfig>((mysql_config, index) =>
                    {
                        DpprMySQLconfig mysqlconfig = mysql_list[index];
                        Database mysql = new Database();
                        mysql.Connect(index: index);

                        CheckBackendRule checkDB = new CheckBackendRule(mysql: mysql);
                        dynamic jsonRule = checkDB.CheckTablesExist().GetSettingByTableName("cms_stock_card_atc");

                        Dictionary<string, string> cms_updated_time = mysql.GetUpdatedTime3MonthInterval("cms_stock_card"); //ONE YEAR

                        ArrayList mssql_rule = new ArrayList();

                        if (jsonRule != null)
                        {
                            foreach (var key in jsonRule)
                            {
                                if (key.mssql != null && key.mssql.GetType().ToString() == "Newtonsoft.Json.Linq.JArray")
                                {
                                    string query = "SELECT * from StockDTL";

                                    logger.Broadcast("sync_today_only:" + this.sync_today_only);

                                    string updated_at = string.Empty;

                                    if (this.sync_today_only == "1")
                                    {
                                        cms_updated_time = mysql.GetUpdatedTimeToday("cms_stock_card");

                                        if (cms_updated_time.Count > 0)
                                        {
                                            updated_at = cms_updated_time["updated_at"].ToString().MSSQLdate();
                                            logger.Broadcast("Sync today only: " + updated_at);
                                            query += " WHERE LastModified >= '" + updated_at + "'";
                                        }
                                    }
                                    else
                                    {
                                        if (cms_updated_time.Count > 0)
                                        {
                                            updated_at = cms_updated_time["updated_at"].ToString().MSSQLdate();
                                            query += " WHERE LastModified >= '" + updated_at + "'";
                                        }
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
                            throw new Exception("ATC Stock Card sync requires backend rules");
                        }

                        string deactivateOldRecords = "SELECT id, stock_dtl_key, doc_type, doc_key FROM cms_stock_card WHERE cancelled = 'F' ";
                        if (cms_updated_time.Count > 0)
                        {
                            string updated_at = cms_updated_time["updated_at"].ToString().MSSQLdate();
                            deactivateOldRecords += " AND last_modified >= '" + updated_at + "'";
                        }
                        ArrayList inDBactiveTrans = mysql.Select(deactivateOldRecords);
                        logger.Broadcast("Active stock card transactions in DB: " + inDBactiveTrans.Count);
                        Dictionary<string, string> uniqueKeyList = new Dictionary<string, string>();
                        for (int i = 0; i < inDBactiveTrans.Count; i++)
                        {
                            Dictionary<string, string> each = (Dictionary<string, string>)inDBactiveTrans[i];
                            string id = each["id"].ToString();
                            string stock_dtl_key = each["stock_dtl_key"].ToString();
                            string doc_type = each["doc_type"].ToString();
                            string doc_key = each["doc_key"].ToString();
                            string unique = stock_dtl_key + doc_type + doc_key;
                            string uniqueLowercase = unique.ToLower();
                            uniqueKeyList.Add(id, uniqueLowercase);
                        }
                        inDBactiveTrans.Clear();

                        mssql_rule.Iterate<ATCRule>((database, idx) =>
                        {
                            SQLServer mssql = new SQLServer();
                            mssql.Connect(dbname: database.DBname);

                            Console.WriteLine(database.Query);

                            ArrayList queryResult = mssql.Select(database.Query);
                            Console.WriteLine("queryResult.Count: " + queryResult.Count);

                            string mysql_insert = string.Empty;
                            string mssql_insert = string.Empty;

                            string insertQuery = "INSERT INTO cms_stock_card (@columns) VALUES @values ON DUPLICATE KEY UPDATE @update_columns";
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
                                string stockDtlKey = string.Empty;
                                string docType = string.Empty;
                                string docKey = string.Empty;

                                database.Include.Iterate<Dictionary<string, string>>((include, inIdx) =>
                                {
                                    string nullfield = include["nullfield"];
                                    string find_mssql_field = include["mssql"];
                                    string corr_mysql_field = include["mysql"];

                                    bool NoMssqlField = true;
                                    bool addedToRow = false;

                                    if (find_mssql_field == "DocDate")
                                    {
                                        string Date = string.Empty;

                                        map.Iterate<KeyValuePair<string, string>>((mssql_fields, mssqIndx) =>
                                        {
                                            if (mssql_fields.Key == "DocDate")
                                            {
                                                Date = mssql_fields.Value;                //"14/02/2020 11:37:33"
                                                Date = Convert.ToDateTime(Date).ToString("yyyy-MM-dd HH:mm:ss");
                                            }
                                        });

                                        row += inIdx == 0 ? "('" + Date + "" : "','" + Date;

                                        NoMssqlField = false;
                                        addedToRow = true;
                                    }

                                    if (find_mssql_field == "LastModified")
                                    {
                                        string Date = string.Empty;

                                        map.Iterate<KeyValuePair<string, string>>((mssql_fields, mssqIndx) =>
                                        {
                                            if (mssql_fields.Key == "LastModified")
                                            {
                                                Date = mssql_fields.Value;                //"14/02/2020 11:37:33"
                                                Date = Convert.ToDateTime(Date).ToString("yyyy-MM-dd HH:mm:ss");
                                            }
                                        });

                                        row += inIdx == 0 ? "('" + Date + "" : "','" + Date;

                                        NoMssqlField = false;
                                        addedToRow = true;
                                    }

                                    if (find_mssql_field == "DocType")
                                    {
                                        string DocType = string.Empty;

                                        map.Iterate<KeyValuePair<string, string>>((mssql_fields, mssqIndx) =>
                                        {
                                            if (mssql_fields.Key == "DocType")
                                            {
                                                DocType = mssql_fields.Value;
                                                //IV - invoice
                                                //CN - creditnote
                                                //CS - cash sales
                                                //PI - purchase invoice
                                                //ST - stock transfer ---- change to XF (follow sql)
                                                if (DocType == "ST")
                                                {
                                                    DocType = "XF";
                                                }
                                            }
                                        });

                                        docType = DocType;
                                        row += inIdx == 0 ? "('" + DocType + "" : "','" + DocType;

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
                                                    /* all field except itemcode/itemtype && find_mssql_field which has string/join dont insert here */
                                                    if (corr_mysql_field == eachField && LogicParser.IsCodeStr(find_mssql_field) == false)
                                                    {
                                                        Database.Sanitize(ref tmp);
                                                        stockDtlKey = stockDtlKey == string.Empty && corr_mysql_field == "stock_dtl_key" ? tmp : stockDtlKey = stockDtlKey;
                                                        docKey = docKey == string.Empty && corr_mysql_field == "doc_key" ? tmp : docKey = docKey;
                                                        row += inIdx == 0 ? "('" + tmp + "" : "','" + tmp;
                                                        addedToRow = true;
                                                    }
                                                }
                                            }

                                            if (!addedToRow)
                                            {
                                                Database.Sanitize(ref tmp);
                                                if (row.Contains(tmp) == false && corr_mysql_field != "category_id" && corr_mysql_field != "product_code")
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
                                        string tmp = LogicParser.Parse(corr_mysql_field, find_mssql_field, map, nullfield)[corr_mysql_field];
                                        Database.Sanitize(ref tmp);
                                        row += inIdx == 0 ? "('" + tmp + "" : "','" + tmp;
                                    }
                                });

                                if (row != string.Empty)
                                {
                                    RecordCount++;
                                    row += "')";
                                    valueString.Add(row);

                                    string uniqueKey = stockDtlKey + docType + docKey;
                                    string uniqueLowercase = uniqueKey.ToLower();
                                    if (uniqueKeyList.ContainsValue(uniqueLowercase))
                                    {
                                        var key = uniqueKeyList.Where(pair => pair.Value == uniqueLowercase)
                                                    .Select(pair => pair.Key)
                                                    .FirstOrDefault();
                                        if (key != null)
                                        {
                                            uniqueKeyList.Remove(key);
                                        }
                                    }
                                }

                                if (valueString.Count % 2000 == 0)
                                {
                                    string values = valueString.Join(",");

                                    insertQuery = insertQuery.ReplaceAll(values, "@values");

                                    mysql.Insert(insertQuery);
                                    mysql.Message(insertQuery);

                                    insertQuery = insertQuery.ReplaceAll("@values", values);
                                    valueString.Clear();

                                    logger.message = string.Format("{0} stock card records is inserted into " + mysqlconfig.config_database, RecordCount);
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

                                logger.message = string.Format("{0} stock card records is inserted into " + mysqlconfig.config_database, RecordCount);
                                logger.Broadcast();
                            }

                            if (sync_today_only == "0")
                            {
                                if (uniqueKeyList.Count > 0)
                                {
                                    logger.Broadcast("Total stock card transactions to be deactivated: " + uniqueKeyList.Count);

                                    HashSet<string> deactivateId = new HashSet<string>();
                                    for (int i = 0; i < uniqueKeyList.Count; i++)
                                    {
                                        string _id = uniqueKeyList.ElementAt(i).Key;
                                        deactivateId.Add(_id);
                                    }

                                    string ToBeDeactivate = "'" + string.Join("','", deactivateId) + "'";
                                    Console.WriteLine(ToBeDeactivate);

                                    string inactive = "UPDATE cms_stock_card SET cancelled = 'T' WHERE id IN (" + ToBeDeactivate + ")";
                                    mysql.Insert(inactive);

                                    logger.Broadcast(uniqueKeyList.Count + " stock card transactions deactivated");

                                    uniqueKeyList.Clear();
                                }
                            }

                            string updateTimeQuery = cms_updated_time.Count > 0 ? "UPDATE cms_update_time SET updated_at = NOW() WHERE table_name = 'cms_stock_card'" : "INSERT INTO cms_update_time(table_name, updated_at) VALUES('cms_stock_card', NOW())";
                            mysql.Insert(updateTimeQuery);
                            mysql.Message("updateTimeQuery: " + updateTimeQuery);

                            RecordCount = 0; /* reset count for the next database */
                            mysqlFieldList.Clear();
                            queryResult.Clear();
                        });
                        mssql_rule.Clear();
                        cms_updated_time.Clear();
                    });

                    mysql_list.Clear();
                    mssql_list.Clear();

                    slog.action_identifier = Constants.Action_ATCStockCardSync;
                    slog.action_details = "cms_stock_card" + Constants.Is_Finished + string.Format(" ({0}) records", RecordCount);
                    slog.action_failure = 0;
                    slog.action_failure_message = string.Empty;
                    slog.action_time = DateTime.Now.ToString();

                    DateTime endTime = DateTime.Now;
                    TimeSpan ts = endTime - startTime;
                    //Console.WriteLine("Completed in: " + ts.TotalSeconds.ToString("F"));

                    LocalDB.InsertSyncLog(slog);

                    logger.message = "ATC stock card sync finished in (" + ts.TotalSeconds.ToString("F") + " seconds)";
                    logger.Broadcast();

                });

                thread.Start();
                //thread.Join();

            }
            catch (ThreadAbortException e)
            {
                DpprException ex = new DpprException
                {
                    file_name = "JobATCStockCardSync",
                    exception = e.Message,
                    time = DateTime.Now.ToString()
                };
                LocalDB.InsertException(ex);

                Console.WriteLine(Constants.Thread_Exception + e.Message);
            }
        }
    }
}