using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using EasySales.Model;
using EasySales.Object;
using Google.Protobuf.WellKnownTypes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Org.BouncyCastle.Asn1;
using Quartz;
using Quartz.Logging;

namespace EasySales.Job
{
    [DisallowConcurrentExecution]
    public class JobATCTransferSO : IJob
    {
        private string socket_OrderId = string.Empty; //from socket transfer
        private string FormatAsRTF(string rtfString)
        {
            System.Windows.Forms.RichTextBox rtf = new System.Windows.Forms.RichTextBox();
            rtf.Text = rtfString;
            return rtf.Rtf;
        }
        public double calcDiscount(float price, float quantity, float disc1, float disc2, float disc3)
        {
            double finalPrice;

            finalPrice = 1 * price;

            finalPrice = finalPrice - (finalPrice * disc1) / 100;

            finalPrice = finalPrice - (finalPrice * disc2) / 100;

            finalPrice = finalPrice - (finalPrice * disc3) / 100;

            finalPrice = Math.Round(finalPrice, 2) * quantity;

            return finalPrice;
        }

        //public JobATCTransferSO(string socket_OrderId)
        //{
        //    this.socket_OrderId = socket_OrderId;
        //    this.Execute();
        //}
        //public JobATCTransferSO()
        //{
        //    this.Execute();
        //}

        public void ExecuteSocket(string socket_OrderId)
        {
            this.socket_OrderId = socket_OrderId;
            Execute();
        }

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
                    slog.action_identifier = Constants.Action_ATC_Transfer_SO;
                    slog.action_failure = 0;
                    slog.action_failure_message = string.Empty;
                    slog.action_time = DateTime.Now.ToString();

                    DateTime startTime = DateTime.Now;

                    LocalDB.InsertSyncLog(slog);

                    logger.message = "ATC Transfer SO is running";
                    logger.Broadcast();

                    List<DpprMySQLconfig> mysql_list = LocalDB.GetRemoteDatabaseConfig().Distinct().ToList();
                    List<DpprSQLServerconfig> mssql_list = LocalDB.GetRemoteSQLServerConfig();

