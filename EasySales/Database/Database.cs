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

namespace EasySales
{
    public class Database
    {
        private string connectionString = string.Empty;

        private readonly string checkIfStoreProcExistsQuery = "SELECT ROUTINE_NAME FROM INFORMATION_SCHEMA.ROUTINES WHERE ROUTINE_TYPE='PROCEDURE' AND ROUTINE_SCHEMA=(SELECT DATABASE()) AND ROUTINE_NAME LIKE '%kill_other_processes%';";

        private readonly string createStoreProcToKill = @"DROP PROCEDURE IF EXISTS kill_other_processes;
                                                CREATE PROCEDURE kill_other_processes()
                                                BEGIN
                                                DECLARE finished INT DEFAULT 0;
                                                DECLARE proc_id INT;
                                                DECLARE proc_id_cursor CURSOR FOR SELECT * FROM information_schema.processlist WHERE `db` = (SELECT DATABASE()) AND `command` LIKE '%sleep%' AND `time` > 60;
                                                DECLARE CONTINUE HANDLER FOR NOT FOUND SET finished = 1;
                                                OPEN proc_id_cursor;
                                                proc_id_cursor_loop: LOOP
                                                    FETCH proc_id_cursor INTO proc_id;
                                                    IF finished = 1 THEN 
                                                    LEAVE proc_id_cursor_loop;
                                                    END IF;
                                                    IF proc_id <> CONNECTION_ID() THEN
                                                    KILL proc_id;
                                                    END IF;
                                                END LOOP proc_id_cursor_loop;
                                                CLOSE proc_id_cursor;
                                                END;";

        private readonly string killSleepyHeads = "CALL kill_other_processes();";

        private bool checkInternet = true;
        public Database()
        {
            this.connectionString = this.Connect();

            ArrayList storeProcCheck = this.Select(checkIfStoreProcExistsQuery);
            if (storeProcCheck.Count == 0)
            {
                this.Insert(createStoreProcToKill);
            }
        }

        public string Connect(int index)
        {
            this.connectionString =  this._Connect(index: index);
            return this.connectionString;
        }

        public string Connect()
        {
            this.connectionString = this._Connect(0);
            return this.connectionString;
        }

        private string _Connect(int index)
        {
            List<DpprMySQLconfig> list = LocalDB.GetRemoteDatabaseConfig();
            DpprMySQLconfig config = list[index];
            //Console.WriteLine(config.config_host);
            return string.Format("Server={0}; database={1}; UID={2}; password={3}; Pooling=false;", config.config_host, config.config_database, config.config_username, config.config_password); ;
        }

        public bool Insert(string mQuery)
        {
            try
            {
                checkInternet = InternetConnection.IsConnectedToInternet();
                if(!checkInternet)
                {
                    Message("No internet connection at the moment", true);
                    goto ENDJOB;
                }

                using (MySqlConnection mConnection = new MySqlConnection(this.connectionString))
                {
                    mConnection.Open();
                    Message("Connection is open", true);
                    using (MySqlCommand mCommand = new MySqlCommand(mQuery, mConnection))
                    {
                        mCommand.CommandType = CommandType.Text;
                        mCommand.CommandTimeout = 120;              
                        try
                        {
                            mCommand.ExecuteNonQuery();
                            mCommand.Dispose();
                        }
                        catch (MySqlException e)
                        {
                            Message(e.Message + "----> " + mQuery, true);
                            mConnection.Close();
                            Message("[catch: MYSQL Insert] mConnection.State ---> " + mConnection.State.ToString());
                            mConnection.Dispose();
                            Message("Connection is closed", true);
                            return false;
                        }
                    }
                    //using (MySqlCommand mCommand = new MySqlCommand(killSleepyHeads, mConnection))
                    //{
                    //    mCommand.CommandType = CommandType.Text;
                    //    mCommand.ExecuteNonQuery();
                    //    mCommand.Dispose();
                    //}
                    
                    mConnection.Close();
                    Message("[MYSQL Insert] mConnection.State ---> " + mConnection.State.ToString());
                    mConnection.Dispose();
                    Message("Connection is close", true);
                };
                return true;
            }
            catch(MySqlException e)
            {
                Message(e.Message + "---- " + mQuery, true);
            }
            ENDJOB:
            return false;
        }

