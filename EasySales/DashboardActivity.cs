using EasySales.Model;
using EasySales.Object;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using System.Reflection;
using AutoUpdaterDotNET;
using System.Net;
using System.Drawing;
using Ubiety.Dns.Core.Records;
using Quartz;
using Quartz.Impl.Matchers;
using Quartz.Impl;
using System.Collections.Specialized;
using EasySales.Job;
using System.Runtime.Remoting.Contexts;
using EasySales.Job.APS;
using System.Threading;
using Microsoft.EntityFrameworkCore.Internal;
using System.Collections;
using System.Threading.Tasks;
using System.Linq;
//using SuperWebSocket;
using System.Net.Sockets;
using System.Text;
using WebSocketSharp.Server;
using SocketIOClient;

namespace EasySales
{
    public partial class DashboardActivity : Form
    {
        
        SQLAccApi software = null;
        private UpdateInfoEventArgs args;
        public DashboardActivity()
        {
            InitializeComponent();
            Task connected = SocketClient.Connect();

            List<DpprAccountingSoftware> getAccSoftware = LocalDB.GetAccountingSoftwares();
            DpprAccountingSoftware accSoftware = getAccSoftware[0];

            if (accSoftware.software_name == "QNE")
            {
                LocalDB.QNEDBCleanup();
            }
            else
            {
                LocalDB.DBCleanup();
            }

            Version version = Assembly.GetExecutingAssembly().GetName().Version;
            DateTime buildDate = new DateTime(2000, 1, 1)
                                    .AddDays(version.Build).AddSeconds(version.Revision * 2);
            string displayableVersion = $"(v.{version})";

            this.Text = "Dashboard " + displayableVersion;

            List<DpprMySQLconfig> mysql_list = LocalDB.GetRemoteDatabaseConfig().Distinct().ToList();
            if (mysql_list.Count > 1)
            {
                int basedIndex = 0;
                int index = mysql_list.Count - 1;

                string _compName = string.Empty;
                string compName = string.Empty;
                string companies = string.Empty;

                for (int i = 0; i < mysql_list.Count; i++)
                {
                    DpprMySQLconfig mysql_config = mysql_list[i];
                    _compName = mysql_config.config_database;
                    compName = _compName.ReplaceAll("", "easysale_");
                    compName = _compName.ReplaceAll("", "easyicec_");
                    compName = compName.ToUpper();

                    if (compName == "GMCOMMUNICATION")
                    {
                        compName = "GM HQ";
                    }
                    else if (compName == "GMCOMMUNICATION_JB")
                    {
                        compName = "";
                    }

                    companies += i == 0 ? compName : "\n" + compName;

                    Console.WriteLine(companies);
                }
                lbl_company.Text = companies;
            }
            else
            {
                DpprMySQLconfig mysql_config = mysql_list[0];
                string _companyName = mysql_config.config_database;
                string companyName = string.Empty;
                companyName = _companyName.ReplaceAll("", "easysale_");
                companyName = _companyName.ReplaceAll("", "easyicec_");

                companyName = companyName.ToUpper();

                if (companyName == "GMCOMMUNICATION")
                {
                    companyName = "GM HQ";
                }
                else if (companyName == "GMCOMMUNICATION_JB")
                {
                    companyName = "GM JOHOR";
                }

                lbl_company.Text = companyName;
            }

            //AutoUpdater.Start();
            //AutoUpdater.Start("https://easysales.asia/staging/easysales/AutoUpdaterTest.xml", new NetworkCredential("staging@easysales.asia", "staging123@"));

            //AutoUpdater.CheckForUpdateEvent += AutoUpdaterOnCheckForUpdateEvent;

            //List<DpprAccountingSoftware> getAccSoftware = LocalDB.GetAccountingSoftwares();
            //DpprAccountingSoftware accSoftware = getAccSoftware[0];

            List<DpprSQLiteSequenceLog> listSeq = LocalDB.btnAlertCrashCondition();
            if (listSeq.Count > 0)
            {
                DpprSQLiteSequenceLog value = listSeq[0];
                if (value.seq == "1")
                {
                    btn_testalert_crash.Visible = true;
                }
            }

            if (accSoftware.software_name == "SQLAccounting")
            {
                btn_trigger_sqlaccounting.Visible = true;
                btn_terminate.Visible = true;
                cb_outso.Enabled = true;
                nt_outso_intv.Enabled = true;
                cb_branch.Enabled = true;
                nt_branch_intv.Enabled = true;
                cb_productspecialprice.Enabled = true;
                nt_productspecialprice_intv.Enabled = true;
                cb_debitnote.Enabled = true;
                nt_debitnote_intv.Enabled = true;
                cb_productspecialprice.Enabled = true;
                nt_productspecialprice_intv.Enabled = true;
                cb_readimage.Enabled = true;
                nt_readimage_intv.Enabled = true;
                cb_item_template_dtl.Enabled = true;
                nt_item_template_dtl_intv.Enabled = true;
                cb_item_template.Enabled = true;
                nt_item_template_intv.Enabled = true;
                cb_productgroup.Enabled = true;
                nt_productgroup_intv.Enabled = true;
                cb_stock_transfer.Enabled = true;
                nt_post_stock_transfer.Enabled = true;
                cb_knockoff.Enabled = true;
                nt_knockoff_intv.Enabled = true;
                cb_warehouse.Enabled = true;
                nt_warehouse_intv.Enabled = true;

                cb_post_salesinvoice.Enabled = true;
                nt_post_salesinvoices_intv.Enabled = true;

                lbl_updateinfo.Visible = false;
                btn_updatenow.Visible = false;

                btn_run_custagentsync.Enabled = true;
                btn_run_ageingkosync.Enabled = true;
                btn_run_branchsync.Enabled = true;
                btn_run_cndtlsync.Enabled = true;
                btn_run_cnsync.Enabled = true;
                btn_run_dnsync.Enabled = true;
                btn_run_imagesync.Enabled = true;
                btn_run_invdtlsync.Enabled = true;
                btn_run_invsync.Enabled = true;
                btn_run_itemtmpdtlsync.Enabled = true;
                btn_run_itemtmpsync.Enabled = true;
                btn_run_outsosync.Enabled = true;
                btn_run_rcptsync.Enabled = true;
                btn_run_specialpricesync.Enabled = true;
                btn_run_stockcatsync.Enabled = true;
                btn_run_stockgroupsync.Enabled = true;
                btn_run_transfersalesinv.Enabled = true;
                btn_run_uompricesync.Enabled = true;
                btn_run_stock_transfer.Enabled = true;
                btn_run_whqtysync.Enabled = true;
                btn_run_costpricesync.Enabled = true;

                List<DpprMySQLconfig> __mysql_list = LocalDB.GetRemoteDatabaseConfig().Distinct().ToList();
                DpprMySQLconfig mysql_config = __mysql_list[0];
                string compName = mysql_config.config_database;

                if(compName == "easysale_uvjoy")
                {
                    cb_branch.Text = "Collection Interval (min)";
                }

                LocalDB.Execute("INSERT INTO sync_log (action_identifier, action_details) VALUES ('transfer_SO', 'Finished'),('transfer_CS', 'Finished'),('transfer_INV', 'Finished'),('transfer_Payment', 'Finished')");
            }
            else if (accSoftware.software_name == "QNE")
            {
                cb_post_salesorders.Enabled = false;         //true for bizsoft, false for gm
                nt_post_salesorders_intv.Enabled = false;    //true for bizsoft, false for gm
                cb_readimage.Enabled = true;
                nt_readimage_intv.Enabled = true;
                btn_trigger_sqlaccounting.Visible = false;
                btn_terminate.Visible = false;
                cb_outso.Enabled = false;
                nt_outso_intv.Enabled = false;
                cb_branch.Enabled = false;
                nt_branch_intv.Enabled = false;
                cb_productspecialprice.Enabled = false;
                nt_productspecialprice_intv.Enabled = false;
                cb_debitnote.Enabled = false;
                nt_debitnote_intv.Enabled = false;
                cb_productspecialprice.Enabled = false;
                nt_productspecialprice_intv.Enabled = false;
                cb_item_template_dtl.Enabled = false;
                nt_item_template_dtl_intv.Enabled = false;
                cb_item_template.Enabled = false;
                nt_item_template_intv.Enabled = false;
                cb_productgroup.Enabled = false;
                nt_productgroup_intv.Enabled = false;
                cb_costprice.Enabled = false;
                nt_costprice_intv.Enabled = false;
                cb_knockoff.Enabled = false;
                nt_knockoff_intv.Enabled = false;
                cb_warehouse.Enabled = false;
                nt_warehouse_intv.Enabled = false;

                nt_creditnote_details_intv.Enabled = false;
                cb_creditnote_details.Enabled = false;
                cb_stock_transfer.Enabled = false;
                cb_post_salesinvoice.Enabled = true;
                nt_post_salesinvoices_intv.Enabled = true;

                cb_sales_inv.Enabled = false;
                cb_sales_cn.Enabled = false;
                cb_sales_dn.Enabled = false;
                cb_postpobasket.Enabled = false;
                cb_postquo.Enabled = false;
                cb_cust_refund.Enabled = false;
                cb_do.Enabled = false;
                cb_sosync.Enabled = false;
                cb_postpayment.Enabled = false;
                nt_postpayment_intv.Enabled = false;
                cb_post_cashsales.Enabled = false;
                nt_post_cashsales_intv.Enabled = false;
                nt_cust_refund_intv.Enabled = false;
                nt_sosync_intv.Enabled = false;
                nt_do_intv.Enabled = false;
                nt_postpobasket_intv.Enabled = false;
                nt_post_quo_intv.Enabled = false;
                nt_sales_cn_intv.Enabled = false;
                nt_sales_dn_intv.Enabled = false;
                nt_sales_invoice_intv.Enabled = false;

                btn_run_custagentsync.Enabled = true;
                btn_run_cnsync.Enabled = true;
                btn_run_invdtlsync.Enabled = true;
                btn_run_invsync.Enabled = true;
                btn_run_rcptsync.Enabled = true;
                btn_run_stockcatsync.Enabled = true;
                btn_run_transfersalesinv.Enabled = true;
                btn_run_uompricesync.Enabled = true;
                btn_run_custsync.Enabled = true;
                btn_run_stocksync.Enabled = true;

                btn_run_ageingkosync.Enabled = false;
                btn_run_branchsync.Enabled = false;
                btn_run_cndtlsync.Enabled = false;
                btn_run_dnsync.Enabled = false;
                btn_run_imagesync.Enabled = true;
                btn_run_itemtmpdtlsync.Enabled = false;
                btn_run_itemtmpsync.Enabled = false;
                btn_run_outsosync.Enabled = false;
                btn_run_specialpricesync.Enabled = false;
                btn_run_stockgroupsync.Enabled = false;
                btn_run_stock_transfer.Enabled = false;
                btn_run_whqtysync.Enabled = false;
                btn_run_costpricesync.Enabled = false;
                btn_run_transferso.Enabled = false;
                btn_run_dosync.Enabled = false;
                btn_run_cfsync.Enabled = false;
                btn_run_postpaymentsync.Enabled = false;
                btn_run_postpobasketsync.Enabled = false;
                btn_run_salesdn.Enabled = false;
                btn_run_salescn.Enabled = false;
                btn_run_salesinv.Enabled = false;
                btn_run_sosync.Enabled = false;
                btn_run_transferquo.Enabled = false;
                btn_run_transfercashsales.Enabled = false;

                lbl_updateinfo.Visible = false;
                btn_updatenow.Visible = false;
                
                LocalDB.Execute("INSERT INTO sync_log (action_identifier, action_details) VALUES ('salesinvoices_post', 'Finished'),('salesorders_post', 'Finished'),('salescns_post', 'Finished');");
            }
            else if (accSoftware.software_name == "APS")
            {
                btn_trigger_sqlaccounting.Visible = false;
                btn_terminate.Visible = false;
                cb_outso.Enabled = true;
                nt_outso_intv.Enabled = true;
                cb_branch.Enabled = true;
                nt_branch_intv.Enabled = true;
                cb_productspecialprice.Enabled = true;
                nt_productspecialprice_intv.Enabled = true;
                cb_debitnote.Enabled = true;
                nt_debitnote_intv.Enabled = true;
                cb_productspecialprice.Enabled = true;
                nt_productspecialprice_intv.Enabled = true;
                cb_readimage.Enabled = true;
                nt_readimage_intv.Enabled = true;
                cb_item_template_dtl.Enabled = true;
                nt_item_template_dtl_intv.Enabled = true;
                cb_item_template.Enabled = true;
                nt_item_template_intv.Enabled = true;
                cb_productgroup.Enabled = true;
                nt_productgroup_intv.Enabled = true;
                cb_stock_transfer.Enabled = true;
                nt_post_stock_transfer.Enabled = true;
                cb_knockoff.Enabled = true;
                nt_knockoff_intv.Enabled = true;
                cb_warehouse.Enabled = true;
                nt_warehouse_intv.Enabled = true;

                cb_post_salesinvoice.Enabled = false;
                nt_post_salesinvoices_intv.Enabled = false;

                lbl_updateinfo.Visible = false;
                btn_updatenow.Visible = false;
                LocalDB.Execute("INSERT INTO sync_log (action_identifier, action_details) VALUES ('APS_transfer_SO', 'Finished')");
            }
            else if (accSoftware.software_name == "AutoCount")
            {
                button_test_atc_integration.Visible = true;
                cb_sdk_atc.Visible = true;
                //cb_transfer_package_sdk.Visible = true;
                btn_trigger_sqlaccounting.Visible = false;
                btn_terminate.Visible = false;
                cb_atc_v2.Visible = true;
            }
            else
            {
                btn_trigger_sqlaccounting.Visible = false;
                btn_terminate.Visible = false;
                cb_outso.Enabled = true;
                nt_outso_intv.Enabled = true;
                cb_branch.Enabled = true;
                nt_branch_intv.Enabled = true;
                cb_productspecialprice.Enabled = true;
                nt_productspecialprice_intv.Enabled = true;
                cb_debitnote.Enabled = true;
                nt_debitnote_intv.Enabled = true;
                cb_productspecialprice.Enabled = true;
                nt_productspecialprice_intv.Enabled = true;
                cb_readimage.Enabled = true;
                nt_readimage_intv.Enabled = true;
                cb_item_template_dtl.Enabled = true;
                nt_item_template_dtl_intv.Enabled = true;
                cb_item_template.Enabled = true;
                nt_item_template_intv.Enabled = true;
                cb_productgroup.Enabled = true;
                nt_productgroup_intv.Enabled = true;
                cb_stock_transfer.Enabled = true;
                nt_post_stock_transfer.Enabled = true;
                cb_knockoff.Enabled = true;
                nt_knockoff_intv.Enabled = true;
                cb_warehouse.Enabled = true;
                nt_warehouse_intv.Enabled = true;

                cb_post_salesinvoice.Enabled = false;
                nt_post_salesinvoices_intv.Enabled = false;

                lbl_updateinfo.Visible = false;
                btn_updatenow.Visible = false;
            }

            bool autostart = false;

            List<DpprUserSettings> list = LocalDB.GetUserSettings();
            for (int i = 0; i < list.Count; i++)
            {
                DpprUserSettings settings = list[i];
                if (settings.name == Constants.Setting_AutoStart)
                {
                    autostart = settings.setting == Constants.YES;
                }
                if (settings.name == Constants.Setting_Inv_Intv)
                {
                    this.nt_inv_intv.Value = int.Parse(settings.setting);
                }
                if (settings.name == Constants.Setting_InvDtl_Intv)
                {
                    this.nt_invdtl_intv.Value = int.Parse(settings.setting);
                }
                if (settings.name == Constants.Setting_OutstandingSo_Intv)
                {
                    this.nt_outso_intv.Value = int.Parse(settings.setting);
                }
                if (settings.name == Constants.Setting_Customer_Intv)
                {
                    this.nt_customer_intv.Value = int.Parse(settings.setting);
                }
                if (settings.name == Constants.Setting_Stock_Intv)
                {
                    this.nt_stock_intv.Value = int.Parse(settings.setting);
                }
                if (settings.name == Constants.Setting_StockCategories_Intv)
                {
                    this.nt_stockcategories_intv.Value = int.Parse(settings.setting);
                }
                if (settings.name == Constants.Setting_StockUomPrice_Intv)
                {
                    this.nt_stockuomprice_intv.Value = int.Parse(settings.setting);
                }
                if (settings.name == Constants.Setting_CustomerAgent_Intv)
                {
                    this.nt_customeragent_intv.Value = int.Parse(settings.setting);
                }
                if (settings.name == Constants.Setting_CreditNote_Intv)
                {
                    this.nt_creditnote_intv.Value = int.Parse(settings.setting);
                }
                if (settings.name == Constants.Setting_CreditNoteDtl_Intv)                      /* CREDIT NOTE DETAILS */
                {
                    this.nt_creditnote_details_intv.Value = int.Parse(settings.setting);
                }
                if (settings.name == Constants.Setting_DebitNote_Intv)
                {
                    this.nt_debitnote_intv.Value = int.Parse(settings.setting);
                }
                if (settings.name == Constants.Setting_CustomerRefund_Intv)
                {
                    this.nt_cust_refund_intv.Value = int.Parse(settings.setting);
                }
                if (settings.name == Constants.Setting_Receipt_Intv)
                {
                    this.nt_receipt_intv.Value = int.Parse(settings.setting);
                }
                if (settings.name == Constants.Setting_Post_SalesOrders_Intv)
                {
                    this.nt_post_salesorders_intv.Value = int.Parse(settings.setting);
                }
                if (settings.name == Constants.Setting_Post_CashSales_Intv)
                {
                    this.nt_post_cashsales_intv.Value = int.Parse(settings.setting);
                }
                if (settings.name == Constants.Setting_Post_SalesInvoices_Intv)
                {
                    this.nt_post_salesinvoices_intv.Value = int.Parse(settings.setting);
                }
                if (settings.name == Constants.Setting_Post_SalesCNs_Intv)
                {
                    this.nt_post_salescns_intv.Value = int.Parse(settings.setting);
                }
                if (settings.name == Constants.Setting_Branch_Intv)
                {
                    this.nt_branch_intv.Value = int.Parse(settings.setting);
                }
                if (settings.name == Constants.Setting_ProductSpecialPrice_Intv)
                {
                    this.nt_productspecialprice_intv.Value = int.Parse(settings.setting);
                }
                if (settings.name == Constants.Setting_ReadImage_Intv)                      /* READ IMAGE FROM SQLACC */
                {
                    this.nt_readimage_intv.Value = int.Parse(settings.setting);
                }
                if (settings.name == Constants.Setting_ProductGroup_Intv)                      /* PRODUCT GROUP */
                {
                    this.nt_productgroup_intv.Value = int.Parse(settings.setting);
                }
                if (settings.name == Constants.Setting_StockCard_Intv)                      /* STOCK CARD */
                {
                    this.nt_stockcard_intv.Value = int.Parse(settings.setting);
                }
                if (settings.name == Constants.Setting_ItemTemplate_Intv)                      /* ITEM TEMPLATE */
                {
                    this.nt_item_template_intv.Value = int.Parse(settings.setting);
                }
                if (settings.name == Constants.Setting_ItemTemplateDtl_Intv)                      /* ITEM TEMPLATE DTL */
                {
                    this.nt_item_template_dtl_intv.Value = int.Parse(settings.setting);
                }
                if (settings.name == Constants.Setting_ItemTemplateDtl_Intv)                      /* COST PRICE */
                {
                    this.nt_item_template_dtl_intv.Value = int.Parse(settings.setting);
                }
                if (settings.name == Constants.Setting_PostStock_Intv)                          /* CASH SALES */
                {
                    this.nt_post_stock_transfer.Value = int.Parse(settings.setting);
                }
                if (settings.name == Constants.Setting_KnockOff_Intv)                          /* KNOCKOFF */
                {
                    this.nt_knockoff_intv.Value = int.Parse(settings.setting);
                }
                if (settings.name == Constants.Setting_Warehouse_Intv)                          /* Warehouse APS */
                {
                    this.nt_warehouse_intv.Value = int.Parse(settings.setting);
                }
                if (settings.name == Constants.Setting_DO_Intv)                          /* DO APS */
                {
                    this.nt_do_intv.Value = int.Parse(settings.setting);
                }
                if (settings.name == Constants.Setting_Post_Quotation_Intv)                          /* QUO APS */
                {
                    this.nt_post_quo_intv.Value = int.Parse(settings.setting);
                }
                if (settings.name == Constants.Setting_Post_PObasket_Intv)                          /* PO APS */
                {
                    this.nt_postpobasket_intv.Value = int.Parse(settings.setting);
                }
                if (settings.name == Constants.Setting_Post_Payment_Intv)                          /* Transfer Payment */
                {
                    this.nt_postpayment_intv.Value = int.Parse(settings.setting);
                }
                if (settings.name == Constants.Setting_SOSync_Intv)                          /* so sync */
                {
                    this.nt_sosync_intv.Value = int.Parse(settings.setting);
                }
                if (settings.name == Constants.Setting_INV_Sales_Intv)                          /* INV SALES sync */
                {
                    this.nt_sales_invoice_intv.Value = int.Parse(settings.setting);
                }
                if (settings.name == Constants.Setting_CN_Sales_Intv)                          /* CN SALES sync */
                {
                    this.nt_sales_cn_intv.Value = int.Parse(settings.setting);
                }
                if (settings.name == Constants.Setting_DN_Sales_Intv)                          /* DN SALES sync */
                {
                    this.nt_sales_dn_intv.Value = int.Parse(settings.setting);
                }

                if (settings.name == Constants.Setting_OutstandingSo_Enable)
                {
                    this.cb_outso.Checked = settings.setting == Constants.YES;
                }
                if (settings.name == Constants.Setting_Inv_Enable)
                {
                    this.cb_invoice.Checked = settings.setting == Constants.YES;
                }
                if (settings.name == Constants.Setting_InvDtl_Enable)
                {
                    this.cb_inv_dtl.Checked = settings.setting == Constants.YES;
                }
                if (settings.name == Constants.Setting_Customer_Enable)
                {
                    this.cb_customer.Checked = settings.setting == Constants.YES;
                }
                if (settings.name == Constants.Setting_Stock_Enable)
                {
                    this.cb_stock.Checked = settings.setting == Constants.YES;
                }
                if (settings.name == Constants.Setting_StockCategories_Enable)
                {
                    this.cb_stockcategories.Checked = settings.setting == Constants.YES;
                }
                if (settings.name == Constants.Setting_StockUomPrice_Enable)
                {
                    this.cb_stockuomprice.Checked = settings.setting == Constants.YES;
                }
                if (settings.name == Constants.Setting_CustomerAgent_Enable)
                {
                    this.cb_customeragent.Checked = settings.setting == Constants.YES;
                }
                if (settings.name == Constants.Setting_CreditNote_Enable)
                {
                    this.cb_creditnote.Checked = settings.setting == Constants.YES;
                }
                if (settings.name == Constants.Setting_CreditNoteDtl_Enable)                    /* CN DETAILS */
                {
                    this.cb_creditnote_details.Checked = settings.setting == Constants.YES;
                }
                if (settings.name == Constants.Setting_DebitNote_Enable)
                {
                    this.cb_debitnote.Checked = settings.setting == Constants.YES;
                }
                if (settings.name == Constants.Setting_CustomerRefund_Enable)
                {
                    this.cb_cust_refund.Checked = settings.setting == Constants.YES;
                }
                if (settings.name == Constants.Setting_Receipt_Enable)
                {
                    this.cb_receipt.Checked = settings.setting == Constants.YES;
                }
                if (settings.name == Constants.Setting_Post_SalesOrders_Enable)
                {
                    this.cb_post_salesorders.Checked = settings.setting == Constants.YES;
                }
                if (settings.name == Constants.Setting_Post_CashSales_Enable)
                {
                    this.cb_post_cashsales.Checked = settings.setting == Constants.YES;
                }
                if (settings.name == Constants.Setting_Post_SalesInvoices_Enable)
                {
                    this.cb_post_salesinvoice.Checked = settings.setting == Constants.YES;
                }
                if (settings.name == Constants.Setting_Post_SalesCNs_Enable)
                {
                    this.cb_postsalescns.Checked = settings.setting == Constants.YES;
                }
                if (settings.name == Constants.Setting_Branch_Enable)
                {
                    this.cb_branch.Checked = settings.setting == Constants.YES;
                }
                if (settings.name == Constants.Setting_ProductSpecialPrice_Enable)
                {
                    this.cb_productspecialprice.Checked = settings.setting == Constants.YES;
                }
                if (settings.name == Constants.Setting_ReadImage_Enable)                            /* READ IMAGE FROM SQLACC */
                {
                    this.cb_readimage.Checked = settings.setting == Constants.YES;
                }
                if (settings.name == Constants.Setting_StockCard_Enable)                       /* STOCK CARD */
                {
                    this.cb_stockcard.Checked = settings.setting == Constants.YES;
                }
                if (settings.name == Constants.Setting_ProductGroup_Enable)                       /* PRODUCT GROUP */
                {
                    this.cb_productgroup.Checked = settings.setting == Constants.YES;
                }
                if (settings.name == Constants.Setting_ItemTemplate_Enable)                        /* ITEM TEMPLATE */
                {
                    this.cb_item_template.Checked = settings.setting == Constants.YES;
                }
                if (settings.name == Constants.Setting_ItemTemplateDtl_Enable)                     /* ITEM TEMPLATE DTL */
                {
                    this.cb_item_template_dtl.Checked = settings.setting == Constants.YES;
                }
                if (settings.name == Constants.Setting_CostPrice_Enable)                            /* COST PRICE */
                {
                    this.cb_costprice.Checked = settings.setting == Constants.YES;
                }
                if (settings.name == Constants.Setting_PostStock_Enable)                              /* CASH SALES */
                {
                    this.cb_stock_transfer.Checked = settings.setting == Constants.YES;
                }
                if (settings.name == Constants.Setting_KnockOff_Enable)                          /* KNOCKOFF */
                {
                    this.cb_knockoff.Checked = settings.setting == Constants.YES;
                }
                if (settings.name == Constants.Setting_Warehouse_Enable)                          /* Warehouse */
                {
                    this.cb_warehouse.Checked = settings.setting == Constants.YES;
                }
                if (settings.name == Constants.Setting_DO_Enable)                          /* DO */
                {
                    this.cb_do.Checked = settings.setting == Constants.YES;
                }
                if (settings.name == Constants.Setting_Post_Quotation_Enable)                          /* QUO */
                {
                    this.cb_postquo.Checked = settings.setting == Constants.YES;
                }
                if (settings.name == Constants.Setting_Post_PObasket_Enable)                          /* PO */
                {
                    this.cb_postpobasket.Checked = settings.setting == Constants.YES;
                }
                if (settings.name == Constants.Setting_Post_Payment_Enable)                          /* PO */
                {
                    this.cb_postpayment.Checked = settings.setting == Constants.YES;
                }
                if (settings.name == Constants.Setting_SOSync_Enable)                          /* so sync */
                {
                    this.cb_sosync.Checked = settings.setting == Constants.YES;
                }
                if (settings.name == Constants.Setting_INV_Sales_Enable)                          /* INV SALES sync */
                {
                    this.cb_sales_inv.Checked = settings.setting == Constants.YES;
                }
                if (settings.name == Constants.Setting_CN_Sales_Enable)                          /* CN SALES sync */
                {
                    this.cb_sales_cn.Checked = settings.setting == Constants.YES;
                }
                if (settings.name == Constants.Setting_DN_Sales_Enable)                          /* DN SALES sync */
                {
                    this.cb_sales_dn.Checked = settings.setting == Constants.YES;
                }
                if (settings.name == Constants.Setting_TransferSO_SDK_Enable)                          /* SDK Transfer SO ATC */
                {
                    this.cb_sdk_atc.Checked = settings.setting == Constants.YES;
                }
                if (settings.name == Constants.Setting_ATCv2)                          /*Setting_ATC_V2_Enable*/
                {
                    this.cb_atc_v2.Checked = settings.setting == Constants.YES;
                }
            }

            if (autostart)
            {
                this.cb_autoStart.Checked = true;

                MethodInvoker myProcessStarter = new MethodInvoker(TriggerWorker);

                myProcessStarter.BeginInvoke(null, null);
            }

        }

