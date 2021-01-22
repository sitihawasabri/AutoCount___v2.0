using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EasySales.Model;
using EasySales.Object;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Internal;
using Quartz;
using Quartz.Impl;
using Quartz.Logging;
using RestSharp;

namespace EasySales.Job
{
    [DisallowConcurrentExecution]
    public class JobStockUomPriceSync : IJob
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

                    GlobalLogger logger = new GlobalLogger();

                    DpprSyncLog slog = new DpprSyncLog();
                    slog.action_identifier = Constants.Action_StockUomPriceSync;
                    slog.action_details = Constants.Tbl_cms_product_uom_price_v2 + Constants.Is_Starting;
                    slog.action_failure = 0;
                    slog.action_failure_message = string.Empty;
                    slog.action_time = DateTime.Now.ToString();
                    DateTime startTime = DateTime.Now;
                    LocalDB.InsertSyncLog(slog);

                    logger.message = "Stock UOM price sync is running";
                    logger.Broadcast();

                    QNEApi api = new QNEApi();
                    int RecordCount = 0;
                    int stdPrice = 0; /* default product_std_price, get from listPrice */
                    dynamic jsonRule = new CheckBackendRule().CheckTablesExist().GetSettingByTableName("cms_product_uom_price_v2");

                    if (jsonRule.Count > 0)
                    {
                        foreach (var key in jsonRule)
                        {
                            dynamic _std_price = key.std_price;

                            if (_std_price != 0)
                            {
                                stdPrice = _std_price;
                            }
                        }
                    }
                    else
                    {
                        goto ENDJOB;
                    }

                    dynamic json = api.GetByName("Stocks");
                    api.Message("Stock UOM price sync is running");
                    HashSet<string> queryList = new HashSet<string>();

                    Database mysql = new Database();

                    ArrayList inDBactiveProductsUOM = mysql.Select("SELECT product_uom_price_id, product_code, product_uom, product_uom_rate FROM cms_product_uom_price_v2 WHERE active_status = 1;");

                    Dictionary<string, string> uniqueDictUOM = new Dictionary<string, string>();
                    for (int i = 1; i < inDBactiveProductsUOM.Count; i++)
                    {
                        Dictionary<string, string> each = (Dictionary<string, string>)inDBactiveProductsUOM[i];
                        string id = each["product_uom_price_id"];
                        string code = each["product_code"];
                        string uom = each["product_uom"];
                        string uom_rate = each["product_uom_rate"];
                        string unique = code + uom + uom_rate;

                        uniqueDictUOM.Add(id, unique);
                    }
                    inDBactiveProductsUOM.Clear();
                    logger.Broadcast("Active UOM price in DB: " + uniqueDictUOM.Count);

                    string query = "INSERT INTO cms_product_uom_price_v2(product_code, product_uom, product_uom_rate, product_std_price, product_min_price, product_default_price, active_status, updated_at) VALUES ";
                    string updateQuery = " ON DUPLICATE KEY UPDATE product_code = VALUES(product_code), product_uom = VALUES(product_uom), product_uom_rate = VALUES(product_uom_rate), product_std_price = VALUES(product_std_price), product_min_price = VALUES(product_min_price), product_default_price=VALUES(product_default_price), active_status = VALUES(active_status), updated_at = VALUES(updated_at);";

