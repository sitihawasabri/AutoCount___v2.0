using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasySales.Object
{
    public class DpprAccountingSoftware
    {
        public int running_id { get; set; }
        public string software_name { get; set; }
        public string software_username { get; set; }
        public string software_password { get; set; }
        public string software_link { get; set; }
        public string software_db { get; set; }
        public string software_comp { get; set; }

        public DpprAccountingSoftware() { }
    }
}
