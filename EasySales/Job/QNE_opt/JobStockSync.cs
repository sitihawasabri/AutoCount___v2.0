using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using EasySales.Model;
using EasySales.Object;
using Newtonsoft.Json;
using Quartz;
using Quartz.Impl;
using Quartz.Logging;
using RestSharp;

namespace EasySales.Job
{
    [DisallowConcurrentExecution]
    public class JobStockSync : IJob
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
                    slog.action_identifier = Constants.Action_StockSync;
                    slog.action_details = Constants.Tbl_cms_product + Constants.Is_Starting;
                    slog.action_failure = 0;
                    slog.action_failure_message = string.Empty;
                    slog.action_time = DateTime.Now.ToString();
                    DateTime startTime = DateTime.Now;
                    LocalDB.InsertSyncLog(slog);

                    logger.message = "Stock sync is running";
                    logger.Broadcast();

                    QNEApi api = new QNEApi();
                    int RecordCount = 0;
                    string whCode = string.Empty; /* HQ/JOHOR */
                    dynamic jsonRule = new CheckBackendRule().CheckTablesExist().GetSettingByTableName("cms_warehouse_stock");

                    if (jsonRule.Count > 0)
                    {
                        foreach (var key in jsonRule)
                        {
                            dynamic _whCode = key.wh_code;

                            if (_whCode != string.Empty)
                            {
                                whCode = _whCode;
                            }
                        }
                    }
                    else
                    {
                        goto ENDJOB;
                    }

                    dynamic json = api.GetByName("Stocks");
                    api.Message("Stock sync is running");
                    HashSet<string> queryList = new HashSet<string>();
                    HashSet<string> itemsToInactive = new HashSet<string>();

                    string query = "INSERT INTO cms_product(category_id, product_code, QR_code, product_name, product_desc, product_remark, sequence_no, product_status, product_available_quantity, product_cost_price) VALUES ";

                    string updateQuery = " ON DUPLICATE KEY UPDATE category_id = VALUES(category_id), product_code = VALUES(product_code), QR_code = VALUES(QR_code), product_name = VALUES(product_name), product_desc = VALUES(product_desc), product_remark = VALUES(product_remark), sequence_no = VALUES(sequence_no), product_status=VALUES(product_status), product_available_quantity = VALUES(product_available_quantity), product_cost_price = VALUES(product_cost_price);";

                    Database mysql = new Database(); // remember to open once

                    ArrayList categoriesFromDb = mysql.Select("SELECT * FROM cms_product_category;");
                    Dictionary<string, string> categoryList = new Dictionary<string, string>();

                    for (int i = 0; i < categoriesFromDb.Count; i++)
                    {
                        Dictionary<string, string> each = (Dictionary<string, string>)categoriesFromDb[i];
                        categoryList.Add(each["categoryIdentifierId"], each["category_id"]);
                    }

                    ArrayList inDBactiveProducts = mysql.Select("SELECT * FROM cms_product WHERE product_status = 1;");

                    ArrayList inDBproducts = new ArrayList();
                    for (int i = 0; i < inDBactiveProducts.Count; i++)
                    {
                        Dictionary<string, string> each = (Dictionary<string, string>)inDBactiveProducts[i];
                        inDBproducts.Add(each["product_code"].ToString());
                    }
                    inDBactiveProducts.Clear();