        //private void AutoUpdaterOnCheckForUpdateEvent(UpdateInfoEventArgs args)
        //{
        //    this.args = args;

        //    string newLine = Environment.NewLine;

        //    if (args != null)
        //    {
        //        if (args.IsUpdateAvailable)
        //        {
        //            lbl_updateinfo.Text = $@"There is new version {args.CurrentVersion} available. You are using version {args.InstalledVersion}." + newLine + newLine + "Press Update Now button below to begin updating the application.";
        //            btn_updatenow.Enabled = true;
        //        }
        //        else
        //        {
        //            lbl_updateinfo.Text = $@"There is no new version available. You are using version {args.InstalledVersion}.";
        //            btn_updatenow.Visible = false;
        //        }
        //    }
        //    else
        //    {
        //        lbl_updateinfo.Text = @"There is a problem reaching update server please check your internet connection and try again later.";
        //    }
        //}

        private void btn_updatenow_Click(object sender, EventArgs e)
        {
            AutoUpdater.Start("https://easysales.asia/staging/easysales/AutoUpdaterTest.xml", new NetworkCredential("staging@easysales.asia", "staging123@"));

            //try
            //{
            //    // Developer can also use Download Update dialog used by AutoUpdater.NET to download the update.
            //    AutoUpdater.DownloadUpdate();
            //}
            //catch (Exception exception)
            //{
            //    MessageBox.Show(exception.Message, exception.GetType().ToString(), MessageBoxButtons.OK, MessageBoxIcon.Error);
            //}

            
            //try
            //{
            //    if (AutoUpdater.DownloadUpdate(this.args))
            //    {
            //        Application.Exit();
            //    }
            //}
            //catch (Exception exception)
            //{
            //    MessageBox.Show(exception.Message, exception.GetType().ToString(), MessageBoxButtons.OK,
            //        MessageBoxIcon.Error);
            //}
        }

