using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EasySales.Model;
using EasySales.Object;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Quartz;
using Quartz.Impl;
using Quartz.Logging;
using Renci.SshNet.Messages.Connection;
using Ubiety.Dns.Core.Records;

namespace EasySales.Job
{
    [DisallowConcurrentExecution]
    public class JobWhStockSync : IJob
    {
        private ArrayList onlyItem = new ArrayList();
        public void ExecuteOnlyItem(ArrayList onlyItem)
        {
            this.onlyItem = onlyItem;
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

                    /**
                     * Here we will run SQLAccounting Codes
                     * */

                    DpprSyncLog slog = new DpprSyncLog();
                    slog.action_identifier = Constants.Action_WarehouseSync;                               /*check again */
                    slog.action_details = Constants.Tbl_cms_warehouse_stock + Constants.Is_Starting;    /*check again */
                    slog.action_failure = 0;
                    slog.action_failure_message = string.Empty;
                    slog.action_time = DateTime.Now.ToString();

                    DateTime startTime = DateTime.Now;

                    LocalDB.InsertSyncLog(slog);
                    logger.message = "----------------------------------------------------------------------------";
                    logger.Broadcast();
                    logger.message = "Warehouse stock sync is running";
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

                    string query, updateQuery, lSQL;
                    dynamic lDataSet;
                    string whCode, whName, whAddress, whRemark, whStatus;
                    HashSet<string> queryList = new HashSet<string>();
                    Database mysql = new Database();
                    dynamic jsonRule = new CheckBackendRule().CheckTablesExist().GetSettingByTableName("cms_warehouse");
                    ArrayList exCode = new ArrayList();
                    ArrayList inCode = new ArrayList();

                    if (jsonRule != null)
                    {
                        foreach (var rule in jsonRule)
                        {
                            dynamic _excludeCode = rule.exclude_code;

                            foreach (string value in _excludeCode)
                            {
                                exCode.Add(value);
                            }

                            dynamic _includeCode = rule.include_code;

                            foreach (string value in _includeCode)
                            {
                                inCode.Add(value);
                            }
                        }
                    }

                    query = "INSERT INTO cms_warehouse(wh_code, wh_name, wh_address, wh_remark, wh_status) VALUES ";

                    updateQuery = " ON DUPLICATE KEY UPDATE wh_name = VALUES(wh_name), wh_address = VALUES(wh_address), wh_remark = VALUES(wh_remark), wh_status = VALUES(wh_status)";

                    lSQL = "SELECT * FROM ST_LOCATION";

                    if(this.onlyItem.Count > 0)
                    {
                        goto syncWhStock;
                    }

                    try
                    {

                        lDataSet = ComServer.DBManager.NewDataSet(lSQL);

                        while (!lDataSet.eof)
                        {
                            RecordCount++;
                            int activeValue = 1;

                            whCode = lDataSet.FindField("CODE").AsString;
                            
                            //if (whCode == "----")
                            //{
                            //    whCode = "HQ";
                            //}

                            whName = lDataSet.FindField("DESCRIPTION").AsString;
                            whAddress = lDataSet.FindField("ADDRESS1").AsString;
                            whRemark = "";

                            whStatus = lDataSet.FindField("ISACTIVE").AsString;

                            if (whStatus == "F")
                            {
                                activeValue = 0;
                            }

                            if (inCode.Count > 0)
                            {
                                if (!inCode.Contains(whCode))
                                {
                                    activeValue = 0;
                                }
                            }

                            Database.Sanitize(ref whCode);
                            Database.Sanitize(ref whName);
                            Database.Sanitize(ref whAddress);
                            Database.Sanitize(ref whRemark);

                            string Values = string.Format("('{0}','{1}','{2}','{3}','{4}')", whCode, whName, whAddress, whRemark, activeValue);

                            queryList.Add(Values);

                            if (queryList.Count % 2000 == 0)
                            {
                                string tmp_query = query;
                                tmp_query += string.Join(", ", queryList);
                                tmp_query += updateQuery;

                                mysql.Insert(tmp_query);

                                queryList.Clear();
                                tmp_query = string.Empty;

                                logger.message = string.Format("{0} warehouse records is inserted", RecordCount);
                                logger.Broadcast();
                            }
                            lDataSet.Next();
                        }

                        if (queryList.Count > 0)
                        {
                            query = query + string.Join(", ", queryList) + updateQuery;
                            mysql.Insert(query);

                            logger.message = string.Format("{0} warehouse records is inserted", RecordCount);
                            logger.Broadcast();
                        }
                    }
                    catch (Exception ex)
                    {
                        try
                        {
                            logger.Broadcast("CATCH: " + ex.Message);
                            //goto CHECKAGAIN;
                        }
                        catch (Exception exc)
                        {
                            DpprException exception = new DpprException()
                            {
                                file_name = "SQLAccounting + JobWhStockSync",
                                exception = exc.Message,
                                time = DateTime.Now.ToString()
                            };
                            LocalDB.InsertException(exception);
                        }
                    }

                    RecordCount = 0;

                syncWhStock:

                    Database _mysql = new Database();

                    ArrayList itemList = new ArrayList();
                    ArrayList itemFromDb = _mysql.Select("SELECT product_code FROM cms_product WHERE product_status = 1");

                    for (int i = 0; i < itemFromDb.Count; i++)
                    {
                        Dictionary<string, string> each = (Dictionary<string, string>)itemFromDb[i];
                        itemList.Add(each["product_code"]);
                    }
                    itemFromDb.Clear();

                    if (this.onlyItem.Count > 0)
                    {
                        itemList = this.onlyItem;
                    }

                    ArrayList whList = new ArrayList();
                    ArrayList whFromDb = _mysql.Select("SELECT wh_code FROM cms_warehouse");

                    for (int i = 0; i < whFromDb.Count; i++)
                    {
                        Dictionary<string, string> each = (Dictionary<string, string>)whFromDb[i];
                        whList.Add(each["wh_code"]);
                    }
                    whFromDb.Clear();

                    string queryStock, updateQueryStock, lSQLStock;
                    string XFStock, DOStock, IVStock, CNStock, DNStock;
                    dynamic lDataSet1, lDataSetXFStock, lDataSetDOStock, lDataSetIVStock, lDataSetCNStock, lDataSetDNStock;

                    HashSet<string> queryList1 = new HashSet<string>();
                    HashSet<string> queryList2 = new HashSet<string>();

                    queryStock = "INSERT INTO cms_warehouse_stock(wh_code, product_code, ready_st_qty, available_st_qty, active_status, uom_name) VALUES ";

                    updateQueryStock = " ON DUPLICATE KEY UPDATE ready_st_qty = VALUES(ready_st_qty), available_st_qty = VALUES(available_st_qty)";

                    try
                    {
                        //SELECT ST_TR.ITEMCODE, ST_TR.LOCATION, SUM(ST_TR.QTY) AS SUMQTY, ST_XFDTL.UOM FROM ST_TR LEFT JOIN ST_XFDTL ON(ST_TR.DTLKEY = ST_XFDTL.DTLKEY) WHERE ST_TR.DOCTYPE = 'XF' GROUP BY ST_TR.LOCATION, ST_TR.ITEMCODE, ST_XFDTL.UOM ORDER BY ST_TR.ITEMCODE

                        string itemCode = string.Empty;
                        string uom = string.Empty;
                        string location = string.Empty;
                        string qty = string.Empty;
                        string Values = string.Empty;

                        Dictionary<string, string> qtyList = new Dictionary<string, string>();
                        Dictionary<string, string> baseUomList = new Dictionary<string, string>();
                        ArrayList baseUom = mysql.Select("SELECT product_code, product_uom FROM cms_product_uom_price_v2 WHERE product_default_price = 1 AND active_status = 1");

                        Dictionary<string, string> uniqueKeyList = new Dictionary<string, string>();
                        for (int i = 0; i < baseUom.Count; i++)
                        {
                            Dictionary<string, string> each = (Dictionary<string, string>)baseUom[i];
                            string product_code = each["product_code"].ToString();
                            string product_uom = each["product_uom"].ToString();
                            uniqueKeyList.Add(product_code, product_uom);
                        }
                        baseUom.Clear();

                        for (int i = 0; i < itemList.Count; i++)
                        {
                            string uomName = string.Empty;
                            string productCode = itemList[i].ToString();

                            string _prodCode = productCode.Replace("'", "''");

                            lSQLStock = "select location, sum(qty) from ST_TR where ItemCode = '" + _prodCode + "' group by location;";
                            lDataSet1 = ComServer.DBManager.NewDataSet(lSQLStock);
                            lDataSet1.First();

                            while (!lDataSet1.eof)
                            {
                                RecordCount++;
                                int activeValue = 1;

                                itemCode = productCode;
                                location = lDataSet1.FindField("LOCATION").AsString;
                                if (location == null || location == string.Empty)
                                {
                                    location = "";
                                }

                                qty = lDataSet1.FindField("SUM").AsString;
                                if (qty == null || qty == string.Empty)
                                {
                                    qty = "0";
                                }

                                if (uniqueKeyList.ContainsKey(itemCode))
                                {
                                    var value = uniqueKeyList.Where(pair => pair.Key == itemCode)
                                                .Select(pair => pair.Value)
                                                .FirstOrDefault();
                                    uomName = value;
                                    Console.WriteLine(value);
                                }
                                Database.Sanitize(ref itemCode);
                                Database.Sanitize(ref location);
                                Database.Sanitize(ref qty);
                                Database.Sanitize(ref uomName);

                                if (itemCode != string.Empty && itemCode != null)
                                {
                                    Values = string.Format("('{0}','{1}','{2}','{3}','{4}','{5}')", location, itemCode, qty, qty, activeValue, uomName);
                                    queryList1.Add(Values);
                                }

                                if (queryList1.Count % 2000 == 0)
                                {
                                    string tmp_query = queryStock;
                                    tmp_query += string.Join(", ", queryList1);
                                    tmp_query += updateQueryStock;

                                    mysql.Insert(tmp_query);
                                    mysql.Message("wh stock: " + tmp_query);

                                    queryList1.Clear();
                                    tmp_query = string.Empty;

                                    logger.message = string.Format("{0} warehouse stock records is inserted", RecordCount);
                                    logger.Broadcast();
                                }
                                lDataSet1.Next();
                            }
                        }

                        if (queryList1.Count > 0)
                        {
                            string tmp_query = queryStock;
                            tmp_query += string.Join(", ", queryList1);
                            tmp_query += updateQueryStock;

                            mysql.Insert(tmp_query);
                            mysql.Message(tmp_query);

                            queryList1.Clear();
                            tmp_query = string.Empty;

                            logger.message = string.Format("{0} warehouse stock records is inserted", RecordCount);
                            logger.Broadcast();
                        }
                    }
                    catch (Exception exx)
                    {
                        try
                        {
                            logger.Broadcast("CATCH: " + exx.Message);
                        }
                        catch (Exception exc)
                        {
                            DpprException exception = new DpprException()
                            {
                                file_name = "SQLAccounting + JobWhStockSync",
                                exception = exc.Message,
                                time = DateTime.Now.ToString()
                            };
                            LocalDB.InsertException(exception);
                        }
                    }

                    slog.action_identifier = Constants.Action_WarehouseSync;
                    slog.action_details = Constants.Tbl_cms_warehouse + Constants.Is_Finished + string.Format(" ({0}) records", RecordCount);
                    slog.action_failure = 0;
                    slog.action_failure_message = string.Empty;
                    slog.action_time = DateTime.Now.ToString();

                    DateTime endTime = DateTime.Now;
                    TimeSpan ts = endTime - startTime;
                    Console.WriteLine("Completed in: " + ts.TotalSeconds.ToString("F"));

                    LocalDB.InsertSyncLog(slog);

                    logger.message = "Warehouse sync finished in (" + ts.TotalSeconds.ToString("F") + " seconds)";
                    logger.Broadcast();
                });

