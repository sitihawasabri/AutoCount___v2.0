using AutoCount;
using AutoCount.Const;
using AutoCount.Authentication;
using EasySales.Object;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasySales
{
    public class AutoCountV2
    {
        private static ATC_Connection connection;

        static public UserSession PerformAuth(ref ATC_Connection _connection)
        {
            GlobalLogger logger = new GlobalLogger();
            try
            {

                string serverName = _connection.db_server + "\\" + _connection.db_instance;

                logger.Broadcast("autoCount_db: " + _connection.autoCount_db);
                logger.Broadcast("serverName: " + serverName);
                logger.Broadcast("password: " + _connection.sql_password);

                if (string.IsNullOrEmpty(_connection.sql_password))
                    return new AutoCount.Authentication.UserSession(new AutoCount.Data.DBSetting(AutoCount.Data.DBServerType.SQL2000, serverName, _connection.autoCount_db));
                else
                    return new AutoCount.Authentication.UserSession(new AutoCount.Data.DBSetting(AutoCount.Data.DBServerType.SQL2000, serverName,
                        AutoCount.Const.AppConst.DefaultUserName, _connection.sql_password, _connection.autoCount_db));
            }
            catch (AppException ex)
            {
                logger.Broadcast("UserSession: null ---> " + ex.Message);
                return null;
            }
        }


        static public bool TriggerConnection()
        {
            GlobalLogger logger = new GlobalLogger();
            connection = ATC_Configuration.Init_config();
            connection.userSession = PerformAuth(ref connection);
            logger.Broadcast("connection.userSession: " + connection.userSession);
            logger.Broadcast("connection.userSession.DBSetting: " + connection.userSession.DBSetting);
            if (connection.userSession.DBSetting != null)
            {
                logger.Broadcast("Getting subProjectSuccess");

                bool subProjectSuccess = PerformSubProject(connection.userSession);

                logger.Broadcast("subProjectSuccess: " + subProjectSuccess);

                if (subProjectSuccess)
                {
                    logger.Broadcast("Trying to login AutoCount");

                    bool isAutoCountLogin = PerformAuthInAutoCount(connection);

                    if (isAutoCountLogin)
                    {
                        logger.Broadcast("return true");
                        return true;
                    }
                }
            }
            logger.Broadcast("return false");
            return false;
        }

        static private bool PerformSubProject(UserSession userSession)
        {
            GlobalLogger logger = new GlobalLogger();
            try
            {
                logger.Broadcast("userSession.DBSetting: " + userSession.DBSetting);
                if(userSession.DBSetting == null)
                {
                    logger.Broadcast("userSession.DBSetting == null");
                }
                else
                {
                    logger.Broadcast("userSession.DBSetting != null : " + userSession.DBSetting);
                    //AutoCount.MainEntry.Startup.Default.SubProjectStartup(userSession.DBSetting); //*Does not load plugin; *License Code;
                    //AutoCount.MainEntry.Startup.Default.ValidateDB(userSession.DBSetting);

                    AutoCount.MainEntry.Startup.Default.SubProjectStartup(userSession, AutoCount.MainEntry.StartupPlugInOption.NoLoad);

                    logger.Broadcast("AutoCount subproject is created");
                    logger.Broadcast("return true");
                    return true;
                }
                logger.Broadcast("AutoCount subproject failed");
                logger.Broadcast("return false");
                return false;
            }
            catch (Exception ex)
            {
                logger.Broadcast("[CATCH] AutoCount subproject failed");
                logger.Broadcast(ex.Message);
                return false;
            }
        }

        static public bool PerformAuthInAutoCount(ATC_Connection connection)
        {
            GlobalLogger logger = new GlobalLogger();
            logger.Broadcast("connection.autoCount_id: " + connection.autoCount_id);
            logger.Broadcast("connection.autoCount_password: " + connection.autoCount_password);
            logger.Broadcast("connection.userSession == null: " + (connection.userSession == null).ToString());
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

        static public bool TestSubProjectStartup(AutoCount.Authentication.UserSession userSession)
        {
            GlobalLogger logger = new GlobalLogger();
            try
            {
                logger.Broadcast("userSession.DBSetting: " + userSession.DBSetting);
                if(userSession.DBSetting != null)
                {
                    logger.Broadcast("userSession.DBSetting != null");
                    AutoCount.MainEntry.Startup.Default.SubProjectStartup(userSession, AutoCount.MainEntry.StartupPlugInOption.NoLoad);
                    logger.Broadcast("Successfully created AutoCount Accounting SubProjectStartup.");
                    return true;
                }
                else
                {
                    logger.Broadcast("userSession.DBSetting null");
                    logger.Broadcast("Failed");
                    return false;
                }
                
            }
            catch (Exception ex)
            {
                logger.Broadcast("[CATCH] Failed initiating AutoCount Accounting SubProjectStartup ---> " + ex.Message);
                return false;
            }
        }

        public static void Message(string msg)
        {
            DpprException ex = new DpprException
            {
                exception = msg,
                file_name = "AutoCountV2",
                time = DateTime.Now.ToLongTimeString()
            };
            LocalDB.InsertException(ex);
        }
    }
}
