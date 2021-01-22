using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EasySales.Model;
using EasySales.Object;
using Quartz;
using Quartz.Impl;
using Quartz.Logging;

namespace EasySales.Job
{
    [DisallowConcurrentExecution]
    public class JobProductCategorySync : IJob
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
                    slog.action_identifier = Constants.Action_ProductCategorySync;
                    slog.action_details = Constants.Tbl_cms_product_category + Constants.Is_Starting;
                    slog.action_failure = 0;
                    slog.action_failure_message = string.Empty;
                    slog.action_time = DateTime.Now.ToString();

                    DateTime startTime = DateTime.Now;

                    LocalDB.InsertSyncLog(slog);
                    logger.message = "----------------------------------------------------------------------------";
                    logger.Broadcast();
                    logger.message = "Product category sync is running";
                    logger.Broadcast();
                    logger.message = "----------------------------------------------------------------------------";
                    logger.Broadcast();

                /**
                * Here we will run SQLAccounting Codes
                * */

                CHECKAGAIN:
                    SQLAccApi instance = SQLAccApi.getInstance();

                    dynamic ComServer = instance.GetComServer();

                    if (!instance.RPCAvailable())
                    {
                        goto CHECKAGAIN;
                    }

                    Database mysql = new Database();

                    dynamic jsonRule = new CheckBackendRule()
                                            .CheckTablesExist()
                                            .GetSettingByTableName("cms_product_category");

                    ArrayList exCode = new ArrayList();
                    ArrayList inCode = new ArrayList();

                    ArrayList inDBactiveProductCat = mysql.Select("SELECT categoryIdentifierId FROM cms_product_category WHERE category_status = 1;");

                    ArrayList inDBproductCat = new ArrayList();
                    for (int i = 0; i < inDBactiveProductCat.Count; i++)
                    {
                        Dictionary<string, string> each = (Dictionary<string, string>)inDBactiveProductCat[i];
                        inDBproductCat.Add(each["categoryIdentifierId"].ToString());
                    }
                    inDBactiveProductCat.Clear();

                    logger.Broadcast("Active Product Categories in DB (inDBproducts): " + inDBproductCat.Count);

                    string name = string.Empty;

                    if(jsonRule != null)
                    {
                        foreach (var rule in jsonRule)
                        {
                            dynamic _name = rule.name;
                            name = _name;

                            dynamic _excludeCode = rule.exclude_code;

                            foreach (string value in _excludeCode)
                            {
                                exCode.Add(value);
                            }
                            
                            dynamic _includeCode = rule.include_code;

                            foreach (string value in _includeCode)
                            {
                                inCode.Add(value);
                            }
                        }
                    }

