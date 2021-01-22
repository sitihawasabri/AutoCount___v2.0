using EasySales.Object;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace EasySales.Model
{
    public class InternetConnection
    {
        public static bool IsConnectedToInternet()
        {
            List<DpprMySQLconfig> list = LocalDB.GetRemoteDatabaseConfig();
            DpprMySQLconfig config = list[0];
            //config.config_host;
            string host = config.config_host;// "www.easysales.asia";
            bool result = false;
            Ping p = new Ping();
            int checkcount = 0;

        CHECKAGAIN:

            try
            {
                checkcount++;
                PingReply reply = p.Send(host, 1000);
                Console.WriteLine(reply.Status);
                Message(reply.Status.ToString());
                if (reply.Status == IPStatus.Success)
                {
                    return true;
                }
                else
                {
                    Console.WriteLine(reply.Status); //TimedOut
                    Message(reply.Status.ToString());
                    Task.Delay(5000); //ping again after 5 seconds
                    if (checkcount < 3)
                    {
                        goto CHECKAGAIN;
                    }
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Message(ex.Message);
                Task.Delay(5000); //ping again after 5 seconds
                if (checkcount < 3)
                {
                    goto CHECKAGAIN;
                }
            }
            return result;
        }

        public static bool PingMSSQL()
        {
            List<DpprSQLServerconfig> list = LocalDB.GetRemoteSQLServerConfig();
            DpprSQLServerconfig config = list[0];

            string host = config.data_source;
            int index = host.LastIndexOf("\\");
            if (index > 0)
                host = host.Substring(0, index);
            bool result = false;
            Ping p = new Ping();
            int checkcount = 0;

        CHECKAGAIN:

            try
            {
                checkcount++;
                PingReply reply = p.Send(host, 500); //0.5 seconds - make sure every thing is inserted in one shot
                Console.WriteLine(reply.Status);
                Message(reply.Status.ToString());
                if (reply.Status == IPStatus.Success)
                {
                    return true;
                }
                else
                {
                    Console.WriteLine(reply.Status); //TimedOut
                    Message(reply.Status.ToString());
                    Task.Delay(5000); //ping again after 5 seconds
                    if (checkcount < 3)
                    {
                        goto CHECKAGAIN;
                    }
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Message(ex.Message);
                Task.Delay(5000); //ping again after 5 seconds
                if (checkcount < 3)
                {
                    goto CHECKAGAIN;
                }
            }
            list.Clear();
            return result;
            //return true;
        }

        public static void Message(string msg, bool error = false, bool show = false)
        {
            DpprException ex = new DpprException
            {
                exception = msg,
                file_name = "PING",
                time = DateTime.Now.ToLongTimeString()
            };
            LocalDB.InsertException(ex);
        }
    }
}
