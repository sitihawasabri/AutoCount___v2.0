using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using AutoCount.Stock.StockStatus;
using EasySales.Model;
using EasySales.Object;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Quartz;
using RestSharp;
using System.Text;
using System.Data;
using System.Globalization;
using AutoCount.Invoicing.Sales.SalesOrder;
using AutoCount.Invoicing.Sales;

namespace EasySales.Job
{
    [DisallowConcurrentExecution]
    public class JobATCOutstandingSO : IJob
    {
        private ATC_Connection connection = null;
        private System.Data.DataTable resultTable;

        public DataSet GetAllOutstandingSO()
        {
            try
            {
                SalesOrderOutstandingDetailReportCommand cmd = SalesOrderOutstandingDetailReportCommand.Create(this.connection.userSession);

                if (cmd != null)
                {
                    DataTable dtMaster = new DataTable("Master");

                    DataSet dsOutstandingSO = new DataSet();

                    dsOutstandingSO.Tables.Add(dtMaster);

                    SalesOrderOutStandingReportingCriteria crit = new SalesOrderOutStandingReportingCriteria();
                    crit.FromDate = new DateTime(2019, 1, 1);
                    crit.ToDate = DateTime.Today.Date;

                    cmd.BasicSearch(crit, "DocNo,DocKey,DocDate,DebtorCode,DebtorName,SmallestQty,TransferedQty,RemainingSmallestQty,ItemCode,SalesAgent", dsOutstandingSO, "");
                    return dsOutstandingSO;
                }

                return null;
            }
            catch (AutoCount.AppException ex)
            {
                return null;
            }

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
                    slog.action_identifier = Constants.Action_ATCOutSOSync;
                    slog.action_details = Constants.Tbl_cms_outstanding + Constants.Is_Starting;
                    slog.action_failure = 0;
                    slog.action_failure_message = string.Empty;
                    slog.action_time = DateTime.Now.ToString();

                    DateTime startTime = DateTime.Now;

                    LocalDB.InsertSyncLog(slog);

                    logger.message = "ATC oustanding SO sync is running";
                    logger.Broadcast();

                    List<DpprMySQLconfig> mysql_list = LocalDB.GetRemoteDatabaseConfig().Distinct().ToList();
                    List<DpprSQLServerconfig> mssql_list = LocalDB.GetRemoteSQLServerConfig();

                    mysql_list.Iterate<DpprMySQLconfig>((mysql_config, index) =>
                    {
                        DpprMySQLconfig mysqlconfig = mysql_list[index];
                        Database mysql = new Database();
                        mysql.Connect(index: index);

                        CheckBackendRule checkDB = new CheckBackendRule(mysql: mysql);
                        dynamic jsonRule = checkDB.CheckTablesExist().GetSettingByTableName("cms_outstanding_so_atc");

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
                                            Exclude = exclude
                                        };

                                        mssql_rule.Add(ATC_rule);
                                    }
                                }
                            }
                        }
                        else
                        {
                            throw new Exception("ATC Oustanding SO sync requires backend rules");
                        }

                        mssql_rule.Iterate<ATCRule>((database, idx) =>
                        {
                            SQLServer mssql = new SQLServer();
                            mssql.Connect(dbname: database.DBname);

                            string mysql_insert = string.Empty;
                            string mssql_insert = string.Empty;

                            string insertQuery = "INSERT INTO cms_outstanding_so (@columns) VALUES @values ON DUPLICATE KEY UPDATE @update_columns";
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

                            this.connection = ATC_Configuration.Init_config();
                            this.connection.dBSetting = AutoCountV1.PerformAuth(ref this.connection);

                            if (AutoCountV1.PerformAuthInAutoCount(this.connection))
                            {
                                logger.Broadcast("Trying to read outstanding SO from AutoCount");

                                ArrayList saleagents = mysql.Select("SELECT staff_code, login_id FROM cms_login");

                                HashSet<string> queryList = new HashSet<string>();
                                Dictionary<string, string> valueList = new Dictionary<string, string>();

                                DataSet dataSet = GetAllOutstandingSO();
                                logger.Broadcast("Getting dataSet");

                                if (dataSet != null)
                                {
                                    this.resultTable = dataSet.Tables[0];

                                    foreach (DataRow row in resultTable.Rows)
                                    {
                                        string agent_id = string.Empty;

                                        Thread.CurrentThread.CurrentCulture = new CultureInfo("en-GB");

                                        string date = DateTime.ParseExact(row["DocDate"].ToString(), "dd/MM/yyyy HH:mm:ss",
                                                           System.Globalization.CultureInfo.InvariantCulture).ToString("yyyy-MM-dd H:mm:ss");

                                        string agent = row["SalesAgent"].ToString();

                                        foreach (Dictionary<string, string> map in saleagents)
                                        {
                                            if (agent == map["staff_code"])
                                            {
                                                agent_id = map["login_id"];
                                            }
                                        }

                                        string valueQuery = string.Empty;
                                        valueList.Add("DocNo", row["DocNo"].ToString());
                                        valueList.Add("DocKey", row["DocKey"].ToString());
                                        valueList.Add("ItemCode", row["ItemCode"].ToString());
                                        valueList.Add("SmallestQty", row["SmallestQty"].ToString());
                                        valueList.Add("TransferedQty", row["TransferedQty"].ToString());
                                        valueList.Add("date", date);
                                        valueList.Add("agent_id", agent_id);
                                        valueList.Add("DebtorCode", row["DebtorCode"].ToString());
                                        valueList.Add("RemainingSmallestQty", row["RemainingSmallestQty"].ToString());

                                        database.Include.Iterate<Dictionary<string, string>>((include, inIdx) =>
                                        {
                                            string fieldname = include["mssql"];

                                            valueList.Iterate<KeyValuePair<string, string>>((field, ixx) =>
                                            {
                                                if (field.Key == fieldname)
                                                {
                                                    string value = string.Empty;
                                                    value = field.Value;
                                                    valueQuery += ixx == 0 ? "('" + value + "" : "','" + value;
                                                }
                                            });

                                        });
                                        valueQuery += "')";
                                        //Console.WriteLine(valueQuery);

                                        queryList.Add(valueQuery);
                                        valueList.Clear();
                                    }

                                    string values = queryList.Join(",");
                                    insertQuery = insertQuery.ReplaceAll(values, "@values");
                                    bool isUpdated = mysql.Insert(insertQuery);
                                    mysql.Message("outstandingSO query" + insertQuery);

                                    if (isUpdated)
                                    {
                                        logger.message = string.Format("{0} oustanding so records is inserted", queryList.Count);
                                        logger.Broadcast();
                                    }
                                    else
                                    {
                                        logger.message = string.Format("{0} oustanding so records is inserted", queryList.Count);
                                        logger.Broadcast();
                                    }
                                }
                            }
                        });
                    });

                    mysql_list.Clear();
                    mssql_list.Clear();

                    slog.action_identifier = Constants.Action_ATCOutSOSync;
                    slog.action_details = Constants.Tbl_cms_outstanding + Constants.Is_Finished + string.Format(" ({0}) records", RecordCount);
                    slog.action_failure = 0;
                    slog.action_failure_message = string.Empty;
                    slog.action_time = DateTime.Now.ToString();


                    DateTime endTime = DateTime.Now;
                    TimeSpan timespan = endTime - startTime;
                    //Console.WriteLine("Completed in: " + timespan.TotalSeconds.ToString("F"));

                    LocalDB.InsertSyncLog(slog);

                    logger.message = "ATC outstanding SO sync finished in (" + timespan.TotalSeconds.ToString("F") + " seconds)";
                    logger.Broadcast();

                });

                thread.Start();
            }
            catch (ThreadAbortException e)
            {
                DpprException ex = new DpprException
                {
                    file_name = "JobATCReadyStockSync",
                    exception = e.Message,
                    time = DateTime.Now.ToString()
                };
                LocalDB.InsertException(ex);

                Console.WriteLine(Constants.Thread_Exception + e.Message);
            }
        }
    }
}