                    mysql_list.Iterate<DpprMySQLconfig>((mysql_config, index) =>
                    {
                        DpprMySQLconfig mysqlconfig = mysql_list[index];
                        Database mysql = new Database();
                        mysql.Connect(index: index);

                        string targetDBname = string.Empty;
                        string order_status = "1";
                        string autoCount_sst = "false";

                        CheckBackendRule checkDB = new CheckBackendRule(mysql: mysql);
                        dynamic jsonRule = checkDB.CheckTablesExist().GetSettingByTableName("transfer_so_atc");

                        ArrayList mssql_rule = new ArrayList();
                        if (jsonRule != null)
                        {
                            foreach (var key in jsonRule)
                            {
                                if (key.mssql != null && key.mssql.GetType().ToString() == "Newtonsoft.Json.Linq.JArray")
                                {
                                    var mssql_rules = key.mssql;
                                    foreach (var db in mssql_rules)
                                    {
                                        targetDBname = db.name;
                                        order_status = db.order_status;
                                        autoCount_sst = db.autoCount_sst;
                                    }
                                }
                            }
                        }
                        else
                        {
                            throw new Exception("ATC Transfer SO sync requires backend rules");
                        }

                        string SOQuery = string.Empty;
                        //order_id = "\"SO-SS-001\",\"SO-SS-003\",\"SO-SS-002\"";
                        if (socket_OrderId != string.Empty)
                        {
                            socket_OrderId = socket_OrderId.Replace("\"", "\'");
                            //Console.WriteLine("socket_OrderId:" + socket_OrderId);
                            SOQuery = "SELECT cms_order.*, cms_login.staff_code FROM cms_order LEFT JOIN cms_login ON cms_order.salesperson_id = cms_login.login_id WHERE order_status = " + order_status + " AND cancel_status = 0 AND doc_type = 'sales' AND order_fault = 0 AND order_id IN (" + socket_OrderId + ")";
                            mysql.Message("with order id: " + SOQuery);
                        }
                        else
                        {
                            SOQuery = "SELECT cms_order.*, cms_login.staff_code FROM cms_order LEFT JOIN cms_login ON cms_order.salesperson_id = cms_login.login_id WHERE order_status = " + order_status + " AND cancel_status = 0 AND doc_type = 'sales' AND order_fault = 0";
                            mysql.Message("W/O order id: " + SOQuery);
                        }

                        //string SOQuery = "SELECT cms_order.*, cms_login.staff_code FROM cms_order LEFT JOIN cms_login ON cms_order.salesperson_id = cms_login.login_id WHERE order_status = 1 AND cancel_status = 0 AND pack_confirmed = 1 AND doc_type = 'sales' AND order_fault = 0";
                        //string SOQuery = "SELECT cms_order.*, cms_login.staff_code FROM cms_order LEFT JOIN cms_login ON cms_order.salesperson_id = cms_login.login_id WHERE order_status = 1 AND cancel_status = 0 AND doc_type = 'sales' AND order_fault = 0";
                        ArrayList queryResult = mysql.Select(SOQuery);
                        mysql.Message(SOQuery);

                        logger.Broadcast("Total orders to be transferred: " + queryResult.Count);

                        if (queryResult.Count == 0)
                        {
                            logger.message = "No orders to be transferred";
                            logger.Broadcast();
                        }
                        else
                        {
                            SQLServer mssql = new SQLServer();
                            mssql.Connect(targetDBname);

                            for (int i = 0; i < queryResult.Count; i++)
                            {
                                int fault = 0;
                                double total_tax = 0;
                                double tax_amount = 0;
                                double total_amount = 0;
                                string _total_amount;

                                Dictionary<string, string> orderObj = (Dictionary<string, string>)queryResult[i];
                                string orderId = orderObj["order_id"];

                                ArrayList totalAmt = mysql.Select("SELECT SUM(sub_total) AS sub_total FROM cms_order_item WHERE cancel_status = 0 AND isParent <> 1 AND order_id = '" + orderId + "'");

                                if(totalAmt.Count > 0)
                                {
                                    Dictionary<string, string> eachAmt = (Dictionary<string, string>)totalAmt[0];

                                    _total_amount = eachAmt["sub_total"];
                                    double.TryParse(_total_amount, out total_amount);
                                    eachAmt.Clear();
                                }
                                totalAmt.Clear();

                                tax_amount = total_amount * 0.00;
                                total_tax = total_amount + tax_amount;

                                string custId, custCode, custTel, custGroupId, areaCode, salesmanId, staffId, staffCode, orderDate, varRemarks, billingAddress1, billingAddress2, billingAddress3, billingAddress4, shippingAddress1, shippingAddress2, shippingAddress3, shippingAddress4, baskedId, custFax, custcompanyName, custInchargePerson, termCode, deliveryDate, branchCode;
                                
                                ArrayList dockeys = mssql.Select("(SELECT ISNULL(MAX(DocKey)+1,0) as dockey FROM dbo.SO WITH(SERIALIZABLE, UPDLOCK))"); //{[dockey, 61239]}
                                string dockey = string.Empty;

                                if (dockeys.Count > 0)
                                {
                                    Dictionary<string, string> dockeyObj = (Dictionary<string, string>)dockeys[0];
                                    dockey = dockeyObj["dockey"];
                                    dockeyObj.Clear();
                                }
                                else
                                {
                                    fault++;
                                }
                                dockeys.Clear();

                                Encoding utf8 = Encoding.UTF8;
                                string conv_cust_company_name = orderObj["cust_company_name"];
                                byte[] ccmBytes = utf8.GetBytes(conv_cust_company_name);
                                conv_cust_company_name = utf8.GetString(ccmBytes);

                                orderDate = Convert.ToDateTime(orderObj["order_date"]).ToString("yyyy-MM-dd");

                                custCode = orderObj["cust_code"];
                                billingAddress1 = orderObj["billing_address1"].Replace("'", "''");
                                billingAddress2 = orderObj["billing_address2"].Replace("'", "''");
                                billingAddress3 = orderObj["billing_address3"].Replace("'", "''");
                                billingAddress4 = orderObj["billing_address4"].Replace("'", "''");
                                shippingAddress1 = orderObj["shipping_address1"].Replace("'", "''");
                                shippingAddress2 = orderObj["shipping_address2"].Replace("'", "''");
                                shippingAddress3 = orderObj["shipping_address3"].Replace("'", "''");
                                shippingAddress4 = orderObj["shipping_address4"].Replace("'", "''");
                                custFax = orderObj["cust_fax"].Replace("'", "''");
                                custcompanyName = orderObj["cust_company_name"].Replace("'", "''");
                                custInchargePerson = orderObj["cust_incharge_person"];
                                termCode = orderObj["termcode"];
                                deliveryDate = Convert.ToDateTime(orderObj["delivery_date"]).ToString("yyyy-MM-dd");
                                branchCode = orderObj["branch_code"];
                                staffCode = orderObj["staff_code"];
                                custTel = orderObj["cust_tel"];

                                if (branchCode == "N/A" || branchCode == "")
                                {
                                    branchCode = "NULL";
                                }

                                string current_timestamp = string.Empty;
                                DateTime date = DateTime.Now;
                                current_timestamp = date.ToString("s");

                                string ss_sql = "INSERT INTO dbo.SO (DocKey, DocNo, DocDate, DebtorCode, DebtorName, Description, DisplayTerm, SalesAgent, InvAddr1, InvAddr2, InvAddr3, InvAddr4, Phone1, Fax1, DeliverAddr1, DeliverAddr2, DeliverAddr3, DeliverAddr4, Attention, DeliverContact, Total, Footer1Amt, Footer2Amt, Footer3Amt, CurrencyCode, CurrencyRate, NetTotal, LocalNetTotal, AnalysisNetTotal, LocalAnalysisNetTotal, Tax, LocalTax, Transferable, PrintCount, Cancelled, CanSync, LastUpdate, SalesLocation, ExTax, LocalExTax, ToTaxCurrencyRate, CalcDiscountOnUnitPrice, TotalExTax, TaxableAmt, InclusiveTax, IsRoundAdj, RoundAdj, FinalTotal, RoundingMethod, LocalTaxableAmt, TaxCurrencyTax, TaxCurrencyTaxableAmt, LastModified, LastModifiedUserID, CreatedTimeStamp, CreatedUserID, BranchCode) VALUES "; //57
                                
                                string values = string.Empty;
                                if(branchCode == "NULL")
                                {
                                    values = string.Format("('{0}', '{1}', '{2}', '{3}', {4}', '{5}', '{6}', '{7}', {8}', {9}', {10}', {11}', '{12}', '{13}', {14}', {15}', {16}', {17}', {18}', '{19}', '{20}', '{21}', '{22}', '{23}', '{24}', '{25}', '{26}', '{27}', '{28}', '{29}', '{30}', '{31}', '{32}', '{33}', '{34}', '{35}', '{36}', '{37}', '{38}', '{39}', '{40}', '{41}', '{42}', '{43}', '{44}', '{45}', '{46}', '{47}', '{48}', '{49}', '{50}', '{51}', '{52}', '{53}','{54}', '{55}', {56})", dockey, orderId, orderDate, custCode, "N'" + custcompanyName, "SALES ORDER", termCode, staffCode, "N'" + billingAddress1, "N'" + billingAddress2, "N'" + billingAddress3, "N'" + billingAddress4, custTel, custFax, "N'" + shippingAddress1, "N'" + shippingAddress2, "N'" + shippingAddress3, "N'" + shippingAddress4, "N'" + custInchargePerson, custInchargePerson, total_amount, 0, 0, 0, "MYR", 1, total_tax, total_tax, total_amount, total_amount, tax_amount, tax_amount, "T", 0, "F", "F", 0, "HQ", tax_amount, tax_amount, 1, "F", total_amount, 0, "F", "F", 0, total_tax, 4, total_amount, tax_amount, total_amount, current_timestamp, "ADMIN", current_timestamp, "ADMIN", branchCode); //57
                                }
                                else
                                {
                                    values = string.Format("('{0}', '{1}', '{2}', '{3}', {4}', '{5}', '{6}', '{7}', {8}', {9}', {10}', {11}', '{12}', '{13}', {14}', {15}', {16}', {17}', {18}', '{19}', '{20}', '{21}', '{22}', '{23}', '{24}', '{25}', '{26}', '{27}', '{28}', '{29}', '{30}', '{31}', '{32}', '{33}', '{34}', '{35}', '{36}', '{37}', '{38}', '{39}', '{40}', '{41}', '{42}', '{43}', '{44}', '{45}', '{46}', '{47}', '{48}', '{49}', '{50}', '{51}', '{52}', '{53}','{54}', '{55}', '{56}')", dockey, orderId, orderDate, custCode, "N'" + custcompanyName, "SALES ORDER", termCode, staffCode, "N'" + billingAddress1, "N'" + billingAddress2, "N'" + billingAddress3, "N'" + billingAddress4, custTel, custFax, "N'" + shippingAddress1, "N'" + shippingAddress2, "N'" + shippingAddress3, "N'" + shippingAddress4, "N'" + custInchargePerson, custInchargePerson, total_amount, 0, 0, 0, "MYR", 1, total_tax, total_tax, total_amount, total_amount, tax_amount, tax_amount, "T", 0, "F", "F", 0, "HQ", tax_amount, tax_amount, 1, "F", total_amount, 0, "F", "F", 0, total_tax, 4, total_amount, tax_amount, total_amount, current_timestamp, "ADMIN", current_timestamp, "ADMIN", branchCode); //57
                                }

                                ArrayList allOrderItems = mysql.Select("SELECT oi.order_item_id, oi.disc_1, oi.disc_2, oi.disc_3 , oi.order_id, oi.ipad_item_id, oi.product_id, oi.product_code, oi.salesperson_remark, oi.quantity, oi.editted_quantity, oi.unit_price, oi.unit_uom, oi.attribute_remark, oi.optional_remark, oi.discount_method, oi.discount_amount, oi.sub_total, oi.sequence_no, oi.uom_id, oi.packing_status, oi.packed_by, oi.cancel_status, oi.updated_at, p.*, up.product_min_price FROM cms_order_item AS oi LEFT JOIN cms_product p ON p.product_code = oi.product_code LEFT JOIN cms_product_uom_price_v2 up ON up.product_code = oi.product_code AND up.product_uom = oi.unit_uom WHERE cancel_status = 0 AND(oi.isParent = 0 OR oi.product_code <> oi.parent_code) AND  order_id = '" + orderId + "'"); /* get all items based on orderId */

                                string itemCode, uom, sub_total, priceToInsert, sequence_no;
                                float quantity, disc1, disc2, disc3, unitPrice;
                                double price_to_insert;

                                int priceSetting = 0; //maybe backend
                                HashSet<string> ValuesDtlList = new HashSet<string>();

                                string stmt_maxDelKey = "(SELECT ISNULL(MAX(DtlKey)+1,0) as maxDtlkey FROM dbo.SODTL WITH(SERIALIZABLE, UPDLOCK))";
                                ArrayList maxDelKeyList = mssql.Select(stmt_maxDelKey);
                                string maxDelKey = string.Empty;
                                if (maxDelKeyList.Count > 0)
                                {
                                    Dictionary<string, string> maxDelKeyObj = (Dictionary<string, string>)maxDelKeyList[0];
                                    maxDelKey = maxDelKeyObj["maxDtlkey"];
                                    maxDelKeyObj.Clear();
                                }
                                else
                                {
                                    fault++;
                                }
                                maxDelKeyList.Clear();

                                int.TryParse(maxDelKey, out int _maxDelKey);
                                
                                for (int idx = 0; idx < allOrderItems.Count; idx++)
                                {
                                    Dictionary<string, string> item = (Dictionary<string, string>)allOrderItems[idx];
                                    itemCode = item["product_code"];
                                    uom = item["unit_uom"];
                                    quantity = float.Parse(item["quantity"]);
                                    sequence_no = item["sequence_no"];
                                    price_to_insert = 0;
                                    //      seqeunce_no += 16;
                                    //last_seq_no = $seqeunce_no;                       ?????

                                    float.TryParse(item["unit_price"], out unitPrice);
                                    float.TryParse(item["disc_1"], out disc1);
                                    float.TryParse(item["disc_2"], out disc2);
                                    float.TryParse(item["disc_3"], out disc3);

                                    double discounted_unit = calcDiscount(unitPrice, 1, disc1, disc2, disc3);

                                    sub_total = item["sub_total"];
                                    double.TryParse(sub_total, out double subTotal);

                                    total_tax = subTotal * 0.00;

                                    if (priceSetting == 1)
                                    {
                                        price_to_insert = discounted_unit;
                                    }
                                    else
                                    {
                                        priceToInsert = item["unit_price"];
                                        double.TryParse(priceToInsert, out price_to_insert);
                                    }

                                    string conv_product_name = item["product_name"];
                                    byte[] cpnBytes = utf8.GetBytes(conv_product_name);
                                    conv_product_name = utf8.GetString(cpnBytes);
                                    item["product_name"] = conv_product_name.Replace("'", "''");

                                    //27 April 2018 - get taxcode by product code by Zack Loi///////////////////////////////////////////////////

                                    string taxType, product_code;
                                    string stmt;
                                    double TaxrateGet;

                                    taxType = "SR_S";

                                    product_code = item["product_code"].Replace("'", "''");

                                    stmt = "select taxType.taxType as ttaxtype, TaxRate from dbo.taxType left join dbo.item on taxtype.taxtype=item.Taxtype where itemcode='" + product_code + "'";
                                    ArrayList Tax = mssql.Select(stmt);

                                    if (Tax.Count > 0)
                                    {
                                        Dictionary<string, string> taxObj = (Dictionary<string, string>)Tax[0];
                                        taxType = taxObj["ttaxtype"];
                                        taxObj.Clear();
                                    }
                                    Tax.Clear();

                                    if (taxType == "SR_S")
                                    {
                                        total_tax = subTotal * 0.00;
                                        TaxrateGet = 0.0;
                                    }
                                    else
                                    {
                                        total_tax = subTotal * 0.0;
                                        TaxrateGet = 0.0;
                                    }
                                    //try to stop the loop but fail, useless loop...

                                    //reset all GST code to SR-0

                                    total_tax = subTotal * 0.0;
                                    TaxrateGet = 0.0;
                                    taxType = "NULL"; // this code must be existed in taxtype otherwise hang.....

                                    if(idx == 0)
                                    {
                                        //dont add 1
                                    }
                                    else
                                    {
                                        _maxDelKey++;
                                    }

                                    item["unit_uom"] = item["unit_uom"].Replace("'", "''");

                                    // Julfikar added 14-02-19
                                    string discount = "";
                                    if (disc1 > 0 || disc2 > 0 || disc3 > 0)
                                    {
                                        discount = item["disc_1'"] + "%/" + item["disc_2"] + "%/" + item["disc_3"] + "%";
                                        subTotal = calcDiscount(unitPrice, quantity, disc1, disc2, disc3);
                                    }

                                    //if (insertDiscount == 0) //from config.ini
                                    //{
                                    //discount = "";
                                    //}

                                    //20th feb 2019 Julfikar added (Fix of the rate issue)
                                    string stmtu = "SELECT UOM, Rate, ItemCode, Volume, Weight FROM ItemUOM where UOM = N\'" + item["unit_uom"] + "' and ItemCode = '" + product_code + "'";
                                    ArrayList itemUom = mssql.Select(stmtu);

                                    double smallest_unit_price = 0;
                                    int smallestQty = 0;
                                    int qty = 0;
                                    int udf_twkg = 0;
                                    string u_rate = "";

                                    string weight = string.Empty;
                                    string volume = string.Empty;

                                    if (itemUom.Count > 0)
                                    {
                                        Dictionary<string, string> uomList = (Dictionary<string, string>)itemUom[0];
                                        u_rate = uomList["Rate"];
                                        double.TryParse(u_rate, out double uRate);
                                        smallest_unit_price = discounted_unit / uRate;
                                        weight = uomList["Weight"];
                                        volume = uomList["Volume"];
                                        uomList.Clear();
                                    }
                                    itemUom.Clear();

                                    string salespersonRemark = item["salesperson_remark"];
                                    string furtherDescription = FormatAsRTF(salespersonRemark);
                                    //Console.WriteLine(furtherDescription);
                                    string atc_description = item["product_name"];

                                    int.TryParse(item["quantity"], out smallestQty);
                                    string orderKey = dockey; //dockey from order insert

                                    string values_dtl = string.Format("('{0}', '{1}', '{2}', '{3}', '{4}', {5}', {6}', {7}', {8}', '{9}', '{10}', '{11}', '{12}', '{13}', '{14}', '{15}', '{16}', '{17}', '{18}', '{19}', '{20}', '{21}', '{22}', '{23}', '{24}', '{25}', '{26}', '{27}', '{28}', '{29}', '{30}', '{31}', '{32}', '{33}', '{34}', '{35}')", _maxDelKey, orderKey, sequence_no, "T", product_code, "N'" + furtherDescription, "N'HQ", "N'" + atc_description, "N'" + item["unit_uom"], item["unit_uom"], item["quantity"], u_rate, smallestQty, 0, 0, smallest_unit_price, price_to_insert, 0, total_tax, sub_total, sub_total, "T", "T", "N", "T", "F", sub_total, total_tax, deliveryDate, sub_total, sub_total, TaxrateGet, sub_total, total_tax, sub_total, discount);

                                    if (fault == 0)
                                    {
                                        ValuesDtlList.Add(values_dtl);
                                        mysql.Insert("UPDATE cms_warehouse_stock SET cloud_qty = cloud_qty - " + quantity + " WHERE product_code = '" + product_code + "';");
                                    }
                                }

                                if (fault == 0)
                                {
                                    /* insert order here */
                                    string insertSO = ss_sql + string.Join(", ", values);
                                    bool insertedOrder = mssql.Insert(insertSO);
                                    mssql.Message(insertSO);
                                    if (!insertedOrder)
                                    {
                                        mssql.Message("[" + orderId + "] Order did not transfer due to false return by MSSQL");
                                        fault++;
                                        //mysql.Insert("UPDATE cms_order SET order_fault = 1, order_fault_message = 'Order did not transfer due to false return by MSSQL' WHERE order_id = '" + orderId + "'");
                                    }

                                    /* insert order detail here */
                                    string ss_sql_dtl = "INSERT INTO dbo.SODTL (DtlKey,DocKey,Seq,MainItem,ItemCode,FurtherDescription,Location,Description,UOM,UserUOM,Qty,Rate,SmallestQty,TransferedQty,FOCQty,SmallestUnitPrice,UnitPrice,DiscountAmt,Tax,SubTotal,LocalSubTotal,Transferable,PrintOut,DtlType,AddToSubTotal,StockReceived,SubTotalExTax,LocalTax,DeliveryDate,TaxableAmt,LocalSubTotalExTax,TaxRate,LocalTaxableAmt,TaxCurrencyTax,TaxCurrencyTaxableAmt,Discount) VALUES ";
                                    string insertedPicked = ss_sql_dtl + string.Join(", ", ValuesDtlList);
                                    bool insertedDtl = mssql.Insert(insertedPicked);
                                    mssql.Message(insertedPicked);
                                    if (!insertedDtl)
                                    {
                                        mssql.Message("[" + insertedPicked + "] Order dtl did not transfer due to false return by MSSQL");
                                        fault++;
                                        //mysql.Insert("UPDATE cms_order SET order_fault = 1, order_fault_message = 'Order did not transfer due to false return by MSSQL' WHERE order_id = '" + orderId + "'");
                                    }

                                    if(fault == 0)
                                    {
                                        int.TryParse(order_status, out int _order_status);
                                        int updateOrderStatus = _order_status + 1;
                                        string updateOrderStatusQuery = "INSERT INTO cms_order (order_id, order_status) VALUES ('" + orderId + "','" + updateOrderStatus + "') ON DUPLICATE KEY UPDATE order_status = VALUES(order_status)";
                                        mysql.Insert(updateOrderStatusQuery);
                                        logger.Broadcast(orderId + " created");
                                    }
                                }
                                ValuesDtlList.Clear();
                                allOrderItems.Clear();
                            }
                        }
                        queryResult.Clear();
                        mssql_rule.Clear();
                    });
                    
                    mysql_list.Clear();
                    mssql_list.Clear();

                    slog.action_identifier = Constants.Action_ATC_Transfer_SO;
                    slog.action_failure = 0;
                    slog.action_failure_message = string.Empty;
                    slog.action_time = DateTime.Now.ToString();

                    DateTime endTime = DateTime.Now;
                    TimeSpan ts = endTime - startTime;
                    //Console.WriteLine("Completed in: " + ts.TotalSeconds.ToString("F"));

                    LocalDB.InsertSyncLog(slog);

                    logger.message = "Transfer SO finished in (" + ts.TotalSeconds.ToString("F") + " seconds)";
                    logger.Broadcast();

                });

                thread.Start();
                if (socket_OrderId != string.Empty)
                {
                    thread.Join();
                }
            }
            catch (ThreadAbortException e)
            {
                DpprException ex = new DpprException
                {
                    file_name = "JobATCTransferSO",
                    exception = e.Message,
                    time = DateTime.Now.ToString()
                };
                LocalDB.InsertException(ex);

                Console.WriteLine(Constants.Thread_Exception + e.Message);
            }
        }
    }
}