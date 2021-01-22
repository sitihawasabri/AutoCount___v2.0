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
    public class JobPostSalesCNs : IJob
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
                    slog.action_identifier = Constants.Action_PostSalesCNs;
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
                            logger.message = "QNE POST Sales CNs is already running";
                            logger.Broadcast();
                            goto ENDJOB;
                        }
                    }
                    LocalDB.InsertSyncLog(slog);

                    logger.message = "POST Sales CNs is running";
                    logger.Broadcast();

                    Database _mysql = new Database();
                    QNEApi api = new QNEApi();
                    api.Message("POST Sales CNs is running");
                    dynamic jsonRule = new CheckBackendRule().CheckTablesExist().GetSettingByTableName("transfer_cn");

                    string warehouseName = string.Empty;
                    string title = string.Empty;

                    if (jsonRule.Count > 0)
                    {
                        foreach (var key in jsonRule)
                        {
                            dynamic _warehouseName = key.warehouse_name;
                            warehouseName = _warehouseName;
                            
                            dynamic _title = key.title;
                            title = _title;
                        }
                    }
                    else
                    {
                        goto ENDJOB;
                    }

                    int order_status = 2;

                    string cnQuery = "SELECT cms_order.*,DATE_FORMAT(cms_order.order_date,'%d/%m/%Y %H:%s:%i') AS order_date_format, cms_login.staff_code FROM cms_order LEFT JOIN cms_login ON cms_order.salesperson_id = cms_login.login_id WHERE order_status = 2 AND cancel_status = 0 AND doc_type = 'credit'";

                    ArrayList cnFromDb = _mysql.Select(cnQuery);

                    if (cnFromDb.Count == 0)
                    {
                        logger.message = "No CNs to insert";
                        logger.Broadcast();
                        return;
                    }

                    for (int index = 0; index < cnFromDb.Count; index++)
                    {
                        Dictionary<string, string> cnObj = (Dictionary<string, string>)cnFromDb[index];

                        Thread.CurrentThread.CurrentCulture = new CultureInfo("en-GB");

                        SalesCNsPostParams @cnparams = new SalesCNsPostParams();
                        @cnparams.cnDate = Database.NativeDateTime(cnObj["order_date_format"]);
                        @cnparams.customer = cnObj["cust_code"];
                        @cnparams.customerName = cnObj["cust_company_name"];
                        @cnparams.salesPerson = cnObj["staff_code"];
                        @cnparams.address1 = cnObj["billing_address1"];
                        @cnparams.address2 = cnObj["billing_address2"];
                        @cnparams.address3 = cnObj["billing_address3"];
                        @cnparams.address4 = cnObj["billing_address4"];
                        @cnparams.term = cnObj["termcode"];
                        @cnparams.isApproved = true;
                        @cnparams.isCancelled = false;
                        @cnparams.title = "CREDIT NOTE";
                        @cnparams.remark1 = cnObj["order_remark"];
                        @cnparams.referenceNo = cnObj["order_id"];              
                        //put our order_id - the INV related will be insert manually by the admin

                        @cnparams.isRounding = false;
                        @cnparams.isTaxInclusive = false;
                        @cnparams.currencyRate = 1;

                        @cnparams.stockLocation = string.Empty;

                        if (jsonRule != null)
                        {
                            @cnparams.title = title;
                            @cnparams.stockLocation = warehouseName;
                        }

                        string cnQueryItem = "SELECT * FROM cms_order_item WHERE order_id = '" + cnObj["order_id"] + "' AND cancel_status = 0";

                        ArrayList cnDetailsFromDb = _mysql.Select(cnQueryItem);

                        ArrayList detailList = new ArrayList();

                        for (int iIdx = 0; iIdx < cnDetailsFromDb.Count; iIdx++)
                        {
                            Dictionary<string, string> item = (Dictionary<string, string>)cnDetailsFromDb[iIdx];

                            int quantity = 0;
                            string _quantity = string.Empty;

                            _quantity = item["quantity"];
                            int.TryParse(_quantity, out quantity);

                            double unit_price = 0.00;
                            double.TryParse(item["unit_price"], out unit_price);

                            SalesCNsDetailPostParams detailPostParams = new SalesCNsDetailPostParams();

                            detailPostParams.stock = item["product_code"];
                            detailPostParams.qty = quantity;
                            detailPostParams.unitPrice = unit_price;
                            detailPostParams.description = item["product_name"];
                            detailPostParams.uom = item["unit_uom"];
                            detailPostParams.ref1 = item["salesperson_remark"];  
                            detailPostParams.discount = item["discount_amount"];

                            detailList.Add(detailPostParams);
                        }

                        @cnparams.Details = detailList.ToArray(typeof(SalesCNsDetailPostParams)) as SalesCNsDetailPostParams[];

                        SalesCNsResp isInserted = api.PostSalesCNs(@cnparams);

                        if (isInserted != null)
                        {
                            int updateOrderStatus = order_status + 1;
                            _mysql.Insert("INSERT INTO cms_order (order_id, order_status, order_reference) VALUES ('" + cnObj["order_id"] + "','"+ updateOrderStatus + "','" + isInserted.cnCode + "') ON DUPLICATE KEY UPDATE order_status = VALUES(order_status),order_reference=VALUES(order_reference)");
                            logger.message = isInserted.cnCode + " created";
                            logger.Broadcast();
                        }

                    }

                    api.Message("POST Sales CNs finished");

                    slog.action_identifier = Constants.Action_PostSalesCNs;
                    slog.action_failure = 0;
                    slog.action_details = "Finished";
                    slog.action_failure_message = string.Empty;
                    slog.action_time = DateTime.Now.ToString();

                    DateTime endTime = DateTime.Now;
                    TimeSpan ts = endTime - startTime;
                    Console.WriteLine("Completed in: " + ts.TotalSeconds.ToString("F"));

                    LocalDB.InsertSyncLog(slog);
                    logger.message = "POST Sales CNs finished in (" + ts.TotalSeconds.ToString("F") + " seconds)";
                    logger.Broadcast();
                ENDJOB:
                    Console.WriteLine("ENDJOB");
                });

                thread.Start();
            }
            catch (ThreadAbortException e)
            {
                DpprException ex = new DpprException
                {
                    file_name = "JobPostSalesCNs",
                    exception = e.Message,
                    time = DateTime.Now.ToString()
                };
                LocalDB.InsertException(ex);

                Console.WriteLine(Constants.Thread_Exception + e.Message);
            }
        }
    }
}