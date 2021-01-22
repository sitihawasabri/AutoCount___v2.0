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

namespace EasySales.Job
{
    [DisallowConcurrentExecution]
    public class JobSOSync : IJob
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

                    DpprSyncLog slog = new DpprSyncLog();
                    slog.action_identifier = Constants.Action_SOSync;
                    slog.action_details = Constants.Tbl_cms_acc_existing_order + Constants.Is_Starting;
                    slog.action_failure = 0;
                    slog.action_failure_message = string.Empty;
                    slog.action_time = DateTime.Now.ToString();

                    DateTime startTime = DateTime.Now;

                    LocalDB.InsertSyncLog(slog);
                    logger.message = "----------------------------------------------------------------------------";
                    logger.Broadcast();
                    logger.message = "Sales Order sync is running";
                    logger.Broadcast();
                    logger.message = "----------------------------------------------------------------------------";
                    logger.Broadcast();

                /**
                * Here we will run SQLAccounting Codes
                * */

                CHECKAGAIN:
                    SQLAccApi instance = SQLAccApi.getInstance();

                    dynamic ComServer = instance.GetComServer();

                    if (!instance.RPCAvailable())
                    {
                        goto CHECKAGAIN;
                    }

                    dynamic lDataSet;
                    //AREA = billing_state
                    //DESCRIPTION = doctype
                    //Ext. No = in lbManager, DOCNOEX
                    string lSQL, DOCKEY, DOCNO, DOCDATE, CODE, COMPANYNAME, ADDRESS1, ADDRESS2, ADDRESS3, ADDRESS4, PHONE1, MOBILE, FAX1, AREA, AGENT, TERMS, DESCRIPTION, CANCELLED, LOCALDOCAMT, DOCREF1, BRANCHNAME, DADDRESS1, DADDRESS2, DADDRESS3, DADDRESS4, ATTENTION, NOTE;
                    string query, updateQuery;

                    HashSet<string> queryList = new HashSet<string>();

                    Database mysql = new Database();
                    Dictionary<string, string> salespersonList = new Dictionary<string, string>();
                    ArrayList salespersonFromDb = mysql.Select("SELECT login_id, staff_code FROM cms_login");

                    for (int i = 0; i < salespersonFromDb.Count; i++)
                    {
                        Dictionary<string, string> each = (Dictionary<string, string>)salespersonFromDb[i];
                        salespersonList.Add(each["staff_code"], each["login_id"]);
                        /*using key (staff_code) to get value (login_id) */
                    }
                    salespersonFromDb.Clear();

                    Dictionary<string, string> customerList = new Dictionary<string, string>();
                    ArrayList customerFromDb = mysql.Select("SELECT cust_code, cust_id FROM cms_customer");

                    for (int i = 0; i < customerFromDb.Count; i++)
                    {
                        Dictionary<string, string> each = (Dictionary<string, string>)customerFromDb[i];
                        customerList.Add(each["cust_code"], each["cust_id"]);
                    }
                    customerFromDb.Clear();

                    ArrayList soList = new ArrayList();
                    ArrayList soFromDb = mysql.Select("SELECT order_id FROM cms_order");

                    for (int i = 0; i < soFromDb.Count; i++)
                    {
                        Dictionary<string, string> each = (Dictionary<string, string>)soFromDb[i];
                        soList.Add(each["order_id"]);
                    }
                    soFromDb.Clear();

                    lSQL = "SELECT * FROM SL_SO;";
                    lDataSet = ComServer.DBManager.NewDataSet(lSQL);

