using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using System.Collections;
using EasySales.Object;
using EasySales.Model;
using System.Data;
using AutoCount.Data;

namespace EasySales
{
    class SQLHandler_ATC
    {
        public string IsServerName(string server, string instance, string strPort)
        {
            return AutoCountServer.JoinServerInstance(server, instance, Helper.ToInteger(strPort));
        }

        public DBSetting IsDBsetting(string server, string db, string password)
        {
            if (string.IsNullOrEmpty(password))
            {
                return new DBSetting(DBServerType.SQL2000, server, db);
            }
            else
            {
                return new DBSetting(DBServerType.SQL2000, server, "sa", password, db);
            }
        }

        public bool IsConnected(DBSetting dBSetting)
        {
            try
            {
                object obj = dBSetting.ExecuteScalar("SELECT 1");
                return obj == null ? false : AutoCount.Converter.ToInt32(obj) == 1;
            }
            catch (AutoCount.AppException)
            {
                return false;
            }
        }
    }
}