                thread.Start();
            }
            catch (ThreadAbortException e)
            {
                DpprException ex = new DpprException
                {
                    file_name = "JobWarehouseSync",
                    exception = e.Message,
                    time = DateTime.Now.ToString()
                };
                LocalDB.InsertException(ex);

                Console.WriteLine(Constants.Thread_Exception + e.Message);
            }
        }
    }
}

//XFStock = "SELECT ST_TR.ITEMCODE, ST_TR.LOCATION, SUM(ST_TR.QTY) AS SUMQTY, ST_XFDTL.UOM FROM ST_TR LEFT JOIN ST_XFDTL ON(ST_TR.DTLKEY = ST_XFDTL.DTLKEY) WHERE ST_TR.DOCTYPE = 'XF' GROUP BY ST_TR.LOCATION, ST_TR.ITEMCODE, ST_XFDTL.UOM ORDER BY ST_TR.ITEMCODE";
//IVStock = "SELECT ST_TR.ITEMCODE, ST_TR.LOCATION, SUM(ST_TR.QTY) AS SUMQTY, SL_IVDTL.UOM FROM ST_TR LEFT JOIN SL_IVDTL ON(ST_TR.DTLKEY = SL_IVDTL.DTLKEY) WHERE ST_TR.DOCTYPE = 'IV' GROUP BY ST_TR.LOCATION, ST_TR.ITEMCODE, SL_IVDTL.UOM ORDER BY ST_TR.ITEMCODE";
//DOStock = "SELECT ST_TR.ITEMCODE, ST_TR.LOCATION, SUM(ST_TR.QTY) AS SUMQTY, SL_DODTL.UOM FROM ST_TR LEFT JOIN SL_DODTL ON(ST_TR.DTLKEY = SL_DODTL.DTLKEY) WHERE ST_TR.DOCTYPE = 'DO' GROUP BY ST_TR.LOCATION, ST_TR.ITEMCODE, SL_DODTL.UOM ORDER BY ST_TR.ITEMCODE";
//CNStock = "SELECT ST_TR.ITEMCODE, ST_TR.LOCATION, SUM(ST_TR.QTY) AS SUMQTY, SL_CNDTL.UOM FROM ST_TR LEFT JOIN SL_CNDTL ON(ST_TR.DTLKEY = SL_CNDTL.DTLKEY) WHERE ST_TR.DOCTYPE = 'CN' GROUP BY ST_TR.LOCATION, ST_TR.ITEMCODE, SL_CNDTL.UOM ORDER BY ST_TR.ITEMCODE";
//DNStock = "SELECT ST_TR.ITEMCODE, ST_TR.LOCATION, SUM(ST_TR.QTY) AS SUMQTY, SL_DNDTL.UOM FROM ST_TR LEFT JOIN SL_DNDTL ON(ST_TR.DTLKEY = SL_DNDTL.DTLKEY) WHERE ST_TR.DOCTYPE = 'DN' GROUP BY ST_TR.LOCATION, ST_TR.ITEMCODE, SL_DNDTL.UOM ORDER BY ST_TR.ITEMCODE";

