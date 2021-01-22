using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using EasySales.Model;
using EasySales.Object;
using Newtonsoft.Json.Linq;
using Quartz;
using Quartz.Impl;
using Quartz.Logging;

namespace EasySales.Job
{
    [DisallowConcurrentExecution]
    public class JobCostPriceSync : IJob
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

                    DpprSyncLog slog = new DpprSyncLog
                    {
                        action_identifier = Constants.Action_CostPriceSync,
                        action_details = Constants.Tbl_cms_product_purchase_price,
                        action_failure = 0,
                        action_failure_message = "Cost price sync is running",
                        action_time = DateTime.Now.ToLongDateString()
                    };

                    DateTime startTime = DateTime.Now;

                    LocalDB.InsertSyncLog(slog);
                    logger.message = "----------------------------------------------------------------------------";
                    logger.Broadcast();
                    logger.message = "Cost price sync is running";
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

                    Database mysql = new Database();

                    dynamic lDataSet;
                    string lSQL, query, updateQuery;
                    string Pr_code, CompanyCode, CompanyName, ItemCode, ItemName, ItemUom, ItemUomRate, Quantity, UnitPrice, TotalPrice, Pr_date;

                    query = "INSERT INTO cms_product_purchase_price (pr_code, supplier_code, supplier_name, product_code, product_name, product_uom, product_uom_rate, quantity, unit_price, grand_total, pr_date, active_status) VALUES ";

                    updateQuery = " ON DUPLICATE KEY UPDATE supplier_code = VALUES(supplier_code), supplier_name = VALUES(supplier_name), product_code = VALUES(product_code), product_name = VALUES(product_name), product_uom = VALUES(product_uom), product_uom_rate = VALUES(product_uom_rate), quantity = VALUES(quantity), unit_price = VALUES(unit_price), grand_total = VALUES(grand_total), pr_date = VALUES(pr_date)";

                    HashSet<string> queryList = new HashSet<string>();

                    lSQL = "select dtl.DTLKEY,dtl.itemcode,po.docdate, po.CODE,po.COMPANYNAME, dtl.AMOUNT,dtl.DESCRIPTION,dtl.QTY,dtl.RATE,dtl.UOM,dtl.UNITPRICE from ph_po as po left join ph_podtl dtl on dtl.dockey = po.dockey where itemcode is not null order by docdate desc";
                    lDataSet = ComServer.DBManager.NewDataSet(lSQL);

                    lDataSet.First();

                    while (!lDataSet.eof)
                    {
                        RecordCount++;

                        Pr_code = lDataSet.FindField("DTLKEY").AsString;
                        CompanyCode = lDataSet.FindField("CODE").AsString;
                        CompanyName = lDataSet.FindField("COMPANYNAME").AsString;
                        ItemName = lDataSet.FindField("DESCRIPTION").AsString;
                        ItemCode = lDataSet.FindField("itemcode").AsString;
                        UnitPrice = lDataSet.FindField("unitprice").AsString;
                        ItemUom = lDataSet.FindField("UOM").AsString;
                        ItemUomRate = lDataSet.FindField("RATE").AsString;
                        Quantity = lDataSet.FindField("QTY").AsString;
                        TotalPrice = lDataSet.FindField("AMOUNT").AsString;
                        Pr_date = lDataSet.FindField("docdate").AsString;
                        Pr_date = Convert.ToDateTime(Pr_date).ToString("yyyy-MM-dd");

                        Database.Sanitize(ref Pr_code);
                        Database.Sanitize(ref CompanyCode);
                        Database.Sanitize(ref CompanyName);
                        Database.Sanitize(ref ItemCode);
                        Database.Sanitize(ref ItemName);
                        Database.Sanitize(ref ItemUom);
                        Database.Sanitize(ref ItemUomRate);
                        Database.Sanitize(ref Pr_date);

                        string Values = string.Format("('{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}','{8}','{9}','{10}','{11}')", Pr_code, CompanyCode, CompanyName, ItemCode, ItemName, ItemUom, ItemUomRate, Quantity, UnitPrice, TotalPrice, Pr_date, 1);

                        queryList.Add(Values);

                        if (queryList.Count % 2000 == 0)
                        {
                            string tmp_query = query;
                            tmp_query += string.Join(", ", queryList);
                            tmp_query += updateQuery;

                            mysql.Insert(tmp_query);
                            //mysql.Close();

                            queryList.Clear();
                            tmp_query = string.Empty;

                            logger.message = string.Format("{0} cost price records is inserted", RecordCount);
                            logger.Broadcast();
                        }

                        lDataSet.Next();
                    }

                    if (queryList.Count > 0)
                    {
                        query = query + string.Join(", ", queryList) + updateQuery;

                        mysql.Insert(query);
                        //mysql.Close();

                        logger.message = string.Format("{0} cost price records is inserted", RecordCount);
                        logger.Broadcast();
                    }

                    slog.action_identifier = Constants.Action_CostPriceSync;
                    slog.action_details = Constants.Tbl_cms_product_purchase_price + Constants.Is_Finished + string.Format(" ({0}) records", RecordCount);
                    slog.action_failure = 0;
                    slog.action_failure_message = string.Empty;
                    slog.action_time = DateTime.Now.ToString();

                    DateTime endTime = DateTime.Now;
                    TimeSpan ts = endTime - startTime;
                    Console.WriteLine("Completed in: " + ts.TotalSeconds.ToString("F"));

                    LocalDB.InsertSyncLog(slog);

                    logger.message = "Cost price sync finished in (" + ts.TotalSeconds.ToString("F") + " seconds)";
                    logger.Broadcast();
                });

                thread.Start();
                //thread.Join();

                //await Console.Out.WriteLineAsync("Cost Price Sync");
            }
            catch (ThreadAbortException e)
            {
                DpprException ex = new DpprException
                {
                    file_name = "JobCostPriceSync",
                    exception = e.Message,
                    time = DateTime.Now.ToLongTimeString()
                };
                LocalDB.InsertException(ex);

                Console.WriteLine(Constants.Thread_Exception + e.Message);
            }
        }
    }
}