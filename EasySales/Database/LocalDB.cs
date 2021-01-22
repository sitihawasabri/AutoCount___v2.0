using Dapper;
using EasySales.Model;
using EasySales.Object;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EasySales
{
    public static class LocalDB
    {
        private static ReaderWriterLockSlim _readerWriterLock = new ReaderWriterLockSlim();
        public static event EventHandler<GlobalEvent> notification = delegate { };

        public static List<object> Execute(string query)
        {
            try
            {
                _readerWriterLock.EnterReadLock();

                using (IDbConnection cn = new SQLiteConnection(ConnectionString()))
                {
                    var result = cn.Query<object>(query, new DynamicParameters());
                    return result.ToList();
                }
            }
            catch(SQLiteException e)
            {
                Console.WriteLine(Constants.SQLite_Exception+e.Message);
            }
            finally
            {
                _readerWriterLock.ExitReadLock();
            }
            return new List<object>();
        }
        public static List<DpprSyncLog> checkJobRunning()
        {
            try
            {
                _readerWriterLock.EnterReadLock();

                using (IDbConnection cn = new SQLiteConnection(ConnectionString()))
                {
                    string query = "SELECT MAX(running_id), action_identifier, action_details FROM sync_log where action_identifier = 'APS_transfer_SO' OR action_identifier = 'salesinvoices_post' OR action_identifier = 'transfer_SO'";
                    //"SELECT MAX(running_id), action_details FROM sync_log where action_identifier = 'APS_transfer_SO'"
                    var result = cn.Query<DpprSyncLog>(query, new DynamicParameters());
                    return result.ToList();
                }
            }
            catch (SQLiteException e)
            {
                Console.WriteLine(Constants.SQLite_Exception + e.Message);
            }
            finally
            {
                _readerWriterLock.ExitReadLock();
            }
            return new List<DpprSyncLog>();
        }

        public static List<DpprSQLiteSequenceLog> btnAlertCrashCondition()
        {
            try
            {
                _readerWriterLock.EnterReadLock();

                using (IDbConnection cn = new SQLiteConnection(ConnectionString()))
                {
                    string query = "SELECT * FROM sqlite_sequence WHERE name = 'testAlertEmail'";
                    var result = cn.Query<DpprSQLiteSequenceLog>(query, new DynamicParameters());
                    return result.ToList();
                }
            }
            catch (SQLiteException e)
            {
                Console.WriteLine(Constants.SQLite_Exception + e.Message);
            }
            finally
            {
                _readerWriterLock.ExitReadLock();
            }
            return new List<DpprSQLiteSequenceLog>();
        }

        public static List<DpprUserSettings> GetUserSettings()
        {
            try
            {
                _readerWriterLock.EnterReadLock();

                using (IDbConnection cn = new SQLiteConnection(ConnectionString()))
                {
                    var result = cn.Query<DpprUserSettings>("SELECT * FROM user_settings", new DynamicParameters());
                    return result.ToList();
                }
            }
            catch (SQLiteException e)
            {
                Console.WriteLine(Constants.SQLite_Exception + e.Message);
            }
            finally
            {
                _readerWriterLock.ExitReadLock();
            }
            return new List<DpprUserSettings>();
        }

        public static DpprUserSettings GetParticularSetting(string name)
        {
            try
            {
                _readerWriterLock.EnterReadLock();

                using (IDbConnection cn = new SQLiteConnection(ConnectionString()))
                {
                    var result = cn.Query<DpprUserSettings>("SELECT * FROM user_settings WHERE name = '"+name+"'", new DynamicParameters());
                    return result.ToList()[0];
                }
            }
            catch (SQLiteException e)
            {
                Console.WriteLine(Constants.SQLite_Exception + e.Message);
            }
            finally
            {
                _readerWriterLock.ExitReadLock();
            }
            return null;
        }

        public static List<DpprAccountingSoftware> GetAccountingSoftwares()
        {
            try
            {
                _readerWriterLock.EnterReadLock();

                using (IDbConnection cn = new SQLiteConnection(ConnectionString()))
                {
                    var result = cn.Query<DpprAccountingSoftware>("SELECT * FROM accounting_software", new DynamicParameters());
                    return result.ToList();
                }
            }
            catch (SQLiteException e)
            {
                Console.WriteLine(Constants.SQLite_Exception + e.Message);
            }
            finally
            {
                _readerWriterLock.ExitReadLock();
            }
            return new List<DpprAccountingSoftware>();
        }

        public static List<DpprTransferLog> GetTransferLogById(string doc_id)
        {
            try
            {
                _readerWriterLock.EnterReadLock();

                using (IDbConnection cn = new SQLiteConnection(ConnectionString()))
                {
                    var result = cn.Query<DpprTransferLog>("SELECT * FROM transfer_log = '" + doc_id + "'", new DynamicParameters());
                    return result.ToList();
                }
            }
            catch (SQLiteException e)
            {
                Console.WriteLine(Constants.SQLite_Exception + e.Message);
            }
            finally
            {
                _readerWriterLock.ExitReadLock();
            }
            return new List<DpprTransferLog>();
        }

        public static List<DpprMySQLconfig> GetRemoteDatabaseConfig()
        {
            try
            {
                _readerWriterLock.EnterReadLock();

                using (IDbConnection cn = new SQLiteConnection(ConnectionString()))
                {
                    var result = cn.Query<DpprMySQLconfig>("SELECT * FROM configuration", new DynamicParameters());
                    return result.ToList();
                }
            }
            catch (SQLiteException e)
            {
                Console.WriteLine(Constants.SQLite_Exception + e.Message);
            }
            finally
            {
                _readerWriterLock.ExitReadLock();
            }
            return new List<DpprMySQLconfig>();
        }

        public static List<DpprFTPServerConfig> GetFTPServerConfig()                                                /* FTP SERVER CONFIG*/
        {
            try
            {
                _readerWriterLock.EnterReadLock();

                using (IDbConnection cn = new SQLiteConnection(ConnectionString()))
                {
                    var result = cn.Query<DpprFTPServerConfig>("SELECT * FROM ftp_server", new DynamicParameters());
                    return result.ToList();
                }
            }
            catch (SQLiteException e)
            {
                Console.WriteLine(Constants.SQLite_Exception + e.Message);
            }
            finally
            {
                _readerWriterLock.ExitReadLock();
            }
            return new List<DpprFTPServerConfig>();
        }

        public static List<DpprSQLServerconfig> GetRemoteSQLServerConfig()                                          /* SQL SERVER CONFIG*/
        {
            try
            {
                _readerWriterLock.EnterReadLock();

                using (IDbConnection cn = new SQLiteConnection(ConnectionString()))
                {
                    var result = cn.Query<DpprSQLServerconfig>("SELECT * FROM sql_server", new DynamicParameters());
                    return result.ToList();
                }
            }
            catch (SQLiteException e)
            {
                Console.WriteLine(Constants.SQLite_Exception + e.Message);
            }
            finally
            {
                _readerWriterLock.ExitReadLock();
            }
            return new List<DpprSQLServerconfig>();
        }

        public static void InsertJobLog(DpprJobQueueLog log)
        {
            bool isBroken = false;
            try
            {
                _readerWriterLock.EnterWriteLock();
                if (_readerWriterLock.WaitingReadCount > 0)
                {
                    isBroken = true;
                }
                else
                {
                    using (IDbConnection cn = new SQLiteConnection(ConnectionString()))
                    {
                        cn.Execute("INSERT INTO jobqueue_log (job_name,job_level,job_param,job_exec_time) VALUES (@job_name,@job_level,@job_param,@job_exec_time)", log);

                        //notification(null, new GlobalEvent(ObjectToString<DpprJobQueueLog>(log)));
                    }
                }
            }
            catch (SQLiteException e)
            {
                Console.WriteLine(Constants.SQLite_Exception + e.Message);
            }
            finally
            {
                _readerWriterLock.ExitWriteLock();
            }
            if (isBroken)
            {
                Thread.Sleep(10);
                InsertJobLog(log);
            }
        }

        public static void InsertUserSetting(DpprUserSettings log)
        {
            bool isBroken = false;
            try
            {
                _readerWriterLock.EnterWriteLock();
                if (_readerWriterLock.WaitingReadCount > 0)
                {
                    isBroken = true;
                }
                else
                {
                    using (IDbConnection cn = new SQLiteConnection(ConnectionString()))
                    {
                        cn.Execute("INSERT INTO user_settings (name,setting) VALUES (@name,@setting)  ON CONFLICT(name) DO UPDATE SET setting = '" + log.setting+"'", log);

                        //notification(null, new GlobalEvent(ObjectToString<DpprSyncLog>(log)));
                    }
                }
            }
            catch (SQLiteException e)
            {
                Console.WriteLine(Constants.SQLite_Exception + e.Message);
            }
            finally
            {
                _readerWriterLock.ExitWriteLock();
            }
            if (isBroken)
            {
                Thread.Sleep(10);
                InsertUserSetting(log);
            }
        }

        public static void InsertSyncLog(DpprSyncLog log)
        {
            bool isBroken = false;
            try
            {
                _readerWriterLock.EnterWriteLock();
                if (_readerWriterLock.WaitingReadCount > 0)
                {
                    isBroken = true;
                }
                else
                {
                    using (IDbConnection cn = new SQLiteConnection(ConnectionString()))
                    {
                        cn.Execute("INSERT INTO sync_log (action_identifier,action_details,action_time,action_failure,action_failure_message) VALUES (@action_identifier,@action_details,@action_time,@action_failure,@action_failure_message)", log);

                        //notification(null, new GlobalEvent(ObjectToString<DpprSyncLog>(log)));
                    }
                }
            }
            catch (SQLiteException e)
            {
                Console.WriteLine(Constants.SQLite_Exception + e.Message);
            }
            finally
            {
                _readerWriterLock.ExitWriteLock();
            }
            if (isBroken)
            {
                Thread.Sleep(10);
                InsertSyncLog(log);
            }
        }

        public static void InsertTransferSyncLog(DpprTransferLog log)
        {
            bool isBroken = false;
            try
            {
                _readerWriterLock.EnterWriteLock();
                if (_readerWriterLock.WaitingReadCount > 0)
                {
                    isBroken = true;
                }
                else
                {
                    using (IDbConnection cn = new SQLiteConnection(ConnectionString()))
                    {
                        cn.Execute("INSERT INTO transfer_log (doc_id, doc_failure, doc_failure_message, last_tried_at, tried) VALUES (@doc_id, @doc_failure, @doc_failure_message, @last_tried_at, @tried) ON CONFLICT(doc_id) DO UPDATE SET tried = tried + 1", log);

                        //notification(null, new GlobalEvent(ObjectToString<DpprTransferLog>(log)));
                    }
                }
            }
            catch (SQLiteException e)
            {
                Console.WriteLine(Constants.SQLite_Exception + e.Message);
            }
            finally
            {
                _readerWriterLock.ExitWriteLock();
            }
            if (isBroken)
            {
                Thread.Sleep(10);
                InsertTransferSyncLog(log);
            }
        }

        public static void InsertException (DpprException exception)
        {
            bool isBroken = false;
            try
            {
                _readerWriterLock.EnterWriteLock();
                if (_readerWriterLock.WaitingReadCount > 0)
                {
                    isBroken = true;
                }
                else
                {
                    using (IDbConnection cn = new SQLiteConnection(ConnectionString()))
                    {
                        cn.Execute("INSERT INTO app_exceptions (file_name, exception, time) VALUES (@file_name, @exception, @time)", exception);

                        //notification(null, new GlobalEvent(ObjectToString<DpprException>(exception)));
                    }
                }
            }
            catch (SQLiteException e)
            {
                Console.WriteLine(Constants.SQLite_Exception + e.Message);
            }
            finally
            {
                _readerWriterLock.ExitWriteLock();
            }
            if (isBroken)
            {
                Thread.Sleep(10);
                InsertException(exception);
            }
        }

        public static void Add(string query)
        {
            bool isBroken = false;
            try
            {
                _readerWriterLock.EnterWriteLock();
                if (_readerWriterLock.WaitingReadCount > 0)
                {
                    isBroken = true;
                }
                else
                {
                    using (IDbConnection cn = new SQLiteConnection(ConnectionString()))
                    {
                        cn.Execute(query);
                    }
                }
            }
            catch (SQLiteException e)
            {
                Console.WriteLine(Constants.SQLite_Exception + e.Message);
            }
            finally
            {
                _readerWriterLock.ExitWriteLock();
            }
            if (isBroken)
            {
                Thread.Sleep(10);
                Add(query);
            }
        }

        public static void Delete(string query)
        {
            bool isBroken = false;
            try
            {
                _readerWriterLock.EnterWriteLock();
                if (_readerWriterLock.WaitingReadCount > 0)
                {
                    isBroken = true;
                }
                else
                {
                    using (IDbConnection cn = new SQLiteConnection(ConnectionString()))
                    {
                        cn.Execute(query);
                    }
                }
            }
            catch (SQLiteException e)
            {
                Console.WriteLine(Constants.SQLite_Exception + e.Message);
            }
            finally
            {
                _readerWriterLock.ExitWriteLock();
            }
            if (isBroken)
            {
                Thread.Sleep(10);
                Delete(query);
            }
        }

        public static void DBCleanup()
        {
            //Delete("delete from app_exceptions where running_id in (select running_id from app_exceptions order by running_id desc limit 5000,100000000)");
            Delete("delete from app_exceptions where running_id in (select running_id from app_exceptions order by running_id desc limit 5000,100000000) AND file_name NOT LIKE '%EXE%' and file_name NOT LIKE '%Unhandle%'");
            Delete("delete from sync_log where running_id in (select running_id from sync_log order by running_id desc limit 1000,100000000)");
            Execute("VACUUM;");
        }

        public static void QNEDBCleanup()
        {
            Delete("DELETE FROM app_exceptions WHERE file_name NOT LIKE '%QNEAPI%' and file_name NOT LIKE '%Unhandle%'");
            Delete("delete from sync_log where running_id in (select running_id from sync_log order by running_id desc limit 1000,100000000)");
            Execute("VACUUM;");
        }

        public static void GlobalLog(GlobalLogger logger)
        {
            notification(null, new GlobalEvent(ObjectToString<GlobalLogger>(logger)));
        }

        private static string ConnectionString(string connectionId = "Default")
        {
            return ConfigurationManager.ConnectionStrings[connectionId].ConnectionString;
        }

        public static string ObjectToString<T>(T typeObject) where T : class
        {
            string line = string.Empty;

            foreach (var prop in typeObject.GetType().GetProperties())
            {
                line += string.Format("{1} ",prop.Name, prop.GetValue(typeObject,null));
            }
            return line;
        }
    }
}