////'PI','RC','SC','AJ','AS','IS','DS','GR','SD'
////PIStock = "SELECT ST_TR.ITEMCODE, ST_TR.LOCATION, SUM(ST_TR.QTY) AS SUMQTY, SL_DNDTL.UOM FROM ST_TR LEFT JOIN SL_DNDTL ON(ST_TR.DTLKEY = SL_DNDTL.DTLKEY) WHERE ST_TR.DOCTYPE = 'DN' GROUP BY ST_TR.LOCATION, ST_TR.ITEMCODE, SL_DNDTL.UOM ORDER BY ST_TR.ITEMCODE";
////RCStock = "SELECT ST_TR.ITEMCODE, ST_TR.LOCATION, SUM(ST_TR.QTY) AS SUMQTY, SL_DNDTL.UOM FROM ST_TR LEFT JOIN SL_DNDTL ON(ST_TR.DTLKEY = SL_DNDTL.DTLKEY) WHERE ST_TR.DOCTYPE = 'DN' GROUP BY ST_TR.LOCATION, ST_TR.ITEMCODE, SL_DNDTL.UOM ORDER BY ST_TR.ITEMCODE";
////SCStock = "SELECT ST_TR.ITEMCODE, ST_TR.LOCATION, SUM(ST_TR.QTY) AS SUMQTY, SL_DNDTL.UOM FROM ST_TR LEFT JOIN SL_DNDTL ON(ST_TR.DTLKEY = SL_DNDTL.DTLKEY) WHERE ST_TR.DOCTYPE = 'DN' GROUP BY ST_TR.LOCATION, ST_TR.ITEMCODE, SL_DNDTL.UOM ORDER BY ST_TR.ITEMCODE";
////AJStock = "SELECT ST_TR.ITEMCODE, ST_TR.LOCATION, SUM(ST_TR.QTY) AS SUMQTY, SL_DNDTL.UOM FROM ST_TR LEFT JOIN SL_DNDTL ON(ST_TR.DTLKEY = SL_DNDTL.DTLKEY) WHERE ST_TR.DOCTYPE = 'DN' GROUP BY ST_TR.LOCATION, ST_TR.ITEMCODE, SL_DNDTL.UOM ORDER BY ST_TR.ITEMCODE";
////ASStock = "SELECT ST_TR.ITEMCODE, ST_TR.LOCATION, SUM(ST_TR.QTY) AS SUMQTY, SL_DNDTL.UOM FROM ST_TR LEFT JOIN SL_DNDTL ON(ST_TR.DTLKEY = SL_DNDTL.DTLKEY) WHERE ST_TR.DOCTYPE = 'DN' GROUP BY ST_TR.LOCATION, ST_TR.ITEMCODE, SL_DNDTL.UOM ORDER BY ST_TR.ITEMCODE";
////DSStock = "SELECT ST_TR.ITEMCODE, ST_TR.LOCATION, SUM(ST_TR.QTY) AS SUMQTY, SL_DNDTL.UOM FROM ST_TR LEFT JOIN SL_DNDTL ON(ST_TR.DTLKEY = SL_DNDTL.DTLKEY) WHERE ST_TR.DOCTYPE = 'DN' GROUP BY ST_TR.LOCATION, ST_TR.ITEMCODE, SL_DNDTL.UOM ORDER BY ST_TR.ITEMCODE";
////GRStock = "SELECT ST_TR.ITEMCODE, ST_TR.LOCATION, SUM(ST_TR.QTY) AS SUMQTY, SL_DNDTL.UOM FROM ST_TR LEFT JOIN SL_DNDTL ON(ST_TR.DTLKEY = SL_DNDTL.DTLKEY) WHERE ST_TR.DOCTYPE = 'DN' GROUP BY ST_TR.LOCATION, ST_TR.ITEMCODE, SL_DNDTL.UOM ORDER BY ST_TR.ITEMCODE";
////SDStock = "SELECT ST_TR.ITEMCODE, ST_TR.LOCATION, SUM(ST_TR.QTY) AS SUMQTY, SL_DNDTL.UOM FROM ST_TR LEFT JOIN SL_DNDTL ON(ST_TR.DTLKEY = SL_DNDTL.DTLKEY) WHERE ST_TR.DOCTYPE = 'DN' GROUP BY ST_TR.LOCATION, ST_TR.ITEMCODE, SL_DNDTL.UOM ORDER BY ST_TR.ITEMCODE";

