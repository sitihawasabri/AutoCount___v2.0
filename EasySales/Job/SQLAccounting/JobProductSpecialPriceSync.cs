using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Globalization;
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
    public class JobProductSpecialPriceSync : IJob
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
                    slog.action_identifier = Constants.Action_ProductSpecialPriceSync;
                    slog.action_details = Constants.Tbl_cms_product_price_v2 + Constants.Is_Starting;
                    slog.action_failure = 0;
                    slog.action_failure_message = string.Empty;
                    slog.action_time = DateTime.Now.ToString();

                    DateTime startTime = DateTime.Now;

                    LocalDB.InsertSyncLog(slog);
                    logger.message = "----------------------------------------------------------------------------";
                    logger.Broadcast();
                    logger.message = "Product Special Price sync is running";
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

                    dynamic lRptVar;
                    string Price_tag, Code, Cust_code, Uom, Quantity, Date_from, Date_to, Price, Discount;
                    string query, updateQuery;

                    HashSet<string> queryList = new HashSet<string>();

                    try
                    {
                        lRptVar = ComServer.DBManager.NewDataSet("SELECT * FROM ST_ITEM_PRICE WHERE TAGTYPE = 'C' ORDER BY CODE ASC;");

                        lRptVar.First();

                        query = "INSERT INTO cms_product_price_v2 (product_code, price_cat, cust_code, date_from, date_to, product_price, disc_1, product_uom, quantity, active_status) VALUES ";
                        updateQuery = "ON DUPLICATE KEY UPDATE disc_1 = VALUES(disc_1), product_price = VALUES(product_price), price_cat = VALUES(price_cat), product_uom = VALUES(product_uom), quantity = VALUES(quantity),active_status = VALUES(active_status),date_from = VALUES(date_from),date_to = VALUES(date_to);";

                        HashSet<string> prodCodeList = new HashSet<string>();

                        while (!lRptVar.eof)
                        {
                            RecordCount++;

                            Price_tag = lRptVar.FindField("PRICETAG").AsString;
                            Code = lRptVar.FindField("CODE").AsString;

                            string sanitizeCode = Code;
                            Database.Sanitize(ref sanitizeCode);
                            prodCodeList.Add(sanitizeCode);

                            Cust_code = lRptVar.FindField("COMPANY").AsString;
                            Uom = lRptVar.FindField("UOM").AsString;
                            Quantity = lRptVar.FindField("QTY").AsString;
                            Date_from = lRptVar.FindField("DATEFROM").AsString;
                            Date_to = lRptVar.FindField("DATETO").AsString;

                            if (Date_from != null)
                            {
                                Date_from = lRptVar.FindField("DATEFROM").AsString;
                                Date_from = Convert.ToDateTime(Date_from).ToString("yyyy-MM-dd");
                            }
                            else
                            {
                                Date_from = "0000-00-00 00:00:00";
                            }

                            if (Date_to != null)
                            {
                                Date_to = lRptVar.FindField("DATETO").AsString;
                                Date_to = Convert.ToDateTime(Date_to).ToString("yyyy-MM-dd");
                            }
                            else
                            {
                                Date_to = "0000-00-00 00:00:00";
                            }

                            Price = lRptVar.FindField("STOCKVALUE").AsString;

                            Discount = lRptVar.FindField("DISCOUNT").AsString;

                            if (Discount != null)
                            {
                                Discount = lRptVar.FindField("DISCOUNT").AsString;
                            }
                            else
                            {
                                Discount = "0";
                            }

                            int ActiveStatus = 1;

                            Database.Sanitize(ref Price_tag);
                            Database.Sanitize(ref Code);
                            Database.Sanitize(ref Cust_code);
                            Database.Sanitize(ref Uom);
                            Database.Sanitize(ref Quantity);
                            Database.Sanitize(ref Date_from);
                            Database.Sanitize(ref Date_to);
                            Database.Sanitize(ref Price);
                            Database.Sanitize(ref Discount);

                            string Values = string.Format("('{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}','{8}','{9}')", Code, Price_tag, Cust_code, Date_from, Date_to, Price, Discount, Uom, Quantity, ActiveStatus);

                            queryList.Add(Values);

                            if (queryList.Count % 1000 == 0)
                            {
                                string ToBeDeactivate = "'" + string.Join("','", prodCodeList) + "'";
                                Console.WriteLine(ToBeDeactivate);
                                string deactivateInBatch = "UPDATE cms_product_price_v2 SET active_status = 0 WHERE product_code IN(" + ToBeDeactivate + ")";
                                Console.WriteLine(deactivateInBatch);

                                Database mysql = new Database();
                                mysql.Insert(deactivateInBatch);
                                prodCodeList.Clear();

                                string tmp_query = query;
                                tmp_query += string.Join(", ", queryList);
                                tmp_query += updateQuery;
                                mysql.Insert(tmp_query);

                                queryList.Clear();
                                tmp_query = string.Empty;

                                logger.message = string.Format("{0} product special price records is inserted", RecordCount);
                                logger.Broadcast();
                            }
                            lRptVar.Next();
                        }

                        if (queryList.Count > 0)
                        {
                            string ToBeDeactivate = "'" + string.Join("','", prodCodeList) + "'";
                            Console.WriteLine(ToBeDeactivate);
                            string deactivateInBatch = "UPDATE cms_product_price_v2 SET active_status = 0 WHERE product_code IN(" + ToBeDeactivate + ")";
                            Console.WriteLine(deactivateInBatch);

                            Database mysql = new Database();
                            mysql.Insert(deactivateInBatch);

                            query = query + string.Join(", ", queryList) + updateQuery;
                            mysql.Insert(query);

                            logger.message = string.Format("{0} product special price records is inserted", RecordCount);
                            logger.Broadcast();
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
                                file_name = "SQLAccounting + JobProductSpecialPriceSync",
                                exception = exc.Message,
                                time = DateTime.Now.ToString()
                            };
                            LocalDB.InsertException(exception);
                        }
                    }

                    slog.action_identifier = Constants.Action_ProductSpecialPriceSync;
                    slog.action_details = Constants.Tbl_cms_product_price_v2 + Constants.Is_Finished + string.Format(" ({0}) records", RecordCount);
                    slog.action_failure = 0;
                    slog.action_failure_message = string.Empty;
                    slog.action_time = DateTime.Now.ToString();

                    DateTime endTime = DateTime.Now;
                    TimeSpan ts = endTime - startTime;
                    Console.WriteLine("Completed in: " + ts.TotalSeconds.ToString("F"));

                    LocalDB.InsertSyncLog(slog);

                    logger.message = "Product special price sync finished in (" + ts.TotalSeconds.ToString("F") + " seconds)";
                    logger.Broadcast();
                });

                thread.Start();
                //thread.Join();

                //await Console.Out.WriteLineAsync("Product special price Sync");
            }
            catch (ThreadAbortException e)
            {
                DpprException ex = new DpprException
                {
                    file_name = "JobProductSpecialPriceSync",
                    exception = e.Message,
                    time = DateTime.Now.ToString()
                };
                LocalDB.InsertException(ex);

                Console.WriteLine(Constants.Thread_Exception + e.Message);
            }
        }
    }
}