        public ArrayList Select(string mQuery)
        {
            ArrayList result = new ArrayList();
            try
            {
                checkInternet = InternetConnection.IsConnectedToInternet();
                if (!checkInternet)
                {
                    Message("No internet connection at the moment", true);
                    goto ENDJOB;
                }
                using (MySqlConnection mConnection = new MySqlConnection(this.connectionString))
                {
                    mConnection.Open();
                    Message("Connection is open", true);
                    try
                    {
                        using (MySqlCommand mCommand = new MySqlCommand(mQuery, mConnection))
                        {
                            mCommand.CommandType = CommandType.Text;
                            mCommand.CommandTimeout = 120;          
                            using (MySqlDataReader mReader = mCommand.ExecuteReader())
                            {
                                while (mReader.Read())
                                {
                                    Dictionary<string, string> map = new Dictionary<string, string>();
                                    for (int i = 0, size = mReader.FieldCount; i < size; i++)
                                    {
                                        string key = mReader.GetName(i).ToString();
                                        map[key] = mReader[key].ToString();
                                    }
                                    result.Add(map);
                                }
                                mReader.Close();
                            };
                            mCommand.Dispose();
                        };
                    }
                    catch (MySqlException e)
                    {
                        Message(e.Message, true);
                        mConnection.Close();
                        Message("[catch: MYSQL Select] mConnection.State ---> " + mConnection.State.ToString());
                        mConnection.Dispose();
                        Message("Connection is closed", true);
                    }

                    mConnection.Close();
                    Message("[MYSQL Select] mConnection.State ---> " + mConnection.State.ToString());
                    mConnection.Dispose();
                    Message("Connection is close", true);
                };
            }
            catch(MySqlException e)
            {
                Message(e.Message, true);
            }
            ENDJOB:
            return result;
        }

        public static void Sanitize(ref string str)
        {
            if (str != null)
            {
                str = MySqlHelper.EscapeString(str.ToString());
            }
            else
            {
                str = string.Empty;
            }
        }

        public static DateTime NativeDateTime(string strDatetime)
        {
            if (strDatetime == null)
            {
                return DateTime.Now;
            }
            return DateTime.ParseExact(strDatetime, "dd/MM/yyyy HH:mm:ss",
                                       System.Globalization.CultureInfo.InvariantCulture);
        }

        public Dictionary<string, string> GetUpdatedTime(string tablename)
        {
            ArrayList res = this.Select(string.Format("SELECT(updated_at - INTERVAL 3 DAY) AS updated_at FROM cms_update_time WHERE table_name = '{0}'", tablename));
            if (res.Count > 0)
            {
                return (Dictionary<string, string>)res[0];
            }
            return new Dictionary<string, string>();
        }
        
        public Dictionary<string, string> GetImageUpdatedTime()
        {
            ArrayList res = this.Select(string.Format("SELECT (updated_at - INTERVAL 15 DAY) AS updated_at FROM cms_product_image WHERE product_image_id = (SELECT MAX(product_image_id) FROM cms_product_image);"));
            if (res.Count > 0)
            {
                return (Dictionary<string, string>)res[0];
            }
            return new Dictionary<string, string>();
        }
        
        public Dictionary<string, string> GetUpdatedTime1MonthInterval(string tablename)
        {
            ArrayList res = this.Select(string.Format("SELECT(updated_at - INTERVAL 1 MONTH) AS updated_at FROM cms_update_time WHERE table_name = '{0}'", tablename));
            if (res.Count > 0)
            {
                return (Dictionary<string, string>)res[0];
            }
            return new Dictionary<string, string>();
        }
        
        public Dictionary<string, string> GetUpdatedTime6MonthInterval(string tablename)
        {
            ArrayList res = this.Select(string.Format("SELECT(updated_at - INTERVAL 6 MONTH) AS updated_at FROM cms_update_time WHERE table_name = '{0}'", tablename));
            if (res.Count > 0)
            {
                return (Dictionary<string, string>)res[0];
            }
            return new Dictionary<string, string>();
        }

        public Dictionary<string, string> GetUpdatedTime3MonthInterval(string tablename)
        {
            ArrayList res = this.Select(string.Format("SELECT(updated_at - INTERVAL 3 MONTH) AS updated_at FROM cms_update_time WHERE table_name = '{0}'", tablename));
            if (res.Count > 0)
            {
                return (Dictionary<string, string>)res[0];
            }
            return new Dictionary<string, string>();
        }

        public Dictionary<string, string> GetUpdatedTime1YearInterval(string tablename)
        {
            ArrayList res = this.Select(string.Format("SELECT(updated_at - INTERVAL 1 YEAR) AS updated_at FROM cms_update_time WHERE table_name = '{0}'", tablename));
            if (res.Count > 0)
            {
                return (Dictionary<string, string>)res[0];
            }
            return new Dictionary<string, string>();
        }

        public Dictionary<string, string> GetUpdatedTimeToday(string tablename)
        {
            ArrayList res = this.Select(string.Format("SELECT updated_at FROM cms_update_time WHERE table_name = '{0}'", tablename));
            if (res.Count > 0)
            {
                return (Dictionary<string, string>)res[0];
            }
            return new Dictionary<string, string>();
        }

        public void Message(string msg, bool error = false, bool show = false)
        {
            DpprException ex = new DpprException
            {
                exception = msg,
                file_name = "MySQL",
                time = DateTime.Now.ToString()//DateTime.Now.ToLongTimeString()
            };
            LocalDB.InsertException(ex);
        }
    }
}