                    try
                    {
                        foreach (var item in json)
                        {
                            RecordCount++;

                            int activeValue = 0;
                            int uomRate = 1;
                            int defaultPrice = 1;
                            dynamic productStdPrice = 0.00;

                            productStdPrice = item.listPrice; /* default product_std_price */

                            if (item.isActive == true)
                            {
                                activeValue = 1;
                            }

                            string p_code = item.stockCode, p_baseUOM = item.baseUOM;

                            if (jsonRule != null)
                            {
                                if (stdPrice == 1) /* GM */
                                {
                                    productStdPrice = item.minPrice;
                                }
                            }

                            string unique = p_code + p_baseUOM + uomRate;

                            if (uniqueDictUOM.Count > 0)
                            {
                                //deactivate those deleted uom
                                if (uniqueDictUOM.ContainsValue(unique))
                                {
                                    var key = uniqueDictUOM.Where(pair => pair.Value == unique)
                                                .Select(pair => pair.Key)
                                                .FirstOrDefault();
                                    if (key != null)
                                    {
                                        uniqueDictUOM.Remove(key);
                                    }
                                }
                            }

                            string updated_at = string.Empty;
                            DateTime date = DateTime.Now;
                            updated_at = date.ToString("s"); //2020-09-08 15:30:36

                            Database.Sanitize(ref updated_at);
                            Database.Sanitize(ref p_code);
                            Database.Sanitize(ref p_baseUOM);
                            

                            string Values = string.Format("('{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}')", p_code, p_baseUOM, uomRate, productStdPrice, item.minPrice, defaultPrice, activeValue, updated_at);

                            queryList.Add(Values);

                            if (queryList.Count % 2000 == 0) //change all the batch size and thead sleep also..remove them ok
                            {
                                string tmp_query = query;
                                tmp_query += string.Join(", ", queryList);
                                tmp_query += updateQuery;

                                mysql.Insert(tmp_query);

                                queryList.Clear();
                                tmp_query = string.Empty;

                                logger.message = string.Format("{0} stock UOM price records is inserted", RecordCount);
                                logger.Broadcast();
                            }
                        }

                        if (queryList.Count > 0)
                        {
                            query = query + string.Join(", ", queryList) + updateQuery;

                            mysql.Insert(query);

                            logger.message = string.Format("{0} stock UOM price records is inserted", RecordCount);
                            logger.Broadcast();

                            if (uniqueDictUOM.Count > 0)
                            {
                                logger.Broadcast("Total UOM to be deactivated: " + uniqueDictUOM.Count);

                                HashSet<string> deactivateId = new HashSet<string>();
                                for (int i = 0; i < uniqueDictUOM.Count; i++)
                                {
                                    string _id = uniqueDictUOM.ElementAt(i).Key;
                                    deactivateId.Add(_id);
                                }

                                string ToBeDeactivate = "'" + string.Join("','", deactivateId) + "'";
                                Console.WriteLine(ToBeDeactivate);

                                string inactive = "UPDATE cms_product_uom_price_v2 SET active_status = 0 WHERE product_uom_price_id IN (" + ToBeDeactivate + ")";
                                mysql.Insert(inactive);
                                mysql.Message("UOM Price deactivated query: " + inactive);

                                logger.Broadcast(uniqueDictUOM.Count + " UOM deactivated");
                                uniqueDictUOM.Clear();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        api.Message("QNEAPI + JobStockUOMPriceSync.cs ---> " + ex.Message);
                    }

                    ENDJOB:
                    api.Message("Stock UOM price sync finished");

                    slog.action_identifier = Constants.Action_StockUomPriceSync;
                    slog.action_details = Constants.Tbl_cms_product_uom_price_v2 + Constants.Is_Finished + string.Format(" ({0}) records", RecordCount);
                    slog.action_failure = 0;
                    slog.action_failure_message = string.Empty;
                    slog.action_time = DateTime.Now.ToString();

                    DateTime endTime = DateTime.Now;
                    TimeSpan ts = endTime - startTime;
                    Console.WriteLine("Completed in: " + ts.TotalSeconds.ToString("F"));

                    LocalDB.InsertSyncLog(slog);
                    logger.message = "Stock UOM price sync finished in (" + ts.TotalSeconds.ToString("F") + " seconds)";
                    logger.Broadcast();
                });

                thread.Start();
                //thread.Join();

                //await Console.Out.WriteLineAsync("Done Stock Sync");
            }
            catch (ThreadAbortException e)
            {
                DpprException ex = new DpprException
                {
                    file_name = "JobStockUomPriceSync",
                    exception = e.Message,
                    time = DateTime.Now.ToString()
                };
                LocalDB.InsertException(ex);

                Console.WriteLine(Constants.Thread_Exception + e.Message);
            }
        }
    }
}