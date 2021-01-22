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
    public class JobOutstandingSOSync : IJob
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
                    slog.action_identifier = Constants.Action_OutstandingSync;
                    slog.action_details = Constants.Tbl_cms_outstanding + Constants.Is_Starting;
                    slog.action_failure = 0;
                    slog.action_failure_message = string.Empty;
                    slog.action_time = DateTime.Now.ToString();

                    DateTime startTime = DateTime.Now;

                    LocalDB.InsertSyncLog(slog);
                    logger.message = "----------------------------------------------------------------------------";
                    logger.Broadcast();
                    logger.message = "Outstanding sync is running";
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

                    Database _mysql = new Database();

                    dynamic jsonRule = new CheckBackendRule()
                                           .CheckTablesExist()
                                           .GetSettingByTableName("cms_outstanding_so");

                    string fromDate = "January 01, 2020";
                    string toDate = "December 31, 2021";

                    if (jsonRule.Count > 0)
                    {
                        foreach (var rule in jsonRule)
                        {
                            dynamic _fromDate = rule.fromDate; 
                            if (_fromDate != null)
                            {
                                fromDate = _fromDate;
                            }

                            dynamic _toDate = rule.toDate;
                            if (_toDate != null)
                            {
                                toDate = _toDate;
                            }
                        }
                    }

                    Dictionary<string, string> salespersonList = new Dictionary<string, string>();

                    ArrayList salespersons = _mysql.Select("SELECT staff_code, login_id FROM cms_login");
                    for (int i = 0; i < salespersons.Count; i++)
                    {
                        Dictionary<string, string> each = (Dictionary<string, string>)salespersons[i];
                        salespersonList.Add(each["staff_code"], each["login_id"]);
                    }

                    salespersons.Clear();
                    dynamic lRptVar, lMain, lSub;
                    string Dtlkey, DocNo, ItemCode, SQty, OutstandingQty, DocDate, Agent, CustCode, TransQty;
                    string query, updateQuery;

                    ArrayList inDBactiveoutsoqty = _mysql.Select("SELECT so_docno FROM cms_outstanding_so WHERE active_status = 1");

                    logger.Broadcast("Active oustanding SO: " + inDBactiveoutsoqty.Count);
                    ArrayList inDBoutsoqty = new ArrayList();
                    for (int i = 0; i < inDBactiveoutsoqty.Count; i++)
                    {
                        Dictionary<string, string> each = (Dictionary<string, string>)inDBactiveoutsoqty[i];
                        inDBoutsoqty.Add(each["so_docno"].ToString());
                    }
                    inDBactiveoutsoqty.Clear();
                    //_mysql.Close();

                    HashSet<string> queryList = new HashSet<string>();

                    try
                    {
                        lRptVar = ComServer.RptObjects.Find("Sales.OutstandingSO.RO");

                        lRptVar.Params.Find("AllAgent").Value = true;
                        lRptVar.Params.Find("AllArea").Value = true;
                        lRptVar.Params.Find("AllCompany").Value = true;
                        lRptVar.Params.Find("AllDocument").Value = true;
                        lRptVar.Params.Find("AllItem").Value = true;
                        lRptVar.Params.Find("AllItemProject").Value = true;

                        //dynamic lDateFrom = DateTime.Parse("January 01, 2019");
                        //dynamic lDateTo = DateTime.Parse("December 31, 2020");
                        
                        dynamic lDateFrom = DateTime.Parse(fromDate); //sync one year is enough //maybe can put this 2 dates in cms_backend_rule
                        dynamic lDateTo = DateTime.Parse(toDate);

                        lRptVar.Params.Find("DateFrom").Value = lDateFrom;
                        lRptVar.Params.Find("DateTo").Value = lDateTo;
                        lRptVar.Params.Find("IncludeCancelled").Value = false;
                        lRptVar.Params.Find("PrintFulfilledItem").Value = true;
                        lRptVar.Params.Find("PrintOutstandingItem").Value = true;
                        lRptVar.Params.Find("SelectDate").Value = true;
                        lRptVar.Params.Find("SelectDeliveryDate").Value = false;
                        lRptVar.Params.Find("SortBy").Value = "DocDate;DocNo;Code";
                        lRptVar.Params.Find("AllDocProject").Value = true;
                        lRptVar.Params.Find("AllLocation").Value = true;
                        lRptVar.Params.Find("AllCompanyCategory").Value = true;
                        lRptVar.Params.Find("AllBatch").Value = true;
                        lRptVar.Params.Find("HasCategory").Value = false;
                        lRptVar.Params.Find("AllStockGroup").Value = true;
                        lRptVar.Params.Find("AllTariff").Value = true;
                        lRptVar.Params.Find("TranferDocFilterDate").Value = false;
                        lRptVar.CalculateReport();

                        lMain = lRptVar.DataSets.Find("cdsMain");
                        lSub = lRptVar.DataSets.Find("cdsTransfer");

                        query = "INSERT INTO cms_outstanding_so (so_docno,so_dockey,so_product_code,so_ori_qty,so_out_qty,so_trans_qty,so_doc_date,so_salesperson_id,so_cust_code, active_status) VALUES ";
                        updateQuery = " ON DUPLICATE KEY UPDATE so_ori_qty = VALUES(so_ori_qty),so_out_qty = VALUES(so_out_qty),so_trans_qty = VALUES(so_trans_qty),so_doc_date = VALUES(so_doc_date),so_cust_code = VALUES(so_cust_code),so_salesperson_id = VALUES(so_salesperson_id), active_status = VALUES(active_status);";

                        lMain.DisableControls();
                        lMain.First();

                        lSub.DisableControls();
                        lSub.First();

                        while (!lMain.eof)
                        {
                            RecordCount++;

                            Dtlkey = lMain.FindField("Dtlkey").AsString;

                            if (inDBoutsoqty.Contains(Dtlkey))
                            {
                                int index = inDBoutsoqty.IndexOf(Dtlkey);
                                if (index != -1)
                                {
                                    inDBoutsoqty.RemoveAt(index);
                                }
                            }

                            string DocKey = lMain.FindField("Dockey").AsString;

                            while (Dtlkey == lSub.FindField("FromDtlKey").AsString &&
                                    DocKey == lSub.FindField("FromDocKey").AsString)
                            {
                                lSub.Next();
                                if (lSub.eof)
                                {
                                    break;
                                }
                            }

                            DocNo = lMain.FindField("DocNo").AsString;

                            ItemCode = lMain.FindField("ItemCode").AsString;
                            SQty = "0";
                            OutstandingQty = "0";

                            if (lMain.FindField("SQTY") != null)
                            {
                                SQty = lMain.FindField("SQTY").AsString;
                            }
                            if (lMain.FindField("OutstandingQty") != null)
                            {
                                OutstandingQty = lMain.FindField("OutstandingQty").Value;
                            }

                            int.TryParse(OutstandingQty, out int outQty);
                            int.TryParse(SQty, out int sQty);

                            int TransferQTY = sQty - outQty;
                            TransQty = TransferQTY.ToString();

                            DocDate = lMain.FindField("DocDate").AsString;
                            DocDate = Convert.ToDateTime(DocDate).ToString("yyyy-MM-dd");
                            Agent = lMain.FindField("Agent").AsString;

                            if (!salespersonList.TryGetValue(Agent, out Agent))
                            {
                                Agent = "0";
                            }

                            CustCode = lMain.FindField("Code").AsString;

                            Database.Sanitize(ref Dtlkey);
                            Database.Sanitize(ref ItemCode);
                            Database.Sanitize(ref SQty);
                            Database.Sanitize(ref OutstandingQty);
                            Database.Sanitize(ref TransQty);
                            Database.Sanitize(ref DocDate);
                            Database.Sanitize(ref Agent);
                            Database.Sanitize(ref CustCode);

                            string Values = string.Format("('{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}','{8}','{9}')", Dtlkey, DocNo, ItemCode, SQty, OutstandingQty, TransQty, DocDate, Agent, CustCode, 1);

                            queryList.Add(Values);

                            if (queryList.Count % 2000 == 0)
                            {
                                string tmp_query = query;
                                tmp_query += string.Join(", ", queryList);
                                tmp_query += updateQuery;

                                Database mysql = new Database();
                                mysql.Insert(tmp_query);
                                //mysql.Close();

                                queryList.Clear();
                                tmp_query = string.Empty;

                                logger.message = string.Format("{0} outstanding so records is inserted", RecordCount);
                                logger.Broadcast();
                            }

                            lMain.Next();
                        }

                        if (queryList.Count > 0)
                        {
                            query = query + string.Join(", ", queryList) + updateQuery;

                            Database mysql = new Database();

                            mysql.Insert(query);
                            //mysql.Close();

                            logger.message = string.Format("{0} outstanding so records is inserted", RecordCount);
                            logger.Broadcast();

                            if (inDBoutsoqty.Count > 0)
                            {
                                logger.Broadcast("Total outstanding SO records to be deactivated: " + inDBoutsoqty.Count);
                                /* only deployed on cozbeauty server */
                                string inactive = "UPDATE cms_outstanding_so SET active_status = '{0}' WHERE so_docno = '{1}'";

                                for (int i = 0; i < inDBoutsoqty.Count; i++)
                                {
                                    string _docno = inDBoutsoqty[i].ToString();

                                    Database.Sanitize(ref _docno);
                                    string _query = string.Format(inactive, '0', _docno);

                                    mysql.Insert(_query);
                                }

                                logger.Broadcast(inDBoutsoqty.Count + " outstanding SO records deactivated");

                                inDBoutsoqty.Clear();
                            }
                        }
                    }
                    catch
                    {
                        try
                        {
                            goto CHECKAGAIN;
                        }
                        catch (Exception exc)
                        {
                            DpprException exception = new DpprException()
                            {
                                file_name = "SQLAccounting + JobOutSOSync",
                                exception = exc.Message,
                                time = DateTime.Now.ToString()
                            };
                            LocalDB.InsertException(exception);
                        }
                    }

                    slog.action_identifier = Constants.Action_OutstandingSync;
                    slog.action_details = Constants.Tbl_cms_outstanding + Constants.Is_Finished + string.Format(" ({0}) records", RecordCount);
                    slog.action_failure = 0;
                    slog.action_failure_message = string.Empty;
                    slog.action_time = DateTime.Now.ToString();

                    DateTime endTime = DateTime.Now;
                    TimeSpan ts = endTime - startTime;
                    Console.WriteLine("Completed in: " + ts.TotalSeconds.ToString("F"));

                    LocalDB.InsertSyncLog(slog);

                    logger.message = "Outstanding so sync finished in (" + ts.TotalSeconds.ToString("F") + " seconds)";
                    logger.Broadcast();
                });

                thread.Start();
                //thread.Join();

                //await Console.Out.WriteLineAsync("Outstanding Sync");
            }
            catch (ThreadAbortException e)
            {
                DpprException ex = new DpprException
                {
                    file_name = "JobOutstandingSync",
                    exception = e.Message,
                    time = DateTime.Now.ToString()
                };
                LocalDB.InsertException(ex);

                Console.WriteLine(Constants.Thread_Exception + e.Message);
            }
        }
    }
}