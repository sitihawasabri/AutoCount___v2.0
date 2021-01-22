using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasySales.Object.QNE
{
    public class SalesInvoiceDetailResp
    {
            public Guid id { get; set; }
            public string numbering { get; set; }
            public string stock { get; set; }
            public string description { get; set; }
            public object note { get; set; }
            public decimal qty { get; set; }
            public string uom { get; set; }
            public double unitPrice { get; set; }
            public string discount { get; set; }
            public double amount { get; set; }
            public double amountLocal { get; set; }
            public bool? isTaxInclusive { get; set; }
            public string taxCode { get; set; }
            public double taxRate { get; set; }
            public double taxAmount { get; set; }
            public double taxAmountLocal { get; set; }
            public double subAmount { get; set; }
            public double subAmountLocal { get; set; }
            public double taxExclusiveAmount { get; set; }
            public double taxExclusiveAmountLocal { get; set; }
            public bool? isPartialTransfer { get; set; }
            public int pos { get; set; }
            public string referenceNo { get; set; }
            public double discountAmount { get; set; }
            public double discountAmountLocal { get; set; }
            public double netAmount { get; set; }
            public double netAmountLocal { get; set; }
            public string itemTypeCode { get; set; }
            public DateTime? requireDate { get; set; }
            public bool? isBundled { get; set; }
            public bool? isSubItem { get; set; }
            public decimal? transferred { get; set; } 
            public object wTaxCode { get; set; }
            public double wTaxRate { get; set; }
            public double wTaxAmount { get; set; }
            public double wTaxAmountLocal { get; set; }
            public decimal? cancelledQty { get; set; }
            public object serialNumber { get; set; }
            public object serialNumberRef1 { get; set; }
            public object serialNumberRef2 { get; set; }
            public object stockLocation { get; set; }
            public object costCentre { get; set; }
            public object project { get; set; }
            public string salesInvoice { get; set; }
        }
    }