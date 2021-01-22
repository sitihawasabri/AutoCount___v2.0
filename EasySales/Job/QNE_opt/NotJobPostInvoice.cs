using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
using Newtonsoft.Json;

namespace EasySales.Job
{
    public class NotJobPostInvoice
    {
        public enum Operation
        {
            SO_Related,
            Only_Invoice
        };
        public string So_Id = string.Empty;

        public string product_code = string.Empty;
        public string product_uom = string.Empty;

        public string Ref_No = string.Empty;
        private Operation operation;

        private Database database;
        private QNEApi api;
        private GlobalLogger logger;

        public NotJobPostInvoice(Operation e)
        {
            this.operation = e;
            this.database = new Database();
            this.api = new QNEApi();
            this.logger = new GlobalLogger();
        }
        public String GetQuery()
        {  
            if(this.operation == Operation.SO_Related)
            {
                if(this.So_Id == string.Empty)
                {
                    return this.So_Id;
                }
                return "SELECT cms_order.*,DATE_FORMAT(cms_order.order_date,'%d/%m/%Y %H:%s:%i') AS order_date_format, cms_login.staff_code FROM cms_order LEFT JOIN cms_login ON cms_order.salesperson_id = cms_login.login_id WHERE order_id = '"+this.So_Id+"'";                             //get so id from the postsalesorder
            }
            return "SELECT cms_order.*,DATE_FORMAT(cms_order.order_date,'%d/%m/%Y %H:%s:%i') AS order_date_format, cms_login.staff_code FROM cms_order LEFT JOIN cms_login ON cms_order.salesperson_id = cms_login.login_id WHERE order_status = 1 AND cancel_status = 0 AND packing_status = 1 AND doc_type = 'invoice' AND order_fault = 0"; //AND packing_confirmed = 1 //added 21092020 order_fault = 0 
            //waiting for QNE regarding error msg issue
        }

