using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using EasySales.Model;
using EasySales.Object;
using Quartz;
using Quartz.Impl;
using Quartz.Logging;

namespace EasySales.Job
{
    [DisallowConcurrentExecution]
    public class JobInvoiceSync : IJob
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
                    slog.action_identifier = Constants.Action_InvoiceSync;
                    slog.action_details = Constants.Tbl_cms_invoice + Constants.Is_Starting;
                    slog.action_failure = 0;
                    slog.action_failure_message = string.Empty;
                    slog.action_time = DateTime.Now.ToString();

                    DateTime startTime = DateTime.Now;

                    LocalDB.InsertSyncLog(slog);
                    logger.message = "----------------------------------------------------------------------------";
                    logger.Broadcast();
                    logger.message = "Invoice sync is running";
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
                    string lSQL, DOCNO, CODE, DOCDATE, DOCAMT, PAYMENTAMT, CANCELLED, DUEDATE, AGENT, CURRENCYRATE, NOTE, DOCNOEX, DESCRIPTION, PROJECT;
                    string NOTEBLOB = string.Empty;
                    string query, updateQuery;

                    Database _mysql = new Database();

                    HashSet<string> queryList = new HashSet<string>();

                    Dictionary<string, string> cms_updated_time = _mysql.GetUpdatedTime1YearInterval("cms_invoice"); //{[updated_at, 28/06/2020 5:38:23 PM]}
                    string updated_at = string.Empty;

                    if (cms_updated_time.Count > 0)
                    {
                        updated_at = cms_updated_time["updated_at"].ToString().MSSQLdate();
                    }

                    dynamic jsonRule = new CheckBackendRule()
                                           .CheckTablesExist()
                                           .GetSettingByTableName("cms_invoice");

                    string inv_remark = "0";
                    string name = string.Empty;
                    string sqlacc_field = string.Empty;
                    string fieldname = string.Empty;
                    string separator = string.Empty;

                    ArrayList FieldNameList = new ArrayList();
                    Dictionary<string, string> FieldSeparatorList = new Dictionary<string, string>();

                    if (jsonRule.Count > 0)
                    {
                        foreach (var rule in jsonRule)
                        {
                            dynamic _inv_remark = rule.inv_remark;
                            if (_inv_remark != null)
                            {
                                if (_inv_remark != "0")
                                {
                                    inv_remark = _inv_remark;
                                }
                            }

                            dynamic _field = rule.order_field;
                            if (_field != null)
                            {
                                foreach (var field in _field)
                                {
                                    dynamic _sqlacc = field.sqlacc;
                                    if (_sqlacc != string.Empty)
                                    {
                                        sqlacc_field = _sqlacc;
                                    }

                                    FieldNameList.Add(sqlacc_field);
                                }
                            }

                            dynamic _field_separator = rule.order_field_separator;
                            if (_field_separator != null)
                            {
                                foreach (var field in _field_separator)
                                {
                                    dynamic _fieldname = field.fieldname;
                                    if (_fieldname != string.Empty)
                                    {
                                        fieldname = _fieldname;
                                    }

                                    dynamic _separator = field.separator;
                                    separator = _separator;

                                    FieldSeparatorList.Add(fieldname, separator);
                                }
                            }
                        }
                    }
                    else
                    {
                        goto ENDJOB;
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

                    string getActiveInv = "SELECT invoice_code FROM cms_invoice WHERE cancelled = 'F'";
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

                    string inv_remark_query = string.Empty;
                    string inv_remark_updateQuery = string.Empty;

                    if (inv_remark == "1")
                    {
                        inv_remark_query = " , inv_remark ";
                        inv_remark_updateQuery = " ,inv_remark = VALUES(inv_remark) ";
                    }

                    query = "INSERT INTO cms_invoice(invoice_code,cust_code, invoice_date, invoice_amount, outstanding_amount, cancelled, invoice_due_date, salesperson_id " +inv_remark_query+ ") VALUES ";
                    updateQuery = " ON DUPLICATE KEY UPDATE cancelled = VALUES(cancelled), invoice_amount = VALUES(invoice_amount), outstanding_amount = VALUES(outstanding_amount), updated_at = VALUES(updated_at), cust_code = VALUES(cust_code),invoice_due_date=VALUES(invoice_due_date), invoice_date = VALUES(invoice_date), salesperson_id=VALUES(salesperson_id) " + inv_remark_updateQuery + ";";

                    //lSQL = "SELECT DOCNO,CODE,DOCDATE,DOCAMT,PAYMENTAMT,CANCELLED,DUEDATE,AGENT,CURRENCYCODE,CURRENCYRATE FROM AR_IV WHERE AGENT != '' ORDER BY DOCDATE DESC";
                    //lSQL = "SELECT DOCNO,CODE,DOCDATE,DOCAMT,PAYMENTAMT,CANCELLED,DUEDATE,AGENT,CURRENCYCODE,CURRENCYRATE FROM AR_IV WHERE AGENT != '' ";
                    //lSQL = "SELECT DOCNO,CODE,DOCDATE,DOCAMT,LOCALDOCAMT,PAYMENTAMT,CANCELLED,DUEDATE,AGENT,CURRENCYCODE,CURRENCYRATE FROM AR_IV WHERE AGENT != '' ";
                    lSQL = "SELECT DOCNO,CODE,DOCDATE,DOCAMT,LOCALDOCAMT,PAYMENTAMT,CANCELLED,DUEDATE,AGENT,CURRENCYCODE,CURRENCYRATE,DOCNOEX,DESCRIPTION,CAST(NOTE AS VARCHAR(2000)) AS NOTEBLOB FROM AR_IV WHERE AGENT != '' ";

                    if (cms_updated_time.Count > 0)
                    {
                        lSQL += " AND DOCDATE >= '" + updated_at + "'";
                    }

                    try
                    {
                        lDataSet = ComServer.DBManager.NewDataSet(lSQL);
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

                            PAYMENTAMT = lDataSet.FindField("PAYMENTAMT").AsString;

                            Double.TryParse(PAYMENTAMT, out double paymentAmt);

                            CANCELLED = lDataSet.FindField("CANCELLED").AsString;

                            CURRENCYRATE = lDataSet.FindField("CURRENCYRATE").AsString;
                            double.TryParse(CURRENCYRATE, out double currencyRate);

                            if (currencyRate != 1)
                            {
                                paymentAmt = paymentAmt * currencyRate;
                            }

                            double outstandingAmt = docAmt - paymentAmt;                        //just added 08092020; only for DN & INV sync at this moment
                            outstandingAmt = Math.Round(outstandingAmt, 2);
                            string OUTSTANDINGAMT = outstandingAmt.ToString();

                            DUEDATE = lDataSet.FindField("DUEDATE").AsString;
                            DUEDATE = Convert.ToDateTime(DUEDATE).ToString("yyyy-MM-dd");

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

                            string inv_remark_field = string.Empty;
                            if (FieldNameList.Count > 0)
                            {
                                string mysql_column = string.Empty;
                                string sqlacc_column = string.Empty;

                                for (int idx = 0; idx < FieldNameList.Count; idx++) 
                                {
                                    sqlacc_column = FieldNameList[idx].ToString();

                                    if(sqlacc_column == "NOTEBLOB")
                                    {
                                        NOTE = lDataSet.FindField("NOTEBLOB").AsString;
                                        if (NOTE != null)
                                        {
                                            RichTextBox rtBox = new RichTextBox();
                                            rtBox.Rtf = NOTE;
                                            NOTEBLOB = rtBox.Text;
                                            inv_remark_field = NOTEBLOB;
                                            Console.WriteLine(DOCNO + ": ---> " + NOTEBLOB);
                                            Console.WriteLine("-----------");
                                            break;
                                        }
                                    }
                                    else
                                    {
                                        sqlacc_column = lDataSet.FindField(sqlacc_column).AsString;
                                        if (sqlacc_column != null)
                                        {
                                            inv_remark_field = sqlacc_column;
                                        }
                                    }
                                }
                            }

                            Database.Sanitize(ref DOCNO);
                            Database.Sanitize(ref CODE);
                            Database.Sanitize(ref DOCDATE);
                            Database.Sanitize(ref DOCAMT);
                            Database.Sanitize(ref PAYMENTAMT);
                            Database.Sanitize(ref OUTSTANDINGAMT);
                            Database.Sanitize(ref CANCELLED);
                            Database.Sanitize(ref DUEDATE);
                            Database.Sanitize(ref inv_remark_field);


                            string Values = string.Format("('{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}')", DOCNO, CODE, DOCDATE, docAmt, OUTSTANDINGAMT, CANCELLED, DUEDATE, AGENTID);
                            if (inv_remark == "1")
                            {
                                Values = string.Format("('{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}','{8}')", DOCNO, CODE, DOCDATE, docAmt, OUTSTANDINGAMT, CANCELLED, DUEDATE, AGENTID, inv_remark_field);
                            }

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

                                logger.message = string.Format("{0} invoice records is inserted", RecordCount);
                                logger.Broadcast();
                            }

                            lDataSet.Next();
                        }

