using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasySales.Object.QNE
{
    public class SalesCNsDetailPostParams
    {
        public string numbering { get; set; }
        public string stock { get; set; }
        public string description { get; set; }
        public string note { get; set; }
        public string uom { get; set; }
        public decimal qty { get; set; }
        public string taxCode { get; set; }
        public bool isTaxInclusive { get; set; }
        public double unitPrice { get; set; }
        public string discount { get; set; }
        public string referenceNo { get; set; }
        public string wTaxCode { get; set; }
        public string stockLocation { get; set; }
        public string project { get; set; }
        public string costCentre { get; set; }
        public string ref1 { get; set; }
        public string ref2 { get; set; }
        public string ref3 { get; set; }
        public string ref4 { get; set; }
        public string ref5 { get; set; }
        public DateTime? dateRef1 { get; set; }
        public DateTime? dateRef2 { get; set; }
        public int numRef1 { get; set; }
        public int numRef2 { get; set; }
        public int pos { get; set; }
    }
}