                    if (name == "category")
                    {
                        dynamic lDataSet;
                        string lSQL, query, updateQuery;
                        string Code, Description, ActiveStatus;

                        query = "INSERT INTO cms_product_category(categoryIdentifierId, category_name, sequence_no, category_status) VALUES ";
                        updateQuery = " ON DUPLICATE KEY UPDATE category_name = VALUES(category_name),category_status = VALUES(category_status);";

                        HashSet<string> queryList = new HashSet<string>();

                        lSQL = "SELECT * FROM ST_CATEGORY";

                        try
                        {
                            lDataSet = ComServer.DBManager.NewDataSet(lSQL);

                            lDataSet.First();

                            while (!lDataSet.eof)
                            {
                                RecordCount++;
                                ActiveStatus = "1";

                                Code = lDataSet.FindField("CODE").AsString;
                                if (inDBproductCat.Count > 0)
                                {
                                    if (inDBproductCat.Contains(Code))
                                    {
                                        int index = inDBproductCat.IndexOf(Code);
                                        if (index != -1)
                                        {
                                            inDBproductCat.RemoveAt(index);
                                        }
                                    }
                                }

                                Description = lDataSet.FindField("DESCRIPTION").AsString;

                                if (exCode.Count == 0) //if exclude is empty, get from include array
                                {
                                    if (inCode.Count > 0)
                                    {
                                        if (!inCode.Contains(Code))
                                        {
                                            ActiveStatus = "0";
                                        }
                                    }
                                }
                                else
                                {
                                    if (exCode.Contains(Code))
                                    {
                                        ActiveStatus = "0";
                                    }
                                }

                                Database.Sanitize(ref Code);
                                Database.Sanitize(ref Description);

                                string Values = string.Format("('{0}','{1}','{2}','{3}')", Code, Description, RecordCount, ActiveStatus);

                                queryList.Add(Values);

                                if (queryList.Count % 2000 == 0)
                                {
                                    string tmp_query = query;
                                    tmp_query += string.Join(", ", queryList);
                                    tmp_query += updateQuery;

                                    mysql.Insert(tmp_query);

                                    queryList.Clear();
                                    tmp_query = string.Empty;

                                    logger.message = string.Format("{0} product category records is inserted", RecordCount);
                                    logger.Broadcast();
                                }

                                lDataSet.Next();
                            }

                            if (queryList.Count > 0)
                            {
                                query = query + string.Join(", ", queryList) + updateQuery;

                                mysql.Insert(query);

                                logger.message = string.Format("{0} product category records is inserted", RecordCount);
                                logger.Broadcast();
                                queryList.Clear();
                            }

                            if (inDBproductCat.Count > 0) /*inactivate products which no longer in SQLAcc */
                            {
                                string inactive = "INSERT INTO cms_product_category (categoryIdentifierId, category_status) VALUES ";
                                string inactive_duplicate = "ON DUPLICATE KEY UPDATE category_status=VALUES(category_status);";
                                for (int i = 0; i < inDBproductCat.Count; i++)
                                {
                                    string _code = inDBproductCat[i].ToString();
                                    Database.Sanitize(ref _code);
                                    string _query = string.Format("('{0}',0)", _code);
                                    mysql.Insert(inactive + _query + inactive_duplicate);
                                }

                                logger.Broadcast(inDBproductCat.Count + " product category deactivated");
                                inDBproductCat.Clear();
                            }
                        }
                        catch
                        {
                            try
                            {
                                goto CHECKAGAIN;
                            }
                            catch (Exception exc)
                            {
                                DpprException exception = new DpprException()
                                {
                                    file_name = "SQLAccounting + JobProductCatSync",
                                    exception = exc.Message,
                                    time = DateTime.Now.ToString()
                                };
                                LocalDB.InsertException(exception);
                            }
                        }
                    }

