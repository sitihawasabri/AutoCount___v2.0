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


                if (KnockOffEnabled)                                                               /* KnockOff */
                {
                    //if (accSoftware.software_name == "SQLAccounting")
                    //{
                    //    IJobDetail knockoffjob = JobBuilder.Create<JobKnockOffSync>()
                    //    .WithIdentity(Constants.Job_KnockOff, Constants.Job_Group_Sync)
                    //    .Build();

                    //    ITrigger knockofftrigger = TriggerBuilder.Create()
                    //       .WithIdentity(Constants.Trigger_KnockOff, Constants.Job_Group_Sync)
                    //       .StartAt(dtNow.AddSeconds(GetSecondsFromMinute(KnockOffInterval)))
                    //       .WithSimpleSchedule(x => x
                    //           .WithIntervalInSeconds(GetSecondsFromMinute(KnockOffInterval))
                    //           .RepeatForever())
                    //       .Build();

                    //    logger.message = string.Format("SQLAcc Knock off trigger is set with interval of {0} min", KnockOffInterval);
                    //    logger.Broadcast();

                    //    await scheduler.ScheduleJob(knockoffjob, knockofftrigger);
                    //    ActiveJobs++;
                    //}
                }
                
                if (PostStockEnabled)                                                               /* CASH SALES */
                {
                    if (accSoftware.software_name == "AutoCount")
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
                }

                if (StockCardEnabled)
                {
                    if (accSoftware.software_name == "AutoCount")
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
                }

                if (ItemTemplateEnabled)
                {
                    if (accSoftware.software_name == "AutoCount")
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
                }

                if (ItemTemplateDtlEnabled)
                {
                    if (accSoftware.software_name == "AutoCount")
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
                }

                if (SalesOrdersEnabled) 
                {
                    if (accSoftware.software_name == "AutoCount")
                    {
                        if (ATCSDKEnabled)
                        {
                            if(ATCV2Enabled)
                            {
                                IJobDetail atctransfersosdkjob = JobBuilder.Create<JobATCTransferSOSDKv2>()
                        .WithIdentity(Constants.Job_ATCTransferSOSDK, Constants.Job_Group_Transfer)
                        .Build();

                                ITrigger atctransfersosdktrigger = TriggerBuilder.Create()
                                  .WithIdentity(Constants.Trigger_ATCTransferSOSDK, Constants.Job_Group_Transfer)
                                  .StartAt(dtNow.AddSeconds(GetSecondsFromMinute(SalesOrdersInterval)))
                                  .WithSimpleSchedule(x => x
                                      .WithIntervalInSeconds(GetSecondsFromMinute(SalesOrdersInterval))
                                      .RepeatForever())
                                  .Build();

                                logger.message = string.Format("[SDK] ATC transfer SO v2.0 trigger is set with interval of {0} min", SalesOrdersInterval);
                                logger.Broadcast();

                                await scheduler.ScheduleJob(atctransfersosdkjob, atctransfersosdktrigger);
                                ActiveJobs++;
                            }
                            else
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
                }

                if (TransferCSEnabled)
                {
                    if (accSoftware.software_name == "AutoCount")
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
                }

                if (CustomerAgentEnabled)
                {
                    if (accSoftware.software_name == "AutoCount")
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
                }

                if (OutsoEnabled)
                {
                   if(accSoftware.software_name == "AutoCount")
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
                }

                if (InvEnabled)
                {
                   if (accSoftware.software_name == "AutoCount")
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
                }

                if (InvDtlEnabled)
                {
                    if (accSoftware.software_name == "AutoCount")
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
                }

                if (CustomerEnabled)
                {
                    if (accSoftware.software_name == "AutoCount")
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
                }

                if (StockEnabled) 
                {
                    if (accSoftware.software_name == "AutoCount")
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
                        //    IJobDetail atcreadystockjob = JobBuilder.Create<JobATCReadyStock>()                                       /* ATC */
                        //.WithIdentity(Constants.Job_ATCReadyStock, Constants.Job_Group_Sync)
                        //.Build();

                        //    ITrigger atcreadystocktrigger = TriggerBuilder.Create()
                        //        .WithIdentity(Constants.Trigger_ATCReadyStock, Constants.Job_Group_Sync)
                        //        .StartAt(dtNow.AddSeconds(GetSecondsFromMinute(StockInterval)))
                        //        .WithSimpleSchedule(x => x
                        //            .WithIntervalInSeconds(GetSecondsFromMinute(StockInterval))
                        //            .RepeatForever())
                        //        .Build();

                        //    logger.message = string.Format("ATC Ready Stock (SDK) trigger is set with interval of {0} min", StockInterval);
                        //    logger.Broadcast();

                        //    IJobDetail atcstockcardjob = JobBuilder.Create<JobATCStockCardSync>()                                       /* ATC */
                        //.WithIdentity(Constants.Job_ATCStockCardSync, Constants.Job_Group_Sync)
                        //.Build();

                        //    ITrigger atcstockcardtrigger = TriggerBuilder.Create()
                        //        .WithIdentity(Constants.Trigger_ATCStockCardSync, Constants.Job_Group_Sync)
                        //        .StartAt(dtNow.AddSeconds(GetSecondsFromMinute(StockInterval)))
                        //        .WithSimpleSchedule(x => x
                        //            .WithIntervalInSeconds(GetSecondsFromMinute(StockInterval))
                        //            .RepeatForever())
                        //        .Build();

                        //    logger.message = string.Format("ATC stock card (SDK) trigger is set with interval of {0} min", StockInterval);
                        //    logger.Broadcast();

                        //    //await scheduler.ScheduleJob(atcreadystockjob, atcreadystocktrigger);
                        //    await scheduler.ScheduleJob(atcstockcardjob, atcstockcardtrigger);
                        //    ActiveJobs++;
                        }
                    }
                }

                if (WarehouseEnabled)
                {
                    if (accSoftware.software_name == "AutoCount")
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

                        //if(ATCSDKEnabled)
                        //{
                        //    IJobDetail atcwarehouseqtyjob = JobBuilder.Create<JobATCWHReadyStock>()                               /* ATC */
                        //                            .WithIdentity(Constants.Job_ATCWarehouseQty, Constants.Job_Group_Sync)
                        //                            .Build();

                        //    ITrigger atcwarehouseqtytrigger = TriggerBuilder.Create()
                        //        .WithIdentity(Constants.Trigger_ATCWarehouseQty, Constants.Job_Group_Sync)
                        //        .StartAt(dtNow.AddSeconds(GetSecondsFromMinute(WarehouseInterval)))
                        //        .WithSimpleSchedule(x => x
                        //            .WithIntervalInSeconds(GetSecondsFromMinute(WarehouseInterval))
                        //            .RepeatForever())
                        //        .Build();

                        //    logger.message = string.Format("ATC Warehouse Ready Stock (SDK) trigger is set with interval of {0} min", WarehouseInterval);
                        //    logger.Broadcast();

                        //    await scheduler.ScheduleJob(atcwarehouseqtyjob, atcwarehouseqtytrigger);
                        //}
                        //else
                        //{
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
                        //}

                        await scheduler.ScheduleJob(atcwarehousejob, atcwarehousetrigger);
                        ActiveJobs++;
                    }
                }


                if (StockCategoriesEnabled)
                {
                    if (accSoftware.software_name == "AutoCount")
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
                }

                if (StockUomPriceEnabled)
                {
                    if (accSoftware.software_name == "AutoCount")
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
                }

                if (ProductSpecialPriceEnabled)
                {
                    if (accSoftware.software_name == "AutoCount")
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
                }

                if (CreditNoteEnabled) 
                {
                    if(accSoftware.software_name == "AutoCount")
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
                    if (accSoftware.software_name == "AutoCount")
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
                }
                

                if (DebitNoteEnabled) 
                {
                    if (accSoftware.software_name == "AutoCount")
                    {
                        //dont have yet
                    }
                }
                
                if (ReceiptEnabled) 
                {
                    if (accSoftware.software_name == "AutoCount")
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
                }
                
                if (PostSalesInvoicesEnabled)
                {
                    if(accSoftware.software_name == "AutoCount")
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
                }


                if (PostSalesCNsEnabled)
                {
                    if (accSoftware.software_name == "AutoCount")
                    {
                        IJobDetail atcpostsalescnsjob = JobBuilder.Create<JobATCTransferCNSDK>()
                  .WithIdentity(Constants.Job_ATCTransferCNSDK, Constants.Job_Group_Transfer)
                  .Build();

                        ITrigger atcpostsalescnstrigger = TriggerBuilder.Create()
                          .WithIdentity(Constants.Trigger_Post_SalesCNs, Constants.Job_Group_Transfer)
                          .StartAt(dtNow.AddSeconds(GetSecondsFromMinute(PostSalesCNsInterval)))
                          .WithSimpleSchedule(x => x
                              .WithIntervalInSeconds(GetSecondsFromMinute(PostSalesCNsInterval))
                              .RepeatForever())
                          .Build();

                        logger.message = string.Format("AutoCount SDK Transfer CNs trigger is set with interval of {0} min", PostSalesCNsInterval);
                        logger.Broadcast();

                        await scheduler.ScheduleJob(atcpostsalescnsjob, atcpostsalescnstrigger);
                        ActiveJobs++;
                    }
                }
                
                if (BranchEnabled)
                {
                    List<DpprMySQLconfig> mysql_list = LocalDB.GetRemoteDatabaseConfig().Distinct().ToList();
                    DpprMySQLconfig mysql_config = mysql_list[0];
                    string compName = mysql_config.config_database;

                    if (accSoftware.software_name == "AutoCount")
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