                        if (queryList.Count > 0)
                        {
                            query = query + string.Join(", ", queryList) + updateQuery;

                            Database mysql = new Database();
                            mysql.Insert(query);

                            logger.message = string.Format("{0} invoice records is inserted", RecordCount);
                            logger.Broadcast();
                        }

                        if (inDBinvoice.Count > 0)
                        {
                            logger.Broadcast("Total invoice records to be deactivated: " + inDBinvoice.Count);

                            string inactive = "UPDATE cms_invoice SET cancelled = '{0}' WHERE invoice_code = '{1}'";

                            Database mysql = new Database();

                            for (int i = 0; i < inDBinvoice.Count; i++)
                            {
                                string _docno = inDBinvoice[i].ToString();

                                Database.Sanitize(ref _docno);
                                string _query = string.Format(inactive, 'T', _docno);

                                mysql.Insert(_query);

                            }

                            logger.Broadcast(inDBinvoice.Count + " invoice records deactivated");

                            inDBinvoice.Clear();
                        }

                        _mysql.Insert("INSERT INTO cms_update_time(table_name, updated_at) VALUES ('cms_invoice', NOW()) ON DUPLICATE KEY UPDATE table_name = VALUES(table_name), updated_at = VALUES(updated_at)");

                    }
                    catch (Exception ex)
                    {
                        try
                        {
                            Console.WriteLine(ex.Message);
                            instance.KillSQLAccounting();
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

                    ENDJOB:


                    slog.action_identifier = Constants.Action_InvoiceSync;
                    slog.action_details = Constants.Tbl_cms_invoice + Constants.Is_Finished + string.Format(" ({0}) records", RecordCount);
                    slog.action_failure = 0;
                    slog.action_failure_message = string.Empty;
                    slog.action_time = DateTime.Now.ToString();

                    DateTime endTime = DateTime.Now;
                    TimeSpan ts = endTime - startTime;
                    Console.WriteLine("Completed in: " + ts.TotalSeconds.ToString("F"));

                    LocalDB.InsertSyncLog(slog);

                    logger.message = "Invoice sync finished in (" + ts.TotalSeconds.ToString("F") + " seconds)";
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