                    if(name == "group")
                    {
                        dynamic lRptVar, lMain;
                        string Code, Description, Active;
                        string query, updateQuery;

                        HashSet<string> queryList = new HashSet<string>();

                        try
                        {
                            lRptVar = ComServer.RptObjects.Find("Stock.GROUP.RO");
                            lRptVar.CalculateReport();

                            lMain = lRptVar.DataSets.Find("cdsMain");

                            query = "INSERT INTO cms_product_category(categoryIdentifierId, category_name, sequence_no, category_status) VALUES ";
                            updateQuery = " ON DUPLICATE KEY UPDATE category_name = VALUES(category_name),category_status = VALUES(category_status);";

                            lMain.DisableControls();
                            lMain.First();

                            while (!lMain.eof)
                            {
                                RecordCount++;

                                Code = lMain.FindField("CODE").AsString;
                                if (inDBproductCat.Count > 0)
                                {
                                    if (inDBproductCat.Contains(Code))
                                    {
                                        int index = inDBproductCat.IndexOf(Code);
                                        if (index != -1)
                                        {
                                            inDBproductCat.RemoveAt(index);
                                        }
                                    }
                                }

                                Description = lMain.FindField("DESCRIPTION").AsString;
                                Active = lMain.FindField("ISACTIVE").AsString;

                                int activeValue = 1;

                                if (Active == "F")
                                {
                                    activeValue = 0;
                                }

                                if (exCode.Count == 0) //if exclude is empty, get from include array
                                {
                                    if (inCode.Count > 0)
                                    {
                                        if (!inCode.Contains(Code))
                                        {
                                            activeValue = 0;
                                        }
                                    }
                                }
                                else
                                {
                                    if (exCode.Contains(Code))
                                    {
                                        activeValue = 0;
                                    }
                                }

                                Database.Sanitize(ref Code);
                                Database.Sanitize(ref Description);

                                string Values = string.Format("('{0}','{1}','{2}','{3}')", Code, Description, RecordCount, activeValue);

                                queryList.Add(Values);

                                if (queryList.Count % 2000 == 0)
                                {
                                    string tmp_query = query;
                                    tmp_query += string.Join(", ", queryList);
                                    tmp_query += updateQuery;

                                    mysql.Insert(tmp_query);

                                    queryList.Clear();
                                    tmp_query = string.Empty;

                                    logger.message = string.Format("{0} product category records is inserted", RecordCount);
                                    logger.Broadcast();
                                }

                                lMain.Next();
                            }

                            if (queryList.Count > 0)
                            {
                                query = query + string.Join(", ", queryList) + updateQuery;

                                mysql.Insert(query);

                                logger.message = string.Format("{0} product category records is inserted", RecordCount);
                                logger.Broadcast();
                            }

                            /*inactivate product category which no longer in SQLAcc */
                            if (inDBproductCat.Count > 0)
                            {
                                string inactive = "INSERT INTO cms_product_category (categoryIdentifierId, category_status) VALUES ";
                                string inactive_duplicate = "ON DUPLICATE KEY UPDATE category_status=VALUES(category_status);";
                                for (int i = 0; i < inDBproductCat.Count; i++)
                                {
                                    string _code = inDBproductCat[i].ToString();
                                    Database.Sanitize(ref _code);
                                    string _query = string.Format("('{0}',0)", _code);
                                    mysql.Insert(inactive + _query + inactive_duplicate);
                                }

                                logger.Broadcast(inDBproductCat.Count + " product category deactivated");
                                inDBproductCat.Clear();
                            }
                        }
                        catch
                        {
                            try
                            {
                                goto CHECKAGAIN;
                            }
                            catch (Exception exc)
                            {
                                DpprException exception = new DpprException()
                                {
                                    file_name = "SQLAccounting + JobProductCatSync",
                                    exception = exc.Message,
                                    time = DateTime.Now.ToString()
                                };
                                LocalDB.InsertException(exception);
                            }
                        }
                    }

                    slog.action_identifier = Constants.Action_ProductCategorySync;
                    slog.action_details = Constants.Tbl_cms_product_category + Constants.Is_Finished + string.Format(" ({0}) records", RecordCount);
                    slog.action_failure = 0;
                    slog.action_failure_message = string.Empty;
                    slog.action_time = DateTime.Now.ToString();

                    DateTime endTime = DateTime.Now;
                    TimeSpan ts = endTime - startTime;
                    Console.WriteLine("Completed in: " + ts.TotalSeconds.ToString("F"));

                    LocalDB.InsertSyncLog(slog);

                    logger.message = "Product category sync finished in (" + ts.TotalSeconds.ToString("F") + " seconds)";
                    logger.Broadcast();
                });

                thread.Start();
                //thread.Join();

                //await Console.Out.WriteLineAsync("Product Category Sync");
            }
            catch (ThreadAbortException e)
            {
                DpprException ex = new DpprException
                {
                    file_name = "JobProductCategorySync",
                    exception = e.Message,
                    time = DateTime.Now.ToString()
                };
                LocalDB.InsertException(ex);

                Console.WriteLine(Constants.Thread_Exception + e.Message);
            }
        }
    }
}