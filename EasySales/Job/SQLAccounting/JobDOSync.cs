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
    public class JobDOSync : IJob
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
                    slog.action_identifier = Constants.Action_DOSync;                               /*check again */
                    slog.action_details = Constants.Tbl_cms_do + Constants.Is_Starting;             /*check again */
                    slog.action_failure = 0;
                    slog.action_failure_message = string.Empty;
                    slog.action_time = DateTime.Now.ToString();

                    DateTime startTime = DateTime.Now;

                    LocalDB.InsertSyncLog(slog);
                    logger.message = "----------------------------------------------------------------------------";
                    logger.Broadcast();
                    logger.message = "DO sync is running";
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

                    dynamic hasUdf = new CheckBackendRule().CheckTablesExist().GetSettingByTableName("cms_do");
                    string from_do_date = string.Empty;
                    string date_to_conv_status = string.Empty;
                    string refer_doc_ref = string.Empty;
                    string refer_doc_field = string.Empty;
                    string refer_doc_value = string.Empty;

                    if (hasUdf.Count > 0)
                    {
                        foreach (var include in hasUdf)
                        {
                            dynamic _include = include.include;

                            dynamic _from_do_date = _include.from_do_date;
                            if (_from_do_date != null)
                            {
                                if (_from_do_date != 0)
                                {
                                    from_do_date = _from_do_date;
                                }
                            }

                            dynamic _date_to_conv_status = _include.date_to_conv_status;
                            if (_date_to_conv_status != null)
                            {
                                if (_date_to_conv_status != string.Empty)
                                {
                                    date_to_conv_status = _date_to_conv_status; //"01/12/2020"
                                }
                            }
                            
                            dynamic _refer_doc_ref = _include.refer_doc_ref;
                            if (_refer_doc_ref != null)
                            {
                                if (_refer_doc_ref != "0")
                                {
                                    refer_doc_ref = _refer_doc_ref;
                                }
                            }
                            
                            dynamic _refer_doc_field = _include.refer_doc_field;
                            if (_refer_doc_field != null)
                            {
                                if (_refer_doc_field != string.Empty)
                                {
                                    refer_doc_field = _refer_doc_field;
                                }
                            }
                            
                            dynamic _refer_doc_value = _include.refer_doc_value;
                            if (_refer_doc_value != null)
                            {
                                if (_refer_doc_value != string.Empty)
                                {
                                    refer_doc_value = _refer_doc_value;
                                }
                            }
                        }
                    }

                    Dictionary<string, string> salespersonList = new Dictionary<string, string>();
                    ArrayList salespersonFromDb = mysql.Select("SELECT login_id, staff_code FROM cms_login");

                    for (int i = 0; i < salespersonFromDb.Count; i++)
                    {
                        Dictionary<string, string> each = (Dictionary<string, string>)salespersonFromDb[i];
                        salespersonList.Add(each["staff_code"], each["login_id"]);
                        /*using key (staff_code) to get value (login_id) */
                    }
                    salespersonFromDb.Clear();

                    dynamic lDataSet;
                    string docKey, doCode, doDate, doAmount, agent, custCode, remarks, cancelled, transferStatus, cancel_status, sendToPickPackApp;
                    string query, updateQuery;

                    HashSet<string> queryList = new HashSet<string>();

                    Dictionary<string, string> cms_updated_time = mysql.GetUpdatedTime("cms_do"); //{[updated_at, 28/06/2020 5:38:23 PM]}
                    string updated_at = string.Empty;

                    if (cms_updated_time.Count > 0)
                    {
                        updated_at = cms_updated_time["updated_at"].ToString().MSSQLdate();
                    }

                    string getActiveDO = "SELECT do_code FROM cms_do WHERE cancelled = 'F'";
                    if (cms_updated_time.Count > 0)
                    {
                        getActiveDO += " AND do_date >= '" + updated_at + "'";
                    }

                    ArrayList inDBactiveDO = mysql.Select(getActiveDO);

                    logger.Broadcast("Active DO in DB: " + inDBactiveDO.Count);
                    ArrayList inDBDO = new ArrayList();
                    for (int i = 0; i < inDBactiveDO.Count; i++)
                    {
                        Dictionary<string, string> each = (Dictionary<string, string>)inDBactiveDO[i];
                        inDBDO.Add(each["do_code"].ToString());
                    }
                    inDBactiveDO.Clear();

                    query = "INSERT INTO cms_do(do_code, do_date, cust_code, do_amount, salesperson, salesperson_id, remarks, transfer_status, cancelled, ref_no) VALUES ";
                    updateQuery = " ON DUPLICATE KEY UPDATE do_date = VALUES(do_date), cust_code = VALUES(cust_code), do_amount = VALUES(do_amount), salesperson = VALUES(salesperson), salesperson_id = VALUES(salesperson_id), remarks = VALUES(remarks), transfer_status = VALUES(transfer_status), cancelled = VALUES(cancelled), ref_no = VALUES(ref_no)";

                    //string lSQL = "SELECT DOCKEY, DOCNO, DOCDATE, POSTDATE, CODE, AGENT, CANCELLED, DOCAMT, LOCALDOCAMT, DOCREF1, DOCREF4 FROM SL_DO";
                    string lSQL = "SELECT * FROM SL_DO";
                    string checkConvertedDO = "SELECT DO.DOCNO AS DO_DOCNO, IV.DOCNO AS IV_DOCNO, IVDTL.ITEMCODE FROM SL_DO AS DO LEFT JOIN SL_IVDTL AS IVDTL ON IVDTL.FROMDOCKEY = DO.DOCKEY LEFT JOIN SL_IV AS IV ON IV.DOCKEY = IVDTL.DOCKEY WHERE IVDTL.FROMDOCTYPE = 'DO'";

                    if (cms_updated_time.Count > 0)
                    {
                        lSQL += " WHERE DOCDATE >= '" + updated_at + "'";
                        checkConvertedDO += " AND DO.DOCDATE >= '" + updated_at + "'";
                    }
                    lDataSet = ComServer.DBManager.NewDataSet(lSQL);

                    ArrayList convertedDOList = new ArrayList();
                    dynamic ConvertedDODataSet = ComServer.DBManager.NewDataSet(checkConvertedDO);
                    ConvertedDODataSet.First();

                    while (!ConvertedDODataSet.eof)
                    {
                        string DO_DOCNO = ConvertedDODataSet.FindField("DO_DOCNO").AsString;
                        string IV_DOCNO = ConvertedDODataSet.FindField("IV_DOCNO").AsString;
                        convertedDOList.Add(DO_DOCNO);
                        ConvertedDODataSet.Next();
                    }

                    Console.WriteLine(refer_doc_field);
                    Console.WriteLine(refer_doc_value);

                    try
                    {
                        lDataSet.First();

                        while (!lDataSet.eof)
                        {
                            RecordCount++;

                            docKey = lDataSet.FindField("DOCKEY").AsString;
                            doCode = lDataSet.FindField("DOCNO").AsString;
                            if (inDBDO.Contains(doCode))
                            {
                                int index = inDBDO.IndexOf(doCode);
                                if (index != -1)
                                {
                                    inDBDO.RemoveAt(index);
                                }
                            }

                            doDate = lDataSet.FindField("DOCDATE").AsString;
                            doDate = Convert.ToDateTime(doDate).ToString("yyyy-MM-dd");
                            doAmount = lDataSet.FindField("LOCALDOCAMT").AsString;
                            custCode = lDataSet.FindField("CODE").AsString;
                            remarks = lDataSet.FindField("DOCREF1").AsString;

                            agent = lDataSet.FindField("AGENT").AsString;
                            agent = agent.ToUpper();

                            if (string.IsNullOrEmpty(agent) || !salespersonList.TryGetValue(agent, out string agentId))
                            {
                                agentId = "0";
                            }

                            cancelled = lDataSet.FindField("CANCELLED").AsString;

                            if(refer_doc_ref == "1")
                            {
                                sendToPickPackApp = lDataSet.FindField(refer_doc_field).AsString;

                                if(sendToPickPackApp == refer_doc_value)
                                {
                                    if (convertedDOList.Contains(doCode))
                                    {
                                        transferStatus = "1";
                                    }
                                    else
                                    {
                                        transferStatus = "0";
                                    }
                                }
                                else
                                {
                                    transferStatus = "1";
                                }
                            }
                            else
                            {
                                if (convertedDOList.Contains(doCode))
                                {
                                    transferStatus = "1";
                                }
                                else
                                {
                                    DateTime dt1 = DateTime.Parse(doDate);
                                    DateTime dt2 = DateTime.Parse(date_to_conv_status); //"01/12/2020"

                                    if (dt1.Date >= dt2.Date)
                                    {
                                        //It's a equal date or later date --> should appear in pickpack app
                                        transferStatus = "0";
                                    }
                                    else
                                    {
                                        //It's an earlier //old DO set as converted
                                        transferStatus = "1";
                                    }
                                }
                            }

                            Database.Sanitize(ref doCode);
                            Database.Sanitize(ref doDate);
                            Database.Sanitize(ref doAmount);
                            Database.Sanitize(ref agent);
                            Database.Sanitize(ref custCode);
                            Database.Sanitize(ref remarks);
                            Database.Sanitize(ref cancelled);

                            string Values = string.Format("('{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}','{8}','{9}')", doCode, doDate, custCode, doAmount, agent, agentId, remarks, transferStatus, cancelled, docKey);

                            queryList.Add(Values);

                            if (queryList.Count % 2000 == 0)
                            {
                                string tmp_query = query;
                                tmp_query += string.Join(", ", queryList);
                                tmp_query += updateQuery;

                                mysql.Insert(tmp_query);
                                mysql.Message("DO Query: " + tmp_query);

                                queryList.Clear();
                                tmp_query = string.Empty;

                                logger.message = string.Format("{0} delivery order records is inserted", RecordCount);
                                logger.Broadcast();
                            }

                            lDataSet.Next();
                        }

                        if (queryList.Count > 0)
                        {
                            query = query + string.Join(", ", queryList) + updateQuery;
                            mysql.Insert(query);
                            mysql.Message("DO Query: " + query);

                            logger.message = string.Format("{0} delivery order records is inserted", RecordCount);
                            logger.Broadcast();
                        }

                        if (inDBDO.Count > 0)
                        {
                            logger.Broadcast("Total DO records to be deactivated: " + inDBDO.Count);

                            string inactive = "UPDATE cms_do SET cancelled = '{0}' WHERE do_code = '{1}'";

                            for (int i = 0; i < inDBDO.Count; i++)
                            {
                                string _docno = inDBDO[i].ToString();

                                Database.Sanitize(ref _docno);
                                string _query = string.Format(inactive, 'T', _docno);

                                mysql.Insert(_query);

                            }

                            logger.Broadcast(inDBDO.Count + " DO records deactivated");

                            inDBDO.Clear();
                        }

                        mysql.Insert("INSERT INTO cms_update_time(table_name, updated_at) VALUES ('cms_do', NOW()) ON DUPLICATE KEY UPDATE table_name = VALUES(table_name), updated_at = VALUES(updated_at)");

                    }
                    catch(Exception ex)
                    {
                        try
                        {
                            logger.Broadcast("Error while getting the DO: " + ex.Message);
                            goto CHECKAGAIN;
                        }
                        catch (Exception exc)
                        {
                            DpprException exception = new DpprException()
                            {
                                file_name = "SQLAccounting + JobDOSync",
                                exception = exc.Message,
                                time = DateTime.Now.ToString()
                            };
                            LocalDB.InsertException(exception);
                        }
                    }

                    RecordCount = 0;
                    dynamic lDataSetDtl, BizObject, lMain, lDetail;
                    string lSQLDtl;
                    string DTLKEY, DOCKEY, DOCNO, ITEMCODE, DESCRIPTION, UNITPRICE, QTY, UOM, DISC, AMOUNT, QRCODE, LOCATION, ITEMSTATUS, DONOTE, BATCH, BARCODE;
                    string queryDtl, updateQueryDtl;
                    HashSet<string> queryListDtl = new HashSet<string>();

                    string activeINVDtl = "SELECT cms_do.do_code, cms_do.do_date, dodtl.id, dodtl.do_code AS dodtl_do_code, dodtl.item_code FROM cms_do AS cms_do LEFT JOIN cms_do_details AS dodtl ON cms_do.do_code = dodtl.do_code WHERE active_status = 1 ";
                    if (cms_updated_time.Count > 0)
                    {
                        activeINVDtl += " AND cms_do.do_date >= '" + updated_at + "' ORDER BY cms_do.do_date DESC";
                    }

                    ArrayList inDBactiveinvdtl = mysql.Select(activeINVDtl);
                    logger.Broadcast("Active DO details in DB: " + inDBactiveinvdtl.Count);
                    Dictionary<string, string> uniqueKeyList = new Dictionary<string, string>();
                    for (int i = 0; i < inDBactiveinvdtl.Count; i++)
                    {
                        Dictionary<string, string> each = (Dictionary<string, string>)inDBactiveinvdtl[i];
                        string id = each["id"].ToString();
                        string dtlInvoiceCode = each["dodtl_do_code"].ToString();
                        string itemCode = each["item_code"].ToString();
                        string unique = dtlInvoiceCode + itemCode;
                        string uniqueLowercase = unique.ToLower();
                        uniqueKeyList.Add(id, uniqueLowercase);
                    }
                    inDBactiveinvdtl.Clear();

                    Dictionary<string, string> doNoteRemark = new Dictionary<string, string>();
                    string getUnconvertedDO = "SELECT do_code FROM cms_do WHERE transfer_status = 0 AND cancelled = 'F' ";
                    if (from_do_date != string.Empty)
                    {
                        getUnconvertedDO += "AND do_date >= '" + from_do_date + "'";
                    }

                    ArrayList transferListFromDB = mysql.Select(getUnconvertedDO);
                    ArrayList doCodeList = new ArrayList();
                    for (int i = 0; i < transferListFromDB.Count; i++)
                    {
                        Dictionary<string, string> each = (Dictionary<string, string>)transferListFromDB[i];
                        string do_code = each["do_code"].ToString();
                        doCodeList.Add(do_code);
                    }

                    queryDtl = "INSERT INTO cms_do_details(do_code, item_code, item_name, item_price, quantity, uom, discount, total_price, qr_code, ref_no, location) VALUES ";
                    updateQueryDtl = " ON DUPLICATE KEY UPDATE quantity = VALUES(quantity), total_price = VALUES(total_price), item_price = VALUES(item_price), discount = VALUES(discount), qr_code=VALUES(qr_code), ref_no=VALUES(ref_no), location=VALUES(location);";

                    lSQLDtl = "SELECT SL_DO.DOCNO, SL_DO.DOCKEY, SL_DODTL.DTLKEY, SL_DODTL.ITEMCODE, SL_DODTL.DESCRIPTION, SL_DODTL.UNITPRICE, SL_DODTL.QTY, SL_DODTL.UOM, SL_DODTL.LOCALAMOUNT, SL_DODTL.DISC,  SL_DODTL.LOCATION, SL_DODTL.BATCH, SL_DODTL.DTLKEY FROM SL_DODTL LEFT JOIN SL_DO ON SL_DO.DOCKEY = SL_DODTL.DOCKEY";

                    if (cms_updated_time.Count > 0)
                    {
                        lSQLDtl += " WHERE SL_DO.DOCDATE >= '" + updated_at + "' ORDER BY DOCDATE DESC";
                    }
                    else
                    {
                        lSQLDtl += " ORDER BY DOCDATE DESC";
                    }

                    string barCodeQuery = "SELECT * FROM ST_ITEM_BARCODE";
                    dynamic barcodeDataSet;
                    Dictionary<string, string> barCodeQueryList = new Dictionary<string, string>();
                    string productCode = string.Empty;
                    string productUom = string.Empty;
                    string barCode = string.Empty;

                    try
                    {
                        barcodeDataSet = ComServer.DBManager.NewDataSet(barCodeQuery);
                        barcodeDataSet.First();

                        while (!barcodeDataSet.eof)
                        {
                            productCode = barcodeDataSet.FindField("CODE").AsString;
                            productUom = barcodeDataSet.FindField("UOM").AsString;
                            barCode = barcodeDataSet.FindField("BARCODE").AsString;

                            productCode = productCode.ToUpper();
                            productUom = productUom.ToUpper();
                            string prodCodeUom = productCode + productUom;

                            barCodeQueryList.Add(barCode, prodCodeUom);
                            barcodeDataSet.Next();
                        }
                    }
                    catch (Exception ex)
                    {
                        try
                        {
                            //instance.Message(ex.Message);
                            logger.Broadcast("Error while getting the DO DTL BarCode: " + ex.Message);
                            DpprException exception = new DpprException()
                            {
                                file_name = "SQLAccounting + JobDOSync + BarCode Dataset",
                                exception = ex.Message,
                                time = DateTime.Now.ToString()
                            };
                            LocalDB.InsertException(exception);
                            //goto CHECKAGAIN;
                        }
                        catch (Exception exc)
                        {
                            DpprException exception = new DpprException()
                            {
                                file_name = "SQLAccounting + JobDOSync + BarCode Dataset",
                                exception = exc.Message,
                                time = DateTime.Now.ToString()
                            };
                            LocalDB.InsertException(exception);
                        }
                    }

                    Console.WriteLine("barCodeQueryList.Count" + barCodeQueryList.Count);


                    try
                    {
                        lDataSetDtl = ComServer.DBManager.NewDataSet(lSQLDtl);
                        lDataSetDtl.First();

                        DONOTE = string.Empty;
                        string currentDocNo = string.Empty;
                        while (!lDataSetDtl.eof)
                        {
                            RecordCount++;
                            DTLKEY = lDataSetDtl.FindField("DTLKEY").AsString;
                            //logger.Broadcast("passing dtlkey");
                            DOCKEY = lDataSetDtl.FindField("DOCKEY").AsString;
                            //logger.Broadcast("passing dockey");
                            DOCNO = lDataSetDtl.FindField("DOCNO").AsString;
                            //logger.Broadcast("passing docno");
                            ITEMCODE = lDataSetDtl.FindField("ITEMCODE").AsString;
                            //logger.Broadcast("passing itemcode : " + ITEMCODE);
                            DESCRIPTION = lDataSetDtl.FindField("DESCRIPTION").AsString;
                            //logger.Broadcast("passing desc");
                            UNITPRICE = lDataSetDtl.FindField("UNITPRICE").AsString;
                            //logger.Broadcast("passing unitprice");
                            QTY = lDataSetDtl.FindField("QTY").AsString;
                            //logger.Broadcast("passing qty");

                            if (doCodeList.Contains(DOCNO))
                            {
                                //set the P (Pending) status in the DO item basis and insert (all item code + qty) into DO note
                                if (currentDocNo == string.Empty)
                                {
                                    currentDocNo = DOCNO;
                                }

                                if (currentDocNo == DOCNO)
                                {
                                    DONOTE += DONOTE != string.Empty ? ";\n" + ITEMCODE + "[" + QTY + "]" : ITEMCODE + "[" + QTY + "]";
                                }
                                else
                                {
                                    currentDocNo = DOCNO;
                                    DONOTE = string.Empty;
                                    DONOTE += ITEMCODE + "[" + QTY + "]";
                                    //logger.Broadcast("passing donote: " + DONOTE);
                                }


                                ITEMSTATUS = "PENDING FOR PICKING";

                                //insert 'P' for each item
                                BizObject = ComServer.BizObjects.Find("SL_DO");
                                lMain = BizObject.DataSets.Find("MainDataSet");
                                lDetail = BizObject.DataSets.Find("cdsDocDetail");

                                if (Convert.IsDBNull(DOCKEY) != null)
                                {//Edit Data if found
                                    BizObject.Params.Find("Dockey").Value = DOCKEY;
                                    BizObject.Open();
                                    BizObject.Edit();
                                    lMain.FindField("DocNo").value = DOCNO;

                                    if (lDetail.Locate("DtlKey", DTLKEY, false, false))
                                    {
                                        lDetail.Edit();
                                        lDetail.FindField("Remark2").AsString = ITEMSTATUS;
                                        lDetail.Post();
                                    }
                                    BizObject.Save();
                                    BizObject.Close();
                                }
                            }
                            else
                            {
                                DONOTE = string.Empty;
                            }

                            if (doNoteRemark.ContainsKey(DOCKEY))
                            {
                                doNoteRemark.Remove(DOCKEY);
                                if (DONOTE != string.Empty)
                                {
                                    doNoteRemark.Add(DOCKEY, DONOTE);
                                }
                            }
                            else
                            {
                                if (DONOTE != string.Empty)
                                {
                                    doNoteRemark.Add(DOCKEY, DONOTE);
                                }
                            }

                            LOCATION = lDataSetDtl.FindField("LOCATION").AsString;
                            //logger.Broadcast("passing location");
                            UOM = lDataSetDtl.FindField("UOM").AsString;
                            ///logger.Broadcast("passing uom");
                            DISC = lDataSetDtl.FindField("DISC").AsString;
                            //logger.Broadcast("passing disc");
                            AMOUNT = lDataSetDtl.FindField("LOCALAMOUNT").AsString;
                            //logger.Broadcast("passing localamount");
                            BATCH = lDataSetDtl.FindField("BATCH").AsString;
                            //logger.Broadcast("passing batch");

                            string _itemCode = string.Empty;
                            if (ITEMCODE != string.Empty && ITEMCODE != null)
                            {
                                _itemCode = ITEMCODE.ToUpper();
                            }
                            //logger.Broadcast("_itemCode: " + _itemCode);
                            string _uom = string.Empty;
                            if (UOM != string.Empty && UOM != null)
                            {
                                _uom = UOM.ToUpper();
                            }
                            //logger.Broadcast("_uom: " + _uom);
                            string findBarcode = _itemCode + _uom;
                            //logger.Broadcast("findBarcode: " + findBarcode);
                            BARCODE = string.Empty;
                            if(barCodeQueryList.Count > 0)
                            {
                                if (barCodeQueryList.ContainsValue(findBarcode))
                                {
                                    var key = barCodeQueryList.Where(pair => pair.Value == findBarcode)
                                                .Select(pair => pair.Key)
                                                .FirstOrDefault();

                                    BARCODE = key;
                                    //logger.Broadcast("getting barcode: " + BARCODE);
                                }
                            }

                            QRCODE = "|" + BARCODE + "|" + BATCH + "|";
                            //logger.Broadcast("passing qrcode: " + QRCODE);
                            //Console.WriteLine(ITEMCODE +": "+ QRCODE);

                            string unique = DOCNO + ITEMCODE;
                            //logger.Broadcast("passing unique: " + unique);
                            string uniqueLowercase = unique.ToLower();
                            //logger.Broadcast("passing uniqueLowercase: " + uniqueLowercase);

                            if (uniqueKeyList.Count > 0)
                            {
                                if (uniqueKeyList.ContainsValue(uniqueLowercase))
                                {
                                    var key = uniqueKeyList.Where(pair => pair.Value == uniqueLowercase)
                                                .Select(pair => pair.Key)
                                                .FirstOrDefault();
                                    //logger.Broadcast("passing key: " + key);
                                    if (key != null)
                                    {
                                        uniqueKeyList.Remove(key);
                                    }
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
                            Database.Sanitize(ref QRCODE);
                            Database.Sanitize(ref LOCATION);

                            if(ITEMCODE != string.Empty && ITEMCODE != null)
                            {
                                string Values = string.Format("('{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}','{8}','{9}','{10}')", DOCNO, ITEMCODE, DESCRIPTION, UNITPRICE, QTY, UOM, DISC, AMOUNT, QRCODE, DTLKEY, LOCATION);
                                //logger.Broadcast("passing values : " + Values);

                                queryListDtl.Add(Values);
                            }

                            if (queryListDtl.Count % 2000 == 0)
                            {
                                string tmp_query = queryDtl;
                                tmp_query += string.Join(", ", queryListDtl);
                                tmp_query += updateQueryDtl;

                                //logger.Broadcast("inserting to db %");
                                mysql.Insert(tmp_query);
                                mysql.Message("DO DTL Query: " + tmp_query);

                                queryListDtl.Clear();
                                tmp_query = string.Empty;

                                logger.message = string.Format("{0} delivery order details records is inserted", RecordCount);
                                logger.Broadcast();
                            }

                            lDataSetDtl.Next();
                        }

                        if (queryListDtl.Count > 0)
                        {
                            string tmp_query = queryDtl;
                            tmp_query += string.Join(", ", queryListDtl);
                            tmp_query += updateQueryDtl;

                            //logger.Broadcast("inserting to db > ");
                            mysql.Insert(tmp_query);
                            mysql.Message("DO DTL Query: " + tmp_query);

                            queryListDtl.Clear();
                            tmp_query = string.Empty;

                            logger.message = string.Format("{0} delivery order details records is inserted", RecordCount);
                            logger.Broadcast();
                        }

                        //logger.Broadcast("getting to doNoteRemark");
                        if (doNoteRemark.Count > 0)
                        {
                            for (int i = 0; i < doNoteRemark.Count; i++)
                            {
                                string doKey = doNoteRemark.ElementAt(i).Key;
                                string doNote = doNoteRemark.ElementAt(i).Value;

                                BizObject = ComServer.BizObjects.Find("SL_DO");
                                lMain = BizObject.DataSets.Find("MainDataSet");

                                BizObject.Params.Find("Dockey").Value = doKey;
                                BizObject.Open();
                                BizObject.Edit();
                                lMain.FindField("Dockey").value = doKey;

                                lMain.Edit();
                                lMain.FindField("Note").AsString = doNote;
                                lMain.Post();

                                BizObject.Save();
                                BizObject.Close();
                            }
                        }

                        if (uniqueKeyList.Count > 0)
                        {
                            logger.Broadcast("Total DO details records to be deactivated: " + uniqueKeyList.Count);

                            HashSet<string> deactivateId = new HashSet<string>();
                            for (int i = 0; i < uniqueKeyList.Count; i++)
                            {
                                string _id = uniqueKeyList.ElementAt(i).Key;
                                deactivateId.Add(_id);
                            }

                            string ToBeDeactivate = "'" + string.Join("','", deactivateId) + "'";
                            Console.WriteLine(ToBeDeactivate);

                            string inactive = "UPDATE cms_do_details SET active_status = 0 WHERE id IN (" + ToBeDeactivate + ")";
                            mysql.Insert(inactive);

                            logger.Broadcast(uniqueKeyList.Count + " DO details records deactivated");

                            uniqueKeyList.Clear();
                        }

                        mysql.Insert("INSERT INTO cms_update_time(table_name, updated_at) VALUES ('cms_do_details', NOW()) ON DUPLICATE KEY UPDATE table_name = VALUES(table_name), updated_at = VALUES(updated_at)");

                    }
                    catch (Exception ex)
                    {
                        try
                        {
                            logger.Broadcast("Error while getting the DO DTL: " + ex.Message);
                            //goto CHECKAGAIN;
                        }
                        catch (Exception exc)
                        {
                            DpprException exception = new DpprException()
                            {
                                file_name = "SQLAccounting + JobDODtlSync",
                                exception = exc.Message,
                                time = DateTime.Now.ToString()
                            };
                            LocalDB.InsertException(exception);
                        }
                    }

                    slog.action_identifier = Constants.Action_DODtlSync;
                    slog.action_details = Constants.Tbl_cms_do_details + Constants.Is_Finished + string.Format(" ({0}) records", RecordCount);
                    slog.action_failure = 0;
                    slog.action_failure_message = string.Empty;
                    slog.action_time = DateTime.Now.ToString();

                    DateTime endTime = DateTime.Now;
                    TimeSpan ts = endTime - startTime;
                    Console.WriteLine("Completed in: " + ts.TotalSeconds.ToString("F"));

                    LocalDB.InsertSyncLog(slog);

                    logger.message = "DO Details sync finished in (" + ts.TotalSeconds.ToString("F") + " seconds)";
                    logger.Broadcast();
                });

                thread.Start();
            }
            catch (ThreadAbortException e)
            {
                DpprException ex = new DpprException
                {
                    file_name = "JobDODtlSync",
                    exception = e.Message,
                    time = DateTime.Now.ToString()
                };
                LocalDB.InsertException(ex);

                Console.WriteLine(Constants.Thread_Exception + e.Message);
            }
        }
    }
}