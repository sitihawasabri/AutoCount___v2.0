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
using Quartz;
using Quartz.Impl;
using Quartz.Logging;
using RestSharp;
using System.Data;
using System.Diagnostics;
using System.Net;
using Newtonsoft.Json;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System.Web.Script.Serialization;

namespace EasySales.Job
{
    [DisallowConcurrentExecution]
    public class JobInvoiceDetailsQneSync : IJob
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
                    slog.action_identifier = Constants.Action_InvoiceDetailsQneSync;
                    slog.action_details = Constants.Tbl_cms_invoice_details_qne + Constants.Is_Starting;
                    slog.action_failure = 0;
                    slog.action_failure_message = string.Empty;
                    slog.action_time = DateTime.Now.ToString();
                    DateTime startTime = DateTime.Now;
                    LocalDB.InsertSyncLog(slog);

                    logger.message = "Invoice details QNE sync is running";
                    logger.Broadcast();

                    QNEApi api = new QNEApi();
                    Database mysql = new Database();
                    api.Message("Invoice details QNE sync is running");
                    HashSet<string> queryList = new HashSet<string>();
                    int RecordCount = 0;

                    string query = "INSERT INTO cms_invoice_details(invoice_code, item_code, item_name, item_price, quantity, uom, discount, total_price) VALUES ";

                    string updateQuery = " ON DUPLICATE KEY UPDATE invoice_code = VALUES(invoice_code), item_code = VALUES(item_code), item_name = VALUES(item_name), item_price = VALUES(item_price), quantity=VALUES(quantity), uom = VALUES(uom), discount = VALUES(discount), total_price=VALUES(total_price);";

                    ArrayList invoices = mysql.Select("SELECT invoice_code FROM cms_invoice WHERE invoice_date BETWEEN CURRENT_DATE() - INTERVAL 1 YEAR AND CURRENT_DATE() ORDER BY invoice_date DESC;");

                    try
                    {
                        for (int inDx = 0; inDx < invoices.Count; inDx++)
                        {
                            Dictionary<string, string> dict = (Dictionary<string, string>)invoices[inDx];

                            dynamic json = api.GetByName("SalesInvoices/Find", new Parameter("code", dict["invoice_code"], ParameterType.QueryString));

                            foreach (var item in json)
                            {
                                dynamic _item = item.details;

                                foreach (var dtl in _item)
                                {
                                    RecordCount++;

                                    string inv_code = dtl.salesInvoice, item_code = dtl.stock, desc = dtl.description, uom = dtl.uom;

                                    Database.Sanitize(ref inv_code);
                                    Database.Sanitize(ref item_code);
                                    Database.Sanitize(ref desc);
                                    Database.Sanitize(ref uom);

                                    string Values = string.Format("('{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}')", inv_code, item_code, desc, dtl.unitPrice, dtl.qty, uom, dtl.discountAmount, dtl.amount);

                                    string tmp_query = query;
                                    tmp_query += Values;
                                    tmp_query += updateQuery;

                                    queryList.Add(tmp_query);

                                    if (queryList.Count % 2000 == 0)
                                    {
                                        string multiInsert = string.Join(" ", queryList);

                                        mysql.Insert(multiInsert);

                                        queryList.Clear();

                                        logger.message = string.Format("{0} invoice details QNE records is inserted", RecordCount);
                                        logger.Broadcast();
                                    }
                                }
                            }
                        }

                        if (queryList.Count > 0)
                        {
                            string multiInsert = string.Join(" ", queryList);

                            mysql.Insert(multiInsert);

                            logger.message = string.Format("{0} invoice details QNE records is inserted", RecordCount);
                            logger.Broadcast();
                        }
                    }
                    catch (Exception ex)
                    {
                        api.Message("QNEAPI + JobInvoiceDetailsSync.cs ---> " + ex.Message);
                    }
                    api.Message("Invoice details QNE sync finished");
                    slog.action_identifier = Constants.Action_InvoiceDetailsQneSync;
                    slog.action_details = Constants.Tbl_cms_invoice_details_qne + Constants.Is_Finished + string.Format(" ({0}) records", RecordCount);
                    slog.action_failure = 0;
                    slog.action_failure_message = string.Empty;
                    slog.action_time = DateTime.Now.ToString();

                    DateTime endTime = DateTime.Now;
                    TimeSpan ts = endTime - startTime;
                    Console.WriteLine("Completed in: " + ts.TotalSeconds.ToString("F"));

                    LocalDB.InsertSyncLog(slog);
                    logger.message = "Invoice details QNE sync finished in (" + ts.TotalSeconds.ToString("F") + " seconds)";
                    logger.Broadcast();
                });

                thread.Start();
                //thread.Join();

                //await Console.Out.WriteLineAsync("Done QNE Invoice Details Sync");
            }
            catch (ThreadAbortException e)
            {
                DpprException ex = new DpprException
                {
                    file_name = "JobInvoiceDetailsQneSync",
                    exception = e.Message,
                    time = DateTime.Now.ToString()
                };
                LocalDB.InsertException(ex);

                Console.WriteLine(Constants.Thread_Exception + e.Message);
            }
        }
    }
}