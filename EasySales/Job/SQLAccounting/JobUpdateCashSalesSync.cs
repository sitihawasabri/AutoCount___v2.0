using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EasySales.Model;
using EasySales.Object;
using Quartz;
using Quartz.Impl;
using Quartz.Logging;

namespace EasySales.Job
{
    [DisallowConcurrentExecution]
    public class JobUpdateCashSalesSync : IJob
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

                    DpprSyncLog slog = new DpprSyncLog();
                    slog.action_identifier = Constants.Action_UpdateCashSalesSync;                               /*check again */
                    slog.action_details = Constants.Tbl_cms_invoice + Constants.Is_Starting;                    /*check again */
                    slog.action_failure = 0;
                    slog.action_failure_message = string.Empty;
                    slog.action_time = DateTime.Now.ToString();

                    DateTime startTime = DateTime.Now;

                    LocalDB.InsertSyncLog(slog);
                    logger.message = "----------------------------------------------------------------------------";
                    logger.Broadcast();
                    logger.message = "Update cash sales sync is running";
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

                    Dictionary<string, string> salespersonList = new Dictionary<string, string>();
                    ArrayList salespersonFromDb = mysql.Select("SELECT login_id, staff_code FROM cms_login");

                    for (int i = 0; i < salespersonFromDb.Count; i++)
                    {
                        Dictionary<string, string> each = (Dictionary<string, string>)salespersonFromDb[i];
                        salespersonList.Add(each["staff_code"], each["login_id"]);
                    }
                    salespersonFromDb.Clear();

                    dynamic lRptVar;
                    string ivCode, custCode, ivDate, ivAmt, ivOutstanding, ivCancelled, agent;
                    string query, updateQuery;

                    HashSet<string> queryList = new HashSet<string>();

                    lRptVar = ComServer.DBManager.NewDataSet("SELECT AGENT, DOCNO, CODE, DOCDATE, DOCAMT, P_AMOUNT, CANCELLED FROM SL_CS ORDER BY DOCDATE DESC;");

                    query = "INSERT INTO cms_invoice(invoice_code, cust_code, invoice_date, invoice_amount, outstanding_amount, cancelled, salesperson_id) VALUES "; //updated_at
                    updateQuery = " ON DUPLICATE KEY UPDATE cancelled = VALUES(cancelled), invoice_amount = VALUES(invoice_amount), outstanding_amount = VALUES(outstanding_amount), cust_code = VALUES(cust_code), salesperson_id = VALUES(salesperson_id)"; //updated_at = VALUES(updated_at)

                    lRptVar.First();

                    while (!lRptVar.eof)
                    {
                        RecordCount++;

                        ivCode = lRptVar.FindField("DOCNO").AsString;
                        custCode = lRptVar.FindField("CODE").AsString;
                        ivDate = lRptVar.FindField("DOCDATE").AsString;
                        ivDate = Convert.ToDateTime(ivDate).ToString("yyyy-MM-dd");
                        ivAmt = lRptVar.FindField("DOCAMT").AsString;
                        ivOutstanding = lRptVar.FindField("P_AMOUNT").AsString;
                        ivCancelled = lRptVar.FindField("CANCELLED").AsString;
                        //iv_date		     = DateTime::createFromFormat("d-M-y", iv_date).format("Y-m-d");
                        //ivDate = DateTime::createFromFormat("d/m/Y", iv_date).format("Y/m/d");

                        string _agentId = "0";

                        if (_agentId == "0")
                        {
                            agent = lRptVar.FindField("AGENT").AsString;

                            if (string.IsNullOrEmpty(agent) || !salespersonList.TryGetValue(agent, out _agentId))
                            {
                                _agentId = "0";
                            }
                        }
                        int.TryParse(_agentId, out int agentId);

                        Database.Sanitize(ref ivCode);
                        Database.Sanitize(ref custCode);
                        Database.Sanitize(ref ivDate);
                        Database.Sanitize(ref ivAmt);
                        Database.Sanitize(ref ivOutstanding);
                        Database.Sanitize(ref ivCancelled);

                        string Values = string.Format("('{0}','{1}','{2}','{3}','{4}','{5}','{6}')", ivCode, custCode, ivDate, ivAmt, ivOutstanding, ivCancelled, agentId);

                        queryList.Add(Values);

                        if (queryList.Count % 2000 == 0)
                        {
                            string tmp_query = query;
                            tmp_query += string.Join(", ", queryList);
                            tmp_query += updateQuery;

                            Database _mysql = new Database();
                            _mysql.Insert(tmp_query);
                            //_mysql.Close();

                            queryList.Clear();
                            tmp_query = string.Empty;

                            logger.message = string.Format("{0} update cash sales records is inserted", RecordCount);
                            logger.Broadcast();
                        }

                        lRptVar.Next();
                    }

                    if (queryList.Count > 0)
                    {
                        query = query + string.Join(", ", queryList) + updateQuery;

                        Database _mysql = new Database();
                        _mysql.Insert(query);
                        //_mysql.Close();

                        logger.message = string.Format("{0} update cash sales records is inserted", RecordCount);
                        logger.Broadcast();
                    }

                    slog.action_identifier = Constants.Action_UpdateCashSalesSync;
                    slog.action_details = Constants.Tbl_cms_invoice + Constants.Is_Finished + string.Format(" ({0}) records", RecordCount);
                    slog.action_failure = 0;
                    slog.action_failure_message = string.Empty;
                    slog.action_time = DateTime.Now.ToString();

                    DateTime endTime = DateTime.Now;
                    TimeSpan ts = endTime - startTime;
                    Console.WriteLine("Completed in: " + ts.TotalSeconds.ToString("F"));

                    LocalDB.InsertSyncLog(slog);

                    logger.message = "Update cash sales sync finished in (" + ts.TotalSeconds.ToString("F") + " seconds)";
                    logger.Broadcast();
                });

                thread.Start();
                //thread.Join();
            }
            catch (ThreadAbortException e)
            {
                DpprException ex = new DpprException
                {
                    file_name = "JobCashSalesUpdateSync",
                    exception = e.Message,
                    time = DateTime.Now.ToString()
                };
                LocalDB.InsertException(ex);

                Console.WriteLine(Constants.Thread_Exception + e.Message);
            }
        }
    }
}