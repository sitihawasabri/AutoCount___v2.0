using EasySales.Object;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EasySales.Model
{
    public class SQLAccApi
    {
        private static SQLAccApi instance = null;

        bool hasRPCserver = false;
        public Int32 lBuildNo;
        dynamic ComServer;
        Type lBizType;

        public void KillSQLAccounting()
        {
            try
            {
                foreach (Process prc in Process.GetProcessesByName("SQLAcc"))
                {
                    prc.Kill();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public SQLAccApi()
        {
            List<DpprAccountingSoftware> configurationList = LocalDB.GetAccountingSoftwares();
            if(configurationList.Count == 0)
            {
                GlobalLogger logger = new GlobalLogger();
                logger.message = "------------------------NO SQLACCOUNTING SETTINGS FOUND-----------------------";
                logger.Broadcast();
                return;
            }
            DpprAccountingSoftware config = configurationList[0];

        ReLogin:
            KillSQLAccounting();

            Thread.Sleep(1000);

            try
            {
                try
                {
                    lBizType = Type.GetTypeFromProgID("SQLAcc.BizApp");
                    ComServer = Activator.CreateInstance(lBizType);

                    if (!ComServer.IsLogin)
                    {
                        try
                        {
                            ComServer.Login(config.software_username, config.software_password, config.software_link, config.software_db);
                            ComServer.Minimize();
                            hasRPCserver = true;
                        }
                        catch (Exception ex)
                        {
                            DpprException exception = new DpprException()
                            {
                                file_name = "SQLAccounting",
                                exception = ex.Message + " Login Exception",
                                time = DateTime.Now.ToString()
                            };
                            LocalDB.InsertException(exception);
                            FreeBiz(ComServer);
                        }
                    }
                    if (ComServer.IsLogin)
                    {
                        lBuildNo = ComServer.BuildNo;
                        hasRPCserver = true;
                    }
                    else
                    {
                        goto ReLogin;
                    }
                }
                catch (Exception e)
                {
                    DpprException exception = new DpprException()
                    {
                        file_name = "SQLAccounting",
                        exception = e.Message + " Activator Exception",
                        time = DateTime.Now.ToLongDateString() + " " + DateTime.Now.ToLongTimeString()
                    };
                    LocalDB.InsertException(exception);
                }
            }
            catch(Exception e)
            {
                DpprException exception = new DpprException()
                {
                    file_name = "SQLAccounting",
                    exception = e.Message + " SQLAccounting Exception",
                    time = DateTime.Now.ToLongDateString() + " " + DateTime.Now.ToLongTimeString()
                };
                LocalDB.InsertException(exception);
                goto ReLogin;
            }
            bool sameCompany = checkCompany();
            if(!sameCompany)
            {
                KillSQLAccounting();
                goto ReLogin;
            }
        }

        public bool checkCompany()
        {
            GlobalLogger logger = new GlobalLogger();
            List<DpprAccountingSoftware> getAccSoftware = LocalDB.GetAccountingSoftwares();
            DpprAccountingSoftware accSoftware = getAccSoftware[0];
            // check company name
            dynamic RptObject, lDataSet;
            RptObject = ComServer.RptObjects.Find("Common.Agent.RO");
            RptObject.CalculateReport();
            lDataSet = RptObject.DataSets.Find("cdsProfile");
            lDataSet.First();

            string configCompanyName = accSoftware.software_comp;
            string companyName = string.Empty;
            while (!lDataSet.eof)
            {
                companyName = lDataSet.FindField("CompanyName").AsString;
                lDataSet.Next();
            }

            if(companyName != configCompanyName)
            {
                Logout();
                logger.Broadcast("SQL Accounting company name is not same. Kindly check the configuration details.");
                return false;
            }
            return true;
        }

        public void Logout()
        {
            try
            {
                if (ComServer != null && RPCAvailable())
                {
                    ComServer.Logout();
                    FreeBiz(ComServer);
                }
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message); //try again ok
            }
            ComServer = null;
            instance = null;
            KillSQLAccounting();
        }

        public void FreeBiz(object AbizObj)
        {
            System.Runtime.InteropServices.Marshal.ReleaseComObject(AbizObj);
        }

        public static SQLAccApi getInstance()
        {
            if(instance == null)
            {
                instance = new SQLAccApi();
            }
            return instance;
        }

        public static SQLAccApi CurrentState()
        {
            return instance;
        }

        public dynamic GetComServer()
        {
            return ComServer;
        }

        public bool RPCAvailable()
        {
            bool isDead = true;
            foreach (Process prc in Process.GetProcessesByName("SQLAcc"))
            {
                isDead = false;
            }
            if (isDead)
            {
                Logout();
                return false; 
            }
            return hasRPCserver;
        }

        public void Message(string msg, bool error = false, bool show = false)
        {
            DpprException ex = new DpprException
            {
                exception = msg,
                file_name = "SQLAccounting",
                time = DateTime.Now.ToLongTimeString()
            };
            LocalDB.InsertException(ex);
        }
    }
}