        private void btn_trigger_sqlaccounting_Click(object sender, EventArgs e)
        {

            if (SQLAccApi.CurrentState() != null)
            {
                software.Logout();
            }
            else
            {
                software = SQLAccApi.getInstance();
            }
            software.GetComServer();
        }

        private void DashboardActivity_Load(object sender, EventArgs e)
        {
            LocalDB.notification += GlobalEvenListener;
        }

        private void DashboardActivity_FormClosing(object sender, FormClosingEventArgs e)
        {
            DpprException ex = new DpprException
            {
                file_name = "EXE",
                exception = "EXE closed",
                time = DateTime.Now.ToString()
            };
            LocalDB.InsertException(ex);
            if (software != null)
            {
                software.Logout();
            }
            Suicide();
        }

        public void Suicide()
        {
            try
            {
                Process me = Process.GetCurrentProcess();
                me.Kill();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }
        private void btn_terminate_Click(object sender, EventArgs e)
        {
            DpprException ex = new DpprException
            {
                file_name = "EXE",
                exception = "EXE terminated",
                time = DateTime.Now.ToString()
            };
            LocalDB.InsertException(ex);

            SQLAccApi.getInstance().Logout();
            Suicide();
        }

        private void GlobalEvenListener(object sender, GlobalEvent e)
        {
            string currentLog = DateTime.Now.ToString() + "  :  " + e.message;
            SetLog(currentLog);
        }

        delegate void SetLogCallback(string text);

        private void SetLog(string text)
        {
            if (this.logview.InvokeRequired)
            {
                SetLogCallback d = new SetLogCallback(SetLog);
                this.Invoke(d, new object[] { text });
            }
            else
            {
                if(this.logview.Text.Length % 100000 == 0)
                {
                    this.logview.Text = DateTime.Now.ToString() + "  :  " + "-------------Cleared previous session-------------";
                }
                this.logview.AppendText(Environment.NewLine + text);
            }
        }

        private void cb_autoStart_CheckedChanged(object sender, EventArgs e)
        {
            DpprUserSettings settings = new DpprUserSettings();
            settings.name = Constants.Setting_AutoStart;

            if (cb_autoStart.Checked)
            {
                settings.setting = Constants.YES;
            }
            else
            {
                settings.setting = Constants.NO;
            }

            LocalDB.InsertUserSetting(settings);
        }

        private void cb_atc_v2_CheckedChanged(object sender, EventArgs e)
        {
            DpprUserSettings settings = new DpprUserSettings();
            settings.name = Constants.Setting_ATCv2;

            if (cb_atc_v2.Checked)
            {
                settings.setting = Constants.YES;
            }
            else
            {
                settings.setting = Constants.NO;
            }

            LocalDB.InsertUserSetting(settings);
        }

        private void cb_sdk_atc_CheckedChanged(object sender, EventArgs e)    /* SDK ATC */
        {
            DpprUserSettings settings = new DpprUserSettings();
            settings.name = Constants.Setting_TransferSO_SDK_Enable;

            if (cb_sdk_atc.Checked)
            {
                settings.setting = Constants.YES;
            }
            else
            {
                settings.setting = Constants.NO;
            }

            LocalDB.InsertUserSetting(settings);
        }

        private void nt_inv_intv_ValueChanged(object sender, EventArgs e)
        {
            DpprUserSettings settings = new DpprUserSettings();
            settings.name = Constants.Setting_Inv_Intv;
            settings.setting = this.nt_inv_intv.Value.ToString();

            LocalDB.InsertUserSetting(settings);
        }

        private void nt_invdtl_intv_ValueChanged(object sender, EventArgs e)
        {
            DpprUserSettings settings = new DpprUserSettings();
            settings.name = Constants.Setting_InvDtl_Intv;
            settings.setting = this.nt_invdtl_intv.Value.ToString();

            LocalDB.InsertUserSetting(settings);
        }
        
        private void nt_sosync_intv_ValueChanged(object sender, EventArgs e)                /* so sync */
        {
            DpprUserSettings settings = new DpprUserSettings();
            settings.name = Constants.Setting_SOSync_Intv;
            settings.setting = this.nt_sosync_intv.Value.ToString();

            LocalDB.InsertUserSetting(settings);
        }

        private void nt_outso_intv_ValueChanged(object sender, EventArgs e)
        {
            DpprUserSettings settings = new DpprUserSettings();
            settings.name = Constants.Setting_OutstandingSo_Intv;
            settings.setting = this.nt_outso_intv.Value.ToString();

            LocalDB.InsertUserSetting(settings);
        }

        private void nt_customer_intv_ValueChanged(object sender, EventArgs e) 
        {
            DpprUserSettings settings = new DpprUserSettings();
            settings.name = Constants.Setting_Customer_Intv;
            settings.setting = this.nt_customer_intv.Value.ToString();

            LocalDB.InsertUserSetting(settings);
        }
        private void nt_stock_intv_ValueChanged(object sender, EventArgs e) 
        {
            DpprUserSettings settings = new DpprUserSettings();
            settings.name = Constants.Setting_Stock_Intv;
            settings.setting = this.nt_stock_intv.Value.ToString();

            LocalDB.InsertUserSetting(settings);
        }

        private void nt_stockcategories_intv_ValueChanged(object sender, EventArgs e) 
        {
            DpprUserSettings settings = new DpprUserSettings();
            settings.name = Constants.Setting_StockCategories_Intv;
            settings.setting = this.nt_stockcategories_intv.Value.ToString();

            LocalDB.InsertUserSetting(settings);
        }

        private void nt_stockuomprice_intv_ValueChanged(object sender, EventArgs e) 
        {
            DpprUserSettings settings = new DpprUserSettings();
            settings.name = Constants.Setting_StockUomPrice_Intv;
            settings.setting = this.nt_stockuomprice_intv.Value.ToString();

            LocalDB.InsertUserSetting(settings);
        }
        private void nt_customeragent_intv_ValueChanged(object sender, EventArgs e) 
        {
            DpprUserSettings settings = new DpprUserSettings();
            settings.name = Constants.Setting_CustomerAgent_Intv;
            settings.setting = this.nt_customeragent_intv.Value.ToString();

            LocalDB.InsertUserSetting(settings);
        }

        private void nt_creditnote_intv_ValueChanged(object sender, EventArgs e) 
        {
            DpprUserSettings settings = new DpprUserSettings();
            settings.name = Constants.Setting_CreditNote_Intv;
            settings.setting = this.nt_creditnote_intv.Value.ToString();

            LocalDB.InsertUserSetting(settings);
        }
        
        private void nt_creditnote_details_intv_ValueChanged(object sender, EventArgs e)        /* CN DETAILS */
        {
            DpprUserSettings settings = new DpprUserSettings();
            settings.name = Constants.Setting_CreditNoteDtl_Intv;
            settings.setting = this.nt_creditnote_details_intv.Value.ToString();

            LocalDB.InsertUserSetting(settings);
        }
        
        private void nt_debitnote_intv_ValueChanged(object sender, EventArgs e) 
        {
            DpprUserSettings settings = new DpprUserSettings();
            settings.name = Constants.Setting_DebitNote_Intv;
            settings.setting = this.nt_debitnote_intv.Value.ToString();

            LocalDB.InsertUserSetting(settings);
        }
        
        private void nt_cust_refund_intv_ValueChanged(object sender, EventArgs e) 
        {
            DpprUserSettings settings = new DpprUserSettings();
            settings.name = Constants.Setting_CustomerRefund_Intv;
            settings.setting = this.nt_cust_refund_intv.Value.ToString();

            LocalDB.InsertUserSetting(settings);
        }
        
        private void nt_receipt_intv_ValueChanged(object sender, EventArgs e)
        {
            DpprUserSettings settings = new DpprUserSettings();
            settings.name = Constants.Setting_Receipt_Intv;
            settings.setting = this.nt_receipt_intv.Value.ToString();

            LocalDB.InsertUserSetting(settings);
        }
        private void nt_post_salesorders_intv_ValueChanged(object sender, EventArgs e)
        {
            DpprUserSettings settings = new DpprUserSettings();
            settings.name = Constants.Setting_Post_SalesOrders_Intv;
            settings.setting = this.nt_post_salesorders_intv.Value.ToString();

            LocalDB.InsertUserSetting(settings);
        }
        
        private void nt_post_cashsales_intv_ValueChanged(object sender, EventArgs e)
        {
            DpprUserSettings settings = new DpprUserSettings();
            settings.name = Constants.Setting_Post_CashSales_Intv;
            settings.setting = this.nt_post_cashsales_intv.Value.ToString();

            LocalDB.InsertUserSetting(settings);
        }

        private void nt_post_salesinvoices_intv_ValueChanged(object sender, EventArgs e)
        {
            DpprUserSettings settings = new DpprUserSettings();
            settings.name = Constants.Setting_Post_SalesInvoices_Intv;
            settings.setting = this.nt_post_salesinvoices_intv.Value.ToString();

            LocalDB.InsertUserSetting(settings);
        } 
        
        private void nt_post_salescns_intv_ValueChanged(object sender, EventArgs e)
        {
            DpprUserSettings settings = new DpprUserSettings();
            settings.name = Constants.Setting_Post_SalesInvoices_Intv;
            settings.setting = this.nt_post_salesinvoices_intv.Value.ToString();

            LocalDB.InsertUserSetting(settings);
        }
        private void nt_branch_intv_ValueChanged(object sender, EventArgs e)
        {
            DpprUserSettings settings = new DpprUserSettings();
            settings.name = Constants.Setting_Branch_Intv;
            settings.setting = this.nt_branch_intv.Value.ToString();

            LocalDB.InsertUserSetting(settings);
        }
        private void nt_productspecialprice_intv_ValueChanged(object sender, EventArgs e)
        {
            DpprUserSettings settings = new DpprUserSettings();
            settings.name = Constants.Setting_ProductSpecialPrice_Intv;
            settings.setting = this.nt_productspecialprice_intv.Value.ToString();

            LocalDB.InsertUserSetting(settings);
        }
        
        private void nt_readimage_intv_ValueChanged(object sender, EventArgs e)                 /* READ IMAGES FROM SQLACC */
        {
            DpprUserSettings settings = new DpprUserSettings();
            settings.name = Constants.Setting_ReadImage_Intv;
            settings.setting = this.nt_readimage_intv.Value.ToString();

            LocalDB.InsertUserSetting(settings);
        }
        private void nt_productgroup_intv_ValueChanged(object sender, EventArgs e)         /* PRODUCT GROUP */
        {
            DpprUserSettings settings = new DpprUserSettings();
            settings.name = Constants.Setting_ProductGroup_Intv;
            settings.setting = this.nt_productgroup_intv.Value.ToString();

            LocalDB.InsertUserSetting(settings);
        }
        
        private void nt_stockcard_intv_ValueChanged(object sender, EventArgs e)         /* STOCK CARD */
        {
            DpprUserSettings settings = new DpprUserSettings();
            settings.name = Constants.Setting_StockCard_Intv;
            settings.setting = this.nt_stockcard_intv.Value.ToString();

            LocalDB.InsertUserSetting(settings);
        }
        private void nt_item_template_intv_ValueChanged(object sender, EventArgs e)         /* ITEM TEMPLATE */
        {
            DpprUserSettings settings = new DpprUserSettings();
            settings.name = Constants.Setting_ItemTemplate_Intv;
            settings.setting = this.nt_item_template_intv.Value.ToString();

            LocalDB.InsertUserSetting(settings);
        }
        private void nt_item_template_dtl_intv_ValueChanged(object sender, EventArgs e)         /* ITEM TEMPLATE DTL */
        {
            DpprUserSettings settings = new DpprUserSettings();
            settings.name = Constants.Setting_ItemTemplateDtl_Intv;
            settings.setting = this.nt_item_template_dtl_intv.Value.ToString();

            LocalDB.InsertUserSetting(settings);
        }
        
        private void nt_costprice_intv_ValueChanged(object sender, EventArgs e)                      /* COST PRICE */
        {
            DpprUserSettings settings = new DpprUserSettings();
            settings.name = Constants.Setting_CostPrice_Intv;
            settings.setting = this.nt_costprice_intv.Value.ToString();

            LocalDB.InsertUserSetting(settings);
        }
        private void nt_update_cashsales_intv_ValueChanged(object sender, EventArgs e)                      /* CASH SALES */
        {
            DpprUserSettings settings = new DpprUserSettings();
            settings.name = Constants.Setting_PostStock_Intv;
            settings.setting = this.nt_post_stock_transfer.Value.ToString();

            LocalDB.InsertUserSetting(settings);
        }
        
        private void nt_knockoff_intv_ValueChanged(object sender, EventArgs e)                      /* KNOCKOFF */
        {
            DpprUserSettings settings = new DpprUserSettings();
            settings.name = Constants.Setting_KnockOff_Intv;
            settings.setting = this.nt_knockoff_intv.Value.ToString();

            LocalDB.InsertUserSetting(settings);
        }

        private void nt_warehouse_intv_ValueChanged(object sender, EventArgs e)                      /* KNOCKOFF */
        {
            DpprUserSettings settings = new DpprUserSettings();
            settings.name = Constants.Setting_Warehouse_Intv;
            settings.setting = this.nt_warehouse_intv.Value.ToString();

            LocalDB.InsertUserSetting(settings);
        }
        private void nt_do_intv_ValueChanged(object sender, EventArgs e)                      /* DO */
        {
            DpprUserSettings settings = new DpprUserSettings();
            settings.name = Constants.Setting_DO_Intv;
            settings.setting = this.nt_do_intv.Value.ToString();

            LocalDB.InsertUserSetting(settings);
        }
        private void nt_post_quo_intv_ValueChanged(object sender, EventArgs e)                      /* POST QUO */
        {
            DpprUserSettings settings = new DpprUserSettings();
            settings.name = Constants.Setting_Post_Quotation_Intv;
            settings.setting = this.nt_post_quo_intv.ToString();

            LocalDB.InsertUserSetting(settings);
        }
        
        private void nt_postpobasket_intv_ValueChanged(object sender, EventArgs e)                      /* POST PO BASKET */
        {
            DpprUserSettings settings = new DpprUserSettings();
            settings.name = Constants.Setting_Post_PObasket_Intv;
            settings.setting = this.nt_postpobasket_intv.Value.ToString();

            LocalDB.InsertUserSetting(settings);
        }
        
        private void nt_postpayment_intv_ValueChanged(object sender, EventArgs e)                      /* POST PAYMENT */
        {
            DpprUserSettings settings = new DpprUserSettings();
            settings.name = Constants.Setting_Post_Payment_Intv;
            settings.setting = this.nt_postpayment_intv.Value.ToString();

            LocalDB.InsertUserSetting(settings);
        } 
        private void nt_sales_invoice_intv_ValueChanged(object sender, EventArgs e)                      /* INV SALES */
        {
            DpprUserSettings settings = new DpprUserSettings();
            settings.name = Constants.Setting_INV_Sales_Intv; //"inv_Sales_interval"
            settings.setting = this.nt_sales_invoice_intv.Value.ToString(); //100

            LocalDB.InsertUserSetting(settings);
        } 
        private void nt_sales_cn_intv_ValueChanged(object sender, EventArgs e)                      /* CN SALES */
        {
            DpprUserSettings settings = new DpprUserSettings();
            settings.name = Constants.Setting_CN_Sales_Intv;
            settings.setting = this.nt_sales_cn_intv.Value.ToString();

            LocalDB.InsertUserSetting(settings);
        } 

        private void nt_sales_dn_intv_ValueChanged(object sender, EventArgs e)                      /* DN SALES */
        {
            DpprUserSettings settings = new DpprUserSettings();
            settings.name = Constants.Setting_DN_Sales_Intv;
            settings.setting = this.nt_sales_dn_intv.Value.ToString();

            LocalDB.InsertUserSetting(settings);
        }

        private void btn_reset_setting_Click(object sender, EventArgs e)
        {
            LocalDB.Execute("DELETE FROM accounting_software;DELETE FROM configuration;DELETE FROM user_settings;DELETE FROM ftp_server;DELETE FROM sql_server");

            SQLAccApi.getInstance().Logout();
            Suicide();
        }

        private void cb_outso_CheckedChanged(object sender, EventArgs e)
        {
            DpprUserSettings settings = new DpprUserSettings();
            settings.name = Constants.Setting_OutstandingSo_Enable;

            if (cb_outso.Checked)
            {
                settings.setting = Constants.YES;
            }
            else
            {
                settings.setting = Constants.NO;
            }

            LocalDB.InsertUserSetting(settings);
        }

        private void cb_invoice_CheckedChanged(object sender, EventArgs e)
        {
            DpprUserSettings settings = new DpprUserSettings();
            settings.name = Constants.Setting_Inv_Enable;

            if (cb_invoice.Checked)
            {
                settings.setting = Constants.YES;
            }
            else
            {
                settings.setting = Constants.NO;
            }

            LocalDB.InsertUserSetting(settings);
        }

        private void cb_inv_dtl_CheckedChanged(object sender, EventArgs e)
        {
            DpprUserSettings settings = new DpprUserSettings();
            settings.name = Constants.Setting_InvDtl_Enable;

            if (cb_inv_dtl.Checked)
            {
                settings.setting = Constants.YES;
            }
            else
            {
                settings.setting = Constants.NO;
            }

            LocalDB.InsertUserSetting(settings);
        }
        
        private void cb_sosync_CheckedChanged(object sender, EventArgs e)               /* so sync */
        {
            DpprUserSettings settings = new DpprUserSettings();
            settings.name = Constants.Setting_SOSync_Enable;

            if (cb_sosync.Checked)
            {
                settings.setting = Constants.YES;
            }
            else
            {
                settings.setting = Constants.NO;
            }

            LocalDB.InsertUserSetting(settings);
        }

        private void cb_customer_CheckedChanged(object sender, EventArgs e)
        {
            DpprUserSettings settings = new DpprUserSettings();
            settings.name = Constants.Setting_Customer_Enable;

            if (cb_customer.Checked)
            {
                settings.setting = Constants.YES;
            }
            else
            {
                settings.setting = Constants.NO;
            }

            LocalDB.InsertUserSetting(settings);
        } 
        private void cb_stock_CheckedChanged(object sender, EventArgs e) 
        {
            DpprUserSettings settings = new DpprUserSettings();
            settings.name = Constants.Setting_Stock_Enable;

            if (cb_stock.Checked)
            {
                settings.setting = Constants.YES;
            }
            else
            {
                settings.setting = Constants.NO;
            }

            LocalDB.InsertUserSetting(settings);
        }

        private void cb_stockcategories_CheckedChanged(object sender, EventArgs e) 
        {
            DpprUserSettings settings = new DpprUserSettings();
            settings.name = Constants.Setting_StockCategories_Enable;

            if (cb_stockcategories.Checked)
            {
                settings.setting = Constants.YES;
            }
            else
            {
                settings.setting = Constants.NO;
            }

            LocalDB.InsertUserSetting(settings);
        }

        private void cb_stockuomprice_CheckedChanged(object sender, EventArgs e) 
        {
            DpprUserSettings settings = new DpprUserSettings();
            settings.name = Constants.Setting_StockUomPrice_Enable;

            if (cb_stockuomprice.Checked)
            {
                settings.setting = Constants.YES;
            }
            else
            {
                settings.setting = Constants.NO;
            }

            LocalDB.InsertUserSetting(settings);
        }
        private void cb_customeragent_CheckedChanged(object sender, EventArgs e) 
        {
            DpprUserSettings settings = new DpprUserSettings();
            settings.name = Constants.Setting_CustomerAgent_Enable;

            if (cb_customeragent.Checked)
            {
                settings.setting = Constants.YES;
            }
            else
            {
                settings.setting = Constants.NO;
            }

            LocalDB.InsertUserSetting(settings);
        }

        private void cb_creditnote_CheckedChanged(object sender, EventArgs e) 
        {
            DpprUserSettings settings = new DpprUserSettings();
            settings.name = Constants.Setting_CreditNote_Enable;

            if (cb_creditnote.Checked)
            {
                settings.setting = Constants.YES;
            }
            else
            {
                settings.setting = Constants.NO;
            }

            LocalDB.InsertUserSetting(settings);
        }
        
        private void cb_creditnote_details_CheckedChanged(object sender, EventArgs e) 
        {
            DpprUserSettings settings = new DpprUserSettings();
            settings.name = Constants.Setting_CreditNoteDtl_Enable;

            if (cb_creditnote_details.Checked)
            {
                settings.setting = Constants.YES;
            }
            else
            {
                settings.setting = Constants.NO;
            }

            LocalDB.InsertUserSetting(settings);
        }
        
        private void cb_debitnote_CheckedChanged(object sender, EventArgs e) 
        {
            DpprUserSettings settings = new DpprUserSettings();
            settings.name = Constants.Setting_DebitNote_Enable;

            if (cb_debitnote.Checked)
            {
                settings.setting = Constants.YES;
            }
            else
            {
                settings.setting = Constants.NO;
            }

            LocalDB.InsertUserSetting(settings);
        }
        
        private void cb_cust_refund_CheckedChanged(object sender, EventArgs e) 
        {
            DpprUserSettings settings = new DpprUserSettings();
            settings.name = Constants.Setting_CustomerRefund_Enable;

            if (cb_cust_refund.Checked)
            {
                settings.setting = Constants.YES;
            }
            else
            {
                settings.setting = Constants.NO;
            }

            LocalDB.InsertUserSetting(settings);
        }

        private void cb_receipt_CheckedChanged(object sender, EventArgs e)
        {
            DpprUserSettings settings = new DpprUserSettings();
            settings.name = Constants.Setting_Receipt_Enable;

            if (cb_receipt.Checked)
            {
                settings.setting = Constants.YES;
            }
            else
            {
                settings.setting = Constants.NO;
            }

            LocalDB.InsertUserSetting(settings);
        }
        private void cb_post_salesorders_CheckedChanged(object sender, EventArgs e)
        {
            DpprUserSettings settings = new DpprUserSettings();
            settings.name = Constants.Setting_Post_SalesOrders_Enable;

            if (cb_post_salesorders.Checked)
            {
                settings.setting = Constants.YES;
            }
            else
            {
                settings.setting = Constants.NO;
            }

            LocalDB.InsertUserSetting(settings);
        }
        
        private void cb_post_cashsales_CheckedChanged(object sender, EventArgs e)
        {
            DpprUserSettings settings = new DpprUserSettings();
            settings.name = Constants.Setting_Post_CashSales_Enable;

            if (cb_post_cashsales.Checked)
            {
                settings.setting = Constants.YES;
            }
            else
            {
                settings.setting = Constants.NO;
            }

            LocalDB.InsertUserSetting(settings);
        }
        
        private void cb_sales_cn_CheckedChanged(object sender, EventArgs e)
        {
            DpprUserSettings settings = new DpprUserSettings();
            settings.name = Constants.Setting_CN_Sales_Enable;

            if (cb_sales_cn.Checked)
            {
                settings.setting = Constants.YES;
            }
            else
            {
                settings.setting = Constants.NO;
            }

            LocalDB.InsertUserSetting(settings);
        }
        private void cb_sales_dn_CheckedChanged(object sender, EventArgs e)
        {
            DpprUserSettings settings = new DpprUserSettings();
            settings.name = Constants.Setting_DN_Sales_Enable;

            if (cb_sales_dn.Checked)
            {
                settings.setting = Constants.YES;
            }
            else
            {
                settings.setting = Constants.NO;
            }

            LocalDB.InsertUserSetting(settings);
        }
        
        private void cb_sales_inv_CheckedChanged(object sender, EventArgs e)
        {
            DpprUserSettings settings = new DpprUserSettings();
            settings.name = Constants.Setting_INV_Sales_Enable;

            if (cb_sales_inv.Checked)
            {
                settings.setting = Constants.YES;
            }
            else
            {
                settings.setting = Constants.NO;
            }

            LocalDB.InsertUserSetting(settings);
        }
        private void cb_post_salesinvoices_CheckedChanged(object sender, EventArgs e)
        {
            DpprUserSettings settings = new DpprUserSettings();
            settings.name = Constants.Setting_Post_SalesInvoices_Enable;

            if (cb_post_salesinvoice.Checked)
            {
                settings.setting = Constants.YES;
            }
            else
            {
                settings.setting = Constants.NO;
            }

            LocalDB.InsertUserSetting(settings);
        } 
        
        private void cb_postsalescns_CheckedChanged(object sender, EventArgs e)
        {
            DpprUserSettings settings = new DpprUserSettings();
            settings.name = Constants.Setting_Post_SalesCNs_Enable;

            if (cb_postsalescns.Checked)
            {
                settings.setting = Constants.YES;
            }
            else
            {
                settings.setting = Constants.NO;
            }

            LocalDB.InsertUserSetting(settings);
        }
        
        private void cb_branch_CheckedChanged(object sender, EventArgs e)
        {
            DpprUserSettings settings = new DpprUserSettings();
            settings.name = Constants.Setting_Branch_Enable;

            if (cb_branch.Checked)
            {
                settings.setting = Constants.YES;
            }
            else
            {
                settings.setting = Constants.NO;
            }

            LocalDB.InsertUserSetting(settings);
        }
        
        private void cb_productspecialprice_CheckedChanged(object sender, EventArgs e)
        {
            DpprUserSettings settings = new DpprUserSettings();
            settings.name = Constants.Setting_ProductSpecialPrice_Enable;

            if (cb_productspecialprice.Checked)
            {
                settings.setting = Constants.YES;
            }
            else
            {
                settings.setting = Constants.NO;
            }

            LocalDB.InsertUserSetting(settings);
        }
        
        private void cb_readimage_CheckedChanged(object sender, EventArgs e)                    /* READ IMAGES FROM SQLACC */
        {
            DpprUserSettings settings = new DpprUserSettings();
            settings.name = Constants.Setting_ReadImage_Enable;

            if (cb_readimage.Checked)
            {
                settings.setting = Constants.YES;
            }
            else
            {
                settings.setting = Constants.NO;
            }

            LocalDB.InsertUserSetting(settings);
        }

        private void cb_stockcard_CheckedChanged(object sender, EventArgs e)                    /* STOCK CARD */
        {
            DpprUserSettings settings = new DpprUserSettings();
            settings.name = Constants.Setting_StockCard_Enable;

            if (cb_stockcard.Checked)
            {
                settings.setting = Constants.YES;
            }
            else
            {
                settings.setting = Constants.NO;
            }

            LocalDB.InsertUserSetting(settings);
        }
        
        private void cb_productgroup_CheckedChanged(object sender, EventArgs e)                    /* PRODUCT GROUP */
        {
            DpprUserSettings settings = new DpprUserSettings();
            settings.name = Constants.Setting_ProductGroup_Enable;

            if (cb_productgroup.Checked)
            {
                settings.setting = Constants.YES;
            }
            else
            {
                settings.setting = Constants.NO;
            }

            LocalDB.InsertUserSetting(settings);
        }

        private void cb_item_template_CheckedChanged(object sender, EventArgs e)                    /* ITEM TEMPLATE */
        {
            DpprUserSettings settings = new DpprUserSettings();
            settings.name = Constants.Setting_ItemTemplate_Enable;

            if (cb_item_template.Checked)
            {
                settings.setting = Constants.YES;
            }
            else
            {
                settings.setting = Constants.NO;
            }

            LocalDB.InsertUserSetting(settings);
        }
        private void cb_item_template_dtl_CheckedChanged(object sender, EventArgs e)                    /* ITEM TEMPLATE DTL */
        {
            DpprUserSettings settings = new DpprUserSettings();
            settings.name = Constants.Setting_ItemTemplateDtl_Enable;

            if (cb_item_template_dtl.Checked)
            {
                settings.setting = Constants.YES;
            }
            else
            {
                settings.setting = Constants.NO;
            }

            LocalDB.InsertUserSetting(settings);
        }
        private void cb_costprice_CheckedChanged(object sender, EventArgs e)                            /* COST PRICE */
        {
            DpprUserSettings settings = new DpprUserSettings();
            settings.name = Constants.Setting_CostPrice_Enable;

            if (cb_costprice.Checked)
            {
                settings.setting = Constants.YES;
            }
            else
            {
                settings.setting = Constants.NO;
            }

            LocalDB.InsertUserSetting(settings);
        }
        private void cb_updatecashsales_CheckedChanged(object sender, EventArgs e)                            /* CASH SALES */
        {
            DpprUserSettings settings = new DpprUserSettings();
            settings.name = Constants.Setting_PostStock_Enable;

            if (cb_stock_transfer.Checked)
            {
                settings.setting = Constants.YES;
            }
            else
            {
                settings.setting = Constants.NO;
            }

            LocalDB.InsertUserSetting(settings);
        }
        
        private void cb_knockoff_CheckedChanged(object sender, EventArgs e)                            /* KNOCKOFF */
        {
            DpprUserSettings settings = new DpprUserSettings();
            settings.name = Constants.Setting_KnockOff_Enable;

            if (cb_knockoff.Checked)
            {
                settings.setting = Constants.YES;
            }
            else
            {
                settings.setting = Constants.NO;
            }

            LocalDB.InsertUserSetting(settings);
        }

        private void cb_warehouse_CheckedChanged(object sender, EventArgs e)                            /* Warehouse */
        {
            DpprUserSettings settings = new DpprUserSettings();
            settings.name = Constants.Setting_Warehouse_Enable;

            if (cb_warehouse.Checked)
            {
                settings.setting = Constants.YES;
            }
            else
            {
                settings.setting = Constants.NO;
            }

            LocalDB.InsertUserSetting(settings);
        }
        private void cb_do_CheckedChanged(object sender, EventArgs e)                            /* DO */
        {
            DpprUserSettings settings = new DpprUserSettings();
            settings.name = Constants.Setting_DO_Enable;

            if (cb_do.Checked)
            {
                settings.setting = Constants.YES;
            }
            else
            {
                settings.setting = Constants.NO;
            }

            LocalDB.InsertUserSetting(settings);
        }
        
        private void cb_postquo_CheckedChanged(object sender, EventArgs e)                            /* APS POST QUO */
        {
            DpprUserSettings settings = new DpprUserSettings();
            settings.name = Constants.Setting_Post_Quotation_Enable;

            if (cb_postquo.Checked)
            {
                settings.setting = Constants.YES;
            }
            else
            {
                settings.setting = Constants.NO;
            }

            LocalDB.InsertUserSetting(settings);
        }
        
        private void cb_postpobasket_CheckedChanged(object sender, EventArgs e)                            /* APS POST PO Basket */
        {
            DpprUserSettings settings = new DpprUserSettings();
            settings.name = Constants.Setting_Post_PObasket_Enable;

            if (cb_postpobasket.Checked)
            {
                settings.setting = Constants.YES;
            }
            else
            {
                settings.setting = Constants.NO;
            }

            LocalDB.InsertUserSetting(settings);
        }
        
        private void cb_postpayment_CheckedChanged(object sender, EventArgs e)                            /* POST Payment */
        {
            DpprUserSettings settings = new DpprUserSettings();
            settings.name = Constants.Setting_Post_Payment_Enable;

            if (cb_postpayment.Checked)
            {
                settings.setting = Constants.YES;
            }
            else
            {
                settings.setting = Constants.NO;
            }

            LocalDB.InsertUserSetting(settings);
        }
        
        private void btn_run_sync_Click(object sender, EventArgs e)
        {
            MethodInvoker myProcessStarter = new MethodInvoker(TriggerWorker);

            myProcessStarter.BeginInvoke(null, null);
        }

        private void TriggerWorker()
        {
            JobQueue.Run().GetAwaiter().GetResult();
        }

        private void btn_run_stocksync_Click(object sender, EventArgs e)
        {
            GlobalLogger logger = new GlobalLogger();
            List<DpprAccountingSoftware> getAccSoftware = LocalDB.GetAccountingSoftwares();
            DpprAccountingSoftware accSoftware = getAccSoftware[0];

            if (accSoftware.software_name == "SQLAccounting")
            {
                logger.message = "[SQL Accounting] stock sync is set to run now";
                logger.Broadcast();
                new JobProductSync().Execute();
            }
            else if (accSoftware.software_name == "QNE")
            {
                logger.message = "[QNE] stock sync is set to run now";
                logger.Broadcast();
                new JobStockSync().Execute();
            }
            else if (accSoftware.software_name == "APS")
            {
                logger.message = "[APS] stock sync is set to run now";
                logger.Broadcast();
                new JobAPSStockSync().Execute();
            }
            else if (accSoftware.software_name == "AutoCount")
            {
                logger.message = "[AUTOCOUNT] stock sync is set to run now";
                logger.Broadcast();
                new JobATCStockSync().Execute();
                if(cb_sdk_atc.Checked)
                {
                    //new JobATCReadyStock().Execute();
                    //new JobATCStockCardSync().Execute();
                }
            }
            else if (accSoftware.software_name == "Sage UBS")
            {
                logger.message = "[Sage UBS] stock sync is set to run now";
                logger.Broadcast();
            }
            else
            {
                logger.message = "This button is unable for the moment";
                logger.Broadcast();
            }
        }

        private void btn_run_stockcardsync_Click(object sender, EventArgs e)
        {
            GlobalLogger logger = new GlobalLogger();
            List<DpprAccountingSoftware> getAccSoftware = LocalDB.GetAccountingSoftwares();
            DpprAccountingSoftware accSoftware = getAccSoftware[0];

            if (accSoftware.software_name == "SQLAccounting")
            {
                logger.message = "[SQL Accounting] stock card sync is set to run now";
                logger.Broadcast();
                new JobStockCardSync().Execute();
            }
            else if (accSoftware.software_name == "AutoCount")
            {
                logger.message = "[ATC] stock card sync is set to run now";
                logger.Broadcast();
                new JobATCStockCardSync().Execute();
            }
            else 
            {
                //dont assign any job
            }
        }

        private void btn_run_custsync_Click(object sender, EventArgs e)
        {
            GlobalLogger logger = new GlobalLogger();
            List<DpprAccountingSoftware> getAccSoftware = LocalDB.GetAccountingSoftwares();
            DpprAccountingSoftware accSoftware = getAccSoftware[0];

            if (accSoftware.software_name == "SQLAccounting")
            {
                logger.message = "[SQL Accounting] customer sync is set to run now";
                logger.Broadcast();
                new JobCustSync().Execute();
                btn_run_custsync.Enabled = false;
            }
            else if (accSoftware.software_name == "QNE")
            {
                //logger.message = "This button is not enabled for QNE customer sync at the moment. Customer sync will run at set interval";
                logger.message = "[QNE] customer sync is set to run now";
                logger.Broadcast();
                new JobCustomerSync().Execute();
            }
            else if(accSoftware.software_name == "APS")
            {
                //logger.message = "This button is not enabled for [APS] customer sync at the moment. Customer sync will run at set interval";
                logger.message = "[APS] customer sync is set to run now";
                logger.Broadcast();
                new JobAPSCustomerSync().Execute();
            }
            else if (accSoftware.software_name == "AutoCount")
            {
                logger.message = "[AUTOCOUNT] customer sync is set to run now";
                logger.Broadcast();
                new JobATCCustomerSync().Execute();
            }
            else if (accSoftware.software_name == "Sage UBS")
            {
                logger.message = "[Sage UBS] customer sync is set to run now";
                logger.Broadcast();
            }

        }

        private void btn_run_postpaymentsync_Click(object sender, EventArgs e)
        {
            GlobalLogger logger = new GlobalLogger();
            List<DpprAccountingSoftware> getAccSoftware = LocalDB.GetAccountingSoftwares();
            DpprAccountingSoftware accSoftware = getAccSoftware[0];

            if (accSoftware.software_name == "SQLAccounting")
            {
                logger.message = "[SQL Accounting] Transfer Payment is set to run now";
                logger.Broadcast();
                new JobPaymentTransfer().Execute();
            }
            else if (accSoftware.software_name == "QNE")
            {
                logger.message = "This button is not enabled for QNE";// transfer SO sync at the moment. Transfer SO sync will run at set interval";
                //logger.message = "[QNE] Transfer SO sync is set to run now";
                logger.Broadcast();
            }
            else
            {
                logger.message = "This button is not enabled for [APS]";// transfer SO sync at the moment. Transfer SO sync will run at set interval";
                //logger.message = "[APS] Transfer SO is set to run now";
                logger.Broadcast();
            }
        }

        private void btn_run_transferso_Click(object sender, EventArgs e)
        {
            GlobalLogger logger = new GlobalLogger();
            List<DpprAccountingSoftware> getAccSoftware = LocalDB.GetAccountingSoftwares();
            DpprAccountingSoftware accSoftware = getAccSoftware[0];

            if (accSoftware.software_name == "SQLAccounting")
            {
                logger.message = "[SQL Accounting] Transfer SO is set to run now";
                logger.Broadcast();
                new JobSoTransfer().Execute();
                //throw new Exception("crash on purpose");
            }
            else if (accSoftware.software_name == "QNE")
            {
                //logger.message = "This button is not enabled for QNE transfer SO sync at the moment. Transfer SO sync will run at set interval";
                logger.message = "[QNE] Transfer SO sync is set to run now";
                logger.Broadcast();
                new JobPostSalesOrders().Execute();
            }
            else if (accSoftware.software_name == "APS")
            {
                logger.message = "[APS] Transfer SO is set to run now";
                logger.Broadcast();
                new JobAPSTransferSO().Execute();
                //new JobAPSTransferSOTrial().Execute();
            }
            else if (accSoftware.software_name == "AutoCount")
            {
                logger.message = "[AUTOCOUNT] Transfer SO is set to run now";
                logger.Broadcast();
                if(cb_sdk_atc.Checked)
                {
                    if (cb_atc_v2.Checked)
                    {
                        logger.Broadcast("Transfer SO SDK v2.0");
                        new JobATCTransferSOSDKv2().Execute();
                    }
                    else
                    {
                        logger.Broadcast("Transfer SO SDK v1.9");
                        new JobATCTransferSOSDK().Execute();
                    }
                }
                else
                {
                    new JobATCTransferSO().Execute();
                }
            }
            else if (accSoftware.software_name == "Sage UBS")
            {
                logger.message = "[Sage UBS] Transfer SO is set to run now";
                logger.Broadcast();
            }
            else
            {
                logger.message = "Not available";
                logger.Broadcast();
            }
        }
        private void btn_run_invsync_Click(object sender, EventArgs e)
        {
            GlobalLogger logger = new GlobalLogger();
            List<DpprAccountingSoftware> getAccSoftware = LocalDB.GetAccountingSoftwares();
            DpprAccountingSoftware accSoftware = getAccSoftware[0];

            if (accSoftware.software_name == "SQLAccounting")
            {
                logger.message = "[SQL Accounting] invoice sync is set to run now";
                logger.Broadcast();
                new JobInvoiceSync().Execute();
            }
            else if (accSoftware.software_name == "QNE")
            {
                logger.message = "[QNE] invoice sync is set to run now";
                logger.Broadcast();
                new JobInvoiceQneSync().Execute();
            }
            else if (accSoftware.software_name == "APS")
            {
                logger.message = "[APS] invoice sync is set to run now";
                logger.Broadcast();
                new JobAPSInvoiceSync().Execute();
            }
            else if (accSoftware.software_name == "AutoCount")
            {
                logger.message = "[AUTOCOUNT] invoice sync is set to run now";
                logger.Broadcast();
                new JobATCInvoiceSync().Execute();
            }
            else if(accSoftware.software_name == "Sage UBS")
            {
                logger.message = "[SAGE UBS] invoice sync is set to run now";
                logger.Broadcast();
                //new JobATCInvoiceSync().Execute(); //later change to ubs job
            }
            else 
            {
                logger.message = "This job is disable from this play button. Kindly contact EasySales team.";
                logger.Broadcast();
            }
        }

        private void btn_run_invdtlsync_Click(object sender, EventArgs e)
        {
            GlobalLogger logger = new GlobalLogger();
            List<DpprAccountingSoftware> getAccSoftware = LocalDB.GetAccountingSoftwares();
            DpprAccountingSoftware accSoftware = getAccSoftware[0];

            if (accSoftware.software_name == "SQLAccounting")
            {
                logger.message = "[SQL Accounting] invoice details sync is set to run now";
                logger.Broadcast();
                new JobInvoiceDetailsSync().Execute();
            }
            else if (accSoftware.software_name == "QNE")
            {
                logger.message = "[QNE] invoice details sync is set to run now";
                logger.Broadcast();
                new JobInvoiceDetailsQneSync().Execute();
            }
            else if (accSoftware.software_name == "APS")
            {
                //logger.message = "This button is not enabled for [APS] invoice detail sync at the moment. Invoice detail sync will run at set interval";
                logger.message = "[APS] invoice details sync is set to run now";
                logger.Broadcast();
                new JobAPSInvoiceDetailSync().Execute();
            }
            else if (accSoftware.software_name == "AutoCount")
            {
                logger.message = "[AUTOCOUNT] invoice details sync is set to run now";
                logger.Broadcast();
                new JobATCInvoiceDetailSync().Execute();
            }
            else if (accSoftware.software_name == "Sage UBS")
            {
                logger.message = "[SAGE UBS] invoice details sync is set to run now";
                logger.Broadcast();
                //new JobATCInvoiceSync().Execute(); //later change to ubs job
            }
            else
            {
                logger.message = "This job is disable from this play button. Kindly contact EasySales team.";
                logger.Broadcast();
            }
        }

        private void btn_run_custagentsync_Click(object sender, EventArgs e)
        {
            GlobalLogger logger = new GlobalLogger();
            List<DpprAccountingSoftware> getAccSoftware = LocalDB.GetAccountingSoftwares();
            DpprAccountingSoftware accSoftware = getAccSoftware[0];

            if (accSoftware.software_name == "SQLAccounting")
            {
                logger.message = "[SQL Accounting] customer-agent sync is set to run now";
                logger.Broadcast();
                new JobSalesPersonSync().Execute();
            }
            else if (accSoftware.software_name == "QNE")
            {
                //logger.message = "This button is not enabled for [QNE] customer-agent sync at the moment. Customer-agent sync will run at set interval";
                logger.message = "[QNE] customer-agent sync is set to run now";
                logger.Broadcast();
                new JobAgentSync().Execute();
                new JobCustomerAgentSync().Execute();
            }
            else if (accSoftware.software_name == "APS")
            {
                logger.message = "[APS] customer-agent sync is set to run now";
                logger.Broadcast();

                logger.message = "[APS] ref sync is set to run now";
                logger.Broadcast();

                new JobAPSSalespersonSync().Execute();
                new JobAPSSalespersonCustomerSync().Execute();
                new JobAPSRefSync().Execute();
            }
            else if (accSoftware.software_name == "AutoCount")
            {
                logger.message = "[AUTOCOUNT] salesperson sync is set to run now";
                logger.Broadcast();

                new JobATCSalespersonSync().Execute();
                new JobATCSalespersonCustomerSync().Execute();
            }
            else if (accSoftware.software_name == "Sage UBS")
            {
                logger.message = "[SAGE UBS] salesperson sync is set to run now";
                logger.Broadcast();
                //new JobATCInvoiceSync().Execute(); //later change to ubs job
            }
            else
            {
                logger.message = "This job is disable from this play button. Kindly contact EasySales team.";
                logger.Broadcast();
            }
        }
        
        private void btn_run_branchsync_Click(object sender, EventArgs e)
        {
            GlobalLogger logger = new GlobalLogger();
            List<DpprAccountingSoftware> getAccSoftware = LocalDB.GetAccountingSoftwares();
            DpprAccountingSoftware accSoftware = getAccSoftware[0];

            List<DpprMySQLconfig> mysql_list = LocalDB.GetRemoteDatabaseConfig().Distinct().ToList();
            DpprMySQLconfig mysql_config = mysql_list[0];
            string compName = mysql_config.config_database;

            if (accSoftware.software_name == "SQLAccounting")
            {
                if(compName == "easysale_uvjoy")
                {
                    logger.message = "Collection sync is set to run now";
                    logger.Broadcast();
                    new JobUJ().Execute();
                }
                else
                {
                    logger.message = "[SQL Accounting] branch sync is set to run now";
                    logger.Broadcast();
                    new JobBranchSync().Execute();
                }
            }
            else if (accSoftware.software_name == "QNE")
            {
                //logger.message = "QNE stock sync is set to run now";
                logger.message = "This button is not enabled for [QNE] branch sync at the moment.";
                logger.Broadcast();
            }
            else if (accSoftware.software_name == "APS")
            {
                logger.message = "This button is not enabled for [APS] branch sync at the moment. Branch sync will run at set interval";
                logger.Broadcast();
            }
            else if (accSoftware.software_name == "AutoCount")
            {
                logger.message = "[AUTOCOUNT] branch sync is set to run now";
                logger.Broadcast();
                new JobATCBranchSync().Execute();
            }
            else if (accSoftware.software_name == "Sage UBS")
            {
                logger.message = "[SAGE UBS] branch sync is set to run now";
                logger.Broadcast();
                //new JobATCInvoiceSync().Execute(); //later change to ubs job
            }
            else
            {
                logger.message = "This job is disable from this play button. Kindly contact EasySales team.";
                logger.Broadcast();
            }
        }

        private void btn_run_stockcatsync_Click(object sender, EventArgs e)
        {
            GlobalLogger logger = new GlobalLogger();
            List<DpprAccountingSoftware> getAccSoftware = LocalDB.GetAccountingSoftwares();
            DpprAccountingSoftware accSoftware = getAccSoftware[0];

            if (accSoftware.software_name == "SQLAccounting")
            {
                logger.message = "[SQL Accounting] stock category sync is set to run now";
                logger.Broadcast();
                new JobProductCategorySync().Execute();
            }
            else if (accSoftware.software_name == "QNE")
            {
                //logger.message = "This button is not enabled for QNE stock category sync at the moment. Stock category sync will run at set interval";
                logger.message = "[QNE] stock category sync is set to run now";
                logger.Broadcast();
                new JobStockCategoriesSync().Execute();
            }
            else if (accSoftware.software_name == "APS")
            {
                logger.message = "[APS] stock category sync is set to run now";
                logger.Broadcast();
                new JobAPSStockCategorySync().Execute();
            }
            else if (accSoftware.software_name == "AutoCount")
            {
                logger.message = "[AUTOCOUNT] stock category sync is set to run now";
                logger.Broadcast();
                new JobATCStockCategorySync().Execute();
            }
            else if (accSoftware.software_name == "Sage UBS")
            {
                logger.message = "[SAGE UBS] stock category sync is set to run now";
                logger.Broadcast();
                //new JobATCInvoiceSync().Execute(); //later change to ubs job
            }
            else
            {
                logger.message = "This job is disable from this play button. Kindly contact EasySales team.";
                logger.Broadcast();
            }
        }

        private void btn_run_uompricesync_Click(object sender, EventArgs e)
        {
            GlobalLogger logger = new GlobalLogger();
            List<DpprAccountingSoftware> getAccSoftware = LocalDB.GetAccountingSoftwares();
            DpprAccountingSoftware accSoftware = getAccSoftware[0];

            if (accSoftware.software_name == "SQLAccounting")
            {
                logger.message = "[SQL Accounting] stock UOM price sync is set to run now";
                logger.Broadcast();
                new JobProductPriceSync().Execute();
            }
            else if (accSoftware.software_name == "QNE")
            {
                //logger.message = "This button is not enabled for QNE stock UOM price sync at the moment. Stock UOM price sync will run at set interval";
                logger.message = "[QNE] stock UOM price sync is set to run now";
                logger.Broadcast();
                new JobStockUomPriceSync().Execute();
            }
            else if (accSoftware.software_name == "APS")
            {
                logger.message = "[APS] stock UOM price sync is set to run now";
                logger.Broadcast();
                new JobAPSStockUomPriceSync().Execute();
            }
            else if (accSoftware.software_name == "AutoCount")
            {
                logger.message = "[AUTOCOUNT] stock UOM price sync is set to run now";
                logger.Broadcast();
                new JobATCStockUomPriceSync().Execute();
            }
            else if (accSoftware.software_name == "Sage UBS")
            {
                logger.message = "[SAGE UBS] stock UOM price sync is set to run now";
                logger.Broadcast();
                //new JobATCInvoiceSync().Execute(); //later change to ubs job
            }
            else
            {
                logger.message = "This job is disable from this play button. Kindly contact EasySales team.";
                logger.Broadcast();
            }
        }

        private void btn_run_specialpricesync_Click(object sender, EventArgs e)
        {
            GlobalLogger logger = new GlobalLogger();
            List<DpprAccountingSoftware> getAccSoftware = LocalDB.GetAccountingSoftwares();
            DpprAccountingSoftware accSoftware = getAccSoftware[0];

            if (accSoftware.software_name == "SQLAccounting")
            {
                logger.message = "[SQL Accounting] special price sync is set to run now";
                logger.Broadcast();
                new JobProductSpecialPriceSync().Execute();
            }
            else if (accSoftware.software_name == "QNE")
            {
                //logger.message = "QNE stock sync is set to run now";
                logger.message = "This button is not enabled for QNE";// stock special price sync at the moment. Stock special price sync will run at set interval";
                logger.Broadcast();
            }
            else if(accSoftware.software_name == "APS")
            {
                logger.message = "[APS] stock special price sync is set to run now";
                logger.Broadcast();
                new JobAPSStockSpecialPriceSync().Execute();
            }
            else if (accSoftware.software_name == "AutoCount")
            {
                logger.message = "[AUTOCOUNT] stock special price sync is set to run now";
                logger.Broadcast();
                new JobATCStockSpecialPriceSync().Execute();
            }
            else if (accSoftware.software_name == "Sage UBS")
            {
                logger.message = "[SAGE UBS] stock special price sync is set to run now";
                logger.Broadcast();
                //new JobATCInvoiceSync().Execute(); //later change to ubs job
            }
            else
            {
                logger.message = "This job is disable from this play button. Kindly contact EasySales team.";
                logger.Broadcast();
            }
        }

        private void btn_run_stockgroupsync_Click(object sender, EventArgs e)
        {
            GlobalLogger logger = new GlobalLogger();
            List<DpprAccountingSoftware> getAccSoftware = LocalDB.GetAccountingSoftwares();
            DpprAccountingSoftware accSoftware = getAccSoftware[0];

            if (accSoftware.software_name == "SQLAccounting")
            {
                logger.message = "[SQL Accounting] stock group sync is set to run now";
                logger.Broadcast();
                new JobProductGroupsSync().Execute();
            }
            else if (accSoftware.software_name == "QNE")
            {
                //logger.message = "QNE stock sync is set to run now";
                logger.message = "This button is not enabled for QNE"; //stock group sync at the moment. Stock group sync will run at set interval";
                logger.Broadcast();
            }
            else
            {
                logger.message = "This button is not enabled for this accounting software at the moment. Please contact EasySales Team.";
                logger.Broadcast();
            }
        }

        private void btn_run_costpricesync_Click(object sender, EventArgs e)
        {
            GlobalLogger logger = new GlobalLogger();
            List<DpprAccountingSoftware> getAccSoftware = LocalDB.GetAccountingSoftwares();
            DpprAccountingSoftware accSoftware = getAccSoftware[0];

            if (accSoftware.software_name == "SQLAccounting")
            {
                logger.message = "[SQL Accounting] cost price sync is set to run now";
                logger.Broadcast();
                new JobCostPriceSync().Execute();
            }
            else if (accSoftware.software_name == "QNE")
            {
                //logger.message = "QNE stock sync is set to run now";
                logger.message = "This button is not enabled for QNE";// cost price sync at the moment. Cost price sync will run at set interval";
                logger.Broadcast();
            }
            else 
            {
                logger.message = "This button is not enabled for this accounting software at the moment.";
                logger.Broadcast();
            }
        }

        private void btn_run_ageingkosync_Click(object sender, EventArgs e)
        {
            GlobalLogger logger = new GlobalLogger();
            List<DpprAccountingSoftware> getAccSoftware = LocalDB.GetAccountingSoftwares();
            DpprAccountingSoftware accSoftware = getAccSoftware[0];

            if (accSoftware.software_name == "SQLAccounting")
            {
                logger.message = "[SQL Accounting] ageing KO sync is set to run now";
                logger.Broadcast();
                new JobKnockOffSync().Execute();
            }
            else if (accSoftware.software_name == "QNE")
            {
                //logger.message = "QNE stock sync is set to run now";
                logger.message = "This button is not enabled for QNE";// ageing KO sync at the moment. Ageing KO sync will run at set interval";
                logger.Broadcast();
            }
            else if (accSoftware.software_name == "APS")
            {
                logger.message = "APS Aging KO CN & Receipt is set to run now";
                logger.Broadcast();
                new JobAPSCreditNoteKOSync().Execute();
                new JobAPSReceiptKOSync().Execute();
            }
            else if (accSoftware.software_name == "AutoCount")
            {
                logger.message = "[AutoCount] ageing KO sync is set to run now";
                logger.Broadcast();
                new JobATCINVKOSync().Execute();                
                new JobATCCNKOSync().Execute();
            }
            else if (accSoftware.software_name == "Sage UBS")
            {
                logger.message = "[SAGE UBS] ageing KO sync is set to run now";
                logger.Broadcast();
                //new JobATCInvoiceSync().Execute(); //later change to ubs job
            }
            else
            {
                logger.message = "This job is disable from this play button. Kindly contact EasySales team.";
                logger.Broadcast();
            }
        }

        private void btn_run_whqtysync_Click(object sender, EventArgs e)
        {
            GlobalLogger logger = new GlobalLogger();
            List<DpprAccountingSoftware> getAccSoftware = LocalDB.GetAccountingSoftwares();
            DpprAccountingSoftware accSoftware = getAccSoftware[0];

            if (accSoftware.software_name == "SQLAccounting")
            {
                logger.message = "[SQL Accounting] warehouse sync is set to run now";
                logger.Broadcast();
                new JobWhStockSync().Execute();
            }
            else if (accSoftware.software_name == "QNE")
            {
                logger.message = "This button is not enabled for QNE";//warehouse quantity sync at the moment. Warehouse quantity sync will run at set interval";
                logger.Broadcast();
            }
            else if (accSoftware.software_name == "APS")
            {
                logger.message = "[APS] warehouse quantity sync is set to run now";
                logger.Broadcast();
                //new JobAPSWarehouseSync().Execute();
                new JobAPSWarehouseQtySync().Execute();
            }
            else if (accSoftware.software_name == "AutoCount")
            {
                logger.message = "[AUTOCOUNT] warehouse quantity sync is set to run now";
                logger.Broadcast();

                //if(cb_sdk_atc.Checked)
                //{
                    //new JobATCWarehouseSync().Execute();
                    //new JobATCWHReadyStock().Execute();
                //}
                //else
                //{
                    new JobATCWarehouseSync().Execute();
                    new JobATCWarehouseQtySync().Execute();
                //}
                
            }
            else if (accSoftware.software_name == "Sage UBS")
            {
                logger.message = "[SAGE UBS] warehouse quantity sync is set to run now";
                logger.Broadcast();
                //new JobATCInvoiceSync().Execute(); //later change to ubs job
            }
            else
            {
                logger.message = "This job is disable from this play button. Kindly contact EasySales team.";
                logger.Broadcast();
            }
        }

        private void btn_run_rcptsync_Click(object sender, EventArgs e)
        {
            GlobalLogger logger = new GlobalLogger();
            List<DpprAccountingSoftware> getAccSoftware = LocalDB.GetAccountingSoftwares();
            DpprAccountingSoftware accSoftware = getAccSoftware[0];

            if (accSoftware.software_name == "SQLAccounting")
            {
                logger.message = "[SQL Accounting] receipt sync is set to run now";
                logger.Broadcast();
                new JobRcptSyncSQLAcc().Execute();
            }
            else if (accSoftware.software_name == "QNE")
            {
                //logger.message = "This button is not enabled for QNE receipt sync at the moment. Receipt sync will run at set interval";
                logger.message = "[QNE] receipt sync is set to run now";
                logger.Broadcast();
                new JobReceiptSync().Execute();
            }
            else if (accSoftware.software_name == "APS")
            {
                logger.message = "[APS] receipt sync is set to run now";
                logger.Broadcast();
                new JobAPSReceiptSync().Execute();
            }
            else if (accSoftware.software_name == "AutoCount")
            {
                logger.message = "[AUTOCOUNT] receipt sync is set to run now";
                logger.Broadcast();
                new JobATCReceiptSync().Execute();
            }
            else if (accSoftware.software_name == "Sage UBS")
            {
                logger.message = "[SAGE UBS] receipt sync is set to run now";
                logger.Broadcast();
                //new JobATCInvoiceSync().Execute(); //later change to ubs job
            }
            else
            {
                logger.message = "This job is disable from this play button. Kindly contact EasySales team.";
                logger.Broadcast();
            }
        }

        private void btn_run_cnsync_Click(object sender, EventArgs e)
        {
            GlobalLogger logger = new GlobalLogger();
            List<DpprAccountingSoftware> getAccSoftware = LocalDB.GetAccountingSoftwares();
            DpprAccountingSoftware accSoftware = getAccSoftware[0];

            if (accSoftware.software_name == "SQLAccounting")
            {
                logger.message = "[SQL Accounting] credit note sync is set to run now";
                logger.Broadcast();
                new JobCNSync().Execute();
            }
            else if (accSoftware.software_name == "QNE")
            {
                logger.message = "[QNE] credit note sync is set to run now";
                logger.Broadcast();
                new JobCreditNoteSync().Execute();
            }
            else if (accSoftware.software_name == "APS")
            {
                logger.message = "[APS] credit note sync is set to run now";
                logger.Broadcast();
                new JobAPSCreditNoteSync().Execute();
            }
            else if (accSoftware.software_name == "AutoCount")
            {
                logger.message = "[AUTOCOUNT] credit note sync is set to run now";
                logger.Broadcast();
                new JobATCCreditNoteSync().Execute();
            }
            else if (accSoftware.software_name == "Sage UBS")
            {
                logger.message = "[SAGE UBS] credit note sync is set to run now";
                logger.Broadcast();
                //new JobATCInvoiceSync().Execute(); //later change to ubs job
            }
            else
            {
                logger.message = "This job is disable from this play button. Kindly contact EasySales team.";
                logger.Broadcast();
            }
        }

        private void btn_run_cndtlsync_Click(object sender, EventArgs e)
        {
            GlobalLogger logger = new GlobalLogger();
            List<DpprAccountingSoftware> getAccSoftware = LocalDB.GetAccountingSoftwares();
            DpprAccountingSoftware accSoftware = getAccSoftware[0];

            if (accSoftware.software_name == "SQLAccounting")
            {
                logger.message = "[SQL Accounting] credit note details sync is set to run now";
                logger.Broadcast();
                new JobCNDtlSync().Execute();
            }
            else if (accSoftware.software_name == "QNE")
            {
                logger.message = "This button is not enabled for QNE";  //credit note details sync at the moment. Credit note details sync will run at set interval";
                //logger.message = "[QNE] credit note details sync is set to run now";
                logger.Broadcast();
            }
            else if (accSoftware.software_name == "APS")
            {
                logger.message = "[APS] credit note details sync is set to run now";
                logger.Broadcast();
                new JobAPSCreditNoteDetailSync().Execute();
            }
            else if (accSoftware.software_name == "AutoCount")
            {
                logger.message = "[AUTOCOUNT] credit note details sync is set to run now";
                logger.Broadcast();
                new JobATCCreditNoteDetailSync().Execute();
            }
            else if (accSoftware.software_name == "Sage UBS")
            {
                logger.message = "[SAGE UBS] credit note details sync is set to run now";
                logger.Broadcast();
                //new JobATCInvoiceSync().Execute(); //later change to ubs job
            }
            else
            {
                logger.message = "This job is disable from this play button. Kindly contact EasySales team.";
                logger.Broadcast();
            }
        }

        private void btn_run_dnsync_Click(object sender, EventArgs e)
        {
            GlobalLogger logger = new GlobalLogger();
            List<DpprAccountingSoftware> getAccSoftware = LocalDB.GetAccountingSoftwares();
            DpprAccountingSoftware accSoftware = getAccSoftware[0];

            if (accSoftware.software_name == "SQLAccounting")
            {
                logger.message = "[SQL Accounting] debit note sync is set to run now";
                logger.Broadcast();
                new JobDNSync().Execute();
                new JobDNDtlSync().Execute();
            }
            else if (accSoftware.software_name == "QNE")
            {
                logger.message = "[QNE] debit note sync is set to run now";
                logger.Broadcast();
                new JobDebitNoteSync().Execute();
            }
            else if (accSoftware.software_name == "APS")
            {
                logger.message = "[APS] debit note sync is set to run now";
                logger.Broadcast();
                new JobAPSDebitNoteSync().Execute();
            }
            else if (accSoftware.software_name == "AutoCount")
            {
                logger.message = "[APS] debit note sync is set to run now";
                logger.Broadcast();
                new JobATCDebitNoteSync().Execute();
            }
            else if (accSoftware.software_name == "Sage UBS")
            {
                logger.message = "[SAGE UBS] debit note sync is set to run now";
                logger.Broadcast();
                //new JobATCInvoiceSync().Execute(); //later change to ubs job
            }
            else
            {
                logger.message = "This job is disable from this play button. Kindly contact EasySales team.";
                logger.Broadcast();
            }
        }

        private void btn_run_outsosync_Click(object sender, EventArgs e)
        {
            GlobalLogger logger = new GlobalLogger();
            List<DpprAccountingSoftware> getAccSoftware = LocalDB.GetAccountingSoftwares();
            DpprAccountingSoftware accSoftware = getAccSoftware[0];

            if (accSoftware.software_name == "SQLAccounting")
            {
                logger.message = "[SQL Accounting] outstanding SO sync is set to run now";
                logger.Broadcast();
                new JobOutstandingSOSync().Execute();
            }
            else if (accSoftware.software_name == "QNE")
            {
                logger.message = "This button is not enabled for QNE";// outstanding SO sync at the moment. Outstanding SO sync will run at set interval";
                logger.Broadcast();
            }
            else if (accSoftware.software_name == "APS")
            {
                logger.message = "This button is not enabled for [APS] outstanding SO sync at the moment.";
                logger.Broadcast();
            }
            else if (accSoftware.software_name == "AutoCount")
            {
                new JobATCOutstandingSO().Execute();
                logger.message = "[ATC] outstanding SO sync is set to run now";
                logger.Broadcast();
            }
            else if (accSoftware.software_name == "Sage UBS")
            {
                logger.message = "[SAGE UBS] outstanding SO sync is set to run now";
                logger.Broadcast();
                //new JobATCInvoiceSync().Execute(); //later change to ubs job
            }
            else
            {
                logger.message = "This job is disable from this play button. Kindly contact EasySales team.";
                logger.Broadcast();
            }
        }

        private void btn_run_transfersalesinv_Click(object sender, EventArgs e)
        {
            GlobalLogger logger = new GlobalLogger();
            List<DpprAccountingSoftware> getAccSoftware = LocalDB.GetAccountingSoftwares();
            DpprAccountingSoftware accSoftware = getAccSoftware[0];

            if (accSoftware.software_name == "SQLAccounting")
            {
                logger.message = "[SQL Accounting] Transfer Sales Invoices sync is set to run now";
                logger.Broadcast();
                new JobINVTransfer().Execute();
            }
            else if (accSoftware.software_name == "QNE")
            {
                logger.message = "[QNE] Transfer Sales Invoices sync is set to run now";
                logger.Broadcast();
                new JobPostSalesInvoice().Execute();
            }
            else if (accSoftware.software_name == "APS")
            {
                logger.message = "This button is not enabled for [AutoCount]";
                logger.Broadcast();
            }
            else if (accSoftware.software_name == "AutoCount")
            {
                if (cb_sdk_atc.Checked)
                {
                    new JobATCTransferINVSDK().Execute();
                    logger.message = "[ATC] Transfer Sales Invoices (SDK) sync is set to run now";
                    logger.Broadcast();
                }
                else
                {
                    new JobATCTransferINV().Execute();
                    logger.message = "[ATC] Transfer Sales Invoices sync is set to run now";
                    logger.Broadcast();
                }
                
                
            }
            else if (accSoftware.software_name == "Sage UBS")
            {
                logger.message = "[SAGE UBS] Transfer Sales Invoices sync is set to run now";
                logger.Broadcast();
                //new JobATCInvoiceSync().Execute(); //later change to ubs job
            }
            else
            {
                logger.message = "This job is disable from this play button. Kindly contact EasySales team.";
                logger.Broadcast();
            }
        }

        private void btn_run_transfersalescns_Click(object sender, EventArgs e)
        {
            GlobalLogger logger = new GlobalLogger();
            List<DpprAccountingSoftware> getAccSoftware = LocalDB.GetAccountingSoftwares();
            DpprAccountingSoftware accSoftware = getAccSoftware[0];

            if (accSoftware.software_name == "SQLAccounting")
            {
                logger.message = "[SQLAccounting] Transfer Sales CNs sync is set to run now";
                logger.Broadcast();
                new JobCNTransfer().Execute();
            }
            else if (accSoftware.software_name == "QNE")
            {
                logger.message = "[QNE] Transfer Sales CNs sync is set to run now";
                logger.Broadcast();
                new JobPostSalesCNs().Execute();
            }
            else if (accSoftware.software_name == "APS")
            {
                logger.message = "[APS] Transfer Sales CNs sync is set to run now";
                logger.Broadcast();
                new JobAPSTransferCN().Execute();
            }
            else if (accSoftware.software_name == "AutoCount")
            {
                if(cb_sdk_atc.Checked)
                {
                    logger.message = "[AutoCount] Transfer Sales CNs sync is set to run now";
                    logger.Broadcast();
                    new JobATCTransferCNSDK().Execute();
                }
            }
            else if (accSoftware.software_name == "Sage UBS")
            {
                logger.message = "[SAGE UBS] Transfer Sales CNs sync is set to run now";
                logger.Broadcast();
                //new JobATCInvoiceSync().Execute(); //later change to ubs job
            }
            else
            {
                logger.message = "This job is disable from this play button. Kindly contact EasySales team.";
                logger.Broadcast();
            }
        }

        private void btn_run_imagesync_Click(object sender, EventArgs e)
        {
            GlobalLogger logger = new GlobalLogger();
            List<DpprAccountingSoftware> getAccSoftware = LocalDB.GetAccountingSoftwares();
            DpprAccountingSoftware accSoftware = getAccSoftware[0];

            if (accSoftware.software_name == "SQLAccounting")
            {
                logger.message = "[SQL Accounting] image sync is set to run now";
                logger.Broadcast();
                new JobReadImageSync().Execute();
            }
            else if (accSoftware.software_name == "QNE")
            {
                logger.message = "[QNE] image sync is set to run now";
                logger.Broadcast();
                new TranferImgGM().Execute();
            }
            else if (accSoftware.software_name == "APS")
            {
                logger.message = "[APS] image sync is set to run now";
                logger.Broadcast();
                new JobAPSImageSync().Execute();
            }
            else if (accSoftware.software_name == "AutoCount")
            {
                logger.message = "[AutoCount] This button is not enabled for this job";
                logger.Broadcast();
            }
            else if (accSoftware.software_name == "Sage UBS")
            {
                logger.message = "[SAGE UBS] invoice sync is set to run now";
                logger.Broadcast();
                //new JobATCInvoiceSync().Execute(); //later change to ubs job
            }
            else
            {
                logger.message = "This job is disable from this play button. Kindly contact EasySales team.";
                logger.Broadcast();
            }
        }

        private void btn_run_itemtmpsync_Click(object sender, EventArgs e)
        {
            GlobalLogger logger = new GlobalLogger();
            List<DpprAccountingSoftware> getAccSoftware = LocalDB.GetAccountingSoftwares();
            DpprAccountingSoftware accSoftware = getAccSoftware[0];

            if (accSoftware.software_name == "SQLAccounting")
            {
                logger.message = "[SQL Accounting] item template sync is set to run now";
                logger.Broadcast();
                new JobItemTemplateSync().Execute();
            }
            else if (accSoftware.software_name == "QNE")
            {
                //logger.message = "QNE stock sync is set to run now";
                logger.message = "This button is not enabled for QNE";// item template sync at the moment. Item template sync will run at set interval";
                logger.Broadcast();
            }
            else if (accSoftware.software_name == "APS")
            {
                logger.message = "This button is not enabled for [APS] item template sync at the moment.";
                logger.Broadcast();
            }
            else if (accSoftware.software_name == "AutoCount")
            {
                logger.message = "[AUTOCOUNT] item template sync is set to run now";
                logger.Broadcast();
                new JobATCItemTemplateSync().Execute();
            }
            else if (accSoftware.software_name == "Sage UBS")
            {
                logger.message = "[SAGE UBS] item template sync is set to run now";
                logger.Broadcast();
                //new JobATCInvoiceSync().Execute(); //later change to ubs job
            }
            else
            {
                logger.message = "This job is disable from this play button. Kindly contact EasySales team.";
                logger.Broadcast();
            }
        }

        private void btn_run_itemtmpdtlsync_Click(object sender, EventArgs e)
        {
            GlobalLogger logger = new GlobalLogger();
            List<DpprAccountingSoftware> getAccSoftware = LocalDB.GetAccountingSoftwares();
            DpprAccountingSoftware accSoftware = getAccSoftware[0];

            if (accSoftware.software_name == "SQLAccounting")
            {
                logger.message = "[SQL Accounting] item template details sync is set to run now";
                logger.Broadcast();
                new JobItemTemplateDtlSync().Execute();
            }
            else if (accSoftware.software_name == "QNE")
            {
                logger.message = "This button is not enabled for QNE";// item template details sync at the moment. Item template details sync will run at set interval";
                logger.Broadcast();
            } 
            else if (accSoftware.software_name == "APS")
            {
                logger.message = "This button is not enabled for [APS] item template details sync at the moment.";
                logger.Broadcast();
            }
            else if (accSoftware.software_name == "AutoCount")
            {
                logger.message = "[AUTOCOUNT] item template details sync is set to run now";
                logger.Broadcast();
                new JobATCItemTemplateDtlSync().Execute();
            }
            else if (accSoftware.software_name == "Sage UBS")
            {
                logger.message = "[SAGE UBS] item template details sync is set to run now";
                logger.Broadcast();
                //new JobATCInvoiceSync().Execute(); //later change to ubs job
            }
            else
            {
                logger.message = "This job is disable from this play button. Kindly contact EasySales team.";
                logger.Broadcast();
            }
        }

        private void btn_run_stock_transfer_Click(object sender, EventArgs e)
        {
            GlobalLogger logger = new GlobalLogger();
            List<DpprAccountingSoftware> getAccSoftware = LocalDB.GetAccountingSoftwares();
            DpprAccountingSoftware accSoftware = getAccSoftware[0];

            if (accSoftware.software_name == "SQLAccounting")
            {
                logger.message = "[SQLAccounting] Stock Transfer will run now";
                logger.Broadcast();
                new JobStockTransfer().Execute();
            }
            else if (accSoftware.software_name == "QNE")
            {
                logger.message = "This button is not enabled for QNE";
                logger.Broadcast();
            }
            else if (accSoftware.software_name == "APS")
            {
                logger.message = "This button is not enabled for [APS] update cash sales sync at the moment.";
                logger.Broadcast();
            }
            else if (accSoftware.software_name == "AutoCount")
            {
                logger.message = "[AutoCount] Transfer Stock will run now";
                logger.Broadcast();

                if(cb_sdk_atc.Checked)
                {
                    new JobATCTransferStockSDK().Execute();
                }
                else
                {
                    logger.Broadcast("Current AutoCount didn't have SDK license. If yes, kindly tick the SDK checkbox.");
                }
            }
            else if (accSoftware.software_name == "Sage UBS")
            {
                logger.message = "[SAGE UBS] update cash sales sync is set to run now";
                logger.Broadcast();
                //new JobATCInvoiceSync().Execute(); //later change to ubs job
            }
            else
            {
                logger.message = "This job is disable from this play button. Kindly contact EasySales team.";
                logger.Broadcast();
            }


        }

        private void btn_run_dosync_Click(object sender, EventArgs e)
        {
            GlobalLogger logger = new GlobalLogger();
            List<DpprAccountingSoftware> getAccSoftware = LocalDB.GetAccountingSoftwares();
            DpprAccountingSoftware accSoftware = getAccSoftware[0];

            if (accSoftware.software_name == "SQLAccounting")
            {
                logger.message = "[SQLAccounting] delivery order sync is set to run now";
                logger.Broadcast();
                new JobDOSync().Execute();
            }
            else if (accSoftware.software_name == "QNE")
            {
                logger.message = "This button is not enabled for QNE";
                logger.Broadcast();
            }
            else if (accSoftware.software_name == "APS")
            {
                logger.message = "[APS] delivery order sync is set to run now";
                logger.Broadcast();
                new JobAPSDOSync().Execute();
                new JobAPSDODetailSync().Execute();
            }
            else if (accSoftware.software_name == "AutoCount")
            {
                logger.message = "[AutoCount] This button is not enabled for this job";
                logger.Broadcast();
            }
            else if (accSoftware.software_name == "Sage UBS")
            {
                logger.message = "[SAGE UBS] delivery order sync is set to run now";
                logger.Broadcast();
                //new JobATCInvoiceSync().Execute(); //later change to ubs job
            }
            else
            {
                logger.message = "This job is disable from this play button. Kindly contact EasySales team.";
                logger.Broadcast();
            }
        }

        private void btn_run_postpobasketsync_Click(object sender, EventArgs e)
        {
            GlobalLogger logger = new GlobalLogger();
            List<DpprAccountingSoftware> getAccSoftware = LocalDB.GetAccountingSoftwares();
            DpprAccountingSoftware accSoftware = getAccSoftware[0];

            if (accSoftware.software_name == "SQLAccounting")
            {
                logger.message = "This button is not enabled for [SQLAccounting]"; 
                logger.Broadcast();
                new JobUJ().Execute();
            }
            else if (accSoftware.software_name == "QNE")
            {
                logger.message = "This button is not enabled for QNE";
                logger.Broadcast();
            }
            else if (accSoftware.software_name == "APS")
            {
                logger.message = "[APS] transfer PO basket is set to run now";
                logger.Broadcast();
                new JobAPSTransferPOBasket().Execute();
            }
            else if (accSoftware.software_name == "AutoCount")
            {
                logger.message = "[AutoCount] This button is not enabled for this job";
                logger.Broadcast();
            }
            else if (accSoftware.software_name == "Sage UBS")
            {
                logger.message = "[SAGE UBS] transfer PO basket sync is set to run now";
                logger.Broadcast();
                //new JobATCInvoiceSync().Execute(); //later change to ubs job
            }
            else
            {
                logger.message = "This job is disable from this play button. Kindly contact EasySales team.";
                logger.Broadcast();
            }
        }

        private void btn_run_transferquo_Click(object sender, EventArgs e)
        {
            GlobalLogger logger = new GlobalLogger();
            List<DpprAccountingSoftware> getAccSoftware = LocalDB.GetAccountingSoftwares();
            DpprAccountingSoftware accSoftware = getAccSoftware[0];

            if (accSoftware.software_name == "SQLAccounting")
            {
                logger.message = "This button is not enabled for [SQLAccounting]"; 
                logger.Broadcast();
            }
            else if (accSoftware.software_name == "QNE")
            {
                logger.message = "This button is not enabled for QNE";
                logger.Broadcast();
            }
            else if (accSoftware.software_name == "APS")
            {
                logger.message = "[APS] transfer quotation is set to run now";
                logger.Broadcast();
                new JobAPSTransferQuotation().Execute();
            }
            else if (accSoftware.software_name == "AutoCount")
            {
                logger.message = "[AutoCount] This button is not enabled for this job";
                logger.Broadcast();
            }
            else if (accSoftware.software_name == "Sage UBS")
            {
                logger.message = "[SAGE UBS] transfer quotation sync is set to run now";
                logger.Broadcast();
                //new JobATCInvoiceSync().Execute(); //later change to ubs job
            }
            else
            {
                logger.message = "This job is disable from this play button. Kindly contact EasySales team.";
                logger.Broadcast();
            }
        }

        private void btn_run_cfsync_Click(object sender, EventArgs e)
        {
            GlobalLogger logger = new GlobalLogger();
            List<DpprAccountingSoftware> getAccSoftware = LocalDB.GetAccountingSoftwares();
            DpprAccountingSoftware accSoftware = getAccSoftware[0];

            if (accSoftware.software_name == "SQLAccounting")
            {
                logger.message = "Customer Refund and Customer Contra Local is set to run now";
                logger.Broadcast();
                new JobCFSync().Execute();
            }
            else if (accSoftware.software_name == "QNE")
            {
                logger.message = "This button is not enabled for QNE";
                logger.Broadcast();
            }
            else if (accSoftware.software_name == "APS")
            {
                logger.message = "This button is not enabled for [APS]";
                logger.Broadcast();
            }
            else if (accSoftware.software_name == "AutoCount")
            {
                logger.message = "This button is not enabled for [AutoCount]";
                logger.Broadcast();
            }
            else if (accSoftware.software_name == "Sage UBS")
            {
                logger.message = "[SAGE UBS] This button is not enabled for this job";
                logger.Broadcast();
                //new JobATCInvoiceSync().Execute(); //later change to ubs job
            }
            else
            {
                logger.message = "This job is disable from this play button. Kindly contact EasySales team.";
                logger.Broadcast();
            }
        }

        private void btn_run_sosync_Click(object sender, EventArgs e)
        {
            GlobalLogger logger = new GlobalLogger();
            List<DpprAccountingSoftware> getAccSoftware = LocalDB.GetAccountingSoftwares();
            DpprAccountingSoftware accSoftware = getAccSoftware[0];

            if (accSoftware.software_name == "SQLAccounting")
            {
                logger.message = "Sales order sync is set to run now";
                logger.Broadcast();
                new JobSOSync().Execute();
            }
            else if (accSoftware.software_name == "QNE")
            {
                logger.message = "This button is not enabled for QNE";
                logger.Broadcast();
            }
            else if (accSoftware.software_name == "APS")
            {
                logger.message = "This button is not enabled for [APS]";
                logger.Broadcast();
            }
            else if (accSoftware.software_name == "AutoCount")
            {
                logger.message = "This button is not enabled for [AutoCount]";
                logger.Broadcast();
            }
            else if (accSoftware.software_name == "Sage UBS")
            {
                logger.message = "This button is not enabled for [Sage UBS]";
                logger.Broadcast();
                //new JobATCInvoiceSync().Execute(); //later change to ubs job
            }
            else
            {
                logger.message = "This job is disable from this play button. Kindly contact EasySales team.";
                logger.Broadcast();
            }

        }

        private void button_check_data_Click(object sender, EventArgs e)
        {
            //var datatable = new DataTable();
            //datatable.Show();
        }

        private void btn_run_transfercashsales_Click(object sender, EventArgs e)
        {
            GlobalLogger logger = new GlobalLogger();
            List<DpprAccountingSoftware> getAccSoftware = LocalDB.GetAccountingSoftwares();
            DpprAccountingSoftware accSoftware = getAccSoftware[0];

            if (accSoftware.software_name == "SQLAccounting")
            {
                logger.message = "Transfer Cash Sales is set to run now";
                logger.Broadcast();
                new JobCSTransfer().Execute();
            }
            else if (accSoftware.software_name == "QNE")
            {
                logger.message = "This button is not enabled for QNE";
                logger.Broadcast();
            }
            else if (accSoftware.software_name == "AutoCount")
            {
                logger.message = "[AutoCount] Transfer Cash Sales will run now";
                logger.Broadcast();

                if(cb_sdk_atc.Checked)
                {
                    new JobATCTransferCSSDK().Execute();
                }
            }
        }

        private void btn_run_salescn_Click(object sender, EventArgs e)
        {
            GlobalLogger logger = new GlobalLogger();
            List<DpprAccountingSoftware> getAccSoftware = LocalDB.GetAccountingSoftwares();
            DpprAccountingSoftware accSoftware = getAccSoftware[0];

            if (accSoftware.software_name == "SQLAccounting")
            {
                logger.message = "Sales Credit Note is set to run now";
                logger.Broadcast();
                new JobCNSalesSync().Execute();
            }
            else
            {
                logger.message = "This button is not enabled for this accounting software. Kindly contact EasySales Team";
                logger.Broadcast();
            }
        }

        private void btn_run_salesdn_Click(object sender, EventArgs e)
        {
            GlobalLogger logger = new GlobalLogger();
            List<DpprAccountingSoftware> getAccSoftware = LocalDB.GetAccountingSoftwares();
            DpprAccountingSoftware accSoftware = getAccSoftware[0];

            if (accSoftware.software_name == "SQLAccounting")
            {
                logger.message = "Sales Debit Note is set to run now";
                logger.Broadcast();
                new JobDNSalesSync().Execute();
            }
            else
            {
                logger.message = "This button is not enabled for this accounting software. Kindly contact EasySales Team";
                logger.Broadcast();
            }
        }

        private void btn_run_salesinv_Click(object sender, EventArgs e)
        {
            GlobalLogger logger = new GlobalLogger();
            List<DpprAccountingSoftware> getAccSoftware = LocalDB.GetAccountingSoftwares();
            DpprAccountingSoftware accSoftware = getAccSoftware[0];

            if (accSoftware.software_name == "SQLAccounting")
            {
                logger.message = "Sales Invoice & Cash Sales are set to run now";
                logger.Broadcast();
                new JobInvoiceSalesSync().Execute();
            }
            else
            {
                logger.message = "This button is not enabled for this accounting software. Kindly contact EasySales Team";
                logger.Broadcast();
            }
        }

        private void button_test_atc_integration_Click(object sender, EventArgs e) //test atc connection
        {
            //throw new Exception("Crash on purpose");
            try
            {
                bool isConnected = AutoCountV1.TriggerConnection();
                if (isConnected == true)
                {
                    MessageBox.Show("Integration successful!", "EasySales-AutoCount", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("Integration failed! Please check the configuration details.", "EasySales", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                MessageBox.Show("Connection failed! Please check the configuration details.", "EasySales", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }
        private void btn_testalert_crash_Click(object sender, EventArgs e)
        {
            throw new Exception("testing crash alert email");
        }
    }
}