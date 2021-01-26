using EasySales.Object;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoCount.Invoicing.Sales;
using AutoCount.Invoicing.Sales.Invoice;

namespace EasySales
{
    public class ATChandler
    {
        public bool isV2 = false;
        private ATC_Connection connection = null;
        public ATChandler(bool isV2)
        {
            this.isV2 = isV2;
            this.connection = ATC_Configuration.Init_config();
        }

        public ATC_Connection PerformAuth()
        {
            if (isV2)
            {
                //Actually UserSession has not successfully connected to AutoCount Accounting Server.
                //It is to create a connection object.
                this.connection.userSession = AutoCountV2.PerformAuth(ref this.connection);
                GlobalLogger logger = new GlobalLogger();
                logger.Broadcast("----> this.connection.userSession: " + this.connection.userSession);

                if (this.connection.userSession != null)
                {
                    logger.Broadcast("Successfully created user session");
                }
                else
                {
                    logger.Broadcast("Failed created user session");
                }
            }
            else
            {
                this.connection.userSession = null;
                this.connection.dBSetting = AutoCountV1.PerformAuth(ref this.connection);
            }
            return this.connection;
        }

        public bool PerformAuthInAutoCount()
        {
            if (isV2)
            {
                return AutoCountV2.PerformAuthInAutoCount(this.connection);
            }
            return AutoCountV1.PerformAuthInAutoCount(this.connection);
        }

        public void Message(string msg)
        {
            if (isV2)
            {
                AutoCountV2.Message(msg);
            }
            else
            {
                AutoCountV1.Message(msg);
            }
        }

        public dynamic NewInvoice()
        {
            GlobalLogger logger = new GlobalLogger();

            logger.Broadcast("isV2: " + isV2);

            if (isV2)
            {
                try
                {
                    logger.Broadcast("this.isV2 = " + this.isV2);
                    logger.Broadcast("this.connection.userSession: " + this.connection.userSession);
                    logger.Broadcast("this.connection.userSession.DBSetting: " + this.connection.userSession.DBSetting);

                    AutoCountV2.Message("this.isV2 = " + this.isV2 + "this.connection.userSession: " + this.connection.userSession + "this.connection.userSession.DBSetting: " + this.connection.userSession.DBSetting);

                    AutoCount.Invoicing.Sales.Invoice.InvoiceCommand cmd =
            AutoCount.Invoicing.Sales.Invoice.InvoiceCommand.Create(this.connection.userSession, this.connection.userSession.DBSetting);
                    AutoCount.Invoicing.Sales.Invoice.Invoice doc = cmd.AddNew();

                    logger.Broadcast("doc = " + doc);

                    return doc;
                }
                catch (Exception ex)
                {
                    logger.Broadcast("-----> catch: " + ex.Message);
                    return null;
                }

            }
            else
            {
                try
                {
                    logger.Broadcast("this.isV2 = " + this.isV2);
                    logger.Broadcast("this.connection.dBSetting: " + this.connection.dBSetting);

                    AutoCount.Invoicing.Sales.Invoice.InvoiceCommand cmd = AutoCount.Invoicing.Sales.Invoice.InvoiceCommand.Create(connection.userSession, this.connection.dBSetting);
                    AutoCount.Invoicing.Sales.Invoice.Invoice doc = cmd.AddNew();

                    logger.Broadcast("Doc = " + doc);
                    return doc;
                }
                catch (Exception ex)
                {
                    logger.Broadcast("-----> catch: " + ex.Message);
                    return null;
                }
            }
        }

        public dynamic NewInvoiceDetails(dynamic doc)
        {
            GlobalLogger logger = new GlobalLogger();
            logger.Broadcast("----- creating doc details");
            if (isV2)
            {
                try
                {
                    AutoCount.Invoicing.Sales.Invoice.InvoiceDetail dtl;
                    dtl = doc.AddDetail();
                    logger.Broadcast("added dtl");
                    return dtl;
                }
                catch (Exception ex)
                {
                    logger.Broadcast("-----> dtl catch: " + ex.Message);
                    return null;
                }
            }
            else
            {
                try
                {
                    AutoCount.Invoicing.Sales.Invoice.InvoiceDetail dtl;
                    dtl = doc.AddDetail();
                    logger.Broadcast("bce added dtl");
                    return dtl;
                }
                catch (Exception ex)
                {
                    logger.Broadcast("-----> dtl catch: " + ex.Message);
                    return null;
                }
            }
        }

