using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasySales.Object.QNE
{
    public class SalesOrdersDetailResp
    {
        public Guid Id { get; set; }
        public string Numbering { get; set; }
        public string Stock { get; set; }
        public string Description { get; set; }
        public object Note { get; set; }
        public decimal Qty { get; set; } //int
        public string Uom { get; set; }
        public double UnitPrice { get; set; } //int
        public string Discount { get; set; }
        public double Amount { get; set; }
        public double AmountLocal { get; set; }
        public bool IsTaxInclusive { get; set; }
        public object TaxCode { get; set; }
        public double TaxRate { get; set; }
        public double TaxAmount { get; set; }
        public double TaxAmountLocal { get; set; }
        public double SubAmount { get; set; }
        public double SubAmountLocal { get; set; }
        public double TaxExclusiveAmount { get; set; }
        public double TaxExclusiveAmountLocal { get; set; }
        public bool? IsPartialTransfer { get; set; }
        public int Pos { get; set; }
        public string ReferenceNo { get; set; }
        public double DiscountAmount { get; set; }
        public double DiscountAmountLocal { get; set; }
        public double NetAmount { get; set; }
        public double NetAmountLocal { get; set; }
        public object ItemTypeCode { get; set; }
        public string RequireDate { get; set; }
        public bool IsBundled { get; set; }
        public bool IsSubItem { get; set; }
        public decimal Transferred { get; set; } //int
        public object WTaxCode { get; set; } //
        public double WTaxRate { get; set; }
        public double WTaxAmount { get; set; }
        public double WTaxAmountLocal { get; set; }
        public decimal CancelledQty { get; set; } //int
        public object SerialNumber { get; set; }
        public object SerialNumberRef1 { get; set; }
        public object SerialNumberRef2 { get; set; }
        public object StockLocation { get; set; }
        public object CostCentre { get; set; }
        public object Project { get; set; }
        public string SalesOrder { get; set; }
        public SalesOrdersDetailTransferFrom TransferFrom { get; set; }
    }
}
