using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasySales.Object.QNE
{
    public class SalesInvoicePostParams
    {
        public string customer { get; set; }
        public DateTime invoiceDate { get; set; }
        public string invoiceCode { get; set; }
        public string invoiceTo { get; set; }
        public string deliveryTerm { get; set; }
        public string term { get; set; }
        public string stockLocation { get; set; }
        public string attention { get; set; }
        public string phone { get; set; }
        public string fax { get; set; }
        public string address1 { get; set; }
        public string address2 { get; set; }
        public string address3 { get; set; }
        public string address4 { get; set; }
        public string referenceNo { get; set; }
        public string salesPerson { get; set; }
        public string ourDONO { get; set; }
        public string title { get; set; }
        public string title2 { get; set; }
        public string ref1 { get; set; }
        public string ref2 { get; set; }
        public string ref3 { get; set; }
        public string ref4 { get; set; }
        public string ref5 { get; set; }
        public string remark1 { get; set; }
        public string remark2 { get; set; }
        public string remark3 { get; set; }
        public string remark4 { get; set; }
        public string remark5 { get; set; }
        public string project { get; set; }
        public string costCentre { get; set; }
        public decimal currencyRate { get; set; }
        public bool isTaxInclusive { get; set; }
        public bool isRounding { get; set; }
        public string doContact { get; set; }
        public string doPhone { get; set; }
        public string doAddress1 { get; set; }
        public string doAddress2 { get; set; }
        public string doAddress3 { get; set; }
        public string doAddress4 { get; set; }
        public SalesInvoiceDetailPostParams[] Details { get; set; }
    }
}
