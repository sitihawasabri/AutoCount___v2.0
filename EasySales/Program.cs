using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using EasySales.Object;
using EasySales.Model;
using SocketIOClient;
using System.Net.Mail;

namespace EasySales
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]

        
        static void Main()
        {
            //Application.EnableVisualStyles();
            //Application.Run(new SQLInterface());

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Application.ThreadException += new ThreadExceptionEventHandler(Application_ThreadException);
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);

            List<object> config = LocalDB.Execute("SELECT * FROM accounting_software");

            if (config.Count == 0)
            {
                Application.Run(new MainActivity());
            }
            else
            {
                Application.Run(new DashboardActivity());
            }
        }

        static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
        {
            //MessageBox.Show(e.Exception.Message, "Unhandled Thread Exception");
            //MessageBox.Show(e.Exception.Message, DateTime.Now.ToString() + " [Unhandled Thread Exception]", MessageBoxButtons.OK);
            // here you can log the exception ...

            sendAlert("Unhandled Thread Exception", e.Exception.Message);

            DpprException ex = new DpprException
            {
                file_name = "Unhandled Thread Exception",
                exception = e.Exception.Message,
                time = DateTime.Now.ToString()
            };
            LocalDB.InsertException(ex);
        }

        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            //MessageBox.Show((e.ExceptionObject as Exception).Message, "Unhandled UI Exception");
            //MessageBox.Show((e.ExceptionObject as Exception).Message, DateTime.Now.ToString() + " [Unhandled UI Exception]");
            // here you can log the exception ...
            sendAlert("Unhandled UI Exception", (e.ExceptionObject as Exception).Message);

            DpprException ex = new DpprException
            {
                file_name = "Unhandled UI Exception",
                exception = (e.ExceptionObject as Exception).Message,
                time = DateTime.Now.ToString()
            };
            LocalDB.InsertException(ex);
        }

        static void sendAlert(string exceptionName, string exceptionMsg)
        {
            List<DpprMySQLconfig> mysql_list = LocalDB.GetRemoteDatabaseConfig().Distinct().ToList();
            string companyName = string.Empty;
            DpprMySQLconfig mysql_config = mysql_list[0];
            string _companyName = mysql_config.config_database;
            companyName = _companyName.ReplaceAll("", "easysale_");

            string msg = "[" + companyName + "]: " + "----> crashed at " + DateTime.Now.ToString() + "\nDetails: "+ exceptionName + " --> " + exceptionMsg;
            try
            {
                MailMessage mail = new MailMessage();
                SmtpClient SmtpServer = new SmtpClient("smtp.gmail.com");

                mail.From = new MailAddress("sidzerone01@gmail.com");
                mail.To.Add("sitihawamohdsabri11@gmail.com");
                mail.Subject = "EXE CRASH";
                mail.Body = msg;

                SmtpServer.Port = 587;
                SmtpServer.UseDefaultCredentials = false;
                SmtpServer.Credentials = new System.Net.NetworkCredential("sidzerone01@gmail.com", "siddrive28");
                SmtpServer.EnableSsl = true;
                SmtpServer.Send(mail);
            }
            catch (Exception exc)
            {
                DpprException ex = new DpprException
                {
                    file_name = "Alert Failed",
                    exception = "Failed to send alert email: " + exc.Message,
                    time = DateTime.Now.ToString()
                };
                LocalDB.InsertException(ex);
            }
        }
    }
}
