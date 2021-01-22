using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using System.ComponentModel;
using System.Threading;
using EasySales.Model;
using EasySales.Object;
using Quartz;
using Quartz.Impl;
using Quartz.Logging;
using RestSharp;
using EasySales.Object.QNE;
using System.Globalization;

namespace EasySales.Job
{
    [DisallowConcurrentExecution]
    public class JobPostSalesOrders : IJob
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
                    slog.action_identifier = Constants.Action_PostSalesOrders;
                    slog.action_details = "Starting";
                    slog.action_failure = 0;
                    slog.action_failure_message = string.Empty;
                    slog.action_time = DateTime.Now.ToString();
                    DateTime startTime = DateTime.Now;

                    List<DpprSyncLog> list = LocalDB.checkJobRunning();
                    if (list.Count > 0)
                    {
                        DpprSyncLog value = list[0];
                        if (value.action_details == "Starting")
                        {
                            logger.message = "QNE POST Sales Orders is already running";
                            logger.Broadcast();
                            goto ENDJOB;
                        }
                    }
                    LocalDB.InsertSyncLog(slog);

                    logger.message = "POST Sales Orders is running";
                    logger.Broadcast();

                    Database _mysql = new Database();
                    QNEApi api = new QNEApi();
                    api.Message("POST Sales Orders is running");
                    //int querySetting = 0; /* default = only post sales order, don't post sales invoice; if 1, then post to sales invoice also */
                    int pickpackLink = 0; /* link with pickpack app - [+] packing query */
                    int includeInvoice = 0; /* if 1, include Invoice, if 0, don't include */
                    string sortingItem = string.Empty;

                    dynamic jsonRule = new CheckBackendRule().CheckTablesExist().GetSettingByTableName("cms_order");

                    if (jsonRule.Count > 0)
                    {
                        foreach (var key in jsonRule)
                        {
                            //dynamic _querySet = key.query_setting;
                            dynamic _pickpack_link = key.pickpack_link;

                            if (_pickpack_link != 0)
                            {
                                pickpackLink = _pickpack_link;
                            }

                            dynamic _incInv = key.include_invoice;

                            if (_incInv != 0)
                            {
                                includeInvoice = _incInv;
                            }

                            dynamic _sorting = key.sorting;

                            sortingItem = _sorting;
                        }
                    }
                    else
                    {
                        goto ENDJOB;
                    }

                    string soQuery = "SELECT cms_order.*,DATE_FORMAT(cms_order.order_date,'%d/%m/%Y %H:%s:%i') AS order_date_format, cms_login.staff_code FROM cms_order LEFT JOIN cms_login ON cms_order.salesperson_id = cms_login.login_id WHERE order_status = 1 AND cancel_status = 0 AND doc_type = 'sales'"; /* 0 - didnt link with pickpack app [-] packing status query */

                    if (jsonRule.Count > 0)
                    {
                        if (pickpackLink == 1)
                        {
                            soQuery = "SELECT cms_order.*,DATE_FORMAT(cms_order.order_date,'%d/%m/%Y %H:%s:%i') AS order_date_format, cms_login.staff_code FROM cms_order LEFT JOIN cms_login ON cms_order.salesperson_id = cms_login.login_id WHERE order_status = 1 AND cancel_status = 0 AND packing_status = 1 AND doc_type = 'sales'"; 
                            /* default GM - set rules in GM query_setting = 1 - link with pickpack [+] packing status query */
                        }
                    }

                    ArrayList salesOrdersFromDb = _mysql.Select(soQuery);

                    if (salesOrdersFromDb.Count == 0)
                    {
                        logger.message = "No Order to insert";
                        logger.Broadcast();
                        return;
                    }

