using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Documents;
using System.Windows.Forms;
using EasySales.Model;
using EasySales.Object;
using Quartz;
using Quartz.Impl;
using Quartz.Logging;

namespace EasySales.Job
{
    [DisallowConcurrentExecution]
    public class JobInvoiceSalesSync : IJob
    {
        public string RtfToPlainText(string rtf)
        {
            var flowDocument = new FlowDocument();
            var textRange = new TextRange(flowDocument.ContentStart, flowDocument.ContentEnd);

            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(rtf ?? string.Empty)))
            {
                textRange.Load(stream, DataFormats.Rtf);
            }

            //Console.WriteLine(textRange.Text);


            return textRange.Text;
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

                    DpprSyncLog slog = new DpprSyncLog();
                    slog.action_identifier = Constants.Action_InvoiceSync;
                    slog.action_details = Constants.Tbl_cms_invoice_sales + Constants.Is_Starting;
                    slog.action_failure = 0;
                    slog.action_failure_message = string.Empty;
                    slog.action_time = DateTime.Now.ToString();

                    DateTime startTime = DateTime.Now;

                    LocalDB.InsertSyncLog(slog);
                    logger.message = "----------------------------------------------------------------------------";
                    logger.Broadcast();
                    logger.message = "Sales Invoice & Sales Cash Sales sync is running";
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

                    dynamic lDataSet, lDataSet2;
                    string lSQLIV, lSQLCS, DOCNO, CODE, DOCDATE, DOCAMT, CANCELLED, AGENT, CURRENCYRATE;
                    string query, updateQuery;

                    Database _mysql = new Database();

                    HashSet<string> queryList = new HashSet<string>();
                    HashSet<string> queryList1 = new HashSet<string>();

                    Dictionary<string, string> cms_updated_time = _mysql.GetUpdatedTime1YearInterval("cms_invoice_sales"); //{[updated_at, 28/06/2020 5:38:23 PM]}
                    string updated_at = string.Empty;

                    if (cms_updated_time.Count > 0)
                    {
                        updated_at = cms_updated_time["updated_at"].ToString().MSSQLdate();
                    }

                    Dictionary<string, string> salespersonList = new Dictionary<string, string>();
                    ArrayList salespersonFromDb = _mysql.Select("SELECT login_id, staff_code FROM cms_login");

                    for (int i = 0; i < salespersonFromDb.Count; i++)
                    {
                        Dictionary<string, string> each = (Dictionary<string, string>)salespersonFromDb[i];
                        salespersonList.Add(each["staff_code"], each["login_id"]);
                        /*using key (staff_code) to get value (login_id) */
                    }
                    salespersonFromDb.Clear();

                    string getActiveInv = "SELECT invoice_code FROM cms_invoice_sales WHERE cancelled = 'F'";
                    if (cms_updated_time.Count > 0)
                    {
                        getActiveInv += " AND invoice_date >= '" + updated_at + "'";
                    }

                    ArrayList inDBactiveinvoice = _mysql.Select(getActiveInv);

                    logger.Broadcast("Active invoice in DB: " + inDBactiveinvoice.Count);
                    ArrayList inDBinvoice = new ArrayList();
                    for (int i = 0; i < inDBactiveinvoice.Count; i++)
                    {
                        Dictionary<string, string> each = (Dictionary<string, string>)inDBactiveinvoice[i];
                        inDBinvoice.Add(each["invoice_code"].ToString());
                    }
                    inDBactiveinvoice.Clear();

                    //[TESTING] lSQLIV = "SELECT DOCNO,CODE,DOCDATE,DOCAMT,LOCALDOCAMT,CANCELLED,AGENT,CURRENCYCODE,CURRENCYRATE, CAST(NOTE AS VARCHAR(2000)) AS NOTEBLOB FROM SL_IV WHERE AGENT != '' ";


                    lSQLIV = "SELECT DOCNO,CODE,DOCDATE,DOCAMT,LOCALDOCAMT,CANCELLED,AGENT,CURRENCYCODE,CURRENCYRATE FROM SL_IV WHERE AGENT != '' ";
                    lSQLCS = "SELECT DOCNO,CODE,DOCDATE,DOCAMT,LOCALDOCAMT,CANCELLED,AGENT,CURRENCYCODE,CURRENCYRATE FROM SL_CS WHERE AGENT != '' ";

                    if (cms_updated_time.Count > 0)
                    {
                        lSQLIV += " AND DOCDATE >= '" + updated_at + "'";
                        lSQLCS += " AND DOCDATE >= '" + updated_at + "'";
                    }