//lDataSetXFStock = ComServer.DBManager.NewDataSet(XFStock);
//lDataSetXFStock.First();
//while ((!lDataSetXFStock.Eof))
//{
//    RecordCount++;
//    int activeValue = 1;

//    itemCode = lDataSetXFStock.FindField("ITEMCODE").AsString;
//    uom = lDataSetXFStock.FindField("UOM").AsString;
//    qty = lDataSetXFStock.FindField("SUMQTY").AsString;
//    location = lDataSetXFStock.FindField("LOCATION").AsString;
//    //if (location == "----")
//    //{
//    //    location = "HQ";
//    //}

//    string unique = itemCode + "|" + uom + "|" + location;
//    ////string unique = unique.ToLower();
//    Console.WriteLine(unique + "[" + qty + "]");

//    if (qtyList.ContainsKey(unique))
//    {
//        var key = qtyList.Where(pair => pair.Key == unique)
//                    .Select(pair => pair.Key)
//                    .FirstOrDefault();
//        if (key != null)
//        {
//            var value = qtyList.Where(pair => pair.Key == unique)
//                    .Select(pair => pair.Value)
//                    .FirstOrDefault();
//            int.TryParse(qty, out int intQTY);
//            int.TryParse(value, out int intVALUE);
//            int newQTY = intQTY + intVALUE;
//            string _newQTY = newQTY.ToString();

