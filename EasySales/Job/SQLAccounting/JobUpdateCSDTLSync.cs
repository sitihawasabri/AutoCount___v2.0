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
    public class JobUpdateCSDTLSync : IJob
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
                    slog.action_identifier = Constants.Action_UpdateCashSalesDtlSync;                               /*check again */
                    slog.action_details = Constants.Tbl_cms_invoice_details + Constants.Is_Starting;                /*check again */
                    slog.action_failure = 0;
                    slog.action_failure_message = string.Empty;
                    slog.action_time = DateTime.Now.ToString();

                    DateTime startTime = DateTime.Now;

                    LocalDB.InsertSyncLog(slog);
                    logger.message = "----------------------------------------------------------------------------";
                    logger.Broadcast();
                    logger.message = "Update cash sales details sync is running";
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

                    //MOVED TO INV DTL SYNC 01102020

                    //dynamic lRptVar;
                    //string ivCode, itemCode, itemName, itemPrice, itemQty, itemUom, discount, total, refno;
                    //string lSQL, query, updateQuery;

                    //HashSet<string> queryList = new HashSet<string>();
                    //Database _mysql = new Database();

                    //Dictionary<string, string> cms_updated_time = _mysql.GetUpdatedTime1YearInterval("cms_invoice_details");
                    //string updated_at = string.Empty;

                    //if (cms_updated_time.Count > 0)
                    //{
                    //    updated_at = cms_updated_time["updated_at"].ToString().MSSQLdate();
                    //}

                    //string activeCNDtl = "SELECT inv.invoice_code, inv.invoice_date, dtl.id, dtl.invoice_code AS dtl_invoice_code, dtl.item_code, dtl.ref_no FROM cms_invoice AS inv LEFT JOIN cms_invoice_details AS dtl ON inv.invoice_code = dtl.invoice_code WHERE active_status = 1 ";
                    //if (cms_updated_time.Count > 0)
                    //{
                    //    activeCNDtl += " AND inv.invoice_date >= '" + updated_at + "' ";
                    //}

                    //ArrayList inDBactiveinvdtl = _mysql.Select(activeCNDtl);
                    //logger.Broadcast("Active invoice details in DB: " + inDBactiveinvdtl.Count);
                    //Dictionary<string, string> uniqueKeyList = new Dictionary<string, string>();
                    //for (int i = 0; i < inDBactiveinvdtl.Count; i++)
                    //{
                    //    Dictionary<string, string> each = (Dictionary<string, string>)inDBactiveinvdtl[i];
                    //    string id = each["id"].ToString();
                    //    string ref_no = each["ref_no"].ToString();
                    //    string dtl_invoice_code = each["dtl_invoice_code"].ToString();
                    //    string item_code = each["item_code"].ToString();
                    //    string unique = dtl_invoice_code + item_code + ref_no;
                    //    uniqueKeyList.Add(id, unique);
                    //}
                    //inDBactiveinvdtl.Clear();

                    ////"SELECT SL_CS.DOCNO, SL_CS.DOCKEY, SL_CSDTL.ITEMCODE, SL_CSDTL.DESCRIPTION, SL_CSDTL.UNITPRICE, SL_CSDTL.QTY, SL_CSDTL.UOM, SL_CSDTL.LOCALAMOUNT, SL_CSDTL.DISC FROM SL_CSDTL LEFT JOIN SL_CS ON SL_CS.DOCKEY = SL_CSDTL.DOCKEY ORDER BY DOCDATE DESC"
                    //lSQL = "SELECT SL_CS.DOCNO, SL_CS.DOCKEY, SL_CS.DOCDATE, SL_CSDTL.ITEMCODE, SL_CSDTL.DESCRIPTION, SL_CSDTL.UNITPRICE, SL_CSDTL.QTY, SL_CSDTL.UOM, SL_CSDTL.LOCALAMOUNT, SL_CSDTL.DISC, SL_CSDTL.DTLKEY FROM SL_CSDTL LEFT JOIN SL_CS ON SL_CS.DOCKEY = SL_CSDTL.DOCKEY ";

                    //if (cms_updated_time.Count > 0)
                    //{
                    //    lSQL += " WHERE SL_CS.DOCDATE >= '" + updated_at + "' ORDER BY DOCDATE DESC";
                    //}
                    //else
                    //{
                    //    lSQL += " ORDER BY SL_CS.DOCDATE DESC";
                    //}

                    //lRptVar = ComServer.DBManager.NewDataSet(lSQL);

                    //query = "INSERT INTO cms_invoice_details(invoice_code, item_code, item_name, item_price, quantity, uom, discount, total_price, ref_no) VALUES ";
                    //updateQuery = " ON DUPLICATE KEY UPDATE quantity = VALUES(quantity), total_price = VALUES(total_price), item_price = VALUES(item_price), discount = VALUES(discount) ";

                    //lRptVar.First();

                    //while (!lRptVar.eof)
                    //{
                    //    RecordCount++;

                    //    ivCode = lRptVar.FindField("DOCNO").AsString;
                    //    itemCode = lRptVar.FindField("ITEMCODE").AsString;
                    //    itemName = lRptVar.FindField("DESCRIPTION").AsString;
                    //    itemPrice = lRptVar.FindField("UNITPRICE").AsString;
                    //    itemQty = lRptVar.FindField("QTY").AsString;
                    //    itemUom = lRptVar.FindField("UOM").AsString;
                    //    discount =lRptVar.FindField("DISC").AsString;
                    //    total = lRptVar.FindField("LOCALAMOUNT").AsString; //LOCALAMOUNT
                    //    refno = lRptVar.FindField("DTLKEY").AsString;

                    //    string unique = ivCode + itemCode + refno;
                    //    string uniqueLowercase = unique.ToLower();
                    //    if (uniqueKeyList.ContainsValue(uniqueLowercase))
                    //    {
                    //        for (int ix = 0; ix < uniqueKeyList.Count; ix++)
                    //        {
                    //            string key = uniqueKeyList.ElementAt(ix).Key;
                    //            string value = uniqueKeyList.ElementAt(ix).Value;

                    //            if (value == unique)
                    //            {
                    //                uniqueKeyList.Remove(key);
                    //                break;
                    //            }
                    //        }
                    //    }

                    //    Database.Sanitize(ref ivCode);
                    //    Database.Sanitize(ref itemCode);
                    //    Database.Sanitize(ref itemName);
                    //    Database.Sanitize(ref itemPrice);
                    //    Database.Sanitize(ref itemQty);
                    //    Database.Sanitize(ref itemUom);
                    //    Database.Sanitize(ref discount);
                    //    Database.Sanitize(ref total);

                    //    string Values = string.Format("('{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}','{8}')", ivCode, itemCode, itemName, itemPrice, itemQty, itemUom, discount, total, refno);

                    //    queryList.Add(Values);

                    //    if (queryList.Count % 2000 == 0)
                    //    {
                    //        string tmp_query = query;
                    //        tmp_query += string.Join(", ", queryList);
                    //        tmp_query += updateQuery;

                    //        _mysql.Insert(tmp_query);
                    //        //_mysql.Close();

                    //        queryList.Clear();
                    //        tmp_query = string.Empty;

                    //        logger.message = string.Format("{0} update cash sales details records is inserted", RecordCount);
                    //        logger.Broadcast();
                    //    }

                    //    lRptVar.Next();
                    //}

                    //if (queryList.Count > 0)
                    //{
                    //    query = query + string.Join(", ", queryList) + updateQuery;

                    //    _mysql.Insert(query);
                    //    //_mysql.Close();

                    //    logger.message = string.Format("{0} update cash sales details records is inserted", RecordCount);
                    //    logger.Broadcast();
                    //}

                    //if (uniqueKeyList.Count > 0)
                    //{
                    //    logger.Broadcast("Total invoice details records to be deactivated: " + uniqueKeyList.Count);
                    //    Database mysql = new Database();

                    //    HashSet<string> deactivateId = new HashSet<string>();
                    //    for (int i = 0; i < uniqueKeyList.Count; i++)
                    //    {
                    //        string _id = uniqueKeyList.ElementAt(i).Key;
                    //        deactivateId.Add(_id);
                    //    }

                    //    string ToBeDeactivate = "'" + string.Join("','", deactivateId) + "'";
                    //    Console.WriteLine(ToBeDeactivate);

                    //    string inactive = "UPDATE cms_invoice_details SET active_status = 0 WHERE id IN (" + ToBeDeactivate + ")";
                    //    mysql.Insert(inactive);

                    //    logger.Broadcast(uniqueKeyList.Count + " credit note details records deactivated");

                    //    uniqueKeyList.Clear();
                    //}

                    slog.action_identifier = Constants.Action_UpdateCashSalesDtlSync;
                    slog.action_details = Constants.Tbl_cms_invoice_details + Constants.Is_Finished + string.Format(" ({0}) records", RecordCount);
                    slog.action_failure = 0;
                    slog.action_failure_message = string.Empty;
                    slog.action_time = DateTime.Now.ToString();

                    DateTime endTime = DateTime.Now;
                    TimeSpan ts = endTime - startTime;
                    Console.WriteLine("Completed in: " + ts.TotalSeconds.ToString("F"));

                    LocalDB.InsertSyncLog(slog);

                    logger.message = "Update cash sales details sync finished in (" + ts.TotalSeconds.ToString("F") + " seconds)";
                    logger.Broadcast();
                });

                thread.Start();
                //thread.Join();
            }
            catch (ThreadAbortException e)
            {
                DpprException ex = new DpprException
                {
                    file_name = "JobCashSalesDetailUpdateSync",
                    exception = e.Message,
                    time = DateTime.Now.ToString()
                };
                LocalDB.InsertException(ex);

                Console.WriteLine(Constants.Thread_Exception + e.Message);
            }
        }
    }
}