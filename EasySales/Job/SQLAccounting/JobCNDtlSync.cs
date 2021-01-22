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
    public class JobCNDtlSync : IJob
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
                    slog.action_identifier = Constants.Action_CreditNoteDetailsSync;                               /*check again */
                    slog.action_details = Constants.Tbl_cms_creditnote_details + Constants.Is_Starting;                /*check again */
                    slog.action_failure = 0;
                    slog.action_failure_message = string.Empty;
                    slog.action_time = DateTime.Now.ToString();

                    DateTime startTime = DateTime.Now;

                    LocalDB.InsertSyncLog(slog);
                    logger.message = "----------------------------------------------------------------------------";
                    logger.Broadcast();
                    logger.message = "Credit note details sync is running";
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

                    dynamic lRptVar, lRptVarARCNDtl;
                    string lSQL, ARCNDtlQuery;
                    string cnCode, itemCode, itemName, itemPrice, itemQty, itemUom, discount, total, refNo;
                    string query, updateQuery;

                    HashSet<string> queryList = new HashSet<string>();
                    Database _mysql = new Database();

                    dynamic jsonRule = new CheckBackendRule()
                                           .CheckTablesExist()
                                           .GetSettingByTableName("cms_creditnote_details");
                    int include_arcndtl = 0;
                    if (jsonRule.Count > 0)
                    {
                        foreach (var rule in jsonRule)
                        {
                            dynamic _include_arcndtl = rule.ar_cndtl; //include ar customer details or not
                            if (_include_arcndtl != null)
                            {
                                if (_include_arcndtl != "0")
                                {
                                    include_arcndtl = _include_arcndtl;
                                }
                            }
                        }
                    }
                    else
                    {
                        goto ENDJOB;
                    }

                    Dictionary<string, string> cms_updated_time = _mysql.GetUpdatedTime1YearInterval("cms_creditnote_details"); //{[updated_at, 28/06/2020 5:38:23 PM]}
                    string updated_at = string.Empty;

                    if (cms_updated_time.Count > 0)
                    {
                        updated_at = cms_updated_time["updated_at"].ToString().MSSQLdate();
                    }

                    string activeCNDtl = "SELECT dtl.id, cn.cn_code, cn.cn_date, dtl.ref_no FROM cms_creditnote AS cn LEFT JOIN cms_creditnote_details AS dtl ON cn.cn_code = dtl.cn_code WHERE active_status = 1 ";
                    if (cms_updated_time.Count > 0)
                    {
                        activeCNDtl += " AND cn_date >= '" + updated_at + "' ";
                    }

                    ArrayList inDBactiveCNDtl = _mysql.Select(activeCNDtl);
                    logger.Broadcast("Active credit note details in DB: " + inDBactiveCNDtl.Count);
                    Dictionary<string, string> uniqueKeyList = new Dictionary<string, string>();
                    for (int i = 0; i < inDBactiveCNDtl.Count; i++)
                    {
                        Dictionary<string, string> each = (Dictionary<string, string>)inDBactiveCNDtl[i];
                        string refno = each["ref_no"].ToString();
                        string cncode = each["cn_code"].ToString();
                        string id = each["id"].ToString();
                        string _uniqueKey = cncode + refno;
                        string uniqueKey = _uniqueKey.ToLower();
                        uniqueKeyList.Add(id, uniqueKey);
                    }
                    inDBactiveCNDtl.Clear();

                    lSQL = "SELECT SL_CN.DOCNO, SL_CN.DOCKEY, SL_CNDTL.ITEMCODE, SL_CNDTL.DESCRIPTION, SL_CNDTL.UNITPRICE, SL_CNDTL.QTY, SL_CNDTL.UOM, SL_CNDTL.LOCALAMOUNT, SL_CNDTL.DISC, SL_CNDTL.DTLKEY FROM SL_CNDTL LEFT JOIN SL_CN ON SL_CN.DOCKEY = SL_CNDTL.DOCKEY ";
                    ARCNDtlQuery = "SELECT AR_CN.DOCKEY, AR_CN.DOCNO, AR_CN.DOCDATE, AR_CNDTL.DTLKEY, AR_CNDTL.DESCRIPTION, AR_CNDTL.LOCALAMOUNT FROM AR_CNDTL LEFT JOIN AR_CN ON AR_CN.DOCKEY = AR_CNDTL.DOCKEY ";

                    query = "INSERT INTO cms_creditnote_details(cn_code, item_code, item_name, item_price, quantity, uom, discount, total_price, ref_no) VALUES ";
                    updateQuery = " ON DUPLICATE KEY UPDATE quantity = VALUES(quantity), total_price = VALUES(total_price), item_price = VALUES(item_price), discount = VALUES(discount);";

                    if (cms_updated_time.Count > 0)
                    {
                        lSQL += " WHERE SL_CN.DOCDATE >= '" + updated_at + "' ORDER BY DOCDATE DESC";
                        ARCNDtlQuery += " WHERE AR_CN.DOCDATE >= '" + updated_at + "' ORDER BY AR_CN.DOCDATE DESC";
                    }
                    else
                    {
                        lSQL += " ORDER BY DOCDATE DESC";
                        ARCNDtlQuery += "ORDER BY AR_CN.DOCDATE DESC";
                    }

                    try
                    {
                        lRptVar = ComServer.DBManager.NewDataSet(lSQL);
                        lRptVar.First();

                        while (!lRptVar.eof)
                        {
                            RecordCount++;

                            cnCode = lRptVar.FindField("DOCNO").AsString;
                            itemCode = lRptVar.FindField("ITEMCODE").AsString;
                            itemName = lRptVar.FindField("DESCRIPTION").AsString;
                            itemPrice = lRptVar.FindField("UNITPRICE").AsString;
                            itemQty = lRptVar.FindField("QTY").AsString;
                            itemUom = lRptVar.FindField("UOM").AsString;
                            discount = lRptVar.FindField("DISC").AsString;
                            total = lRptVar.FindField("LOCALAMOUNT").AsString; //LOCALAMOUNT
                            refNo = lRptVar.FindField("DTLKEY").AsString;

                            string unique = cnCode + refNo;
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

                            Database.Sanitize(ref cnCode);
                            Database.Sanitize(ref itemCode);
                            Database.Sanitize(ref itemName);
                            Database.Sanitize(ref itemPrice);
                            Database.Sanitize(ref itemQty);
                            Database.Sanitize(ref itemUom);
                            Database.Sanitize(ref discount);
                            Database.Sanitize(ref total);
                            Database.Sanitize(ref refNo);

                            string Values = string.Format("('{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}','{8}')", cnCode, itemCode, itemName, itemPrice, itemQty, itemUom, discount, total, refNo);

                            queryList.Add(Values);

                            if (queryList.Count % 2000 == 0)
                            {
                                string tmp_query = query;
                                tmp_query += string.Join(", ", queryList);
                                tmp_query += updateQuery;

                                _mysql.Insert(tmp_query);

                                queryList.Clear();
                                tmp_query = string.Empty;

                                logger.message = string.Format("{0} credit note details records is inserted", RecordCount);
                                logger.Broadcast();
                            }

                            lRptVar.Next();
                        }

                        if (queryList.Count > 0)
                        {
                            string tmp_query = query;
                            tmp_query += string.Join(", ", queryList);
                            tmp_query += updateQuery;

                            _mysql.Insert(tmp_query);

                            queryList.Clear();
                            tmp_query = string.Empty;

                            logger.message = string.Format("{0} credit note details records is inserted", RecordCount);
                            logger.Broadcast();
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
                                file_name = "SQLAccounting + JobCNDtlSync",
                                exception = exc.Message,
                                time = DateTime.Now.ToString()
                            };
                            LocalDB.InsertException(exception);
                        }
                    }

                    if(include_arcndtl == 1)
                    {
                        try
                        {
                            lRptVarARCNDtl = ComServer.DBManager.NewDataSet(ARCNDtlQuery);
                            lRptVarARCNDtl.First();

                            while (!lRptVarARCNDtl.eof)
                            {
                                RecordCount++;

                                cnCode = lRptVarARCNDtl.FindField("DOCNO").AsString;
                                itemName = lRptVarARCNDtl.FindField("DESCRIPTION").AsString;
                                total = lRptVarARCNDtl.FindField("LOCALAMOUNT").AsString; 
                                itemPrice = lRptVarARCNDtl.FindField("LOCALAMOUNT").AsString; //same amt as localamount
                                refNo = lRptVarARCNDtl.FindField("DTLKEY").AsString;
                                itemCode = string.Empty;
                                itemUom = string.Empty;
                                discount = string.Empty;
                                itemQty = "1";

                                string unique = cnCode + refNo;
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

                                Database.Sanitize(ref cnCode);
                                Database.Sanitize(ref itemName);
                                Database.Sanitize(ref total);
                                Database.Sanitize(ref refNo);

                                string Values = string.Format("('{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}','{8}')", cnCode, itemCode, itemName, itemPrice, itemQty, itemUom, discount, total, refNo);

                                queryList.Add(Values);

                                if (queryList.Count % 2000 == 0)
                                {
                                    string tmp_query = query;
                                    tmp_query += string.Join(", ", queryList);
                                    tmp_query += updateQuery;

                                    _mysql.Insert(tmp_query);

                                    queryList.Clear();
                                    tmp_query = string.Empty;

                                    logger.message = string.Format("{0} customer credit note details records is inserted", RecordCount);
                                    logger.Broadcast();
                                }

                                lRptVarARCNDtl.Next();
                            }

                            if (queryList.Count > 0)
                            {
                                query = query + string.Join(", ", queryList) + updateQuery;

                                _mysql.Insert(query);

                                logger.message = string.Format("{0} customer credit note details records is inserted", RecordCount);
                                logger.Broadcast();
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
                                    file_name = "SQLAccounting + JobARCNDtlSync",
                                    exception = exc.Message,
                                    time = DateTime.Now.ToString()
                                };
                                LocalDB.InsertException(exception);
                            }
                        }
                    }

                    if (uniqueKeyList.Count > 0)
                    {
                        logger.Broadcast("Total credit note details records to be deactivated: " + uniqueKeyList.Count);
                        Database mysql = new Database();

                        HashSet<string> deactivateId = new HashSet<string>();
                        for (int i = 0; i < uniqueKeyList.Count; i++)
                        {
                            string _id = uniqueKeyList.ElementAt(i).Key;
                            deactivateId.Add(_id);
                        }

                        string ToBeDeactivate = "'" + string.Join("','", deactivateId) + "'";
                        Console.WriteLine(ToBeDeactivate);

                        string inactive = "UPDATE cms_creditnote_details SET active_status = 0 WHERE id IN (" + ToBeDeactivate + ")";
                        mysql.Insert(inactive);

                        logger.Broadcast(uniqueKeyList.Count + " credit note details records deactivated");

                        uniqueKeyList.Clear();
                    }
                    _mysql.Insert("INSERT INTO cms_update_time(table_name, updated_at) VALUES ('cms_creditnote_details', NOW()) ON DUPLICATE KEY UPDATE table_name = VALUES(table_name), updated_at = VALUES(updated_at)");

                    ENDJOB:

                    slog.action_identifier = Constants.Action_CreditNoteDetailsSync;
                    slog.action_details = Constants.Tbl_cms_creditnote_details + Constants.Is_Finished + string.Format(" ({0}) records", RecordCount);
                    slog.action_failure = 0;
                    slog.action_failure_message = string.Empty;
                    slog.action_time = DateTime.Now.ToString();

                    DateTime endTime = DateTime.Now;
                    TimeSpan ts = endTime - startTime;
                    Console.WriteLine("Completed in: " + ts.TotalSeconds.ToString("F"));

                    LocalDB.InsertSyncLog(slog);

                    logger.message = "Credit note details finished in (" + ts.TotalSeconds.ToString("F") + " seconds)";
                    logger.Broadcast();
                });

                thread.Start();
                //thread.Join();
            }
            catch (ThreadAbortException e)
            {
                DpprException ex = new DpprException
                {
                    file_name = "JobCreditNoteDetailUpdateSync",
                    exception = e.Message,
                    time = DateTime.Now.ToString()
                };
                LocalDB.InsertException(ex);

                Console.WriteLine(Constants.Thread_Exception + e.Message);
            }
        }
    }
}