                    for (int index = 0; index < salesOrdersFromDb.Count; index++)
                    {
                        Dictionary<string, string> orderObj = (Dictionary<string, string>)salesOrdersFromDb[index];

                        Thread.CurrentThread.CurrentCulture = new CultureInfo("en-GB");

                        SalesOrdersPostParams @params = new SalesOrdersPostParams();
                        @params.OrderDate = Database.NativeDateTime(orderObj["order_date_format"]);
                        @params.Customer = orderObj["cust_code"];
                        @params.SalesPerson = orderObj["staff_code"];
                        @params.Address1 = orderObj["billing_address1"];
                        @params.Address2 = orderObj["billing_address2"];
                        @params.Address3 = orderObj["billing_address3"];
                        @params.Address4 = orderObj["billing_address4"];
                        @params.Term = orderObj["termcode"];
                        @params.IsApproved = true;
                        @params.IsCancelled = false;
                        @params.Title = "SALES";
                        @params.Remark1 = orderObj["order_remark"];   
                        @params.ReferenceNo = orderObj["order_id"];     //app order_id

                        @params.IsRounding = false;
                        @params.IsTaxInclusive = false;
                        @params.CurrencyRate = 1;

                        string soQueryItem = "SELECT * FROM cms_order_item WHERE order_id = '" + orderObj["order_id"] + "' AND cancel_status = 0"; 
                        /* no order by */
                        /* 0 - didnt link with pickpack [-] packing status query */
                        /* sortingItem = ipad_item_id DESC */
                        if (jsonRule != null)
                        {
                            if (sortingItem != string.Empty)
                            {
                                soQueryItem = "SELECT * FROM cms_order_item WHERE order_id = '" + orderObj["order_id"] + "' AND cancel_status = 0 ORDER BY " + sortingItem;
                            }

                            if (pickpackLink == 1)
                            {
                                soQueryItem = "SELECT * FROM cms_order_item WHERE order_id = '" +
                           orderObj["order_id"] + "' AND cancel_status = 0 AND packing_status = 1 AND packed_qty <> 0 ORDER BY " + sortingItem ;
                                /*default GM integrated with picker app : 1 -link with pickpack  [+] packing status query */
                            }
                        }

                        ArrayList salesOrderDetailsFromDb = _mysql.Select(soQueryItem);

                        ArrayList detailList = new ArrayList();

                        for (int iIdx = 0; iIdx < salesOrderDetailsFromDb.Count; iIdx++)
                        {
                            Dictionary<string, string> item = (Dictionary<string, string>)salesOrderDetailsFromDb[iIdx];

                            int quantity = 0;
                            string _quantity = string.Empty;

                            _quantity = item["quantity"];
                            int.TryParse(_quantity, out quantity);

                            if (jsonRule != null)
                            {
                                if (pickpackLink == 1) /* link with pickpack app */
                                {
                                    _quantity = item["packed_qty"]; //packed_qty --> amount of items the picker packed
                                    int.TryParse(_quantity, out quantity);
                                }
                            }

                            double unit_price = 0.00;
                            double.TryParse(item["unit_price"], out unit_price);

                            SalesOrdersDetailPostParams detailPostParams = new SalesOrdersDetailPostParams();

                            detailPostParams.Stock = item["product_code"];
                            detailPostParams.Qty = quantity;
                            detailPostParams.UnitPrice = unit_price;
                            detailPostParams.Description = item["product_name"];
                            detailPostParams.Uom = item["unit_uom"];
                            detailPostParams.Ref = item["salesperson_remark"];    /* can see at ref [BIZSOFT] */
                            detailPostParams.Discount = item["disc_1"] + "%";

                            detailList.Add(detailPostParams);
                        }

                        @params.Details = detailList.ToArray(typeof(SalesOrdersDetailPostParams)) as SalesOrdersDetailPostParams[];

                        SalesOrdersResp isInserted = api.PostSalesOrders(@params);

                        if (isInserted != null)
                        {
                            _mysql.Insert("INSERT INTO cms_order (order_id, order_status, order_reference) VALUES ('" + orderObj["order_id"] + "','2','" + isInserted.OrderCode + "') ON DUPLICATE KEY UPDATE order_status = VALUES(order_status),order_reference=VALUES(order_reference)");
                            logger.message = "Order " + isInserted.OrderCode + " created";
                            logger.Broadcast();

                            if (jsonRule != null)
                            {
                                if (includeInvoice == 1) /* set backend rules in GM - include_invoice = 1 */
                                {
                                    NotJobPostInvoice invoice = new NotJobPostInvoice(NotJobPostInvoice.Operation.SO_Related);
                                    invoice.SetSoId(orderObj["order_id"]);
                                    invoice.SetRefNo(isInserted.OrderCode);
                                    invoice.Execute();
                                }
                            }
                        }

                    }
                    api.Message("POST Sales Orders finished");
                    slog.action_identifier = Constants.Action_PostSalesOrders;
                    slog.action_failure = 0;
                    slog.action_failure_message = string.Empty;
                    slog.action_time = DateTime.Now.ToString();

                    DateTime endTime = DateTime.Now;
                    TimeSpan ts = endTime - startTime;
                    Console.WriteLine("Completed in: " + ts.TotalSeconds.ToString("F"));

                    LocalDB.InsertSyncLog(slog);
                    logger.message = "Post Sales Order finished in (" + ts.TotalSeconds.ToString("F") + " seconds)";
                    LocalDB.InsertSyncLog(slog);
                    ENDJOB:
                    Console.WriteLine("ENDJOB");
                });

                thread.Start();
                //thread.Join();

                //await Console.Out.WriteLineAsync("Done POST Sales Orders");
            }
            catch (ThreadAbortException e)
            {
                DpprException ex = new DpprException
                {
                    file_name = "JobPostSalesOrders",
                    exception = e.Message,
                    time = DateTime.Now.ToString()
                };
                LocalDB.InsertException(ex);

                Console.WriteLine(Constants.Thread_Exception + e.Message);
            }
        }
    }
}