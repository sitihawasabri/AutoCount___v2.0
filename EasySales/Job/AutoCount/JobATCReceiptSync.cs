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
    public class JobATCReceiptSync : IJob
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
                    slog.action_identifier = Constants.Action_ATCReceiptSync;
                    slog.action_details = Constants.Tbl_cms_receipt + Constants.Is_Starting;
                    slog.action_failure = 0;
                    slog.action_failure_message = string.Empty;
                    slog.action_time = DateTime.Now.ToString();

                    DateTime startTime = DateTime.Now;

                    LocalDB.InsertSyncLog(slog);

                    logger.message = "ATC receipt sync is running";
                    logger.Broadcast();

                    List<DpprMySQLconfig> mysql_list = LocalDB.GetRemoteDatabaseConfig().Distinct().ToList();
                    List<DpprSQLServerconfig> mssql_list = LocalDB.GetRemoteSQLServerConfig();

                    mysql_list.Iterate<DpprMySQLconfig>((mysql_config, index) =>
                    {
                        DpprMySQLconfig mysqlconfig = mysql_list[index];
                        Database mysql = new Database();
                        mysql.Connect(index: index);

                        CheckBackendRule checkDB = new CheckBackendRule(mysql: mysql);
                        dynamic jsonRule = checkDB.CheckTablesExist().GetSettingByTableName("cms_receipt_atc");

                        Dictionary<string, string> cms_updated_time = mysql.GetUpdatedTime("cms_receipt");

                        ArrayList mssql_rule = new ArrayList();

                        if (jsonRule != null)
                        {
                            foreach (var key in jsonRule)
                            {
                                if (key.mssql != null && key.mssql.GetType().ToString() == "Newtonsoft.Json.Linq.JArray")
                                {
                                    string query = "SELECT pm.DocKey, pm.Description, DocNo, DebtorCode, DocDate, pm.PaymentAmt, KnockOffAmt, Cancelled, RefundAmt, (SELECT ChequeNo+ ',' FROM dbo.ARPaymentDTL WHERE DocKey = pm.DocKey GROUP BY DocKey,ChequeNo FOR XML PATH('')) as ChequeNo FROM dbo.ARPayment AS pm LEFT JOIN dbo.ARPaymentDTL AS dtl ON pm.DocKey = dtl.DocKey ORDER BY docdate";

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
                            throw new Exception("ATC Receipt sync requires backend rules");
                        }

                        mysql.Insert("UPDATE cms_receipt SET cancelled = 'T'"); //deactivate all first

                        ArrayList custAgentFromDb = mysql.Select("SELECT cs.customer_id, c.cust_code, cs.salesperson_id FROM cms_customer_salesperson AS cs LEFT JOIN cms_customer AS c ON cs.customer_id = c.cust_id");
                        Dictionary<string, string> custAgentList = new Dictionary<string, string>();

                        for (int i = 0; i < custAgentFromDb.Count; i++)
                        {
                            Dictionary<string, string> each = (Dictionary<string, string>)custAgentFromDb[i];
                            string ___custCode = each["cust_code"];
                            if (!custAgentList.ContainsKey(___custCode))
                            {
                                custAgentList.Add(each["cust_code"], each["salesperson_id"]);
                            }
                        }
                        custAgentFromDb.Clear();
                        logger.Broadcast("custAgentList : " + custAgentList.Count);

                        mssql_rule.Iterate<ATCRule>((database, idx) =>
                        {
                            SQLServer mssql = new SQLServer();
                            mssql.Connect(dbname: database.DBname);

                            //Console.WriteLine(database.Query);

                            ArrayList queryResult = mssql.Select(database.Query);

                            string mysql_insert = string.Empty;
                            string mssql_insert = string.Empty;

                            string insertQuery = "INSERT INTO cms_receipt (@columns) VALUES @values ON DUPLICATE KEY UPDATE @update_columns";
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
                                string custCode = string.Empty;
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
                                        string rcptDate = string.Empty;

                                        map.Iterate<KeyValuePair<string, string>>((mssql_fields, mssqIndx) =>
                                        {
                                            if (mssql_fields.Key == "DocDate")
                                            {
                                                rcptDate = mssql_fields.Value;                //"14/02/2020 11:37:33"
                                                rcptDate = Convert.ToDateTime(rcptDate).ToString("yyyy-MM-dd HH:mm:ss");
                                            }
                                        });

                                        row += inIdx == 0 ? "('" + rcptDate + "" : "','" + rcptDate;

                                        NoMssqlField = false;
                                        addedToRow = true;
                                    }

                                    if (find_mssql_field == "payment_amt")
                                    {
                                        string _PaymentAmt = string.Empty;
                                        string _RefundAmt = string.Empty;

                                        map.Iterate<KeyValuePair<string, string>>((mssql_fields, mssqIndx) =>
                                        {
                                            //$payment_amt = floatval($row->PaymentAmt) - floatval($row->RefundAmt);
                                            if (mssql_fields.Key == "PaymentAmt")
                                            {
                                                _PaymentAmt = mssql_fields.Value;
                                            }

                                            if (mssql_fields.Key == "RefundAmt")
                                            {
                                                _RefundAmt = mssql_fields.Value;
                                            }

                                        });

                                        double.TryParse(_PaymentAmt, out double PaymentAmt);
                                        double.TryParse(_RefundAmt, out double RefundAmt);

                                        double payment_amt;
                                        payment_amt = PaymentAmt - RefundAmt;

                                        row += inIdx == 0 ? "('" + payment_amt + "" : "','" + payment_amt;

                                        NoMssqlField = false;
                                        addedToRow = true;
                                    }

                                    if (find_mssql_field == "DebtorCode")
                                    {
                                        RecordCount++;

                                        map.Iterate<KeyValuePair<string, string>>((mssql_fields, mssqIndx) =>
                                        {
                                            if (mssql_fields.Key == "DebtorCode")
                                            {
                                                custCode = mssql_fields.Value;
                                            }
                                        });

                                        row += inIdx == 0 ? "('" + custCode + "" : "','" + custCode;

                                        NoMssqlField = false;
                                        addedToRow = true;
                                    }

                                    if (corr_mysql_field == "salesperson_id")
                                    {
                                        string salespersonId = "0";
                                        //Console.WriteLine("salespersonCode:" + salespersonCode);
                                        if (string.IsNullOrEmpty(custCode) || !custAgentList.TryGetValue(custCode, out salespersonId))
                                        {
                                            salespersonId = "0";
                                        }

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
                                    mysql.Message("Receipt Query: " + insertQuery);

                                    insertQuery = insertQuery.ReplaceAll("@values", values);
                                    valueString.Clear();

                                    logger.message = string.Format("{0} receipt records is inserted into " + mysqlconfig.config_database, RecordCount);
                                    logger.Broadcast();
                                }
                            });
                            

                            if (valueString.Count > 0)
                            {
                                string values = valueString.Join(",");

                                insertQuery = insertQuery.ReplaceAll(values, "@values");

                                mysql.Insert(insertQuery);
                                mysql.Message("Receipt Query: " + insertQuery);

                                insertQuery = insertQuery.ReplaceAll("@values", values);
                                valueString.Clear();

                                logger.message = string.Format("{0} receipt records is inserted into " + mysqlconfig.config_database, RecordCount);
                                logger.Broadcast();
                            }

                            if (cms_updated_time.Count > 0)
                            {
                                mysql.Insert("UPDATE cms_update_time SET updated_at = NOW() WHERE table_name = 'cms_receipt'");
                            }
                            else
                            {
                                mysql.Insert("INSERT INTO cms_update_time(table_name, updated_at) VALUES('cms_receipt', NOW())");
                            }

                            RecordCount = 0; /* reset count */
                            mysqlFieldList.Clear();
                            queryResult.Clear();
                        });
                        mssql_rule.Clear();
                        cms_updated_time.Clear();
                    });

                    mysql_list.Clear();
                    mssql_list.Clear();

                    slog.action_identifier = Constants.Action_ATCReceiptSync;
                    slog.action_details = Constants.Tbl_cms_receipt + Constants.Is_Finished + string.Format(" ({0}) records", RecordCount);
                    slog.action_failure = 0;
                    slog.action_failure_message = string.Empty;
                    slog.action_time = DateTime.Now.ToString();

                    DateTime endTime = DateTime.Now;
                    TimeSpan ts = endTime - startTime;
                    //Console.WriteLine("Completed in: " + ts.TotalSeconds.ToString("F"));

                    LocalDB.InsertSyncLog(slog);

                    logger.message = "ATC receipt sync finished in (" + ts.TotalSeconds.ToString("F") + " seconds)";
                    logger.Broadcast();

                });

                thread.Start();
                //thread.Join();

            }
            catch (ThreadAbortException e)
            {
                DpprException ex = new DpprException
                {
                    file_name = "JobATCReceiptSync",
                    exception = e.Message,
                    time = DateTime.Now.ToString()
                };
                LocalDB.InsertException(ex);

                Console.WriteLine(Constants.Thread_Exception + e.Message);
            }
        }
    }
}