//            qtyList.Remove(key);
//            qtyList.Add(unique, _newQTY);
//        }
//    }
//    else
//    {
//        qtyList.Add(unique, qty);
//    }

//    lDataSetXFStock.Next();
//}


//lDataSetIVStock = ComServer.DBManager.NewDataSet(IVStock);

//lDataSetIVStock.First();
//while ((!lDataSetIVStock.Eof))
//{
//    RecordCount++;
//    int activeValue = 1;

//    itemCode = lDataSetIVStock.FindField("ITEMCODE").AsString;
//    uom = lDataSetIVStock.FindField("UOM").AsString;
//    qty = lDataSetIVStock.FindField("SUMQTY").AsString;
//    location = lDataSetIVStock.FindField("LOCATION").AsString;
//    //if (location == "----")
//    //{
//    //    location = "HQ";
//    //}

//    string unique = itemCode + "|" + uom + "|" + location;
//    ////string unique = unique.ToLower();
//    Console.WriteLine(unique + "[" + qty + "]");
//    if (qtyList.ContainsKey(unique))
//    {
//        var key = qtyList.Where(pair => pair.Key == unique)
//                    .Select(pair => pair.Key)
//                    .FirstOrDefault();
//        if (key != null)
//        {
//            var value = qtyList.Where(pair => pair.Key == unique)
//                    .Select(pair => pair.Value)
//                    .FirstOrDefault();
//            int.TryParse(qty, out int intQTY);
//            int.TryParse(value, out int intVALUE);
//            int newQTY = intQTY + intVALUE;
//            string _newQTY = newQTY.ToString();

