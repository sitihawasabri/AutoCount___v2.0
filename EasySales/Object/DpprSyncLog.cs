using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasySales.Object
{
    public class DpprSyncLog
    {
        public string action_identifier { get; set; }
        public string action_details { get; set; }
        public string action_time { get; set; }
        public int action_failure { get; set; }
        public string action_failure_message { get; set; }

        public DpprSyncLog() { }
    }
}
