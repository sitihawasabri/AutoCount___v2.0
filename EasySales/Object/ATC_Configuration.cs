using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoCount.Data;
using System.Collections.Specialized;
using System.Configuration;
using System.IO;

namespace EasySales.Object
{
    public class ATC_Configuration
    {
        public static ATC_Connection Init_config()
        {
            ATC_Connection connection = new ATC_Connection();

            List<DpprMySQLconfig> mysql_config = LocalDB.GetRemoteDatabaseConfig();
            DpprMySQLconfig config_mysql = mysql_config[0];

            connection.mysql_host = config_mysql.config_host;
            connection.mysql_user = config_mysql.config_username;
            connection.mysql_password = config_mysql.config_password;
            connection.mysql_db = config_mysql.config_database;

            List<DpprSQLServerconfig> mssql_config = LocalDB.GetRemoteSQLServerConfig();
            DpprSQLServerconfig config_mssql = mssql_config[0];

            string host = config_mssql.data_source;
            string[] splited = host.Split('\\');
            connection.db_server = splited[0];
            connection.db_instance = splited[1];
            connection.autoCount_db = config_mssql.database_name;
            connection.sql_id = config_mssql.user_id;
            connection.sql_password = config_mssql.password;

            List<DpprAccountingSoftware> getAccSoftware = LocalDB.GetAccountingSoftwares();
            DpprAccountingSoftware accSoftware = getAccSoftware[0];

            connection.autoCount_id = accSoftware.software_username;
            connection.autoCount_password = accSoftware.software_password;

            return connection;
        }
    }
}