                    try
                    {
                        lDataSet = ComServer.DBManager.NewDataSet(lSQLIV);

                        query = "INSERT INTO cms_invoice_sales(invoice_code,cust_code, invoice_date, invoice_amount, outstanding_amount, cancelled, salesperson_id) VALUES ";
                        updateQuery = " ON DUPLICATE KEY UPDATE cancelled = VALUES(cancelled), invoice_amount = VALUES(invoice_amount), outstanding_amount = VALUES(outstanding_amount), updated_at = VALUES(updated_at), cust_code = VALUES(cust_code), invoice_date = VALUES(invoice_date), salesperson_id=VALUES(salesperson_id);";

                        lDataSet.First();

                        while (!lDataSet.eof)
                        {
                            RecordCount++;

                            DOCNO = lDataSet.FindField("DOCNO").AsString;
                            
                            if (inDBinvoice.Contains(DOCNO))
                            {
                                int index = inDBinvoice.IndexOf(DOCNO);
                                if (index != -1)
                                {
                                    inDBinvoice.RemoveAt(index);
                                }
                            }

                            CODE = lDataSet.FindField("CODE").AsString;
                            DOCDATE = lDataSet.FindField("DOCDATE").AsString;
                            DOCDATE = Convert.ToDateTime(DOCDATE).ToString("yyyy-MM-dd");
                            DOCAMT = lDataSet.FindField("LOCALDOCAMT").AsString;            //LOCALDOCAMT
                            double.TryParse(DOCAMT, out double docAmt);

                            CANCELLED = lDataSet.FindField("CANCELLED").AsString;

                            string _AGENTID = "0";

                            if (_AGENTID == "0")
                            {
                                AGENT = lDataSet.FindField("AGENT").AsString;
                                AGENT = AGENT.ToUpper();

                                if (string.IsNullOrEmpty(AGENT) || !salespersonList.TryGetValue(AGENT, out _AGENTID))
                                {
                                    _AGENTID = "0";
                                }
                            }
                            int.TryParse(_AGENTID, out int AGENTID);

                            Database.Sanitize(ref DOCNO);
                            Database.Sanitize(ref CODE);
                            Database.Sanitize(ref DOCDATE);
                            Database.Sanitize(ref DOCAMT);
                            Database.Sanitize(ref CANCELLED);

                            string Values = string.Format("('{0}','{1}','{2}','{3}','{4}','{5}','{6}')", DOCNO, CODE, DOCDATE, docAmt, 0, CANCELLED, AGENTID); //outstanding_amt put 0 to all

                            queryList.Add(Values);

                            if (queryList.Count % 2000 == 0)
                            {
                                string tmp_query = query;
                                tmp_query += string.Join(", ", queryList);
                                tmp_query += updateQuery;

                                Database mysql = new Database();
                                mysql.Insert(tmp_query);

                                queryList.Clear();
                                tmp_query = string.Empty;

                                logger.message = string.Format("{0} sales invoice records is inserted", RecordCount);
                                logger.Broadcast();
                            }

                            lDataSet.Next();
                        }

                        if (queryList.Count > 0)
                        {
                            //query = query + string.Join(", ", queryList) + updateQuery;

                            //Database mysql = new Database();
                            //mysql.Insert(query);

                            string tmp_query = query;
                            tmp_query += string.Join(", ", queryList);
                            tmp_query += updateQuery;

                            Database mysql = new Database();
                            mysql.Insert(tmp_query);

                            queryList.Clear();
                            tmp_query = string.Empty;

                            logger.message = string.Format("{0} sales invoice records is inserted", RecordCount);
                            logger.Broadcast();
                        }

                        /* sync cash sales */
                        Console.WriteLine(lSQLCS);
                        lDataSet2 = ComServer.DBManager.NewDataSet(lSQLCS);

                        lDataSet2.First();

                        while (!lDataSet2.eof)
                        {
                            RecordCount++;

                            DOCNO = lDataSet2.FindField("DOCNO").AsString;

                            if (inDBinvoice.Contains(DOCNO))
                            {
                                int index = inDBinvoice.IndexOf(DOCNO);
                                if (index != -1)
                                {
                                    inDBinvoice.RemoveAt(index);
                                }
                            }

                            CODE = lDataSet2.FindField("CODE").AsString;
                            DOCDATE = lDataSet2.FindField("DOCDATE").AsString;
                            DOCDATE = Convert.ToDateTime(DOCDATE).ToString("yyyy-MM-dd");
                            DOCAMT = lDataSet2.FindField("LOCALDOCAMT").AsString;           //LOCALDOCAMT
                            double.TryParse(DOCAMT, out double docAmt);

                            CANCELLED = lDataSet2.FindField("CANCELLED").AsString;

                            CURRENCYRATE = lDataSet2.FindField("CURRENCYRATE").AsString;
                            double.TryParse(CURRENCYRATE, out double currencyRate);

                            if (currencyRate != 1)
                            {
                                docAmt = docAmt * currencyRate;
                            }

                            string _AGENTID = "0";

                            if (_AGENTID == "0")
                            {
                                AGENT = lDataSet2.FindField("AGENT").AsString;
                                AGENT = AGENT.ToUpper();

                                if (string.IsNullOrEmpty(AGENT) || !salespersonList.TryGetValue(AGENT, out _AGENTID))
                                {
                                    _AGENTID = "0";
                                }
                            }
                            int.TryParse(_AGENTID, out int AGENTID);

                            Database.Sanitize(ref DOCNO);
                            Database.Sanitize(ref CODE);
                            Database.Sanitize(ref DOCDATE);
                            Database.Sanitize(ref DOCAMT);
                            Database.Sanitize(ref CANCELLED);

                            string Values = string.Format("('{0}','{1}','{2}','{3}','{4}','{5}','{6}')", DOCNO, CODE, DOCDATE, docAmt, 0, CANCELLED, AGENTID);

                            queryList1.Add(Values);

                            if (queryList1.Count % 2000 == 0)
                            {
                                string tmp_query = query;
                                tmp_query += string.Join(", ", queryList1);
                                tmp_query += updateQuery;

                                Database mysql = new Database();
                                mysql.Insert(tmp_query);

                                queryList1.Clear();
                                tmp_query = string.Empty;

                                logger.message = string.Format("{0} sales cash sales records is inserted", RecordCount);
                                logger.Broadcast();
                            }

                            lDataSet2.Next();
                        }

                        if (queryList1.Count > 0)
                        {
                            query = query + string.Join(", ", queryList1) + updateQuery;

                            Database mysql = new Database();
                            mysql.Insert(query);

                            logger.message = string.Format("{0} sales cash sales records is inserted", RecordCount);
                            logger.Broadcast();
                        }

                        if (inDBinvoice.Count > 0)
                        {
                            logger.Broadcast("Total sales invoice records to be deactivated: " + inDBinvoice.Count);

                            string inactive = "UPDATE cms_invoice_sales SET cancelled = '{0}' WHERE invoice_code = '{1}'";

                            Database mysql = new Database();

                            for (int i = 0; i < inDBinvoice.Count; i++)
                            {
                                string _docno = inDBinvoice[i].ToString();

                                Database.Sanitize(ref _docno);
                                string _query = string.Format(inactive, 'T', _docno);

                                mysql.Insert(_query);

                            }

                            logger.Broadcast(inDBinvoice.Count + " sales invoice records deactivated");

                            inDBinvoice.Clear();
                        }

                        _mysql.Insert("INSERT INTO cms_update_time(table_name, updated_at) VALUES ('cms_invoice_sales', NOW()) ON DUPLICATE KEY UPDATE table_name = VALUES(table_name), updated_at = VALUES(updated_at)");

                    }
                    catch (Exception ex)
                    {
                        try
                        {
                            Console.WriteLine(ex.Message);
                            goto CHECKAGAIN;
                        }
                        catch (Exception exc)
                        {
                            DpprException exception = new DpprException()
                            {
                                file_name = "SQLAccounting + JobInvSync",
                                exception = exc.Message,
                                time = DateTime.Now.ToString()
                            };
                            LocalDB.InsertException(exception);
                        }
                    }
                    
                    slog.action_identifier = Constants.Action_InvoiceSync;
                    slog.action_details = Constants.Tbl_cms_invoice_sales + Constants.Is_Finished + string.Format(" ({0}) records", RecordCount);
                    slog.action_failure = 0;
                    slog.action_failure_message = string.Empty;
                    slog.action_time = DateTime.Now.ToString();

                    DateTime endTime = DateTime.Now;
                    TimeSpan ts = endTime - startTime;
                    Console.WriteLine("Completed in: " + ts.TotalSeconds.ToString("F"));

                    LocalDB.InsertSyncLog(slog);

                    logger.message = "Sales Invoice sync finished in (" + ts.TotalSeconds.ToString("F") + " seconds)";
                    logger.Broadcast();
                });

                thread.Start();
                //thread.Join();
            }
            catch (ThreadAbortException e)
            {
                DpprException ex = new DpprException
                {
                    file_name = "JobInvoiceSync",
                    exception = e.Message,
                    time = DateTime.Now.ToString()
                };
                LocalDB.InsertException(ex);

                Console.WriteLine(Constants.Thread_Exception + e.Message);
            }
        }
    }
}