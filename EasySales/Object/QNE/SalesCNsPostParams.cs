using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasySales.Object.QNE
{
    public class SalesCNsPostParams
    {
        public string customer { get; set; }
        public DateTime cnDate { get; set; }
        public string cnCode { get; set; }
        public string customerName { get; set; }
        public string deliveryTerm { get; set; }
        public string term { get; set; }
        public string stockLocation { get; set; }
        public string attention { get; set; }
        public string salesPerson { get; set; }
        public string ourDONO { get; set; }
        public string project { get; set; }
        public string costCentre { get; set; }
        public decimal? currencyRate { get; set; }
        public string referenceNo { get; set; }
        public bool isRounding { get; set; }
        public string phone { get; set; }
        public string fax { get; set; }
        public string address1 { get; set; }
        public string address2 { get; set; }
        public string address3 { get; set; }
        public string address4 { get; set; }
        public int termId { get; set; }
        public int salesPersonId { get; set; }
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
        public bool isCancelled { get; set; }
        public string doBranchCode { get; set; }
        public string doBranchName { get; set; }
        public string doContact { get; set; }
        public string doPhone { get; set; }
        public string doFax { get; set; }
        public string doAddress1 { get; set; }
        public string doAddress2 { get; set; }
        public string doAddress3 { get; set; }
        public string doAddress4 { get; set; }
        public string discount { get; set; }
        public string notes { get; set; }
        public bool isTaxInclusive { get; set; }
        public DateTime taxDate { get; set; }
        public string doRegistationNo { get; set; }
        public string doGstRegNo { get; set; }
        public string doPhone2 { get; set; }
        public string doEmail { get; set; }
        public string doRemark { get; set; }
        public string deliveryArea { get; set; }
        public bool isApproved { get; set; }
        public SalesCNsDetailPostParams[] Details { get; set; }
    }
}
