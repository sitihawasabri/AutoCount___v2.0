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
    public class JobStockCardSync : IJob
    {
        private string sync_today_only = "0";
        public void ExecuteSyncTodayOnly(string sync_today_only)
        {
            GlobalLogger logger = new GlobalLogger();
            this.sync_today_only = sync_today_only;
            logger.Broadcast("sync_today_only:" + this.sync_today_only);
            Execute();
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
                    slog.action_identifier = Constants.Action_StockCardSync;                      
                    slog.action_details = Constants.Tbl_cms_stock_card + Constants.Is_Starting;    
                    slog.action_failure = 0;
                    slog.action_failure_message = string.Empty;
                    slog.action_time = DateTime.Now.ToString();

                    DateTime startTime = DateTime.Now;

                    LocalDB.InsertSyncLog(slog);
                    logger.message = "----------------------------------------------------------------------------";
                    logger.Broadcast();
                    logger.message = "Stock Card sync is running";
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

                    dynamic lDataSet, lDataSet1, lDataSet2, lDataSet3, lDataSetDN, lDataSetCN;
                    string lSQLXF, lSQLCS, lSQLDO, lSQLIV, lSQLCN, lSQLDN;
                    string TRANSNO, ITEMCODE, LOCATION, DEFUOM_ST, POSTDATE, DOCTYPE, DOCNO, DOCKEY, DTLKEY, QTY, PRICE, REFTO;
                    string query, updateQuery;

                    HashSet<string> queryList = new HashSet<string>();
                    Database mysql = new Database();
                    Dictionary<string, string> cms_updated_time = mysql.GetUpdatedTime1YearInterval("cms_stock_card"); //
                    //{[updated_at, 28/06/2020 5:38:23 PM]}
                    string updated_at = string.Empty;

                    logger.Broadcast("sync_today_only:" + this.sync_today_only);

                    if (this.sync_today_only == "1")
                    {
                        cms_updated_time = mysql.GetUpdatedTimeToday("cms_stock_card");
                        
                        if (cms_updated_time.Count > 0)
                        {
                            updated_at = cms_updated_time["updated_at"].ToString().MSSQLdate();
                            logger.Broadcast("Sync today only: " + updated_at);
                        }
                    }
                    else
                    {
                        if (cms_updated_time.Count > 0)
                        {
                            updated_at = cms_updated_time["updated_at"].ToString().MSSQLdate();
                        }
                    }

                    string deactivateOldRecords = "SELECT id, stock_dtl_key, doc_no FROM cms_stock_card WHERE cancelled = 'F' ";

                    //lSQL = "SELECT ST_TR.*, ST_ITEM_UOM.UOM FROM ST_TR LEFT JOIN ST_ITEM_UOM ON ST_TR.ITEMCODE = ST_ITEM_UOM.CODE ";
                    lSQLXF = "SELECT ST_TR.*, ST_XFDTL.UOM FROM ST_TR LEFT JOIN ST_XFDTL ON(ST_TR.DTLKEY = ST_XFDTL.DTLKEY) WHERE ST_TR.DOCTYPE = 'XF' ";
                    lSQLCS = "SELECT ST_TR.*, SL_CSDTL.UOM FROM ST_TR LEFT JOIN SL_CSDTL ON(ST_TR.DTLKEY = SL_CSDTL.DTLKEY) WHERE ST_TR.DOCTYPE = 'CS' ";
                    lSQLIV = "SELECT ST_TR.*, SL_IVDTL.UOM FROM ST_TR LEFT JOIN SL_IVDTL ON(ST_TR.DTLKEY = SL_IVDTL.DTLKEY) WHERE ST_TR.DOCTYPE = 'IV' ";
                    lSQLDO = "SELECT ST_TR.*, SL_DODTL.UOM FROM ST_TR LEFT JOIN SL_DODTL ON(ST_TR.DTLKEY = SL_DODTL.DTLKEY) WHERE ST_TR.DOCTYPE = 'DO' ";
                    lSQLCN = "SELECT ST_TR.*, SL_CNDTL.UOM FROM ST_TR LEFT JOIN SL_CNDTL ON(ST_TR.DTLKEY = SL_CNDTL.DTLKEY) WHERE ST_TR.DOCTYPE = 'CN' ";
                    lSQLDN = "SELECT ST_TR.*, SL_DNDTL.UOM FROM ST_TR LEFT JOIN SL_DNDTL ON(ST_TR.DTLKEY = SL_DNDTL.DTLKEY) WHERE ST_TR.DOCTYPE = 'DN' ";
                    
                    if (cms_updated_time.Count > 0)
                    {
                        lSQLXF += " AND ST_TR.POSTDATE >='" + updated_at + "' ORDER BY TRANSNO DESC";
                        lSQLCS += " AND ST_TR.POSTDATE >='" + updated_at + "' ORDER BY TRANSNO DESC";
                        lSQLIV += " AND ST_TR.POSTDATE >='" + updated_at + "' ORDER BY TRANSNO DESC";
                        lSQLDO += " AND ST_TR.POSTDATE >='" + updated_at + "' ORDER BY TRANSNO DESC";
                        deactivateOldRecords += " AND doc_date >='" + updated_at + "'";
                    }
                    else
                    {
                        lSQLXF += " ORDER BY TRANSNO DESC";
                        lSQLCS += " ORDER BY TRANSNO DESC";
                        lSQLIV += " ORDER BY TRANSNO DESC";
                        lSQLDO += " ORDER BY TRANSNO DESC";
                    }

                    Console.WriteLine(lSQLXF);
                    Console.WriteLine(lSQLCS);

                    ArrayList inDBactiveTrans = mysql.Select(deactivateOldRecords);
                    logger.Broadcast("Active stock card transactions in DB: " + inDBactiveTrans.Count);
                    Dictionary<string, string> uniqueKeyList = new Dictionary<string, string>();
                    for (int i = 0; i < inDBactiveTrans.Count; i++)
                    {
                        Dictionary<string, string> each = (Dictionary<string, string>)inDBactiveTrans[i];
                        string id = each["id"].ToString();
                        string stock_dtl_key = each["stock_dtl_key"].ToString();
                        string doc_no = each["doc_no"].ToString();
                        string unique = stock_dtl_key + doc_no;
                        string uniqueLowercase = unique.ToLower();
                        uniqueKeyList.Add(id, uniqueLowercase);
                    }
                    inDBactiveTrans.Clear();

                    query = "INSERT INTO cms_stock_card(stock_dtl_key, product_code, location, unit_uom, doc_date, doc_type, doc_no, doc_key, dtl_key, quantity, unit_price, refer_to) VALUES ";
                    updateQuery = " ON DUPLICATE KEY UPDATE product_code = VALUES(product_code), location = VALUES(location), unit_uom = VALUES(unit_uom),doc_date = VALUES(doc_date), doc_type = VALUES(doc_type), doc_no = VALUES(doc_no), doc_key = VALUES(doc_key), dtl_key = VALUES(dtl_key), quantity = VALUES(quantity), unit_price = VALUES(unit_price), refer_to = VALUES(refer_to) "; //cust_code

                    try
                    {
                        lDataSet = ComServer.DBManager.NewDataSet(lSQLXF);
                        lDataSet.First();

                        while (!lDataSet.eof)
                        {
                            RecordCount++;

                            TRANSNO = lDataSet.FindField("TRANSNO").AsString;
                            ITEMCODE = lDataSet.FindField("ITEMCODE").AsString;
                            LOCATION = lDataSet.FindField("LOCATION").AsString;
                            DEFUOM_ST = lDataSet.FindField("UOM").AsString;
                            POSTDATE = lDataSet.FindField("POSTDATE").AsString;
                            POSTDATE = Convert.ToDateTime(POSTDATE).ToString("yyyy-MM-dd");
                            DOCTYPE = lDataSet.FindField("DOCTYPE").AsString;
                            DOCNO = lDataSet.FindField("DOCNO").AsString;
                            DOCKEY = lDataSet.FindField("DOCKEY").AsString;
                            DTLKEY = lDataSet.FindField("DTLKEY").AsString;
                            QTY = lDataSet.FindField("QTY").AsString;
                            PRICE = lDataSet.FindField("PRICE").AsString;
                            REFTO = lDataSet.FindField("REFTO").AsString;

                            string unique = TRANSNO + DOCNO;
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

                            Database.Sanitize(ref TRANSNO);
                            Database.Sanitize(ref ITEMCODE);
                            Database.Sanitize(ref LOCATION);
                            Database.Sanitize(ref DEFUOM_ST);
                            Database.Sanitize(ref POSTDATE);
                            Database.Sanitize(ref DOCTYPE);
                            Database.Sanitize(ref DOCNO);
                            Database.Sanitize(ref DOCKEY);
                            Database.Sanitize(ref DTLKEY);
                            Database.Sanitize(ref QTY);
                            Database.Sanitize(ref PRICE);
                            Database.Sanitize(ref REFTO);
                            //stock_dtl_key, product_code, location, unit_uom, doc_date, doc_type, doc_no, doc_key, dtl_key, quantity, unit_price, refer_to
                            string Values = string.Format("('{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}','{8}','{9}','{10}','{11}')", TRANSNO, ITEMCODE, LOCATION, DEFUOM_ST, POSTDATE, DOCTYPE, DOCNO, DOCKEY, DTLKEY, QTY, PRICE, REFTO);

                            queryList.Add(Values);

                            if (queryList.Count % 2000 == 0)
                            {
                                string tmp_query = query;
                                tmp_query += string.Join(", ", queryList);
                                tmp_query += updateQuery;

                                mysql.Insert(tmp_query);

                                queryList.Clear();
                                tmp_query = string.Empty;

                                logger.message = string.Format("{0} stock card records is inserted", RecordCount);
                                logger.Broadcast();
                            }

                            lDataSet.Next();
                        }

                        if (queryList.Count > 0)
                        {
                            string tmp_query = query;
                            tmp_query += string.Join(", ", queryList);
                            tmp_query += updateQuery;

                            mysql.Insert(tmp_query);

                            queryList.Clear();
                            tmp_query = string.Empty;

                            logger.message = string.Format("{0} stock card records is inserted", RecordCount);
                            logger.Broadcast();
                        }

                        mysql.Insert("INSERT INTO cms_update_time(table_name, updated_at) VALUES ('cms_stock_card', NOW()) ON DUPLICATE KEY UPDATE table_name = VALUES(table_name), updated_at = VALUES(updated_at)");

                    }
                    catch (Exception exx)
                    {
                        try
                        {
                            Console.WriteLine(exx.Message);
                            goto CHECKAGAIN;
                        }
                        catch (Exception exc)
                        {
                            DpprException exception = new DpprException()
                            {
                                file_name = "SQLAccounting + JobStockCardSync",
                                exception = exc.Message,
                                time = DateTime.Now.ToString()
                            };
                            LocalDB.InsertException(exception);
                        }
                    }

                    try
                    {
                        lDataSet1 = ComServer.DBManager.NewDataSet(lSQLCS);
                        lDataSet1.First();

                        while (!lDataSet1.eof)
                        {
                            RecordCount++;

                            TRANSNO = lDataSet1.FindField("TRANSNO").AsString;
                            ITEMCODE = lDataSet1.FindField("ITEMCODE").AsString;
                            LOCATION = lDataSet1.FindField("LOCATION").AsString;
                            DEFUOM_ST = lDataSet1.FindField("UOM").AsString;
                            POSTDATE = lDataSet1.FindField("POSTDATE").AsString;
                            POSTDATE = Convert.ToDateTime(POSTDATE).ToString("yyyy-MM-dd");
                            DOCTYPE = lDataSet1.FindField("DOCTYPE").AsString;
                            DOCNO = lDataSet1.FindField("DOCNO").AsString;
                            DOCKEY = lDataSet1.FindField("DOCKEY").AsString;
                            DTLKEY = lDataSet1.FindField("DTLKEY").AsString;
                            QTY = lDataSet1.FindField("QTY").AsString;
                            PRICE = lDataSet1.FindField("PRICE").AsString;
                            REFTO = lDataSet1.FindField("REFTO").AsString;

                            string unique = TRANSNO + DOCNO;
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

                            Database.Sanitize(ref TRANSNO);
                            Database.Sanitize(ref ITEMCODE);
                            Database.Sanitize(ref LOCATION);
                            Database.Sanitize(ref DEFUOM_ST);
                            Database.Sanitize(ref POSTDATE);
                            Database.Sanitize(ref DOCTYPE);
                            Database.Sanitize(ref DOCNO);
                            Database.Sanitize(ref DOCKEY);
                            Database.Sanitize(ref DTLKEY);
                            Database.Sanitize(ref QTY);
                            Database.Sanitize(ref PRICE);
                            Database.Sanitize(ref REFTO);
                            //stock_dtl_key, product_code, location, unit_uom, doc_date, doc_type, doc_no, doc_key, dtl_key, quantity, unit_price, refer_to
                            string Values = string.Format("('{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}','{8}','{9}','{10}','{11}')", TRANSNO, ITEMCODE, LOCATION, DEFUOM_ST, POSTDATE, DOCTYPE, DOCNO, DOCKEY, DTLKEY, QTY, PRICE, REFTO);

                            queryList.Add(Values);

                            if (queryList.Count % 2000 == 0)
                            {
                                string tmp_query = query;
                                tmp_query += string.Join(", ", queryList);
                                tmp_query += updateQuery;

                                mysql.Insert(tmp_query);

                                queryList.Clear();
                                tmp_query = string.Empty;

                                logger.message = string.Format("{0} stock card records is inserted", RecordCount);
                                logger.Broadcast();
                            }

                            lDataSet1.Next();
                        }

                        if (queryList.Count > 0)
                        {
                            string tmp_query = query;
                            tmp_query += string.Join(", ", queryList);
                            tmp_query += updateQuery;

                            mysql.Insert(tmp_query);

                            queryList.Clear();
                            tmp_query = string.Empty;

                            logger.message = string.Format("{0} stock card records is inserted", RecordCount);
                            logger.Broadcast();
                        }

                        mysql.Insert("INSERT INTO cms_update_time(table_name, updated_at) VALUES ('cms_stock_card', NOW()) ON DUPLICATE KEY UPDATE table_name = VALUES(table_name), updated_at = VALUES(updated_at)");

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
                                file_name = "SQLAccounting + JobStockCardSync",
                                exception = exc.Message,
                                time = DateTime.Now.ToString()
                            };
                            LocalDB.InsertException(exception);
                        }
                    }


                    try
                    {
                        lDataSet2 = ComServer.DBManager.NewDataSet(lSQLIV);
                        lDataSet2.First();

                        while (!lDataSet2.eof)
                        {
                            RecordCount++;

                            TRANSNO = lDataSet2.FindField("TRANSNO").AsString;
                            ITEMCODE = lDataSet2.FindField("ITEMCODE").AsString;
                            LOCATION = lDataSet2.FindField("LOCATION").AsString;
                            DEFUOM_ST = lDataSet2.FindField("UOM").AsString;
                            POSTDATE = lDataSet2.FindField("POSTDATE").AsString;
                            POSTDATE = Convert.ToDateTime(POSTDATE).ToString("yyyy-MM-dd");
                            DOCTYPE = lDataSet2.FindField("DOCTYPE").AsString;
                            DOCNO = lDataSet2.FindField("DOCNO").AsString;
                            DOCKEY = lDataSet2.FindField("DOCKEY").AsString;
                            DTLKEY = lDataSet2.FindField("DTLKEY").AsString;
                            QTY = lDataSet2.FindField("QTY").AsString;
                            PRICE = lDataSet2.FindField("PRICE").AsString;
                            REFTO = lDataSet2.FindField("REFTO").AsString;

                            string unique = TRANSNO + DOCNO;
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

                            Database.Sanitize(ref TRANSNO);
                            Database.Sanitize(ref ITEMCODE);
                            Database.Sanitize(ref LOCATION);
                            Database.Sanitize(ref DEFUOM_ST);
                            Database.Sanitize(ref POSTDATE);
                            Database.Sanitize(ref DOCTYPE);
                            Database.Sanitize(ref DOCNO);
                            Database.Sanitize(ref DOCKEY);
                            Database.Sanitize(ref DTLKEY);
                            Database.Sanitize(ref QTY);
                            Database.Sanitize(ref PRICE);
                            Database.Sanitize(ref REFTO);
                            //stock_dtl_key, product_code, location, unit_uom, doc_date, doc_type, doc_no, doc_key, dtl_key, quantity, unit_price, refer_to
                            string Values = string.Format("('{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}','{8}','{9}','{10}','{11}')", TRANSNO, ITEMCODE, LOCATION, DEFUOM_ST, POSTDATE, DOCTYPE, DOCNO, DOCKEY, DTLKEY, QTY, PRICE, REFTO);

                            queryList.Add(Values);

                            if (queryList.Count % 2000 == 0)
                            {
                                string tmp_query = query;
                                tmp_query += string.Join(", ", queryList);
                                tmp_query += updateQuery;

                                mysql.Insert(tmp_query);

                                queryList.Clear();
                                tmp_query = string.Empty;

                                logger.message = string.Format("{0} stock card records is inserted", RecordCount);
                                logger.Broadcast();
                            }

                            lDataSet2.Next();
                        }

                        if (queryList.Count > 0)
                        {
                            string tmp_query = query;
                            tmp_query += string.Join(", ", queryList);
                            tmp_query += updateQuery;

                            mysql.Insert(tmp_query);

                            queryList.Clear();
                            tmp_query = string.Empty;

                            logger.message = string.Format("{0} stock card records is inserted", RecordCount);
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
                                file_name = "SQLAccounting + JobStockCardSync",
                                exception = exc.Message,
                                time = DateTime.Now.ToString()
                            };
                            LocalDB.InsertException(exception);
                        }
                    }

                    try
                    {
                        lDataSet3 = ComServer.DBManager.NewDataSet(lSQLDO);
                        lDataSet3.First();

                        while (!lDataSet3.eof)
                        {
                            RecordCount++;

                            TRANSNO = lDataSet3.FindField("TRANSNO").AsString;
                            ITEMCODE = lDataSet3.FindField("ITEMCODE").AsString;
                            LOCATION = lDataSet3.FindField("LOCATION").AsString;
                            DEFUOM_ST = lDataSet3.FindField("UOM").AsString;
                            POSTDATE = lDataSet3.FindField("POSTDATE").AsString;
                            POSTDATE = Convert.ToDateTime(POSTDATE).ToString("yyyy-MM-dd");
                            DOCTYPE = lDataSet3.FindField("DOCTYPE").AsString;
                            DOCNO = lDataSet3.FindField("DOCNO").AsString;
                            DOCKEY = lDataSet3.FindField("DOCKEY").AsString;
                            DTLKEY = lDataSet3.FindField("DTLKEY").AsString;
                            QTY = lDataSet3.FindField("QTY").AsString;
                            PRICE = lDataSet3.FindField("PRICE").AsString;
                            REFTO = lDataSet3.FindField("REFTO").AsString;

                            string unique = TRANSNO + DOCNO;
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

                            Database.Sanitize(ref TRANSNO);
                            Database.Sanitize(ref ITEMCODE);
                            Database.Sanitize(ref LOCATION);
                            Database.Sanitize(ref DEFUOM_ST);
                            Database.Sanitize(ref POSTDATE);
                            Database.Sanitize(ref DOCTYPE);
                            Database.Sanitize(ref DOCNO);
                            Database.Sanitize(ref DOCKEY);
                            Database.Sanitize(ref DTLKEY);
                            Database.Sanitize(ref QTY);
                            Database.Sanitize(ref PRICE);
                            Database.Sanitize(ref REFTO);
                            //stock_dtl_key, product_code, location, unit_uom, doc_date, doc_type, doc_no, doc_key, dtl_key, quantity, unit_price, refer_to
                            string Values = string.Format("('{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}','{8}','{9}','{10}','{11}')", TRANSNO, ITEMCODE, LOCATION, DEFUOM_ST, POSTDATE, DOCTYPE, DOCNO, DOCKEY, DTLKEY, QTY, PRICE, REFTO);

                            queryList.Add(Values);

                            if (queryList.Count % 2000 == 0)
                            {
                                string tmp_query = query;
                                tmp_query += string.Join(", ", queryList);
                                tmp_query += updateQuery;

                                mysql.Insert(tmp_query);

                                queryList.Clear();
                                tmp_query = string.Empty;

                                logger.message = string.Format("{0} stock card records is inserted", RecordCount);
                                logger.Broadcast();
                            }

                            lDataSet3.Next();
                        }

                        if (queryList.Count > 0)
                        {
                            string tmp_query = query;
                            tmp_query += string.Join(", ", queryList);
                            tmp_query += updateQuery;

                            mysql.Insert(tmp_query);

                            queryList.Clear();
                            tmp_query = string.Empty;

                            logger.message = string.Format("{0} stock card records is inserted", RecordCount);
                            logger.Broadcast();
                        }

                        mysql.Insert("INSERT INTO cms_update_time(table_name, updated_at) VALUES ('cms_stock_card', NOW()) ON DUPLICATE KEY UPDATE table_name = VALUES(table_name), updated_at = VALUES(updated_at)");

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
                                file_name = "SQLAccounting + JobStockCardSync",
                                exception = exc.Message,
                                time = DateTime.Now.ToString()
                            };
                            LocalDB.InsertException(exception);
                        }
                    }

                    try
                    {
                        lDataSetCN = ComServer.DBManager.NewDataSet(lSQLCN);
                        lDataSetCN.First();

                        while (!lDataSetCN.eof)
                        {
                            RecordCount++;

                            TRANSNO = lDataSetCN.FindField("TRANSNO").AsString;
                            ITEMCODE = lDataSetCN.FindField("ITEMCODE").AsString;
                            LOCATION = lDataSetCN.FindField("LOCATION").AsString;
                            DEFUOM_ST = lDataSetCN.FindField("UOM").AsString;
                            POSTDATE = lDataSetCN.FindField("POSTDATE").AsString;
                            POSTDATE = Convert.ToDateTime(POSTDATE).ToString("yyyy-MM-dd");
                            DOCTYPE = lDataSetCN.FindField("DOCTYPE").AsString;
                            DOCNO = lDataSetCN.FindField("DOCNO").AsString;
                            DOCKEY = lDataSetCN.FindField("DOCKEY").AsString;
                            DTLKEY = lDataSetCN.FindField("DTLKEY").AsString;
                            QTY = lDataSetCN.FindField("QTY").AsString;
                            PRICE = lDataSetCN.FindField("PRICE").AsString;
                            REFTO = lDataSetCN.FindField("REFTO").AsString;

                            string unique = TRANSNO + DOCNO;
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

                            Database.Sanitize(ref TRANSNO);
                            Database.Sanitize(ref ITEMCODE);
                            Database.Sanitize(ref LOCATION);
                            Database.Sanitize(ref DEFUOM_ST);
                            Database.Sanitize(ref POSTDATE);
                            Database.Sanitize(ref DOCTYPE);
                            Database.Sanitize(ref DOCNO);
                            Database.Sanitize(ref DOCKEY);
                            Database.Sanitize(ref DTLKEY);
                            Database.Sanitize(ref QTY);
                            Database.Sanitize(ref PRICE);
                            Database.Sanitize(ref REFTO);
                            //stock_dtl_key, product_code, location, unit_uom, doc_date, doc_type, doc_no, doc_key, dtl_key, quantity, unit_price, refer_to
                            string Values = string.Format("('{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}','{8}','{9}','{10}','{11}')", TRANSNO, ITEMCODE, LOCATION, DEFUOM_ST, POSTDATE, DOCTYPE, DOCNO, DOCKEY, DTLKEY, QTY, PRICE, REFTO);

                            queryList.Add(Values);

                            if (queryList.Count % 2000 == 0)
                            {
                                string tmp_query = query;
                                tmp_query += string.Join(", ", queryList);
                                tmp_query += updateQuery;

                                mysql.Insert(tmp_query);

                                queryList.Clear();
                                tmp_query = string.Empty;

                                logger.message = string.Format("{0} stock card records is inserted", RecordCount);
                                logger.Broadcast();
                            }

                            lDataSetCN.Next();
                        }

                        if (queryList.Count > 0)
                        {
                            string tmp_query = query;
                            tmp_query += string.Join(", ", queryList);
                            tmp_query += updateQuery;

                            mysql.Insert(tmp_query);

                            queryList.Clear();
                            tmp_query = string.Empty;

                            logger.message = string.Format("{0} stock card records is inserted", RecordCount);
                            logger.Broadcast();
                        }

                        mysql.Insert("INSERT INTO cms_update_time(table_name, updated_at) VALUES ('cms_stock_card', NOW()) ON DUPLICATE KEY UPDATE table_name = VALUES(table_name), updated_at = VALUES(updated_at)");

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
                                file_name = "SQLAccounting + JobStockCardSync",
                                exception = exc.Message,
                                time = DateTime.Now.ToString()
                            };
                            LocalDB.InsertException(exception);
                        }
                    }

                    try
                    {
                        lDataSetDN = ComServer.DBManager.NewDataSet(lSQLDN);
                        lDataSetDN.First();

                        while (!lDataSetDN.eof)
                        {
                            RecordCount++;

                            TRANSNO = lDataSetDN.FindField("TRANSNO").AsString;
                            ITEMCODE = lDataSetDN.FindField("ITEMCODE").AsString;
                            LOCATION = lDataSetDN.FindField("LOCATION").AsString;
                            DEFUOM_ST = lDataSetDN.FindField("UOM").AsString;
                            POSTDATE = lDataSetDN.FindField("POSTDATE").AsString;
                            POSTDATE = Convert.ToDateTime(POSTDATE).ToString("yyyy-MM-dd");
                            DOCTYPE = lDataSetDN.FindField("DOCTYPE").AsString;
                            DOCNO = lDataSetDN.FindField("DOCNO").AsString;
                            DOCKEY = lDataSetDN.FindField("DOCKEY").AsString;
                            DTLKEY = lDataSetDN.FindField("DTLKEY").AsString;
                            QTY = lDataSetDN.FindField("QTY").AsString;
                            PRICE = lDataSetDN.FindField("PRICE").AsString;
                            REFTO = lDataSetDN.FindField("REFTO").AsString;

                            string unique = TRANSNO + DOCNO;
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

                            Database.Sanitize(ref TRANSNO);
                            Database.Sanitize(ref ITEMCODE);
                            Database.Sanitize(ref LOCATION);
                            Database.Sanitize(ref DEFUOM_ST);
                            Database.Sanitize(ref POSTDATE);
                            Database.Sanitize(ref DOCTYPE);
                            Database.Sanitize(ref DOCNO);
                            Database.Sanitize(ref DOCKEY);
                            Database.Sanitize(ref DTLKEY);
                            Database.Sanitize(ref QTY);
                            Database.Sanitize(ref PRICE);
                            Database.Sanitize(ref REFTO);
                            //stock_dtl_key, product_code, location, unit_uom, doc_date, doc_type, doc_no, doc_key, dtl_key, quantity, unit_price, refer_to
                            string Values = string.Format("('{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}','{8}','{9}','{10}','{11}')", TRANSNO, ITEMCODE, LOCATION, DEFUOM_ST, POSTDATE, DOCTYPE, DOCNO, DOCKEY, DTLKEY, QTY, PRICE, REFTO);

                            queryList.Add(Values);

                            if (queryList.Count % 2000 == 0)
                            {
                                string tmp_query = query;
                                tmp_query += string.Join(", ", queryList);
                                tmp_query += updateQuery;

                                mysql.Insert(tmp_query);

                                queryList.Clear();
                                tmp_query = string.Empty;

                                logger.message = string.Format("{0} stock card records is inserted", RecordCount);
                                logger.Broadcast();
                            }

                            lDataSetDN.Next();
                        }

                        if (queryList.Count > 0)
                        {
                            string tmp_query = query;
                            tmp_query += string.Join(", ", queryList);
                            tmp_query += updateQuery;

                            mysql.Insert(tmp_query);

                            queryList.Clear();
                            tmp_query = string.Empty;

                            logger.message = string.Format("{0} stock card records is inserted", RecordCount);
                            logger.Broadcast();
                        }

                        mysql.Insert("INSERT INTO cms_update_time(table_name, updated_at) VALUES ('cms_stock_card', NOW()) ON DUPLICATE KEY UPDATE table_name = VALUES(table_name), updated_at = VALUES(updated_at)");

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
                                file_name = "SQLAccounting + JobStockCardSync",
                                exception = exc.Message,
                                time = DateTime.Now.ToString()
                            };
                            LocalDB.InsertException(exception);
                        }
                    }

                    if (uniqueKeyList.Count > 0)
                    {
                        logger.Broadcast("Total stock card transactions to be deactivated: " + uniqueKeyList.Count);

                        HashSet<string> deactivateId = new HashSet<string>();
                        for (int i = 0; i < uniqueKeyList.Count; i++)
                        {
                            string _id = uniqueKeyList.ElementAt(i).Key;
                            deactivateId.Add(_id);
                        }

                        string ToBeDeactivate = "'" + string.Join("','", deactivateId) + "'";
                        Console.WriteLine(ToBeDeactivate);

                        string inactive = "UPDATE cms_stock_card SET cancelled = 'T' WHERE id IN (" + ToBeDeactivate + ")";
                        mysql.Insert(inactive);

                        logger.Broadcast(uniqueKeyList.Count + " stock card transactions deactivated");

                        uniqueKeyList.Clear();
                    }


                    slog.action_identifier = Constants.Action_StockCardSync;
                    slog.action_details = Constants.Tbl_cms_stock_card + Constants.Is_Finished + string.Format(" ({0}) records", RecordCount);
                    slog.action_failure = 0;
                    slog.action_failure_message = string.Empty;
                    slog.action_time = DateTime.Now.ToString();

                    DateTime endTime = DateTime.Now;
                    TimeSpan ts = endTime - startTime;
                    Console.WriteLine("Completed in: " + ts.TotalSeconds.ToString("F"));

                    LocalDB.InsertSyncLog(slog);

                    logger.message = "Stock Card sync finished in (" + ts.TotalSeconds.ToString("F") + " seconds)";
                    logger.Broadcast();
                });

                thread.Start();
            }
            catch (ThreadAbortException e)
            {
                DpprException ex = new DpprException
                {
                    file_name = "JobStockCardSync",
                    exception = e.Message,
                    time = DateTime.Now.ToString()
                };
                LocalDB.InsertException(ex);

                Console.WriteLine(Constants.Thread_Exception + e.Message);
            }
        }
    }
}