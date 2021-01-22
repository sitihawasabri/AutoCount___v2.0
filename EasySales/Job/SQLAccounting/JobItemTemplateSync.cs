using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EasySales.Model;
using EasySales.Object;
using Newtonsoft.Json.Linq;
using Quartz;
using Quartz.Impl;
using Quartz.Logging;

namespace EasySales.Job
{
    [DisallowConcurrentExecution]
    public class JobItemTemplateSync : IJob
    {
        public void Execute()
        {
            this.Run();
        }
        public async Task Execute(IJobExecutionContext context)
        {
            this.Run();
        }

        public void Run()
        {
            try
            {
                Thread thread = new Thread(p =>
                {
                    Thread.CurrentThread.IsBackground = true;

                    int RecordCount = 0;
                    GlobalLogger logger = new GlobalLogger();

                    /**
                     * Here we will run SQLAccounting Codes
                     * */

                    DpprSyncLog slog = new DpprSyncLog
                    {
                        action_identifier = Constants.Action_ItemTemplateSync,      
                        action_details = Constants.Tbl_cms_package,                 
                        action_failure = 0,
                        action_failure_message = "Item template sync is running",
                        action_time = DateTime.Now.ToLongDateString()
                    };

                    DateTime startTime = DateTime.Now;

                    LocalDB.InsertSyncLog(slog);
                    logger.message = "----------------------------------------------------------------------------";
                    logger.Broadcast();
                    logger.message = "Item template sync is running";
                    logger.Broadcast();
                    logger.message = "----------------------------------------------------------------------------";
                    logger.Broadcast();

                CHECKAGAIN:

                    SQLAccApi instance = SQLAccApi.getInstance();

                    dynamic ComServer = instance.GetComServer();

                    if (!instance.RPCAvailable())
                    {
                        goto CHECKAGAIN;
                    }

                    Database mysql = new Database();

                    dynamic lDataSet;
                    string lSQL, query, updateQuery;
                    string Code, Name, Desc, Remark, Price, Active;

                    query = "INSERT INTO cms_package (pkg_code, pkg_name, pkg_desc, pkg_unit_price, pkg_remark, pkg_status) VALUES";

                    updateQuery = " ON DUPLICATE KEY UPDATE pkg_name = VALUES(pkg_name), pkg_desc = VALUES(pkg_desc), pkg_unit_price = VALUES(pkg_unit_price), pkg_remark = VALUES(pkg_remark), pkg_status = VALUES(pkg_status);";

                    HashSet<string> queryList = new HashSet<string>();

                    lSQL = "SELECT * FROM ST_ITEM_TPL;";
                    lDataSet    = ComServer.DBManager.NewDataSet(lSQL);
                    
                    lDataSet.First();

                    while (!lDataSet.eof)
                    {
                        RecordCount++;

                        Code = lDataSet.FindField("CODE").AsString;
                        Name = lDataSet.FindField("DESCRIPTION").AsString;
                        Desc = lDataSet.FindField("DESCRIPTION2").AsString;
                        Remark = lDataSet.FindField("DESCRIPTION3").AsString;
                        Price = lDataSet.FindField("REFPRICE").AsString;
                        Active = lDataSet.FindField("ISACTIVE").AsString;


                        int activeValue = 1;

                        if (Active == "F")
                        {
                             activeValue = 0;
                        }

                        Database.Sanitize(ref Code);
                        Database.Sanitize(ref Name);
                        Database.Sanitize(ref Desc);
                        Database.Sanitize(ref Price);
                        Database.Sanitize(ref Remark);

                        string Values = string.Format("('{0}','{1}','{2}','{3}','{4}','{5}')", Code, Name, Desc, Price, Remark, activeValue);

                        queryList.Add(Values);

                        if (queryList.Count % 2000 == 0)
                        {
                            string tmp_query = query;
                            tmp_query += string.Join(", ", queryList);
                            tmp_query += updateQuery;

                            mysql.Insert(tmp_query);
                            //mysql.Close();

                            queryList.Clear();
                            tmp_query = string.Empty;

                            logger.message = string.Format("{0} item template records is inserted", RecordCount);
                            logger.Broadcast();
                        }

                        lDataSet.Next();
                    }

                    if (queryList.Count > 0)
                    {
                        query = query + string.Join(", ", queryList) + updateQuery;

                        mysql.Insert(query);
                        //mysql.Close();

                        logger.message = string.Format("{0} item template records is inserted", RecordCount);
                        logger.Broadcast();
                    }

                    slog.action_identifier = Constants.Action_ItemTemplateSync; 
                    slog.action_details = Constants.Tbl_cms_package + Constants.Is_Finished + string.Format(" ({0}) records", RecordCount);
                    slog.action_failure = 0;
                    slog.action_failure_message = string.Empty;
                    slog.action_time = DateTime.Now.ToString();

                    DateTime endTime = DateTime.Now;
                    TimeSpan ts = endTime - startTime;
                    Console.WriteLine("Completed in: " + ts.TotalSeconds.ToString("F"));

                    LocalDB.InsertSyncLog(slog);

                    logger.message = "Item template sync finished in (" + ts.TotalSeconds.ToString("F") + " seconds)";
                    logger.Broadcast();
                });

                thread.Start();
                //thread.Join();

                //await Console.Out.WriteLineAsync("Item Template Sync");
            }
            catch (ThreadAbortException e)
            {
                DpprException ex = new DpprException
                {
                    file_name = "JobItemTemplateSync",
                    exception = e.Message,
                    time = DateTime.Now.ToLongTimeString()
                };
                LocalDB.InsertException(ex);

                Console.WriteLine(Constants.Thread_Exception + e.Message);
            }
        }
    }
}