        public dynamic NewCreditNote()
        {
            GlobalLogger logger = new GlobalLogger();

            logger.Broadcast("isV2: " + isV2);

            if (isV2)
            {
                try
                {
                    logger.Broadcast("this.isV2 = " + this.isV2);
                    logger.Broadcast("this.connection.userSession: " + this.connection.userSession);
                    logger.Broadcast("this.connection.userSession.DBSetting: " + this.connection.userSession.DBSetting);

                    AutoCountV2.Message("this.isV2 = " + this.isV2 + "this.connection.userSession: " + this.connection.userSession + "this.connection.userSession.DBSetting: " + this.connection.userSession.DBSetting);

                    AutoCount.ARAP.ARCN.ARCNDataAccess cmd = AutoCount.ARAP.ARCN.ARCNDataAccess.Create(this.connection.userSession, this.connection.userSession.DBSetting);
                    AutoCount.ARAP.ARCN.ARCNEntity doc = cmd.NewARCN();

                    logger.Broadcast("doc = " + doc);

                    return doc;
                }
                catch (Exception ex)
                {
                    logger.Broadcast("-----> catch: " + ex.Message);
                    return null;
                }
            }
            return null;
        }

        public dynamic NewCreditNoteDetails(dynamic doc)
        {
            GlobalLogger logger = new GlobalLogger();
            logger.Broadcast("----- creating doc details");
            if (isV2)
            {
                try
                {
                    //AutoCount.ARAP.ARCN.ARCNDTLEntity dtl = null;
                    AutoCount.Invoicing.Sales.CreditNote.CreditNoteDetail dtl;
                    dtl = doc.AddDetail();
                    logger.Broadcast("added dtl");
                    return dtl;
                }
                catch (Exception ex)
                {
                    logger.Broadcast("-----> dtl catch: " + ex.Message);
                    return null;
                }
            }
            else
            {
                try
                {
                    AutoCount.Invoicing.Sales.Invoice.InvoiceDetail dtl;
                    dtl = doc.AddDetail();
                    logger.Broadcast("bce added dtl");
                    return dtl;
                }
                catch (Exception ex)
                {
                    logger.Broadcast("-----> dtl catch: " + ex.Message);
                    return null;
                }
            }
        }

        public dynamic NewCashSales()
        {
            GlobalLogger logger = new GlobalLogger();

            logger.Broadcast("isV2: " + isV2);

            if (isV2)
            {
                try
                {
                    logger.Broadcast("this.isV2 = " + this.isV2);
                    logger.Broadcast("this.connection.userSession: " + this.connection.userSession);
                    logger.Broadcast("this.connection.userSession.DBSetting: " + this.connection.userSession.DBSetting);
                    AutoCountV2.Message("this.isV2 = " + this.isV2 + "this.connection.userSession: " + this.connection.userSession + "this.connection.userSession.DBSetting: " + this.connection.userSession.DBSetting);

                    AutoCount.Invoicing.Sales.CashSale.CashSaleCommand cmd =
        AutoCount.Invoicing.Sales.CashSale.CashSaleCommand.Create(this.connection.userSession, this.connection.userSession.DBSetting);
                    AutoCount.Invoicing.Sales.CashSale.CashSale doc = cmd.AddNew();
                    logger.Broadcast("doc = " + doc);
                    return doc;
                }
                catch (Exception ex)
                {
                    logger.Broadcast("-----> catch: " + ex.Message);
                    return null;
                }

            }
            else
            {
                try
                {
                    logger.Broadcast("this.isV2 = " + this.isV2);
                    logger.Broadcast("this.connection.dBSetting: " + this.connection.dBSetting);
                    AutoCountV1.Message("this.isV2 = " + this.isV2 + "this.connection.userSession: " + this.connection.userSession + "this.connection.userSession.DBSetting: " + this.connection.userSession.DBSetting);
                    AutoCount.Invoicing.Sales.CashSale.CashSaleCommand cmd =
       AutoCount.Invoicing.Sales.CashSale.CashSaleCommand.Create(connection.userSession, this.connection.dBSetting);
                    AutoCount.Invoicing.Sales.CashSale.CashSale doc = cmd.AddNew();
                    logger.Broadcast("doc = " + doc);
                    return doc;
                }
                catch (Exception ex)
                {
                    logger.Broadcast("-----> catch: " + ex.Message);
                    return null;
                }
            }
        }

        public dynamic NewCashSalesDetails(dynamic doc)
        {
            GlobalLogger logger = new GlobalLogger();
            logger.Broadcast("----- creating doc details");
            if (isV2)
            {
                try
                {
                    logger.Broadcast("ATC this.isV2 = " + this.isV2);
                    AutoCount.Invoicing.Sales.CashSale.CashSaleDetail dtl;
                    logger.Broadcast("after atc");
                    dtl = doc.AddDetail();
                    logger.Broadcast("added dtl");
                    return dtl;
                }
                catch (Exception ex)
                {
                    logger.Broadcast("-----> dtl catch: " + ex.Message);
                    return null;
                }
            }
            else
            {
                try
                {
                    logger.Broadcast("BCE ATC this.isV2 = " + this.isV2);
                    AutoCount.Invoicing.Sales.CashSale.CashSaleDetail dtl;
                    logger.Broadcast("bce after atc");
                    dtl = doc.AddDetail();
                    logger.Broadcast("bce added dtl");
                    return dtl;
                }
                catch (Exception ex)
                {
                    logger.Broadcast("-----> dtl catch: " + ex.Message);
                    return null;
                }
            }
        }
    }
}