                    //(order_id, order_date, delivery_date, grand_total, gst_amount, gst_tax_amount, cust_id, cust_code, cust_company_name, cust_incharge_person, cust_tel, cust_fax, billing_address1, billing_address2, billing_address3, billing_address4, billing_city, billing_state, billing_zipcode, billing_country, shipping_address1, shipping_address2, shipping_address3, shipping_address4, termcode, salesperson_id, order_status, cancel_status, order_reference, order_delivery_note, doc_type)
                    query = "INSERT INTO cms_acc_existing_order(ref_no, order_id, order_date, delivery_date, grand_total, gst_amount, gst_tax_amount, cust_id, cust_code, cust_company_name, cust_incharge_person, cust_tel, cust_fax, billing_address1, billing_address2, billing_address3, billing_address4,billing_city, billing_state, billing_zipcode, billing_country, shipping_address1, shipping_address2, shipping_address3, shipping_address4, termcode, salesperson_id, order_status, order_remark, order_reference, order_delivery_note, doc_type) VALUES ";
                    updateQuery = " ON DUPLICATE KEY UPDATE order_date = VALUES(order_date), ref_no = VALUES(ref_no),delivery_date = VALUES(delivery_date),grand_total = VALUES(grand_total),gst_amount = VALUES(gst_amount),cust_id = VALUES(cust_id),cust_code = VALUES(cust_code), cust_company_name = VALUES(cust_company_name),cust_incharge_person = VALUES(cust_incharge_person), cust_tel = VALUES(cust_tel),cust_fax = VALUES(cust_fax),billing_address1 = VALUES(billing_address1),billing_address2 = VALUES(billing_address2),billing_address3 = VALUES(billing_address3),billing_address4 = VALUES(billing_address4),billing_city = VALUES(billing_city),billing_state = VALUES(billing_state),billing_zipcode = VALUES(billing_zipcode),billing_country = VALUES(billing_country),shipping_address1 = VALUES(shipping_address1),shipping_address2 = VALUES(shipping_address2),shipping_address3 = VALUES(shipping_address3),shipping_address4 = VALUES(shipping_address4),termcode = VALUES(termcode),salesperson_id = VALUES(salesperson_id),order_status = VALUES(order_status),cancel_status = VALUES(cancel_status),order_reference = VALUES(order_reference),order_delivery_note = VALUES(order_delivery_note),doc_type = VALUES(doc_type)";
                    lDataSet.First();

