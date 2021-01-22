using EasySales.Model;
using EasySales.Object;
using Quartz;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace EasySales.Job
{
    [DisallowConcurrentExecution]
    public class JobItemTemplateDtlSync : IJob
    {
        public string pad(string val)
        {
            if (val != null)
            {
                return val + " | ";
            }
            return val;
        }

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
                        action_identifier = Constants.Action_ItemTemplateDtlSync,     
                        action_details = Constants.Tbl_cms_package_dtl,           
                        action_failure = 0,
                        action_failure_message = "Item template detail sync is running",
                        action_time = DateTime.Now.ToLongDateString()
                    };

                    DateTime startTime = DateTime.Now;

                    LocalDB.InsertSyncLog(slog);
                    logger.message = "----------------------------------------------------------------------------";
                    logger.Broadcast();
                    logger.message = "Item template detail sync is running";
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
                    string Code, Name, Remark, Price, ItemCode, Qty, Uom;

                    query = "INSERT INTO cms_package_dtl (dtl_parent,dtl_code,dtl_name,dtl_remark,dtl_qty,dtl_uom,dtl_unit_price) VALUES";

                    updateQuery = " ON DUPLICATE KEY UPDATE dtl_name = VALUES(dtl_name), dtl_remark = VALUES(dtl_remark), dtl_qty = VALUES(dtl_qty), dtl_uom = VALUES(dtl_uom), dtl_unit_price=VALUES(dtl_unit_price);";

                    HashSet<string> queryList = new HashSet<string>();

                    dynamic hasUDF = new CheckBackendRule()
                                            .CheckTablesExist()
                                            .GetSettingByTableName("cms_package_dtl");

                    ArrayList combineRemark = new ArrayList();
                    foreach (var include in hasUDF)
                    {
                        dynamic _include = include.include;

                        foreach (var includeChild in _include)
                        {
                            foreach (var prodRemarkChild in includeChild)
                            {
                                dynamic _function = prodRemarkChild.function;
                                dynamic _accounting = prodRemarkChild.accounting;
                                dynamic _separator = prodRemarkChild.separator;

                                foreach (string remarks in _accounting)
                                {
                                    combineRemark.Add(remarks);
                                }
                            }
                        }
                    }

                    lSQL = "SELECT * FROM ST_ITEM_TPLDTL";
                    lDataSet = ComServer.DBManager.NewDataSet(lSQL);

                    lDataSet.First();

                    while (!lDataSet.eof)
                    {
                        RecordCount++;

                        Code = lDataSet.FindField("CODE").AsString;
                        ItemCode = lDataSet.FindField("ITEMCODE").AsString;
                        Name = lDataSet.FindField("DESCRIPTION").AsString;
                        Price = lDataSet.FindField("UNITAMOUNT").AsString;
                        Remark = lDataSet.FindField("REMARK2").AsString;

                        //get udf fields
                        if (combineRemark != null)                        /* GET WITHIN LOOP */
                        {
                            for (int i = 0; i < combineRemark.Count; i++)
                            {
                                string Remarks = combineRemark[i].ToString();
                                Remark += pad(Remarks + ": " + lDataSet.FindField(Remarks).AsString);        
                            }
                        }
                        else
                        {
                            Remark = lDataSet.FindField("REMARK2").AsString;
                        }

                        Qty = lDataSet.FindField("QTY").AsString;
                        Uom = lDataSet.FindField("UOM").AsString;

                        Database.Sanitize(ref Code);
                        Database.Sanitize(ref ItemCode);
                        Database.Sanitize(ref Name);
                        Database.Sanitize(ref Price);
                        Database.Sanitize(ref Remark);

                        string Values = string.Format("('{0}','{1}','{2}','{3}','{4}','{5}','{6}')", Code, ItemCode, Name, Remark, Qty, Uom, Price);

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

                            logger.message = string.Format("{0} item template details records is inserted", RecordCount);
                            logger.Broadcast();
                        }

                        lDataSet.Next();
                    }

                    if (queryList.Count > 0)
                    {
                        query = query + string.Join(", ", queryList) + updateQuery;

                        mysql.Insert(query);
                        //mysql.Close();

                        logger.message = string.Format("{0} item template details records is inserted", RecordCount);
                        logger.Broadcast();
                    }


                    slog.action_identifier = Constants.Action_ItemTemplateDtlSync;
                    slog.action_details = Constants.Tbl_cms_package_dtl + Constants.Is_Finished + string.Format(" ({0}) records", RecordCount);
                    slog.action_failure = 0;
                    slog.action_failure_message = string.Empty;
                    slog.action_time = DateTime.Now.ToString();

                    DateTime endTime = DateTime.Now;
                    TimeSpan ts = endTime - startTime;
                    Console.WriteLine("Completed in: " + ts.TotalSeconds.ToString("F"));

                    LocalDB.InsertSyncLog(slog);

                    logger.message = "Item template detail sync finished in (" + ts.TotalSeconds.ToString("F") + " seconds)";
                    logger.Broadcast();
                });

                thread.Start();
                //thread.Join();

                //await Console.Out.WriteLineAsync("Item Template Detail Sync");
            }
            catch (ThreadAbortException e)
            {
                DpprException ex = new DpprException
                {
                    file_name = "JobItemTemplateDtlSync",
                    exception = e.Message,
                    time = DateTime.Now.ToLongTimeString()
                };
    LocalDB.InsertException(ex);

                Console.WriteLine(Constants.Thread_Exception + e.Message);
            }
        }
    }
}