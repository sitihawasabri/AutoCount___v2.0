using System.Reflection;

namespace EasySales
{
    partial class DashboardActivity
    {
       
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        
        

#region Windows Form Designer generated code

/// <summary>
/// Required method for Designer support - do not modify
/// the contents of this method with the code editor.
/// </summary>
private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(DashboardActivity));
            this.btn_trigger_sqlaccounting = new System.Windows.Forms.Button();
            this.btn_run_sync = new System.Windows.Forms.Button();
            this.btn_terminate = new System.Windows.Forms.Button();
            this.logview = new System.Windows.Forms.TextBox();
            this.cb_autoStart = new System.Windows.Forms.CheckBox();
            this.nt_inv_intv = new System.Windows.Forms.NumericUpDown();
            this.nt_invdtl_intv = new System.Windows.Forms.NumericUpDown();
            this.nt_outso_intv = new System.Windows.Forms.NumericUpDown();
            this.btn_reset_setting = new System.Windows.Forms.Button();
            this.cb_outso = new System.Windows.Forms.CheckBox();
            this.cb_invoice = new System.Windows.Forms.CheckBox();
            this.cb_inv_dtl = new System.Windows.Forms.CheckBox();
            this.cb_customer = new System.Windows.Forms.CheckBox();
            this.nt_customer_intv = new System.Windows.Forms.NumericUpDown();
            this.cb_stock = new System.Windows.Forms.CheckBox();
            this.nt_stock_intv = new System.Windows.Forms.NumericUpDown();
            this.cb_stockcategories = new System.Windows.Forms.CheckBox();
            this.nt_stockcategories_intv = new System.Windows.Forms.NumericUpDown();
            this.nt_stockuomprice_intv = new System.Windows.Forms.NumericUpDown();
            this.cb_stockuomprice = new System.Windows.Forms.CheckBox();
            this.nt_customeragent_intv = new System.Windows.Forms.NumericUpDown();
            this.cb_customeragent = new System.Windows.Forms.CheckBox();
            this.nt_creditnote_intv = new System.Windows.Forms.NumericUpDown();
            this.cb_creditnote = new System.Windows.Forms.CheckBox();
            this.nt_debitnote_intv = new System.Windows.Forms.NumericUpDown();
            this.cb_debitnote = new System.Windows.Forms.CheckBox();
            this.nt_receipt_intv = new System.Windows.Forms.NumericUpDown();
            this.cb_receipt = new System.Windows.Forms.CheckBox();
            this.nt_post_salesorders_intv = new System.Windows.Forms.NumericUpDown();
            this.cb_post_salesorders = new System.Windows.Forms.CheckBox();
            this.nt_post_salesinvoices_intv = new System.Windows.Forms.NumericUpDown();
            this.cb_post_salesinvoice = new System.Windows.Forms.CheckBox();
            this.nt_productspecialprice_intv = new System.Windows.Forms.NumericUpDown();
            this.cb_productspecialprice = new System.Windows.Forms.CheckBox();
            this.nt_branch_intv = new System.Windows.Forms.NumericUpDown();
            this.cb_branch = new System.Windows.Forms.CheckBox();
            this.labelSOFTWARENAME = new System.Windows.Forms.Label();
            this.nt_readimage_intv = new System.Windows.Forms.NumericUpDown();
            this.cb_readimage = new System.Windows.Forms.CheckBox();
            this.nt_item_template_dtl_intv = new System.Windows.Forms.NumericUpDown();
            this.cb_item_template_dtl = new System.Windows.Forms.CheckBox();
            this.nt_item_template_intv = new System.Windows.Forms.NumericUpDown();
            this.cb_item_template = new System.Windows.Forms.CheckBox();
            this.nt_productgroup_intv = new System.Windows.Forms.NumericUpDown();
            this.cb_productgroup = new System.Windows.Forms.CheckBox();
            this.nt_costprice_intv = new System.Windows.Forms.NumericUpDown();
            this.cb_costprice = new System.Windows.Forms.CheckBox();
            this.nt_post_stock_transfer = new System.Windows.Forms.NumericUpDown();
            this.cb_stock_transfer = new System.Windows.Forms.CheckBox();
            this.nt_creditnote_details_intv = new System.Windows.Forms.NumericUpDown();
            this.cb_creditnote_details = new System.Windows.Forms.CheckBox();
            this.lbl_updateinfo = new System.Windows.Forms.Label();
            this.btn_updatenow = new System.Windows.Forms.Button();
            this.nt_knockoff_intv = new System.Windows.Forms.NumericUpDown();
            this.cb_knockoff = new System.Windows.Forms.CheckBox();
            this.btn_run_custsync = new System.Windows.Forms.Button();
            this.btn_run_transferso = new System.Windows.Forms.Button();
            this.btn_run_stocksync = new System.Windows.Forms.Button();
            this.nt_warehouse_intv = new System.Windows.Forms.NumericUpDown();
            this.cb_warehouse = new System.Windows.Forms.CheckBox();
            this.btn_run_invsync = new System.Windows.Forms.Button();
            this.btn_run_stockgroupsync = new System.Windows.Forms.Button();
            this.btn_run_specialpricesync = new System.Windows.Forms.Button();
            this.btn_run_uompricesync = new System.Windows.Forms.Button();
            this.btn_run_stockcatsync = new System.Windows.Forms.Button();
            this.btn_run_branchsync = new System.Windows.Forms.Button();
            this.btn_run_custagentsync = new System.Windows.Forms.Button();
            this.btn_run_invdtlsync = new System.Windows.Forms.Button();
            this.btn_run_stock_transfer = new System.Windows.Forms.Button();
            this.btn_run_itemtmpdtlsync = new System.Windows.Forms.Button();
            this.btn_run_itemtmpsync = new System.Windows.Forms.Button();
            this.btn_run_imagesync = new System.Windows.Forms.Button();
            this.btn_run_transfersalesinv = new System.Windows.Forms.Button();
            this.btn_run_outsosync = new System.Windows.Forms.Button();
            this.btn_run_dnsync = new System.Windows.Forms.Button();
            this.btn_run_cndtlsync = new System.Windows.Forms.Button();
            this.btn_run_cnsync = new System.Windows.Forms.Button();
            this.btn_run_rcptsync = new System.Windows.Forms.Button();
            this.btn_run_whqtysync = new System.Windows.Forms.Button();
            this.btn_run_ageingkosync = new System.Windows.Forms.Button();
            this.btn_run_costpricesync = new System.Windows.Forms.Button();
            this.btn_run_transfersalescns = new System.Windows.Forms.Button();
            this.nt_post_salescns_intv = new System.Windows.Forms.NumericUpDown();
            this.cb_postsalescns = new System.Windows.Forms.CheckBox();
            this.btn_run_dosync = new System.Windows.Forms.Button();
            this.nt_do_intv = new System.Windows.Forms.NumericUpDown();
            this.cb_do = new System.Windows.Forms.CheckBox();
            this.btn_run_transferquo = new System.Windows.Forms.Button();
            this.nt_post_quo_intv = new System.Windows.Forms.NumericUpDown();
            this.cb_postquo = new System.Windows.Forms.CheckBox();
            this.btn_run_postpobasketsync = new System.Windows.Forms.Button();
            this.nt_postpobasket_intv = new System.Windows.Forms.NumericUpDown();
            this.cb_postpobasket = new System.Windows.Forms.CheckBox();
            this.lbl_company = new System.Windows.Forms.Label();
            this.btn_run_postpaymentsync = new System.Windows.Forms.Button();
            this.nt_postpayment_intv = new System.Windows.Forms.NumericUpDown();
            this.cb_postpayment = new System.Windows.Forms.CheckBox();
            this.btn_run_cfsync = new System.Windows.Forms.Button();
            this.nt_cust_refund_intv = new System.Windows.Forms.NumericUpDown();
            this.cb_cust_refund = new System.Windows.Forms.CheckBox();
            this.btn_run_sosync = new System.Windows.Forms.Button();
            this.nt_sosync_intv = new System.Windows.Forms.NumericUpDown();
            this.cb_sosync = new System.Windows.Forms.CheckBox();
            this.button_check_data = new System.Windows.Forms.Button();
            this.btn_run_transfercashsales = new System.Windows.Forms.Button();
            this.nt_post_cashsales_intv = new System.Windows.Forms.NumericUpDown();
            this.cb_post_cashsales = new System.Windows.Forms.CheckBox();
            this.btn_run_salescn = new System.Windows.Forms.Button();
            this.nt_sales_cn_intv = new System.Windows.Forms.NumericUpDown();
            this.cb_sales_cn = new System.Windows.Forms.CheckBox();
            this.btn_run_salesdn = new System.Windows.Forms.Button();
            this.nt_sales_dn_intv = new System.Windows.Forms.NumericUpDown();
            this.cb_sales_dn = new System.Windows.Forms.CheckBox();
            this.btn_run_salesinv = new System.Windows.Forms.Button();
            this.cb_sales_inv = new System.Windows.Forms.CheckBox();
            this.nt_sales_invoice_intv = new System.Windows.Forms.NumericUpDown();
            this.button_test_atc_integration = new System.Windows.Forms.Button();
            this.cb_sdk_atc = new System.Windows.Forms.CheckBox();
            this.btn_testalert_crash = new System.Windows.Forms.Button();
            this.btn_run_stockcardsync = new System.Windows.Forms.Button();
            this.nt_stockcard_intv = new System.Windows.Forms.NumericUpDown();
            this.cb_stockcard = new System.Windows.Forms.CheckBox();
            this.cb_atc_v2 = new System.Windows.Forms.CheckBox();
            this.label1 = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.nt_inv_intv)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nt_invdtl_intv)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nt_outso_intv)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nt_customer_intv)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nt_stock_intv)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nt_stockcategories_intv)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nt_stockuomprice_intv)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nt_customeragent_intv)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nt_creditnote_intv)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nt_debitnote_intv)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nt_receipt_intv)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nt_post_salesorders_intv)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nt_post_salesinvoices_intv)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nt_productspecialprice_intv)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nt_branch_intv)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nt_readimage_intv)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nt_item_template_dtl_intv)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nt_item_template_intv)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nt_productgroup_intv)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nt_costprice_intv)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nt_post_stock_transfer)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nt_creditnote_details_intv)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nt_knockoff_intv)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nt_warehouse_intv)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nt_post_salescns_intv)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nt_do_intv)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nt_post_quo_intv)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nt_postpobasket_intv)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nt_postpayment_intv)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nt_cust_refund_intv)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nt_sosync_intv)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nt_post_cashsales_intv)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nt_sales_cn_intv)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nt_sales_dn_intv)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nt_sales_invoice_intv)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nt_stockcard_intv)).BeginInit();
            this.SuspendLayout();
            // 
            // btn_trigger_sqlaccounting
            // 
            this.btn_trigger_sqlaccounting.Location = new System.Drawing.Point(968, 255);
            this.btn_trigger_sqlaccounting.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.btn_trigger_sqlaccounting.Name = "btn_trigger_sqlaccounting";
            this.btn_trigger_sqlaccounting.Size = new System.Drawing.Size(225, 32);
            this.btn_trigger_sqlaccounting.TabIndex = 0;
            this.btn_trigger_sqlaccounting.Text = "Toggle SQLAccounting";
            this.btn_trigger_sqlaccounting.UseVisualStyleBackColor = true;
            this.btn_trigger_sqlaccounting.Click += new System.EventHandler(this.btn_trigger_sqlaccounting_Click);
            // 
            // btn_run_sync
            // 
            this.btn_run_sync.Location = new System.Drawing.Point(968, 155);
            this.btn_run_sync.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.btn_run_sync.Name = "btn_run_sync";
            this.btn_run_sync.Size = new System.Drawing.Size(225, 32);
            this.btn_run_sync.TabIndex = 1;
            this.btn_run_sync.Text = "Run";
            this.btn_run_sync.UseVisualStyleBackColor = true;
            this.btn_run_sync.Click += new System.EventHandler(this.btn_run_sync_Click);
            // 
            // btn_terminate
            // 
            this.btn_terminate.Location = new System.Drawing.Point(968, 222);
            this.btn_terminate.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.btn_terminate.Name = "btn_terminate";
            this.btn_terminate.Size = new System.Drawing.Size(225, 32);
            this.btn_terminate.TabIndex = 2;
            this.btn_terminate.Text = "Terminate";
            this.btn_terminate.UseVisualStyleBackColor = true;
            this.btn_terminate.Click += new System.EventHandler(this.btn_terminate_Click);
            // 
            // logview
            // 
            this.logview.BackColor = System.Drawing.SystemColors.ControlLight;
            this.logview.Location = new System.Drawing.Point(9, 566);
            this.logview.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.logview.Multiline = true;
            this.logview.Name = "logview";
            this.logview.ReadOnly = true;
            this.logview.Size = new System.Drawing.Size(1189, 361);
            this.logview.TabIndex = 3;
            this.logview.Text = "--------------LOG--------------";
            // 
            // cb_autoStart
            // 
            this.cb_autoStart.AutoSize = true;
            this.cb_autoStart.Location = new System.Drawing.Point(974, 125);
            this.cb_autoStart.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.cb_autoStart.Name = "cb_autoStart";
            this.cb_autoStart.Size = new System.Drawing.Size(108, 24);
            this.cb_autoStart.TabIndex = 4;
            this.cb_autoStart.Text = "Auto Start";
            this.cb_autoStart.UseVisualStyleBackColor = true;
            this.cb_autoStart.CheckedChanged += new System.EventHandler(this.cb_autoStart_CheckedChanged);
            // 
            // nt_inv_intv
            // 
            this.nt_inv_intv.Location = new System.Drawing.Point(302, 245);
            this.nt_inv_intv.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.nt_inv_intv.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this.nt_inv_intv.Minimum = new decimal(new int[] {
            5,
            0,
            0,
            0});
            this.nt_inv_intv.Name = "nt_inv_intv";
            this.nt_inv_intv.Size = new System.Drawing.Size(92, 26);
            this.nt_inv_intv.TabIndex = 96;
            this.nt_inv_intv.Value = new decimal(new int[] {
            5,
            0,
            0,
            0});
            this.nt_inv_intv.ValueChanged += new System.EventHandler(this.nt_inv_intv_ValueChanged);
            // 
            // nt_invdtl_intv
            // 
            this.nt_invdtl_intv.Location = new System.Drawing.Point(302, 271);
            this.nt_invdtl_intv.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.nt_invdtl_intv.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this.nt_invdtl_intv.Minimum = new decimal(new int[] {
            5,
            0,
            0,
            0});
            this.nt_invdtl_intv.Name = "nt_invdtl_intv";
            this.nt_invdtl_intv.Size = new System.Drawing.Size(92, 26);
            this.nt_invdtl_intv.TabIndex = 95;
            this.nt_invdtl_intv.Value = new decimal(new int[] {
            5,
            0,
            0,
            0});
            this.nt_invdtl_intv.ValueChanged += new System.EventHandler(this.nt_invdtl_intv_ValueChanged);
            // 
            // nt_outso_intv
            // 
            this.nt_outso_intv.Location = new System.Drawing.Point(784, 163);
            this.nt_outso_intv.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.nt_outso_intv.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this.nt_outso_intv.Minimum = new decimal(new int[] {
            5,
            0,
            0,
            0});
            this.nt_outso_intv.Name = "nt_outso_intv";
            this.nt_outso_intv.Size = new System.Drawing.Size(92, 26);
            this.nt_outso_intv.TabIndex = 94;
            this.nt_outso_intv.Value = new decimal(new int[] {
            5,
            0,
            0,
            0});
            this.nt_outso_intv.ValueChanged += new System.EventHandler(this.nt_outso_intv_ValueChanged);
            // 
            // btn_reset_setting
            // 
            this.btn_reset_setting.BackColor = System.Drawing.Color.Red;
            this.btn_reset_setting.ForeColor = System.Drawing.SystemColors.ButtonHighlight;
            this.btn_reset_setting.Location = new System.Drawing.Point(968, 189);
            this.btn_reset_setting.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.btn_reset_setting.Name = "btn_reset_setting";
            this.btn_reset_setting.Size = new System.Drawing.Size(225, 32);
            this.btn_reset_setting.TabIndex = 13;
            this.btn_reset_setting.Text = "RESET SETTINGS";
            this.btn_reset_setting.UseVisualStyleBackColor = false;
            this.btn_reset_setting.Click += new System.EventHandler(this.btn_reset_setting_Click);
            // 
            // cb_outso
            // 
            this.cb_outso.AutoSize = true;
            this.cb_outso.Location = new System.Drawing.Point(458, 165);
            this.cb_outso.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.cb_outso.Name = "cb_outso";
            this.cb_outso.Size = new System.Drawing.Size(244, 24);
            this.cb_outso.TabIndex = 14;
            this.cb_outso.Text = "Outstanding SO Interval (min)";
            this.cb_outso.UseVisualStyleBackColor = true;
            this.cb_outso.CheckedChanged += new System.EventHandler(this.cb_outso_CheckedChanged);
            // 
            // cb_invoice
            // 
            this.cb_invoice.AutoSize = true;
            this.cb_invoice.Location = new System.Drawing.Point(10, 245);
            this.cb_invoice.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.cb_invoice.Name = "cb_invoice";
            this.cb_invoice.Size = new System.Drawing.Size(180, 24);
            this.cb_invoice.TabIndex = 15;
            this.cb_invoice.Text = "Invoice Interval (min)";
            this.cb_invoice.UseVisualStyleBackColor = true;
            this.cb_invoice.CheckedChanged += new System.EventHandler(this.cb_invoice_CheckedChanged);
            // 
            // cb_inv_dtl
            // 
            this.cb_inv_dtl.AutoSize = true;
            this.cb_inv_dtl.Location = new System.Drawing.Point(10, 272);
            this.cb_inv_dtl.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.cb_inv_dtl.Name = "cb_inv_dtl";
            this.cb_inv_dtl.Size = new System.Drawing.Size(233, 24);
            this.cb_inv_dtl.TabIndex = 16;
            this.cb_inv_dtl.Text = "Invoice Details Interval (min)";
            this.cb_inv_dtl.UseVisualStyleBackColor = true;
            this.cb_inv_dtl.CheckedChanged += new System.EventHandler(this.cb_inv_dtl_CheckedChanged);
            // 
            // cb_customer
            // 
            this.cb_customer.AutoSize = true;
            this.cb_customer.Location = new System.Drawing.Point(10, 2);
            this.cb_customer.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.cb_customer.Name = "cb_customer";
            this.cb_customer.Size = new System.Drawing.Size(199, 24);
            this.cb_customer.TabIndex = 17;
            this.cb_customer.Text = "Customer Interval (min)";
            this.cb_customer.UseVisualStyleBackColor = true;
            this.cb_customer.CheckedChanged += new System.EventHandler(this.cb_customer_CheckedChanged);
            // 
            // nt_customer_intv
            // 
            this.nt_customer_intv.Location = new System.Drawing.Point(302, 2);
            this.nt_customer_intv.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.nt_customer_intv.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this.nt_customer_intv.Minimum = new decimal(new int[] {
            5,
            0,
            0,
            0});
            this.nt_customer_intv.Name = "nt_customer_intv";
            this.nt_customer_intv.Size = new System.Drawing.Size(92, 26);
            this.nt_customer_intv.TabIndex = 93;
            this.nt_customer_intv.Value = new decimal(new int[] {
            5,
            0,
            0,
            0});
            this.nt_customer_intv.ValueChanged += new System.EventHandler(this.nt_customer_intv_ValueChanged);
            // 
            // cb_stock
            // 
            this.cb_stock.AutoSize = true;
            this.cb_stock.Location = new System.Drawing.Point(10, 109);
            this.cb_stock.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.cb_stock.Name = "cb_stock";
            this.cb_stock.Size = new System.Drawing.Size(171, 24);
            this.cb_stock.TabIndex = 19;
            this.cb_stock.Text = "Stock Interval (min)";
            this.cb_stock.UseVisualStyleBackColor = true;
            this.cb_stock.CheckedChanged += new System.EventHandler(this.cb_stock_CheckedChanged);
            // 
            // nt_stock_intv
            // 
            this.nt_stock_intv.Location = new System.Drawing.Point(302, 109);
            this.nt_stock_intv.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.nt_stock_intv.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this.nt_stock_intv.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.nt_stock_intv.Name = "nt_stock_intv";
            this.nt_stock_intv.Size = new System.Drawing.Size(92, 26);
            this.nt_stock_intv.TabIndex = 92;
            this.nt_stock_intv.Value = new decimal(new int[] {
            5,
            0,
            0,
            0});
            this.nt_stock_intv.ValueChanged += new System.EventHandler(this.nt_stock_intv_ValueChanged);
            // 
            // cb_stockcategories
            // 
            this.cb_stockcategories.AutoSize = true;
            this.cb_stockcategories.Location = new System.Drawing.Point(10, 83);
            this.cb_stockcategories.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.cb_stockcategories.Name = "cb_stockcategories";
            this.cb_stockcategories.Size = new System.Drawing.Size(252, 24);
            this.cb_stockcategories.TabIndex = 21;
            this.cb_stockcategories.Text = "Stock Categories Interval (min)";
            this.cb_stockcategories.UseVisualStyleBackColor = true;
            this.cb_stockcategories.CheckedChanged += new System.EventHandler(this.cb_stockcategories_CheckedChanged);
            // 
            // nt_stockcategories_intv
            // 
            this.nt_stockcategories_intv.Location = new System.Drawing.Point(302, 82);
            this.nt_stockcategories_intv.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.nt_stockcategories_intv.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this.nt_stockcategories_intv.Minimum = new decimal(new int[] {
            5,
            0,
            0,
            0});
            this.nt_stockcategories_intv.Name = "nt_stockcategories_intv";
            this.nt_stockcategories_intv.Size = new System.Drawing.Size(92, 26);
            this.nt_stockcategories_intv.TabIndex = 91;
            this.nt_stockcategories_intv.Value = new decimal(new int[] {
            5,
            0,
            0,
            0});
            this.nt_stockcategories_intv.ValueChanged += new System.EventHandler(this.nt_stockcategories_intv_ValueChanged);
            // 
            // nt_stockuomprice_intv
            // 
            this.nt_stockuomprice_intv.Location = new System.Drawing.Point(302, 135);
            this.nt_stockuomprice_intv.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.nt_stockuomprice_intv.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this.nt_stockuomprice_intv.Minimum = new decimal(new int[] {
            5,
            0,
            0,
            0});
            this.nt_stockuomprice_intv.Name = "nt_stockuomprice_intv";
            this.nt_stockuomprice_intv.Size = new System.Drawing.Size(92, 26);
            this.nt_stockuomprice_intv.TabIndex = 90;
            this.nt_stockuomprice_intv.Value = new decimal(new int[] {
            5,
            0,
            0,
            0});
            this.nt_stockuomprice_intv.ValueChanged += new System.EventHandler(this.nt_stockuomprice_intv_ValueChanged);
            // 
            // cb_stockuomprice
            // 
            this.cb_stockuomprice.AutoSize = true;
            this.cb_stockuomprice.Location = new System.Drawing.Point(10, 137);
            this.cb_stockuomprice.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.cb_stockuomprice.Name = "cb_stockuomprice";
            this.cb_stockuomprice.Size = new System.Drawing.Size(251, 24);
            this.cb_stockuomprice.TabIndex = 23;
            this.cb_stockuomprice.Text = "Stock UOM Price Interval (min)";
            this.cb_stockuomprice.UseVisualStyleBackColor = true;
            this.cb_stockuomprice.CheckedChanged += new System.EventHandler(this.cb_stockuomprice_CheckedChanged);
            // 
            // nt_customeragent_intv
            // 
            this.nt_customeragent_intv.Location = new System.Drawing.Point(302, 28);
            this.nt_customeragent_intv.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.nt_customeragent_intv.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this.nt_customeragent_intv.Minimum = new decimal(new int[] {
            5,
            0,
            0,
            0});
            this.nt_customeragent_intv.Name = "nt_customeragent_intv";
            this.nt_customeragent_intv.Size = new System.Drawing.Size(92, 26);
            this.nt_customeragent_intv.TabIndex = 89;
            this.nt_customeragent_intv.Value = new decimal(new int[] {
            5,
            0,
            0,
            0});
            this.nt_customeragent_intv.ValueChanged += new System.EventHandler(this.nt_customeragent_intv_ValueChanged);
            // 
            // cb_customeragent
            // 
            this.cb_customeragent.AutoSize = true;
            this.cb_customeragent.Location = new System.Drawing.Point(10, 29);
            this.cb_customeragent.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.cb_customeragent.Name = "cb_customeragent";
            this.cb_customeragent.Size = new System.Drawing.Size(255, 24);
            this.cb_customeragent.TabIndex = 31;
            this.cb_customeragent.Text = "Customer - Agent Interval (min)";
            this.cb_customeragent.UseVisualStyleBackColor = true;
            this.cb_customeragent.CheckedChanged += new System.EventHandler(this.cb_customeragent_CheckedChanged);
            // 
            // nt_creditnote_intv
            // 
            this.nt_creditnote_intv.Location = new System.Drawing.Point(784, 28);
            this.nt_creditnote_intv.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.nt_creditnote_intv.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this.nt_creditnote_intv.Minimum = new decimal(new int[] {
            5,
            0,
            0,
            0});
            this.nt_creditnote_intv.Name = "nt_creditnote_intv";
            this.nt_creditnote_intv.Size = new System.Drawing.Size(92, 26);
            this.nt_creditnote_intv.TabIndex = 88;
            this.nt_creditnote_intv.Value = new decimal(new int[] {
            5,
            0,
            0,
            0});
            this.nt_creditnote_intv.ValueChanged += new System.EventHandler(this.nt_creditnote_intv_ValueChanged);
            // 
            // cb_creditnote
            // 
            this.cb_creditnote.AutoSize = true;
            this.cb_creditnote.Location = new System.Drawing.Point(458, 29);
            this.cb_creditnote.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.cb_creditnote.Name = "cb_creditnote";
            this.cb_creditnote.Size = new System.Drawing.Size(210, 24);
            this.cb_creditnote.TabIndex = 33;
            this.cb_creditnote.Text = "Credit Note Interval (min)";
            this.cb_creditnote.UseVisualStyleBackColor = true;
            this.cb_creditnote.CheckedChanged += new System.EventHandler(this.cb_creditnote_CheckedChanged);
            // 
            // nt_debitnote_intv
            // 
            this.nt_debitnote_intv.Location = new System.Drawing.Point(784, 109);
            this.nt_debitnote_intv.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.nt_debitnote_intv.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this.nt_debitnote_intv.Minimum = new decimal(new int[] {
            5,
            0,
            0,
            0});
            this.nt_debitnote_intv.Name = "nt_debitnote_intv";
            this.nt_debitnote_intv.Size = new System.Drawing.Size(92, 26);
            this.nt_debitnote_intv.TabIndex = 87;
            this.nt_debitnote_intv.Value = new decimal(new int[] {
            5,
            0,
            0,
            0});
            this.nt_debitnote_intv.ValueChanged += new System.EventHandler(this.nt_debitnote_intv_ValueChanged);
            // 
            // cb_debitnote
            // 
            this.cb_debitnote.AutoSize = true;
            this.cb_debitnote.Location = new System.Drawing.Point(458, 109);
            this.cb_debitnote.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.cb_debitnote.Name = "cb_debitnote";
            this.cb_debitnote.Size = new System.Drawing.Size(290, 24);
            this.cb_debitnote.TabIndex = 35;
            this.cb_debitnote.Text = "Debit Note and Details Interval (min)";
            this.cb_debitnote.UseVisualStyleBackColor = true;
            this.cb_debitnote.CheckedChanged += new System.EventHandler(this.cb_debitnote_CheckedChanged);
            // 
            // nt_receipt_intv
            // 
            this.nt_receipt_intv.Location = new System.Drawing.Point(784, 2);
            this.nt_receipt_intv.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.nt_receipt_intv.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this.nt_receipt_intv.Minimum = new decimal(new int[] {
            5,
            0,
            0,
            0});
            this.nt_receipt_intv.Name = "nt_receipt_intv";
            this.nt_receipt_intv.Size = new System.Drawing.Size(92, 26);
            this.nt_receipt_intv.TabIndex = 86;
            this.nt_receipt_intv.Value = new decimal(new int[] {
            5,
            0,
            0,
            0});
            this.nt_receipt_intv.ValueChanged += new System.EventHandler(this.nt_receipt_intv_ValueChanged);
            // 
            // cb_receipt
            // 
            this.cb_receipt.AutoSize = true;
            this.cb_receipt.Location = new System.Drawing.Point(458, 2);
            this.cb_receipt.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.cb_receipt.Name = "cb_receipt";
            this.cb_receipt.Size = new System.Drawing.Size(185, 24);
            this.cb_receipt.TabIndex = 37;
            this.cb_receipt.Text = "Receipt Interval (min)";
            this.cb_receipt.UseVisualStyleBackColor = true;
            this.cb_receipt.CheckedChanged += new System.EventHandler(this.cb_receipt_CheckedChanged);
            // 
            // nt_post_salesorders_intv
            // 
            this.nt_post_salesorders_intv.Location = new System.Drawing.Point(784, 189);
            this.nt_post_salesorders_intv.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.nt_post_salesorders_intv.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this.nt_post_salesorders_intv.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.nt_post_salesorders_intv.Name = "nt_post_salesorders_intv";
            this.nt_post_salesorders_intv.Size = new System.Drawing.Size(92, 26);
            this.nt_post_salesorders_intv.TabIndex = 85;
            this.nt_post_salesorders_intv.Value = new decimal(new int[] {
            5,
            0,
            0,
            0});
            this.nt_post_salesorders_intv.ValueChanged += new System.EventHandler(this.nt_post_salesorders_intv_ValueChanged);
            // 
            // cb_post_salesorders
            // 
            this.cb_post_salesorders.AutoSize = true;
            this.cb_post_salesorders.Location = new System.Drawing.Point(458, 191);
            this.cb_post_salesorders.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.cb_post_salesorders.Name = "cb_post_salesorders";
            this.cb_post_salesorders.Size = new System.Drawing.Size(258, 24);
            this.cb_post_salesorders.TabIndex = 39;
            this.cb_post_salesorders.Text = "Post Sales Orders Interval (min)";
            this.cb_post_salesorders.UseVisualStyleBackColor = true;
            this.cb_post_salesorders.CheckedChanged += new System.EventHandler(this.cb_post_salesorders_CheckedChanged);
            // 
            // nt_post_salesinvoices_intv
            // 
            this.nt_post_salesinvoices_intv.Location = new System.Drawing.Point(784, 217);
            this.nt_post_salesinvoices_intv.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.nt_post_salesinvoices_intv.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this.nt_post_salesinvoices_intv.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.nt_post_salesinvoices_intv.Name = "nt_post_salesinvoices_intv";
            this.nt_post_salesinvoices_intv.Size = new System.Drawing.Size(92, 26);
            this.nt_post_salesinvoices_intv.TabIndex = 84;
            this.nt_post_salesinvoices_intv.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.nt_post_salesinvoices_intv.ValueChanged += new System.EventHandler(this.nt_post_salesinvoices_intv_ValueChanged);
            // 
            // cb_post_salesinvoice
            // 
            this.cb_post_salesinvoice.AutoSize = true;
            this.cb_post_salesinvoice.Location = new System.Drawing.Point(458, 218);
            this.cb_post_salesinvoice.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.cb_post_salesinvoice.Name = "cb_post_salesinvoice";
            this.cb_post_salesinvoice.Size = new System.Drawing.Size(268, 24);
            this.cb_post_salesinvoice.TabIndex = 41;
            this.cb_post_salesinvoice.Text = "Post Sales Invoices Interval (min)";
            this.cb_post_salesinvoice.UseVisualStyleBackColor = true;
            this.cb_post_salesinvoice.CheckedChanged += new System.EventHandler(this.cb_post_salesinvoices_CheckedChanged);
            // 
            // nt_productspecialprice_intv
            // 
            this.nt_productspecialprice_intv.Location = new System.Drawing.Point(302, 163);
            this.nt_productspecialprice_intv.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.nt_productspecialprice_intv.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this.nt_productspecialprice_intv.Minimum = new decimal(new int[] {
            5,
            0,
            0,
            0});
            this.nt_productspecialprice_intv.Name = "nt_productspecialprice_intv";
            this.nt_productspecialprice_intv.Size = new System.Drawing.Size(92, 26);
            this.nt_productspecialprice_intv.TabIndex = 83;
            this.nt_productspecialprice_intv.Value = new decimal(new int[] {
            5,
            0,
            0,
            0});
            this.nt_productspecialprice_intv.ValueChanged += new System.EventHandler(this.nt_productspecialprice_intv_ValueChanged);
            // 
            // cb_productspecialprice
            // 
            this.cb_productspecialprice.AutoSize = true;
            this.cb_productspecialprice.Location = new System.Drawing.Point(10, 165);
            this.cb_productspecialprice.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.cb_productspecialprice.Name = "cb_productspecialprice";
            this.cb_productspecialprice.Size = new System.Drawing.Size(266, 24);
            this.cb_productspecialprice.TabIndex = 43;
            this.cb_productspecialprice.Text = "Stock Special Price Interval (min)";
            this.cb_productspecialprice.UseVisualStyleBackColor = true;
            this.cb_productspecialprice.CheckedChanged += new System.EventHandler(this.cb_productspecialprice_CheckedChanged);
            // 
            // nt_branch_intv
            // 
            this.nt_branch_intv.Location = new System.Drawing.Point(302, 55);
            this.nt_branch_intv.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.nt_branch_intv.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this.nt_branch_intv.Minimum = new decimal(new int[] {
            5,
            0,
            0,
            0});
            this.nt_branch_intv.Name = "nt_branch_intv";
            this.nt_branch_intv.Size = new System.Drawing.Size(92, 26);
            this.nt_branch_intv.TabIndex = 82;
            this.nt_branch_intv.Value = new decimal(new int[] {
            5,
            0,
            0,
            0});
            this.nt_branch_intv.ValueChanged += new System.EventHandler(this.nt_branch_intv_ValueChanged);
            // 
            // cb_branch
            // 
            this.cb_branch.AutoSize = true;
            this.cb_branch.Location = new System.Drawing.Point(10, 55);
            this.cb_branch.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.cb_branch.Name = "cb_branch";
            this.cb_branch.Size = new System.Drawing.Size(181, 24);
            this.cb_branch.TabIndex = 45;
            this.cb_branch.Text = "Branch Interval (min)";
            this.cb_branch.UseVisualStyleBackColor = true;
            this.cb_branch.CheckedChanged += new System.EventHandler(this.cb_branch_CheckedChanged);
            // 
            // labelSOFTWARENAME
            // 
            this.labelSOFTWARENAME.AutoSize = true;
            this.labelSOFTWARENAME.Location = new System.Drawing.Point(255, 175);
            this.labelSOFTWARENAME.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.labelSOFTWARENAME.Name = "labelSOFTWARENAME";
            this.labelSOFTWARENAME.Size = new System.Drawing.Size(0, 20);
            this.labelSOFTWARENAME.TabIndex = 47;
            // 
            // nt_readimage_intv
            // 
            this.nt_readimage_intv.Location = new System.Drawing.Point(302, 406);
            this.nt_readimage_intv.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.nt_readimage_intv.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this.nt_readimage_intv.Minimum = new decimal(new int[] {
            5,
            0,
            0,
            0});
            this.nt_readimage_intv.Name = "nt_readimage_intv";
            this.nt_readimage_intv.Size = new System.Drawing.Size(92, 26);
            this.nt_readimage_intv.TabIndex = 81;
            this.nt_readimage_intv.Value = new decimal(new int[] {
            5,
            0,
            0,
            0});
            this.nt_readimage_intv.ValueChanged += new System.EventHandler(this.nt_readimage_intv_ValueChanged);
            // 
            // cb_readimage
            // 
            this.cb_readimage.AutoSize = true;
            this.cb_readimage.Location = new System.Drawing.Point(10, 408);
            this.cb_readimage.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.cb_readimage.Name = "cb_readimage";
            this.cb_readimage.Size = new System.Drawing.Size(214, 24);
            this.cb_readimage.TabIndex = 50;
            this.cb_readimage.Text = "Image Sync Interval (min)";
            this.cb_readimage.UseVisualStyleBackColor = true;
            this.cb_readimage.CheckedChanged += new System.EventHandler(this.cb_readimage_CheckedChanged);
            // 
            // nt_item_template_dtl_intv
            // 
            this.nt_item_template_dtl_intv.Location = new System.Drawing.Point(784, 432);
            this.nt_item_template_dtl_intv.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.nt_item_template_dtl_intv.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this.nt_item_template_dtl_intv.Minimum = new decimal(new int[] {
            5,
            0,
            0,
            0});
            this.nt_item_template_dtl_intv.Name = "nt_item_template_dtl_intv";
            this.nt_item_template_dtl_intv.Size = new System.Drawing.Size(92, 26);
            this.nt_item_template_dtl_intv.TabIndex = 80;
            this.nt_item_template_dtl_intv.Value = new decimal(new int[] {
            5,
            0,
            0,
            0});
            this.nt_item_template_dtl_intv.ValueChanged += new System.EventHandler(this.nt_item_template_dtl_intv_ValueChanged);
            // 
            // cb_item_template_dtl
            // 
            this.cb_item_template_dtl.AutoSize = true;
            this.cb_item_template_dtl.Location = new System.Drawing.Point(458, 434);
            this.cb_item_template_dtl.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.cb_item_template_dtl.Name = "cb_item_template_dtl";
            this.cb_item_template_dtl.Size = new System.Drawing.Size(285, 24);
            this.cb_item_template_dtl.TabIndex = 52;
            this.cb_item_template_dtl.Text = "Item Template Details Interval (min)";
            this.cb_item_template_dtl.UseVisualStyleBackColor = true;
            this.cb_item_template_dtl.CheckedChanged += new System.EventHandler(this.cb_item_template_dtl_CheckedChanged);
            // 
            // nt_item_template_intv
            // 
            this.nt_item_template_intv.Location = new System.Drawing.Point(784, 406);
            this.nt_item_template_intv.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.nt_item_template_intv.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this.nt_item_template_intv.Minimum = new decimal(new int[] {
            5,
            0,
            0,
            0});
            this.nt_item_template_intv.Name = "nt_item_template_intv";
            this.nt_item_template_intv.Size = new System.Drawing.Size(92, 26);
            this.nt_item_template_intv.TabIndex = 79;
            this.nt_item_template_intv.Value = new decimal(new int[] {
            5,
            0,
            0,
            0});
            this.nt_item_template_intv.ValueChanged += new System.EventHandler(this.nt_item_template_intv_ValueChanged);
            // 
            // cb_item_template
            // 
            this.cb_item_template.AutoSize = true;
            this.cb_item_template.Location = new System.Drawing.Point(458, 408);
            this.cb_item_template.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.cb_item_template.Name = "cb_item_template";
            this.cb_item_template.Size = new System.Drawing.Size(232, 24);
            this.cb_item_template.TabIndex = 54;
            this.cb_item_template.Text = "Item Template Interval (min)";
            this.cb_item_template.UseVisualStyleBackColor = true;
            this.cb_item_template.CheckedChanged += new System.EventHandler(this.cb_item_template_CheckedChanged);
            // 
            // nt_productgroup_intv
            // 
            this.nt_productgroup_intv.Location = new System.Drawing.Point(302, 189);
            this.nt_productgroup_intv.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.nt_productgroup_intv.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this.nt_productgroup_intv.Minimum = new decimal(new int[] {
            5,
            0,
            0,
            0});
            this.nt_productgroup_intv.Name = "nt_productgroup_intv";
            this.nt_productgroup_intv.Size = new System.Drawing.Size(92, 26);
            this.nt_productgroup_intv.TabIndex = 78;
            this.nt_productgroup_intv.Value = new decimal(new int[] {
            5,
            0,
            0,
            0});
            this.nt_productgroup_intv.ValueChanged += new System.EventHandler(this.nt_productgroup_intv_ValueChanged);
            // 
            // cb_productgroup
            // 
            this.cb_productgroup.AutoSize = true;
            this.cb_productgroup.Location = new System.Drawing.Point(10, 191);
            this.cb_productgroup.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.cb_productgroup.Name = "cb_productgroup";
            this.cb_productgroup.Size = new System.Drawing.Size(220, 24);
            this.cb_productgroup.TabIndex = 56;
            this.cb_productgroup.Text = "Stock Group Interval (min)";
            this.cb_productgroup.UseVisualStyleBackColor = true;
            this.cb_productgroup.CheckedChanged += new System.EventHandler(this.cb_productgroup_CheckedChanged);
            // 
            // nt_costprice_intv
            // 
            this.nt_costprice_intv.BackColor = System.Drawing.Color.White;
            this.nt_costprice_intv.ForeColor = System.Drawing.SystemColors.WindowText;
            this.nt_costprice_intv.Location = new System.Drawing.Point(784, 462);
            this.nt_costprice_intv.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.nt_costprice_intv.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this.nt_costprice_intv.Minimum = new decimal(new int[] {
            5,
            0,
            0,
            0});
            this.nt_costprice_intv.Name = "nt_costprice_intv";
            this.nt_costprice_intv.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.nt_costprice_intv.Size = new System.Drawing.Size(92, 26);
            this.nt_costprice_intv.TabIndex = 77;
            this.nt_costprice_intv.Value = new decimal(new int[] {
            5,
            0,
            0,
            0});
            this.nt_costprice_intv.ValueChanged += new System.EventHandler(this.nt_costprice_intv_ValueChanged);
            // 
            // cb_costprice
            // 
            this.cb_costprice.AutoSize = true;
            this.cb_costprice.Location = new System.Drawing.Point(458, 462);
            this.cb_costprice.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.cb_costprice.Name = "cb_costprice";
            this.cb_costprice.Size = new System.Drawing.Size(202, 24);
            this.cb_costprice.TabIndex = 58;
            this.cb_costprice.Text = "Cost Price Interval (min)";
            this.cb_costprice.UseVisualStyleBackColor = true;
            this.cb_costprice.CheckedChanged += new System.EventHandler(this.cb_costprice_CheckedChanged);
            // 
            // nt_post_stock_transfer
            // 
            this.nt_post_stock_transfer.Location = new System.Drawing.Point(784, 378);
            this.nt_post_stock_transfer.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.nt_post_stock_transfer.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this.nt_post_stock_transfer.Minimum = new decimal(new int[] {
            5,
            0,
            0,
            0});
            this.nt_post_stock_transfer.Name = "nt_post_stock_transfer";
            this.nt_post_stock_transfer.Size = new System.Drawing.Size(92, 26);
            this.nt_post_stock_transfer.TabIndex = 75;
            this.nt_post_stock_transfer.Value = new decimal(new int[] {
            5,
            0,
            0,
            0});
            this.nt_post_stock_transfer.ValueChanged += new System.EventHandler(this.nt_update_cashsales_intv_ValueChanged);
            // 
            // cb_stock_transfer
            // 
            this.cb_stock_transfer.AutoSize = true;
            this.cb_stock_transfer.Location = new System.Drawing.Point(458, 380);
            this.cb_stock_transfer.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.cb_stock_transfer.Name = "cb_stock_transfer";
            this.cb_stock_transfer.Size = new System.Drawing.Size(270, 24);
            this.cb_stock_transfer.TabIndex = 62;
            this.cb_stock_transfer.Text = "Post Stock Transfer Interval (min)";
            this.cb_stock_transfer.UseVisualStyleBackColor = true;
            this.cb_stock_transfer.CheckedChanged += new System.EventHandler(this.cb_updatecashsales_CheckedChanged);
            // 
            // nt_creditnote_details_intv
            // 
            this.nt_creditnote_details_intv.Location = new System.Drawing.Point(784, 55);
            this.nt_creditnote_details_intv.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.nt_creditnote_details_intv.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this.nt_creditnote_details_intv.Minimum = new decimal(new int[] {
            5,
            0,
            0,
            0});
            this.nt_creditnote_details_intv.Name = "nt_creditnote_details_intv";
            this.nt_creditnote_details_intv.Size = new System.Drawing.Size(92, 26);
            this.nt_creditnote_details_intv.TabIndex = 74;
            this.nt_creditnote_details_intv.Value = new decimal(new int[] {
            5,
            0,
            0,
            0});
            this.nt_creditnote_details_intv.ValueChanged += new System.EventHandler(this.nt_creditnote_details_intv_ValueChanged);
            // 
            // cb_creditnote_details
            // 
            this.cb_creditnote_details.AutoSize = true;
            this.cb_creditnote_details.Location = new System.Drawing.Point(458, 55);
            this.cb_creditnote_details.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.cb_creditnote_details.Name = "cb_creditnote_details";
            this.cb_creditnote_details.Size = new System.Drawing.Size(263, 24);
            this.cb_creditnote_details.TabIndex = 64;
            this.cb_creditnote_details.Text = "Credit Note Details Interval (min)";
            this.cb_creditnote_details.UseVisualStyleBackColor = true;
            this.cb_creditnote_details.CheckedChanged += new System.EventHandler(this.cb_creditnote_details_CheckedChanged);
            // 
            // lbl_updateinfo
            // 
            this.lbl_updateinfo.AutoSize = true;
            this.lbl_updateinfo.ForeColor = System.Drawing.SystemColors.Highlight;
            this.lbl_updateinfo.Location = new System.Drawing.Point(968, 322);
            this.lbl_updateinfo.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lbl_updateinfo.MaximumSize = new System.Drawing.Size(225, 231);
            this.lbl_updateinfo.Name = "lbl_updateinfo";
            this.lbl_updateinfo.Size = new System.Drawing.Size(147, 20);
            this.lbl_updateinfo.TabIndex = 66;
            this.lbl_updateinfo.Text = "Update Information";
            this.lbl_updateinfo.Visible = false;
            // 
            // btn_updatenow
            // 
            this.btn_updatenow.Location = new System.Drawing.Point(966, 354);
            this.btn_updatenow.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.btn_updatenow.Name = "btn_updatenow";
            this.btn_updatenow.Size = new System.Drawing.Size(225, 32);
            this.btn_updatenow.TabIndex = 67;
            this.btn_updatenow.Text = "Check For Updates";
            this.btn_updatenow.UseVisualStyleBackColor = true;
            this.btn_updatenow.Visible = false;
            this.btn_updatenow.Click += new System.EventHandler(this.btn_updatenow_Click);
            // 
            // nt_knockoff_intv
            // 
            this.nt_knockoff_intv.Location = new System.Drawing.Point(302, 325);
            this.nt_knockoff_intv.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.nt_knockoff_intv.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this.nt_knockoff_intv.Minimum = new decimal(new int[] {
            5,
            0,
            0,
            0});
            this.nt_knockoff_intv.Name = "nt_knockoff_intv";
            this.nt_knockoff_intv.Size = new System.Drawing.Size(92, 26);
            this.nt_knockoff_intv.TabIndex = 73;
            this.nt_knockoff_intv.Value = new decimal(new int[] {
            5,
            0,
            0,
            0});
            this.nt_knockoff_intv.ValueChanged += new System.EventHandler(this.nt_knockoff_intv_ValueChanged);
            // 
            // cb_knockoff
            // 
            this.cb_knockoff.AutoSize = true;
            this.cb_knockoff.Location = new System.Drawing.Point(10, 326);
            this.cb_knockoff.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.cb_knockoff.Name = "cb_knockoff";
            this.cb_knockoff.Size = new System.Drawing.Size(206, 24);
            this.cb_knockoff.TabIndex = 68;
            this.cb_knockoff.Text = "Ageing KO Interval (min)";
            this.cb_knockoff.UseVisualStyleBackColor = true;
            this.cb_knockoff.CheckedChanged += new System.EventHandler(this.cb_knockoff_CheckedChanged);
            // 
            // btn_run_custsync
            // 
            this.btn_run_custsync.BackColor = System.Drawing.Color.White;
            this.btn_run_custsync.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.btn_run_custsync.ForeColor = System.Drawing.Color.Transparent;
            this.btn_run_custsync.Image = ((System.Drawing.Image)(resources.GetObject("btn_run_custsync.Image")));
            this.btn_run_custsync.Location = new System.Drawing.Point(396, 0);
            this.btn_run_custsync.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.btn_run_custsync.Name = "btn_run_custsync";
            this.btn_run_custsync.Size = new System.Drawing.Size(44, 26);
            this.btn_run_custsync.TabIndex = 70;
            this.btn_run_custsync.UseVisualStyleBackColor = false;
            this.btn_run_custsync.Click += new System.EventHandler(this.btn_run_custsync_Click);
            // 
            // btn_run_transferso
            // 
            this.btn_run_transferso.BackColor = System.Drawing.Color.White;
            this.btn_run_transferso.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.btn_run_transferso.ForeColor = System.Drawing.Color.Transparent;
            this.btn_run_transferso.Image = ((System.Drawing.Image)(resources.GetObject("btn_run_transferso.Image")));
            this.btn_run_transferso.Location = new System.Drawing.Point(880, 189);
            this.btn_run_transferso.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.btn_run_transferso.Name = "btn_run_transferso";
            this.btn_run_transferso.Size = new System.Drawing.Size(44, 26);
            this.btn_run_transferso.TabIndex = 72;
            this.btn_run_transferso.UseVisualStyleBackColor = false;
            this.btn_run_transferso.Click += new System.EventHandler(this.btn_run_transferso_Click);
            // 
            // btn_run_stocksync
            // 
            this.btn_run_stocksync.BackColor = System.Drawing.Color.White;
            this.btn_run_stocksync.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.btn_run_stocksync.ForeColor = System.Drawing.Color.Transparent;
            this.btn_run_stocksync.Image = ((System.Drawing.Image)(resources.GetObject("btn_run_stocksync.Image")));
            this.btn_run_stocksync.Location = new System.Drawing.Point(396, 109);
            this.btn_run_stocksync.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.btn_run_stocksync.Name = "btn_run_stocksync";
            this.btn_run_stocksync.Size = new System.Drawing.Size(44, 26);
            this.btn_run_stocksync.TabIndex = 97;
            this.btn_run_stocksync.UseVisualStyleBackColor = false;
            this.btn_run_stocksync.Click += new System.EventHandler(this.btn_run_stocksync_Click);
            // 
            // nt_warehouse_intv
            // 
            this.nt_warehouse_intv.Location = new System.Drawing.Point(302, 352);
            this.nt_warehouse_intv.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.nt_warehouse_intv.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this.nt_warehouse_intv.Minimum = new decimal(new int[] {
            5,
            0,
            0,
            0});
            this.nt_warehouse_intv.Name = "nt_warehouse_intv";
            this.nt_warehouse_intv.Size = new System.Drawing.Size(92, 26);
            this.nt_warehouse_intv.TabIndex = 99;
            this.nt_warehouse_intv.Value = new decimal(new int[] {
            5,
            0,
            0,
            0});
            this.nt_warehouse_intv.ValueChanged += new System.EventHandler(this.nt_warehouse_intv_ValueChanged);
            // 
            // cb_warehouse
            // 
            this.cb_warehouse.AutoSize = true;
            this.cb_warehouse.Location = new System.Drawing.Point(10, 352);
            this.cb_warehouse.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.cb_warehouse.Name = "cb_warehouse";
            this.cb_warehouse.Size = new System.Drawing.Size(240, 24);
            this.cb_warehouse.TabIndex = 98;
            this.cb_warehouse.Text = "Warehouse Qty Interval (min)";
            this.cb_warehouse.UseVisualStyleBackColor = true;
            this.cb_warehouse.CheckedChanged += new System.EventHandler(this.cb_warehouse_CheckedChanged);
            // 
            // btn_run_invsync
            // 
            this.btn_run_invsync.BackColor = System.Drawing.Color.White;
            this.btn_run_invsync.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.btn_run_invsync.ForeColor = System.Drawing.Color.Transparent;
            this.btn_run_invsync.Image = ((System.Drawing.Image)(resources.GetObject("btn_run_invsync.Image")));
            this.btn_run_invsync.Location = new System.Drawing.Point(396, 243);
            this.btn_run_invsync.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.btn_run_invsync.Name = "btn_run_invsync";
            this.btn_run_invsync.Size = new System.Drawing.Size(44, 26);
            this.btn_run_invsync.TabIndex = 100;
            this.btn_run_invsync.UseVisualStyleBackColor = false;
            this.btn_run_invsync.Click += new System.EventHandler(this.btn_run_invsync_Click);
            // 
            // btn_run_stockgroupsync
            // 
            this.btn_run_stockgroupsync.BackColor = System.Drawing.Color.White;
            this.btn_run_stockgroupsync.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.btn_run_stockgroupsync.ForeColor = System.Drawing.Color.Transparent;
            this.btn_run_stockgroupsync.Image = ((System.Drawing.Image)(resources.GetObject("btn_run_stockgroupsync.Image")));
            this.btn_run_stockgroupsync.Location = new System.Drawing.Point(396, 189);
            this.btn_run_stockgroupsync.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.btn_run_stockgroupsync.Name = "btn_run_stockgroupsync";
            this.btn_run_stockgroupsync.Size = new System.Drawing.Size(44, 26);
            this.btn_run_stockgroupsync.TabIndex = 101;
            this.btn_run_stockgroupsync.UseVisualStyleBackColor = false;
            this.btn_run_stockgroupsync.Click += new System.EventHandler(this.btn_run_stockgroupsync_Click);
            // 
            // btn_run_specialpricesync
            // 
            this.btn_run_specialpricesync.BackColor = System.Drawing.Color.White;
            this.btn_run_specialpricesync.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.btn_run_specialpricesync.ForeColor = System.Drawing.Color.Transparent;
            this.btn_run_specialpricesync.Image = ((System.Drawing.Image)(resources.GetObject("btn_run_specialpricesync.Image")));
            this.btn_run_specialpricesync.Location = new System.Drawing.Point(396, 162);
            this.btn_run_specialpricesync.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.btn_run_specialpricesync.Name = "btn_run_specialpricesync";
            this.btn_run_specialpricesync.Size = new System.Drawing.Size(44, 26);
            this.btn_run_specialpricesync.TabIndex = 102;
            this.btn_run_specialpricesync.UseVisualStyleBackColor = false;
            this.btn_run_specialpricesync.Click += new System.EventHandler(this.btn_run_specialpricesync_Click);
            // 
            // btn_run_uompricesync
            // 
            this.btn_run_uompricesync.BackColor = System.Drawing.Color.White;
            this.btn_run_uompricesync.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.btn_run_uompricesync.ForeColor = System.Drawing.Color.Transparent;
            this.btn_run_uompricesync.Image = ((System.Drawing.Image)(resources.GetObject("btn_run_uompricesync.Image")));
            this.btn_run_uompricesync.Location = new System.Drawing.Point(396, 135);
            this.btn_run_uompricesync.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.btn_run_uompricesync.Name = "btn_run_uompricesync";
            this.btn_run_uompricesync.Size = new System.Drawing.Size(44, 26);
            this.btn_run_uompricesync.TabIndex = 103;
            this.btn_run_uompricesync.UseVisualStyleBackColor = false;
            this.btn_run_uompricesync.Click += new System.EventHandler(this.btn_run_uompricesync_Click);
            // 
            // btn_run_stockcatsync
            // 
            this.btn_run_stockcatsync.BackColor = System.Drawing.Color.White;
            this.btn_run_stockcatsync.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.btn_run_stockcatsync.ForeColor = System.Drawing.Color.Transparent;
            this.btn_run_stockcatsync.Image = ((System.Drawing.Image)(resources.GetObject("btn_run_stockcatsync.Image")));
            this.btn_run_stockcatsync.Location = new System.Drawing.Point(396, 82);
            this.btn_run_stockcatsync.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.btn_run_stockcatsync.Name = "btn_run_stockcatsync";
            this.btn_run_stockcatsync.Size = new System.Drawing.Size(44, 26);
            this.btn_run_stockcatsync.TabIndex = 104;
            this.btn_run_stockcatsync.UseVisualStyleBackColor = false;
            this.btn_run_stockcatsync.Click += new System.EventHandler(this.btn_run_stockcatsync_Click);
            // 
            // btn_run_branchsync
            // 
            this.btn_run_branchsync.BackColor = System.Drawing.Color.White;
            this.btn_run_branchsync.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.btn_run_branchsync.ForeColor = System.Drawing.Color.Transparent;
            this.btn_run_branchsync.Image = ((System.Drawing.Image)(resources.GetObject("btn_run_branchsync.Image")));
            this.btn_run_branchsync.Location = new System.Drawing.Point(396, 54);
            this.btn_run_branchsync.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.btn_run_branchsync.Name = "btn_run_branchsync";
            this.btn_run_branchsync.Size = new System.Drawing.Size(44, 26);
            this.btn_run_branchsync.TabIndex = 105;
            this.btn_run_branchsync.UseVisualStyleBackColor = false;
            this.btn_run_branchsync.Click += new System.EventHandler(this.btn_run_branchsync_Click);
            // 
            // btn_run_custagentsync
            // 
            this.btn_run_custagentsync.BackColor = System.Drawing.Color.White;
            this.btn_run_custagentsync.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.btn_run_custagentsync.ForeColor = System.Drawing.Color.Transparent;
            this.btn_run_custagentsync.Image = ((System.Drawing.Image)(resources.GetObject("btn_run_custagentsync.Image")));
            this.btn_run_custagentsync.Location = new System.Drawing.Point(396, 28);
            this.btn_run_custagentsync.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.btn_run_custagentsync.Name = "btn_run_custagentsync";
            this.btn_run_custagentsync.Size = new System.Drawing.Size(44, 26);
            this.btn_run_custagentsync.TabIndex = 106;
            this.btn_run_custagentsync.UseVisualStyleBackColor = false;
            this.btn_run_custagentsync.Click += new System.EventHandler(this.btn_run_custagentsync_Click);
            // 
            // btn_run_invdtlsync
            // 
            this.btn_run_invdtlsync.BackColor = System.Drawing.Color.White;
            this.btn_run_invdtlsync.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.btn_run_invdtlsync.ForeColor = System.Drawing.Color.Transparent;
            this.btn_run_invdtlsync.Image = ((System.Drawing.Image)(resources.GetObject("btn_run_invdtlsync.Image")));
            this.btn_run_invdtlsync.Location = new System.Drawing.Point(396, 269);
            this.btn_run_invdtlsync.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.btn_run_invdtlsync.Name = "btn_run_invdtlsync";
            this.btn_run_invdtlsync.Size = new System.Drawing.Size(44, 26);
            this.btn_run_invdtlsync.TabIndex = 107;
            this.btn_run_invdtlsync.UseVisualStyleBackColor = false;
            this.btn_run_invdtlsync.Click += new System.EventHandler(this.btn_run_invdtlsync_Click);
            // 
            // btn_run_stock_transfer
            // 
            this.btn_run_stock_transfer.BackColor = System.Drawing.Color.White;
            this.btn_run_stock_transfer.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.btn_run_stock_transfer.ForeColor = System.Drawing.Color.Transparent;
            this.btn_run_stock_transfer.Image = ((System.Drawing.Image)(resources.GetObject("btn_run_stock_transfer.Image")));
            this.btn_run_stock_transfer.Location = new System.Drawing.Point(880, 378);
            this.btn_run_stock_transfer.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.btn_run_stock_transfer.Name = "btn_run_stock_transfer";
            this.btn_run_stock_transfer.Size = new System.Drawing.Size(44, 26);
            this.btn_run_stock_transfer.TabIndex = 108;
            this.btn_run_stock_transfer.UseVisualStyleBackColor = false;
            this.btn_run_stock_transfer.Click += new System.EventHandler(this.btn_run_stock_transfer_Click);
            // 
            // btn_run_itemtmpdtlsync
            // 
            this.btn_run_itemtmpdtlsync.BackColor = System.Drawing.Color.White;
            this.btn_run_itemtmpdtlsync.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.btn_run_itemtmpdtlsync.ForeColor = System.Drawing.Color.Transparent;
            this.btn_run_itemtmpdtlsync.Image = ((System.Drawing.Image)(resources.GetObject("btn_run_itemtmpdtlsync.Image")));
            this.btn_run_itemtmpdtlsync.Location = new System.Drawing.Point(880, 432);
            this.btn_run_itemtmpdtlsync.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.btn_run_itemtmpdtlsync.Name = "btn_run_itemtmpdtlsync";
            this.btn_run_itemtmpdtlsync.Size = new System.Drawing.Size(44, 26);
            this.btn_run_itemtmpdtlsync.TabIndex = 109;
            this.btn_run_itemtmpdtlsync.UseVisualStyleBackColor = false;
            this.btn_run_itemtmpdtlsync.Click += new System.EventHandler(this.btn_run_itemtmpdtlsync_Click);
            // 
            // btn_run_itemtmpsync
            // 
            this.btn_run_itemtmpsync.BackColor = System.Drawing.Color.White;
            this.btn_run_itemtmpsync.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.btn_run_itemtmpsync.ForeColor = System.Drawing.Color.Transparent;
            this.btn_run_itemtmpsync.Image = ((System.Drawing.Image)(resources.GetObject("btn_run_itemtmpsync.Image")));
            this.btn_run_itemtmpsync.Location = new System.Drawing.Point(880, 405);
            this.btn_run_itemtmpsync.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.btn_run_itemtmpsync.Name = "btn_run_itemtmpsync";
            this.btn_run_itemtmpsync.Size = new System.Drawing.Size(44, 26);
            this.btn_run_itemtmpsync.TabIndex = 110;
            this.btn_run_itemtmpsync.UseVisualStyleBackColor = false;
            this.btn_run_itemtmpsync.Click += new System.EventHandler(this.btn_run_itemtmpsync_Click);
            // 
            // btn_run_imagesync
            // 
            this.btn_run_imagesync.BackColor = System.Drawing.Color.White;
            this.btn_run_imagesync.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.btn_run_imagesync.ForeColor = System.Drawing.Color.Transparent;
            this.btn_run_imagesync.Image = ((System.Drawing.Image)(resources.GetObject("btn_run_imagesync.Image")));
            this.btn_run_imagesync.Location = new System.Drawing.Point(396, 405);
            this.btn_run_imagesync.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.btn_run_imagesync.Name = "btn_run_imagesync";
            this.btn_run_imagesync.Size = new System.Drawing.Size(44, 26);
            this.btn_run_imagesync.TabIndex = 111;
            this.btn_run_imagesync.UseVisualStyleBackColor = false;
            this.btn_run_imagesync.Click += new System.EventHandler(this.btn_run_imagesync_Click);
            // 
            // btn_run_transfersalesinv
            // 
            this.btn_run_transfersalesinv.BackColor = System.Drawing.Color.White;
            this.btn_run_transfersalesinv.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.btn_run_transfersalesinv.ForeColor = System.Drawing.Color.Transparent;
            this.btn_run_transfersalesinv.Image = ((System.Drawing.Image)(resources.GetObject("btn_run_transfersalesinv.Image")));
            this.btn_run_transfersalesinv.Location = new System.Drawing.Point(880, 215);
            this.btn_run_transfersalesinv.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.btn_run_transfersalesinv.Name = "btn_run_transfersalesinv";
            this.btn_run_transfersalesinv.Size = new System.Drawing.Size(44, 26);
            this.btn_run_transfersalesinv.TabIndex = 112;
            this.btn_run_transfersalesinv.UseVisualStyleBackColor = false;
            this.btn_run_transfersalesinv.Click += new System.EventHandler(this.btn_run_transfersalesinv_Click);
            // 
            // btn_run_outsosync
            // 
            this.btn_run_outsosync.BackColor = System.Drawing.Color.White;
            this.btn_run_outsosync.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.btn_run_outsosync.ForeColor = System.Drawing.Color.Transparent;
            this.btn_run_outsosync.Image = ((System.Drawing.Image)(resources.GetObject("btn_run_outsosync.Image")));
            this.btn_run_outsosync.Location = new System.Drawing.Point(880, 162);
            this.btn_run_outsosync.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.btn_run_outsosync.Name = "btn_run_outsosync";
            this.btn_run_outsosync.Size = new System.Drawing.Size(44, 26);
            this.btn_run_outsosync.TabIndex = 113;
            this.btn_run_outsosync.UseVisualStyleBackColor = false;
            this.btn_run_outsosync.Click += new System.EventHandler(this.btn_run_outsosync_Click);
            // 
            // btn_run_dnsync
            // 
            this.btn_run_dnsync.BackColor = System.Drawing.Color.White;
            this.btn_run_dnsync.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.btn_run_dnsync.ForeColor = System.Drawing.Color.Transparent;
            this.btn_run_dnsync.Image = ((System.Drawing.Image)(resources.GetObject("btn_run_dnsync.Image")));
            this.btn_run_dnsync.Location = new System.Drawing.Point(880, 108);
            this.btn_run_dnsync.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.btn_run_dnsync.Name = "btn_run_dnsync";
            this.btn_run_dnsync.Size = new System.Drawing.Size(44, 26);
            this.btn_run_dnsync.TabIndex = 114;
            this.btn_run_dnsync.UseVisualStyleBackColor = false;
            this.btn_run_dnsync.Click += new System.EventHandler(this.btn_run_dnsync_Click);
            // 
            // btn_run_cndtlsync
            // 
            this.btn_run_cndtlsync.BackColor = System.Drawing.Color.White;
            this.btn_run_cndtlsync.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.btn_run_cndtlsync.ForeColor = System.Drawing.Color.Transparent;
            this.btn_run_cndtlsync.Image = ((System.Drawing.Image)(resources.GetObject("btn_run_cndtlsync.Image")));
            this.btn_run_cndtlsync.Location = new System.Drawing.Point(880, 54);
            this.btn_run_cndtlsync.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.btn_run_cndtlsync.Name = "btn_run_cndtlsync";
            this.btn_run_cndtlsync.Size = new System.Drawing.Size(44, 26);
            this.btn_run_cndtlsync.TabIndex = 115;
            this.btn_run_cndtlsync.UseVisualStyleBackColor = false;
            this.btn_run_cndtlsync.Click += new System.EventHandler(this.btn_run_cndtlsync_Click);
            // 
            // btn_run_cnsync
            // 
            this.btn_run_cnsync.BackColor = System.Drawing.Color.White;
            this.btn_run_cnsync.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.btn_run_cnsync.ForeColor = System.Drawing.Color.Transparent;
            this.btn_run_cnsync.Image = ((System.Drawing.Image)(resources.GetObject("btn_run_cnsync.Image")));
            this.btn_run_cnsync.Location = new System.Drawing.Point(880, 28);
            this.btn_run_cnsync.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.btn_run_cnsync.Name = "btn_run_cnsync";
            this.btn_run_cnsync.Size = new System.Drawing.Size(44, 26);
            this.btn_run_cnsync.TabIndex = 116;
            this.btn_run_cnsync.UseVisualStyleBackColor = false;
            this.btn_run_cnsync.Click += new System.EventHandler(this.btn_run_cnsync_Click);
            // 
            // btn_run_rcptsync
            // 
            this.btn_run_rcptsync.BackColor = System.Drawing.Color.White;
            this.btn_run_rcptsync.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.btn_run_rcptsync.ForeColor = System.Drawing.Color.Transparent;
            this.btn_run_rcptsync.Image = ((System.Drawing.Image)(resources.GetObject("btn_run_rcptsync.Image")));
            this.btn_run_rcptsync.Location = new System.Drawing.Point(880, 0);
            this.btn_run_rcptsync.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.btn_run_rcptsync.Name = "btn_run_rcptsync";
            this.btn_run_rcptsync.Size = new System.Drawing.Size(44, 26);
            this.btn_run_rcptsync.TabIndex = 117;
            this.btn_run_rcptsync.UseVisualStyleBackColor = false;
            this.btn_run_rcptsync.Click += new System.EventHandler(this.btn_run_rcptsync_Click);
            // 
            // btn_run_whqtysync
            // 
            this.btn_run_whqtysync.BackColor = System.Drawing.Color.White;
            this.btn_run_whqtysync.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.btn_run_whqtysync.ForeColor = System.Drawing.Color.Transparent;
            this.btn_run_whqtysync.Image = ((System.Drawing.Image)(resources.GetObject("btn_run_whqtysync.Image")));
            this.btn_run_whqtysync.Location = new System.Drawing.Point(396, 351);
            this.btn_run_whqtysync.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.btn_run_whqtysync.Name = "btn_run_whqtysync";
            this.btn_run_whqtysync.Size = new System.Drawing.Size(44, 26);
            this.btn_run_whqtysync.TabIndex = 118;
            this.btn_run_whqtysync.UseVisualStyleBackColor = false;
            this.btn_run_whqtysync.Click += new System.EventHandler(this.btn_run_whqtysync_Click);
            // 
            // btn_run_ageingkosync
            // 
            this.btn_run_ageingkosync.BackColor = System.Drawing.Color.White;
            this.btn_run_ageingkosync.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.btn_run_ageingkosync.ForeColor = System.Drawing.Color.Transparent;
            this.btn_run_ageingkosync.Image = ((System.Drawing.Image)(resources.GetObject("btn_run_ageingkosync.Image")));
            this.btn_run_ageingkosync.Location = new System.Drawing.Point(396, 325);
            this.btn_run_ageingkosync.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.btn_run_ageingkosync.Name = "btn_run_ageingkosync";
            this.btn_run_ageingkosync.Size = new System.Drawing.Size(44, 26);
            this.btn_run_ageingkosync.TabIndex = 119;
            this.btn_run_ageingkosync.UseVisualStyleBackColor = false;
            this.btn_run_ageingkosync.Click += new System.EventHandler(this.btn_run_ageingkosync_Click);
            // 
            // btn_run_costpricesync
            // 
            this.btn_run_costpricesync.BackColor = System.Drawing.Color.White;
            this.btn_run_costpricesync.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.btn_run_costpricesync.ForeColor = System.Drawing.Color.Transparent;
            this.btn_run_costpricesync.Image = ((System.Drawing.Image)(resources.GetObject("btn_run_costpricesync.Image")));
            this.btn_run_costpricesync.Location = new System.Drawing.Point(880, 463);
            this.btn_run_costpricesync.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.btn_run_costpricesync.Name = "btn_run_costpricesync";
            this.btn_run_costpricesync.Size = new System.Drawing.Size(44, 26);
            this.btn_run_costpricesync.TabIndex = 120;
            this.btn_run_costpricesync.UseVisualStyleBackColor = false;
            this.btn_run_costpricesync.Click += new System.EventHandler(this.btn_run_costpricesync_Click);
            // 
            // btn_run_transfersalescns
            // 
            this.btn_run_transfersalescns.BackColor = System.Drawing.Color.White;
            this.btn_run_transfersalescns.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.btn_run_transfersalescns.ForeColor = System.Drawing.Color.Transparent;
            this.btn_run_transfersalescns.Image = ((System.Drawing.Image)(resources.GetObject("btn_run_transfersalescns.Image")));
            this.btn_run_transfersalescns.Location = new System.Drawing.Point(880, 269);
            this.btn_run_transfersalescns.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.btn_run_transfersalescns.Name = "btn_run_transfersalescns";
            this.btn_run_transfersalescns.Size = new System.Drawing.Size(44, 26);
            this.btn_run_transfersalescns.TabIndex = 124;
            this.btn_run_transfersalescns.UseVisualStyleBackColor = false;
            this.btn_run_transfersalescns.Click += new System.EventHandler(this.btn_run_transfersalescns_Click);
            // 
            // nt_post_salescns_intv
            // 
            this.nt_post_salescns_intv.Location = new System.Drawing.Point(784, 271);
            this.nt_post_salescns_intv.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.nt_post_salescns_intv.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this.nt_post_salescns_intv.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.nt_post_salescns_intv.Name = "nt_post_salescns_intv";
            this.nt_post_salescns_intv.Size = new System.Drawing.Size(92, 26);
            this.nt_post_salescns_intv.TabIndex = 123;
            this.nt_post_salescns_intv.Value = new decimal(new int[] {
            5,
            0,
            0,
            0});
            this.nt_post_salescns_intv.ValueChanged += new System.EventHandler(this.nt_post_salescns_intv_ValueChanged);
            // 
            // cb_postsalescns
            // 
            this.cb_postsalescns.AutoSize = true;
            this.cb_postsalescns.Location = new System.Drawing.Point(458, 272);
            this.cb_postsalescns.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.cb_postsalescns.Name = "cb_postsalescns";
            this.cb_postsalescns.Size = new System.Drawing.Size(240, 24);
            this.cb_postsalescns.TabIndex = 122;
            this.cb_postsalescns.Text = "Post Sales CNs Interval (min)";
            this.cb_postsalescns.UseVisualStyleBackColor = true;
            this.cb_postsalescns.CheckedChanged += new System.EventHandler(this.cb_postsalescns_CheckedChanged);
            // 
            // btn_run_dosync
            // 
            this.btn_run_dosync.BackColor = System.Drawing.Color.White;
            this.btn_run_dosync.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.btn_run_dosync.ForeColor = System.Drawing.Color.Transparent;
            this.btn_run_dosync.Image = ((System.Drawing.Image)(resources.GetObject("btn_run_dosync.Image")));
            this.btn_run_dosync.Location = new System.Drawing.Point(396, 378);
            this.btn_run_dosync.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.btn_run_dosync.Name = "btn_run_dosync";
            this.btn_run_dosync.Size = new System.Drawing.Size(44, 26);
            this.btn_run_dosync.TabIndex = 127;
            this.btn_run_dosync.UseVisualStyleBackColor = false;
            this.btn_run_dosync.Click += new System.EventHandler(this.btn_run_dosync_Click);
            // 
            // nt_do_intv
            // 
            this.nt_do_intv.Location = new System.Drawing.Point(302, 378);
            this.nt_do_intv.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.nt_do_intv.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this.nt_do_intv.Minimum = new decimal(new int[] {
            5,
            0,
            0,
            0});
            this.nt_do_intv.Name = "nt_do_intv";
            this.nt_do_intv.Size = new System.Drawing.Size(92, 26);
            this.nt_do_intv.TabIndex = 126;
            this.nt_do_intv.Value = new decimal(new int[] {
            5,
            0,
            0,
            0});
            this.nt_do_intv.ValueChanged += new System.EventHandler(this.nt_do_intv_ValueChanged);
            // 
            // cb_do
            // 
            this.cb_do.AutoSize = true;
            this.cb_do.Location = new System.Drawing.Point(10, 380);
            this.cb_do.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.cb_do.Name = "cb_do";
            this.cb_do.Size = new System.Drawing.Size(229, 24);
            this.cb_do.TabIndex = 125;
            this.cb_do.Text = "Delivery Order Interval (min)";
            this.cb_do.UseVisualStyleBackColor = true;
            this.cb_do.CheckedChanged += new System.EventHandler(this.cb_do_CheckedChanged);
            // 
            // btn_run_transferquo
            // 
            this.btn_run_transferquo.BackColor = System.Drawing.Color.White;
            this.btn_run_transferquo.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.btn_run_transferquo.ForeColor = System.Drawing.Color.Transparent;
            this.btn_run_transferquo.Image = ((System.Drawing.Image)(resources.GetObject("btn_run_transferquo.Image")));
            this.btn_run_transferquo.Location = new System.Drawing.Point(880, 297);
            this.btn_run_transferquo.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.btn_run_transferquo.Name = "btn_run_transferquo";
            this.btn_run_transferquo.Size = new System.Drawing.Size(44, 26);
            this.btn_run_transferquo.TabIndex = 130;
            this.btn_run_transferquo.UseVisualStyleBackColor = false;
            this.btn_run_transferquo.Click += new System.EventHandler(this.btn_run_transferquo_Click);
            // 
            // nt_post_quo_intv
            // 
            this.nt_post_quo_intv.Location = new System.Drawing.Point(784, 298);
            this.nt_post_quo_intv.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.nt_post_quo_intv.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this.nt_post_quo_intv.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.nt_post_quo_intv.Name = "nt_post_quo_intv";
            this.nt_post_quo_intv.Size = new System.Drawing.Size(92, 26);
            this.nt_post_quo_intv.TabIndex = 129;
            this.nt_post_quo_intv.Value = new decimal(new int[] {
            5,
            0,
            0,
            0});
            this.nt_post_quo_intv.ValueChanged += new System.EventHandler(this.nt_post_quo_intv_ValueChanged);
            // 
            // cb_postquo
            // 
            this.cb_postquo.AutoSize = true;
            this.cb_postquo.Location = new System.Drawing.Point(458, 298);
            this.cb_postquo.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.cb_postquo.Name = "cb_postquo";
            this.cb_postquo.Size = new System.Drawing.Size(236, 24);
            this.cb_postquo.TabIndex = 128;
            this.cb_postquo.Text = "Post Quotation Interval (min)";
            this.cb_postquo.UseVisualStyleBackColor = true;
            this.cb_postquo.CheckedChanged += new System.EventHandler(this.cb_postquo_CheckedChanged);
            // 
            // btn_run_postpobasketsync
            // 
            this.btn_run_postpobasketsync.BackColor = System.Drawing.Color.White;
            this.btn_run_postpobasketsync.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.btn_run_postpobasketsync.ForeColor = System.Drawing.Color.Transparent;
            this.btn_run_postpobasketsync.Image = ((System.Drawing.Image)(resources.GetObject("btn_run_postpobasketsync.Image")));
            this.btn_run_postpobasketsync.Location = new System.Drawing.Point(880, 325);
            this.btn_run_postpobasketsync.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.btn_run_postpobasketsync.Name = "btn_run_postpobasketsync";
            this.btn_run_postpobasketsync.Size = new System.Drawing.Size(44, 26);
            this.btn_run_postpobasketsync.TabIndex = 133;
            this.btn_run_postpobasketsync.UseVisualStyleBackColor = false;
            this.btn_run_postpobasketsync.Click += new System.EventHandler(this.btn_run_postpobasketsync_Click);
            // 
            // nt_postpobasket_intv
            // 
            this.nt_postpobasket_intv.Location = new System.Drawing.Point(784, 325);
            this.nt_postpobasket_intv.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.nt_postpobasket_intv.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this.nt_postpobasket_intv.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.nt_postpobasket_intv.Name = "nt_postpobasket_intv";
            this.nt_postpobasket_intv.Size = new System.Drawing.Size(92, 26);
            this.nt_postpobasket_intv.TabIndex = 132;
            this.nt_postpobasket_intv.Value = new decimal(new int[] {
            5,
            0,
            0,
            0});
            this.nt_postpobasket_intv.ValueChanged += new System.EventHandler(this.nt_postpayment_intv_ValueChanged);
            // 
            // cb_postpobasket
            // 
            this.cb_postpobasket.AutoSize = true;
            this.cb_postpobasket.Location = new System.Drawing.Point(458, 326);
            this.cb_postpobasket.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.cb_postpobasket.Name = "cb_postpobasket";
            this.cb_postpobasket.Size = new System.Drawing.Size(242, 24);
            this.cb_postpobasket.TabIndex = 131;
            this.cb_postpobasket.Text = "Post PO Basket Interval (min)";
            this.cb_postpobasket.UseVisualStyleBackColor = true;
            this.cb_postpobasket.CheckedChanged += new System.EventHandler(this.cb_postpobasket_CheckedChanged);
            // 
            // lbl_company
            // 
            this.lbl_company.AutoSize = true;
            this.lbl_company.BackColor = System.Drawing.Color.MediumBlue;
            this.lbl_company.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lbl_company.ForeColor = System.Drawing.SystemColors.HighlightText;
            this.lbl_company.Location = new System.Drawing.Point(970, 58);
            this.lbl_company.MinimumSize = new System.Drawing.Size(10, 9);
            this.lbl_company.Name = "lbl_company";
            this.lbl_company.Size = new System.Drawing.Size(52, 22);
            this.lbl_company.TabIndex = 134;
            this.lbl_company.Text = "------";
            this.lbl_company.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // btn_run_postpaymentsync
            // 
            this.btn_run_postpaymentsync.BackColor = System.Drawing.Color.White;
            this.btn_run_postpaymentsync.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.btn_run_postpaymentsync.ForeColor = System.Drawing.Color.Transparent;
            this.btn_run_postpaymentsync.Image = ((System.Drawing.Image)(resources.GetObject("btn_run_postpaymentsync.Image")));
            this.btn_run_postpaymentsync.Location = new System.Drawing.Point(880, 351);
            this.btn_run_postpaymentsync.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.btn_run_postpaymentsync.Name = "btn_run_postpaymentsync";
            this.btn_run_postpaymentsync.Size = new System.Drawing.Size(44, 26);
            this.btn_run_postpaymentsync.TabIndex = 137;
            this.btn_run_postpaymentsync.UseVisualStyleBackColor = false;
            this.btn_run_postpaymentsync.Click += new System.EventHandler(this.btn_run_postpaymentsync_Click);
            // 
            // nt_postpayment_intv
            // 
            this.nt_postpayment_intv.Location = new System.Drawing.Point(784, 352);
            this.nt_postpayment_intv.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.nt_postpayment_intv.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this.nt_postpayment_intv.Minimum = new decimal(new int[] {
            5,
            0,
            0,
            0});
            this.nt_postpayment_intv.Name = "nt_postpayment_intv";
            this.nt_postpayment_intv.Size = new System.Drawing.Size(92, 26);
            this.nt_postpayment_intv.TabIndex = 136;
            this.nt_postpayment_intv.Value = new decimal(new int[] {
            5,
            0,
            0,
            0});
            // 
            // cb_postpayment
            // 
            this.cb_postpayment.AutoSize = true;
            this.cb_postpayment.Location = new System.Drawing.Point(458, 352);
            this.cb_postpayment.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.cb_postpayment.Name = "cb_postpayment";
            this.cb_postpayment.Size = new System.Drawing.Size(228, 24);
            this.cb_postpayment.TabIndex = 135;
            this.cb_postpayment.Text = "Post Payment Interval (min)";
            this.cb_postpayment.UseVisualStyleBackColor = true;
            this.cb_postpayment.CheckedChanged += new System.EventHandler(this.cb_postpayment_CheckedChanged);
            // 
            // btn_run_cfsync
            // 
            this.btn_run_cfsync.BackColor = System.Drawing.Color.White;
            this.btn_run_cfsync.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.btn_run_cfsync.ForeColor = System.Drawing.Color.Transparent;
            this.btn_run_cfsync.Image = ((System.Drawing.Image)(resources.GetObject("btn_run_cfsync.Image")));
            this.btn_run_cfsync.Location = new System.Drawing.Point(396, 432);
            this.btn_run_cfsync.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.btn_run_cfsync.Name = "btn_run_cfsync";
            this.btn_run_cfsync.Size = new System.Drawing.Size(44, 26);
            this.btn_run_cfsync.TabIndex = 140;
            this.btn_run_cfsync.UseVisualStyleBackColor = false;
            this.btn_run_cfsync.Click += new System.EventHandler(this.btn_run_cfsync_Click);
            // 
            // nt_cust_refund_intv
            // 
            this.nt_cust_refund_intv.Location = new System.Drawing.Point(302, 432);
            this.nt_cust_refund_intv.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.nt_cust_refund_intv.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this.nt_cust_refund_intv.Minimum = new decimal(new int[] {
            5,
            0,
            0,
            0});
            this.nt_cust_refund_intv.Name = "nt_cust_refund_intv";
            this.nt_cust_refund_intv.Size = new System.Drawing.Size(92, 26);
            this.nt_cust_refund_intv.TabIndex = 139;
            this.nt_cust_refund_intv.Value = new decimal(new int[] {
            5,
            0,
            0,
            0});
            this.nt_cust_refund_intv.ValueChanged += new System.EventHandler(this.nt_cust_refund_intv_ValueChanged);
            // 
            // cb_cust_refund
            // 
            this.cb_cust_refund.AutoSize = true;
            this.cb_cust_refund.Location = new System.Drawing.Point(10, 434);
            this.cb_cust_refund.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.cb_cust_refund.Name = "cb_cust_refund";
            this.cb_cust_refund.Size = new System.Drawing.Size(256, 24);
            this.cb_cust_refund.TabIndex = 138;
            this.cb_cust_refund.Text = "Customer Refund Interval (min)";
            this.cb_cust_refund.UseVisualStyleBackColor = true;
            this.cb_cust_refund.CheckedChanged += new System.EventHandler(this.cb_cust_refund_CheckedChanged);
            // 
            // btn_run_sosync
            // 
            this.btn_run_sosync.BackColor = System.Drawing.Color.White;
            this.btn_run_sosync.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.btn_run_sosync.ForeColor = System.Drawing.Color.Transparent;
            this.btn_run_sosync.Image = ((System.Drawing.Image)(resources.GetObject("btn_run_sosync.Image")));
            this.btn_run_sosync.Location = new System.Drawing.Point(396, 458);
            this.btn_run_sosync.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.btn_run_sosync.Name = "btn_run_sosync";
            this.btn_run_sosync.Size = new System.Drawing.Size(44, 26);
            this.btn_run_sosync.TabIndex = 143;
            this.btn_run_sosync.UseVisualStyleBackColor = false;
            this.btn_run_sosync.Click += new System.EventHandler(this.btn_run_sosync_Click);
            // 
            // nt_sosync_intv
            // 
            this.nt_sosync_intv.Location = new System.Drawing.Point(302, 460);
            this.nt_sosync_intv.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.nt_sosync_intv.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this.nt_sosync_intv.Minimum = new decimal(new int[] {
            5,
            0,
            0,
            0});
            this.nt_sosync_intv.Name = "nt_sosync_intv";
            this.nt_sosync_intv.Size = new System.Drawing.Size(92, 26);
            this.nt_sosync_intv.TabIndex = 142;
            this.nt_sosync_intv.Value = new decimal(new int[] {
            5,
            0,
            0,
            0});
            this.nt_sosync_intv.ValueChanged += new System.EventHandler(this.nt_sosync_intv_ValueChanged);
            // 
            // cb_sosync
            // 
            this.cb_sosync.AutoSize = true;
            this.cb_sosync.Location = new System.Drawing.Point(10, 462);
            this.cb_sosync.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.cb_sosync.Name = "cb_sosync";
            this.cb_sosync.Size = new System.Drawing.Size(253, 24);
            this.cb_sosync.TabIndex = 141;
            this.cb_sosync.Text = "Sales Order Sync Interval (min)";
            this.cb_sosync.UseVisualStyleBackColor = true;
            this.cb_sosync.CheckedChanged += new System.EventHandler(this.cb_sosync_CheckedChanged);
            // 
            // button_check_data
            // 
            this.button_check_data.Enabled = false;
            this.button_check_data.Location = new System.Drawing.Point(968, 288);
            this.button_check_data.Name = "button_check_data";
            this.button_check_data.Size = new System.Drawing.Size(225, 32);
            this.button_check_data.TabIndex = 144;
            this.button_check_data.Text = "Check Data";
            this.button_check_data.UseVisualStyleBackColor = true;
            this.button_check_data.Visible = false;
            this.button_check_data.Click += new System.EventHandler(this.button_check_data_Click);
            // 
            // btn_run_transfercashsales
            // 
            this.btn_run_transfercashsales.BackColor = System.Drawing.Color.White;
            this.btn_run_transfercashsales.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.btn_run_transfercashsales.ForeColor = System.Drawing.Color.Transparent;
            this.btn_run_transfercashsales.Image = ((System.Drawing.Image)(resources.GetObject("btn_run_transfercashsales.Image")));
            this.btn_run_transfercashsales.Location = new System.Drawing.Point(880, 243);
            this.btn_run_transfercashsales.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.btn_run_transfercashsales.Name = "btn_run_transfercashsales";
            this.btn_run_transfercashsales.Size = new System.Drawing.Size(44, 26);
            this.btn_run_transfercashsales.TabIndex = 147;
            this.btn_run_transfercashsales.UseVisualStyleBackColor = false;
            this.btn_run_transfercashsales.Click += new System.EventHandler(this.btn_run_transfercashsales_Click);
            // 
            // nt_post_cashsales_intv
            // 
            this.nt_post_cashsales_intv.Location = new System.Drawing.Point(784, 245);
            this.nt_post_cashsales_intv.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.nt_post_cashsales_intv.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this.nt_post_cashsales_intv.Minimum = new decimal(new int[] {
            5,
            0,
            0,
            0});
            this.nt_post_cashsales_intv.Name = "nt_post_cashsales_intv";
            this.nt_post_cashsales_intv.Size = new System.Drawing.Size(92, 26);
            this.nt_post_cashsales_intv.TabIndex = 146;
            this.nt_post_cashsales_intv.Value = new decimal(new int[] {
            5,
            0,
            0,
            0});
            this.nt_post_cashsales_intv.ValueChanged += new System.EventHandler(this.nt_post_cashsales_intv_ValueChanged);
            // 
            // cb_post_cashsales
            // 
            this.cb_post_cashsales.AutoSize = true;
            this.cb_post_cashsales.Location = new System.Drawing.Point(458, 245);
            this.cb_post_cashsales.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.cb_post_cashsales.Name = "cb_post_cashsales";
            this.cb_post_cashsales.Size = new System.Drawing.Size(247, 24);
            this.cb_post_cashsales.TabIndex = 145;
            this.cb_post_cashsales.Text = "Post Cash Sales Interval (min)";
            this.cb_post_cashsales.UseVisualStyleBackColor = true;
            this.cb_post_cashsales.CheckedChanged += new System.EventHandler(this.cb_post_cashsales_CheckedChanged);
            // 
            // btn_run_salescn
            // 
            this.btn_run_salescn.BackColor = System.Drawing.Color.White;
            this.btn_run_salescn.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.btn_run_salescn.ForeColor = System.Drawing.Color.Transparent;
            this.btn_run_salescn.Image = ((System.Drawing.Image)(resources.GetObject("btn_run_salescn.Image")));
            this.btn_run_salescn.Location = new System.Drawing.Point(880, 82);
            this.btn_run_salescn.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.btn_run_salescn.Name = "btn_run_salescn";
            this.btn_run_salescn.Size = new System.Drawing.Size(44, 26);
            this.btn_run_salescn.TabIndex = 150;
            this.btn_run_salescn.UseVisualStyleBackColor = false;
            this.btn_run_salescn.Click += new System.EventHandler(this.btn_run_salescn_Click);
            // 
            // nt_sales_cn_intv
            // 
            this.nt_sales_cn_intv.Location = new System.Drawing.Point(784, 82);
            this.nt_sales_cn_intv.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.nt_sales_cn_intv.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this.nt_sales_cn_intv.Minimum = new decimal(new int[] {
            5,
            0,
            0,
            0});
            this.nt_sales_cn_intv.Name = "nt_sales_cn_intv";
            this.nt_sales_cn_intv.Size = new System.Drawing.Size(92, 26);
            this.nt_sales_cn_intv.TabIndex = 149;
            this.nt_sales_cn_intv.Value = new decimal(new int[] {
            5,
            0,
            0,
            0});
            this.nt_sales_cn_intv.ValueChanged += new System.EventHandler(this.nt_sales_cn_intv_ValueChanged);
            // 
            // cb_sales_cn
            // 
            this.cb_sales_cn.AutoSize = true;
            this.cb_sales_cn.Location = new System.Drawing.Point(458, 83);
            this.cb_sales_cn.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.cb_sales_cn.Name = "cb_sales_cn";
            this.cb_sales_cn.Size = new System.Drawing.Size(254, 24);
            this.cb_sales_cn.TabIndex = 148;
            this.cb_sales_cn.Text = "Sales Credit Note Interval (min)";
            this.cb_sales_cn.UseVisualStyleBackColor = true;
            this.cb_sales_cn.CheckedChanged += new System.EventHandler(this.cb_sales_cn_CheckedChanged);
            // 
            // btn_run_salesdn
            // 
            this.btn_run_salesdn.BackColor = System.Drawing.Color.White;
            this.btn_run_salesdn.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.btn_run_salesdn.ForeColor = System.Drawing.Color.Transparent;
            this.btn_run_salesdn.Image = ((System.Drawing.Image)(resources.GetObject("btn_run_salesdn.Image")));
            this.btn_run_salesdn.Location = new System.Drawing.Point(880, 135);
            this.btn_run_salesdn.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.btn_run_salesdn.Name = "btn_run_salesdn";
            this.btn_run_salesdn.Size = new System.Drawing.Size(44, 26);
            this.btn_run_salesdn.TabIndex = 153;
            this.btn_run_salesdn.UseVisualStyleBackColor = false;
            this.btn_run_salesdn.Click += new System.EventHandler(this.btn_run_salesdn_Click);
            // 
            // nt_sales_dn_intv
            // 
            this.nt_sales_dn_intv.Location = new System.Drawing.Point(784, 135);
            this.nt_sales_dn_intv.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.nt_sales_dn_intv.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this.nt_sales_dn_intv.Minimum = new decimal(new int[] {
            5,
            0,
            0,
            0});
            this.nt_sales_dn_intv.Name = "nt_sales_dn_intv";
            this.nt_sales_dn_intv.Size = new System.Drawing.Size(92, 26);
            this.nt_sales_dn_intv.TabIndex = 152;
            this.nt_sales_dn_intv.Value = new decimal(new int[] {
            5,
            0,
            0,
            0});
            this.nt_sales_dn_intv.ValueChanged += new System.EventHandler(this.nt_sales_dn_intv_ValueChanged);
            // 
            // cb_sales_dn
            // 
            this.cb_sales_dn.AutoSize = true;
            this.cb_sales_dn.Location = new System.Drawing.Point(458, 137);
            this.cb_sales_dn.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.cb_sales_dn.Name = "cb_sales_dn";
            this.cb_sales_dn.Size = new System.Drawing.Size(250, 24);
            this.cb_sales_dn.TabIndex = 151;
            this.cb_sales_dn.Text = "Sales Debit Note Interval (min)";
            this.cb_sales_dn.UseVisualStyleBackColor = true;
            this.cb_sales_dn.CheckedChanged += new System.EventHandler(this.cb_sales_dn_CheckedChanged);
            // 
            // btn_run_salesinv
            // 
            this.btn_run_salesinv.BackColor = System.Drawing.Color.White;
            this.btn_run_salesinv.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.btn_run_salesinv.ForeColor = System.Drawing.Color.Transparent;
            this.btn_run_salesinv.Image = ((System.Drawing.Image)(resources.GetObject("btn_run_salesinv.Image")));
            this.btn_run_salesinv.Location = new System.Drawing.Point(396, 297);
            this.btn_run_salesinv.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.btn_run_salesinv.Name = "btn_run_salesinv";
            this.btn_run_salesinv.Size = new System.Drawing.Size(44, 26);
            this.btn_run_salesinv.TabIndex = 156;
            this.btn_run_salesinv.UseVisualStyleBackColor = false;
            this.btn_run_salesinv.Click += new System.EventHandler(this.btn_run_salesinv_Click);
            // 
            // cb_sales_inv
            // 
            this.cb_sales_inv.AutoSize = true;
            this.cb_sales_inv.Location = new System.Drawing.Point(10, 298);
            this.cb_sales_inv.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.cb_sales_inv.Name = "cb_sales_inv";
            this.cb_sales_inv.Size = new System.Drawing.Size(281, 24);
            this.cb_sales_inv.TabIndex = 154;
            this.cb_sales_inv.Text = "Sales Invoice and CS Interval (min)";
            this.cb_sales_inv.UseVisualStyleBackColor = true;
            this.cb_sales_inv.CheckedChanged += new System.EventHandler(this.cb_sales_inv_CheckedChanged);
            // 
            // nt_sales_invoice_intv
            // 
            this.nt_sales_invoice_intv.Location = new System.Drawing.Point(302, 298);
            this.nt_sales_invoice_intv.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.nt_sales_invoice_intv.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this.nt_sales_invoice_intv.Minimum = new decimal(new int[] {
            5,
            0,
            0,
            0});
            this.nt_sales_invoice_intv.Name = "nt_sales_invoice_intv";
            this.nt_sales_invoice_intv.Size = new System.Drawing.Size(92, 26);
            this.nt_sales_invoice_intv.TabIndex = 155;
            this.nt_sales_invoice_intv.Value = new decimal(new int[] {
            5,
            0,
            0,
            0});
            this.nt_sales_invoice_intv.ValueChanged += new System.EventHandler(this.nt_sales_invoice_intv_ValueChanged);
            // 
            // button_test_atc_integration
            // 
            this.button_test_atc_integration.Location = new System.Drawing.Point(966, 480);
            this.button_test_atc_integration.Name = "button_test_atc_integration";
            this.button_test_atc_integration.Size = new System.Drawing.Size(225, 32);
            this.button_test_atc_integration.TabIndex = 157;
            this.button_test_atc_integration.Text = "TEST ATC Integration";
            this.button_test_atc_integration.UseVisualStyleBackColor = true;
            this.button_test_atc_integration.Visible = false;
            this.button_test_atc_integration.Click += new System.EventHandler(this.button_test_atc_integration_Click);
            // 
            // cb_sdk_atc
            // 
            this.cb_sdk_atc.AutoSize = true;
            this.cb_sdk_atc.Location = new System.Drawing.Point(970, 425);
            this.cb_sdk_atc.Name = "cb_sdk_atc";
            this.cb_sdk_atc.Size = new System.Drawing.Size(149, 24);
            this.cb_sdk_atc.TabIndex = 158;
            this.cb_sdk_atc.Text = "AutoCount SDK";
            this.cb_sdk_atc.UseVisualStyleBackColor = true;
            this.cb_sdk_atc.Visible = false;
            this.cb_sdk_atc.CheckedChanged += new System.EventHandler(this.cb_sdk_atc_CheckedChanged);
            // 
            // btn_testalert_crash
            // 
            this.btn_testalert_crash.Location = new System.Drawing.Point(966, 388);
            this.btn_testalert_crash.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.btn_testalert_crash.Name = "btn_testalert_crash";
            this.btn_testalert_crash.Size = new System.Drawing.Size(225, 32);
            this.btn_testalert_crash.TabIndex = 159;
            this.btn_testalert_crash.Text = "Test Alert Crash Email";
            this.btn_testalert_crash.UseVisualStyleBackColor = true;
            this.btn_testalert_crash.Visible = false;
            this.btn_testalert_crash.Click += new System.EventHandler(this.btn_testalert_crash_Click);
            // 
            // btn_run_stockcardsync
            // 
            this.btn_run_stockcardsync.BackColor = System.Drawing.Color.White;
            this.btn_run_stockcardsync.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.btn_run_stockcardsync.ForeColor = System.Drawing.Color.Transparent;
            this.btn_run_stockcardsync.Image = ((System.Drawing.Image)(resources.GetObject("btn_run_stockcardsync.Image")));
            this.btn_run_stockcardsync.Location = new System.Drawing.Point(396, 215);
            this.btn_run_stockcardsync.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.btn_run_stockcardsync.Name = "btn_run_stockcardsync";
            this.btn_run_stockcardsync.Size = new System.Drawing.Size(44, 26);
            this.btn_run_stockcardsync.TabIndex = 162;
            this.btn_run_stockcardsync.UseVisualStyleBackColor = false;
            this.btn_run_stockcardsync.Click += new System.EventHandler(this.btn_run_stockcardsync_Click);
            // 
            // nt_stockcard_intv
            // 
            this.nt_stockcard_intv.Location = new System.Drawing.Point(302, 217);
            this.nt_stockcard_intv.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.nt_stockcard_intv.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this.nt_stockcard_intv.Minimum = new decimal(new int[] {
            5,
            0,
            0,
            0});
            this.nt_stockcard_intv.Name = "nt_stockcard_intv";
            this.nt_stockcard_intv.Size = new System.Drawing.Size(92, 26);
            this.nt_stockcard_intv.TabIndex = 161;
            this.nt_stockcard_intv.Value = new decimal(new int[] {
            5,
            0,
            0,
            0});
            this.nt_stockcard_intv.ValueChanged += new System.EventHandler(this.nt_stockcard_intv_ValueChanged);
            // 
            // cb_stockcard
            // 
            this.cb_stockcard.AutoSize = true;
            this.cb_stockcard.Location = new System.Drawing.Point(10, 218);
            this.cb_stockcard.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.cb_stockcard.Name = "cb_stockcard";
            this.cb_stockcard.Size = new System.Drawing.Size(209, 24);
            this.cb_stockcard.TabIndex = 160;
            this.cb_stockcard.Text = "Stock Card Interval (min)";
            this.cb_stockcard.UseVisualStyleBackColor = true;
            this.cb_stockcard.CheckedChanged += new System.EventHandler(this.cb_stockcard_CheckedChanged);
            // 
            // cb_atc_v2
            // 
            this.cb_atc_v2.AutoSize = true;
            this.cb_atc_v2.Location = new System.Drawing.Point(970, 451);
            this.cb_atc_v2.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.cb_atc_v2.Name = "cb_atc_v2";
            this.cb_atc_v2.Size = new System.Drawing.Size(140, 24);
            this.cb_atc_v2.TabIndex = 163;
            this.cb_atc_v2.Text = "Enable ATC v2";
            this.cb_atc_v2.UseVisualStyleBackColor = true;
            this.cb_atc_v2.Visible = false;
            this.cb_atc_v2.CheckedChanged += new System.EventHandler(this.cb_atc_v2_CheckedChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.ForeColor = System.Drawing.SystemColors.Desktop;
            this.label1.Location = new System.Drawing.Point(970, 19);
            this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label1.MaximumSize = new System.Drawing.Size(225, 231);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(148, 22);
            this.label1.TabIndex = 164;
            this.label1.Text = "AutoCount v2.0";
            // 
            // DashboardActivity
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1204, 914);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.cb_atc_v2);
            this.Controls.Add(this.btn_run_stockcardsync);
            this.Controls.Add(this.nt_stockcard_intv);
            this.Controls.Add(this.cb_stockcard);
            this.Controls.Add(this.btn_testalert_crash);
            this.Controls.Add(this.cb_sdk_atc);
            this.Controls.Add(this.button_test_atc_integration);
            this.Controls.Add(this.btn_run_salesinv);
            this.Controls.Add(this.cb_sales_inv);
            this.Controls.Add(this.nt_sales_invoice_intv);
            this.Controls.Add(this.btn_run_salesdn);
            this.Controls.Add(this.nt_sales_dn_intv);
            this.Controls.Add(this.cb_sales_dn);
            this.Controls.Add(this.btn_run_salescn);
            this.Controls.Add(this.nt_sales_cn_intv);
            this.Controls.Add(this.cb_sales_cn);
            this.Controls.Add(this.btn_run_transfercashsales);
            this.Controls.Add(this.nt_post_cashsales_intv);
            this.Controls.Add(this.cb_post_cashsales);
            this.Controls.Add(this.button_check_data);
            this.Controls.Add(this.btn_run_sosync);
            this.Controls.Add(this.nt_sosync_intv);
            this.Controls.Add(this.cb_sosync);
            this.Controls.Add(this.btn_run_cfsync);
            this.Controls.Add(this.nt_cust_refund_intv);
            this.Controls.Add(this.cb_cust_refund);
            this.Controls.Add(this.btn_run_postpaymentsync);
            this.Controls.Add(this.nt_postpayment_intv);
            this.Controls.Add(this.cb_postpayment);
            this.Controls.Add(this.lbl_company);
            this.Controls.Add(this.btn_run_postpobasketsync);
            this.Controls.Add(this.nt_postpobasket_intv);
            this.Controls.Add(this.cb_postpobasket);
            this.Controls.Add(this.btn_run_transferquo);
            this.Controls.Add(this.nt_post_quo_intv);
            this.Controls.Add(this.cb_postquo);
            this.Controls.Add(this.btn_run_dosync);
            this.Controls.Add(this.nt_do_intv);
            this.Controls.Add(this.cb_do);
            this.Controls.Add(this.btn_run_transfersalescns);
            this.Controls.Add(this.nt_post_salescns_intv);
            this.Controls.Add(this.cb_postsalescns);
            this.Controls.Add(this.btn_run_costpricesync);
            this.Controls.Add(this.btn_run_ageingkosync);
            this.Controls.Add(this.btn_run_whqtysync);
            this.Controls.Add(this.btn_run_rcptsync);
            this.Controls.Add(this.btn_run_cnsync);
            this.Controls.Add(this.btn_run_cndtlsync);
            this.Controls.Add(this.btn_run_dnsync);
            this.Controls.Add(this.btn_run_outsosync);
            this.Controls.Add(this.btn_run_transfersalesinv);
            this.Controls.Add(this.btn_run_imagesync);
            this.Controls.Add(this.btn_run_itemtmpsync);
            this.Controls.Add(this.btn_run_itemtmpdtlsync);
            this.Controls.Add(this.btn_run_stock_transfer);
            this.Controls.Add(this.btn_run_invdtlsync);
            this.Controls.Add(this.btn_run_custagentsync);
            this.Controls.Add(this.btn_run_branchsync);
            this.Controls.Add(this.btn_run_stockcatsync);
            this.Controls.Add(this.btn_run_uompricesync);
            this.Controls.Add(this.btn_run_specialpricesync);
            this.Controls.Add(this.btn_run_stockgroupsync);
            this.Controls.Add(this.btn_run_invsync);
            this.Controls.Add(this.nt_warehouse_intv);
            this.Controls.Add(this.cb_warehouse);
            this.Controls.Add(this.btn_run_stocksync);
            this.Controls.Add(this.btn_run_transferso);
            this.Controls.Add(this.btn_run_custsync);
            this.Controls.Add(this.nt_knockoff_intv);
            this.Controls.Add(this.cb_knockoff);
            this.Controls.Add(this.btn_updatenow);
            this.Controls.Add(this.lbl_updateinfo);
            this.Controls.Add(this.nt_creditnote_details_intv);
            this.Controls.Add(this.cb_creditnote_details);
            this.Controls.Add(this.nt_post_stock_transfer);
            this.Controls.Add(this.cb_stock_transfer);
            this.Controls.Add(this.nt_costprice_intv);
            this.Controls.Add(this.cb_costprice);
            this.Controls.Add(this.nt_productgroup_intv);
            this.Controls.Add(this.cb_productgroup);
            this.Controls.Add(this.nt_item_template_intv);
            this.Controls.Add(this.cb_item_template);
            this.Controls.Add(this.nt_item_template_dtl_intv);
            this.Controls.Add(this.cb_item_template_dtl);
            this.Controls.Add(this.nt_readimage_intv);
            this.Controls.Add(this.cb_readimage);
            this.Controls.Add(this.labelSOFTWARENAME);
            this.Controls.Add(this.nt_branch_intv);
            this.Controls.Add(this.cb_branch);
            this.Controls.Add(this.nt_productspecialprice_intv);
            this.Controls.Add(this.cb_productspecialprice);
            this.Controls.Add(this.nt_post_salesinvoices_intv);
            this.Controls.Add(this.cb_post_salesinvoice);
            this.Controls.Add(this.nt_post_salesorders_intv);
            this.Controls.Add(this.cb_post_salesorders);
            this.Controls.Add(this.nt_receipt_intv);
            this.Controls.Add(this.cb_receipt);
            this.Controls.Add(this.nt_debitnote_intv);
            this.Controls.Add(this.cb_debitnote);
            this.Controls.Add(this.nt_creditnote_intv);
            this.Controls.Add(this.cb_creditnote);
            this.Controls.Add(this.nt_customeragent_intv);
            this.Controls.Add(this.cb_customeragent);
            this.Controls.Add(this.nt_stockuomprice_intv);
            this.Controls.Add(this.cb_stockuomprice);
            this.Controls.Add(this.nt_stockcategories_intv);
            this.Controls.Add(this.cb_stockcategories);
            this.Controls.Add(this.nt_stock_intv);
            this.Controls.Add(this.cb_stock);
            this.Controls.Add(this.nt_customer_intv);
            this.Controls.Add(this.cb_customer);
            this.Controls.Add(this.cb_inv_dtl);
            this.Controls.Add(this.cb_invoice);
            this.Controls.Add(this.cb_outso);
            this.Controls.Add(this.btn_reset_setting);
            this.Controls.Add(this.nt_outso_intv);
            this.Controls.Add(this.nt_invdtl_intv);
            this.Controls.Add(this.nt_inv_intv);
            this.Controls.Add(this.cb_autoStart);
            this.Controls.Add(this.logview);
            this.Controls.Add(this.btn_terminate);
            this.Controls.Add(this.btn_run_sync);
            this.Controls.Add(this.btn_trigger_sqlaccounting);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.MaximumSize = new System.Drawing.Size(1226, 970);
            this.MinimumSize = new System.Drawing.Size(1226, 970);
            this.Name = "DashboardActivity";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.DashboardActivity_FormClosing);
            this.Load += new System.EventHandler(this.DashboardActivity_Load);
            ((System.ComponentModel.ISupportInitialize)(this.nt_inv_intv)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nt_invdtl_intv)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nt_outso_intv)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nt_customer_intv)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nt_stock_intv)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nt_stockcategories_intv)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nt_stockuomprice_intv)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nt_customeragent_intv)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nt_creditnote_intv)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nt_debitnote_intv)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nt_receipt_intv)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nt_post_salesorders_intv)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nt_post_salesinvoices_intv)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nt_productspecialprice_intv)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nt_branch_intv)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nt_readimage_intv)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nt_item_template_dtl_intv)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nt_item_template_intv)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nt_productgroup_intv)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nt_costprice_intv)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nt_post_stock_transfer)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nt_creditnote_details_intv)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nt_knockoff_intv)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nt_warehouse_intv)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nt_post_salescns_intv)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nt_do_intv)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nt_post_quo_intv)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nt_postpobasket_intv)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nt_postpayment_intv)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nt_cust_refund_intv)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nt_sosync_intv)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nt_post_cashsales_intv)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nt_sales_cn_intv)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nt_sales_dn_intv)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nt_sales_invoice_intv)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nt_stockcard_intv)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label labelSOFTWARENAME;
        private System.Windows.Forms.Button btn_trigger_sqlaccounting;
        private System.Windows.Forms.Button btn_run_sync;
        private System.Windows.Forms.Button btn_terminate;
        private System.Windows.Forms.Button btn_reset_setting;
        private System.Windows.Forms.TextBox logview;
        private System.Windows.Forms.CheckBox cb_autoStart;
        private System.Windows.Forms.CheckBox cb_outso;
        private System.Windows.Forms.CheckBox cb_invoice;
        private System.Windows.Forms.CheckBox cb_inv_dtl;
        private System.Windows.Forms.CheckBox cb_customer;
        private System.Windows.Forms.CheckBox cb_stock; 
        private System.Windows.Forms.CheckBox cb_stockcategories; 
        private System.Windows.Forms.CheckBox cb_stockuomprice;
        private System.Windows.Forms.CheckBox cb_customeragent;
        private System.Windows.Forms.CheckBox cb_creditnote;
        private System.Windows.Forms.CheckBox cb_debitnote;
        private System.Windows.Forms.CheckBox cb_receipt;
        private System.Windows.Forms.CheckBox cb_post_salesorders;
        private System.Windows.Forms.CheckBox cb_post_salesinvoice;
        private System.Windows.Forms.CheckBox cb_productspecialprice;
        private System.Windows.Forms.CheckBox cb_branch;
        private System.Windows.Forms.CheckBox cb_readimage;
        private System.Windows.Forms.CheckBox cb_item_template_dtl;
        private System.Windows.Forms.CheckBox cb_item_template;
        private System.Windows.Forms.CheckBox cb_productgroup;
        private System.Windows.Forms.CheckBox cb_costprice;
        private System.Windows.Forms.CheckBox cb_stock_transfer;
        private System.Windows.Forms.CheckBox cb_creditnote_details;
        private System.Windows.Forms.NumericUpDown nt_inv_intv;
        private System.Windows.Forms.NumericUpDown nt_invdtl_intv;
        private System.Windows.Forms.NumericUpDown nt_outso_intv;
        private System.Windows.Forms.NumericUpDown nt_customer_intv;
        private System.Windows.Forms.NumericUpDown nt_stock_intv;
        private System.Windows.Forms.NumericUpDown nt_stockcategories_intv; 
        private System.Windows.Forms.NumericUpDown nt_stockuomprice_intv; 
        private System.Windows.Forms.NumericUpDown nt_customeragent_intv;
        private System.Windows.Forms.NumericUpDown nt_creditnote_intv;
        private System.Windows.Forms.NumericUpDown nt_debitnote_intv;
        private System.Windows.Forms.NumericUpDown nt_receipt_intv;
        private System.Windows.Forms.NumericUpDown nt_post_salesorders_intv;
        private System.Windows.Forms.NumericUpDown nt_post_salesinvoices_intv;
        private System.Windows.Forms.NumericUpDown nt_productspecialprice_intv;
        private System.Windows.Forms.NumericUpDown nt_branch_intv;
        private System.Windows.Forms.NumericUpDown nt_readimage_intv;
        private System.Windows.Forms.NumericUpDown nt_item_template_dtl_intv;
        private System.Windows.Forms.NumericUpDown nt_item_template_intv;
        private System.Windows.Forms.NumericUpDown nt_productgroup_intv;
        private System.Windows.Forms.NumericUpDown nt_costprice_intv;
        private System.Windows.Forms.NumericUpDown nt_post_stock_transfer;
        private System.Windows.Forms.NumericUpDown nt_creditnote_details_intv;
        private System.Windows.Forms.Label lbl_updateinfo;
        private System.Windows.Forms.Button btn_updatenow;
        private System.Windows.Forms.NumericUpDown nt_knockoff_intv;
        private System.Windows.Forms.CheckBox cb_knockoff;
        private System.Windows.Forms.Button btn_run_custsync;
        private System.Windows.Forms.Button btn_run_transferso;
        private System.Windows.Forms.Button btn_run_stocksync;
        private System.Windows.Forms.NumericUpDown nt_warehouse_intv;
        private System.Windows.Forms.CheckBox cb_warehouse;
        private System.Windows.Forms.Button btn_run_invsync;
        private System.Windows.Forms.Button btn_run_stockgroupsync;
        private System.Windows.Forms.Button btn_run_specialpricesync;
        private System.Windows.Forms.Button btn_run_uompricesync;
        private System.Windows.Forms.Button btn_run_stockcatsync;
        private System.Windows.Forms.Button btn_run_branchsync;
        private System.Windows.Forms.Button btn_run_custagentsync;
        private System.Windows.Forms.Button btn_run_invdtlsync;
        private System.Windows.Forms.Button btn_run_stock_transfer;
        private System.Windows.Forms.Button btn_run_itemtmpdtlsync;
        private System.Windows.Forms.Button btn_run_itemtmpsync;
        private System.Windows.Forms.Button btn_run_imagesync;
        private System.Windows.Forms.Button btn_run_transfersalesinv;
        private System.Windows.Forms.Button btn_run_outsosync;
        private System.Windows.Forms.Button btn_run_dnsync;
        private System.Windows.Forms.Button btn_run_cndtlsync;
        private System.Windows.Forms.Button btn_run_cnsync;
        private System.Windows.Forms.Button btn_run_rcptsync;
        private System.Windows.Forms.Button btn_run_whqtysync;
        private System.Windows.Forms.Button btn_run_ageingkosync;
        private System.Windows.Forms.Button btn_run_costpricesync;
        private System.Windows.Forms.Button btn_run_transfersalescns;
        private System.Windows.Forms.NumericUpDown nt_post_salescns_intv;
        private System.Windows.Forms.CheckBox cb_postsalescns;
        private System.Windows.Forms.Button btn_run_dosync;
        private System.Windows.Forms.NumericUpDown nt_do_intv;
        private System.Windows.Forms.CheckBox cb_do;
        private System.Windows.Forms.Button btn_run_transferquo;
        private System.Windows.Forms.NumericUpDown nt_post_quo_intv;
        private System.Windows.Forms.CheckBox cb_postquo;
        private System.Windows.Forms.Button btn_run_postpobasketsync;
        private System.Windows.Forms.NumericUpDown nt_postpobasket_intv;
        private System.Windows.Forms.CheckBox cb_postpobasket;
        private System.Windows.Forms.Label lbl_company;
        private System.Windows.Forms.Button btn_run_postpaymentsync;
        private System.Windows.Forms.NumericUpDown nt_postpayment_intv;
        private System.Windows.Forms.CheckBox cb_postpayment;
        private System.Windows.Forms.Button btn_run_cfsync;
        private System.Windows.Forms.NumericUpDown nt_cust_refund_intv;
        private System.Windows.Forms.CheckBox cb_cust_refund;
        private System.Windows.Forms.Button btn_run_sosync;
        private System.Windows.Forms.NumericUpDown nt_sosync_intv;
        private System.Windows.Forms.CheckBox cb_sosync;
        private System.Windows.Forms.Button button_check_data;
        private System.Windows.Forms.Button btn_run_transfercashsales;
        private System.Windows.Forms.NumericUpDown nt_post_cashsales_intv;
        private System.Windows.Forms.CheckBox cb_post_cashsales;
        private System.Windows.Forms.Button btn_run_salescn;
        private System.Windows.Forms.NumericUpDown nt_sales_cn_intv;
        private System.Windows.Forms.CheckBox cb_sales_cn;
        private System.Windows.Forms.Button btn_run_salesdn;
        private System.Windows.Forms.NumericUpDown nt_sales_dn_intv;
        private System.Windows.Forms.CheckBox cb_sales_dn;
        private System.Windows.Forms.Button btn_run_salesinv;
        private System.Windows.Forms.CheckBox cb_sales_inv;
        private System.Windows.Forms.NumericUpDown nt_sales_invoice_intv;
        private System.Windows.Forms.Button button_test_atc_integration;
        private System.Windows.Forms.CheckBox cb_sdk_atc;
        private System.Windows.Forms.Button btn_testalert_crash;
        private System.Windows.Forms.Button btn_run_stockcardsync;
        private System.Windows.Forms.NumericUpDown nt_stockcard_intv;
        private System.Windows.Forms.CheckBox cb_stockcard;
        private System.Windows.Forms.CheckBox cb_atc_v2;
        private System.Windows.Forms.Label label1;
    }
}