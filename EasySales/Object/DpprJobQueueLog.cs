using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasySales.Object
{
    public class DpprJobQueueLog
    {
        public string job_name { get; set; }
        public string job_level { get; set; }
        public string job_param { get; set; }
        public string job_exec_time { get; set; }

        public DpprJobQueueLog() { }
    }
}
