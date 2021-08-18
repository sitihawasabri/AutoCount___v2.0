using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Diagnostics;
using SocketIOClient;
using EasySales.Object;
using EasySales.Model;
using EasySales.Job;

namespace EasySales
{
    public class SocketClient
    {
        private static string companyName = string.Empty;
        public static async Task Connect()
        {
            //https://github.com/doghappy/socket.io-client-csharp/blob/master/src/SocketIOClient.Sample/Program.cs
            //https://github.com/doghappy/socket.io-client-csharp/blob/master/src/SocketIOClient.Test/SocketIOTests/OnErrorTest.cs

            //var uri = new Uri("https://socket-io.doghappy.wang");
            //txt_socket_address
            List<DpprMySQLconfig> list = LocalDB.GetRemoteDatabaseConfig();
            DpprMySQLconfig config = list[0];
            string socketAddress = config.socket_address; //http://117.53.154.68:3230 

            if (socketAddress != string.Empty)
            {
                var uri = new Uri(socketAddress);

                var socket = new SocketIO(uri, new SocketIOOptions
                {
                    Query = new Dictionary<string, string>
                    {
                        {"token", "io" }
                    },
                    ConnectionTimeout = TimeSpan.FromSeconds(10000),
                    EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls
                });

                socket.OnConnected += Socket_OnConnected;
                socket.OnDisconnected += Socket_OnDisconnected;
                socket.OnReconnecting += Socket_OnReconnecting;
                await socket.ConnectAsync();

                List<DpprMySQLconfig> mysql_list = LocalDB.GetRemoteDatabaseConfig().Distinct().ToList();
                DpprMySQLconfig mysql_config = mysql_list[0];
                string _companyName = mysql_config.config_database;
                companyName = _companyName.ReplaceAll("", "easysale_");
                companyName = companyName.ReplaceAll("", "easyicec_");

                if (companyName == "ibeauty")
                {
                    companyName = "insidergroup";
                }
                else
                {
                    companyName = companyName;
                }

                socket.OnReceivedEvent += (sender, e) =>
                {
                    string response = e.Response.GetValue<string>();
                    Message("OnReceivedEvent:" + e.Event);
                    Message(response);
                };

            RePing:
                DateTime startTime = DateTime.Now;
                Console.WriteLine("Start Time: " + startTime);
                await Task.Delay(60000);
                try
                {
                    Console.WriteLine("PING");
                    DateTime endTime = DateTime.Now;
                    Console.WriteLine("End Time: " + endTime);
                    TimeSpan ts = endTime - startTime;
                    Console.WriteLine("Time: " + ts);
                    await socket.EmitAsync("alive", new
                    {
                        client_company = companyName,
                        app_type = "exe",
                        login_id = 0
                    });
                    goto RePing;
                }
                catch (Exception ex)
                {
                    Message("On ping: " + ex.Message);
                }
            }
            //Console.ReadLine();
        }

        private static void Socket_OnReconnecting(object sender, int e)
        {
            //Console.WriteLine($"Reconnecting: attempt = {e}");
            Message($"Reconnecting: attempt = {e}");
        }

        private static void Socket_OnDisconnected(object sender, string e)
        {
            //Console.WriteLine("disconnect: " + e);
            Message("disconnect: " + e);
        }

        private static async void Socket_OnConnected(object sender, EventArgs e)
        {
            GlobalLogger logger = new GlobalLogger();
            var socket = sender as SocketIO;
            //Console.WriteLine("Socket_OnConnected");
            //Console.WriteLine("Socket.Id:" + socket.Id);

            //Message("Socket_OnConnected");
            //Message("Socket.Id:" + socket.Id);
            bool ATCSDKEnabled = false;
            List<DpprAccountingSoftware> getAccSoftware = LocalDB.GetAccountingSoftwares();
            DpprAccountingSoftware accSoftware = getAccSoftware[0];

            List<DpprUserSettings> list = LocalDB.GetUserSettings();
            for (int i = 0; i < list.Count; i++)
            {
                DpprUserSettings settings = list[i];
                if (settings.name == Constants.Setting_TransferSO_SDK_Enable)
                {
                    ATCSDKEnabled = settings.setting == Constants.YES;
                }
            }

            socket.On("order", response =>
            {
                string orderId = response.GetValue<string>();
                //Console.WriteLine("order: response");
                //Console.WriteLine(orderId);

                Console.WriteLine("order: response");
                Console.WriteLine(orderId);

                if (accSoftware.software_name == "AutoCount")
                {
                    if (ATCSDKEnabled) /* transfer via SDK */
                    {
                        new JobATCTransferSOSDK().ExecuteSocket(orderId);
                    }
                    else /* transfer directly to DB */
                    {
                        new JobATCTransferSO().ExecuteSocket(orderId);
                    }
                }        
            });
        }

        public static void Message(string msg)
        {
            DpprException ex = new DpprException
            {
                exception = msg,
                file_name = "socket_client",
                time = DateTime.Now.ToString()//DateTime.Now.ToLongTimeString()
            };
            LocalDB.InsertException(ex);
        }
    }
}