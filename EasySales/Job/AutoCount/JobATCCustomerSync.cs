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
    public class JobATCCustomerSync : IJob
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
                    slog.action_identifier = Constants.Action_ATCCustomerSync;                                  /*check again */
                    slog.action_details = Constants.Tbl_cms_customer + Constants.Is_Starting;                   /*check again */
                    slog.action_failure = 0;
                    slog.action_failure_message = string.Empty;
                    slog.action_time = DateTime.Now.ToString();

                    DateTime startTime = DateTime.Now;
                    LocalDB.DBCleanup();
                    LocalDB.InsertSyncLog(slog);

                    logger.message = "ATC customer sync is running";
                    logger.Broadcast();

                    List<DpprMySQLconfig> mysql_list = LocalDB.GetRemoteDatabaseConfig().Distinct().ToList();
                    List<DpprSQLServerconfig> mssql_list = LocalDB.GetRemoteSQLServerConfig();
                   

                    mysql_list.Iterate<DpprMySQLconfig>((mysql_config, index) =>
                    {
                        DpprMySQLconfig mysqlconfig = mysql_list[index];
                        Database mysql = new Database();
                        mysql.Connect(index: index);
                       
                        CheckBackendRule checkDB = new CheckBackendRule(mysql: mysql);
                        dynamic jsonRule = checkDB.CheckTablesExist().GetSettingByTableName("cms_customer_atc");

                        Dictionary<string, string> cms_updated_time = mysql.GetUpdatedTime("cms_customer");

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
                                        string query = "SELECT AccNo, CreditLimit, CompanyName, Attention, EmailAddress, Phone1, Fax1, Address1, Address2, Address3, Address4, PostCode, DeliverAddr1, DeliverAddr2, DeliverAddr3, DeliverAddr4, DisplayTerm, PriceCategory, IsActive from dbo.Debtor order by AccNo";

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
                            throw new Exception("ATC Customer sync requires backend rules");
                        }

                        ArrayList getActiveCustomers = mysql.Select("SELECT cust_code FROM cms_customer WHERE customer_status = 1");
                        ArrayList activeCustCode = new ArrayList();
                        for(int i = 0; i < getActiveCustomers.Count; i++)
                        {
                            Dictionary<string, string> each = (Dictionary<string, string>)getActiveCustomers[i];
                            string custCode = each["cust_code"];
                            mysql.Message("cust code: " + custCode);

                            if(!activeCustCode.Contains(custCode))
                            {
                                activeCustCode.Add(custCode);
                            }
                        }
                        getActiveCustomers.Clear();
                        logger.Broadcast("Active Cust Code in DB: " + activeCustCode.Count);

                        mssql_rule.Iterate<ATCRule>((database, idx) =>
                        {
                            SQLServer mssql = new SQLServer();
                            mssql.Connect(dbname: database.DBname);

                            ArrayList outstandingFromDb = mssql.Select("SELECT LTRIM ( RTRIM ( invoice.DebtorCode ) ) AS DebtorCode, SUM (CASE WHEN invoice.Outstanding IS NULL THEN 0 ELSE invoice.Outstanding END + CASE WHEN debit.Outstanding IS NULL THEN 0 ELSE debit.Outstanding END - CASE WHEN credit.Outstanding IS NULL THEN 0 ELSE credit.Outstanding END - CASE WHEN payment.Outstanding IS NULL THEN 0 ELSE payment.Outstanding END ) AS Outstanding FROM(SELECT[DebtorCode], SUM( [Outstanding]) AS Outstanding FROM[dbo].[ARInvoice] WHERE[JournalType] = 'SALES' AND[Cancelled] = 'F' GROUP BY[DebtorCode]) invoice LEFT JOIN( SELECT[DebtorCode] , SUM( [Outstanding]) AS Outstanding FROM[dbo].[ARDN] WHERE[Cancelled] = 'F' GROUP BY[DebtorCode]) debit ON debit.DebtorCode = invoice.DebtorCode LEFT JOIN(SELECT[DebtorCode], (SUM ( [NetTotal] ) -SUM( [KnockOffAmt] ) -SUM( [RefundAmt] ) ) AS Outstanding FROM[dbo].[ARCN] WHERE[Cancelled] = 'F' GROUP BY[DebtorCode] ) credit ON credit.DebtorCode = invoice.DebtorCode LEFT JOIN(SELECT[DebtorCode] , (SUM( [PaymentAmt] ) -SUM( [KnockOffAmt] ) -SUM( [RefundAmt] ) ) AS Outstanding FROM[dbo].[ARPayment] WHERE[Cancelled] = 'F' GROUP BY[DebtorCode]) payment ON invoice.DebtorCode = payment.DebtorCode GROUP BY invoice.DebtorCode ORDER BY invoice.DebtorCode");
                            Dictionary<string, string> outstandingList = new Dictionary<string, string>();

                            for (int i = 0; i < outstandingFromDb.Count; i++)
                            {
                                Dictionary<string, string> each = (Dictionary<string, string>)outstandingFromDb[i];
                                if(!outstandingList.ContainsKey(each["DebtorCode"]))
                                {
                                    outstandingList.Add(each["DebtorCode"], each["Outstanding"]);
                                }
                            }
                            outstandingFromDb.Clear();

                            ArrayList queryResult = mssql.Select(database.Query);
                            //Console.WriteLine(database.Query);

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

                                    if (find_mssql_field == "date")
                                    {
                                        string createdDate = string.Empty;
                                        DateTime date = DateTime.Now;
                                        createdDate = date.ToString("s");

                                        Database.Sanitize(ref createdDate);

                                        row += inIdx == 0 ? "('" + createdDate + "" : "','" + createdDate;

                                        NoMssqlField = false;
                                        addedToRow = true;
                                    }

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
                                    
                                    if (corr_mysql_field == "cust_code")
                                    {
                                        string AccNo = string.Empty;

                                        map.Iterate<KeyValuePair<string, string>>((mssql_fields, mssqIndx) =>
                                        {
                                            if (mssql_fields.Key == "AccNo")
                                            {
                                                AccNo = mssql_fields.Value;

                                                mssql.Message("Acc No: " + AccNo);
                                                if (activeCustCode.Contains(AccNo))
                                                {
                                                    int indexx = activeCustCode.IndexOf(AccNo);
                                                    if (indexx != -1)
                                                    {
                                                        mysql.Message("Remove Cust Code ===> " + AccNo);
                                                        activeCustCode.RemoveAt(indexx);
                                                        mysql.Message("After remove activeCustCode ======>" + activeCustCode.Count);
                                                    }
                                                }

                                            }
                                        });

                                        row += inIdx == 0 ? "('" + AccNo + "" : "','" + AccNo;

                                        NoMssqlField = false;
                                        addedToRow = true;
                                    }

                                    if (find_mssql_field == "balance_amt")
                                    {
                                        string outstanding_amt = string.Empty;

                                        map.Iterate<KeyValuePair<string, string>>((mssql_fields, mssqIndx) =>
                                        {
                                            if (mssql_fields.Key == "AccNo")
                                            {
                                                string accNo = mssql_fields.Value;

                                                outstanding_amt = string.Empty;

                                                if (string.IsNullOrEmpty(accNo) || !outstandingList.TryGetValue(accNo, out outstanding_amt))
                                                {
                                                    outstanding_amt = "0";
                                                }
                                            }
                                        });

                                        row += inIdx == 0 ? "('" + outstanding_amt + "" : "','" + outstanding_amt;

                                        NoMssqlField = false;
                                        addedToRow = true;
                                    }

                                    if (find_mssql_field == "longitude")
                                    {
                                        string longitude = string.Empty;

                                        row += inIdx == 0 ? "('" + longitude + "" : "','" + longitude;

                                        NoMssqlField = false;
                                        addedToRow = true;
                                    }
                                    
                                    if (find_mssql_field == "latitude")
                                    {
                                        string latitude = string.Empty;

                                        row += inIdx == 0 ? "('" + latitude + "" : "','" + latitude;

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
                                    mysql.Message(insertQuery);

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
                                mysql.Message(insertQuery);

                                insertQuery = insertQuery.ReplaceAll("@values", values);
                                valueString.Clear();

                                logger.message = string.Format("{0} customer records is inserted into " + mysqlconfig.config_database, RecordCount);
                                logger.Broadcast();
                            }

                            if(activeCustCode.Count > 0)
                            {
                                //for blue martin it keeps deactivating those cust code which returned from mssql -.-
                                logger.Broadcast("Total customer records to be deactivated: " + activeCustCode.Count);

                                HashSet<string> deactivateId = new HashSet<string>();
                                for (int i = 0; i < activeCustCode.Count; i++)
                                {
                                    string _id = activeCustCode[i].ToString();
                                    if(deactivateId.Contains(_id))
                                    {
                                        deactivateId.Add(_id);
                                    }
                                }

                                string ToBeDeactivate = "'" + string.Join("','", deactivateId) + "'";
                                mysql.Message("DEACTIVATE CUSTOMER: " + ToBeDeactivate);

                                string inactive = "UPDATE cms_customer SET customer_status = 0 WHERE cust_code IN (" + ToBeDeactivate + ")";
                                mysql.Insert(inactive);
                                mysql.Message("DEACTIVATE CUSTOMER QUERY ======> " + inactive);

                                logger.Broadcast(activeCustCode.Count + " customer records deactivated");
                                activeCustCode.Clear();
                            }

                            RecordCount = 0; /* reset count for the next db */
                            mysqlFieldList.Clear();
                            queryResult.Clear();
                        });
                        mssql_rule.Clear();

                        if (cms_updated_time.Count > 0)
                        {
                            mysql.Insert("UPDATE cms_update_time SET updated_at = NOW() WHERE table_name = 'cms_customer'");
                        }
                        else
                        {
                            mysql.Insert("INSERT INTO cms_update_time(table_name, updated_at) VALUES('cms_customer', NOW())");
                        }
                        cms_updated_time.Clear();
                    });

                    mysql_list.Clear();
                    mssql_list.Clear();

                    slog.action_identifier = Constants.Action_ATCCustomerSync;
                    slog.action_details = Constants.Tbl_cms_customer + Constants.Is_Finished + string.Format(" ({0}) records", RecordCount);
                    slog.action_failure = 0;
                    slog.action_failure_message = string.Empty;
                    slog.action_time = DateTime.Now.ToString();


                    DateTime endTime = DateTime.Now;
                    TimeSpan timespan = endTime - startTime;
                    //Console.WriteLine("Completed in: " + timespan.TotalSeconds.ToString("F"));

                    LocalDB.InsertSyncLog(slog);

                    logger.message = "ATC customer sync finished in (" + timespan.TotalSeconds.ToString("F") + " seconds)";
                    logger.Broadcast();

                });

                thread.Start();
                //thread.Join();

            }
            catch (ThreadAbortException e)
            {
                DpprException ex = new DpprException
                {
                    file_name = "JobATCCustomerSync",
                    exception = e.Message,
                    time = DateTime.Now.ToString()
                };
                LocalDB.InsertException(ex);

                Console.WriteLine(Constants.Thread_Exception + e.Message);
            }
        }
    }
}
