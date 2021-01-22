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
//using Data;
//using AutoCount;
using AutoCount.Authentication;

namespace EasySales
{
    public class AutoCountV1
    {
        private static ATC_Connection connection = null;
       
        static public bool TriggerConnection()
        {
            GlobalLogger logger = new GlobalLogger();
            connection = ATC_Configuration.Init_config();
            connection.dBSetting = PerformAuth(ref connection);

            if (connection.dBSetting != null)
            {
                bool subProjectSuccess = PerformSubProject(connection);

                if (subProjectSuccess)
                {
                    logger.Broadcast("Trying to login AutoCount");

                    bool isAutoCountLogin = PerformAuthInAutoCount(connection);

                    if (isAutoCountLogin)
                    {
                       return true;
                    }
                }
            }
            return false;
        }

        static private bool PerformSubProject(ATC_Connection connection)
        {
            GlobalLogger logger = new GlobalLogger();
            try
            {
                AutoCount.MainEntry.Startup.Default.SubProjectStartup(connection.userSession, AutoCount.MainEntry.StartupPlugInOption.NoLoad);
                logger.Broadcast("AutoCount subproject is created");
                return true;
            }
            catch (AutoCount.AppException e)
            {
                logger.Broadcast(e.Message);
                return false;
            }
            catch (Exception e)
            {
                logger.Broadcast("AutoCount subproject failed");
                logger.Broadcast(e.Message);
                return false;
            }
        }

        static public DBSetting PerformAuth(ref ATC_Connection connection)
        {
            GlobalLogger logger = new GlobalLogger();
            logger.Broadcast("Trying to connect MSSQL server");

            SQLHandler_ATC sql = new SQLHandler_ATC();
            connection.sql_server = sql.IsServerName(connection.db_server, connection.db_instance, connection.sql_port);

            logger.Broadcast("Connecting with server [ " + connection.sql_server + " ]");

            DBSetting dBSetting = sql.IsDBsetting(connection.sql_server, connection.autoCount_db, connection.sql_password);

            if (dBSetting == null)
            {
                logger.Broadcast("Connection with MSSQL server is failed. Auth used [ " + connection.sql_server + ", " + connection.autoCount_db + ", " + connection.sql_password + " ]");
            }
            else
            {
                logger.Broadcast("Connection with MSSQL is successful");
            }
            return dBSetting;
        }

        static public bool PerformAuthInAutoCount(ATC_Connection connection)
        {
            GlobalLogger logger = new GlobalLogger();
            logger.Broadcast(connection.autoCount_id);
            logger.Broadcast(connection.autoCount_password);
            bool isLoginSuccessful = connection.userSession.Login(connection.autoCount_id, connection.autoCount_password);

            if (isLoginSuccessful)
            {
                logger.Broadcast("Login with AutoCount is successful");
                return true;
            }
            else
            {
                logger.Broadcast("Login with AutoCount is failed");
                return false;
            }
        }
        public static void Message(string msg)
        {
            DpprException ex = new DpprException
            {
                exception = msg,
                file_name = "AutoCount",
                time = DateTime.Now.ToLongTimeString()
            };
            LocalDB.InsertException(ex);
        }

    }
}