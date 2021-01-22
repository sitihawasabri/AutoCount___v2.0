using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasySales.Object.QNE
{
    public class SalesOrdersPostParams
    {
        public string Customer { get; set; }
        public DateTime OrderDate { get; set; }
        public string OrderCode { get; set; }
        public string CustomerName { get; set; }
        public string DeliveryTerm { get; set; }
        public string Term { get; set; }
        public string StockLocation { get; set; }
        public string Attention { get; set; }
        public string SalesPerson { get; set; }
        public string OurDONO { get; set; }
        public string Project { get; set; }
        public string CostCentre { get; set; }
        public decimal? CurrencyRate { get; set; }
        //public int CurrencyRate { get; set; }
        public string ReferenceNo { get; set; }
        public bool? IsRounding { get; set; }
        public string Phone { get; set; }
        public string Fax { get; set; }
        public string Address1 { get; set; }
        public string Address2 { get; set; }
        public string Address3 { get; set; }
        public string Address4 { get; set; }
        public int TermId { get; set; }
        public int SalesPersonId { get; set; }
        public string Title { get; set; }
        public string Title2 { get; set; }
        public string Ref1 { get; set; }
        public string Ref2 { get; set; }
        public string Ref3 { get; set; }
        public string Ref4 { get; set; }
        public string Ref5 { get; set; }
        public string Remark1 { get; set; }
        public string Remark2 { get; set; }
        public string Remark3 { get; set; }
        public string Remark4 { get; set; }
        public string Remark5 { get; set; }
        public bool? IsCancelled { get; set; }
        public DateTime RequireDate { get; set; }
        public string DoBranchCode { get; set; }
        public string DoBranchName { get; set; }
        public string DoContact { get; set; }
        public string DoPhone { get; set; }
        public string DoFax { get; set; }
        public string DoAddress1 { get; set; }
        public string DoAddress2 { get; set; }
        public string DoAddress3 { get; set; }
        public string DoAddress4 { get; set; }
        public string Discount { get; set; }
        public string Notes { get; set; }
        public bool? IsTaxInclusive { get; set; }
        public DateTime TaxDate { get; set; }
        public string DoRegistationNo { get; set; }
        public string DoGstRegNo { get; set; }
        public string DoPhone2 { get; set; }
        public string DoEmail { get; set; }
        public string DoRemark { get; set; }
        public string DeliveryArea { get; set; }
        public bool? IsApproved { get; set; }
        public SalesOrdersDetailPostParams[] Details { get; set; }
    }
}