                    //testing for nvalid character after parsing property name. Expected ':' but got:  . Path '[302].lastSellingDate', line 1, position 278528.
                    //2.Unterminated string.Expected delimiter: ". Path '[1408].stockCode', line 1, position 1294336.
                    //3.Unexpected end of content while loading JArray.Path '[1087].minQty', line 1, position 999424.
                    //4.Unterminated string.Expected delimiter: ". Path '[623].currentBalance', line 1, position 573440
                    //dynamic jsonn = "[{\r\n    \"id\": \"734da921-ce44-409d-b5d2-01962e44e28e\",\r\n    \"stockCode\": \"test\",\r\n    \"stockName\": \"test\",\r\n    \"stockName2\": null,\r\n    \"description\": null,\r\n    \"furtherDescription\": null,\r\n    \"baseUOM\": \"UNIT(S)\",\r\n    \"minQty\": 0,\r\n    \"maxQty\": 0,\r\n    \"reorderLevel\": 0,\r\n    \"reorderQty\": 0,\r\n    \"listPrice\": 0,\r\n    \"minPrice\": 0,\r\n    \"salesDiscount\": null,\r\n    \"purchasePrice\": 0,\r\n    \"purchaseDiscount\": null,\r\n    \"barCode\": null,\r\n    \"weight\": 0,\r\n    \"volumn\": 0\r\n    \"currentBalance\": -1,\r\n    \"lastSellingDate\": null,\r\n    \"lastPurchaseDate\": null,\r\n    \"isActive\": true,\r\n    \"isBundled\": false,\r\n    \"createDate\": \"2017-11-21\",\r\n    \"stockControl\": true,\r\n    \"useSerialNo\": false,\r\n    \"serialNoPrefix\": null,\r\n    \"serialNoSuffix\": null,\r\n    \"remark1\": null,\r\n    \"remark2\": null,\r\n    \"remark3\": null,\r\n    \"remark4\": null,\r\n    \"remark5\": null,\r\n    \"useBatchNo\": false,\r\n    \"itemTypeCode\": null,\r\n    \"category\": null,\r\n    \"group\": null,\r\n    \"class\": null,\r\n    \"defaultInputTaxCode\": null,\r\n    \"defaultOutputTaxCode\": null\r\n  },\r\n  {\r\n    \"id\": \"e8195afa-804e-4b6c-9411-041ddd7d3f17\",\r\n    \"stockCode\": \"GRANDTOTAL\",\r\n    \"stockName\": \"GRAND TOTAL\",\r\n    \"stockName2\": null,\r\n    \"description\": \"GRAND TOTAL\",\r\n    \"furtherDescription\": null,\r\n    \"baseUOM\": null,\r\n    \"minQty\": 0,\r\n    \"maxQty\": 0,\r\n    \"reorderLevel\": 0,\r\n    \"reorderQty\": 0,\r\n    \"listPrice\": 0,\r\n    \"minPrice\": null,\r\n    \"salesDiscount\": null,\r\n    \"purchasePrice\": 0,\r\n    \"purchaseDiscount\": null,\r\n    \"barCode\": null,\r\n    \"weight\": null,\r\n    \"volumn\": null,\r\n    \"currentBalance\": 0,\r\n    \"lastSellingDate\": null,\r\n    \"lastPurchaseDate\": null,\r\n    \"isActive\": true,\r\n    \"isBundled\": false,\r\n    \"createDate\": \"2012-11-09\",\r\n    \"stockControl\": true,\r\n    \"useSerialNo\": false,\r\n    \"serialNoPrefix\": null,\r\n    \"serialNoSuffix\": null,\r\n    \"remark1\": null,\r\n    \"remark2\": null,\r\n    \"remark3\": null,\r\n    \"remark4\": null,\r\n    \"remark5\": null,\r\n    \"useBatchNo\": false,\r\n    \"itemTypeCode\": \"T\",\r\n    \"category\": null,\r\n    \"group\": null,\r\n    \"class\": null,\r\n    \"defaultInputTaxCode\": null,\r\n    \"defaultOutputTaxCode\": null\r\n  }\r\n]";
                    //try
                    //{
                    //    dynamic jsonnnn = JsonConvert.DeserializeObject<IEnumerable<object>>(jsonn);
                    //}
                    //catch (Exception exc)
                    //{
                    //    Console.WriteLine("Stocks: Failed to get content ---> " + exc.Message);
                    //    Console.WriteLine(exc.Message);
                    //}