                    while (!lDataSet.eof)
                    {
                        string Values = string.Empty;

                        DOCKEY = lDataSet.FindField("DOCKEY").AsString;
                        DOCNO = lDataSet.FindField("DOCNO").AsString;
                        DOCDATE = lDataSet.FindField("DOCDATE").AsString;
                        DOCDATE = Convert.ToDateTime(DOCDATE).ToString("yyyy-MM-dd");
                        CODE = lDataSet.FindField("CODE").AsString;
                        COMPANYNAME = lDataSet.FindField("COMPANYNAME").AsString;
                        ADDRESS1 = lDataSet.FindField("ADDRESS1").AsString;
                        ADDRESS2 = lDataSet.FindField("ADDRESS2").AsString;
                        ADDRESS3 = lDataSet.FindField("ADDRESS3").AsString;
                        ADDRESS4 = lDataSet.FindField("ADDRESS4").AsString;
                        PHONE1 = lDataSet.FindField("PHONE1").AsString;
                        MOBILE = lDataSet.FindField("MOBILE").AsString;
                        FAX1 = lDataSet.FindField("FAX1").AsString;
                        AREA = lDataSet.FindField("AREA").AsString;
                        AGENT = lDataSet.FindField("AGENT").AsString;

                        string _AGENTID = "0";

                        if (_AGENTID == "0")
                        {
                            AGENT = lDataSet.FindField("AGENT").AsString;
                            AGENT = AGENT.ToUpper();

                            if (string.IsNullOrEmpty(AGENT) || !salespersonList.TryGetValue(AGENT, out _AGENTID))
                            {
                                _AGENTID = "0";
                            }
                        }
                        int.TryParse(_AGENTID, out int AGENTID);

                        string _CUSTID = "0";

                        if (_CUSTID == "0")
                        {
                            CODE = lDataSet.FindField("CODE").AsString;
                            CODE = CODE.ToUpper();

                            if (string.IsNullOrEmpty(CODE) || !customerList.TryGetValue(CODE, out _CUSTID))
                            {
                                _CUSTID = "0";
                            }
                        }
                        int.TryParse(_CUSTID, out int CUSTID);

                        TERMS = lDataSet.FindField("TERMS").AsString;
                        LOCALDOCAMT = lDataSet.FindField("LOCALDOCAMT").AsString;
                        DOCREF1 = lDataSet.FindField("DOCREF1").AsString;
                        BRANCHNAME = lDataSet.FindField("BRANCHNAME").AsString;
                        DADDRESS1 = lDataSet.FindField("DADDRESS1").AsString;
                        DADDRESS2 = lDataSet.FindField("DADDRESS2").AsString;
                        DADDRESS3 = lDataSet.FindField("DADDRESS3").AsString;
                        DADDRESS4 = lDataSet.FindField("DADDRESS4").AsString;
                        
                        ATTENTION = lDataSet.FindField("ATTENTION").AsString;

                        CANCELLED = lDataSet.FindField("CANCELLED").AsString;
                        if (CANCELLED == "F")
                        {
                            CANCELLED = "0";
                        }
                        else
                        {
                            CANCELLED = "1";
                        }

                        NOTE = string.Empty;
                        DESCRIPTION = "sales";

                        Database.Sanitize(ref DOCNO);
                        Database.Sanitize(ref DOCDATE);
                        Database.Sanitize(ref DOCDATE);
                        Database.Sanitize(ref CODE);
                        Database.Sanitize(ref COMPANYNAME);
                        Database.Sanitize(ref ADDRESS1);
                        Database.Sanitize(ref ADDRESS2);
                        Database.Sanitize(ref ADDRESS3);
                        Database.Sanitize(ref ADDRESS4);
                        Database.Sanitize(ref PHONE1);
                        Database.Sanitize(ref MOBILE);
                        Database.Sanitize(ref FAX1);
                        Database.Sanitize(ref AREA);
                        Database.Sanitize(ref AGENT);
                        Database.Sanitize(ref TERMS);
                        Database.Sanitize(ref DESCRIPTION);
                        Database.Sanitize(ref CANCELLED);
                        Database.Sanitize(ref LOCALDOCAMT);
                        Database.Sanitize(ref ATTENTION);
                        Database.Sanitize(ref DOCREF1);
                        Database.Sanitize(ref BRANCHNAME);
                        Database.Sanitize(ref DADDRESS1);
                        Database.Sanitize(ref DADDRESS2);
                        Database.Sanitize(ref DADDRESS3);
                        Database.Sanitize(ref DADDRESS4);

                        if (!soList.Contains(DOCNO))
                        {
                            RecordCount++;
                            Values = string.Format("('{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}','{8}','{9}','{10}','{11}','{12}','{13}','{14}','{15}','{16}','{17}','{18}','{19}','{20}','{21}','{22}','{23}','{24}','{25}','{26}','{27}','{28}','{29}','{30}','{31}')", DOCKEY, DOCNO, DOCDATE, DOCDATE, LOCALDOCAMT, LOCALDOCAMT, 0, CUSTID, CODE, COMPANYNAME, ATTENTION, PHONE1, FAX1, ADDRESS1, ADDRESS2, ADDRESS3, ADDRESS4, "", AREA, "", "", DADDRESS1, DADDRESS2, DADDRESS3, DADDRESS4, TERMS, AGENTID, 2, CANCELLED, DOCREF1, NOTE, DESCRIPTION);
                            queryList.Add(Values);
                        }
                        
                        if (queryList.Count % 2000 == 0)
                        {
                            string tmp_query = query;
                            tmp_query += string.Join(", ", queryList);
                            tmp_query += updateQuery;

                            mysql.Insert(tmp_query);

                            queryList.Clear();
                            tmp_query = string.Empty;

                            logger.message = string.Format("{0} sales order records is inserted", RecordCount);
                            logger.Broadcast();
                        }

                        lDataSet.Next();
                    }

                    if (queryList.Count > 0)
                    {
                        query = query + string.Join(", ", queryList) + updateQuery;

                        mysql.Insert(query);

                        logger.message = string.Format("{0} sales order records is inserted", RecordCount);
                        logger.Broadcast();
                    }

                    logger.Broadcast("Sales Order sync finished");
                    logger.Broadcast("Inserting Sales Order Details..");

                    RecordCount = 0;
                    //DOCKEY, SEQ, ITEMCODE, DESCRIPTION, QTY, UOM, UNITPRICE, DISC, LOCALAMOUNT, REMARK1, 
                    dynamic lDataSetItem;
                    string itemQuery, itemUpdateQuery, lSQLItem;
                    string DTLKEY, ITEM_DOCKEY, SEQ, ITEMCODE, ITEM_DESCRIPTION, QTY, UOM, UNITPRICE, DISC, LOCALAMOUNT, REMARK1;

