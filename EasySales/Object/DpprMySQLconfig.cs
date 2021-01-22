using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasySales.Object
{
    public class DpprMySQLconfig
    {
        public string config_name { get; set; }
        public string config_host { get; set; }
        public string config_username { get; set; }
        public string config_password { get; set; }
        public string config_database { get; set; }
        public string socket_address { get; set; }
        public DpprMySQLconfig () { }

        public override bool Equals(object obj)
        {
            return ((DpprMySQLconfig)obj).config_database == config_database; //base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return config_database.GetHashCode(); //base.GetHashCode();
        }
    }
}
