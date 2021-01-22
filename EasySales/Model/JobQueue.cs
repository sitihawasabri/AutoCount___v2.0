using Quartz;
using System;
using System.Collections.Generic;
using Quartz.Impl;
using Quartz.Logging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Specialized;
using EasySales.Job;
using System.Threading;
using EasySales.Object;
using Quartz.Impl.Matchers;
using EasySales.Job.APS;

namespace EasySales.Model
{
    public static class JobQueue
    {
        public static async Task Run()
        {
            try
            {
                NameValueCollection props = new NameValueCollection
                {
                    { "quartz.serializer.type", "binary" },
                    { "quartz.threadPool.threadCount" , "20" }
                };

                int InvoiceInterval = 5, InvoiceDetailsInterval = 5, OutstandingSoInterval = 5, 
                    CustomerInterval = 5, StockInterval = 5, StockCategoriesInterval = 5, StockUomPriceInterval = 5, 
                    CustomerAgentInterval = 5, CreditNoteInterval = 5, DebitNoteInterval = 5, CustomerRefundInterval = 5, 
                    ReceiptInterval = 5, SalesOrdersInterval = 5, PostSalesInvoicesInterval = 1, PostSalesCNsInterval = 5, 
                    BranchInterval = 5, ProductSpecialPriceInterval = 5,  ReadImageInterval = 5, ItemTemplateDtlInterval = 5, 
                    ItemTemplateInterval = 5, ProductGroupInterval = 5, CostPriceInterval = 5, PostStockInterval = 5, 
                    CreditNoteDtlInterval = 5, KnockOffInterval = 5, WarehouseInterval = 5, 
                    DOInterval = 5, PostQuotationInterval = 5, PostPOBasketInterval = 5, PostPaymentInterval = 5, 
                    SOSyncInterval = 5, TransferCSInterval = 5, INVSalesInterval = 5, CNSalesInterval = 5, DNSalesInterval = 5,
                    StockCardInterval = 5, ActiveJobs = 0;

                bool InvEnabled = false, InvDtlEnabled = false, OutsoEnabled = false, CustomerEnabled = false,
                    StockEnabled = false, StockCategoriesEnabled = false, StockUomPriceEnabled = false,
                    CustomerAgentEnabled = false, CreditNoteEnabled = false, DebitNoteEnabled = false,
                    CustomerRefundEnabled = false, ReceiptEnabled = false, SalesOrdersEnabled = false,
                    PostSalesInvoicesEnabled = false, PostSalesCNsEnabled = false, BranchEnabled = false,
                    ProductSpecialPriceEnabled = false, ReadImageEnabled = false, ItemTemplateDtlEnabled = false,
                    ItemTemplateEnabled = false, ProductGroupEnabled = false, CostPriceEnabled = false,
                    PostStockEnabled = false, CreditNoteDtlEnabled = false,
                    KnockOffEnabled = false, WarehouseEnabled = false, DOEnabled = false, PostQuotationEnabled = false,
                    PostPOBasketEnabled = false, PostPaymentEnabled = false, SOSyncEnabled = false, TransferCSEnabled = false,
                    INVSalesEnabled = false, CNSalesEnabled = false, DNSalesEnabled = false, ATCSDKEnabled = false, ATCV2Enabled = false, StockCardEnabled = false;

                List<DpprUserSettings> list = LocalDB.GetUserSettings();
                for (int i = 0; i < list.Count; i++)
                {
                    DpprUserSettings settings = list[i];
                    if (settings.name == Constants.Setting_Inv_Intv)
                    {
                        InvoiceInterval = int.Parse(settings.setting);
                    }
                    if (settings.name == Constants.Setting_InvDtl_Intv)
                    {
                        InvoiceDetailsInterval = int.Parse(settings.setting);
                    }
                    if (settings.name == Constants.Setting_OutstandingSo_Intv)
                    {
                        OutstandingSoInterval = int.Parse(settings.setting);
                    }
                    if (settings.name == Constants.Setting_Customer_Intv)
                    {
                        CustomerInterval = int.Parse(settings.setting);
                    }
                    if (settings.name == Constants.Setting_Stock_Intv) 
                    {
                        StockInterval = int.Parse(settings.setting);
                    }
                    if (settings.name == Constants.Setting_StockCategories_Intv) 
                    {
                        StockCategoriesInterval = int.Parse(settings.setting);
                    }
                    if (settings.name == Constants.Setting_StockUomPrice_Intv) 
                    {
                        StockUomPriceInterval = int.Parse(settings.setting);
                    }
                    if (settings.name == Constants.Setting_CustomerAgent_Intv)
                    {
                        CustomerAgentInterval = int.Parse(settings.setting);
                    }
                    if (settings.name == Constants.Setting_CreditNote_Intv) 
                    {
                        CreditNoteInterval = int.Parse(settings.setting);
                    }
                    if (settings.name == Constants.Setting_CreditNoteDtl_Intv)                      /* CN DTL */
                    {
                        CreditNoteDtlInterval = int.Parse(settings.setting);
                    }
                    if (settings.name == Constants.Setting_DebitNote_Intv) 
                    {
                        DebitNoteInterval = int.Parse(settings.setting);
                    }
                    if (settings.name == Constants.Setting_CustomerRefund_Intv) 
                    {
                        CustomerRefundInterval = int.Parse(settings.setting);
                    }
                    if (settings.name == Constants.Setting_Receipt_Intv)
                    {
                        ReceiptInterval = int.Parse(settings.setting);
                    }
                    if (settings.name == Constants.Setting_Post_SalesOrders_Intv)
                    {
                        SalesOrdersInterval = int.Parse(settings.setting);
                    }
                    if (settings.name == Constants.Setting_Post_CashSales_Intv)
                    {
                        TransferCSInterval = int.Parse(settings.setting);
                    }
                    if (settings.name == Constants.Setting_Post_SalesInvoices_Intv)                         
                    {
                        PostSalesInvoicesInterval = int.Parse(settings.setting);
                    }
                    if (settings.name == Constants.Setting_Post_SalesCNs_Intv)                  //POST CNs                   
                    {
                        PostSalesCNsInterval = int.Parse(settings.setting);
                    }
                    if (settings.name == Constants.Setting_Branch_Intv)
                    {
                        BranchInterval = int.Parse(settings.setting);
                    }
                    if (settings.name == Constants.Setting_ProductSpecialPrice_Intv)
                    {
                        ProductSpecialPriceInterval = int.Parse(settings.setting);
                    }
                    if (settings.name == Constants.Setting_ReadImage_Intv)                  /* READ IMAGES FROM SQLACC */
                    {
                        ReadImageInterval = int.Parse(settings.setting);
                    }
                    if (settings.name == Constants.Setting_ProductGroup_Intv)                  /* PRODUCT GROUP */
                    {
                        ProductGroupInterval = int.Parse(settings.setting);
                    }
                    if (settings.name == Constants.Setting_StockCard_Intv)                  /* Stock Card */
                    {
                        StockCardInterval = int.Parse(settings.setting);
                    }
                    if (settings.name == Constants.Setting_ItemTemplate_Intv)                  /* ITEM TEMPLATE */
                    {
                        ItemTemplateInterval = int.Parse(settings.setting);
                    }
                    if (settings.name == Constants.Setting_ItemTemplateDtl_Intv)                  /* ITEM TEMPLATE DTL */
                    {
                        ItemTemplateDtlInterval = int.Parse(settings.setting);
                    }
                    if (settings.name == Constants.Setting_CostPrice_Intv)                      /* COST PRICE */
                    {
                        CostPriceInterval = int.Parse(settings.setting);
                    }
                    if (settings.name == Constants.Setting_PostStock_Intv)                      /* PostStock */
                    {
                        PostStockInterval = int.Parse(settings.setting);
                    }
                    if (settings.name == Constants.Setting_KnockOff_Intv)                      /* KnockOff */
                    {
                        KnockOffInterval = int.Parse(settings.setting);
                    }
                    if (settings.name == Constants.Setting_Warehouse_Intv)                      /* Warehouse */
                    {
                        WarehouseInterval = int.Parse(settings.setting);
                    }
                    if (settings.name == Constants.Setting_DO_Intv)                      /* DO */
                    {
                        DOInterval = int.Parse(settings.setting);
                    }
                    if (settings.name == Constants.Setting_Post_Quotation_Intv)                      /* QUO */
                    {
                        PostQuotationInterval = int.Parse(settings.setting);
                    }
                    if (settings.name == Constants.Setting_Post_PObasket_Intv)                      /* PO BASKET */
                    {
                        PostPOBasketInterval = int.Parse(settings.setting);
                    }
                    if (settings.name == Constants.Setting_Post_Payment_Intv)                      /* POST PAYMENT */
                    {
                        PostPaymentInterval = int.Parse(settings.setting);
                    }
                    if (settings.name == Constants.Setting_SOSync_Intv)                      /* so sync */
                    {
                        SOSyncInterval = int.Parse(settings.setting);
                    }
                    if (settings.name == Constants.Setting_INV_Sales_Intv)                      /* Setting_INVSalesSync_Intv */
                    {
                        INVSalesInterval = int.Parse(settings.setting);
                    }
                    if (settings.name == Constants.Setting_CN_Sales_Intv)                      /*Setting_CNSalesSync_Intv*/
                    {
                        CNSalesInterval = int.Parse(settings.setting);
                    }
                    if (settings.name == Constants.Setting_DN_Sales_Intv)                      /* Setting_DNSalesSync_Intv */
                    {
                        DNSalesInterval = int.Parse(settings.setting);
                    }
                    
                    if (settings.name == Constants.Setting_Inv_Enable)
                    {
                        InvEnabled = settings.setting == Constants.YES;
                    }
                    if (settings.name == Constants.Setting_InvDtl_Enable)
                    {
                        InvDtlEnabled = settings.setting == Constants.YES;
                    }
                    if (settings.name == Constants.Setting_OutstandingSo_Enable)
                    {
                        OutsoEnabled = settings.setting == Constants.YES;
                    }
                    if (settings.name == Constants.Setting_CustomerAgent_Enable)
                    {
                        CustomerAgentEnabled = settings.setting == Constants.YES;
                    }
                    if (settings.name == Constants.Setting_Customer_Enable)
                    {
                        CustomerEnabled = settings.setting == Constants.YES;
                    }
                    if (settings.name == Constants.Setting_Stock_Enable) 
                    {
                        StockEnabled = settings.setting == Constants.YES;
                    }
                    if (settings.name == Constants.Setting_StockCategories_Enable) 
                    {
                        StockCategoriesEnabled = settings.setting == Constants.YES;
                    }
                    if (settings.name == Constants.Setting_StockUomPrice_Enable) 
                    {
                        StockUomPriceEnabled = settings.setting == Constants.YES;
                    }
                    if (settings.name == Constants.Setting_CreditNote_Enable) 
                    {
                        CreditNoteEnabled = settings.setting == Constants.YES;
                    }
                    if (settings.name == Constants.Setting_CreditNoteDtl_Enable)                /* CN DTL */
                    {
                        CreditNoteDtlEnabled = settings.setting == Constants.YES;
                    }
                    if (settings.name == Constants.Setting_DebitNote_Enable) 
                    {
                        DebitNoteEnabled = settings.setting == Constants.YES;
                    }
                    if (settings.name == Constants.Setting_CustomerRefund_Enable) 
                    {
                        CustomerRefundEnabled = settings.setting == Constants.YES;
                    }
                    if (settings.name == Constants.Setting_Receipt_Enable) 
                    {
                        ReceiptEnabled = settings.setting == Constants.YES;
                    }
                    if (settings.name == Constants.Setting_Post_SalesOrders_Enable) 
                    {
                        SalesOrdersEnabled = settings.setting == Constants.YES;
                    } 
                    if (settings.name == Constants.Setting_Post_CashSales_Enable) 
                    {
                        TransferCSEnabled = settings.setting == Constants.YES;
                    }
                    if (settings.name == Constants.Setting_Post_SalesInvoices_Enable)                           
                    {
                        PostSalesInvoicesEnabled = settings.setting == Constants.YES;
                    }
                    if (settings.name == Constants.Setting_Post_SalesCNs_Enable)                        //POST CNs              
                    {
                        PostSalesCNsEnabled = settings.setting == Constants.YES;
                    }
                    if (settings.name == Constants.Setting_Branch_Enable)                                       
                    {
                        BranchEnabled = settings.setting == Constants.YES;
                    }
                    if (settings.name == Constants.Setting_ProductSpecialPrice_Enable)                          
                    {
                        ProductSpecialPriceEnabled = settings.setting == Constants.YES;
                    }
                    if (settings.name == Constants.Setting_ReadImage_Enable)                    /*READ IMAGES FROM SQLACC*/                 
                    {
                        ReadImageEnabled = settings.setting == Constants.YES;
                    }
                    if (settings.name == Constants.Setting_ProductGroup_Enable)                    /* PRODUCT GROUP */
                    {
                        ProductGroupEnabled = settings.setting == Constants.YES;
                    }
                    if (settings.name == Constants.Setting_StockCard_Enable)                    /* Stock Card */
                    {
                        StockCardEnabled = settings.setting == Constants.YES;
                    }
                    if (settings.name == Constants.Setting_ItemTemplate_Enable)                    /*ITEM TEMPLATE*/
                    {
                        ItemTemplateEnabled = settings.setting == Constants.YES;
                    }
                    if (settings.name == Constants.Setting_ItemTemplateDtl_Enable)                    /*ITEM TEMPLATE DTL*/
                    {
                        ItemTemplateDtlEnabled = settings.setting == Constants.YES;
                    }
                    if (settings.name == Constants.Setting_CostPrice_Enable)                        /* COST PRICE */
                    {
                        CostPriceEnabled = settings.setting == Constants.YES;
                    }
                    if (settings.name == Constants.Setting_PostStock_Enable)                        /* Post Stock */
                    {
                        PostStockEnabled = settings.setting == Constants.YES;
                    }
                    if (settings.name == Constants.Setting_KnockOff_Enable)                        /* KnockOff */
                    {
                        KnockOffEnabled = settings.setting == Constants.YES;
                    }
                    if (settings.name == Constants.Setting_Warehouse_Enable)                        /* Warehouse */
                    {
                        WarehouseEnabled = settings.setting == Constants.YES;
                    }
                    if (settings.name == Constants.Setting_DO_Enable)                        /* DO */
                    {
                        DOEnabled = settings.setting == Constants.YES;
                    }
                    if (settings.name == Constants.Setting_Post_Quotation_Enable)                        /* QUO */
                    {
                        PostQuotationEnabled = settings.setting == Constants.YES;
                    }
                    if (settings.name == Constants.Setting_Post_PObasket_Enable)                        /* PO Basket */
                    {
                        PostPOBasketEnabled = settings.setting == Constants.YES;
                    }
                    if (settings.name == Constants.Setting_Post_Payment_Enable)                        /* POST PAYMENT */
                    {
                        PostPaymentEnabled = settings.setting == Constants.YES;
                    }
                    if (settings.name == Constants.Setting_SOSync_Enable)                        /* so sync */
                    {
                        SOSyncEnabled = settings.setting == Constants.YES;
                    }
                    if (settings.name == Constants.Setting_INV_Sales_Enable)                        /* INV_Sales */
                    {
                        INVSalesEnabled = settings.setting == Constants.YES;
                    }
                    if (settings.name == Constants.Setting_CN_Sales_Enable)                        /* CN_Sales*/
                    {
                        CNSalesEnabled = settings.setting == Constants.YES;
                    }
                    if (settings.name == Constants.Setting_DN_Sales_Enable)                        /* DN_Sales */
                    {
                        DNSalesEnabled = settings.setting == Constants.YES;
                    }
                    if (settings.name == Constants.Setting_TransferSO_SDK_Enable)                        /* TransferSO_SDK */
                    {
                        ATCSDKEnabled = settings.setting == Constants.YES;
                    }
                    if (settings.name == Constants.Setting_ATCv2)                        /* ATCV2Enabled */
                    {
                        ATCV2Enabled = settings.setting == Constants.YES;
                    }
                }

                DateTimeOffset dtNow = SystemTime.Now();

                GlobalLogger logger = new GlobalLogger();

                StdSchedulerFactory factory = new StdSchedulerFactory(props);
                IScheduler scheduler = await factory.GetScheduler();

                List<DpprAccountingSoftware> getAccSoftware = LocalDB.GetAccountingSoftwares();
                DpprAccountingSoftware accSoftware = getAccSoftware[0];

                var jobListener = new JobListener();
                scheduler.ListenerManager.AddJobListener(jobListener, GroupMatcher<JobKey>.AnyGroup());

                if (INVSalesEnabled)                                                               /* invsalesjob */
                {
                    if (accSoftware.software_name == "SQLAccounting")
                    {
                        IJobDetail invsalesjob = JobBuilder.Create<JobInvoiceSalesSync>()
                        .WithIdentity(Constants.Job_INVSales, Constants.Job_Group_Sync)
                        .Build();

                        ITrigger invsalestrigger = TriggerBuilder.Create()
                           .WithIdentity(Constants.Trigger_INVSales, Constants.Job_Group_Sync)
                           .StartAt(dtNow.AddSeconds(GetSecondsFromMinute(INVSalesInterval)))
                           .WithSimpleSchedule(x => x
                               .WithIntervalInSeconds(GetSecondsFromMinute(INVSalesInterval))
                               .RepeatForever())
                           .Build();

                        logger.message = string.Format("INV Sales trigger is set with interval of {0} min", INVSalesInterval);
                        logger.Broadcast();

                        await scheduler.ScheduleJob(invsalesjob, invsalestrigger);
                        ActiveJobs++;
                    }
                    else
                    {
                        //dont assign to any job
                    }
                } 
                
                if (CNSalesEnabled)                                                               /* CN sales job */
                {
                    if (accSoftware.software_name == "SQLAccounting")
                    {
                        IJobDetail cnsalesjob = JobBuilder.Create<JobCNSalesSync>()
                        .WithIdentity(Constants.Job_CNSales, Constants.Job_Group_Sync)
                        .Build();

                        ITrigger cnsalestrigger = TriggerBuilder.Create()
                           .WithIdentity(Constants.Trigger_CNSales, Constants.Job_Group_Sync)
                           .StartAt(dtNow.AddSeconds(GetSecondsFromMinute(CNSalesInterval)))
                           .WithSimpleSchedule(x => x
                               .WithIntervalInSeconds(GetSecondsFromMinute(CNSalesInterval))
                               .RepeatForever())
                           .Build();

                        logger.message = string.Format("CN Sales trigger is set with interval of {0} min", CNSalesInterval);
                        logger.Broadcast();

                        await scheduler.ScheduleJob(cnsalesjob, cnsalestrigger);
                        ActiveJobs++;
                    }
                    else
                    {
                        //dont assign to any job
                    }
                    
                } 
                
                if (DNSalesEnabled)                                                               /* invsalesjob */
                {
                    if (accSoftware.software_name == "SQLAccounting")
                    {
                        IJobDetail dnsalesjob = JobBuilder.Create<JobDNSalesSync>()
                            .WithIdentity(Constants.Job_DNSales, Constants.Job_Group_Sync)
                            .Build();

                        ITrigger dnsalestrigger = TriggerBuilder.Create()
                           .WithIdentity(Constants.Trigger_DNSales, Constants.Job_Group_Sync)
                           .StartAt(dtNow.AddSeconds(GetSecondsFromMinute(DNSalesInterval)))
                           .WithSimpleSchedule(x => x
                               .WithIntervalInSeconds(GetSecondsFromMinute(DNSalesInterval))
                               .RepeatForever())
                           .Build();

                        logger.message = string.Format("DN Sales trigger is set with interval of {0} min", DNSalesInterval);
                        logger.Broadcast();

                        await scheduler.ScheduleJob(dnsalesjob, dnsalestrigger);
                        ActiveJobs++;
                    }
                    else
                    {
                        //dont assign to any job
                    }
                }

                if (CustomerRefundEnabled)                                                               /* CF */
                {
                    if (accSoftware.software_name == "SQLAccounting")
                    {
                        IJobDetail cfjob = JobBuilder.Create<JobCFSync>()
                        .WithIdentity(Constants.Job_CF, Constants.Job_Group_Sync)
                        .Build();

                        ITrigger cftrigger = TriggerBuilder.Create()
                           .WithIdentity(Constants.Trigger_CF, Constants.Job_Group_Sync)
                           .StartAt(dtNow.AddSeconds(GetSecondsFromMinute(CustomerRefundInterval)))
                           .WithSimpleSchedule(x => x
                               .WithIntervalInSeconds(GetSecondsFromMinute(CustomerRefundInterval))
                               .RepeatForever())
                           .Build();

                        logger.message = string.Format("Customer refund trigger is set with interval of {0} min", CustomerRefundInterval);
                        logger.Broadcast();

                        await scheduler.ScheduleJob(cfjob, cftrigger);
                        ActiveJobs++;
                    }
                    else
                    {
                        //dont assign to any job
                    }
                }
                
                if (SOSyncEnabled)                                                               /* CF */
                {
                    if (accSoftware.software_name == "SQLAccounting")
                    {
                        IJobDetail sosyncjob = JobBuilder.Create<JobSOSync>()
                        .WithIdentity(Constants.Job_SO, Constants.Job_Group_Sync)
                        .Build();

                        ITrigger sosynctrigger = TriggerBuilder.Create()
                           .WithIdentity(Constants.Trigger_SO, Constants.Job_Group_Sync)
                           .StartAt(dtNow.AddSeconds(GetSecondsFromMinute(SOSyncInterval)))
                           .WithSimpleSchedule(x => x
                               .WithIntervalInSeconds(GetSecondsFromMinute(SOSyncInterval))
                               .RepeatForever())
                           .Build();

                        logger.message = string.Format("Sales Order sync trigger is set with interval of {0} min", SOSyncInterval);
                        logger.Broadcast();

                        await scheduler.ScheduleJob(sosyncjob, sosynctrigger);
                        ActiveJobs++;
                    }
                    else
                    {
                        //dont assign to any job
                    }
                }
                
                if (DOEnabled)                                                               /* do */
                {
                    if(accSoftware.software_name == "SQLAccounting")
                    {
                        IJobDetail dojob = JobBuilder.Create<JobDOSync>()
                        .WithIdentity(Constants.Job_DO, Constants.Job_Group_Sync)
                        .Build();

                        ITrigger dotrigger = TriggerBuilder.Create()
                           .WithIdentity(Constants.Trigger_DO, Constants.Job_Group_Sync)
                           .StartAt(dtNow.AddSeconds(GetSecondsFromMinute(DOInterval)))
                           .WithSimpleSchedule(x => x
                               .WithIntervalInSeconds(GetSecondsFromMinute(DOInterval))
                               .RepeatForever())
                           .Build();

                        logger.message = string.Format("SQL Accounting delivery order trigger is set with interval of {0} min", DOInterval);
                        logger.Broadcast();

                        await scheduler.ScheduleJob(dojob, dotrigger);
                        ActiveJobs++;
                    }
                    else if (accSoftware.software_name == "APS")
                    {
                        IJobDetail dojob = JobBuilder.Create<JobAPSDOSync>()
                        .WithIdentity(Constants.Job_DO, Constants.Job_Group_Sync)
                        .Build();

                        ITrigger dotrigger = TriggerBuilder.Create()
                           .WithIdentity(Constants.Trigger_DO, Constants.Job_Group_Sync)
                           .StartAt(dtNow.AddSeconds(GetSecondsFromMinute(DOInterval)))
                           .WithSimpleSchedule(x => x
                               .WithIntervalInSeconds(GetSecondsFromMinute(DOInterval))
                               .RepeatForever())
                           .Build();

                        logger.message = string.Format("APS delivery order trigger is set with interval of {0} min", DOInterval);
                        logger.Broadcast();

                        await scheduler.ScheduleJob(dojob, dotrigger);

                        IJobDetail dodtljob = JobBuilder.Create<JobAPSDODetailSync>()
                            .WithIdentity(Constants.Job_DO_Dtl, Constants.Job_Group_Sync)
                            .Build();

                        ITrigger dodtltrigger = TriggerBuilder.Create()
                           .WithIdentity(Constants.Trigger_DO_Dtl, Constants.Job_Group_Sync)
                           .StartAt(dtNow.AddSeconds(GetSecondsFromMinute(DOInterval)))
                           .WithSimpleSchedule(x => x
                               .WithIntervalInSeconds(GetSecondsFromMinute(DOInterval))
                               .RepeatForever())
                           .Build();

                        logger.message = string.Format("APS delivery order details trigger is set with interval of {0} min", DOInterval);
                        logger.Broadcast();

                        await scheduler.ScheduleJob(dodtljob, dodtltrigger);
                        ActiveJobs++;
                    }
                    else
                    {
                        //dont assign to any job
                    }
                }

                if (CostPriceEnabled)
                {
                    if (accSoftware.software_name == "SQLAccounting")
                    {
                        IJobDetail costpricejob = JobBuilder.Create<JobCostPriceSync>()
                        .WithIdentity(Constants.Job_CostPrice, Constants.Job_Group_Sync)
                        .Build();

                        ITrigger costpricetrigger = TriggerBuilder.Create()
                           .WithIdentity(Constants.Trigger_CostPrice, Constants.Job_Group_Sync)
                           .StartAt(dtNow.AddSeconds(GetSecondsFromMinute(CostPriceInterval)))
                           .WithSimpleSchedule(x => x
                               .WithIntervalInSeconds(GetSecondsFromMinute(CostPriceInterval))
                               .RepeatForever())
                           .Build();

                        logger.message = string.Format("Cost price trigger is set with interval of {0} min", CostPriceInterval);
                        logger.Broadcast();

                        await scheduler.ScheduleJob(costpricejob, costpricetrigger);
                        ActiveJobs++;
                    }
                    else
                    {
                        //dont assign to any job
                    }
                }
                
                if (PostQuotationEnabled)
                {
                    if (accSoftware.software_name == "APS")
                    {
                        IJobDetail postquojob = JobBuilder.Create<JobAPSTransferQuotation>()
                        .WithIdentity(Constants.Job_APSTransferQuo, Constants.Job_Group_Sync)
                        .Build();

                        ITrigger postquotrigger = TriggerBuilder.Create()
                           .WithIdentity(Constants.Trigger_APSTransferQuo, Constants.Job_Group_Sync)
                           .StartAt(dtNow.AddSeconds(GetSecondsFromMinute(PostQuotationInterval)))
                           .WithSimpleSchedule(x => x
                               .WithIntervalInSeconds(GetSecondsFromMinute(PostQuotationInterval))
                               .RepeatForever())
                           .Build();

                        logger.message = string.Format("APS transfer quotation trigger is set with interval of {0} min", PostQuotationInterval);
                        logger.Broadcast();

                        await scheduler.ScheduleJob(postquojob, postquotrigger);
                        ActiveJobs++;
                    }
                    else
                    {
                        //dont assign to any job
                    }
                }
                
                if (PostPOBasketEnabled)
                {
                    if (accSoftware.software_name == "APS")
                    {
                        IJobDetail postpobasketjob = JobBuilder.Create<JobAPSTransferPOBasket>()
                        .WithIdentity(Constants.Job_APSTransferPOBasket, Constants.Job_Group_Sync)
                        .Build();

                        ITrigger postpobaskettrigger = TriggerBuilder.Create()
                           .WithIdentity(Constants.Job_APSTransferPOBasket, Constants.Job_Group_Sync)
                           .StartAt(dtNow.AddSeconds(GetSecondsFromMinute(PostPOBasketInterval)))
                           .WithSimpleSchedule(x => x
                               .WithIntervalInSeconds(GetSecondsFromMinute(PostPOBasketInterval))
                               .RepeatForever())
                           .Build();

                        logger.message = string.Format("APS transfer PO basket trigger is set with interval of {0} min", PostPOBasketInterval);
                        logger.Broadcast();

                        await scheduler.ScheduleJob(postpobasketjob, postpobaskettrigger);
                        ActiveJobs++;
                    }
                    else
                    {
                        //dont assign to any job
                    }
                }
                
                if (PostPaymentEnabled)
                {
                    if (accSoftware.software_name == "SQLAccounting")
                    {
                        IJobDetail postpaymentjob = JobBuilder.Create<JobPaymentTransfer>()
                        .WithIdentity(Constants.Job_Post_Payment, Constants.Job_Group_Sync)
                        .Build();

                        ITrigger postpaymenttrigger = TriggerBuilder.Create()
                           .WithIdentity(Constants.Trigger_Post_Payment, Constants.Job_Group_Sync)
                           //.StartAt(dtNow.AddSeconds(10))
                           .StartAt(dtNow.AddSeconds(GetSecondsFromMinute(PostPaymentInterval)))
                           .WithSimpleSchedule(x => x
                               .WithIntervalInSeconds(GetSecondsFromMinute(PostPaymentInterval))
                               .RepeatForever())
                           .Build();

                        logger.message = string.Format("SQLACC Transfer Payment trigger is set with interval of {0} min", PostPaymentInterval);
                        logger.Broadcast();

                        await scheduler.ScheduleJob(postpaymentjob, postpaymenttrigger);
                        ActiveJobs++;
                    }
                    else
                    {
                        //dont assign to any job
                    }
                }
                
                if (KnockOffEnabled)                                                               /* KnockOff */
                {
                    if (accSoftware.software_name == "SQLAccounting")
                    {
                        IJobDetail knockoffjob = JobBuilder.Create<JobKnockOffSync>()
                        .WithIdentity(Constants.Job_KnockOff, Constants.Job_Group_Sync)
                        .Build();

                        ITrigger knockofftrigger = TriggerBuilder.Create()
                           .WithIdentity(Constants.Trigger_KnockOff, Constants.Job_Group_Sync)
                           .StartAt(dtNow.AddSeconds(GetSecondsFromMinute(KnockOffInterval)))
                           .WithSimpleSchedule(x => x
                               .WithIntervalInSeconds(GetSecondsFromMinute(KnockOffInterval))
                               .RepeatForever())
                           .Build();

                        logger.message = string.Format("SQLAcc Knock off trigger is set with interval of {0} min", KnockOffInterval);
                        logger.Broadcast();

                        await scheduler.ScheduleJob(knockoffjob, knockofftrigger);
                        ActiveJobs++;
                    }
                    else if (accSoftware.software_name == "QNE")
                    {
                        //don't have yet
                    }
                    else if (accSoftware.software_name == "APS")
                    {
                        IJobDetail apskorcptjob = JobBuilder.Create<JobAPSReceiptKOSync>()
                        .WithIdentity(Constants.Job_APSKORcpt, Constants.Job_Group_Sync)
                        .Build();

                        ITrigger apskorcpttrigger = TriggerBuilder.Create()
                           .WithIdentity(Constants.Trigger_APSKORcpt, Constants.Job_Group_Sync)
                           .StartAt(dtNow.AddSeconds(GetSecondsFromMinute(KnockOffInterval)))
                           .WithSimpleSchedule(x => x
                               .WithIntervalInSeconds(GetSecondsFromMinute(KnockOffInterval))
                               .RepeatForever())
                           .Build();

                        logger.message = string.Format("APS KO Receipt sync trigger is set with interval of {0} min", KnockOffInterval);
                        logger.Broadcast();

                        await scheduler.ScheduleJob(apskorcptjob, apskorcpttrigger);
                        ActiveJobs++;
                        
                        IJobDetail apskocnjob = JobBuilder.Create<JobAPSCreditNoteKOSync>()
                        .WithIdentity(Constants.Job_APSKOCN, Constants.Job_Group_Sync)
                        .Build();

                        ITrigger apskocntrigger = TriggerBuilder.Create()
                           .WithIdentity(Constants.Trigger_APSKOCN, Constants.Job_Group_Sync)
                           .StartAt(dtNow.AddSeconds(GetSecondsFromMinute(KnockOffInterval)))
                           .WithSimpleSchedule(x => x
                               .WithIntervalInSeconds(GetSecondsFromMinute(KnockOffInterval))
                               .RepeatForever())
                           .Build();

                        logger.message = string.Format("APS KO CN sync trigger is set with interval of {0} min", KnockOffInterval);
                        logger.Broadcast();

                        await scheduler.ScheduleJob(apskocnjob, apskocntrigger);
                        ActiveJobs++;
                    }
                    else
                    {
                        //dont assign to any job
                    }
                }
                
                if (PostStockEnabled)                                                               /* CASH SALES */
                {
                    if (accSoftware.software_name == "SQLAccounting")
                    {
                        IJobDetail transferstockjob = JobBuilder.Create<JobStockTransfer>()
                        .WithIdentity(Constants.Job_StockTransfer, Constants.Job_Group_Sync)
                        .Build();

                        ITrigger transferstocktrigger = TriggerBuilder.Create()
                           .WithIdentity(Constants.Trigger_StockTransfer, Constants.Job_Group_Sync)
                           .StartAt(dtNow.AddSeconds(GetSecondsFromMinute(PostStockInterval)))
                           .WithSimpleSchedule(x => x
                               .WithIntervalInSeconds(GetSecondsFromMinute(PostStockInterval))
                               .RepeatForever())
                           .Build();

                        logger.message = string.Format("Stock Transfer trigger is set with interval of {0} min", PostStockInterval);
                        logger.Broadcast();

                        await scheduler.ScheduleJob(transferstockjob, transferstocktrigger);
                        ActiveJobs++;
                    }
                    else if (accSoftware.software_name == "AutoCount")
                    {
                        IJobDetail transferstockjob = JobBuilder.Create<JobATCTransferStockSDK>()
                        .WithIdentity(Constants.Job_ATCStockTransferSync, Constants.Job_Group_Sync)
                        .Build();

                        ITrigger transferstocktrigger = TriggerBuilder.Create()
                           .WithIdentity(Constants.Trigger_ATCTransferStockSync, Constants.Job_Group_Sync)
                           .StartAt(dtNow.AddSeconds(GetSecondsFromMinute(PostStockInterval)))
                           .WithSimpleSchedule(x => x
                               .WithIntervalInSeconds(GetSecondsFromMinute(PostStockInterval))
                               .RepeatForever())
                           .Build();

                        logger.message = string.Format("Stock Transfer trigger is set with interval of {0} min", PostStockInterval);
                        logger.Broadcast();

                        await scheduler.ScheduleJob(transferstockjob, transferstocktrigger);
                        ActiveJobs++;
                    }
                    else
                    {
                        //dont assign to any job
                    }
                }

                if (StockCardEnabled)
                {
                    if (accSoftware.software_name == "SQLAccounting")
                    {
                        IJobDetail stockcardjob = JobBuilder.Create<JobStockCardSync>()
                       .WithIdentity(Constants.Job_StockCard, Constants.Job_Group_Sync)
                       .Build();

                        ITrigger stockcardtrigger = TriggerBuilder.Create()
                           .WithIdentity(Constants.Trigger_StockCard, Constants.Job_Group_Sync)
                           .StartAt(dtNow.AddSeconds(GetSecondsFromMinute(StockCardInterval)))
                           .WithSimpleSchedule(x => x
                               .WithIntervalInSeconds(GetSecondsFromMinute(StockCardInterval))
                               .RepeatForever())
                           .Build();

                        logger.message = string.Format("SQLAcc Stock card trigger is set with interval of {0} min", StockCardInterval);
                        logger.Broadcast();

                        await scheduler.ScheduleJob(stockcardjob, stockcardtrigger);
                        ActiveJobs++;
                    }
                    else if (accSoftware.software_name == "AutoCount")
                    {
                        IJobDetail stockcardjob = JobBuilder.Create<JobATCStockCardSync>()
                       .WithIdentity(Constants.Job_StockCard, Constants.Job_Group_Sync)
                       .Build();

                        ITrigger stockcardtrigger = TriggerBuilder.Create()
                           .WithIdentity(Constants.Trigger_StockCard, Constants.Job_Group_Sync)
                           .StartAt(dtNow.AddSeconds(GetSecondsFromMinute(StockCardInterval)))
                           .WithSimpleSchedule(x => x
                               .WithIntervalInSeconds(GetSecondsFromMinute(StockCardInterval))
                               .RepeatForever())
                           .Build();

                        logger.message = string.Format("ATC Stock card trigger is set with interval of {0} min", StockCardInterval);
                        logger.Broadcast();

                        await scheduler.ScheduleJob(stockcardjob, stockcardtrigger);
                        ActiveJobs++;
                    }
                    else
                    {
                        //dont assign any job
                    }
                }

                if (ProductGroupEnabled)
                {
                    IJobDetail productgroupjob = JobBuilder.Create<JobProductGroupsSync>()
                        .WithIdentity(Constants.Job_ProductGroups, Constants.Job_Group_Sync)
                        .Build();

                    ITrigger productgrouptrigger = TriggerBuilder.Create()
                       .WithIdentity(Constants.Trigger_ProductGroups, Constants.Job_Group_Sync)
                       .StartAt(dtNow.AddSeconds(GetSecondsFromMinute(ProductGroupInterval)))
                       .WithSimpleSchedule(x => x
                           .WithIntervalInSeconds(GetSecondsFromMinute(ProductGroupInterval))
                           .RepeatForever())
                       .Build();

                    logger.message = string.Format("Product group trigger is set with interval of {0} min", ProductGroupInterval);
                    logger.Broadcast();

                    await scheduler.ScheduleJob(productgroupjob, productgrouptrigger);
                    ActiveJobs++;
                }

                if (ItemTemplateEnabled)
                {
                    if (accSoftware.software_name == "SQLAccounting")
                    {
                        IJobDetail itemtemplatejob = JobBuilder.Create<JobItemTemplateSync>()
                        .WithIdentity(Constants.Job_ItemTemplate, Constants.Job_Group_Sync)
                        .Build();

                        ITrigger itemtemplatetrigger = TriggerBuilder.Create()
                           .WithIdentity(Constants.Trigger_ItemTemplate, Constants.Job_Group_Sync)
                           .StartAt(dtNow.AddSeconds(GetSecondsFromMinute(ItemTemplateInterval)))
                           .WithSimpleSchedule(x => x
                               .WithIntervalInSeconds(GetSecondsFromMinute(ItemTemplateInterval))
                               .RepeatForever())
                           .Build();

                        logger.message = string.Format("Item template trigger is set with interval of {0} min", ItemTemplateInterval);
                        logger.Broadcast();

                        await scheduler.ScheduleJob(itemtemplatejob, itemtemplatetrigger);
                        ActiveJobs++;
                    }
                    else if (accSoftware.software_name == "AutoCount")
                    {
                        IJobDetail itemtemplatejob = JobBuilder.Create<JobATCItemTemplateSync>()
                        .WithIdentity(Constants.Job_ATCItemTemplate, Constants.Job_Group_Sync)
                        .Build();

                        ITrigger itemtemplatetrigger = TriggerBuilder.Create()
                           .WithIdentity(Constants.Trigger_ATCItemTemplate, Constants.Job_Group_Sync)
                           .StartAt(dtNow.AddSeconds(GetSecondsFromMinute(ItemTemplateInterval)))
                           .WithSimpleSchedule(x => x
                               .WithIntervalInSeconds(GetSecondsFromMinute(ItemTemplateInterval))
                               .RepeatForever())
                           .Build();

                        logger.message = string.Format("ATC Item template trigger is set with interval of {0} min", ItemTemplateInterval);
                        logger.Broadcast();

                        await scheduler.ScheduleJob(itemtemplatejob, itemtemplatetrigger);
                        ActiveJobs++;
                    }
                    else
                    {

                    }
                }

                if (ItemTemplateDtlEnabled)
                {
                    if (accSoftware.software_name == "SQLAccounting")
                    {
                        IJobDetail itemtemplatedtljob = JobBuilder.Create<JobItemTemplateDtlSync>()
                        .WithIdentity(Constants.Job_ItemTemplateDtl, Constants.Job_Group_Sync)
                        .Build();

                        ITrigger itemtemplatedtltrigger = TriggerBuilder.Create()
                           .WithIdentity(Constants.Trigger_ItemTemplateDtl, Constants.Job_Group_Sync)
                           .StartAt(dtNow.AddSeconds(GetSecondsFromMinute(ItemTemplateDtlInterval)))
                           .WithSimpleSchedule(x => x
                               .WithIntervalInSeconds(GetSecondsFromMinute(ItemTemplateDtlInterval))
                               .RepeatForever())
                           .Build();

                        logger.message = string.Format("SQLAccounting Item template detail trigger is set with interval of {0} min", ItemTemplateDtlInterval);
                        logger.Broadcast();

                        await scheduler.ScheduleJob(itemtemplatedtljob, itemtemplatedtltrigger);
                        ActiveJobs++;
                    }
                    else if (accSoftware.software_name == "AutoCount")
                    {
                        IJobDetail itemtemplatedtljob = JobBuilder.Create<JobATCItemTemplateDtlSync>()
                        .WithIdentity(Constants.Job_ATCItemTemplateDtl, Constants.Job_Group_Sync)
                        .Build();

                        ITrigger itemtemplatedtltrigger = TriggerBuilder.Create()
                           .WithIdentity(Constants.Trigger_ATCItemTemplateDtl, Constants.Job_Group_Sync)
                           .StartAt(dtNow.AddSeconds(GetSecondsFromMinute(ItemTemplateDtlInterval)))
                           .WithSimpleSchedule(x => x
                               .WithIntervalInSeconds(GetSecondsFromMinute(ItemTemplateDtlInterval))
                               .RepeatForever())
                           .Build();

                        logger.message = string.Format("ATC Item template detail trigger is set with interval of {0} min", ItemTemplateDtlInterval);
                        logger.Broadcast();

                        await scheduler.ScheduleJob(itemtemplatedtljob, itemtemplatedtltrigger);
                        ActiveJobs++;
                    }
                    else
                    {

                    }
                }

                if (ReadImageEnabled)
                {
                    if (accSoftware.software_name == "SQLAccounting")
                    {
                        IJobDetail readimagejob = JobBuilder.Create<JobReadImageSync>()
                        .WithIdentity(Constants.Job_ReadImage, Constants.Job_Group_Sync)
                        .Build();

                        ITrigger readimagetrigger = TriggerBuilder.Create()
                           .WithIdentity(Constants.Trigger_ReadImage, Constants.Job_Group_Sync)
                           .StartAt(dtNow.AddSeconds(GetSecondsFromMinute(ReadImageInterval)))
                           .WithSimpleSchedule(x => x
                               .WithIntervalInSeconds(GetSecondsFromMinute(ReadImageInterval))
                               .RepeatForever())
                           .Build();

                        logger.message = string.Format("Read image trigger is set with interval of {0} min", ReadImageInterval);
                        logger.Broadcast();

                        await scheduler.ScheduleJob(readimagejob, readimagetrigger);
                        ActiveJobs++;
                    }
                    else if(accSoftware.software_name == "QNE")
                    {
                        //only for GMHQ migration to GMJOHOR image
                        IJobDetail readimagejob = JobBuilder.Create<TranferImgGM>()
                        .WithIdentity(Constants.Job_ReadImage, Constants.Job_Group_Sync)
                        .Build();

                        ITrigger readimagetrigger = TriggerBuilder.Create()
                           .WithIdentity(Constants.Trigger_ReadImage, Constants.Job_Group_Sync)
                           .StartAt(dtNow.AddSeconds(GetSecondsFromMinute(ReadImageInterval)))
                           .WithSimpleSchedule(x => x
                               .WithIntervalInSeconds(GetSecondsFromMinute(ReadImageInterval))
                               .RepeatForever())
                           .Build();

                        logger.message = string.Format("Sync image trigger is set with interval of {0} min", ReadImageInterval);
                        logger.Broadcast();

                        await scheduler.ScheduleJob(readimagejob, readimagetrigger);
                        ActiveJobs++;
                    }
                    else if (accSoftware.software_name == "APS")
                    {
                        IJobDetail apsimagejob = JobBuilder.Create<JobAPSImageSync>()
                        .WithIdentity(Constants.Job_APSImage, Constants.Job_Group_Sync)
                        .Build();

                        ITrigger apsimagetrigger = TriggerBuilder.Create()
                           .WithIdentity(Constants.Trigger_APSImage, Constants.Job_Group_Sync)
                           .StartAt(dtNow.AddSeconds(GetSecondsFromMinute(ReadImageInterval)))
                           .WithSimpleSchedule(x => x
                               .WithIntervalInSeconds(GetSecondsFromMinute(ReadImageInterval))
                               .RepeatForever())
                           .Build();

                        logger.message = string.Format("APS image sync trigger is set with interval of {0} min", ReadImageInterval);
                        logger.Broadcast();

                        await scheduler.ScheduleJob(apsimagejob, apsimagetrigger);
                        ActiveJobs++;
                    }
                    else
                    {
                        //dont assign to any job
                    }
                }


                if (SalesOrdersEnabled) 
                {
                    if (accSoftware.software_name == "SQLAccounting")
                    {
                        IJobDetail transfersojob = JobBuilder.Create<JobSoTransfer>()
                        .WithIdentity(Constants.Job_Transfer_SO, Constants.Job_Group_Transfer)
                        .Build();

                        ITrigger transfersotrigger = TriggerBuilder.Create()                                
                           .WithIdentity(Constants.Trigger_Transfer_SO, Constants.Job_Group_Transfer)
                           .StartAt(dtNow.AddSeconds(GetSecondsFromMinute(SalesOrdersInterval)))
                           .WithSimpleSchedule(x => x
                               .WithIntervalInSeconds(GetSecondsFromMinute(SalesOrdersInterval))
                               .RepeatForever())
                           .Build();

                        logger.message = string.Format("Transfer Sales Order trigger is set with interval of {0} min", SalesOrdersInterval);
                        logger.Broadcast();

                        await scheduler.ScheduleJob(transfersojob, transfersotrigger); 
                        ActiveJobs++;
                    }
                    else if (accSoftware.software_name == "QNE")
                    {
                        IJobDetail salesordersjob = JobBuilder.Create<JobPostSalesOrders>()
                        .WithIdentity(Constants.Job_Post_SalesOrders, Constants.Job_Group_Transfer) 
                        .Build();

                        ITrigger salesorderstrigger = TriggerBuilder.Create()
                          .WithIdentity(Constants.Trigger_Post_SalesOrders, Constants.Job_Group_Transfer)
                          .StartAt(dtNow.AddSeconds(GetSecondsFromMinute(SalesOrdersInterval)))
                          .WithSimpleSchedule(x => x
                              .WithIntervalInSeconds(GetSecondsFromMinute(SalesOrdersInterval))
                              .RepeatForever())
                          .Build();

                        logger.message = string.Format("POST Sales Order trigger is set with interval of {0} min", SalesOrdersInterval);
                        logger.Broadcast();

                        await scheduler.ScheduleJob(salesordersjob, salesorderstrigger);
                        ActiveJobs++;
                    }
                    else if (accSoftware.software_name == "APS")
                    {
                        IJobDetail apstransfersojob = JobBuilder.Create<JobAPSTransferSO>()
                        .WithIdentity(Constants.Job_APSTransferSO, Constants.Job_Group_Transfer)
                        .Build();

                        ITrigger apstransfersotrigger = TriggerBuilder.Create()
                          .WithIdentity(Constants.Trigger_APSTransferSO, Constants.Job_Group_Transfer)
                          .StartAt(dtNow.AddSeconds(GetSecondsFromMinute(SalesOrdersInterval)))
                          .WithSimpleSchedule(x => x
                              .WithIntervalInSeconds(GetSecondsFromMinute(SalesOrdersInterval))
                              .RepeatForever())
                          .Build();

                        logger.message = string.Format("APS transfer SO trigger is set with interval of {0} min", SalesOrdersInterval);
                        logger.Broadcast();

                        await scheduler.ScheduleJob(apstransfersojob, apstransfersotrigger);
                        ActiveJobs++;
                    }
                    else if (accSoftware.software_name == "AutoCount")
                    {
                        if (ATCSDKEnabled)
                        {
                            IJobDetail atctransfersosdkjob = JobBuilder.Create<JobATCTransferSOSDK>()
                        .WithIdentity(Constants.Job_ATCTransferSOSDK, Constants.Job_Group_Transfer)
                        .Build();

                            ITrigger atctransfersosdktrigger = TriggerBuilder.Create()
                              .WithIdentity(Constants.Trigger_ATCTransferSOSDK, Constants.Job_Group_Transfer)
                              .StartAt(dtNow.AddSeconds(GetSecondsFromMinute(SalesOrdersInterval)))
                              .WithSimpleSchedule(x => x
                                  .WithIntervalInSeconds(GetSecondsFromMinute(SalesOrdersInterval))
                                  .RepeatForever())
                              .Build();

                            logger.message = string.Format("[SDK] ATC transfer SO trigger is set with interval of {0} min", SalesOrdersInterval);
                            logger.Broadcast();

                            await scheduler.ScheduleJob(atctransfersosdkjob, atctransfersosdktrigger);
                            ActiveJobs++;
                        }
                        else
                        {
                            IJobDetail atctransfersojob = JobBuilder.Create<JobATCTransferSO>()
                        .WithIdentity(Constants.Job_ATCTransferSO, Constants.Job_Group_Transfer)
                        .Build();

                            ITrigger atctransfersotrigger = TriggerBuilder.Create()
                              .WithIdentity(Constants.Trigger_ATCTransferSO, Constants.Job_Group_Transfer)
                              .StartAt(dtNow.AddSeconds(GetSecondsFromMinute(SalesOrdersInterval)))
                              .WithSimpleSchedule(x => x
                                  .WithIntervalInSeconds(GetSecondsFromMinute(SalesOrdersInterval))
                                  .RepeatForever())
                              .Build();

                            logger.message = string.Format("ATC transfer SO trigger is set with interval of {0} min", SalesOrdersInterval);
                            logger.Broadcast();

                            await scheduler.ScheduleJob(atctransfersojob, atctransfersotrigger);
                            ActiveJobs++;
                        }
                    }
                    else if (accSoftware.software_name == "Sage UBS")
                    {
                        logger.message = string.Format("[SAGE UBS] transfer SO trigger is set with interval of {0} min", SalesOrdersInterval);
                        logger.Broadcast();
                    }
                    else
                    {
                        //dont assign to any job
                    }
                }

                if (TransferCSEnabled)
                {
                    if (accSoftware.software_name == "SQLAccounting")
                    {
                        IJobDetail transfercsjob = JobBuilder.Create<JobCSTransfer>()
                    .WithIdentity(Constants.Job_Transfer_CS, Constants.Job_Group_Sync)
                    .Build();

                        ITrigger transfercstrigger = TriggerBuilder.Create()
                           .WithIdentity(Constants.Trigger_Transfer_CS, Constants.Job_Group_Sync)
                           .StartAt(dtNow.AddSeconds(GetSecondsFromMinute(TransferCSInterval)))
                           .WithSimpleSchedule(x => x
                               .WithIntervalInSeconds(GetSecondsFromMinute(TransferCSInterval))
                               .RepeatForever())
                           .Build();

                        logger.message = string.Format("Transfer Cash Sales trigger is set with interval of {0} min", TransferCSInterval);
                        logger.Broadcast();

                        await scheduler.ScheduleJob(transfercsjob, transfercstrigger);
                        ActiveJobs++;
                    }
                    else if (accSoftware.software_name == "AutoCount")
                    {
                        if(ATCSDKEnabled)
                        {
                            IJobDetail transfercsjob = JobBuilder.Create<JobATCTransferCSSDK>()
                    .WithIdentity(Constants.Job_Transfer_CS, Constants.Job_Group_Sync)
                    .Build();

                            ITrigger transfercstrigger = TriggerBuilder.Create()
                               .WithIdentity(Constants.Trigger_Transfer_CS, Constants.Job_Group_Sync)
                               .StartAt(dtNow.AddSeconds(GetSecondsFromMinute(TransferCSInterval)))
                               .WithSimpleSchedule(x => x
                                   .WithIntervalInSeconds(GetSecondsFromMinute(TransferCSInterval))
                                   .RepeatForever())
                               .Build();

                            logger.message = string.Format("ATC Transfer Cash Sales (SDK) trigger is set with interval of {0} min", TransferCSInterval);
                            logger.Broadcast();

                            await scheduler.ScheduleJob(transfercsjob, transfercstrigger);
                            ActiveJobs++;
                        }
                    }
                    else
                    {
                        //dont assign any job
                    }
                }

                if (CustomerAgentEnabled)
                {
                    if (accSoftware.software_name == "SQLAccounting")
                    {
                        IJobDetail salespersonjob = JobBuilder.Create<JobSalesPersonSync>()
                        .WithIdentity(Constants.Job_Salesperson, Constants.Job_Group_Sync)
                        .Build();

                        ITrigger salespersontrigger = TriggerBuilder.Create()
                            .WithIdentity(Constants.Trigger_Salesperson, Constants.Job_Group_Sync)
                            .StartAt(dtNow.AddSeconds(GetSecondsFromMinute(CustomerAgentInterval)))
                            .WithSimpleSchedule(x => x
                                .WithIntervalInSeconds(GetSecondsFromMinute(CustomerAgentInterval))
                                .RepeatForever())
                            .Build();

                        logger.message = string.Format("Salesperson trigger is set with interval of {0} min", CustomerAgentInterval);
                        logger.Broadcast();

                        await scheduler.ScheduleJob(salespersonjob, salespersontrigger);
                        ActiveJobs++;
                    }
                    else if (accSoftware.software_name == "QNE")
                    {
                        IJobDetail agentjob = JobBuilder.Create<JobAgentSync>()
                        .WithIdentity(Constants.Job_Agent, Constants.Job_Group_Sync)
                        .Build();

                        ITrigger agenttrigger = TriggerBuilder.Create()
                            .WithIdentity(Constants.Trigger_Agent, Constants.Job_Group_Sync)
                            .StartAt(dtNow.AddSeconds(GetSecondsFromMinute(CustomerAgentInterval)))
                            .WithSimpleSchedule(x => x
                                .WithIntervalInSeconds(GetSecondsFromMinute(CustomerAgentInterval))
                                .RepeatForever())
                            .Build();

                        logger.message = string.Format("Agent trigger is set with interval of {0} min", CustomerAgentInterval);
                        logger.Broadcast();

                        IJobDetail customeragentjob = JobBuilder.Create<JobCustomerAgentSync>()
                        .WithIdentity(Constants.Job_CustomerAgent, Constants.Job_Group_Sync)
                        .Build();

                        ITrigger customeragenttrigger = TriggerBuilder.Create()
                          .WithIdentity(Constants.Trigger_CustomerAgent, Constants.Job_Group_Sync)
                          .StartAt(dtNow.AddSeconds(GetSecondsFromMinute(CustomerAgentInterval)))
                          .WithSimpleSchedule(x => x
                              .WithIntervalInSeconds(GetSecondsFromMinute(CustomerAgentInterval))
                              .RepeatForever())
                          .Build();

                        logger.message = string.Format("Customer Agent trigger is set with interval of {0} min", CustomerAgentInterval);
                        logger.Broadcast();

                        await scheduler.ScheduleJob(agentjob, agenttrigger);
                        await scheduler.ScheduleJob(customeragentjob, customeragenttrigger);
                        ActiveJobs++;
                    }
                    else if (accSoftware.software_name == "APS")
                    {
                        IJobDetail apssalespersonjob = JobBuilder.Create<JobAPSSalespersonSync>()
                        .WithIdentity(Constants.Job_APSSalesperson, Constants.Job_Group_Sync)
                        .Build();

                        ITrigger apssalespersontrigger = TriggerBuilder.Create()
                            .WithIdentity(Constants.Trigger_APSSalesperson, Constants.Job_Group_Sync)
                            .StartAt(dtNow.AddSeconds(GetSecondsFromMinute(CustomerAgentInterval)))
                            .WithSimpleSchedule(x => x
                                .WithIntervalInSeconds(GetSecondsFromMinute(CustomerAgentInterval))
                                .RepeatForever())
                            .Build();

                        logger.message = string.Format("APS salesperson trigger is set with interval of {0} min", CustomerAgentInterval);
                        logger.Broadcast();

                        IJobDetail apssalespersoncustomerjob = JobBuilder.Create<JobAPSSalespersonCustomerSync>()
                        .WithIdentity(Constants.Job_APSSalespersonCustomer, Constants.Job_Group_Sync)
                        .Build();

                        ITrigger apssalespersoncustomertrigger = TriggerBuilder.Create()
                          .WithIdentity(Constants.Trigger_APSSalespersonCustomer, Constants.Job_Group_Sync)
                          .StartAt(dtNow.AddSeconds(GetSecondsFromMinute(CustomerAgentInterval)))
                          .WithSimpleSchedule(x => x
                              .WithIntervalInSeconds(GetSecondsFromMinute(CustomerAgentInterval))
                              .RepeatForever())
                          .Build();

                        logger.message = string.Format("APS customer Agent trigger is set with interval of {0} min", CustomerAgentInterval);
                        logger.Broadcast();

                        IJobDetail refjob = JobBuilder.Create<JobAPSRefSync>()
                        .WithIdentity(Constants.Job_Ref, Constants.Job_Group_Sync)
                        .Build();

                        ITrigger reftrigger = TriggerBuilder.Create()
                           .WithIdentity(Constants.Trigger_Ref, Constants.Job_Group_Sync)
                           .StartAt(dtNow.AddSeconds(GetSecondsFromMinute(CustomerAgentInterval)))
                           .WithSimpleSchedule(x => x
                               .WithIntervalInSeconds(GetSecondsFromMinute(CustomerAgentInterval))
                               .RepeatForever())
                           .Build();

                        logger.message = string.Format("APS ref trigger is set with interval of {0} min", CustomerAgentInterval);
                        logger.Broadcast();

                        await scheduler.ScheduleJob(apssalespersonjob, apssalespersontrigger);                                            
                        await scheduler.ScheduleJob(apssalespersoncustomerjob, apssalespersoncustomertrigger);
                        await scheduler.ScheduleJob(refjob, reftrigger);
                        ActiveJobs++;
                    }
                    else if (accSoftware.software_name == "AutoCount")
                    {
                        IJobDetail atcsalespersonjob = JobBuilder.Create<JobATCSalespersonSync>()
                       .WithIdentity(Constants.Job_ATCSalesperson, Constants.Job_Group_Sync)
                       .Build();

                        ITrigger atcsalespersontrigger = TriggerBuilder.Create()
                            .WithIdentity(Constants.Trigger_ATCSalesperson, Constants.Job_Group_Sync)
                            .StartAt(dtNow.AddSeconds(GetSecondsFromMinute(CustomerAgentInterval)))
                            .WithSimpleSchedule(x => x
                                .WithIntervalInSeconds(GetSecondsFromMinute(CustomerAgentInterval))
                                .RepeatForever())
                            .Build();

                        logger.message = string.Format("ATC salesperson trigger is set with interval of {0} min", CustomerAgentInterval);
                        logger.Broadcast();

                        IJobDetail atcsalespersoncustomerjob = JobBuilder.Create<JobATCSalespersonCustomerSync>()
                        .WithIdentity(Constants.Job_ATCSalespersonCustomer, Constants.Job_Group_Sync)
                        .Build();

                        ITrigger atcsalespersoncustomertrigger = TriggerBuilder.Create()
                          .WithIdentity(Constants.Trigger_ATCSalespersonCustomer, Constants.Job_Group_Sync)
                          .StartAt(dtNow.AddSeconds(GetSecondsFromMinute(CustomerAgentInterval)))
                          .WithSimpleSchedule(x => x
                              .WithIntervalInSeconds(GetSecondsFromMinute(CustomerAgentInterval))
                              .RepeatForever())
                          .Build();

                        logger.message = string.Format("ATC customer-agent trigger is set with interval of {0} min", CustomerAgentInterval);
                        logger.Broadcast();

                        await scheduler.ScheduleJob(atcsalespersonjob, atcsalespersontrigger);
                        await scheduler.ScheduleJob(atcsalespersoncustomerjob, atcsalespersoncustomertrigger);
                        ActiveJobs++;
                    }
                    else if (accSoftware.software_name == "Sage UBS")                               //####LATER CHANGE TO UBS JOB
                    {
                        logger.message = string.Format("[SAGE UBS] customer-agent trigger is set with interval of {0} min", CustomerAgentInterval);
                        logger.Broadcast();
                        ActiveJobs++;
                    }
                    else
                    {
                        //dont assign to any job
                    }
                }

                if (OutsoEnabled)
                {
                    if (accSoftware.software_name == "SQLAccounting")
                    {
                        IJobDetail ojob = JobBuilder.Create<JobOutstandingSOSync>()
                    .WithIdentity(Constants.Job_Outstanding, Constants.Job_Group_Sync)
                    .Build();

                        ITrigger otrigger = TriggerBuilder.Create()
                            .WithIdentity(Constants.Trigger_Outstanding, Constants.Job_Group_Sync)
                            .StartAt(dtNow.AddSeconds(GetSecondsFromMinute(OutstandingSoInterval)))
                            .WithSimpleSchedule(x => x
                                .WithIntervalInSeconds(GetSecondsFromMinute(OutstandingSoInterval))
                                .RepeatForever())
                            .Build();

                        logger.message = string.Format("Outstanding SO trigger is set with interval of {0} min", OutstandingSoInterval);
                        logger.Broadcast();

                        await scheduler.ScheduleJob(ojob, otrigger);
                        ActiveJobs++;
                    }
                    else if(accSoftware.software_name == "AutoCount")
                    {
                        IJobDetail ojob = JobBuilder.Create<JobATCOutstandingSO>()
                    .WithIdentity(Constants.Job_ATCOutSOSync, Constants.Job_Group_Sync)
                    .Build();

                        ITrigger otrigger = TriggerBuilder.Create()
                            .WithIdentity(Constants.Trigger_ATCOutSOSync, Constants.Job_Group_Sync)
                            .StartAt(dtNow.AddSeconds(GetSecondsFromMinute(OutstandingSoInterval)))
                            .WithSimpleSchedule(x => x
                                .WithIntervalInSeconds(GetSecondsFromMinute(OutstandingSoInterval))
                                .RepeatForever())
                            .Build();

                        logger.message = string.Format("ATC Outstanding SO trigger is set with interval of {0} min", OutstandingSoInterval);
                        logger.Broadcast();

                        await scheduler.ScheduleJob(ojob, otrigger);
                        ActiveJobs++;
                    }
                    else
                    {
                        //dont assign to any job
                    }
                }

                if (InvEnabled)
                {
                    if (accSoftware.software_name == "SQLAccounting")
                    {
                        IJobDetail ijob = JobBuilder.Create<JobInvoiceSync>()
                        .WithIdentity(Constants.Job_Invoice, Constants.Job_Group_Sync)
                        .Build();

                        ITrigger itrigger = TriggerBuilder.Create()
                            .WithIdentity(Constants.Trigger_Invoice, Constants.Job_Group_Sync)
                            .StartAt(dtNow.AddSeconds(GetSecondsFromMinute(InvoiceInterval)))
                            .WithSimpleSchedule(x => x
                                .WithIntervalInSeconds(GetSecondsFromMinute(InvoiceInterval))
                                .RepeatForever())
                            .Build();

                        logger.message = string.Format("Invoice trigger is set with interval of {0} min", InvoiceInterval);
                        logger.Broadcast(); 

                        await scheduler.ScheduleJob(ijob, itrigger);
                        ActiveJobs++;
                    }
                    else if (accSoftware.software_name == "QNE")
                    {
                        IJobDetail invoiceqnejob = JobBuilder.Create<JobInvoiceQneSync>()
                        .WithIdentity(Constants.Job_InvoiceQne, Constants.Job_Group_Sync)
                        .Build();

                        ITrigger invqnetrigger = TriggerBuilder.Create()
                            .WithIdentity(Constants.Trigger_Invoice, Constants.Job_Group_Sync)
                            .StartAt(dtNow.AddSeconds(GetSecondsFromMinute(InvoiceInterval)))
                            .WithSimpleSchedule(x => x
                                .WithIntervalInSeconds(GetSecondsFromMinute(InvoiceInterval))
                                .RepeatForever())
                            .Build();

                        logger.message = string.Format("Invoice QNE trigger is set with interval of {0} min", InvoiceInterval);
                        logger.Broadcast();

                        await scheduler.ScheduleJob(invoiceqnejob, invqnetrigger);
                        ActiveJobs++;
                    }
                    else if (accSoftware.software_name == "APS")
                    {
                        IJobDetail apsinvoicejob = JobBuilder.Create<JobAPSInvoiceSync>()
                        .WithIdentity(Constants.Job_APSInvoice, Constants.Job_Group_Sync)
                        .Build();

                        ITrigger apsinvtrigger = TriggerBuilder.Create()
                          .WithIdentity(Constants.Trigger_APSInvoice, Constants.Job_Group_Sync)
                          .StartAt(dtNow.AddSeconds(GetSecondsFromMinute(InvoiceInterval)))
                          .WithSimpleSchedule(x => x
                              .WithIntervalInSeconds(GetSecondsFromMinute(InvoiceInterval))
                              .RepeatForever())
                          .Build();

                        logger.message = string.Format("APS invoice trigger is set with interval of {0} min", InvoiceInterval);
                        logger.Broadcast();

                        await scheduler.ScheduleJob(apsinvoicejob, apsinvtrigger);
                        ActiveJobs++;
                    }
                    else if (accSoftware.software_name == "AutoCount")
                    {
                        IJobDetail atcinvoicejob = JobBuilder.Create<JobATCInvoiceSync>()
                        .WithIdentity(Constants.Job_ATCInvoice, Constants.Job_Group_Sync)
                        .Build();

                        ITrigger atcinvtrigger = TriggerBuilder.Create()
                          .WithIdentity(Constants.Trigger_ATCInvoice, Constants.Job_Group_Sync)
                          .StartAt(dtNow.AddSeconds(GetSecondsFromMinute(InvoiceInterval)))
                          .WithSimpleSchedule(x => x
                              .WithIntervalInSeconds(GetSecondsFromMinute(InvoiceInterval))
                              .RepeatForever())
                          .Build();

                        logger.message = string.Format("ATC invoice trigger is set with interval of {0} min", InvoiceInterval);
                        logger.Broadcast();

                        await scheduler.ScheduleJob(atcinvoicejob, atcinvtrigger);
                        ActiveJobs++;
                    }
                    else if (accSoftware.software_name == "Sage UBS")                               //####LATER CHANGE TO UBS JOB
                    {
                        logger.message = string.Format("[SAGE UBS] invoice trigger is set with interval of {0} min", InvoiceInterval);
                        logger.Broadcast();
                        ActiveJobs++;
                    }
                    else
                    {
                        //dont assign to any job
                    }

                }

                if (InvDtlEnabled)
                {
                    if (accSoftware.software_name == "SQLAccounting")
                    {
                        IJobDetail idjob = JobBuilder.Create<JobInvoiceDetailsSync>()
                        .WithIdentity(Constants.Job_InvoiceDtl, Constants.Job_Group_Sync)
                        .Build();

                        ITrigger idtrigger = TriggerBuilder.Create()
                            .WithIdentity(Constants.Trigger_InvoiceDtl, Constants.Job_Group_Sync)
                            .StartAt(dtNow.AddSeconds(GetSecondsFromMinute(InvoiceDetailsInterval)))
                            .WithSimpleSchedule(x => x
                                .WithIntervalInSeconds(GetSecondsFromMinute(InvoiceDetailsInterval))
                                .RepeatForever())
                            .Build();

                        logger.message = string.Format("Invoice details trigger is set with interval of {0} min", InvoiceDetailsInterval);
                        logger.Broadcast();

                        await scheduler.ScheduleJob(idjob, idtrigger);
                        ActiveJobs++;
                    }
                    else if (accSoftware.software_name == "QNE")
                    {
                        IJobDetail invoicedetailsqnejob = JobBuilder.Create<JobInvoiceDetailsQneSync>()
                        .WithIdentity(Constants.Job_InvoiceDetailsQne, Constants.Job_Group_Sync)
                        .Build();

                        ITrigger invdtlqnetrigger = TriggerBuilder.Create()
                          .WithIdentity(Constants.Trigger_InvoiceDetailsQne, Constants.Job_Group_Sync)
                          .StartAt(dtNow.AddSeconds(GetSecondsFromMinute(InvoiceDetailsInterval)))
                          .WithSimpleSchedule(x => x
                              .WithIntervalInSeconds(GetSecondsFromMinute(InvoiceDetailsInterval))
                              .RepeatForever())
                          .Build();

                        logger.message = string.Format("Invoice details QNE trigger is set with interval of {0} min", InvoiceDetailsInterval);
                        logger.Broadcast();

                        await scheduler.ScheduleJob(invoicedetailsqnejob, invdtlqnetrigger);
                        ActiveJobs++;
                    }
                    else if (accSoftware.software_name == "APS")
                    {
                        IJobDetail apsinvoicedetailjob = JobBuilder.Create<JobAPSInvoiceDetailSync>()
                        .WithIdentity(Constants.Job_APSInvoiceDetail, Constants.Job_Group_Sync)
                        .Build();

                        ITrigger apsinvdtltrigger = TriggerBuilder.Create()
                          .WithIdentity(Constants.Trigger_APSInvoiceDetail, Constants.Job_Group_Sync)
                          .StartAt(dtNow.AddSeconds(GetSecondsFromMinute(InvoiceDetailsInterval)))
                          .WithSimpleSchedule(x => x
                              .WithIntervalInSeconds(GetSecondsFromMinute(InvoiceDetailsInterval))
                              .RepeatForever())
                          .Build();

                        logger.message = string.Format("APS invoice details trigger is set with interval of {0} min", InvoiceDetailsInterval);
                        logger.Broadcast();

                        await scheduler.ScheduleJob(apsinvoicedetailjob, apsinvdtltrigger);
                        ActiveJobs++;
                    }
                    else if (accSoftware.software_name == "AutoCount")
                    {
                        IJobDetail atcinvoicedetailjob = JobBuilder.Create<JobATCInvoiceDetailSync>()
                        .WithIdentity(Constants.Job_ATCInvoiceDetail, Constants.Job_Group_Sync)
                        .Build();

                        ITrigger atcinvdtltrigger = TriggerBuilder.Create()
                          .WithIdentity(Constants.Trigger_ATCInvoiceDetail, Constants.Job_Group_Sync)
                          .StartAt(dtNow.AddSeconds(GetSecondsFromMinute(InvoiceDetailsInterval)))
                          .WithSimpleSchedule(x => x
                              .WithIntervalInSeconds(GetSecondsFromMinute(InvoiceDetailsInterval))
                              .RepeatForever())
                          .Build();

                        logger.message = string.Format("ATC invoice details trigger is set with interval of {0} min", InvoiceDetailsInterval);
                        logger.Broadcast();

                        await scheduler.ScheduleJob(atcinvoicedetailjob, atcinvdtltrigger);
                        ActiveJobs++;
                    }
                    else if (accSoftware.software_name == "Sage UBS")                               //####LATER CHANGE TO UBS JOB
                    {
                        logger.message = string.Format("[SAGE UBS] invoice details trigger is set with interval of {0} min", InvoiceDetailsInterval);
                        logger.Broadcast();
                        ActiveJobs++;
                    }
                    else
                    {
                        //dont assign to any job
                    }

                }

                if (CustomerEnabled)
                {
                    if (accSoftware.software_name == "SQLAccounting")
                    {
                        IJobDetail customerjob = JobBuilder.Create<JobCustSync>()                                       /*sql acc job*/
                        .WithIdentity(Constants.Job_Customer, Constants.Job_Group_Sync)
                        .Build();

                        ITrigger custtrigger = TriggerBuilder.Create()
                           .WithIdentity(Constants.Trigger_Customer, Constants.Job_Group_Sync)
                           .StartAt(dtNow.AddSeconds(GetSecondsFromMinute(CustomerInterval)))
                           .WithSimpleSchedule(x => x
                               .WithIntervalInSeconds(GetSecondsFromMinute(CustomerInterval))
                               .RepeatForever())
                           .Build();

                        logger.message = string.Format("Customer trigger is set with interval of {0} min", CustomerInterval);
                        logger.Broadcast();

                        await scheduler.ScheduleJob(customerjob, custtrigger); 
                        ActiveJobs++;
                    }
                    else if (accSoftware.software_name == "QNE")
                    {
                        IJobDetail custjob = JobBuilder.Create<JobCustomerSync>()                                       /*qne*/
                        .WithIdentity(Constants.Job_Customer, Constants.Job_Group_Sync)
                        .Build();

                        ITrigger custqnetrigger = TriggerBuilder.Create()
                            .WithIdentity(Constants.Trigger_Customer, Constants.Job_Group_Sync)
                            .StartAt(dtNow.AddSeconds(GetSecondsFromMinute(CustomerInterval)))
                            .WithSimpleSchedule(x => x
                                .WithIntervalInSeconds(GetSecondsFromMinute(CustomerInterval))
                                .RepeatForever())
                            .Build();

                        logger.message = string.Format("Customer QNE trigger is set with interval of {0} min", CustomerInterval);
                        logger.Broadcast();
                        await scheduler.ScheduleJob(custjob, custqnetrigger);
                        ActiveJobs++;
                    }
                    else if (accSoftware.software_name == "APS")
                    {
                        IJobDetail apscustomerjob = JobBuilder.Create<JobAPSCustomerSync>()                               /* APS */
                        .WithIdentity(Constants.Job_APSCustomer, Constants.Job_Group_Sync)
                        .Build();

                        ITrigger apscustomertrigger = TriggerBuilder.Create()
                            .WithIdentity(Constants.Trigger_APSCustomer, Constants.Job_Group_Sync)
                            .StartAt(dtNow.AddSeconds(GetSecondsFromMinute(CustomerInterval)))
                            .WithSimpleSchedule(x => x
                                .WithIntervalInSeconds(GetSecondsFromMinute(CustomerInterval))
                                .RepeatForever())
                            .Build();

                        logger.message = string.Format("APS customer trigger is set with interval of {0} min", CustomerInterval);
                        logger.Broadcast();

                        await scheduler.ScheduleJob(apscustomerjob, apscustomertrigger);
                        ActiveJobs++;
                    }
                    else if (accSoftware.software_name == "AutoCount")
                    {
                        IJobDetail atccustomerjob = JobBuilder.Create<JobATCCustomerSync>()                               /* ATC */
                        .WithIdentity(Constants.Job_ATCCustomer, Constants.Job_Group_Sync)
                        .Build();

                        ITrigger atccustomertrigger = TriggerBuilder.Create()
                            .WithIdentity(Constants.Trigger_ATCCustomer, Constants.Job_Group_Sync)
                            .StartAt(dtNow.AddSeconds(GetSecondsFromMinute(CustomerInterval)))
                            .WithSimpleSchedule(x => x
                                .WithIntervalInSeconds(GetSecondsFromMinute(CustomerInterval))
                                .RepeatForever())
                            .Build();

                        logger.message = string.Format("ATC customer trigger is set with interval of {0} min", CustomerInterval);
                        logger.Broadcast();

                        await scheduler.ScheduleJob(atccustomerjob, atccustomertrigger);
                        ActiveJobs++;
                    }
                    else if (accSoftware.software_name == "Sage UBS")                               //####LATER CHANGE TO UBS JOB
                    {
                        logger.message = string.Format("[SAGE UBS] customer trigger is set with interval of {0} min", CustomerInterval);
                        logger.Broadcast();
                        ActiveJobs++;
                    }
                    else
                    {
                        //dont assign to any job
                    }
                }

                if (StockEnabled) 
                {
                    if (accSoftware.software_name == "SQLAccounting")
                    {
                        IJobDetail productjob = JobBuilder.Create<JobProductSync>()
                        .WithIdentity(Constants.Job_Product, Constants.Job_Group_Sync)
                        .Build();

                        ITrigger producttrigger = TriggerBuilder.Create()
                            .WithIdentity(Constants.Trigger_Product, Constants.Job_Group_Sync)
                            .StartAt(dtNow.AddSeconds(GetSecondsFromMinute(StockInterval)))
                            .WithSimpleSchedule(x => x
                                .WithIntervalInSeconds(GetSecondsFromMinute(StockInterval))
                                .RepeatForever())
                            .Build();

                        logger.message = string.Format("Product trigger is set with interval of {0} min", StockInterval);
                        logger.Broadcast();

                        await scheduler.ScheduleJob(productjob, producttrigger);
                        ActiveJobs++;
                    }
                    else if(accSoftware.software_name == "QNE")
                    {
                        IJobDetail stockjob = JobBuilder.Create<JobStockSync>()
                        .WithIdentity(Constants.Job_Stock, Constants.Job_Group_Sync)
                        .Build();

                        ITrigger stocktrigger = TriggerBuilder.Create()
                            .WithIdentity(Constants.Trigger_Stock, Constants.Job_Group_Sync)
                            .StartAt(dtNow.AddSeconds(GetSecondsFromMinute(StockInterval)))
                            .WithSimpleSchedule(x => x
                                .WithIntervalInSeconds(GetSecondsFromMinute(StockInterval))
                                .RepeatForever())
                            .Build();
                        

                        logger.message = string.Format("Stock trigger is set with interval of {0} min", StockInterval);
                        logger.Broadcast();

                        await scheduler.ScheduleJob(stockjob, stocktrigger);
                        ActiveJobs++;
                    }
                    else if (accSoftware.software_name == "APS")
                    {
                        IJobDetail apsstockjob = JobBuilder.Create<JobAPSStockSync>()                                       /* APS */
                        .WithIdentity(Constants.Job_APSStock, Constants.Job_Group_Sync)
                        .Build();

                        ITrigger apsstocktrigger = TriggerBuilder.Create()
                            .WithIdentity(Constants.Trigger_APSStock, Constants.Job_Group_Sync)
                            .StartAt(dtNow.AddSeconds(GetSecondsFromMinute(StockInterval)))
                            .WithSimpleSchedule(x => x
                                .WithIntervalInSeconds(GetSecondsFromMinute(StockInterval))
                                .RepeatForever())
                            .Build();

                        logger.message = string.Format("APS stock trigger is set with interval of {0} min", StockInterval);
                        logger.Broadcast();

                        await scheduler.ScheduleJob(apsstockjob, apsstocktrigger);

                        ActiveJobs++;
                    }
                    else if (accSoftware.software_name == "AutoCount")
                    {
                        IJobDetail atcstockjob = JobBuilder.Create<JobATCStockSync>()                                       /* ATC */
                        .WithIdentity(Constants.Job_ATCStock, Constants.Job_Group_Sync)
                        .Build();

                        ITrigger atcstocktrigger = TriggerBuilder.Create()
                            .WithIdentity(Constants.Trigger_ATCStock, Constants.Job_Group_Sync)
                            .StartAt(dtNow.AddSeconds(GetSecondsFromMinute(StockInterval)))
                            .WithSimpleSchedule(x => x
                                .WithIntervalInSeconds(GetSecondsFromMinute(StockInterval))
                                .RepeatForever())
                            .Build();

                        logger.message = string.Format("ATC stock trigger is set with interval of {0} min", StockInterval);
                        logger.Broadcast();

                        await scheduler.ScheduleJob(atcstockjob, atcstocktrigger);
                        ActiveJobs++;

                        if (ATCSDKEnabled)
                        {
                            IJobDetail atcreadystockjob = JobBuilder.Create<JobATCReadyStock>()                                       /* ATC */
                        .WithIdentity(Constants.Job_ATCReadyStock, Constants.Job_Group_Sync)
                        .Build();

                            ITrigger atcreadystocktrigger = TriggerBuilder.Create()
                                .WithIdentity(Constants.Trigger_ATCReadyStock, Constants.Job_Group_Sync)
                                .StartAt(dtNow.AddSeconds(GetSecondsFromMinute(StockInterval)))
                                .WithSimpleSchedule(x => x
                                    .WithIntervalInSeconds(GetSecondsFromMinute(StockInterval))
                                    .RepeatForever())
                                .Build();

                            logger.message = string.Format("ATC Ready Stock (SDK) trigger is set with interval of {0} min", StockInterval);
                            logger.Broadcast();

                            IJobDetail atcstockcardjob = JobBuilder.Create<JobATCStockCardSync>()                                       /* ATC */
                        .WithIdentity(Constants.Job_ATCStockCardSync, Constants.Job_Group_Sync)
                        .Build();

                            ITrigger atcstockcardtrigger = TriggerBuilder.Create()
                                .WithIdentity(Constants.Trigger_ATCStockCardSync, Constants.Job_Group_Sync)
                                .StartAt(dtNow.AddSeconds(GetSecondsFromMinute(StockInterval)))
                                .WithSimpleSchedule(x => x
                                    .WithIntervalInSeconds(GetSecondsFromMinute(StockInterval))
                                    .RepeatForever())
                                .Build();

                            logger.message = string.Format("ATC stock card (SDK) trigger is set with interval of {0} min", StockInterval);
                            logger.Broadcast();

                            await scheduler.ScheduleJob(atcreadystockjob, atcreadystocktrigger);
                            await scheduler.ScheduleJob(atcstockcardjob, atcstockcardtrigger);
                            ActiveJobs++;
                            ActiveJobs++;
                        }
                    }
                    else if (accSoftware.software_name == "Sage UBS")                               //####LATER CHANGE TO UBS JOB
                    {
                        logger.message = string.Format("[SAGE UBS] stock trigger is set with interval of {0} min", StockInterval);
                        logger.Broadcast();
                        ActiveJobs++;
                    }
                    else
                    {
                        //dont assign to any job
                    }
                }

                if (WarehouseEnabled)
                {
                    if (accSoftware.software_name == "SQLAccounting")
                    {
                        IJobDetail whjob = JobBuilder.Create<JobWhStockSync>()
                        .WithIdentity(Constants.Job_Warehouse, Constants.Job_Group_Sync)
                        .Build();

                        ITrigger whtrigger = TriggerBuilder.Create()
                            .WithIdentity(Constants.Trigger_Warehouse, Constants.Job_Group_Sync)
                            .StartAt(dtNow.AddSeconds(GetSecondsFromMinute(WarehouseInterval)))
                            .WithSimpleSchedule(x => x
                                .WithIntervalInSeconds(GetSecondsFromMinute(WarehouseInterval))
                                .RepeatForever())
                            .Build();

                        logger.message = string.Format("Warehouse trigger is set with interval of {0} min", WarehouseInterval);
                        logger.Broadcast();

                        await scheduler.ScheduleJob(whjob, whtrigger);
                        ActiveJobs++;
                    }
                    else if (accSoftware.software_name == "QNE")
                    {
                        //do not warehouse sync yet
                    }
                    else if (accSoftware.software_name == "APS")
                    {
                        IJobDetail apswarehousejob = JobBuilder.Create<JobAPSWarehouseSync>()                               /* APS */
                        .WithIdentity(Constants.Job_APSWarehouse, Constants.Job_Group_Sync)
                        .Build();

                        ITrigger apswarehousetrigger = TriggerBuilder.Create()
                            .WithIdentity(Constants.Trigger_APSWarehouse, Constants.Job_Group_Sync)
                            .StartAt(dtNow.AddSeconds(GetSecondsFromMinute(WarehouseInterval)))
                            .WithSimpleSchedule(x => x
                                .WithIntervalInSeconds(GetSecondsFromMinute(WarehouseInterval))
                                .RepeatForever())
                            .Build();

                        logger.message = string.Format("APS warehouse trigger is set with interval of {0} min", WarehouseInterval);
                        logger.Broadcast();

                        IJobDetail apswarehouseqtyjob = JobBuilder.Create<JobAPSWarehouseQtySync>()                               /* APS */
                        .WithIdentity(Constants.Job_APSWarehouseQty, Constants.Job_Group_Sync)
                        .Build();

                        ITrigger apswarehouseqtytrigger = TriggerBuilder.Create()
                            .WithIdentity(Constants.Trigger_APSWarehouseQty, Constants.Job_Group_Sync)
                            .StartAt(dtNow.AddSeconds(GetSecondsFromMinute(WarehouseInterval)))
                            .WithSimpleSchedule(x => x
                                .WithIntervalInSeconds(GetSecondsFromMinute(WarehouseInterval))
                                .RepeatForever())
                            .Build();

                        logger.message = string.Format("APS warehouse qty trigger is set with interval of {0} min", WarehouseInterval);
                        logger.Broadcast();

                        await scheduler.ScheduleJob(apswarehousejob, apswarehousetrigger);
                        await scheduler.ScheduleJob(apswarehouseqtyjob, apswarehouseqtytrigger);

                        ActiveJobs++;
                    }
                    else if (accSoftware.software_name == "AutoCount")
                    {
                        IJobDetail atcwarehousejob = JobBuilder.Create<JobATCWarehouseSync>()                               /* ATC */
                        .WithIdentity(Constants.Job_ATCWarehouse, Constants.Job_Group_Sync)
                        .Build();

                        ITrigger atcwarehousetrigger = TriggerBuilder.Create()
                            .WithIdentity(Constants.Trigger_ATCWarehouse, Constants.Job_Group_Sync)
                            .StartAt(dtNow.AddSeconds(GetSecondsFromMinute(WarehouseInterval)))
                            .WithSimpleSchedule(x => x
                                .WithIntervalInSeconds(GetSecondsFromMinute(WarehouseInterval))
                                .RepeatForever())
                            .Build();

                        logger.message = string.Format("ATC warehouse trigger is set with interval of {0} min", WarehouseInterval);
                        logger.Broadcast();

                        if(ATCSDKEnabled)
                        {
                            IJobDetail atcwarehouseqtyjob = JobBuilder.Create<JobATCWHReadyStock>()                               /* ATC */
                                                    .WithIdentity(Constants.Job_ATCWarehouseQty, Constants.Job_Group_Sync)
                                                    .Build();

                            ITrigger atcwarehouseqtytrigger = TriggerBuilder.Create()
                                .WithIdentity(Constants.Trigger_ATCWarehouseQty, Constants.Job_Group_Sync)
                                .StartAt(dtNow.AddSeconds(GetSecondsFromMinute(WarehouseInterval)))
                                .WithSimpleSchedule(x => x
                                    .WithIntervalInSeconds(GetSecondsFromMinute(WarehouseInterval))
                                    .RepeatForever())
                                .Build();

                            logger.message = string.Format("ATC Warehouse Ready Stock (SDK) trigger is set with interval of {0} min", WarehouseInterval);
                            logger.Broadcast();

                            await scheduler.ScheduleJob(atcwarehouseqtyjob, atcwarehouseqtytrigger);
                        }
                        else
                        {
                            IJobDetail atcwarehouseqtyjob = JobBuilder.Create<JobATCWarehouseQtySync>()                               /* ATC */
                                                    .WithIdentity(Constants.Job_ATCWarehouseQty, Constants.Job_Group_Sync)
                                                    .Build();

                            ITrigger atcwarehouseqtytrigger = TriggerBuilder.Create()
                                .WithIdentity(Constants.Trigger_ATCWarehouseQty, Constants.Job_Group_Sync)
                                .StartAt(dtNow.AddSeconds(GetSecondsFromMinute(WarehouseInterval)))
                                .WithSimpleSchedule(x => x
                                    .WithIntervalInSeconds(GetSecondsFromMinute(WarehouseInterval))
                                    .RepeatForever())
                                .Build();

                            logger.message = string.Format("ATC warehouse qty trigger is set with interval of {0} min", WarehouseInterval);
                            logger.Broadcast();

                            await scheduler.ScheduleJob(atcwarehouseqtyjob, atcwarehouseqtytrigger);
                        }

                        await scheduler.ScheduleJob(atcwarehousejob, atcwarehousetrigger);
                        ActiveJobs++;
                    }
                    else
                    {
                        //dont assign to any job
                    }
                }


                if (StockCategoriesEnabled)
                {
                    if (accSoftware.software_name == "SQLAccounting")
                    {
                        IJobDetail productcategoryjob = JobBuilder.Create<JobProductCategorySync>()
                        .WithIdentity(Constants.Job_ProductCategory, Constants.Job_Group_Sync)
                        .Build();

                        ITrigger productcategorytrigger = TriggerBuilder.Create()                                       /*sql acc*/
                           .WithIdentity(Constants.Trigger_ProductCategory, Constants.Job_Group_Sync)
                           .StartAt(dtNow.AddSeconds(GetSecondsFromMinute(StockCategoriesInterval)))
                           .WithSimpleSchedule(x => x
                               .WithIntervalInSeconds(GetSecondsFromMinute(StockCategoriesInterval))
                               .RepeatForever())
                           .Build();

                        logger.message = string.Format("Product category trigger is set with interval of {0} min", StockCategoriesInterval);
                        logger.Broadcast();

                        await scheduler.ScheduleJob(productcategoryjob, productcategorytrigger);
                        ActiveJobs++;
                    }
                    else if(accSoftware.software_name == "QNE")
                    {
                        IJobDetail stockcategoriesjob = JobBuilder.Create<JobStockCategoriesSync>()
                        .WithIdentity(Constants.Job_StockCategories, Constants.Job_Group_Sync)
                        .Build();

                        ITrigger stockcategoriestrigger = TriggerBuilder.Create()
                            .WithIdentity(Constants.Trigger_StockCategories, Constants.Job_Group_Sync)
                            .StartAt(dtNow.AddSeconds(GetSecondsFromMinute(StockCategoriesInterval)))                   
                            .WithSimpleSchedule(x => x
                                .WithIntervalInSeconds(GetSecondsFromMinute(StockCategoriesInterval))
                                .RepeatForever())
                            .Build();

                        logger.message = string.Format("Stock categories trigger is set with interval of {0} min", StockCategoriesInterval);
                        logger.Broadcast();

                        await scheduler.ScheduleJob(stockcategoriesjob, stockcategoriestrigger);
                        ActiveJobs++;
                    }
                    else if (accSoftware.software_name == "APS")
                    {
                        IJobDetail apsstockcategoryjob = JobBuilder.Create<JobAPSStockCategorySync>()
                        .WithIdentity(Constants.Job_APSStockCategory, Constants.Job_Group_Sync)
                        .Build();

                        ITrigger apsstockcategorytrigger = TriggerBuilder.Create()
                            .WithIdentity(Constants.Trigger_APSStockCategory, Constants.Job_Group_Sync)
                            .StartAt(dtNow.AddSeconds(GetSecondsFromMinute(StockCategoriesInterval)))
                            .WithSimpleSchedule(x => x
                                .WithIntervalInSeconds(GetSecondsFromMinute(StockCategoriesInterval))
                                .RepeatForever())
                            .Build();

                        logger.message = string.Format("APS stock category trigger is set with interval of {0} min", StockCategoriesInterval);
                        logger.Broadcast();

                        await scheduler.ScheduleJob(apsstockcategoryjob, apsstockcategorytrigger);
                        ActiveJobs++;
                    }
                    else if (accSoftware.software_name == "AutoCount")
                    {
                        IJobDetail atcstockcategoryjob = JobBuilder.Create<JobATCStockCategorySync>()
                        .WithIdentity(Constants.Job_ATCStockCategory, Constants.Job_Group_Sync)
                        .Build();

                        ITrigger atcstockcategorytrigger = TriggerBuilder.Create()
                            .WithIdentity(Constants.Trigger_ATCStockCategory, Constants.Job_Group_Sync)
                            .StartAt(dtNow.AddSeconds(GetSecondsFromMinute(StockCategoriesInterval)))
                            .WithSimpleSchedule(x => x
                                .WithIntervalInSeconds(GetSecondsFromMinute(StockCategoriesInterval))
                                .RepeatForever())
                            .Build();

                        logger.message = string.Format("ATC stock category trigger is set with interval of {0} min", StockCategoriesInterval);
                        logger.Broadcast();

                        await scheduler.ScheduleJob(atcstockcategoryjob, atcstockcategorytrigger);
                        ActiveJobs++;
                    }
                    else if (accSoftware.software_name == "Sage UBS")                               //####LATER CHANGE TO UBS JOB
                    {
                        logger.message = string.Format("[SAGE UBS] stock category trigger is set with interval of {0} min", StockCategoriesInterval);
                        logger.Broadcast();
                        ActiveJobs++;
                    }
                    else
                    {
                        //dont assign to any job
                    }
                }

                if (StockUomPriceEnabled)
                {
                    if (accSoftware.software_name == "SQLAccounting")
                    {
                        IJobDetail productpricejob = JobBuilder.Create<JobProductPriceSync>()
                        .WithIdentity(Constants.Job_ProductPrice, Constants.Job_Group_Sync)
                        .Build();

                        ITrigger productpricetrigger = TriggerBuilder.Create()                                          /*sql acc*/
                           .WithIdentity(Constants.Trigger_ProductPrice, Constants.Job_Group_Sync)
                           .StartAt(dtNow.AddSeconds(GetSecondsFromMinute(StockUomPriceInterval)))
                           .WithSimpleSchedule(x => x
                               .WithIntervalInSeconds(GetSecondsFromMinute(StockUomPriceInterval))
                               .RepeatForever())
                           .Build();

                        logger.message = string.Format("Product Price trigger is set with interval of {0} min", StockUomPriceInterval);
                        logger.Broadcast();

                        await scheduler.ScheduleJob(productpricejob, productpricetrigger);
                        ActiveJobs++;
                    }
                    else if (accSoftware.software_name == "QNE")
                    {
                        IJobDetail stockuompricejob = JobBuilder.Create<JobStockUomPriceSync>()
                        .WithIdentity(Constants.Job_StockUomPrice, Constants.Job_Group_Sync)
                        .Build();

                        ITrigger stockuompricetrigger = TriggerBuilder.Create()
                            .WithIdentity(Constants.Trigger_StockUomPrice, Constants.Job_Group_Sync)
                            .StartAt(dtNow.AddSeconds(GetSecondsFromMinute(StockUomPriceInterval)))
                            .WithSimpleSchedule(x => x
                                .WithIntervalInSeconds(GetSecondsFromMinute(StockUomPriceInterval))
                                .RepeatForever())
                            .Build();

                        logger.message = string.Format("Stock UOM Price trigger is set with interval of {0} min", StockUomPriceInterval);
                        logger.Broadcast();

                        await scheduler.ScheduleJob(stockuompricejob, stockuompricetrigger);
                        ActiveJobs++;
                    }
                    else if (accSoftware.software_name == "APS")
                    {
                        IJobDetail apsstockuompricejob = JobBuilder.Create<JobAPSStockUomPriceSync>()
                        .WithIdentity(Constants.Job_APSStockUomPrice, Constants.Job_Group_Sync)
                        .Build();

                        ITrigger apsstockuompricetrigger = TriggerBuilder.Create()
                            .WithIdentity(Constants.Trigger_APSStockUomPrice, Constants.Job_Group_Sync)
                            .StartAt(dtNow.AddSeconds(GetSecondsFromMinute(StockUomPriceInterval)))
                            .WithSimpleSchedule(x => x
                                .WithIntervalInSeconds(GetSecondsFromMinute(StockUomPriceInterval))
                                .RepeatForever())
                            .Build();

                        logger.message = string.Format("APS stock UOM price trigger is set with interval of {0} min", StockUomPriceInterval);
                        logger.Broadcast();

                        await scheduler.ScheduleJob(apsstockuompricejob, apsstockuompricetrigger);
                        ActiveJobs++;
                    }
                    else if (accSoftware.software_name == "AutoCount")
                    {
                        IJobDetail atcstockuompricejob = JobBuilder.Create<JobATCStockUomPriceSync>()
                        .WithIdentity(Constants.Job_ATCStockUomPrice, Constants.Job_Group_Sync)
                        .Build();

                        ITrigger atcstockuompricetrigger = TriggerBuilder.Create()
                            .WithIdentity(Constants.Trigger_ATCStockUomPrice, Constants.Job_Group_Sync)
                            .StartAt(dtNow.AddSeconds(GetSecondsFromMinute(StockUomPriceInterval)))
                            .WithSimpleSchedule(x => x
                                .WithIntervalInSeconds(GetSecondsFromMinute(StockUomPriceInterval))
                                .RepeatForever())
                            .Build();

                        logger.message = string.Format("ATC stock UOM price trigger is set with interval of {0} min", StockUomPriceInterval);
                        logger.Broadcast();

                        await scheduler.ScheduleJob(atcstockuompricejob, atcstockuompricetrigger);
                        ActiveJobs++;
                    }
                    else if (accSoftware.software_name == "Sage UBS")                               //####LATER CHANGE TO UBS JOB
                    {
                        logger.message = string.Format("[SAGE UBS] stock UOM price trigger is set with interval of {0} min", StockUomPriceInterval);
                        logger.Broadcast();
                        ActiveJobs++;
                    }
                    else
                    {
                        //dont assign to any job
                    }
                }

                if (ProductSpecialPriceEnabled)
                {
                    if (accSoftware.software_name == "SQLAccounting")
                    {
                        IJobDetail productspecialpricejob = JobBuilder.Create<JobProductSpecialPriceSync>()
                        .WithIdentity(Constants.Job_Product_SpecialPrice, Constants.Job_Group_Sync)
                        .Build();

                        ITrigger productspecialpricetrigger = TriggerBuilder.Create()
                           .WithIdentity(Constants.Trigger_Product_SpecialPrice, Constants.Job_Group_Sync)
                           .StartAt(dtNow.AddSeconds(GetSecondsFromMinute(ProductSpecialPriceInterval)))
                           .WithSimpleSchedule(x => x
                               .WithIntervalInSeconds(GetSecondsFromMinute(ProductSpecialPriceInterval))
                               .RepeatForever())
                           .Build();

                        logger.message = string.Format("Product special price trigger is set with interval of {0} min", ProductSpecialPriceInterval);
                        logger.Broadcast();

                        await scheduler.ScheduleJob(productspecialpricejob, productspecialpricetrigger);
                        ActiveJobs++;
                    }
                    else if (accSoftware.software_name == "QNE")
                    {
                        //dont have yet
                    }
                    else if (accSoftware.software_name == "APS")
                    {
                        IJobDetail apsstockspecialpricejob = JobBuilder.Create<JobAPSStockSpecialPriceSync>() //check later
                        .WithIdentity(Constants.Job_APSStockSpecialPrice, Constants.Job_Group_Sync)
                        .Build();

                        ITrigger apsstockspecialpricetrigger = TriggerBuilder.Create()
                            .WithIdentity(Constants.Trigger_APSStockSpecialPrice, Constants.Job_Group_Sync)
                            .StartAt(dtNow.AddSeconds(GetSecondsFromMinute(ProductSpecialPriceInterval)))
                            .WithSimpleSchedule(x => x
                                .WithIntervalInSeconds(GetSecondsFromMinute(ProductSpecialPriceInterval))
                                .RepeatForever())
                            .Build();

                        logger.message = string.Format("APS stock special price trigger is set with interval of {0} min", ProductSpecialPriceInterval);
                        logger.Broadcast();

                        await scheduler.ScheduleJob(apsstockspecialpricejob, apsstockspecialpricetrigger);
                        ActiveJobs++;
                    }
                    else if (accSoftware.software_name == "AutoCount")
                    {
                        IJobDetail atcstockspecialpricejob = JobBuilder.Create<JobATCStockSpecialPriceSync>() //check later
                        .WithIdentity(Constants.Job_ATCStockSpecialPrice, Constants.Job_Group_Sync)
                        .Build();

                        ITrigger atcstockspecialpricetrigger = TriggerBuilder.Create()
                            .WithIdentity(Constants.Trigger_ATCStockSpecialPrice, Constants.Job_Group_Sync)
                            .StartAt(dtNow.AddSeconds(GetSecondsFromMinute(ProductSpecialPriceInterval)))
                            .WithSimpleSchedule(x => x
                                .WithIntervalInSeconds(GetSecondsFromMinute(ProductSpecialPriceInterval))
                                .RepeatForever())
                            .Build();

                        logger.message = string.Format("ATC stock special price trigger is set with interval of {0} min", ProductSpecialPriceInterval);
                        logger.Broadcast();

                        await scheduler.ScheduleJob(atcstockspecialpricejob, atcstockspecialpricetrigger);
                        ActiveJobs++;
                    }
                    else if (accSoftware.software_name == "Sage UBS")                               //####LATER CHANGE TO UBS JOB
                    {
                        logger.message = string.Format("[SAGE UBS] stock special price trigger is set with interval of {0} min", ProductSpecialPriceInterval);
                        logger.Broadcast();
                        ActiveJobs++;
                    }
                    else
                    {
                        //dont assign to any job
                    }
                }

                if (CreditNoteEnabled) 
                {
                    if (accSoftware.software_name == "SQLAccounting")
                    {
                        IJobDetail cnjob = JobBuilder.Create<JobCNSync>()
                        .WithIdentity(Constants.Job_CN, Constants.Job_Group_Sync)
                        .Build();

                        ITrigger cntrigger = TriggerBuilder.Create()                                                        /*sql acc*/
                           .WithIdentity(Constants.Trigger_CN, Constants.Job_Group_Sync)
                           .StartAt(dtNow.AddSeconds(GetSecondsFromMinute(CreditNoteInterval)))
                           .WithSimpleSchedule(x => x
                               .WithIntervalInSeconds(GetSecondsFromMinute(CreditNoteInterval))
                               .RepeatForever())
                           .Build();

                        logger.message = string.Format("Credit note trigger is set with interval of {0} min", CreditNoteInterval);
                        logger.Broadcast();

                        await scheduler.ScheduleJob(cnjob, cntrigger);
                        ActiveJobs++;
                    }
                    else if (accSoftware.software_name == "QNE")
                    {
                        IJobDetail creditnotejob = JobBuilder.Create<JobCreditNoteSync>()
                        .WithIdentity(Constants.Job_CreditNote, Constants.Job_Group_Sync)
                        .Build();

                        ITrigger creditnotetrigger = TriggerBuilder.Create()
                           .WithIdentity(Constants.Trigger_CreditNote, Constants.Job_Group_Sync)
                           .StartAt(dtNow.AddSeconds(GetSecondsFromMinute(CreditNoteInterval)))
                           .WithSimpleSchedule(x => x
                               .WithIntervalInSeconds(GetSecondsFromMinute(CreditNoteInterval))
                               .RepeatForever())
                           .Build();

                        logger.message = string.Format("Credit note trigger is set with interval of {0} min", CreditNoteInterval);
                        logger.Broadcast();

                        await scheduler.ScheduleJob(creditnotejob, creditnotetrigger);
                        ActiveJobs++;
                    }
                    else if (accSoftware.software_name == "APS")
                    {
                        IJobDetail apscreditnotejob = JobBuilder.Create<JobAPSCreditNoteSync>()
                        .WithIdentity(Constants.Job_APSCreditNote, Constants.Job_Group_Sync)
                        .Build();

                        ITrigger apscreditnotetrigger = TriggerBuilder.Create()
                           .WithIdentity(Constants.Trigger_APSCreditNote, Constants.Job_Group_Sync)
                           .StartAt(dtNow.AddSeconds(GetSecondsFromMinute(CreditNoteInterval)))
                           .WithSimpleSchedule(x => x
                               .WithIntervalInSeconds(GetSecondsFromMinute(CreditNoteInterval))
                               .RepeatForever())
                           .Build();

                        logger.message = string.Format("APS credit note trigger is set with interval of {0} min", CreditNoteInterval);
                        logger.Broadcast();

                        await scheduler.ScheduleJob(apscreditnotejob, apscreditnotetrigger);
                        ActiveJobs++;
                    }
                    else if(accSoftware.software_name == "AutoCount")
                    {
                        IJobDetail atccreditnotejob = JobBuilder.Create<JobATCCreditNoteSync>()
                        .WithIdentity(Constants.Job_ATCCreditNote, Constants.Job_Group_Sync)
                        .Build();

                        ITrigger atccreditnotetrigger = TriggerBuilder.Create()
                           .WithIdentity(Constants.Trigger_ATCCreditNote, Constants.Job_Group_Sync)
                           .StartAt(dtNow.AddSeconds(GetSecondsFromMinute(CreditNoteInterval)))
                           .WithSimpleSchedule(x => x
                               .WithIntervalInSeconds(GetSecondsFromMinute(CreditNoteInterval))
                               .RepeatForever())
                           .Build();

                        logger.message = string.Format("ATC credit note trigger is set with interval of {0} min", CreditNoteInterval);
                        logger.Broadcast();

                        await scheduler.ScheduleJob(atccreditnotejob, atccreditnotetrigger);
                        ActiveJobs++;
                    }
                    else if (accSoftware.software_name == "Sage UBS")                               //####LATER CHANGE TO UBS JOB
                    {
                        logger.message = string.Format("[SAGE UBS] credit note trigger is set with interval of {0} min", CreditNoteInterval);
                        logger.Broadcast();
                        ActiveJobs++;
                    }
                    else
                    {
                        //dont assign to any job
                    }
                }

                if (CreditNoteDtlEnabled)
                {
                    if (accSoftware.software_name == "SQLAccounting")
                    {
                        IJobDetail cndtljob = JobBuilder.Create<JobCNDtlSync>()
                        .WithIdentity(Constants.Job_CreditNoteDtl, Constants.Job_Group_Sync)
                        .Build();

                        ITrigger cndtltrigger = TriggerBuilder.Create()                                                        /*sql acc*/
                           .WithIdentity(Constants.Trigger_CreditNoteDtl, Constants.Job_Group_Sync)
                            .StartAt(dtNow.AddSeconds(GetSecondsFromMinute(CreditNoteDtlInterval)))
                           .WithSimpleSchedule(x => x
                               .WithIntervalInSeconds(GetSecondsFromMinute(CreditNoteDtlInterval))
                               .RepeatForever())
                           .Build();

                        logger.message = string.Format("Credit note details trigger is set with interval of {0} min", CreditNoteDtlInterval);
                        logger.Broadcast();

                        await scheduler.ScheduleJob(cndtljob, cndtltrigger);
                        ActiveJobs++;
                    }
                    else if (accSoftware.software_name == "QNE")
                    {
                        //dont have yet
                    }
                    else if (accSoftware.software_name == "APS")
                    {
                        IJobDetail apscreditnotedtljob = JobBuilder.Create<JobAPSCreditNoteDetailSync>()
                            .WithIdentity(Constants.Job_APSCreditNoteDetail, Constants.Job_Group_Sync)
                            .Build();

                        ITrigger apscreditnotedtltrigger = TriggerBuilder.Create()
                           .WithIdentity(Constants.Trigger_APSCreditNoteDetail, Constants.Job_Group_Sync)
                           .StartAt(dtNow.AddSeconds(GetSecondsFromMinute(CreditNoteDtlInterval)))
                           .WithSimpleSchedule(x => x
                               .WithIntervalInSeconds(GetSecondsFromMinute(CreditNoteDtlInterval))
                               .RepeatForever())
                           .Build();

                        logger.message = string.Format("APS credit note details trigger is set with interval of {0} min", CreditNoteDtlInterval);
                        logger.Broadcast();

                        await scheduler.ScheduleJob(apscreditnotedtljob, apscreditnotedtltrigger);
                        ActiveJobs++;
                    }
                    else if (accSoftware.software_name == "AutoCount")
                    {
                        IJobDetail atccreditnotedtljob = JobBuilder.Create<JobATCCreditNoteDetailSync>()
                        .WithIdentity(Constants.Job_ATCCreditNoteDetail, Constants.Job_Group_Sync)
                        .Build();

                        ITrigger atccreditnotedtltrigger = TriggerBuilder.Create()
                           .WithIdentity(Constants.Trigger_ATCCreditNoteDetail, Constants.Job_Group_Sync)
                           .StartAt(dtNow.AddSeconds(GetSecondsFromMinute(CreditNoteDtlInterval)))
                           .WithSimpleSchedule(x => x
                               .WithIntervalInSeconds(GetSecondsFromMinute(CreditNoteDtlInterval))
                               .RepeatForever())
                           .Build();

                        logger.message = string.Format("ATC credit note details trigger is set with interval of {0} min", CreditNoteDtlInterval);
                        logger.Broadcast();

                        await scheduler.ScheduleJob(atccreditnotedtljob, atccreditnotedtltrigger);
                        ActiveJobs++;
                    }
                    else if (accSoftware.software_name == "Sage UBS")                               //####LATER CHANGE TO UBS JOB
                    {
                        logger.message = string.Format("[SAGE UBS] credit note details trigger is set with interval of {0} min", CreditNoteDtlInterval);
                        logger.Broadcast();
                        ActiveJobs++;
                    }
                    else
                    {
                        //dont assign to any job
                    }
                }
                

                if (DebitNoteEnabled) 
                {
                    if (accSoftware.software_name == "SQLAccounting")
                    {
                        IJobDetail dnjob = JobBuilder.Create<JobDNSync>()
                        .WithIdentity(Constants.Job_DN, Constants.Job_Group_Sync)
                        .Build();

                        ITrigger dntrigger = TriggerBuilder.Create()                                                        /*sql acc*/
                           .WithIdentity(Constants.Trigger_DN, Constants.Job_Group_Sync)
                           .StartAt(dtNow.AddSeconds(GetSecondsFromMinute(DebitNoteInterval)))
                           .WithSimpleSchedule(x => x
                               .WithIntervalInSeconds(GetSecondsFromMinute(DebitNoteInterval))
                               .RepeatForever())
                           .Build();

                        logger.message = string.Format("Debit note trigger is set with interval of {0} min", DebitNoteInterval);
                        logger.Broadcast();

                        await scheduler.ScheduleJob(dnjob, dntrigger);
                        ActiveJobs++;

                        IJobDetail dndtljob = JobBuilder.Create<JobDNDtlSync>()
                       .WithIdentity(Constants.Job_DNDtl, Constants.Job_Group_Sync)
                       .Build();

                        ITrigger dndtltrigger = TriggerBuilder.Create()                                                        /*sql acc*/
                           .WithIdentity(Constants.Trigger_DNDtl, Constants.Job_Group_Sync)
                           .StartAt(dtNow.AddSeconds(GetSecondsFromMinute(DebitNoteInterval)))
                           .WithSimpleSchedule(x => x
                               .WithIntervalInSeconds(GetSecondsFromMinute(DebitNoteInterval))
                               .RepeatForever())
                           .Build();

                        logger.message = string.Format("Debit note details trigger is set with interval of {0} min", DebitNoteInterval);
                        logger.Broadcast();

                        await scheduler.ScheduleJob(dndtljob, dndtltrigger);
                        ActiveJobs++;
                    }
                    else if (accSoftware.software_name == "QNE")
                    {
                        IJobDetail debitnotejob = JobBuilder.Create<JobDebitNoteSync>()
                        .WithIdentity(Constants.Job_DebitNote, Constants.Job_Group_Sync)
                        .Build();

                        ITrigger debitnotetrigger = TriggerBuilder.Create()
                          .WithIdentity(Constants.Trigger_DebitNote, Constants.Job_Group_Sync)
                          .StartAt(dtNow.AddSeconds(GetSecondsFromMinute(DebitNoteInterval)))
                          .WithSimpleSchedule(x => x
                              .WithIntervalInSeconds(GetSecondsFromMinute(DebitNoteInterval))
                              .RepeatForever())
                          .Build();

                        logger.message = string.Format("Debit note trigger is set with interval of {0} min", DebitNoteInterval);
                        logger.Broadcast();

                        await scheduler.ScheduleJob(debitnotejob, debitnotetrigger);
                        ActiveJobs++;
                    }
                    else if (accSoftware.software_name == "APS")
                    {
                        IJobDetail apsdebitnotejob = JobBuilder.Create<JobAPSDebitNoteSync>()
                        .WithIdentity(Constants.Job_APSDebitNote, Constants.Job_Group_Sync)
                        .Build();

                        ITrigger apsdebitnotetrigger = TriggerBuilder.Create()
                          .WithIdentity(Constants.Trigger_APSDebitNote, Constants.Job_Group_Sync)
                          .StartAt(dtNow.AddSeconds(GetSecondsFromMinute(DebitNoteInterval)))
                          .WithSimpleSchedule(x => x
                              .WithIntervalInSeconds(GetSecondsFromMinute(DebitNoteInterval))
                              .RepeatForever())
                          .Build();

                        logger.message = string.Format("APS debit note trigger is set with interval of {0} min", DebitNoteInterval);
                        logger.Broadcast();

                        await scheduler.ScheduleJob(apsdebitnotejob, apsdebitnotetrigger);
                        ActiveJobs++;
                    }
                    else if (accSoftware.software_name == "AutoCount")
                    {
                        //dont have yet
                    }
                    else if (accSoftware.software_name == "Sage UBS")                               //####LATER CHANGE TO UBS JOB
                    {
                        logger.message = string.Format("[SAGE UBS] debit note trigger is set with interval of {0} min", DebitNoteInterval);
                        logger.Broadcast();
                        ActiveJobs++;
                    }
                    else
                    {
                        //dont assign to any job
                    }
                }
                
                if (ReceiptEnabled) 
                {
                    if (accSoftware.software_name == "SQLAccounting")
                    {
                        IJobDetail rcptjob = JobBuilder.Create<JobRcptSyncSQLAcc>()
                        .WithIdentity(Constants.Job_Rcpt, Constants.Job_Group_Sync)
                        .Build();

                        ITrigger rcpttrigger = TriggerBuilder.Create()                                                   /*sql acc*/
                           .WithIdentity(Constants.Trigger_Rcpt, Constants.Job_Group_Sync)
                           .StartAt(dtNow.AddSeconds(GetSecondsFromMinute(ReceiptInterval)))
                           .WithSimpleSchedule(x => x
                               .WithIntervalInSeconds(GetSecondsFromMinute(ReceiptInterval))
                               .RepeatForever())
                           .Build();

                        logger.message = string.Format("Receipt trigger is set with interval of {0} min", ReceiptInterval);
                        logger.Broadcast();

                        await scheduler.ScheduleJob(rcptjob, rcpttrigger);
                        ActiveJobs++;
                    }
                    else if (accSoftware.software_name == "QNE")
                    {
                        IJobDetail receiptjob = JobBuilder.Create<JobReceiptSync>()
                        .WithIdentity(Constants.Job_Receipt, Constants.Job_Group_Sync)
                        .Build();

                        ITrigger receipttrigger = TriggerBuilder.Create()
                           .WithIdentity(Constants.Trigger_Receipt, Constants.Job_Group_Sync)
                           .StartAt(dtNow.AddSeconds(GetSecondsFromMinute(ReceiptInterval)))
                           .WithSimpleSchedule(x => x
                               .WithIntervalInSeconds(GetSecondsFromMinute(ReceiptInterval))
                               .RepeatForever())
                           .Build();

                        logger.message = string.Format("Receipt trigger is set with interval of {0} min", ReceiptInterval);
                        logger.Broadcast();

                        await scheduler.ScheduleJob(receiptjob, receipttrigger);
                        ActiveJobs++;
                    }
                    else if (accSoftware.software_name == "APS")
                    {
                        IJobDetail apsreceiptjob = JobBuilder.Create<JobAPSReceiptSync>()
                        .WithIdentity(Constants.Job_APSReceipt, Constants.Job_Group_Sync)
                        .Build();

                        ITrigger apsreceipttrigger = TriggerBuilder.Create()
                           .WithIdentity(Constants.Trigger_APSReceipt, Constants.Job_Group_Sync)
                           .StartAt(dtNow.AddSeconds(GetSecondsFromMinute(ReceiptInterval)))
                           .WithSimpleSchedule(x => x
                               .WithIntervalInSeconds(GetSecondsFromMinute(ReceiptInterval))
                               .RepeatForever())
                           .Build();

                        logger.message = string.Format("APS receipt trigger is set with interval of {0} min", ReceiptInterval);
                        logger.Broadcast();

                        await scheduler.ScheduleJob(apsreceiptjob, apsreceipttrigger);
                        ActiveJobs++;
                    }
                    else if (accSoftware.software_name == "AutoCount")
                    {
                        IJobDetail atcreceiptjob = JobBuilder.Create<JobATCReceiptSync>()
                        .WithIdentity(Constants.Job_ATCReceipt, Constants.Job_Group_Sync)
                        .Build();

                        ITrigger atcreceipttrigger = TriggerBuilder.Create()
                           .WithIdentity(Constants.Trigger_ATCReceipt, Constants.Job_Group_Sync)
                           .StartAt(dtNow.AddSeconds(GetSecondsFromMinute(ReceiptInterval)))
                           .WithSimpleSchedule(x => x
                               .WithIntervalInSeconds(GetSecondsFromMinute(ReceiptInterval))
                               .RepeatForever())
                           .Build();

                        logger.message = string.Format("ATC receipt trigger is set with interval of {0} min", ReceiptInterval);
                        logger.Broadcast();

                        await scheduler.ScheduleJob(atcreceiptjob, atcreceipttrigger);
                        ActiveJobs++;
                    }
                    else if (accSoftware.software_name == "Sage UBS")                               //####LATER CHANGE TO UBS JOB
                    {
                        logger.message = string.Format("[SAGE UBS] receipt trigger is set with interval of {0} min", ReceiptInterval);
                        logger.Broadcast();
                        ActiveJobs++;
                    }
                    else
                    {
                        //dont assign to any job
                    }
                }
                
                if (PostSalesInvoicesEnabled)
                {
                    if (accSoftware.software_name == "SQLAccounting")
                    {
                        IJobDetail transferINV = JobBuilder.Create<JobINVTransfer>()
                  .WithIdentity(Constants.Job_Transfer_INV, Constants.Job_Group_Transfer)
                  .Build();

                        ITrigger transferINVtrigger = TriggerBuilder.Create()
                          .WithIdentity(Constants.Trigger_Transfer_INV, Constants.Job_Group_Transfer)
                          .StartAt(dtNow.AddSeconds(GetSecondsFromMinute(PostSalesInvoicesInterval)))
                          .WithSimpleSchedule(x => x
                              .WithIntervalInSeconds(GetSecondsFromMinute(PostSalesInvoicesInterval))
                              .RepeatForever())
                          .Build();

                        logger.message = string.Format("Transfer Sales Invoices trigger is set with interval of {0} min", PostSalesInvoicesInterval);
                        logger.Broadcast();

                        await scheduler.ScheduleJob(transferINV, transferINVtrigger);
                        ActiveJobs++;
                    }
                    else if (accSoftware.software_name == "QNE")
                    {
                        IJobDetail postsalesinvoicejob = JobBuilder.Create<JobPostSalesInvoice>()
                  .WithIdentity(Constants.Job_Post_SalesInvoices, Constants.Job_Group_Transfer)
                  .Build();

                        ITrigger postsalesinvoicestrigger = TriggerBuilder.Create()
                          .WithIdentity(Constants.Trigger_Post_SalesInvoices, Constants.Job_Group_Transfer)
                          .StartAt(dtNow.AddSeconds(GetSecondsFromMinute(PostSalesInvoicesInterval)))
                          .WithSimpleSchedule(x => x
                              .WithIntervalInSeconds(GetSecondsFromMinute(PostSalesInvoicesInterval))
                              .RepeatForever())
                          .Build();

                        logger.message = string.Format("POST Sales Invoices trigger is set with interval of {0} min", PostSalesInvoicesInterval);
                        logger.Broadcast();

                        await scheduler.ScheduleJob(postsalesinvoicejob, postsalesinvoicestrigger);
                        ActiveJobs++;
                    }
                    else if(accSoftware.software_name == "AutoCount")
                    {
                        if(ATCSDKEnabled)
                        {
                            IJobDetail transferINV = JobBuilder.Create<JobATCTransferINVSDK>()
                  .WithIdentity(Constants.Job_Transfer_INV, Constants.Job_Group_Transfer)
                  .Build();

                            ITrigger transferINVtrigger = TriggerBuilder.Create()
                              .WithIdentity(Constants.Trigger_Transfer_INV, Constants.Job_Group_Transfer)
                              .StartAt(dtNow.AddSeconds(GetSecondsFromMinute(PostSalesInvoicesInterval)))
                              .WithSimpleSchedule(x => x
                                  .WithIntervalInSeconds(GetSecondsFromMinute(PostSalesInvoicesInterval))
                                  .RepeatForever())
                              .Build();

                            logger.message = string.Format("ATC Transfer Sales Invoices (SDK) trigger is set with interval of {0} min", PostSalesInvoicesInterval);
                            logger.Broadcast();

                            await scheduler.ScheduleJob(transferINV, transferINVtrigger);
                            ActiveJobs++;
                        }
                        else
                        {
                            IJobDetail transferINV = JobBuilder.Create<JobATCTransferINV>()
                  .WithIdentity(Constants.Job_Transfer_INV, Constants.Job_Group_Transfer)
                  .Build();

                            ITrigger transferINVtrigger = TriggerBuilder.Create()
                              .WithIdentity(Constants.Trigger_Transfer_INV, Constants.Job_Group_Transfer)
                              .StartAt(dtNow.AddSeconds(GetSecondsFromMinute(PostSalesInvoicesInterval)))
                              .WithSimpleSchedule(x => x
                                  .WithIntervalInSeconds(GetSecondsFromMinute(PostSalesInvoicesInterval))
                                  .RepeatForever())
                              .Build();

                            logger.message = string.Format("Transfer Sales Invoices trigger is set with interval of {0} min", PostSalesInvoicesInterval);
                            logger.Broadcast();

                            await scheduler.ScheduleJob(transferINV, transferINVtrigger);
                            ActiveJobs++;
                        }
                        
                        
                    }
                    else
                    {
                        //dont assign to any job
                    }
                }


                if (PostSalesCNsEnabled)
                {
                    if (accSoftware.software_name == "SQLAccounting")
                    {
                        IJobDetail postsalescnsjob = JobBuilder.Create<JobCNTransfer>()
                  .WithIdentity(Constants.Job_Post_SalesCNs, Constants.Job_Group_Transfer)
                  .Build();

                        ITrigger postsalescnstrigger = TriggerBuilder.Create()
                          .WithIdentity(Constants.Trigger_Post_SalesCNs, Constants.Job_Group_Transfer)
                          .StartAt(dtNow.AddSeconds(GetSecondsFromMinute(PostSalesCNsInterval)))
                          .WithSimpleSchedule(x => x
                              .WithIntervalInSeconds(GetSecondsFromMinute(PostSalesCNsInterval))
                              .RepeatForever())
                          .Build();

                        logger.message = string.Format("Transfer Sales CNs trigger is set with interval of {0} min", PostSalesCNsInterval);
                        logger.Broadcast();

                        await scheduler.ScheduleJob(postsalescnsjob, postsalescnstrigger);
                        ActiveJobs++;
                    }
                    else if (accSoftware.software_name == "QNE")
                    {
                        IJobDetail postsalescnsjob = JobBuilder.Create<JobPostSalesCNs>()
                  .WithIdentity(Constants.Job_Post_SalesCNs, Constants.Job_Group_Transfer)
                  .Build();

                        ITrigger postsalescnstrigger = TriggerBuilder.Create()
                          .WithIdentity(Constants.Trigger_Post_SalesCNs, Constants.Job_Group_Transfer)
                          .StartAt(dtNow.AddSeconds(GetSecondsFromMinute(PostSalesCNsInterval)))
                          .WithSimpleSchedule(x => x
                              .WithIntervalInSeconds(GetSecondsFromMinute(PostSalesCNsInterval))
                              .RepeatForever())
                          .Build();

                        logger.message = string.Format("POST Sales CNs trigger is set with interval of {0} min", PostSalesCNsInterval);
                        logger.Broadcast();

                        await scheduler.ScheduleJob(postsalescnsjob, postsalescnstrigger);
                        ActiveJobs++;
                    }
                    else if (accSoftware.software_name == "APS")
                    {
                        IJobDetail apspostsalescnsjob = JobBuilder.Create<JobAPSTransferCN>()
                  .WithIdentity(Constants.Job_APSTransferCN, Constants.Job_Group_Transfer)
                  .Build();

                        ITrigger apspostsalescnstrigger = TriggerBuilder.Create()
                          .WithIdentity(Constants.Trigger_Post_SalesCNs, Constants.Job_Group_Transfer)
                          .StartAt(dtNow.AddSeconds(GetSecondsFromMinute(PostSalesCNsInterval)))
                          .WithSimpleSchedule(x => x
                              .WithIntervalInSeconds(GetSecondsFromMinute(PostSalesCNsInterval))
                              .RepeatForever())
                          .Build();

                        logger.message = string.Format("APS transfer Sales CNs trigger is set with interval of {0} min", PostSalesCNsInterval);
                        logger.Broadcast();

                        await scheduler.ScheduleJob(apspostsalescnsjob, apspostsalescnstrigger);
                        ActiveJobs++;
                    }
                    else
                    {
                        //dont assign to any job
                    }
                }
                
                if (BranchEnabled)
                {
                    List<DpprMySQLconfig> mysql_list = LocalDB.GetRemoteDatabaseConfig().Distinct().ToList();
                    DpprMySQLconfig mysql_config = mysql_list[0];
                    string compName = mysql_config.config_database;

                    if (accSoftware.software_name == "SQLAccounting")
                    {
                        if (compName == "easysale_uvjoy")
                        {
                            IJobDetail collectionjob = JobBuilder.Create<JobUJ>()
                    .WithIdentity(Constants.Job_Branch, Constants.Job_Group_Sync)
                    .Build();

                            ITrigger collectiontrigger = TriggerBuilder.Create()
                               .WithIdentity(Constants.Trigger_Branch, Constants.Job_Group_Sync)
                               .StartAt(dtNow.AddSeconds(GetSecondsFromMinute(BranchInterval)))
                               .WithSimpleSchedule(x => x
                                   .WithIntervalInSeconds(GetSecondsFromMinute(BranchInterval))
                                   .RepeatForever())
                               .Build();

                            logger.message = string.Format("Collection trigger is set with interval of {0} min", BranchInterval);
                            logger.Broadcast();

                            await scheduler.ScheduleJob(collectionjob, collectiontrigger);
                            ActiveJobs++;
                        }
                        else
                        {
                            IJobDetail branchjob = JobBuilder.Create<JobBranchSync>()
                    .WithIdentity(Constants.Job_Branch, Constants.Job_Group_Sync)
                    .Build();

                            ITrigger branchtrigger = TriggerBuilder.Create()
                               .WithIdentity(Constants.Trigger_Branch, Constants.Job_Group_Sync)
                               .StartAt(dtNow.AddSeconds(GetSecondsFromMinute(BranchInterval)))
                               .WithSimpleSchedule(x => x
                                   .WithIntervalInSeconds(GetSecondsFromMinute(BranchInterval))
                                   .RepeatForever())
                               .Build();

                            logger.message = string.Format("Branch trigger is set with interval of {0} min", BranchInterval);
                            logger.Broadcast();

                            await scheduler.ScheduleJob(branchjob, branchtrigger);
                            ActiveJobs++;
                        }
                    }
                    else if (accSoftware.software_name == "AutoCount")
                    {
                        IJobDetail branchjob = JobBuilder.Create<JobATCBranchSync>()
                    .WithIdentity(Constants.Job_ATCBranch, Constants.Job_Group_Sync)
                    .Build();

                        ITrigger branchtrigger = TriggerBuilder.Create()
                           .WithIdentity(Constants.Trigger_ATCBranch, Constants.Job_Group_Sync)
                           .StartAt(dtNow.AddSeconds(GetSecondsFromMinute(BranchInterval)))
                           .WithSimpleSchedule(x => x
                               .WithIntervalInSeconds(GetSecondsFromMinute(BranchInterval))
                               .RepeatForever())
                           .Build();

                        logger.message = string.Format("ATC Branch trigger is set with interval of {0} min", BranchInterval);
                        logger.Broadcast();

                        await scheduler.ScheduleJob(branchjob, branchtrigger);
                        ActiveJobs++;
                    }
                    else
                    {
                        //dont have
                    }
                }

                if (ActiveJobs == 0)
                {
                    logger.message = "--------------No active job found. If It's unintentional, please add jobs by clicking on checkbox of particular job------------";
                    logger.Broadcast();
                }
                else
                {
                    logger.message = string.Format("-------{0} job(s) are scheduled to run concurrently----------", ActiveJobs);
                    logger.Broadcast();
                }

                await Task.Delay(TimeSpan.FromSeconds(10));

                await scheduler.Start();

                //await scheduler.Shutdown();
            }
            catch (SchedulerException e)
            {
                GlobalLogger logger = new GlobalLogger();
                logger.message = Constants.Job_Exception + e.Message;
                logger.Broadcast();
                Console.WriteLine(Constants.Job_Exception + e.Message);
            }
        }

        private static int GetSecondsFromMinute(int min)
        {
            return min * 60;
        }
    }

    internal class JobListener : IJobListener
    {
        private int GetCount()
        {
            throw new NotImplementedException();
        }

        public string Name { get { return Constants.Job_Listener; } }

        public async Task JobExecutionVetoed(IJobExecutionContext context, CancellationToken cancellationToken = default(CancellationToken))
        {
            
        }

        public async Task JobToBeExecuted(IJobExecutionContext context, CancellationToken cancellationToken = default(CancellationToken))
        {
        }

        public async Task JobWasExecuted(IJobExecutionContext context, JobExecutionException jobException, CancellationToken cancellationToken = default(CancellationToken))
        {

        }
    }
}