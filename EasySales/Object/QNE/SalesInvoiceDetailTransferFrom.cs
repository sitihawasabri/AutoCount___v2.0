using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasySales.Object.QNE
{
    public class SalesInvoiceDetailTransferFrom
    {
        public string quotationDetailId { get; set; }
        public string salesOrderDetailId { get; set; }
        public string deliveryOrderDetailId { get; set; }
    }
}
