using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasySales.Object
{
    public class DpprTransferLog
    {
        public string doc_id { get; set; }
        public int doc_failure { get; set; }
        public string doc_failure_message { get; set; }
        public string last_tried_at { get; set; }
        public int tried { get; set; }

        public DpprTransferLog () { }
    }
}
