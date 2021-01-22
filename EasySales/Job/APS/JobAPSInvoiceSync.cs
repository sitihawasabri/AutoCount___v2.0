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
    class JobAPSInvoiceSync : IJob
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
                    slog.action_identifier = Constants.Action_APSInvoiceSync;
                    slog.action_details = Constants.Tbl_cms_invoice + Constants.Is_Starting;
                    slog.action_failure = 0;
                    slog.action_failure_message = string.Empty;
                    slog.action_time = DateTime.Now.ToString();

                    DateTime startTime = DateTime.Now;

                    LocalDB.InsertSyncLog(slog);

                    /* force and united sync from HQ, ulsan and zone from own database */
                    logger.message = "APS invoice sync is running";
                    logger.Broadcast();

                    //invoiceCode = obj["varTrxNo"]; invoice_code
                    //invoiceDate = obj["dtIVDate"]; invoice_date
                    //invoiceDate = Convert.ToDateTime(invoiceDate).ToString("yyyy-MM-dd HH:mm:ss"); //check
                    //custCode = obj["charRef"]; cust_code
                    //invoiceAmount = obj["decIVAmt"]; invoice_amount
                    //outstandingAmount = obj["decIVAmt"]; outstanding_amount

                    //if (obj["blnIsDelete"] == "FALSE") cancelled
                    //{
                    //    cancelled = "F";
                    //}
                    //else
                    //{
                    //    cancelled = "T";
                    //}

                    //query = "INSERT INTO cms_invoice(invoice_code, cust_code, invoice_date, invoice_amount, outstanding_amount, cancelled) VALUES ";
                    //updateQuery = " ON DUPLICATE KEY UPDATE cancelled = VALUES(cancelled),invoice_amount = VALUES(invoice_amount), outstanding_amount = VALUES(outstanding_amount)";

                    //{"mysql":"invoice_code", "mssql":"varTrxNo"},{"mysql":"invoice_date", "mssql":"dtIVDate"},{"mysql":"cust_code", "mssql":"charRef"},{"mysql":"invoice_amount", "mssql":"decIVAmt"},{"mysql":"outstanding_amount", "mssql":"decIVAmt"},{"mysql":"cancelled", "mssql":"blnIsDelete"}

                    List<DpprMySQLconfig> mysql_list = LocalDB.GetRemoteDatabaseConfig().Distinct().ToList();
                    List<DpprSQLServerconfig> mssql_list = LocalDB.GetRemoteSQLServerConfig();

                    mysql_list.Iterate<DpprMySQLconfig>((mysql_config, index) =>
                    {
                        DpprMySQLconfig mysqlconfig = mysql_list[index];
                        Database mysql = new Database();
                        mysql.Connect(index: index);

                        CheckBackendRule checkDB = new CheckBackendRule(mysql: mysql);
                        dynamic jsonRule = checkDB.CheckTablesExist().GetSettingByTableName("cms_invoice");

                        Dictionary<string, string> cms_updated_time = mysql.GetUpdatedTime("cms_invoice"); //{[updated_at, 26/07/2020 12:27:50 PM]}

                        ArrayList mssql_rule = new ArrayList();

                        if (jsonRule != null)
                        {
                            foreach (var key in jsonRule)
                            {
                                if (key.mssql != null && key.mssql.GetType().ToString() == "Newtonsoft.Json.Linq.JArray")
                                {
                                    //string query = "SELECT varTrxNo , cust.charRef , iv.dtIVDate , iv.decIVAmt ,  iv.decIVAmt - isnull(decCNOffSetAmt,0) - isnull(decCollOffsetAmt,0) - isnull(decContraOffSetAmt,0) as decIVOSAmt, iv.blnIsDelete from vwActiveSal_InvoiceTbl iv inner join Sal_CustomerTbl cust on iv.intCustID = cust.intCustID left outer join vwSal_CNOffSetAmtForInvoiceGrouping cn on iv.intIVNo = cn.intIVNo left outer join vwSal_CollectionOffSetAmtForInvoiceGrouping col on iv.intIVNo = col.intIVNo left outer join vwSal_ContraOffSetAmtForInvoiceGrouping con on iv.intIVNo = con.intIVNo where iv.dtCreatedDate >= cast('@dateFrom' as Date) and iv.dtCreatedDate <= cast('@dateTo' as Date) order by varTrxNo desc";
                                    //string query = "SELECT varTrxNo , cust.charRef , iv.dtIVDate , iv.decAfterCalcAmt ,  iv.decAfterCalcAmt - isnull(decCNOffSetAmt,0) - isnull(decCollOffsetAmt,0) - isnull(decContraOffSetAmt,0) as decIVOSAmt, iv.blnIsDelete from vwActiveSal_InvoiceTbl iv inner join Sal_CustomerTbl cust on iv.intCustID = cust.intCustID and cust.blnisdelete = 'False' left outer join vwSal_CNOffSetAmtForInvoiceGrouping cn on iv.intIVNo = cn.intIVNo left outer join vwSal_CollectionOffSetAmtForInvoiceGrouping col on iv.intIVNo = col.intIVNo left outer join vwSal_ContraOffSetAmtForInvoiceGrouping con on iv.intIVNo = con.intIVNo where iv.dtModifyDate >= cast('@dateFrom' as Date) and iv.dtCreatedDate <= cast('@dateTo' as Date) order by varTrxNo ASC";
                                    string query = "SELECT varTrxNo , cust.charRef , iv.dtIVDate , iv.decAfterCalcAmt ,  iv.decAfterCalcAmt - isnull(decCNOffSetAmt,0) - isnull(decCollOffsetAmt,0) - isnull(decContraOffSetAmt,0) as decIVOSAmt, iv.blnIsDelete from vwActiveSal_InvoiceTbl iv inner join Sal_CustomerTbl cust on iv.intCustID = cust.intCustID and cust.blnisdelete = 'False' left outer join vwSal_CNOffSetAmtForInvoiceGrouping cn on iv.intIVNo = cn.intIVNo left outer join vwSal_CollectionOffSetAmtForInvoiceGrouping col on iv.intIVNo = col.intIVNo left outer join vwSal_ContraOffSetAmtForInvoiceGrouping con on iv.intIVNo = con.intIVNo @dateQuery order by varTrxNo ASC";
                                    string date_query = " where iv.dtIVDate >= cast('@dateFrom' as Date) and iv.dtIVDate <= cast('@dateTo' as Date) ";

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
                                        Console.WriteLine(query);
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
                            throw new Exception("APS Invoice sync requires backend rules");
                        }

                        mssql_rule.Iterate<APSRule>((database, idx) =>
                        {
                            SQLServer mssql = new SQLServer();
                            mssql.Connect(dbname: database.DBname);

                            Console.WriteLine(database.Query);

                            ArrayList queryResult = mssql.Select(database.Query);
                            mssql.Message("Invoice Query [" + database.DBname + "] ---> " + database.Query);

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

                                    if (find_mssql_field == "dtIVDate")
                                    {
                                        RecordCount++;
                                        string invDate = string.Empty;

                                        map.Iterate<KeyValuePair<string, string>>((mssql_fields, mssqIndx) =>
                                        {
                                            if (mssql_fields.Key == "dtIVDate")
                                            {
                                                invDate = mssql_fields.Value;
                                                invDate = Convert.ToDateTime(invDate).ToString("yyyy-MM-dd HH:mm:ss");
                                            }
                                        });

                                        row += inIdx == 0 ? "('" + invDate + "" : "','" + invDate;

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
                                            //logger.Broadcast(mssql_fields.Key + "----------" + tmp);

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
                                Console.WriteLine(row);

                                valueString.Add(row);

                                if (valueString.Count % 2000 == 0)
                                {
                                    string values = valueString.Join(",");

                                    insertQuery = insertQuery.ReplaceAll(values, "@values");

                                    mysql.Insert(insertQuery);

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

                                insertQuery = insertQuery.ReplaceAll("@values", values);
                                valueString.Clear();

                                logger.message = string.Format("{0} invoice records is inserted into " + mysqlconfig.config_database, RecordCount);
                                logger.Broadcast();
                            }

                            if (cms_updated_time.Count > 0)
                            {
                                mysql.Insert("UPDATE cms_update_time SET updated_at = NOW() WHERE table_name = 'cms_invoice'");
                            }
                            else
                            {
                                mysql.Insert("INSERT INTO cms_update_time(table_name, updated_at) VALUES('cms_invoice', NOW())");
                            }

                            RecordCount = 0; /* reset count */
                           
                        });
                    });

                    slog.action_identifier = Constants.Action_APSInvoiceSync;
                    slog.action_details = Constants.Tbl_cms_invoice + Constants.Is_Finished + string.Format(" ({0}) records", RecordCount);
                    slog.action_failure = 0;
                    slog.action_failure_message = string.Empty;
                    slog.action_time = DateTime.Now.ToString();

                    DateTime endTime = DateTime.Now;
                    TimeSpan ts = endTime - startTime;
                    Console.WriteLine("Completed in: " + ts.TotalSeconds.ToString("F"));

                    LocalDB.InsertSyncLog(slog);
                    logger.message = "APS invoice sync finished in (" + ts.TotalSeconds.ToString("F") + " seconds)";
                    logger.Broadcast();

                });

                thread.Start();
                //thread.Join();

            }
            catch (ThreadAbortException e)
            {
                DpprException ex = new DpprException
                {
                    file_name = "JobAPSInvoiceSync",
                    exception = e.Message,
                    time = DateTime.Now.ToString()
                };
                LocalDB.InsertException(ex);

                Console.WriteLine(Constants.Thread_Exception + e.Message);
            }
        }
    }
}