                    HashSet<string> itemQueryList = new HashSet<string>();

                    ArrayList soItemList = new ArrayList();
                    ArrayList soItemFromDb = mysql.Select("SELECT order_id FROM cms_order_item");

                    for (int i = 0; i < soItemFromDb.Count; i++)
                    {
                        Dictionary<string, string> each = (Dictionary<string, string>)soItemFromDb[i];
                        soItemList.Add(each["order_id"]);
                    }
                    soItemFromDb.Clear();

                    Dictionary<string, string> dockeyList = new Dictionary<string, string>();
                    ArrayList dockeyFromDb = mysql.Select("SELECT ref_no, order_id FROM cms_acc_existing_order");

                    for (int i = 0; i < dockeyFromDb.Count; i++)
                    {
                        Dictionary<string, string> each = (Dictionary<string, string>)dockeyFromDb[i];
                        dockeyList.Add(each["ref_no"], each["order_id"]);
                        /*using key (ref_no) to get value (order_id) */
                    }
                    dockeyFromDb.Clear();

                    Dictionary<string, string> productList = new Dictionary<string, string>();
                    ArrayList productFromDb = mysql.Select("SELECT product_id, product_code FROM cms_product");

                    for (int i = 0; i < productFromDb.Count; i++)
                    {
                        Dictionary<string, string> each = (Dictionary<string, string>)productFromDb[i];
                        productList.Add(each["product_code"], each["product_id"]);
                        /*using key (product_code) to get value (product_id) */
                    }
                    productFromDb.Clear();

                    lSQLItem = "SELECT * FROM SL_SODTL;";
                    lDataSetItem = ComServer.DBManager.NewDataSet(lSQLItem);
                    //ORDER_ID, ITEMCODE, ITEM_ID, ITEM_DESCRIPTION, REMARK1, QTY, UNITPRICE, DISC, UOM, LOCALAMOUNT, SEQ, 2, 0
                    itemQuery = "INSERT INTO cms_acc_existing_order_item(order_id, ipad_item_id, product_code, product_id, product_name, salesperson_remark, quantity, unit_price, disc_1, unit_uom, sub_total, sequence_no, order_item_validity, cancel_status) VALUES ";
                    itemUpdateQuery = " ON DUPLICATE KEY UPDATE order_id = VALUES(order_id), ipad_item_id = VALUES(ipad_item_id), product_code = VALUES(product_code), product_id = VALUES(product_id), product_name = VALUES(product_name), salesperson_remark = VALUES(salesperson_remark), quantity = VALUES(quantity), unit_price = VALUES(unit_price), disc_1 = VALUES(disc_1), unit_uom = VALUES(unit_uom), sub_total = VALUES(sub_total), sequence_no = VALUES(sequence_no), order_item_validity= VALUES(order_item_validity), cancel_status = VALUES(cancel_status)";

                    lDataSetItem.First();