        public void Execute()
        {
            string invoiceQuery = this.GetQuery();
            if(invoiceQuery == string.Empty)
            {
                // there is a problem
            }
            else
            {

                dynamic jsonRule = new CheckBackendRule().CheckTablesExist().GetSettingByTableName("transfer_salesinv");

                string warehouseName = string.Empty;

                if (jsonRule != null)
                {
                    foreach (var key in jsonRule)
                    {
                        dynamic _warehouseName = key.warehouse_name;
                        warehouseName = _warehouseName;
                    }
                }

                ArrayList invoiceResult = this.database.Select(invoiceQuery);

                logger.Broadcast("Sales Invoice Query ----->" + invoiceQuery);

                if (invoiceResult.Count == 0)
                {
                    logger.message = "No Invoices to insert";
                    logger.Broadcast();
                    return;
                }
                else
                {
                    logger.message = invoiceResult.Count + " Invoices to insert";
                    logger.Broadcast();
                }

                ArrayList result = database.Select("SHOW COLUMNS FROM `cms_order` LIKE 'order_fault'");

                for (int index = 0; index < invoiceResult.Count; index++)
                {
                    try
                    {
                        Dictionary<string, string> invObj = (Dictionary<string, string>)invoiceResult[index];

                        SalesInvoicePostParams @invparams = new SalesInvoicePostParams();
                        @invparams.invoiceDate = Database.NativeDateTime(invObj["order_date_format"]);
                        @invparams.customer = invObj["cust_code"];
                        @invparams.salesPerson = invObj["staff_code"];
                        @invparams.address1 = invObj["billing_address1"];
                        @invparams.address2 = invObj["billing_address2"];
                        @invparams.address3 = invObj["billing_address3"];
                        @invparams.address4 = invObj["billing_address4"];
                        @invparams.term = invObj["termcode"];
                        @invparams.title = "INVOICE";
                        @invparams.referenceNo = invObj["order_id"];                       /* qne sales order id (reference no) =  this.Ref_No*/
                        @invparams.stockLocation = warehouseName; //both gm
                        @invparams.remark1 = invObj["order_delivery_note"]; //just added 16102020

                        ArrayList invDtlResult = this.database.Select("SELECT * FROM cms_order_item WHERE order_id = '" + invObj["order_id"] + "' AND cancel_status = 0 AND packing_status = 1 AND packed_qty <> 0");
                        //SELECT * FROM cms_order_item WHERE order_id = '" + invObj["order_id"] + "' AND cancel_status = 0 AND packing_status = 1 AND packed_qty <> 0

                        ArrayList invList = new ArrayList();

                        for (int iIdx = 0; iIdx < invDtlResult.Count; iIdx++)
                        {
                            Dictionary<string, string> invItem = (Dictionary<string, string>)invDtlResult[iIdx];

                            int packed_quantity = 0;
                            int.TryParse(invItem["packed_qty"], out packed_quantity);

                            double unit_price = 0.00;
                            double.TryParse(invItem["unit_price"], out unit_price);

                            SalesInvoiceDetailPostParams invdtlPostParams = new SalesInvoiceDetailPostParams();

                            invdtlPostParams.stock = invItem["product_code"];
                            invdtlPostParams.qty = packed_quantity;
                            invdtlPostParams.unitPrice = unit_price;
                            invdtlPostParams.description = invItem["product_name"];
                            invdtlPostParams.uom = invItem["unit_uom"];
                            invdtlPostParams.stockLocation = warehouseName; //both gm
                            invdtlPostParams.@ref = invItem["salesperson_remark"]; //just added 16102020
                            invList.Add(invdtlPostParams);

                            this.SetItemCode(invItem["product_code"]);
                            this.SetUom(invItem["unit_uom"]);

                        }

                        @invparams.Details = invList.ToArray(typeof(SalesInvoiceDetailPostParams)) as SalesInvoiceDetailPostParams[];

                        try
                        {
                            string invcode = invObj["order_id"];
                            string checkInvCode = "SalesInvoices/?$filter=referenceNo eq '" + invcode + "'"; //https://api.qne.cloud/api/Invoices?$filter=referenceNo eq '00001'&$select=invoiceCode
                            Console.WriteLine(checkInvCode);

                            dynamic checking = api.GetByName(checkInvCode); //if the inv with same referenceNo is return, dont post to api again
                            if (checking.Count > 0)
                            {
                                string invCodeInAPI = string.Empty;
                                foreach (var item in checking)
                                {
                                    Console.WriteLine(checking);
                                    invCodeInAPI = item.invoiceCode;
                                }
                                Console.WriteLine(invCodeInAPI);
                                logger.Broadcast("[" +invObj["order_id"] + "] already created in QNE. Kindly refer this Invoice No in QNE: " +invCodeInAPI+ "");
                                database.Insert("INSERT INTO cms_order (order_id, order_status, order_reference, order_fault, order_fault_message) VALUES ('" + invObj["order_id"] + "','2','" + invCodeInAPI + "', '0', 'Order already transferred to QNE') ON DUPLICATE KEY UPDATE order_status = VALUES(order_status),order_reference=VALUES(order_reference),order_fault=VALUES(order_fault), order_fault_message=VALUES(order_fault_message)"); //changed order status = 2, if possible get the invNo then insert into our order_reference column
                            }
                            else
                            {
                                SalesInvoiceResp isInserted = api.PostSalesInvoice(@invparams); //check whats the return value from API
                                
                                if(isInserted.invoiceCode != string.Empty) //if (isInserted != null)
                                {
                                    database.Insert("INSERT INTO cms_order (order_id, order_status, order_reference) VALUES ('" + invObj["order_id"] + "','2','" + isInserted.invoiceCode + "') ON DUPLICATE KEY UPDATE order_status = VALUES(order_status),order_reference=VALUES(order_reference)");
                                    logger.message = "Invoices " + isInserted.invoiceCode + " created";
                                    logger.Broadcast();
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            if (result.Count > 0)
                            {
                                if (e.Message.IndexOf("does not exist") != -1)
                                {
                                    string msg = e.Message.ReplaceAll("", "Stock '", "' does not exist!");
                                    Database.Sanitize(ref msg);
                                    string faultQuery = "UPDATE cms_order SET order_fault = '2', order_fault_message = 'Invalid Item code(" + msg + "[" + this.product_uom + "])" + "' WHERE order_id = '" + invObj["order_id"] + "'";
                                    //database.Insert("UPDATE cms_order SET order_fault = '2', order_fault_message = 'Invalid Item code(" + this.product_code + "[" + this.product_uom + "])" + "' WHERE order_id = '" + invObj["order_id"] + "'"); //original
                                    database.Insert(faultQuery); //modified
                                }
                                else if (e.Message.IndexOf("uom") != -1)
                                {
                                    database.Insert("UPDATE cms_order SET order_fault = '2', order_fault_message = 'Invalid UOM(" + this.product_code + "[" + this.product_uom + "])" + "' WHERE order_id = '" + invObj["order_id"] + "'");
                                }
                                else if (e.Message.IndexOf("duplicate") != -1)
                                {
                                    string msg = e.Message;
                                    Database.Sanitize(ref msg);
                                    //database.Insert("UPDATE cms_order SET order_fault = '3', order_fault_message = 'Order ID duplicated || " + msg + "' WHERE order_id = '" + invObj["order_id"] + "'");
                                    database.Insert("UPDATE cms_order SET order_fault = '0', order_fault_message = 'Order ID duplicated || " + msg + "' WHERE order_id = '" + invObj["order_id"] + "'");
                                }
                                else if (e.Message.IndexOf("limit") != -1)
                                {
                                    string msg = e.Message;
                                    Database.Sanitize(ref msg);
                                    database.Insert("UPDATE cms_order SET order_fault = '4', order_fault_message = 'Customer credit limit exceeded || " + msg + "' WHERE order_id = '" + invObj["order_id"] + "'");
                                }
                                else
                                {
                                    string msg = e.Message;
                                    Database.Sanitize(ref msg);

                                    //we insert the order fault msg only, so it can be transfer again to qne. but when the checking was made, the same inv number created wont be created anymore.
                                    database.Insert("UPDATE cms_order SET order_fault = '0', order_fault_message = '" + msg + "' WHERE order_id = '" + invObj["order_id"] + "'");  
                                }
                            }
                        }
                    }
                    catch (Exception exc)
                    {
                        DpprException ex = new DpprException
                        {
                            exception = exc.Message,
                            file_name = "QNEAPI + NotJobPostInvoice",
                            time = DateTime.Now.ToLongTimeString()
                        };
                        LocalDB.InsertException(ex);
                    }

                    //logger.message = "POST Sales Invoices is finished";
                    //logger.Broadcast();
                }
            }
        }

        public void SetUom(string uom)
        {
            this.product_uom = uom;
        }

        public string GetUom()
        {
            return this.product_uom;
        }

        public void SetItemCode(string itemCode)
        {
            this.product_code = itemCode;
        }

        public string GetItemCode()
        {
            return this.product_code;
        }

        public void SetSoId(string id)
        {
            this.So_Id = id;
        }

        public string GetSoId()
        {
            return this.So_Id;
        }
        
        public void SetRefNo(string refNo)
        {
            this.Ref_No = refNo;
        }

        public string GetRefNo()
        {
            return this.Ref_No;
        }
    }
}
