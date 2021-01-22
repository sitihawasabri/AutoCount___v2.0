using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasySales.Object
{
    public class DpprSQLServerconfig
    {
        public string data_source { get; set; }
        public string database_name { get; set; }
        public string user_id { get; set; }
        public string password { get; set; }
        public DpprSQLServerconfig() { }
    }
}