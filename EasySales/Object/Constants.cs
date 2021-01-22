using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasySales.Object
{
    public static class Constants
    {
        public static string Action_StockCardSync = "stock_card_sync";
        public static string Tbl_cms_stock_card = "cms_stock_card";

        public static string Action_ProductSync = "product_sync";

        public static string Action_ProductCategorySync = "productcategory_sync";

        public static string Action_WarehouseSync = "warehouse_sync";

        public static string Action_ProductPriceSync = "productprice_sync";

        public static string Action_BranchSync = "branch_sync";
        public static string Tbl_cms_customer_branch = "cms_customer_branch";

        public static string Action_ProductSpecialPriceSync = "productspecialprice_sync";
        public static string Tbl_cms_product_price_v2 = "cms_product_price_v2";

        public static string Action_OutstandingSync = "outstanding_sync";
        public static string Tbl_cms_outstanding = "cms_outstanding_so";

        public static string Action_InvoiceSync = "invoice_sync";
        public static string Tbl_cms_invoice = "cms_invoice";
        public static string Tbl_cms_invoice_sales = "cms_invoice_sales";

        public static string Action_CashSalesSync = "cashsales_sync";       /* SL_CS */
        public static string Tbl_cms_cash_sales = "cms_cash_sales";         /* SL_CS */

        public static string Action_KnockOffSync = "knockOff_sync";                                         /* KNOCKOFF */
        public static string Tbl_cms_customer_ageing_ko = "cms_customer_ageing_ko";

        public static string Action_InvoiceDtlSync = "invoiceDetails_sync";
        public static string Tbl_cms_invoice_details = "cms_invoice_details";

        public static string Action_CustomerSync = "customer_sync";
        public static string Tbl_cms_customer = "cms_customer";

        public static string Action_StockSync = "stock_sync";
        public static string Tbl_cms_product = "cms_product";

        public static string Action_StockCategoriesSync = "stockcategories_sync";
        public static string Tbl_cms_product_category = "cms_product_category";

        public static string Action_DOSync = "sqlacc_do_sync";
        public static string Action_DODtlSync = "sqlacc_dodtl_sync";

        public static string Action_APSStockCategorySync = "apsstockcategory_sync";                         /* APS STOCK CAT */
        public static string Action_APSStockSync = "apsstock_sync";                                         /* APS STOCK */
        public static string Action_APSStockUomPriceSync = "apsstockuomprice_sync";                         /* APS STOCK */
        public static string Action_APSStockSpecialPriceSync = "apsstockspecialprice_sync";                 /* APS STOCK */
        public static string Action_APSCustomerSync = "apscustomer_sync";                                   /* APS CUSTOMER  */
        public static string Action_APSSalespersonSync = "apssalesperson_sync";                             /* APS SALESPERSON  */
        public static string Action_APSSalespersonCustomerSync = "apssalespersoncustomer_sync";             /* APS SALESPERSON CUSTOMER  */
        public static string Action_APSInvoiceSync = "apsinvoice_sync";                                     /* APS INVOICE  */
        public static string Action_APSInvoiceDetailSync = "apsinvoicedetail_sync";                         /* APS INVOICE DTL */
        public static string Action_APSCreditNoteSync = "apscrebitnote_sync";                               /* APS CN  */
        public static string Action_APSCreditNoteKOSync = "apscrebitnoteko_sync";                               /* APS CN KO */
        public static string Action_APSReceiptKOSync = "apsreceiptko_sync";                               /* APS RCPT KO */
        public static string Action_APSCreditNoteDetailSync = "apscrebitnotedetail_sync";                   /* APS CN DTL */
        public static string Action_APSDebitNoteSync = "apsdebitnote_sync";                                 /* APS DN  */
        public static string Action_APSReceiptSync = "apsreceipt_sync";                                     /* APS RECEIPT  */
        public static string Action_APSImageSync = "apsimage_sync";                                         /* APS IMAGE  */

        public static string Action_APSRefSync = "apsref_sync";                                             /* APS REF  */
        public static string Tbl_cms_mobile_module = "cms_mobile_module";

        public static string Action_APSDOSync = "apsdo_sync";                                               /* APS DO  */
        public static string Tbl_cms_do = "cms_do";

        public static string Action_APSDODetailSync = "apsdodetail_sync";                                   /* APS DO DTL */
        public static string Tbl_cms_do_details = "cms_do_details";

        public static string Action_APSWarehouseSync = "apswarehousesync";                                  /* APS WAREHOUSE */
        public static string Tbl_cms_warehouse = "cms_warehouse";

        public static string Action_APSWarehouseQtySync = "apswarehouseqty_sync";                           /* APS WAREHOUSE QTY */
        public static string Tbl_cms_warehouse_stock = "cms_warehouse_stock";
        
        public static string Action_ATCWarehouseSync = "apswarehousesync";                                  /* ATC WAREHOUSE */
        public static string Action_ATCWarehouseQtySync = "apswarehouseqty_sync";                           /* ATC WAREHOUSE QTY */

        /* AUTO COUNT */
        public static string Action_ATCStockCategorySync = "atcstockcategory_sync";                         /* ATC STOCK CAT */
        public static string Action_ATCStockSync = "atcstock_sync";                                         /* ATC STOCK */
        public static string Action_ATCReadyStockSync = "atcreadystock_sync";                                         /* ATC STOCK */
        public static string Action_ATCStockUomPriceSync = "atcstockuomprice_sync";                         /* ATC STOCK */
        public static string Action_ATCStockSpecialPriceSync = "atcstockspecialprice_sync";                 /* ATC STOCK */
        public static string Action_ATCCustomerSync = "atccustomer_sync";                                   /* ATC CUSTOMER  */
        public static string Action_ATCBranchSync = "atcbranch_sync";                                   /* ATC CUSTOMER  */
        public static string Action_ATCSalespersonSync = "atcsalesperson_sync";                             /* ATC SALESPERSON  */
        public static string Action_ATCSalespersonCustomerSync = "atcsalespersoncustomer_sync";             /* ATC SALESPERSON CUSTOMER  */
        public static string Action_ATCInvoiceSync = "atcinvoice_sync";                                     /* ATC INVOICE  */
        public static string Action_ATCInvoiceDetailSync = "atcinvoicedetail_sync";                         /* ATC INVOICE DTL */
        public static string Action_ATCCreditNoteSync = "atccrebitnote_sync";                               /* ATC CN  */
        public static string Action_ATCCreditNoteKOSync = "atccrebitnoteko_sync";                           /* ATC CN KO */
        public static string Action_ATCReceiptKOSync = "atcreceiptko_sync";                                 /* ATC RCPT KO */
        public static string Action_ATCCreditNoteDetailSync = "atccrebitnotedetail_sync";                   /* ATC CN DTL */
        public static string Action_ATCDebitNoteSync = "atcdebitnote_sync";                                 /* ATC DN  */
        public static string Action_ATCReceiptSync = "atcreceipt_sync";                                     /* ATC RECEIPT  */
        public static string Action_ATCImageSync = "atcimage_sync";                                         /* ATC IMAGE  */
        public static string Action_ATCItemTemplateSync = "atcitemtemplate_sync";                           /* ATC ITEM TEMPLATE  */
        public static string Action_ATCItemTemplateDtlSync = "atcitemtemplatedtl_sync";                     /* ATC ITEM TEMPLATE DTL */
        public static string Action_ATC_Transfer_SO = "ATC_transfer_SO";                                    /* ATC Transfer SO*/
        public static string Action_ATCINVKO = "ATC_INV_KO";                                                /* ATC */
        public static string Action_ATCCNKO = "ATC_CN_KO";                                                  /* ATC */
        public static string Action_ATCOutSOSync = "ATCOutSOSync";                                                  /* ATC */
        public static string Action_ATC_Transfer_Stock = "ATCTransferStock";                                                  /* ATC */
        public static string Action_ATC_Transfer_CS = "ATCTransferCS";                                                  /* ATC */
        public static string Action_ATC_Transfer_INV = "ATCTransferINV";                                                  /* ATC */
        public static string Action_ATCStockCardSync = "atcstockcard_sync";                                                  /* ATC */
        public static string Action_ATCWHReadyStockSync = "atcwhreadystock_sync";
        /* AUTO COUNT */

        public static string Action_StockUomPriceSync = "stockuomprice_sync";
        public static string Tbl_cms_product_uom_price_v2 = "cms_product_uom_price_v2";

        public static string Action_AgentSync = "agent_sync";
        public static string Tbl_cms_login = "cms_login";
        
        public static string Action_SOSync = "so_sync";
        public static string Tbl_cms_acc_existing_order = "cms_acc_existing_order";
        public static string Tbl_cms_acc_existing_order_item = "cms_acc_existing_order_item";

        public static string Action_InvoiceQneSync = "invoiceqne_sync";
        public static string Tbl_cms_invoice_qne = "cms_invoice";

        public static string Action_InvoiceDetailsQneSync = "invoicedetailsqne_sync";
        public static string Tbl_cms_invoice_details_qne = "cms_invoice_details";

        public static string Action_CustomerAgentSync = "customeragent_sync";
        public static string Tbl_cms_customer_salesperson = "cms_customer_salesperson";

        public static string Action_CreditNoteSync = "creditnote_sync";
        public static string Tbl_cms_creditnote = "cms_creditnote";
        public static string Tbl_cms_creditnote_sales = "cms_creditnote_sales";
        
        public static string Action_CustomerRefundSync = "customer_refund_sync";
        public static string Tbl_cms_customer_refund = "cms_customer_refund";

        public static string Action_CreditNoteDetailsSync = "creditnotedetails_sync";
        public static string Tbl_cms_creditnote_details = "cms_creditnote_details";         /* CREDIT NOTE DETAILS */
        
        public static string Action_DebitNoteDetailsSync = "debitnotedetails_sync";
        public static string Tbl_cms_debitnote_details = "cms_debitnote_details";         /* DN DETAILS */

        public static string Action_DebitNoteSync = "debitnote_sync";
        public static string Tbl_cms_debitnote = "cms_debitnote";
        public static string Tbl_cms_debitnote_sales = "cms_debitnote_sales";

        public static string Action_ReceiptSync = "receipt_sync";
        public static string Tbl_cms_receipt = "cms_receipt";

        public static string Action_ReadImageSync = "readimage_sync";                                       /* READ IMAGE FROM SQLACC */
        public static string Tbl_cms_product_image = "cms_product_image";

        public static string Action_ProductGroupsSync = "productgroup_sync";                                /* PRODUCT GROUP */
        public static string Tbl_cms_product_group = "cms_product_group";

        public static string Action_ItemTemplateSync = "itemtemplate_sync";                                 /* ITEM TEMPLATE */
        public static string Tbl_cms_package = "cms_package";

        public static string Action_ItemTemplateDtlSync = "itemtemplatedtl_sync";                           /* ITEM TEMPLATE DETAIL */
        public static string Tbl_cms_package_dtl = "cms_package_dtl";

        public static string Action_CostPriceSync = "costPrice_sync";                                       /* COST PRICE */
        public static string Tbl_cms_product_purchase_price = "cms_product_purchase_price";

        public static string Action_UpdateCashSalesSync = "cashSalesUpdate_sync";                           /* CASH SALES UPDATE */

        public static string Action_UpdateCashSalesDtlSync = "cashSalesDetailUpdate_sync";                  /* CASH SALES DETAILS UPDATE */

        public static string Action_PostSalesOrders = "salesorders_post";
        public static string Action_PostSalesInvoices = "salesinvoices_post";
        public static string Action_PostSalesCNs = "salescns_post";

        public static string Action_Transfer_SO = "transfer_SO";
        public static string Action_Transfer_CS = "transfer_CS";
        public static string Action_Transfer_CN = "transfer_CN";
        public static string Action_Transfer_INV = "transfer_INV";
        public static string Action_Transfer_Payment = "transfer_Payment";
        public static string Action_Transfer_DO = "transfer_DO";
        public static string Action_Transfer_Stock = "transfer_Stock";
        public static string Action_APS_Transfer_SO = "APS_transfer_SO";                        /* APS */
        public static string Action_APS_Transfer_CN = "APS_transfer_CN";                        /* APS */

        public static string Action_APS_Transfer_Quotation = "APS_transfer_Quotation";          /* APS */
        public static string Action_APS_Transfer_PO_Basket = "APS_transfer_PO_Basket";          /* APS */

        public static string SQLite_Exception = "[Exception SQLite]";
        public static string Thread_Exception = "[Thread Exception]";
        public static string Job_Exception = "[Job Exception]";

        public static string Job_Listener = "ESJobListener";
        public static string Job_Group_Sync = "syncGroup";
        public static string Job_Group_Transfer = "transferGroup";
        public static string Job_StockTransfer = "stockTransferSync";
        public static string Job_Stock_Card = "StockCardSync";
        public static string Job_Product = "productSync";
        public static string Job_Warehouse = "warehouseSync";
        public static string Job_Outstanding = "outStandingSync";
        public static string Job_Invoice = "invoiceSync";
        public static string Job_InvoiceDtl = "invoiceDetailsSync";
        public static string Job_Transfer = "transferSync";
        public static string Job_Customer = "customerSync";
        public static string Job_ProductCategory = "productCategorySync";                       /*sql acc*/
        public static string Job_ProductPrice = "productPriceSync";                             /*sql acc*/
        public static string Job_Salesperson = "salespersonSync";                               /*sql acc*/
        public static string Job_CN = "cnSync";                                                 /*sql acc*/
        public static string Job_SO = "soSync";                                                 /*sql acc*/
        public static string Job_CF = "cfSync";                                                 /*sql acc*/
        public static string Job_DN = "dnSync";                                                 /*sql acc*/
        public static string Job_DNDtl = "dnDtlSync";                                                 /*sql acc*/
        public static string Job_Rcpt = "rcptSync";                                             /*sql acc*/
        public static string Job_INVSales = "invsalesSync";                                                 /*sql acc*/
        public static string Job_DNSales = "dnsalesSync";                                                 /*sql acc*/
        public static string Job_CNSales = "cnsalesSync";                                             /*sql acc*/
        public static string Job_Transfer_SO = "transfer_SO";                                   /*sql acc*/
        public static string Job_Transfer_CS = "transfer_CS";                                   /*sql acc*/
        public static string Job_Transfer_INV = "transfer_INV";                                   /*sql acc*/
        public static string Job_Stock = "stockSync";
        public static string Job_StockCategories = "stockCategoriesSync";
        public static string Job_StockUomPrice = "stockUomPriceSync";
        public static string Job_InvoiceQne = "invoiceQneSync";
        public static string Job_InvoiceDetailsQne = "invoiceQneDetailsSync";
        public static string Job_CustomerAgent = "customerAgentSync";
        public static string Job_CreditNote = "creditNoteSync";
        public static string Job_DebitNote = "debitNoteSync";
        public static string Job_Receipt = "receiptSync";
        public static string Job_Agent = "agentSync";
        public static string Job_Branch = "branchSync";
        public static string Job_Product_SpecialPrice = "productSpecialPriceSync";
        public static string Job_Post_SalesInvoices = "Post_SalesInvoices";
        public static string Job_Post_SalesOrders = "Post_SalesOrders";
        public static string Job_Post_SalesCNs = "Post_SalesCNs";
        public static string Job_Post_Payment = "PostPayment";
        public static string Job_ReadImage = "readImageSync";                                   /*READ IMAGES FROM SQLACC*/
        public static string Job_ProductGroups = "productGroupsSync";                           /* PRODUCT GROUP */
        public static string Job_ItemTemplate = "itemTemplateSync";                             /* ITEM TEMPLATE */
        public static string Job_ItemTemplateDtl = "itemTemplateDtlSync";                       /* ITEM TEMPLATE DETAIL */
        public static string Job_CostPrice = "costPriceSync";                                   /* COST PRICE */
        public static string Job_CashSales = "cashSalesUpdateSync";                             /* CASH SALES */
        public static string Job_CashSalesDtl = "cashSalesDtlUpdateSync";                       /* CASH SALES DETAILS */
        public static string Job_CreditNoteDtl = "creditNoteDtlUpdateSync";                     /* CASH SALES DETAILS */
        public static string Job_KnockOff = "knockOffSync";                                     /* KNOCKOFF */
        public static string Job_DO = "DOSync";                                                 /* DO */
        public static string Job_DO_Dtl = "DODtlSync";                                                 /* DO DTL*/
        public static string Job_Ref = "RefSync";                                                 /* REF */
        public static string Job_APSStockCategory = "APSStockCategorySync";                                                     /* APS */
        public static string Job_APSStock = "APSStockSync";                                                                     /* APS */
        public static string Job_APSStockUomPrice = "APSStockUomPriceSync";                                                     /* APS */
        public static string Job_APSStockSpecialPrice = "APSStockSpecialPriceSync";                                             /* APS */
        public static string Job_APSWarehouseQty = "APSWarehouseQtySync";                                                       /* APS */
        public static string Job_APSWarehouse = "APSWarehouseSync";                                                       /* APS */
        public static string Job_APSCustomer = "APSCustomerSync";                                                               /* APS */
        public static string Job_APSSalespersonCustomer = "APSSalespersonCustomerSync";                                         /* APS */
        public static string Job_APSSalesperson = "APSSalespersonSync";                                         /* APS */
        public static string Job_APSInvoice = "APSInvoiceSync";                                                                 /* APS */
        public static string Job_APSInvoiceDetail = "APSInvoiceDetailSync";                                                     /* APS */
        public static string Job_APSCreditNote = "APSCreditNoteSync";                                                           /* APS */
        public static string Job_APSCreditNoteDetail = "APSCreditNoteDetailSync";                                               /* APS */
        public static string Job_APSDebitNote = "APSDebitNoteSync";                                                             /* APS */
        public static string Job_APSReceipt = "APSReceiptSync";                                                                 /* APS */
        public static string Job_APSDo = "APSDoSync";                                                                           /* APS */
        public static string Job_APSDoDetail = "APSDoDetailSync";                                                               /* APS */
        public static string Job_APSRef = "APSDoRefSync";                                                                       /* APS */
        public static string Job_APSImage = "APSImageSync";                                                                       /* APS */
        public static string Job_APSTransferSO = "APSTransferSO";                                                                       /* APS */
        public static string Job_APSTransferCN = "APSTransferCN";                                                                       /* APS */
        public static string Job_APSTransferQuo = "APSTransferQuo";                                                                       /* APS */
        public static string Job_APSTransferPOBasket = "APSTransferPOBasket";         
        public static string Job_APSKORcpt = "APSKORcpt";         
        public static string Job_APSKOCN = "APSKOCN";
        public static string Job_ATCStockCategory = "ATCStockCategorySync";                                                     /* ATC */
        public static string Job_ATCStock = "ATCStockSync";                                                                     /* ATC */
        public static string Job_ATCReadyStock = "ATCReadyStockSync";                                                                     /* ATC */
        public static string Job_ATCStockUomPrice = "ATCStockUomPriceSync";                                                     /* ATC */
        public static string Job_ATCStockSpecialPrice = "ATCStockSpecialPriceSync";                                             /* ATC */
        public static string Job_ATCWarehouseQty = "ATCWarehouseQtySync";                                                       /* ATC */
        public static string Job_ATCWarehouse = "ATCWarehouseSync";                                                       /* ATC */
        public static string Job_ATCCustomer = "ATCCustomerSync";                                                               /* ATC */
        public static string Job_ATCSalespersonCustomer = "ATCSalespersonCustomerSync";                                         /* ATC */
        public static string Job_ATCSalesperson = "ATCSalespersonSync";                                         /* ATC */
        public static string Job_ATCInvoice = "ATCInvoiceSync";                                                                 /* ATC */
        public static string Job_ATCInvoiceDetail = "ATCInvoiceDetailSync";                                                     /* ATC */
        public static string Job_ATCCreditNote = "ATCCreditNoteSync";                                                           /* ATC */
        public static string Job_ATCCreditNoteDetail = "ATCCreditNoteDetailSync";                                               /* ATC */
        public static string Job_ATCDebitNote = "ATCDebitNoteSync";                                                             /* ATC */
        public static string Job_ATCReceipt = "ATCReceiptSync";                                                                 /* ATC */
        public static string Job_ATCTransferSO = "ATCTransferSO";                                                                       /* ATC */
        public static string Job_ATCTransferSOSDK = "ATCTransferSOSDK";                                                                       /* ATC */
        public static string Job_ATCBranch = "ATCBranch";                                                                       /* ATC */
        public static string Job_ATCItemTemplate = "ATCItemTemplate";                                                                       /* ATC */
        public static string Job_ATCItemTemplateDtl = "ATCItemTemplateDtl";                                                                       /* ATC */
        public static string Job_ATCOutSOSync = "ATCOutSoSync";                                                                       /* ATC */
        public static string Job_ATCStockTransferSync = "ATCStockTransferSync";                                                                       /* ATC */
        public static string Job_ATCStockCardSync = "ATCStockCardSync";                                                                       /* ATC */
        public static string Job_StockCard = "StockCardSync";                                                                       /* ATC */

        public static string Trigger_StockTransfer = "triggerStockTransfer";
        public static string Trigger_StockCard = "triggerStock_Card";
        public static string Trigger_Product = "triggerProduct";
        public static string Trigger_Warehouse = "triggerWarehouse";
        public static string Trigger_Outstanding = "triggerOutstanding";
        public static string Trigger_Invoice = "triggerInvoice";
        public static string Trigger_InvoiceDtl = "triggerInvoiceDetails";
        public static string Trigger_Transfer = "triggerTransfer";
        public static string Trigger_Customer = "triggerCustomer";
        public static string Trigger_Transfer_SO = "triggerTransfer_SO";                        /*sql acc*/
        public static string Trigger_ProductCategory = "triggerProductCategory";                /*sql acc*/
        public static string Trigger_ProductPrice = "triggerProductPrice";                      /*sql acc*/
        public static string Trigger_Salesperson = "triggerSalesperson";                        /*sql acc*/
        public static string Trigger_CN = "triggerCN";                                          /*sql acc*/
        public static string Trigger_SO = "triggerSO";                                          /*sql acc*/
        public static string Trigger_CF = "triggerCF";                                          /*sql acc*/
        public static string Trigger_DN = "triggerDN";                                          /*sql acc*/
        public static string Trigger_DNDtl = "triggerDNDtl";                                          /*sql acc*/
        public static string Trigger_Rcpt = "triggerRcpt";                                      /*sql acc*/ 
        public static string Trigger_INVSales = "triggerINVSales";                                          /*sql acc*/
        public static string Trigger_DNSales = "triggerDNSales";                                          /*sql acc*/
        public static string Trigger_CNSales = "triggerCNSales";                                      /*sql acc*/
        public static string Trigger_Agent = "triggerAgent";
        public static string Trigger_InvoiceQne = "triggerInvoiceQne";
        public static string Trigger_InvoiceDetailsQne = "triggerInvoiceDetailsQne";
        public static string Trigger_CustomerAgent = "triggerCustomerAgent";
        public static string Trigger_CreditNote = "triggerCreditNote";
        public static string Trigger_DebitNote = "triggerDebitNote";
        public static string Trigger_Receipt = "triggerReceipt";
        public static string Trigger_Stock = "triggerStock";
        public static string Trigger_StockCategories = "triggerStockCategories";
        public static string Trigger_StockUomPrice = "triggerStockUomPrice";
        public static string Trigger_Post_Payment = "triggerPost_Payment";
        public static string Trigger_Post_SalesOrders = "triggerPost_SalesOrders";
        public static string Trigger_Transfer_CS = "triggerPost_CS";
        public static string Trigger_Transfer_INV = "triggerPost_INV";
        public static string Trigger_Post_SalesInvoices = "triggerPost_SalesInvoices";
        public static string Trigger_Post_SalesCNs = "triggerPost_SalesCNs";
        public static string Trigger_Branch = "triggerBranch";
        public static string Trigger_Product_SpecialPrice = "triggerProduct_SpecialPrice";
        public static string Trigger_ReadImage = "triggerReadImage";                                            /*READ IMAGES FROM SQLACC*/
        public static string Trigger_ProductGroups = "triggerProductGroup";                                     /* PRODUCT GROUP */
        public static string Trigger_ItemTemplate = "triggerItemTemplate";                                      /* ITEM TEMPLATE */
        public static string Trigger_ItemTemplateDtl = "triggerItemTemplateDtl";                                /* ITEM TEMPLATE DTL */
        public static string Trigger_CostPrice = "triggerCostPrice";                                            /* COST PRICE */
        public static string Trigger_CashSales = "triggerCashSales";                                            /* CASH SALES */
        public static string Trigger_CashSalesDtl = "triggerCashSalesDtl";                                      /* CASH SALES DETAILS */
        public static string Trigger_CreditNoteDtl = "triggerCreditNoteDtl";                                    /* CREDIT NOTE DETAILS */
        public static string Trigger_KnockOff = "triggerKnockOff";                                              /* KnockOff */
        public static string Trigger_DO = "triggerDO";                                                          /* DO */
        public static string Trigger_DO_Dtl = "triggerDO_Dtl";                                                          /* DO_Dtl */
        public static string Trigger_Ref = "triggerRef";                                                          /* REF */
        public static string Trigger_APSStockCategory = "triggerAPSStockCategory";                                          /* APS */
        public static string Trigger_APSStock = "triggerAPSStock";                                                          /* APS */
        public static string Trigger_APSStockUomPrice = "triggerAPSStockUomPrice";                                          /* APS */
        public static string Trigger_APSStockSpecialPrice = "triggerAPSStockSpecialPrice";                                  /* APS */
        public static string Trigger_APSWarehouseQty = "triggerAPSWarehouseQty";                                            /* APS */
        public static string Trigger_APSWarehouse = "triggerAPSWarehouse";                                            /* APS */
        public static string Trigger_APSCustomer = "triggerAPSCustomer";                                                    /* APS */
        public static string Trigger_APSSalespersonCustomer = "triggerAPSSalespersonCustomer";                              /* APS */
        public static string Trigger_APSSalesperson = "triggerAPSSalesperson";                              /* APS */
        public static string Trigger_APSInvoice = "triggerAPSInvoice";                                                      /* APS */
        public static string Trigger_APSInvoiceDetail = "triggerAPSInvoiceDetail";                                          /* APS */
        public static string Trigger_APSCreditNote = "triggerAPSCreditNote";                                                /* APS */
        public static string Trigger_APSCreditNoteDetail = "triggerAPSCreditNoteDetail";                                    /* APS */
        public static string Trigger_APSDebitNote = "triggerAPSDebitNote";                                                  /* APS */
        public static string Trigger_APSReceipt = "triggerAPSReceipt";                                                      /* APS */
        public static string Trigger_APSDo = "triggerAPSDo";                                                                /* APS */
        public static string Trigger_APSDoDetail = "triggerAPSDoDetail";                                                    /* APS */
        public static string Trigger_APSRef = "triggerAPSRef";                                                    /* APS */
        public static string Trigger_APSImage = "triggerAPSImage";                                                    /* APS */
        public static string Trigger_APSTransferSO = "triggerAPSTransferSO";                                                    /* APS */
        public static string Trigger_APSTransferCN = "triggerAPSTransferCN";                                                    /* APS */
        public static string Trigger_APSTransferQuo = "triggerAPSTransferQuo";                                                    /* APS */
        public static string Trigger_APSTransferPOBasket = "triggerAPSTransferPOBasket";                                                    /* APS */
        public static string Trigger_APSKORcpt = "triggerAPSKORcpt";
        public static string Trigger_APSKOCN = "triggerAPSKOCN";
        public static string Trigger_ATCStockCategory = "triggerATCStockCategory";                                          /* ATC */
        public static string Trigger_ATCStock = "triggerATCStock";                                                          /* ATC */
        public static string Trigger_ATCReadyStock = "triggerATCReadyStock";                                                          /* ATC */
        public static string Trigger_ATCStockUomPrice = "triggerATCStockUomPrice";                                          /* ATC */
        public static string Trigger_ATCStockSpecialPrice = "triggerATCStockSpecialPrice";                                  /* ATC */
        public static string Trigger_ATCWarehouseQty = "triggerATCWarehouseQty";                                            /* ATC */
        public static string Trigger_ATCWarehouse = "triggerATCWarehouse";                                            /* ATC */
        public static string Trigger_ATCCustomer = "triggerATCCustomer";                                                    /* ATC */
        public static string Trigger_ATCSalespersonCustomer = "triggerATCSalespersonCustomer";                              /* ATC */
        public static string Trigger_ATCSalesperson = "triggerATCSalesperson";                              /* ATC */
        public static string Trigger_ATCInvoice = "triggerATCInvoice";                                                      /* ATC */
        public static string Trigger_ATCInvoiceDetail = "triggerATCInvoiceDetail";                                          /* ATC */
        public static string Trigger_ATCCreditNote = "triggerATCCreditNote";                                                /* ATC */
        public static string Trigger_ATCCreditNoteDetail = "triggerATCCreditNoteDetail";                                    /* ATC */
        public static string Trigger_ATCDebitNote = "triggerATCDebitNote";                                                  /* ATC */
        public static string Trigger_ATCReceipt = "triggerATCReceipt";                                                      /* ATC */
        public static string Trigger_ATCTransferSO = "triggerATCTransferSO";                                                    /* ATC */
        public static string Trigger_ATCTransferSOSDK = "triggerATCTransferSOSDK";                                                    /* ATC */
        public static string Trigger_ATCBranch = "triggerATCBranch";
        public static string Trigger_ATCItemTemplate = "ATCItemTemplate";                                                                       /* ATC */
        public static string Trigger_ATCItemTemplateDtl = "ATCItemTemplateDtl";  /* ATC */
        public static string Trigger_ATCOutSOSync = "ATCOutSoSync";  /* ATC */
        public static string Trigger_ATCTransferStockSync = "ATCTransferStocSync";  /* ATC */
        public static string Trigger_ATCStockCardSync = "ATCStockCardSync";  /* ATC */

        public static int High_Priority = 1;
        public static int Normal_Priority = 0;

        public static string Is_Starting = " sync is starting";
        public static string Is_Finished = " sync is finished";

        public static string Setting_AutoStart = "auto_start";
        public static string Setting_ATCv2 = "autocountV2Sdk";

        public static string Setting_Multiple_SQLAcc = "multiple_sqlacc";

        public static string Setting_Inv_Intv = "invoice_interval";
        public static string Setting_Inv_Enable = "invoice_enable";

        public static string Setting_InvDtl_Intv = "invoice_details_interval";
        public static string Setting_InvDtl_Enable = "invoice_details_enable";

        public static string Setting_OutstandingSo_Intv = "outstanding_so_interval";
        public static string Setting_OutstandingSo_Enable = "outstanding_so_enable";

        public static string Setting_Customer_Intv = "customer_interval";
        public static string Setting_Customer_Enable = "customer_enable";
        
        public static string Setting_SOSync_Intv = "sosync_interval";
        public static string Setting_SOSync_Enable = "sosync_enable"; 

        public static string Setting_Stock_Intv = "stock_interval";
        public static string Setting_Stock_Enable = "stock_enable";

        public static string Setting_StockCategories_Intv = "stockCategories_interval";
        public static string Setting_StockCategories_Enable = "stockCategories_enable";

        public static string Setting_StockUomPrice_Intv = "stockUomPrice_interval";
        public static string Setting_StockUomPrice_Enable = "stockUomPrice_enable";

        public static string Setting_Agent_Intv = "agent_interval";
        public static string Setting_Agent_Enable = "agent_enable";

        public static string Setting_InvoiceQne_Intv = "invoiceqne_interval";
        public static string Setting_InvoiceQne_Enable = "invoiceqne_enable";

        public static string Setting_InvoiceDetailsQne_Intv = "invoicedetailsqne_interval";
        public static string Setting_InvoiceDetailsQne_Enable = "invoicedetailsqne_enable";

        public static string Setting_CustomerAgent_Intv = "customeragent_interval";
        public static string Setting_CustomerAgent_Enable = "customeragent_enable";

        public static string Setting_CreditNote_Intv = "creditnote_interval";
        public static string Setting_CreditNote_Enable = "creditnote_enable";
        
        public static string Setting_CustomerRefund_Intv = "customerrefund_interval";
        public static string Setting_CustomerRefund_Enable = "customerrefund_enable";

        public static string Setting_CreditNoteDtl_Intv = "creditnotedtl_interval";                 /* CREDIT NOTE DETAILS */
        public static string Setting_CreditNoteDtl_Enable = "creditnotedtl_enable";

        public static string Setting_DebitNote_Intv = "debitnote_interval";
        public static string Setting_DebitNote_Enable = "debitnote_enable";

        public static string Setting_Receipt_Intv = "receipt_interval";
        public static string Setting_Receipt_Enable = "receipt_enable";

        public static string Setting_Post_SalesOrders_Intv = "post_salesOrders_interval";
        public static string Setting_Post_SalesOrders_Enable = "post_salesOrders_enable";
        
        public static string Setting_Post_CashSales_Intv = "post_cashSales_interval";
        public static string Setting_Post_CashSales_Enable = "post_cashSales_enable";

        public static string Setting_Post_SalesInvoices_Intv = "post_SalesInvoices_interval";
        public static string Setting_Post_SalesInvoices_Enable = "post_SalesInvoices_enable"; 
        
        public static string Setting_Post_SalesCNs_Intv = "post_SalesCNs_interval";
        public static string Setting_Post_SalesCNs_Enable = "post_SalesCNs_enable";
        
        public static string Setting_Post_Quotation_Intv = "post_Quotation_interval";
        public static string Setting_Post_Quotation_Enable = "post_Quotation_enable";
        
        public static string Setting_Post_PObasket_Intv = "post_PObasket_interval";
        public static string Setting_Post_PObasket_Enable = "post_PObasket_enable";
        
        public static string Setting_Post_Payment_Intv = "post_Payment_interval";
        public static string Setting_Post_Payment_Enable = "post_Payment_enable";
        
        public static string Setting_INV_Sales_Intv = "inv_Sales_interval";
        public static string Setting_INV_Sales_Enable = "inv_Salesenable";
        
        public static string Setting_CN_Sales_Intv = "cn_Sales_interval";
        public static string Setting_CN_Sales_Enable = "cn_Salesenable";
        
        public static string Setting_DN_Sales_Intv = "dn_Sales_interval";
        public static string Setting_DN_Sales_Enable = "dn_Salesenable";

        public static string Setting_Branch_Intv = "branch_interval";
        public static string Setting_Branch_Enable = "branch_enable";

        public static string Setting_ProductSpecialPrice_Intv = "productSpecialPrice_interval";
        public static string Setting_ProductSpecialPrice_Enable = "productSpecialPrice_enable";

        public static string Setting_ReadImage_Intv = "readImage_interval";
        public static string Setting_ReadImage_Enable = "readImage_enable";

        public static string Setting_StockCard_Intv = "stockCardinterval";                      /* STOCK CARD */
        public static string Setting_StockCard_Enable = "stockCard_enable";
        
        public static string Setting_ProductGroup_Intv = "productGroupinterval";                      /* PRODUCT GROUP */
        public static string Setting_ProductGroup_Enable = "productGroup_enable";

        public static string Setting_ItemTemplate_Intv = "itemTemplate_interval";                     /* ITEM TEMPLATE */
        public static string Setting_ItemTemplate_Enable = "itemTemplate_enable";

        public static string Setting_ItemTemplateDtl_Intv = "itemTemplateDtl_interval";                 /* ITEM TEMPLATE DTL */
        public static string Setting_ItemTemplateDtl_Enable = "itemTemplateDtl_enable";

        public static string Setting_CostPrice_Intv = "costPrice_interval";                             /* COST PRICE */
        public static string Setting_CostPrice_Enable = "costPrice_enable";

        public static string Setting_PostStock_Intv = "postStock_interval";                             /* PostStock */
        public static string Setting_PostStock_Enable = "postStock_enable";

        public static string Setting_KnockOff_Intv = "knockOff_interval";                           /* KnockOff */
        public static string Setting_KnockOff_Enable = "knockOff_enable";

        public static string Setting_Warehouse_Intv = "warehouse_interval";                           /* Warehouse */
        public static string Setting_Warehouse_Enable = "warehouse_enable"; 
        
        public static string Setting_DO_Intv = "do_interval";                                       /* DO */
        public static string Setting_DO_Enable = "do_enable";
        
        public static string Setting_Ref_Intv = "do_interval";                                       /* REF */
        public static string Setting_Ref_Enable = "do_enable";
        
        public static string Setting_TransferSO_SDK_Enable = "transferso_sdk_enable";
        public static string Setting_TransferPackageSO_SDK_Enable = "transferpackageso_sdk_enable";
        public static string Setting_ATC_V2_Enable = "ATC_V2_Enable";

        //public static string Setting_Multiple_SQLAcc_Enable = "post_ProductSpecialPrice_enable";

        public static string YES = "1";
        public static string NO = "0";
    }
}