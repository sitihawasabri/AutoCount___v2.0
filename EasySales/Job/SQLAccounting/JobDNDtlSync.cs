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
    public class JobDNDtlSync : IJob
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
                    slog.action_identifier = Constants.Action_DebitNoteDetailsSync;                               /*check again */
                    slog.action_details = Constants.Tbl_cms_debitnote_details + Constants.Is_Starting;                /*check again */
                    slog.action_failure = 0;
                    slog.action_failure_message = string.Empty;
                    slog.action_time = DateTime.Now.ToString();

                    DateTime startTime = DateTime.Now;

                    LocalDB.InsertSyncLog(slog);
                    logger.message = "----------------------------------------------------------------------------";
                    logger.Broadcast();
                    logger.message = "Debit note details sync is running";
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

                    dynamic lRptVar, lSQL;
                    string dnCode, itemCode, itemName, itemPrice, itemQty, itemUom, discount, total, refNo;
                    string query, updateQuery;

                    HashSet<string> queryList = new HashSet<string>();
                    Database _mysql = new Database();

                    Dictionary<string, string> cms_updated_time = _mysql.GetUpdatedTime1YearInterval("cms_debitnote_details"); //{[updated_at, 28/06/2020 5:38:23 PM]}
                    string updated_at = string.Empty;

                    if (cms_updated_time.Count > 0)
                    {
                        updated_at = cms_updated_time["updated_at"].ToString().MSSQLdate();
                    }

                    lSQL = "SELECT SL_DN.DOCNO, SL_DN.DOCKEY, SL_DNDTL.ITEMCODE, SL_DNDTL.DESCRIPTION, SL_DNDTL.UNITPRICE, SL_DNDTL.QTY, SL_DNDTL.UOM, SL_DNDTL.LOCALAMOUNT, SL_DNDTL.DISC, SL_DNDTL.DTLKEY FROM SL_DNDTL LEFT JOIN SL_DN ON SL_DN.DOCKEY = SL_DNDTL.DOCKEY ";
                    
                    if (cms_updated_time.Count > 0)
                    {
                        lSQL += " WHERE SL_DN.DOCDATE >= '" + updated_at + "' ORDER BY DOCDATE DESC";
                    }
                    else
                    {
                        lSQL += " ORDER BY DOCDATE DESC";
                    }

                    try
                    {
                        lRptVar = ComServer.DBManager.NewDataSet(lSQL);

                        query = "INSERT INTO cms_debitnote_details(dn_code, item_code, item_name, item_price, quantity, uom, discount, total_price, ref_no) VALUES ";
                        updateQuery = " ON DUPLICATE KEY UPDATE quantity = VALUES(quantity), total_price = VALUES(total_price), item_price = VALUES(item_price), discount = VALUES(discount);";

                        lRptVar.First();

                        while (!lRptVar.eof)
                        {
                            RecordCount++;

                            dnCode = lRptVar.FindField("DOCNO").AsString;
                            itemCode = lRptVar.FindField("ITEMCODE").AsString;
                            itemName = lRptVar.FindField("DESCRIPTION").AsString;
                            itemPrice = lRptVar.FindField("UNITPRICE").AsString;
                            itemQty = lRptVar.FindField("QTY").AsString;
                            itemUom = lRptVar.FindField("UOM").AsString;
                            discount = lRptVar.FindField("DISC").AsString;
                            total = lRptVar.FindField("LOCALAMOUNT").AsString; //LOCALAMOUNT
                            refNo = lRptVar.FindField("DTLKEY").AsString;

                            Database.Sanitize(ref dnCode);
                            Database.Sanitize(ref itemCode);
                            Database.Sanitize(ref itemName);
                            Database.Sanitize(ref itemPrice);
                            Database.Sanitize(ref itemQty);
                            Database.Sanitize(ref itemUom);
                            Database.Sanitize(ref discount);
                            Database.Sanitize(ref total);
                            Database.Sanitize(ref refNo);

                            string Values = string.Format("('{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}','{8}')", dnCode, itemCode, itemName, itemPrice, itemQty, itemUom, discount, total, refNo);

                            queryList.Add(Values);

                            if (queryList.Count % 2000 == 0)
                            {
                                string tmp_query = query;
                                tmp_query += string.Join(", ", queryList);
                                tmp_query += updateQuery;

                                _mysql.Insert(tmp_query);

                                queryList.Clear();
                                tmp_query = string.Empty;

                                logger.message = string.Format("{0} debit note details records is inserted", RecordCount);
                                logger.Broadcast();
                            }

                            lRptVar.Next();
                        }

                        if (queryList.Count > 0)
                        {
                            query = query + string.Join(", ", queryList) + updateQuery;

                            _mysql.Insert(query);

                            logger.message = string.Format("{0} debit note details records is inserted", RecordCount);
                            logger.Broadcast();
                        }

                        _mysql.Insert("INSERT INTO cms_update_time(table_name, updated_at) VALUES ('cms_debitnote_details', NOW()) ON DUPLICATE KEY UPDATE table_name = VALUES(table_name), updated_at = VALUES(updated_at)");

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
                                file_name = "SQLAccounting + JobDNDtlSync",
                                exception = exc.Message,
                                time = DateTime.Now.ToString()
                            };
                            LocalDB.InsertException(exception);
                        }
                    }
                    
                    slog.action_identifier = Constants.Action_DebitNoteDetailsSync;
                    slog.action_details = Constants.Tbl_cms_debitnote_details + Constants.Is_Finished + string.Format(" ({0}) records", RecordCount);
                    slog.action_failure = 0;
                    slog.action_failure_message = string.Empty;
                    slog.action_time = DateTime.Now.ToString();

                    DateTime endTime = DateTime.Now;
                    TimeSpan ts = endTime - startTime;
                    Console.WriteLine("Completed in: " + ts.TotalSeconds.ToString("F"));

                    LocalDB.InsertSyncLog(slog);

                    logger.message = "Debit note details finished in (" + ts.TotalSeconds.ToString("F") + " seconds)";
                    logger.Broadcast();
                });

                thread.Start();
                //thread.Join();
            }
            catch (ThreadAbortException e)
            {
                DpprException ex = new DpprException
                {
                    file_name = "JobDebitNoteDetailUpdateSync",
                    exception = e.Message,
                    time = DateTime.Now.ToString()
                };
                LocalDB.InsertException(ex);

                Console.WriteLine(Constants.Thread_Exception + e.Message);
            }
        }
    }
}