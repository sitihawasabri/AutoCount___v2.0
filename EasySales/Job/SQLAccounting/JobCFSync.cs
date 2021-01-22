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
    public class JobCFSync : IJob
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

                    DpprSyncLog slog = new DpprSyncLog();
                    slog.action_identifier = Constants.Action_CustomerRefundSync;                      
                    slog.action_details = Constants.Tbl_cms_customer_refund + Constants.Is_Starting;    
                    slog.action_failure = 0;
                    slog.action_failure_message = string.Empty;
                    slog.action_time = DateTime.Now.ToString();

                    DateTime startTime = DateTime.Now;

                    LocalDB.InsertSyncLog(slog);
                    logger.message = "----------------------------------------------------------------------------";
                    logger.Broadcast();
                    logger.message = "Customer refund sync is running";
                    logger.Broadcast();
                    logger.message = "----------------------------------------------------------------------------";
                    logger.Broadcast();

                /**
                * Here we will run SQLAccounting Codes
                * */

                CHECKAGAIN:
                    SQLAccApi instance = SQLAccApi.getInstance();

                    dynamic ComServer = instance.GetComServer();

                    if (!instance.RPCAvailable())
                    {
                        goto CHECKAGAIN;
                    }

                    dynamic lDataSet;
                    string lSQL, DOCNO, CODE, SALESPERSON, KOAMT, DOCDATE, DOCAMT, CANCELLED;
                    string query, updateQuery;

                    HashSet<string> queryList = new HashSet<string>();
                    Database mysql = new Database();

                    Dictionary<string, string> cms_updated_time = mysql.GetUpdatedTime1YearInterval("cms_customer_refund"); //{[updated_at, 28/06/2020 5:38:23 PM]}
                    string updated_at = string.Empty;

                    if (cms_updated_time.Count > 0)
                    {
                        updated_at = cms_updated_time["updated_at"].ToString().MSSQLdate();
                    }

                    string getActiveCF = "SELECT cf_code FROM cms_customer_refund WHERE cancelled = 'F'";
                    if (cms_updated_time.Count > 0)
                    {
                        getActiveCF += " AND cf_date >= '" + updated_at + "'";
                    }
                    ArrayList inDBactivecf = mysql.Select(getActiveCF);

                    logger.Broadcast("Active customer refund in DB: " + inDBactivecf.Count);
                    ArrayList inDBcf = new ArrayList();
                    for (int i = 0; i < inDBactivecf.Count; i++)
                    {
                        Dictionary<string, string> each = (Dictionary<string, string>)inDBactivecf[i];
                        string cfCode = each["cf_code"].ToString();
                        if (!inDBcf.Contains(cfCode))
                        {
                            inDBcf.Add(cfCode);
                        }
                    }
                    inDBactivecf.Clear();

                    Dictionary<string, string> salespersonList = new Dictionary<string, string>();
                    ArrayList salespersonFromDb = mysql.Select("SELECT login_id, staff_code FROM cms_login");

                    for (int i = 0; i < salespersonFromDb.Count; i++)
                    {
                        Dictionary<string, string> each = (Dictionary<string, string>)salespersonFromDb[i];
                        salespersonList.Add(each["staff_code"], each["login_id"]);
                        /*using key (staff_code) to get value (login_id) */
                    }
                    salespersonFromDb.Clear();

                    //lSQL = "SELECT AR_CF.DOCNO, AR_CF.AGENT, AR_CF.CODE, KO.KOAMT, AR_CF.DOCDATE, AR_CF.DOCAMT, AR_CF.CANCELLED FROM AR_CF LEFT JOIN(SELECT SUM(AR_KNOCKOFF.KOAMT) AS KOAMT,AR_KNOCKOFF.FROMDOCKEY FROM AR_KNOCKOFF WHERE AR_KNOCKOFF.FROMDOCTYPE = 'CF' GROUP BY AR_KNOCKOFF.FROMDOCKEY) KO ON KO.FROMDOCKEY = AR_CF.DOCKEY;";
                    lSQL = "SELECT AR_CF.DOCNO, AR_CF.AGENT, AR_CF.CODE, KO.KOAMT, AR_CF.DOCDATE, AR_CF.DOCAMT, AR_CF.LOCALDOCAMT, AR_CF.CANCELLED FROM AR_CF LEFT JOIN(SELECT SUM(AR_KNOCKOFF.ACTUALLOCALKOAMT) AS KOAMT,AR_KNOCKOFF.FROMDOCKEY FROM AR_KNOCKOFF WHERE AR_KNOCKOFF.FROMDOCTYPE = 'CF' GROUP BY AR_KNOCKOFF.FROMDOCKEY) KO ON KO.FROMDOCKEY = AR_CF.DOCKEY ";
                    if (cms_updated_time.Count > 0)
                    {
                        lSQL += " WHERE AR_CF.DOCDATE >='" + updated_at + "' ORDER BY DOCKEY ASC";
                    }
                    else
                    {
                        lSQL += " ORDER BY DOCKEY ASC";
                    }

                    lDataSet = ComServer.DBManager.NewDataSet(lSQL);

                    query = "INSERT INTO cms_customer_refund(cf_code, cust_code, cf_knockoff_amount, cf_date, cf_amount, cancelled, salesperson_id) VALUES ";
                    updateQuery = " ON DUPLICATE KEY UPDATE cust_code = VALUES(cust_code), cancelled = VALUES(cancelled), cf_amount = VALUES(cf_amount),cf_knockoff_amount = VALUES(cf_knockoff_amount), cf_date = VALUES(cf_date), salesperson_id = VALUES(salesperson_id)";
                    lDataSet.First();

                    while (!lDataSet.eof)
                    {
                        RecordCount++;

                        DOCNO = lDataSet.FindField("DOCNO").AsString;

                        if (inDBcf.Contains(DOCNO))
                        {
                            int index = inDBcf.IndexOf(DOCNO);
                            if (index != -1)
                            {
                                inDBcf.RemoveAt(index);
                            }
                        }

                        CODE = lDataSet.FindField("CODE").AsString;
                        KOAMT = lDataSet.FindField("KOAMT").AsString;   //ACTUALLOCALKOAMT

                        if (KOAMT == null)
                        {
                            KOAMT = string.Empty;
                        }

                        DOCDATE = lDataSet.FindField("DOCDATE").AsString;
                        DOCDATE = Convert.ToDateTime(DOCDATE).ToString("yyyy-MM-dd");
                        DOCAMT = lDataSet.FindField("LOCALDOCAMT").AsString;            //LOCALDOCAMT
                        CANCELLED = lDataSet.FindField("CANCELLED").AsString;
                        SALESPERSON = lDataSet.FindField("AGENT").AsString;

                        string _SALESPERSONID = "0";

                        if (_SALESPERSONID == "0")
                        {
                            SALESPERSON = lDataSet.FindField("AGENT").AsString;
                            SALESPERSON = SALESPERSON.ToUpper();

                            if (string.IsNullOrEmpty(SALESPERSON) || !salespersonList.TryGetValue(SALESPERSON, out _SALESPERSONID))
                            {
                                _SALESPERSONID = "0";
                            }
                        }
                        int.TryParse(_SALESPERSONID, out int SALESPERSONID);

                        Database.Sanitize(ref DOCNO);
                        Database.Sanitize(ref CODE);
                        Database.Sanitize(ref KOAMT);
                        Database.Sanitize(ref DOCDATE);
                        Database.Sanitize(ref DOCAMT);
                        Database.Sanitize(ref CANCELLED);
                                                                                                
                        string Values = string.Format("('{0}','{1}','{2}','{3}','{4}','{5}','{6}')", DOCNO, CODE, KOAMT, DOCDATE, DOCAMT, CANCELLED, SALESPERSONID);

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

                            logger.message = string.Format("{0} customer refund records is inserted", RecordCount);
                            logger.Broadcast();
                        }

                        lDataSet.Next();
                    }

                    if (queryList.Count > 0)
                    {
                        query = query + string.Join(", ", queryList) + updateQuery;

                        mysql.Insert(query);

                        logger.message = string.Format("{0} customer refund records is inserted", RecordCount);
                        logger.Broadcast();
                    }

                    if (inDBcf.Count > 0)
                    {
                        logger.Broadcast("Total customer refund records to be deactivated: " + inDBcf.Count);

                        HashSet<string> deactivate = new HashSet<string>();
                        for (int i = 0; i < inDBcf.Count; i++)
                        {
                            string _code = inDBcf[i].ToString();
                            deactivate.Add(_code);
                        }

                        string ToBeDeactivate = "'" + string.Join("','", deactivate) + "'";
                        Console.WriteLine(ToBeDeactivate);

                        string inactive = "UPDATE cms_customer_refund SET cancelled = 'T' WHERE cf_code IN (" + ToBeDeactivate + ")";
                        mysql.Insert(inactive);

                        logger.Broadcast(inDBcf.Count + " customer refund records deactivated");

                        inDBcf.Clear();
                    }

                    mysql.Insert("INSERT INTO cms_update_time(table_name, updated_at) VALUES ('cms_customer_refund', NOW()) ON DUPLICATE KEY UPDATE table_name = VALUES(table_name), updated_at = VALUES(updated_at)");

                    DateTime endTime = DateTime.Now;
                    TimeSpan ts = endTime - startTime;
                    Console.WriteLine("Completed in: " + ts.TotalSeconds.ToString("F"));

                    logger.message = "Customer Refund sync finished in (" + ts.TotalSeconds.ToString("F") + " seconds)";
                    logger.Broadcast();

                    DateTime startTimeCT = DateTime.Now;
                    logger.message = "----------------------------------------------------------------------------";
                    logger.Broadcast();
                    logger.message = "Customer contra local sync is running";
                    logger.Broadcast();
                    logger.message = "----------------------------------------------------------------------------";
                    logger.Broadcast();

                    Dictionary<string, string> cms_updated_time_ct = mysql.GetUpdatedTime1YearInterval("cms_customer_contra_local"); //{[updated_at, 28/06/2020 5:38:23 PM]}
                    string updated_at_ct = string.Empty;

                    if (cms_updated_time_ct.Count > 0)
                    {
                        updated_at_ct = cms_updated_time_ct["updated_at"].ToString().MSSQLdate();
                    }

                    string getActiveCT = "SELECT ct_code FROM cms_customer_contra_local WHERE cancelled = 'F'";
                    if (cms_updated_time_ct.Count > 0)
                    {
                        getActiveCT += " AND ct_date >= '" + updated_at + "'";
                    }
                    ArrayList inDBactivect = mysql.Select(getActiveCT);

                    logger.Broadcast("Active customer contra local in DB: " + inDBactivect.Count);
                    ArrayList inDBct = new ArrayList();
                    for (int i = 0; i < inDBactivect.Count; i++)
                    {
                        Dictionary<string, string> each = (Dictionary<string, string>)inDBactivect[i];
                        string ctCode = each["ct_code"].ToString();
                        if (!inDBct.Contains(ctCode))
                        {
                            inDBct.Add(ctCode);
                        }
                    }
                    inDBactivect.Clear();

                    RecordCount = 0;

                    dynamic lDataSetCT;
                    string lSQL2, queryCT, updateQueryCT;
                    string CT_DOCNO, CT_CODE, CT_DOCDATE, CT_LOCALDOCAMT, CT_UNAPPLIEDAMT, CT_CANCELLED, CT_AGENT;

                    HashSet<string> queryListCT = new HashSet<string>();

                    lSQL2 = "SELECT DOCNO,CODE,DOCDATE,LOCALDOCAMT,UNAPPLIEDAMT,CANCELLED,AGENT FROM AR_CT ";
                    if (cms_updated_time_ct.Count > 0)
                    {
                        lSQL += " WHERE DOCDATE >='" + updated_at_ct + "' ORDER BY DOCKEY ASC";
                    }
                    else
                    {
                        lSQL += " ORDER BY DOCKEY ASC";
                    }

                    lDataSetCT = ComServer.DBManager.NewDataSet(lSQL2);

                    queryCT = "INSERT INTO cms_customer_contra_local(ct_code, cust_code, ct_unapplied_amount, ct_date, ct_amount, cancelled, salesperson_id) VALUES ";
                    updateQueryCT = " ON DUPLICATE KEY UPDATE cust_code = VALUES(cust_code), cancelled = VALUES(cancelled), ct_amount = VALUES(ct_amount), ct_unapplied_amount = VALUES(ct_unapplied_amount), ct_date = VALUES(ct_date), salesperson_id = VALUES(salesperson_id)";
                    lDataSetCT.First();

                    while (!lDataSetCT.eof)
                    {
                        RecordCount++;

                        CT_DOCNO = lDataSetCT.FindField("DOCNO").AsString;

                        if (inDBct.Contains(CT_DOCNO))
                        {
                            int index = inDBct.IndexOf(CT_DOCNO);
                            if (index != -1)
                            {
                                inDBct.RemoveAt(index);
                            }
                        }

                        CT_CODE = lDataSetCT.FindField("CODE").AsString;
                        CT_DOCDATE = lDataSetCT.FindField("DOCDATE").AsString;
                        CT_DOCDATE = Convert.ToDateTime(CT_DOCDATE).ToString("yyyy-MM-dd");
                        CT_LOCALDOCAMT = lDataSetCT.FindField("LOCALDOCAMT").AsString;
                        CT_UNAPPLIEDAMT = lDataSetCT.FindField("UNAPPLIEDAMT").AsString;
                        CT_CANCELLED = lDataSetCT.FindField("CANCELLED").AsString;
                        CT_AGENT = lDataSetCT.FindField("AGENT").AsString;

                        string _SALESPERSONID = "0";

                        if (_SALESPERSONID == "0")
                        {
                            CT_AGENT = lDataSetCT.FindField("AGENT").AsString;
                            CT_AGENT = CT_AGENT.ToUpper();

                            if (string.IsNullOrEmpty(CT_AGENT) || !salespersonList.TryGetValue(CT_AGENT, out _SALESPERSONID))
                            {
                                _SALESPERSONID = "0";
                            }
                        }
                        int.TryParse(_SALESPERSONID, out int SALESPERSONID);

                        Database.Sanitize(ref CT_DOCNO);
                        Database.Sanitize(ref CT_CODE);
                        Database.Sanitize(ref CT_DOCDATE);
                        Database.Sanitize(ref CT_LOCALDOCAMT);
                        Database.Sanitize(ref CT_UNAPPLIEDAMT);
                        Database.Sanitize(ref CT_CANCELLED);
                                                                                                    //(ct_code, cust_code, ct_unapplied_amount, ct_date, ct_amount, cancelled, salesperson_id)
                        string Values = string.Format("('{0}','{1}','{2}','{3}','{4}','{5}','{6}')", CT_DOCNO, CT_CODE, CT_UNAPPLIEDAMT, CT_DOCDATE, CT_LOCALDOCAMT, CT_CANCELLED, SALESPERSONID);

                        queryListCT.Add(Values);

                        if (queryListCT.Count % 2000 == 0)
                        {
                            string tmp_query = queryCT;
                            tmp_query += string.Join(", ", queryListCT);
                            tmp_query += updateQueryCT;

                            mysql.Insert(tmp_query);
                            //mysql.Close();

                            queryListCT.Clear();
                            tmp_query = string.Empty;

                            logger.message = string.Format("{0} customer contra local records is inserted", RecordCount);
                            logger.Broadcast();
                        }

                        lDataSetCT.Next();
                    }

                    if (queryListCT.Count > 0)
                    {
                        queryCT = queryCT + string.Join(", ", queryListCT) + updateQueryCT;

                        mysql.Insert(queryCT);

                        logger.message = string.Format("{0} customer contra local records is inserted", RecordCount);
                        logger.Broadcast();
                    }

                    if (inDBct.Count > 0)
                    {
                        logger.Broadcast("Total customer contra local records to be deactivated: " + inDBct.Count);

                        HashSet<string> deactivate = new HashSet<string>();
                        for (int i = 0; i < inDBct.Count; i++)
                        {
                            string _code = inDBct[i].ToString();
                            deactivate.Add(_code);
                        }

                        string ToBeDeactivate = "'" + string.Join("','", deactivate) + "'";
                        Console.WriteLine(ToBeDeactivate);

                        string inactive = "UPDATE cms_customer_contra_local SET cancelled = 'T' WHERE ct_code IN (" + ToBeDeactivate + ")";
                        mysql.Insert(inactive);

                        logger.Broadcast(inDBct.Count + " customer contra local records deactivated");

                        inDBct.Clear();
                    }

                    mysql.Insert("INSERT INTO cms_update_time(table_name, updated_at) VALUES ('cms_customer_contra_local', NOW()) ON DUPLICATE KEY UPDATE table_name = VALUES(table_name), updated_at = VALUES(updated_at)");


                    slog.action_identifier = Constants.Action_CustomerRefundSync;
                    slog.action_details = Constants.Tbl_cms_customer_refund + Constants.Is_Finished + string.Format(" ({0}) records", RecordCount);
                    slog.action_failure = 0;
                    slog.action_failure_message = string.Empty;
                    slog.action_time = DateTime.Now.ToString();

                    LocalDB.InsertSyncLog(slog);

                    DateTime endTimeCT = DateTime.Now;
                    TimeSpan tsCT = endTimeCT - startTimeCT;

                    logger.message = "Customer Contra Local sync finished in (" + tsCT.TotalSeconds.ToString("F") + " seconds)";
                    logger.Broadcast();
                });

                thread.Start();
                //thread.Join();
            }
            catch (ThreadAbortException e)
            {
                DpprException ex = new DpprException
                {
                    file_name = "JobCFSync [CF && CT]",
                    exception = e.Message,
                    time = DateTime.Now.ToString()
                };
                LocalDB.InsertException(ex);

                Console.WriteLine(Constants.Thread_Exception + e.Message);
            }
        }
    }
}