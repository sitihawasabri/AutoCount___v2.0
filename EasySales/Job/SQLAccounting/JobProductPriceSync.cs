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
    public class JobProductPriceSync : IJob
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

                    /**
                     * Here we will run SQLAccounting Codes
                     * */

                    DpprSyncLog slog = new DpprSyncLog();
                    slog.action_identifier = Constants.Action_ProductPriceSync;                             
                    slog.action_details = Constants.Tbl_cms_product_uom_price_v2 + Constants.Is_Starting;    
                    slog.action_failure = 0;
                    slog.action_failure_message = string.Empty;
                    slog.action_time = DateTime.Now.ToString();

                    DateTime startTime = DateTime.Now;

                    LocalDB.InsertSyncLog(slog);
                    logger.message = "----------------------------------------------------------------------------";
                    logger.Broadcast();
                    logger.message = "Product UOM price sync is running";
                    logger.Broadcast();
                    logger.message = "----------------------------------------------------------------------------";
                    logger.Broadcast();

                CHECKAGAIN:
                    SQLAccApi instance = SQLAccApi.getInstance();

                    dynamic ComServer = instance.GetComServer();

                    if (!instance.RPCAvailable())
                    {
                        goto CHECKAGAIN;
                    }

                    dynamic lMain, lRptVar, lUom;
                    string Code, Uom, Uomrate, Refprice, Minprice, Isbase, CodeFromUom; 
                    string query, updateQuery;

                    HashSet<string> queryList = new HashSet<string>();

                    Database _mysql = new Database();
                    ArrayList uniqueKey = _mysql.Select("SELECT product_uom_price_id, product_code, product_uom, product_uom_rate FROM cms_product_uom_price_v2 WHERE active_status = 1");
                    Dictionary<string, string> uniqueDict = new Dictionary<string, string>();

                    for(int i = 1; i < uniqueKey.Count; i++)
                    {
                        Dictionary<string, string> each = (Dictionary<string, string>)uniqueKey[i];
                        string id = each["product_uom_price_id"];
                        string code = each["product_code"];
                        string uom = each["product_uom"];
                        string uom_rate = each["product_uom_rate"];
                        string unique = code + uom + uom_rate;

                        uniqueDict.Add(id, unique);
                    }

                    logger.Broadcast("Active UOM price in DB: " + uniqueDict.Count);

                    try
                    {
                        lRptVar = ComServer.RptObjects.Find("Stock.Item.RO");

                        lRptVar.Params.Find("SelectDate").Value = false;
                        lRptVar.Params.Find("AllItem").Value = true;
                        lRptVar.Params.Find("AllStockGroup").Value = true;
                        lRptVar.Params.Find("SortBy").AsString = "Code";
                        lRptVar.CalculateReport();

                        lMain = lRptVar.DataSets.Find("cdsMain");
                        lUom = lRptVar.DataSets.Find("cdsUOM");

                        lMain.First();
                        lUom.First();

                        ArrayList uomList = new ArrayList();

                        query = "INSERT INTO cms_product_uom_price_v2 (product_code, product_uom, product_uom_rate, product_std_price, product_min_price, product_default_price,active_status, updated_at) VALUES ";

                        updateQuery = " ON DUPLICATE KEY UPDATE product_std_price = VALUES(product_std_price), product_min_price = VALUES(product_min_price), product_default_price = VALUES(product_default_price), product_uom = VALUES(product_uom),active_status = VALUES(active_status), updated_at = VALUES(updated_at)";

                        while (!lMain.eof)
                        {
                            while (!lUom.eof)
                            {
                                CodeFromUom = lUom.FindField("CODE").AsString;

                                if (CodeFromUom != null)
                                {
                                    RecordCount++;

                                    Uom = lUom.FindField("UOM").AsString;
                                    if (Uom != null && Uom != string.Empty)
                                    {
                                        Uom = Uom.Trim();
                                    }

                                    Uomrate = lUom.FindField("RATE").AsString;
                                    if (Uomrate == null || Uomrate == string.Empty)
                                    {
                                        Uomrate = "0";
                                    }

                                    Refprice = lUom.FindField("REFPRICE").AsString;
                                    if (Refprice == null || Refprice == string.Empty)
                                    {
                                        Refprice = "0.00";
                                    }

                                    Minprice = lUom.FindField("MINPRICE").AsString;
                                    if (Minprice == null || Minprice == string.Empty)
                                    {
                                        Minprice = "0.00";
                                    }

                                    Isbase = lUom.FindField("ISBASE").AsString;
                                    if (Isbase == null || Isbase == string.Empty)
                                    {
                                        Isbase = "0";
                                    }

                                    string unique = CodeFromUom + Uom + Uomrate;

                                    if (uniqueDict.Count > 0)
                                    {
                                        //deactivate those deleted uom
                                        if (uniqueDict.ContainsValue(unique))
                                        {
                                            var key = uniqueDict.Where(pair => pair.Value == unique)
                                                        .Select(pair => pair.Key)
                                                        .FirstOrDefault();
                                            if (key != null)
                                            {
                                                uniqueDict.Remove(key);
                                            }
                                        }
                                    }
                                    string updated_at = string.Empty;
                                    DateTime date = DateTime.Now;
                                    updated_at = date.ToString("s");

                                    Database.Sanitize(ref updated_at);
                                    Database.Sanitize(ref CodeFromUom);
                                    Database.Sanitize(ref Isbase);
                                    Database.Sanitize(ref Uom);

                                    string Values = string.Format("('{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}')", CodeFromUom, Uom, Uomrate, Refprice, Minprice, Isbase, 1, updated_at);

                                    queryList.Add(Values);

                                    if (queryList.Count % 2000 == 0)
                                    {
                                        string tmp_query = query;
                                        tmp_query += string.Join(", ", queryList);
                                        tmp_query += updateQuery;

                                        Database mysql = new Database();
                                        mysql.Insert(tmp_query);
                                        mysql.Message("UOM Price: " + tmp_query);

                                        queryList.Clear();
                                        tmp_query = string.Empty;

                                        logger.message = string.Format("{0} products price records is inserted", RecordCount);
                                        logger.Broadcast();
                                    }
                                }
                                lUom.Next();
                            }

                            lMain.Next();
                        }


                        if (queryList.Count > 0)
                        {
                            query = query + string.Join(", ", queryList) + updateQuery;

                            Database mysql = new Database();
                            mysql.Insert(query);
                            mysql.Message("UOM Price: " + query);

                            logger.message = string.Format("{0} products price records is inserted", RecordCount);
                            logger.Broadcast();
                        }

                        if (uniqueDict.Count > 0)
                        {
                            logger.Broadcast("Total UOM to be deactivated: " + uniqueDict.Count);

                            string inactive = "UPDATE cms_product_uom_price_v2 SET active_status = '{0}' WHERE product_uom_price_id = '{1}'";

                            Database mysql = new Database();

                            for (int i = 0; i < uniqueDict.Count; i++)
                            {
                                string id = uniqueDict.ElementAt(i).Key;

                                string _query = string.Format(inactive, '0', id);

                                mysql.Insert(_query);
                            }

                            logger.Broadcast(uniqueDict.Count + " UOM deactivated");

                            uniqueDict.Clear();
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
                                file_name = "SQLAccounting + JobProductPriceSync",
                                exception = exc.Message,
                                time = DateTime.Now.ToString()
                            };
                            LocalDB.InsertException(exception);
                        }
                    }

                    slog.action_identifier = Constants.Action_ProductPriceSync;
                    slog.action_details = Constants.Tbl_cms_product_uom_price_v2 + Constants.Is_Finished + string.Format(" ({0}) records", RecordCount);
                    slog.action_failure = 0;
                    slog.action_failure_message = string.Empty;
                    slog.action_time = DateTime.Now.ToString();

                    DateTime endTime = DateTime.Now;
                    TimeSpan ts = endTime - startTime;
                    Console.WriteLine("Completed in: " + ts.TotalSeconds.ToString("F"));

                    LocalDB.InsertSyncLog(slog);

                    logger.message = "Product price sync finished in (" + ts.TotalSeconds.ToString("F") + " seconds)";
                    logger.Broadcast();
                });

                thread.Start();
            }
            catch (ThreadAbortException e)
            {
                DpprException ex = new DpprException
                {
                    file_name = "JobProductPriceSync",
                    exception = e.Message,
                    time = DateTime.Now.ToString()
                };
                LocalDB.InsertException(ex);

                Console.WriteLine(Constants.Thread_Exception + e.Message);
            }
        }
    }
}