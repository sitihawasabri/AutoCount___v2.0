using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
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
    public class JobInvoiceDetailsSync : IJob
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

                    LocalDB.DBCleanup();

                    int RecordCount = 0;
                    GlobalLogger logger = new GlobalLogger();

                    DpprSyncLog slog = new DpprSyncLog();
                    slog.action_identifier = Constants.Action_InvoiceDtlSync;
                    slog.action_details = Constants.Tbl_cms_invoice_details + Constants.Is_Starting;
                    slog.action_failure = 0;
                    slog.action_failure_message = string.Empty;
                    slog.action_time = DateTime.Now.ToString();

                    DateTime startTime = DateTime.Now;

                    LocalDB.InsertSyncLog(slog);

                    logger.message = "Invoice details sync is running";
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
                    string lSQL, DOCNO, ITEMCODE, DESCRIPTION, UNITPRICE, QTY, UOM, DISC, AMOUNT, REFNO;
                    string query, updateQuery;
                    HashSet<string> queryList = new HashSet<string>();
                    Database _mysql = new Database();

                    Dictionary<string, string> cms_updated_time = _mysql.GetUpdatedTime1YearInterval("cms_invoice_details"); //{[updated_at, 28/06/2020 5:38:23 PM]}
                    string updated_at = string.Empty;

                    if (cms_updated_time.Count > 0)
                    {
                        updated_at = cms_updated_time["updated_at"].ToString().MSSQLdate();
                    }

                    string activeINVDtl = "SELECT inv.invoice_code, inv.invoice_date, dtl.id, dtl.invoice_code AS dtl_invoice_code, dtl.item_code, dtl.ref_no FROM cms_invoice AS inv LEFT JOIN cms_invoice_details AS dtl ON inv.invoice_code = dtl.invoice_code WHERE active_status = 1 ";
                    if (cms_updated_time.Count > 0)
                    {
                        activeINVDtl += " AND inv.invoice_date >= '" + updated_at + "' ORDER BY inv.invoice_date DESC ";
                    }

                    //OrderedDictionary uniqueKeyList = new OrderedDictionary();

                    ArrayList inDBactiveinvdtl = _mysql.Select(activeINVDtl);
                    logger.Broadcast("Active invoice details in DB: " + inDBactiveinvdtl.Count);
                    Dictionary<string, string> uniqueKeyList = new Dictionary<string, string>();
                    for(int i = 0; i < inDBactiveinvdtl.Count; i++)
                    {
                        Dictionary<string, string> each = (Dictionary<string, string>)inDBactiveinvdtl[i];
                        string id = each["id"].ToString();
                        string refno = each["ref_no"].ToString();
                        string dtlInvoiceCode = each["dtl_invoice_code"].ToString();
                        string itemCode = each["item_code"].ToString();
                        string unique = dtlInvoiceCode + itemCode + refno;
                        string uniqueLowercase = unique.ToLower();
                        uniqueKeyList.Add(id, uniqueLowercase);
                    }
                    inDBactiveinvdtl.Clear();

                    lSQL = "SELECT SL_IV.DOCNO, SL_IV.DOCKEY, SL_IVDTL.ITEMCODE, SL_IVDTL.DESCRIPTION, SL_IVDTL.UNITPRICE, SL_IVDTL.QTY, SL_IVDTL.UOM, SL_IVDTL.LOCALAMOUNT, SL_IVDTL.DISC, SL_IVDTL.DTLKEY FROM SL_IVDTL LEFT JOIN SL_IV ON SL_IV.DOCKEY = SL_IVDTL.DOCKEY ";
                    
                    if (cms_updated_time.Count > 0)
                    {
                        lSQL += " WHERE SL_IV.DOCDATE >= '" + updated_at + "' ORDER BY DOCDATE DESC";
                    }
                    else
                    {
                        lSQL += " ORDER BY DOCDATE DESC";
                    }

                    try
                    {
                        lDataSet = ComServer.DBManager.NewDataSet(lSQL);

                        query = "INSERT INTO cms_invoice_details(invoice_code, item_code, item_name, item_price, quantity, uom, discount, total_price, ref_no, active_status) VALUES ";
                        updateQuery = " ON DUPLICATE KEY UPDATE quantity = VALUES(quantity), total_price = VALUES(total_price), item_price = VALUES(item_price), discount = VALUES(discount), active_status=VALUES(active_status), ref_no=VALUES(ref_no);";

                        lDataSet.First();

                        while (!lDataSet.eof)
                        {
                            RecordCount++;

                            DOCNO = lDataSet.FindField("DOCNO").AsString;
                            ITEMCODE = lDataSet.FindField("ITEMCODE").AsString;
                            DESCRIPTION = lDataSet.FindField("DESCRIPTION").AsString;
                            UNITPRICE = lDataSet.FindField("UNITPRICE").AsString;
                            QTY = lDataSet.FindField("QTY").AsString;

                            UOM = lDataSet.FindField("UOM").AsString;
                            DISC = lDataSet.FindField("DISC").AsString;
                            AMOUNT = lDataSet.FindField("LOCALAMOUNT").AsString;            //LOCALAMOUNT
                            REFNO = lDataSet.FindField("DTLKEY").AsString;

                            string unique = DOCNO + ITEMCODE + REFNO;
                            string uniqueLowercase = unique.ToLower();
                            if (uniqueKeyList.ContainsValue(uniqueLowercase))
                            {
                                var key = uniqueKeyList.Where(pair => pair.Value == uniqueLowercase)
                                            .Select(pair => pair.Key)
                                            .FirstOrDefault();
                                if(key != null)
                                {
                                    uniqueKeyList.Remove(key);
                                }
                            }

                            Database.Sanitize(ref DOCNO);
                            Database.Sanitize(ref ITEMCODE);
                            Database.Sanitize(ref DESCRIPTION);
                            Database.Sanitize(ref UNITPRICE);
                            Database.Sanitize(ref QTY);
                            Database.Sanitize(ref UOM);
                            Database.Sanitize(ref DISC);
                            Database.Sanitize(ref AMOUNT);
                            Database.Sanitize(ref REFNO);

                            string Values = string.Format("('{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}','{8}','{9}')", DOCNO, ITEMCODE, DESCRIPTION, UNITPRICE, QTY, UOM, DISC, AMOUNT, REFNO, 1);

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

                                logger.message = string.Format("{0} invoice details records is inserted", RecordCount);
                                logger.Broadcast();
                            }

                            lDataSet.Next();
                        }

                        if (queryList.Count > 0)
                        {
                            string tmp_query = query;
                            tmp_query += string.Join(", ", queryList);
                            tmp_query += updateQuery;

                            Database mysql = new Database();
                            mysql.Insert(tmp_query);

                            queryList.Clear();
                            tmp_query = string.Empty;

                            logger.message = string.Format("{0} invoice details records is inserted", RecordCount);
                            logger.Broadcast();
                        }

                        /* CASH SALES DETAILS */
                        string lSQLCS;
                        dynamic lRptVarCS;
                        string ivCode, itemCode, itemName, itemPrice, itemQty, itemUom, discount, total, refno;

                        HashSet<string> queryListCS = new HashSet<string>();

                        lSQLCS = "SELECT SL_CS.DOCNO, SL_CS.DOCKEY, SL_CS.DOCDATE, SL_CSDTL.ITEMCODE, SL_CSDTL.DESCRIPTION, SL_CSDTL.UNITPRICE, SL_CSDTL.QTY, SL_CSDTL.UOM, SL_CSDTL.LOCALAMOUNT, SL_CSDTL.DISC, SL_CSDTL.DTLKEY FROM SL_CSDTL LEFT JOIN SL_CS ON SL_CS.DOCKEY = SL_CSDTL.DOCKEY ";

                        if (cms_updated_time.Count > 0)
                        {
                            lSQLCS += " WHERE SL_CS.DOCDATE >= '" + updated_at + "' ORDER BY DOCDATE DESC";
                        }
                        else
                        {
                            lSQLCS += " ORDER BY SL_CS.DOCDATE DESC";
                        }

                        lRptVarCS = ComServer.DBManager.NewDataSet(lSQLCS);

                        query = "INSERT INTO cms_invoice_details(invoice_code, item_code, item_name, item_price, quantity, uom, discount, total_price, ref_no) VALUES ";
                        updateQuery = " ON DUPLICATE KEY UPDATE quantity = VALUES(quantity), total_price = VALUES(total_price), item_price = VALUES(item_price), discount = VALUES(discount) ";

                        lRptVarCS.First();

                        while (!lRptVarCS.eof)
                        {
                            RecordCount++;

                            ivCode = lRptVarCS.FindField("DOCNO").AsString;
                            itemCode = lRptVarCS.FindField("ITEMCODE").AsString;
                            itemName = lRptVarCS.FindField("DESCRIPTION").AsString;
                            itemPrice = lRptVarCS.FindField("UNITPRICE").AsString;
                            itemQty = lRptVarCS.FindField("QTY").AsString;
                            itemUom = lRptVarCS.FindField("UOM").AsString;
                            discount = lRptVarCS.FindField("DISC").AsString;
                            total = lRptVarCS.FindField("LOCALAMOUNT").AsString; //LOCALAMOUNT
                            refno = lRptVarCS.FindField("DTLKEY").AsString;

                            string unique = ivCode + itemCode + refno;
                            string uniqueLowercase = unique.ToLower();
                            if (uniqueKeyList.ContainsValue(uniqueLowercase))
                            {
                                var key = uniqueKeyList.Where(pair => pair.Value == uniqueLowercase)
                                            .Select(pair => pair.Key)
                                            .FirstOrDefault();
                                if (key != null)
                                {
                                    uniqueKeyList.Remove(key);
                                }
                            }

                            Database.Sanitize(ref ivCode);
                            Database.Sanitize(ref itemCode);
                            Database.Sanitize(ref itemName);
                            Database.Sanitize(ref itemPrice);
                            Database.Sanitize(ref itemQty);
                            Database.Sanitize(ref itemUom);
                            Database.Sanitize(ref discount);
                            Database.Sanitize(ref total);

                            string Values = string.Format("('{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}','{8}')", ivCode, itemCode, itemName, itemPrice, itemQty, itemUom, discount, total, refno);

                            queryListCS.Add(Values);

                            if (queryListCS.Count % 2000 == 0)
                            {
                                string tmp_query = query;
                                tmp_query += string.Join(", ", queryListCS);
                                tmp_query += updateQuery;

                                _mysql.Insert(tmp_query);

                                queryListCS.Clear();
                                tmp_query = string.Empty;

                                logger.message = string.Format("{0} cash sales details records is inserted", RecordCount);
                                logger.Broadcast();
                            }

                            lRptVarCS.Next();
                        }

                        if (queryListCS.Count > 0)
                        {
                            query = query + string.Join(", ", queryListCS) + updateQuery;

                            _mysql.Insert(query);

                            logger.message = string.Format("{0} cash sales details records is inserted", RecordCount);
                            logger.Broadcast();
                        }

                        if (uniqueKeyList.Count > 0)
                        {
                            logger.Broadcast("Total invoice details records to be deactivated: " + uniqueKeyList.Count);
                            Database mysql = new Database();

                            HashSet<string> deactivateId = new HashSet<string>();
                            for (int i = 0; i < uniqueKeyList.Count; i++)
                            {
                                string _id = uniqueKeyList.ElementAt(i).Key;
                                deactivateId.Add(_id);
                            }

                            string ToBeDeactivate = "'" + string.Join("','", deactivateId) + "'";
                            Console.WriteLine(ToBeDeactivate);

                            string inactive = "UPDATE cms_invoice_details SET active_status = 0 WHERE id IN (" + ToBeDeactivate + ")";
                            mysql.Insert(inactive);

                            logger.Broadcast(uniqueKeyList.Count + " invoice details records deactivated");

                            uniqueKeyList.Clear();
                        }

                        _mysql.Insert("INSERT INTO cms_update_time(table_name, updated_at) VALUES ('cms_invoice_details', NOW()) ON DUPLICATE KEY UPDATE table_name = VALUES(table_name), updated_at = VALUES(updated_at)");

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
                                file_name = "SQLAccounting + JobInvDtlSync",
                                exception = exc.Message,
                                time = DateTime.Now.ToString()
                            };
                            LocalDB.InsertException(exception);
                        }
                    }

                    
                    slog.action_identifier = Constants.Action_InvoiceDtlSync;
                    slog.action_details = Constants.Tbl_cms_invoice_details + Constants.Is_Finished + string.Format(" ({0}) records", RecordCount);
                    slog.action_failure = 0;
                    slog.action_failure_message = string.Empty;
                    slog.action_time = DateTime.Now.ToString();

                    DateTime endTime = DateTime.Now;
                    TimeSpan ts = endTime - startTime;
                    Console.WriteLine("Completed in: " + ts.TotalSeconds.ToString("F"));

                    LocalDB.InsertSyncLog(slog);

                    logger.message = "Invoice details sync finished in (" + ts.TotalSeconds.ToString("F") + " seconds)";
                    logger.Broadcast();
                });

                thread.Start();
                //thread.Join();
            }
            catch (ThreadAbortException e)
            {
                DpprException ex = new DpprException
                {
                    file_name = "JobInvoiceDetailsSync",
                    exception = e.Message,
                    time = DateTime.Now.ToString()
                };
                LocalDB.InsertException(ex);

                Console.WriteLine(Constants.Thread_Exception + e.Message);
            }
        }
    }
}