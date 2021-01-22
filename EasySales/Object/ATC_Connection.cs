using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoCount.Authentication;
using AutoCount.Data;

namespace EasySales.Object
{
    public class ATC_Connection
    {
        public string autoCount_id { get; set; }
        public string autoCount_password { get; set; }
        public string autoCount_db { get; set; }
        public bool autoCount_sst { get; set; }

        public string sql_id { get; set; }
        public string sql_password { get; set; }
        public string sql_server { get; set; }
        public string sql_port { get; set; }
        public string db_server { get; set; }
        public string db_instance { get; set; }

        public string mysql_host { get; set; }
        public string mysql_user { get; set; }
        public string mysql_password { get; set; }
        public string mysql_db { get; set; }

        public DBSetting dBSetting { get; set; }
        public UserSession userSession { get; set; }
    }
}