//            qtyList.Remove(key);
//            qtyList.Add(unique, _newQTY);
//        }
//    }
//    else
//    {
//        qtyList.Add(unique, qty);
//    }

//    lDataSetIVStock.Next();
//}

//lDataSetDOStock = ComServer.DBManager.NewDataSet(DOStock);

//lDataSetDOStock.First();
//while ((!lDataSetDOStock.Eof))
//{
//    RecordCount++;
//    int activeValue = 1;

//    itemCode = lDataSetDOStock.FindField("ITEMCODE").AsString;
//    uom = lDataSetDOStock.FindField("UOM").AsString;
//    qty = lDataSetDOStock.FindField("SUMQTY").AsString;
//    location = lDataSetDOStock.FindField("LOCATION").AsString;
//    //if (location == "----")
//    //{
//    //    location = "HQ";
//    //}

//    string unique = itemCode + "|" + uom + "|" + location;
//    ////string unique = unique.ToLower();
//    Console.WriteLine(unique + "[" + qty + "]");
//    if (qtyList.ContainsKey(unique))
//    {
//        var key = qtyList.Where(pair => pair.Key == unique)
//                    .Select(pair => pair.Key)
//                    .FirstOrDefault();
//        if (key != null)
//        {
//            var value = qtyList.Where(pair => pair.Key == unique)
//                    .Select(pair => pair.Value)
//                    .FirstOrDefault();
//            int.TryParse(qty, out int intQTY);
//            int.TryParse(value, out int intVALUE);
//            int newQTY = intQTY + intVALUE;
//            string _newQTY = newQTY.ToString();

//            qtyList.Remove(key);
//            qtyList.Add(unique, _newQTY);
//        }
//    }
//    else
//    {
//        qtyList.Add(unique, qty);
//    }

//    lDataSetDOStock.Next();
//}


//lDataSetDNStock = ComServer.DBManager.NewDataSet(DNStock);

//lDataSetDNStock.First();
//while ((!lDataSetDNStock.Eof))
//{
//    RecordCount++;
//    int activeValue = 1;

//    itemCode = lDataSetDNStock.FindField("ITEMCODE").AsString;
//    uom = lDataSetDNStock.FindField("UOM").AsString;
//    qty = lDataSetDNStock.FindField("SUMQTY").AsString;
//    location = lDataSetDNStock.FindField("LOCATION").AsString;
//    //if (location == "----")
//    //{
//    //    location = "HQ";
//    //}

