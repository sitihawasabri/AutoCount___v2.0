using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasySales.Object
{
    public class DpprFTPServerConfig
    {
        public string upload_path { get; set; }
        public string username { get; set; }
        public string password { get; set; }
        public string company_name { get; set; }

        public DpprFTPServerConfig() { }
    }
}