                    try
                    {
                        foreach (var item in json)
                        {
                            //var jsonString = JsonConvert.SerializeObject(item);

                            RecordCount++;
                            int activeValue = 0;

                            if (item.isActive == true)
                            {
                                activeValue = 1;
                            }

                            if (inDBproducts.Contains(item.stockCode))
                            {
                                int index = inDBproducts.IndexOf(item.stockCode);
                                if (index != -1)
                                {
                                    inDBproducts.RemoveAt(index);
                                }
                            }

                            string _categoryId = "0";

                            if (string.IsNullOrEmpty(item.category.ToString()) || !categoryList.TryGetValue(item.category.ToString(), out _categoryId))
                            {
                                _categoryId = "0";
                            }

                            int.TryParse(_categoryId, out int CategoryId);

                            string p_code = item.stockCode,
                                    p_qr_code = item.barCode,
                                    p_name = item.stockName,
                                    p_desc = item.description,
                                    p_remark = item.remark1;

                            Database.Sanitize(ref p_code);
                            Database.Sanitize(ref p_qr_code);
                            Database.Sanitize(ref p_name);
                            Database.Sanitize(ref p_desc);
                            Database.Sanitize(ref p_remark);

                            if (inDBproducts.Contains(p_code))
                            {
                                int index = inDBproducts.IndexOf(p_code);
                                if (index != -1)
                                {
                                    inDBproducts.RemoveAt(index);
                                }
                            }

                            string Values = string.Format("({0},'{1}','{2}','{3}','{4}','{5}',{6},{7},{8},{9})", CategoryId, p_code, p_qr_code, p_name, p_desc, p_remark, RecordCount, activeValue, item.currentBalance, item.purchasePrice);

                            queryList.Add(Values);

                            //if (RecordCount % 10000 == 0)
                            //{
                            //    mysql.KillSleepyProcesses();
                            //}

                            if (queryList.Count % 2000 == 0)
                            {
                                string tmp_query = query;
                                tmp_query += string.Join(", ", queryList);
                                tmp_query += updateQuery;

                                mysql.Insert(tmp_query);

                                queryList.Clear();

                                tmp_query = string.Empty;

                                logger.message = string.Format("{0} stock records is inserted", RecordCount);
                                logger.Broadcast();
                            }
                        }

                        if (queryList.Count > 0)
                        {
                            query = query + string.Join(", ", queryList) + updateQuery;

                            mysql.Insert(query);

                            //mysql.Close(); // and close once-- follow the same for all files which are having issues ok.. the batch? make it >> 0

                            logger.message = string.Format("{0} stock records is inserted", RecordCount);
                            logger.Broadcast();

                            if (inDBproducts.Count > 0)
                            {
                                logger.Broadcast("Products to be deactivated: " + inDBproducts.Count);

                                string inactive = "INSERT INTO cms_product (product_code, product_status) VALUES ";
                                string inactive_duplicate = "ON DUPLICATE KEY UPDATE product_status=VALUES(product_status);";
                                for (int i = 0; i < inDBproducts.Count; i++)
                                {
                                    string _code = inDBproducts[i].ToString();
                                    Database.Sanitize(ref _code);
                                    string _query = string.Format("('{0}',0)", _code);
                                    mysql.Insert(inactive + _query + inactive_duplicate);
                                    mysql.Message("Products deactivated query: " + inactive + _query + inactive_duplicate);
                                }
                                inDBproducts.Clear();
                            }
                        }

                        string insertWh = "INSERT INTO cms_warehouse_stock (product_code,wh_code,ready_st_qty,available_st_qty,uom_name) SELECT p.product_code, '" + whCode + "' AS wh_code, p.product_available_quantity AS ready_st_qty, p.product_available_quantity AS available_st_qty, up.product_uom AS uom_name FROM cms_product AS p JOIN cms_product_uom_price_v2 up ON up.product_code = p.product_code AND up.active_status = 1 AND up.product_default_price = 1 ON DUPLICATE KEY UPDATE ready_st_qty = p.product_available_quantity, available_st_qty = p.product_available_quantity;";
                        mysql.Insert(insertWh);
                    }
                    catch (Exception ex)
                    {
                        api.Message("QNEAPI + JobStockSync.cs ---> " + ex.Message);
                    }

                    ENDJOB:

                    api.Message("Stock sync finished");

                    slog.action_identifier = Constants.Action_StockSync;
                    slog.action_details = Constants.Tbl_cms_product + Constants.Is_Finished + string.Format(" ({0}) records", RecordCount);
                    slog.action_failure = 0;
                    slog.action_failure_message = string.Empty;
                    slog.action_time = DateTime.Now.ToString();

                    DateTime endTime = DateTime.Now;
                    TimeSpan ts = endTime - startTime;
                    Console.WriteLine("Completed in: " + ts.TotalSeconds.ToString("F"));

                    LocalDB.InsertSyncLog(slog);

                    logger.message = "Stock sync is finished in (" + ts.TotalSeconds.ToString("F") + " seconds)";
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
                    file_name = "JobStockSync",
                    exception = e.Message,
                    time = DateTime.Now.ToString()
                };
                LocalDB.InsertException(ex);

                Console.WriteLine(Constants.Thread_Exception + e.Message);
            }
        }
    }
}