                    while (!lDataSetItem.eof)
                    {
                        string Values = string.Empty;

                        DTLKEY = lDataSetItem.FindField("DTLKEY").AsString;
                        ITEM_DOCKEY = lDataSetItem.FindField("DOCKEY").AsString;

                        string ORDER_ID = "0";

                        if (ORDER_ID == "0")
                        {
                            ITEM_DOCKEY = lDataSetItem.FindField("DOCKEY").AsString;
                            ITEM_DOCKEY = ITEM_DOCKEY.ToUpper();

                            if (string.IsNullOrEmpty(ITEM_DOCKEY) || !dockeyList.TryGetValue(ITEM_DOCKEY, out ORDER_ID))
                            {
                                ORDER_ID = "0";
                            }
                        }

                        SEQ = lDataSetItem.FindField("SEQ").AsString;
                        ITEMCODE = lDataSetItem.FindField("ITEMCODE").AsString;

                        string ITEM_ID = "0";

                        if (ITEM_ID == "0")
                        {
                            ITEMCODE = lDataSetItem.FindField("ITEMCODE").AsString;
                            if (ITEMCODE != null)
                            {
                                ITEMCODE = ITEMCODE.ToUpper();

                                if (string.IsNullOrEmpty(ITEMCODE) || !productList.TryGetValue(ITEMCODE, out ITEM_ID))
                                {
                                    ITEM_ID = "0";
                                }
                            }
                            else
                            {
                                ITEMCODE = "";
                                ITEM_ID = "";
                            }
                        }

                        ITEM_DESCRIPTION = lDataSetItem.FindField("DESCRIPTION").AsString;
                        QTY = lDataSetItem.FindField("QTY").AsString;
                        UOM = lDataSetItem.FindField("UOM").AsString;
                        UNITPRICE = lDataSetItem.FindField("UNITPRICE").AsString;
                        DISC = lDataSetItem.FindField("DISC").AsString;
                        LOCALAMOUNT = lDataSetItem.FindField("LOCALAMOUNT").AsString;
                        REMARK1 = lDataSetItem.FindField("REMARK1").AsString;

                        Database.Sanitize(ref ITEM_DOCKEY);
                        Database.Sanitize(ref SEQ);
                        Database.Sanitize(ref ITEMCODE);
                        Database.Sanitize(ref ITEM_DESCRIPTION);
                        Database.Sanitize(ref QTY);
                        Database.Sanitize(ref UOM);
                        Database.Sanitize(ref UNITPRICE);
                        Database.Sanitize(ref DISC);
                        Database.Sanitize(ref LOCALAMOUNT);
                        Database.Sanitize(ref REMARK1);

                        if (!soItemList.Contains(ORDER_ID) && ORDER_ID != "0")
                        {
                            RecordCount++;
                            //(order_id, product_code, product_id, product_name, salesperson_remark, quantity, unit_price, disc_1, unit_uom, discount_method, sub_total, sequence_no, order_item_validity, cancel_status)
                            Values = string.Format("('{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}','{8}','{9}','{10}','{11}','{12}','{13}')", ORDER_ID, DTLKEY, ITEMCODE, ITEM_ID, ITEM_DESCRIPTION, REMARK1, QTY, UNITPRICE, DISC, UOM, LOCALAMOUNT, SEQ, 2, 0);
                            //ITEM_DOCKEY, SEQ, ITEMCODE, ITEM_DESCRIPTION, QTY, UOM, UNITPRICE, DISC, LOCALAMOUNT, REMARK1
                            itemQueryList.Add(Values);
                        }

                        if (itemQueryList.Count % 2000 == 0)
                        {
                            string tmp_query = itemQuery;
                            tmp_query += string.Join(", ", itemQueryList);
                            tmp_query += itemUpdateQuery;

                            mysql.Insert(tmp_query);

                            itemQueryList.Clear();
                            tmp_query = string.Empty;

                            logger.message = string.Format("{0} sales order details records is inserted", RecordCount);
                            logger.Broadcast();
                        }

                        lDataSetItem.Next();
                    }

                    if (itemQueryList.Count > 0)
                    {
                        itemQuery = itemQuery + string.Join(", ", itemQueryList) + itemUpdateQuery;

                        mysql.Insert(itemQuery);

                        logger.message = string.Format("{0} sales order details records is inserted", RecordCount);
                        logger.Broadcast();
                    }

                    slog.action_identifier = Constants.Action_SOSync;
                    slog.action_details = Constants.Tbl_cms_acc_existing_order + Constants.Is_Finished + string.Format(" ({0}) records", RecordCount);
                    slog.action_failure = 0;
                    slog.action_failure_message = string.Empty;
                    slog.action_time = DateTime.Now.ToString();

                    DateTime endTime = DateTime.Now;
                    TimeSpan ts = endTime - startTime;
                    Console.WriteLine("Completed in: " + ts.TotalSeconds.ToString("F"));

                    LocalDB.InsertSyncLog(slog);

                    logger.message = "All sales order sync finished in (" + ts.TotalSeconds.ToString("F") + " seconds)";
                    logger.Broadcast();
                });

                thread.Start();
                //thread.Join();
            }
            catch (ThreadAbortException e)
            {
                DpprException ex = new DpprException
                {
                    file_name = "JobSOSync",
                    exception = e.Message,
                    time = DateTime.Now.ToString()
                };
                LocalDB.InsertException(ex);

                Console.WriteLine(Constants.Thread_Exception + e.Message);
            }
        }
    }
}