//    string unique = itemCode + "|" + uom + "|" + location;
//    //string unique = unique.ToLower();
//    Console.WriteLine(unique + "[" + qty + "]");
//    if (qtyList.ContainsKey(unique))
//    {
//        var key = qtyList.Where(pair => pair.Key == unique)
//                    .Select(pair => pair.Key)
//                    .FirstOrDefault();
//        if (key != null)
//        {
//            var value = qtyList.Where(pair => pair.Key == unique)
//                    .Select(pair => pair.Value)
//                    .FirstOrDefault();
//            int.TryParse(qty, out int intQTY);
//            int.TryParse(value, out int intVALUE);
//            int newQTY = intQTY + intVALUE;
//            string _newQTY = newQTY.ToString();

//            qtyList.Remove(key);
//            qtyList.Add(unique, _newQTY);
//        }
//    }
//    else
//    {
//        qtyList.Add(unique, qty);
//    }

//    lDataSetDNStock.Next();
//}


//lDataSetCNStock = ComServer.DBManager.NewDataSet(CNStock);

//lDataSetCNStock.First();
//while ((!lDataSetCNStock.Eof))
//{
//    RecordCount++;
//    int activeValue = 1;

//    itemCode = lDataSetCNStock.FindField("ITEMCODE").AsString;
//    uom = lDataSetCNStock.FindField("UOM").AsString;
//    qty = lDataSetCNStock.FindField("SUMQTY").AsString;
//    location = lDataSetCNStock.FindField("LOCATION").AsString;
//    //if (location == "----")
//    //{
//    //    location = "HQ";
//    //}

//    string unique = itemCode + "|" + uom + "|" + location;
//    ////string unique = unique.ToLower();
//    Console.WriteLine(unique + "[" + qty + "]");
//    if (qtyList.ContainsKey(unique))
//    {
//        var key = qtyList.Where(pair => pair.Key == unique)
//                    .Select(pair => pair.Key)
//                    .FirstOrDefault();
//        if (key != null)
//        {
//            var value = qtyList.Where(pair => pair.Key == unique)
//                    .Select(pair => pair.Value)
//                    .FirstOrDefault();
//            int.TryParse(qty, out int intQTY);
//            int.TryParse(value, out int intVALUE);
//            int newQTY = intQTY + intVALUE;
//            string _newQTY = newQTY.ToString();

//            qtyList.Remove(key);
//            qtyList.Add(unique, _newQTY);
//        }
//    }
//    else
//    {
//        qtyList.Add(unique, qty);
//    }

//    lDataSetCNStock.Next();
//}

////'PI','RC','SC','AJ','AS','IS','DS','GR','SD'
//for (int i = 0; i < qtyList.Count; i++)
//{
//    string key = qtyList.ElementAt(i).Key;
//    string quantity = qtyList.ElementAt(i).Value;
//    string[] words = key.Split('|');

//    string product_code = words[0];
//    string uom_name = words[1];
//    string item_location = words[2];

//    Values = string.Format("('{0}','{1}','{2}','{3}','{4}','{5}')", item_location, product_code, quantity, quantity, 1, uom_name);

//    queryList1.Add(Values);
//    if (queryList1.Count % 2000 == 0)
//    {
//        string tmp_query = queryStock;
//        tmp_query += string.Join(", ", queryList1);
//        tmp_query += updateQueryStock;

//        mysql.Insert(tmp_query);

//        queryList1.Clear();
//        tmp_query = string.Empty;

//        logger.message = string.Format("{0} warehouse stock records is inserted", i);
//        logger.Broadcast();
//    }
//}

//if (queryList1.Count > 0)
//{
//    string tmp_query = queryStock;
//    tmp_query += string.Join(", ", queryList1);
//    tmp_query += updateQueryStock;

//    mysql.Insert(tmp_query);
//    mysql.Message(tmp_query);

//    logger.message = string.Format("{0} warehouse stock records is inserted", queryList1.Count);
//    logger.Broadcast();

//    queryList1.Clear();
//    tmp_query = string.Empty;
//}