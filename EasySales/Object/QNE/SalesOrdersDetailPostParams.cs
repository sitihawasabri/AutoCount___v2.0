using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasySales.Object.QNE
{
    public class SalesOrdersDetailPostParams
    {
        public string Numbering { get; set; }
        public string Stock { get; set; }
        public string Description { get; set; }
        public string Note { get; set; }
        public string Uom { get; set; }
        public decimal Qty { get; set; } //int
        public string TaxCode { get; set; }
        public bool? IsTaxInclusive { get; set; }
        public double UnitPrice { get; set; } //int
        public string Discount { get; set; }
        public string ReferenceNo { get; set; }
        public string WTaxCode { get; set; }
        public string StockLocation { get; set; }
        public string Project { get; set; }
        public string CostCentre { get; set; }
        public string @Ref { get; set; }
        public string Ref2 { get; set; }
        public string Ref3 { get; set; }
        public string Ref4 { get; set; }
        public string Ref5 { get; set; }
        public DateTime? DateRef1 { get; set; }
        public DateTime? DateRef2 { get; set; }
        public int NumRef1 { get; set; }
        public int NumRef2 { get; set; }
        public int Pos { get; set; }
    }
}
