using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using EasySales.Model;
using EasySales.Object;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Quartz;
using Quartz.Impl;
using Quartz.Logging;
using Renci.SshNet.Messages.Connection;

namespace EasySales.Job
{
    [DisallowConcurrentExecution]
    public class JobProductSync : IJob
    {
        public string pad(string val)
        {
            if (val != null){
                return val + " | ";
            }
            return val;
        }

        public ArrayList getRemarkName()
        {
            GlobalLogger logger = new GlobalLogger();

            dynamic hasUDF = new CheckBackendRule()
                            .CheckTablesExist()
                            .GetSettingByTableName("remark_name"); //cms_product

            ArrayList remarkName = new ArrayList();

            if (hasUDF.Count > 0)
            {
                foreach (var remarks in hasUDF)
                {
                    dynamic _remark = remarks.remark_name;

                    foreach (string value in _remark)
                    {
                        remarkName.Add(value);
                    }
                }
            }
            return remarkName;
        }
        public Dictionary<string, string> importPOqty()
        {
            GlobalLogger logger = new GlobalLogger();

            logger.Broadcast("Getting PO quantity..");

        CHECKAGAIN:
            SQLAccApi instance = SQLAccApi.getInstance();

            dynamic ComServer = instance.GetComServer();

            if (!instance.RPCAvailable())
            {
                goto CHECKAGAIN;
            }

            dynamic lRptVar, lMain, lDataSet;
            string itemCode, outQty, mainDocNo, dtDocNo;

            lRptVar = ComServer.RptObjects.Find("Purchase.OutstandingPO.RO");

            lRptVar.Params.Find("AllAgent").Value = true;
            lRptVar.Params.Find("AllArea").Value = true;
            lRptVar.Params.Find("AllCompany").Value = true;
            lRptVar.Params.Find("AllDocument").Value = true;
            lRptVar.Params.Find("AllItem").Value = true;
            lRptVar.Params.Find("AllItemProject").Value = true;
            lRptVar.Params.Find("AllTariff").Value = true;

            lRptVar.Params.Find("IncludeCancelled").Value = false;
            lRptVar.Params.Find("PrintFulfilledItem").Value = true;
            lRptVar.Params.Find("PrintOutstandingItem").Value = true;
            lRptVar.Params.Find("SelectDate").Value = false;
            lRptVar.Params.Find("SelectDeliveryDate").Value = false;
            lRptVar.Params.Find("AllDocProject").Value = true;
            lRptVar.Params.Find("AllLocation").Value = true;
            lRptVar.Params.Find("AllCompanyCategory").Value = true;
            lRptVar.Params.Find("AllBatch").Value = true;
            lRptVar.Params.Find("HasCategory").Value = false;
            lRptVar.Params.Find("AllStockGroup").Value = true;
            lRptVar.Params.Find("TranferDocFilterDate").Value = false;
            //lRptVar.Params.Find("SortBy").Value = "DocDate;DocNo;Code";

            //logger.Broadcast("BEFORE ----> Calculate Report");
            lRptVar.CalculateReport();
            //logger.Broadcast("AFTER ----> Calculate Report");

            try
            {
                lMain = lRptVar.DataSets.Find("cdsMain");
                lDataSet = lRptVar.DataSets.Find("cdsTransfer");

                lMain.First();
                lDataSet.First();

                Dictionary<string, string> outPO = new Dictionary<string, string>();

                while (!lMain.eof)
                {
                    itemCode = lMain.FindField("ItemCode").AsString;
                    mainDocNo = lMain.FindField("DocNo").AsString;

                    while (mainDocNo == lDataSet.FindField("DocNo").AsString)
                    {
                        dtDocNo = lDataSet.FindField("DocNo").AsString;
                        lDataSet.Next();

                        if (lDataSet.eof)
                        {
                            break;
                        }
                    }

                    outQty = lMain.FindField("OutstandingQty").AsString;
                    //logger.Broadcast("IN POQTY(): itemCode: " + itemCode);
                    //logger.Broadcast("IN POQTY(): outQty: " + outQty);

                    if (outPO.ContainsKey(itemCode))
                    {
                        outPO.Remove(itemCode);
                        outPO.Add(itemCode, outQty);
                    }
                    else
                    {
                        outPO.Add(itemCode, outQty);
                    }
                    //logger.Broadcast("outPO.Count: " + outPO.Count);

                    lMain.Next();
                }

                return outPO;
            }
            catch (Exception e)
            {
                DpprException ex = new DpprException
                {
                    file_name = "JobProductSync",
                    exception = e.Message,
                    time = DateTime.Now.ToLongTimeString()
                };
                LocalDB.InsertException(ex);
                logger.Broadcast("IN importPOQty() error: " + e.Message);
            }
            return new Dictionary<string, string>();
        }
        public Dictionary<string, string> importSOQty()
        {
            GlobalLogger logger = new GlobalLogger();

            logger.Broadcast("Getting SO quantity..");

            CHECKAGAIN:
            SQLAccApi instance = SQLAccApi.getInstance();

            dynamic ComServer = instance.GetComServer();

            if (!instance.RPCAvailable())
            {
                goto CHECKAGAIN;
            }

            dynamic lRptVar, lMain, lDataSet;
            string itemCode, outQty, mainDocNo, dtDocNo;

            lRptVar = ComServer.RptObjects.Find("Sales.OutstandingSO.RO");

            lRptVar.Params.Find("AllAgent").Value = true;
            lRptVar.Params.Find("AllArea").Value = true;
            lRptVar.Params.Find("AllCompany").Value = true;
            lRptVar.Params.Find("AllDocument").Value = true;
            lRptVar.Params.Find("AllItem").Value = true;
            lRptVar.Params.Find("AllItemProject").Value = true;
            lRptVar.Params.Find("AllTariff").Value = true;

            lRptVar.Params.Find("IncludeCancelled").Value = false;
            lRptVar.Params.Find("PrintFulfilledItem").Value = true;
            lRptVar.Params.Find("PrintOutstandingItem").Value = true;
            lRptVar.Params.Find("SelectDate").Value = false;
            lRptVar.Params.Find("SelectDeliveryDate").Value = false;
            lRptVar.Params.Find("AllDocProject").Value = true;
            lRptVar.Params.Find("AllLocation").Value = true;
            lRptVar.Params.Find("AllCompanyCategory").Value = true;
            lRptVar.Params.Find("AllBatch").Value = true;
            lRptVar.Params.Find("HasCategory").Value = false;
            lRptVar.Params.Find("AllStockGroup").Value = true;
            lRptVar.Params.Find("TranferDocFilterDate").Value = false;

            lRptVar.CalculateReport();

            try
            {
                lMain = lRptVar.DataSets.Find("cdsMain");
                lDataSet = lRptVar.DataSets.Find("cdsTransfer");

                lMain.First();
                lDataSet.First();

                Dictionary<string, string> outSO = new Dictionary<string, string>();

                while (!lMain.eof)
                {
                    itemCode = lMain.FindField("ItemCode").AsString;
                    mainDocNo = lMain.FindField("DocNo").AsString;

                    while (mainDocNo == lDataSet.FindField("DocNo").AsString)
                    {
                        dtDocNo = lDataSet.FindField("DocNo").AsString;
                        lDataSet.Next();

                        if (lDataSet.eof)
                        {
                            break;
                        }
                    }

                    //QTY+PO QTY-SO QTY = Avail QTY
                    //string SQty = lMain.FindField("SQty").AsString;
                    //string OutstandingQty = lMain.FindField("OutstandingQty").AsString;
                    //Console.WriteLine("["+ itemCode + "] SQty: " + SQty + " || OutstandingQty: " + OutstandingQty);
                    string SOQty = lMain.FindField("OutstandingQty").AsString;

                    if (outSO.ContainsKey(itemCode))
                    {
                        string _prevSOQTY = string.Empty;
                        outSO.TryGetValue(itemCode, out _prevSOQTY);
                        int.TryParse(_prevSOQTY, out int intPrevSOQTY);
                        int.TryParse(SOQty, out int intSOQty);
                        int addedSOQTY = intPrevSOQTY + intSOQty;
                        string _addedSOQTY = addedSOQTY.ToString();
                        outSO.Remove(itemCode); //remove old, added new
                        outSO.Add(itemCode, _addedSOQTY);
                    }
                    else
                    {
                        outSO.Add(itemCode, SOQty);
                    }

                    lMain.Next();
                }

                return outSO;
            }
            catch (Exception e)
            {
                DpprException ex = new DpprException
                {
                    file_name = "JobProductSync",
                    exception = e.Message,
                    time = DateTime.Now.ToLongTimeString()
                };
                LocalDB.InsertException(ex);
                logger.Broadcast("IN importSOQty() error: " + e.Message);
            }
            return new Dictionary<string, string>();
        }
        public Dictionary<string, string> ETA()
        {
            GlobalLogger logger = new GlobalLogger();

            logger.Broadcast("Getting the ETA..");

            CHECKAGAIN:
            SQLAccApi instance = SQLAccApi.getInstance();

            dynamic ComServer = instance.GetComServer();

            if (!instance.RPCAvailable())
            {
                goto CHECKAGAIN;
            }

            dynamic lSQL, lDataSet;
            string ETA, ITEMCODE;

            Dictionary<string, string> ETAItemCodePair = new Dictionary<string, string>();

            lSQL = "SELECT MAX(CAST(po.UDF_ETA AS DATE)) AS ETA, dtl.ITEMCODE FROM PH_PO AS po LEFT JOIN PH_PODTL dtl ON dtl.DOCKEY = po.DOCKEY WHERE po.UDF_ETA IS NOT NULL AND dtl.ITEMCODE IS NOT NULL AND dtl.ITEMCODE != '' GROUP BY dtl.ITEMCODE ORDER BY dtl.ITEMCODE;";
            lDataSet = ComServer.DBManager.NewDataSet(lSQL);

            while (!lDataSet.eof)
            {
                ETA = lDataSet.FindField("ETA").AsString;
                ETA = Convert.ToDateTime(ETA).ToString("yyyy-MM-dd");
                
                ITEMCODE = lDataSet.FindField("ITEMCODE").AsString;
                
                if (ITEMCODE != "")
                {
                    ETAItemCodePair.Add(ITEMCODE, ETA);
                }
                lDataSet.Next();
            }
            return ETAItemCodePair;
        }

        public ArrayList DisableItemBasedOnCategory() /* inactivate items related to inactive category - VIT */
        {
            ArrayList exItemCat = new ArrayList();

            dynamic disableItemCat = new CheckBackendRule()
                                            .CheckTablesExist()
                                            .GetSettingByTableName("disable_item_by_category"); //cms_product

            if (disableItemCat.Count > 0)
            {
                foreach (var excludeItemCat in disableItemCat)
                {
                    dynamic _excludeItemCat = excludeItemCat.exclude_item;

                    foreach (string value in _excludeItemCat)
                    {
                        exItemCat.Add(value);
                    }
                }
            }
            return exItemCat;
        }
        
        public ArrayList AbleItemBasedOnCategory() /* */
        {
            ArrayList inItemCat = new ArrayList();

            dynamic ableItemCat = new CheckBackendRule()
                                            .CheckTablesExist()
                                            .GetSettingByTableName("disable_item_by_category"); //cms_product

            if (ableItemCat.Count > 0)
            {
                foreach (var includeItemCat in ableItemCat)
                {
                    dynamic _includeItemCat = includeItemCat.include_item;

                    foreach (string value in _includeItemCat)
                    {
                        inItemCat.Add(value);
                    }
                }
            }
            return inItemCat;
        }

        public ArrayList DisableItem()
        {
            ArrayList exItem = new ArrayList();

            dynamic disableItem = new CheckBackendRule()
                                            .CheckTablesExist()
                                            .GetSettingByTableName("disable_product");

            if (disableItem.Count > 0)
            {
                foreach (var excludeItem in disableItem)
                {
                    dynamic _excludeItem = excludeItem.exclude_item;

                    foreach (string value in _excludeItem)
                    {
                        exItem.Add(value);
                    }
                }
            }
            return exItem;
        }

        public ArrayList EnableItem()
        {
            ArrayList enItem = new ArrayList();

            dynamic enableItem = new CheckBackendRule()
                                            .CheckTablesExist()
                                            .GetSettingByTableName("disable_product");

            if (enableItem.Count > 0)
            {
                foreach (var includeItem in enableItem)
                {
                    dynamic _includeItem = includeItem.include_item;

                    foreach (string value in _includeItem)
                    {
                        enItem.Add(value);
                    }
                }
            }
            return enItem;
        }

        public ArrayList HasUdf() /* get combineRemark and function importQty - WLS */
        {
            GlobalLogger logger = new GlobalLogger();

            dynamic hasUDF = new CheckBackendRule()
                            .CheckTablesExist()
                            .GetSettingByTableName("product_udf"); //cms_product

            ArrayList combineRemark = new ArrayList();

            if (hasUDF.Count > 0)
            {
                foreach (var include in hasUDF)
                {
                    dynamic _include = include.include;

                    foreach (var includeChild in _include)
                    {
                        foreach (var prodRemarkChild in includeChild)
                        {
                            dynamic _function = prodRemarkChild.function;
                            ArrayList functionList = new ArrayList();

                            //Console.WriteLine("function: " + _function);

                            //foreach (string function in _function)
                            //{
                            //    functionList.Add(function);
                            //}

                            //for (int ifx = 0; ifx < functionList.Count; ifx++)
                            //{
                            //    string fx = functionList[ifx].ToString();

                            //    Type thisType = this.GetType();
                            //    MethodInfo theMethod = thisType.GetMethod(fx);
                            //    theMethod.Invoke(this, null);
                            //}

                            dynamic _accounting = prodRemarkChild.accounting;
                            dynamic _separator = prodRemarkChild.separator;

                            foreach (string remarks in _accounting)
                            {
                                combineRemark.Add(remarks);
                            }
                        }
                    }
                }
            }
            return combineRemark;
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

                        DpprSyncLog slog = new DpprSyncLog
                        {
                            action_identifier = Constants.Action_ProductSync,
                            action_details = Constants.Tbl_cms_product,
                            action_failure = 0,
                            action_failure_message = "Product sync is running",
                            action_time = DateTime.Now.ToLongDateString()
                        };

                        DateTime startTime = DateTime.Now;

                        LocalDB.InsertSyncLog(slog);
                        logger.message = "----------------------------------------------------------------------------";
                        logger.Broadcast();
                        logger.message = "Product sync is running";
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

                        dynamic lRptVar, lMain;
                        string query, updateQuery;
                        string Code, Stockgroup, Description, Description2, Remark, Active, ETADate, Remarks, RemarkFieldName, RemarkField, SLTax;
                        string PoQty = "0";
                        string soQty = "0";
                        string availQty = "0";
                        string Balance = "0";

                        //no need to insert balance for Weiwo

                        List<DpprMySQLconfig> configList = LocalDB.GetRemoteDatabaseConfig();
                        DpprMySQLconfig configDatabase = configList[0];

                        dynamic jsonRule = new CheckBackendRule()
                                           .CheckTablesExist()
                                           .GetSettingByTableName("cms_product");

                        dynamic hasTax = new CheckBackendRule().CheckTablesExist().GetSettingByTableName("cms_order");

                        string tax_type = string.Empty;
                        string tax_rate = string.Empty;
                        Dictionary<string, string> ItemTax = new Dictionary<string, string>();

                        if (hasTax != null)
                        {
                            foreach (var condition in hasTax)
                            {
                                dynamic _condition = condition.condition;
                                
                                dynamic _tax = _condition.tax;
                                if (_tax != null)
                                {
                                    foreach (var taxdtl in _tax)
                                    {
                                        dynamic _taxtype = taxdtl.name;
                                        if (_taxtype != string.Empty)
                                        {
                                            tax_type = _taxtype;
                                        }

                                        dynamic _taxrate = taxdtl.rate;
                                        if (_taxrate != string.Empty)
                                        {
                                            tax_rate = _taxrate;
                                        }

                                        ItemTax.Add(tax_type, tax_rate);
                                    }
                                }
                            }
                        }

                        ArrayList exCode = new ArrayList();
                        ArrayList inCode = new ArrayList();

                        string name = string.Empty;
                        string search_filter = "0";
                        string current_quantity = "1";
                        string batch_info = "0";
                        string batch_info_seq = string.Empty;
                        string batch_info_structure = string.Empty;
                        string avail_quantity = string.Empty;

                        Dictionary<string, string> fieldNameList = new Dictionary<string, string>();

                        if (jsonRule.Count > 0)
                        {
                            foreach (var rule in jsonRule)
                            {
                                dynamic _name = rule.name; //category or group
                                if (_name != null)
                                {
                                    name = _name;
                                }

                                dynamic _search_filter = rule.search_filter;
                                if (_search_filter != null)
                                {
                                    if (_search_filter != "0")
                                    {
                                        search_filter = _search_filter;
                                    }
                                }

                                dynamic _current_quantity = rule.current_quantity;
                                if (_current_quantity != null)
                                {
                                    if (_current_quantity != "1")
                                    {
                                        current_quantity = _current_quantity;
                                    }
                                }

                                dynamic _availQty = rule.avail_quantity; //QTY + PO QTY - SO QTY = Avail QTY
                                if (_availQty != null)
                                {
                                    avail_quantity = _availQty;
                                }

                                dynamic _batch_info = rule.batch_info;
                                if (_batch_info != null)
                                {
                                    if (_batch_info != "0")
                                    {
                                        batch_info = _batch_info;
                                    }
                                }
                                
                                dynamic _batch_info_seq = rule.batch_info_seq;
                                if (_batch_info_seq != null)
                                {
                                    if (_batch_info_seq != string.Empty)
                                    {
                                        batch_info_seq = _batch_info_seq;
                                    }
                                }

                                dynamic _batch_info_structure = rule.batch_info_structure;
                                if (_batch_info_structure != null)
                                {
                                    if (_batch_info_structure != string.Empty)
                                    {
                                        batch_info_structure = _batch_info_structure;
                                    }
                                }
                            }
                        }
                        else
                        {
                            goto ENDJOB;
                        }

                        Dictionary<string, string> categoryList = new Dictionary<string, string>();
                        ArrayList categoryFromDb = mysql.Select("SELECT category_id, categoryIdentifierId FROM cms_product_category WHERE category_status = 1");

                        for (int i = 0; i < categoryFromDb.Count; i++)
                        {
                            Dictionary<string, string> each = (Dictionary<string, string>)categoryFromDb[i];
                            categoryList.Add(each["categoryIdentifierId"], each["category_id"]);
                        }
                        categoryFromDb.Clear();
                        logger.Broadcast("Category List in DB (categoryList): " + categoryList.Count);

                        ArrayList inDBactiveProducts = mysql.Select("SELECT * FROM cms_product WHERE product_status = 1;");

                        ArrayList inDBproducts = new ArrayList();
                        for (int i = 0; i < inDBactiveProducts.Count; i++)
                        {
                            Dictionary<string, string> each = (Dictionary<string, string>)inDBactiveProducts[i];
                            inDBproducts.Add(each["product_code"].ToString());
                        }
                        inDBactiveProducts.Clear();

                        logger.Broadcast("Active Products in DB (inDBproducts): " + inDBproducts.Count);

                        dynamic batchDataSet, maxSeqDT, qtyDT, itemCodeDT;
                        string itemCodeQuery = "SELECT * FROM ST_ITEM_BATCH";
                        string maxSeqQuery = "SELECT MAX(B.Seq) AS Seq FROM ST_TR A INNER JOIN ST_TR_FIFO B ON(A.TRANSNO= B.TRANSNO) WHERE A.PostDate >= '31 Dec 2017' And A.ITEMCODE = @itemCode GROUP BY A.ItemCode, A.Location, A.Batch";
                        string qtyQuery = "SELECT SB.MFGDATE, A.ItemCode, A.Location, A.Batch, B.QTY, B.COST FROM ST_TR A INNER JOIN ST_TR_FIFO B ON(A.TRANSNO = B.TRANSNO) INNER JOIN ST_BATCH SB ON SB.CODE = A.BATCH WHERE B.COSTTYPE = 'U' AND A.PostDate >= '31 Dec 2017' And A.ITEMCODE = @itemCode AND B.SEQ IN(@seq) AND B.QTY <> 0 ORDER BY A.ItemCode, A.Location, A.Batch";
                        
                        string batchQuery = "SELECT B.CODE, B.EXPDATE, B.MFGDATE, IB.ITEMCODE FROM ST_BATCH AS B LEFT JOIN ST_ITEM_BATCH AS IB ON B.AUTOKEY = IB.PARENTKEY WHERE B.ISACTIVE = 1 AND B.CODE = @batch AND IB.ITEMCODE = @itemCode";
                        if(batch_info_seq != string.Empty)
                        {
                            batchQuery += " ORDER BY " + batch_info_seq;
                        }
                        string batchCode, batch_productCode, batchExpiryDate, batchManufactureDate;
                        var batchList = new List<KeyValuePair<string, string>>();
                        ArrayList itemCodeList = new ArrayList();

                        if (batch_info == "1")
                        {
                            itemCodeDT = ComServer.DBManager.NewDataSet(itemCodeQuery);
                            string itemCode = string.Empty;
                            while (!itemCodeDT.eof)
                            {
                                itemCode = itemCodeDT.FindField("ITEMCODE").AsString;
                                if(itemCode != null)
                                {
                                    if(!itemCodeList.Contains(itemCode))
                                    {
                                        Console.WriteLine(itemCode);
                                        itemCodeList.Add(itemCode);
                                    }
                                }
                                itemCodeDT.Next();
                            }

                            string seq = string.Empty;
                            string batch = string.Empty;
                            string qty = string.Empty;

                            for (int ixx = 0; ixx < itemCodeList.Count; ixx++)
                            {
                                string iCode = itemCodeList[ixx].ToString();
                                iCode = LogicParser.QuotedStr(iCode);
                                instance.Message("iCode " + ixx + " ----> " + iCode);
                                string tmp_maxSeqQuery = maxSeqQuery.Replace("@itemCode", iCode);
                                instance.Message("tmp_maxSeqQuery " + ixx + " ----> " + tmp_maxSeqQuery);
                                try
                                {
                                    //Failed to get maxSeqQuery:StartIndex cannot be less than zero.Parameter name: startIndex
                                    maxSeqDT = ComServer.DBManager.NewDataSet(tmp_maxSeqQuery);
                                    seq = "";
                                    maxSeqDT.First();
                                    while ((!maxSeqDT.Eof))
                                    {
                                        seq = seq + maxSeqDT.FindField("SEQ").AsString + ",";
                                        maxSeqDT.Next();
                                    }
                                    seq = seq.Remove(seq.Length - 1);

                                    string tmp_qtyQuery = qtyQuery.Replace("@seq", seq);
                                    tmp_qtyQuery = tmp_qtyQuery.Replace("@itemCode", iCode);
                                    instance.Message("tmp_qtyQuery " + ixx + " ----> " + tmp_qtyQuery);
                                    
                                    try
                                    {
                                        qtyDT = ComServer.DBManager.NewDataSet(tmp_qtyQuery);

                                        qtyDT.First();
                                        while ((!qtyDT.Eof))
                                        {
                                            batch = string.Empty;
                                            qty = string.Empty;
                                            batch = qtyDT.FindField("BATCH").AsString;
                                            instance.Message("batch " + ixx + " ----> " + batch);
                                            batch = LogicParser.QuotedStr(batch);
                                            qty = qtyDT.FindField("QTY").AsString;
                                            instance.Message("qty " + ixx + " ----> " + qty);

                                            string tmp_batchQuery = batchQuery.Replace("@batch", batch);
                                            tmp_batchQuery = tmp_batchQuery.Replace("@itemCode", iCode);
                                            instance.Message("tmp_batchQuery " + ixx + " ----> " + tmp_batchQuery);
                                            batchDataSet = ComServer.DBManager.NewDataSet(tmp_batchQuery);
                                            batchDataSet.First();
                                            string batchInfo = string.Empty;

                                            while (!batchDataSet.eof)
                                            {
                                                batchCode = batchDataSet.FindField("CODE").AsString;
                                                batchExpiryDate = batchDataSet.FindField("EXPDATE").AsString;
                                                batchManufactureDate = batchDataSet.FindField("MFGDATE").AsString;
                                                string tmp = batch_info_structure;

                                                if (qty != "0") //changed 2310
                                                {
                                                    tmp = tmp.Replace("@batchCode", batchCode);
                                                    tmp = tmp.Replace("@expDate", batchExpiryDate);
                                                    tmp = tmp.Replace("@mfgDate", batchManufactureDate);
                                                    tmp = tmp.Replace("@qty", qty);
                                                    logger.Broadcast(tmp);
                                                    batchInfo = tmp;
                                                }
                                                batch_productCode = batchDataSet.FindField("ITEMCODE").AsString;
                                                batchList.Add(new KeyValuePair<string, string>(batch_productCode, batchInfo));

                                                instance.Message("before get another batch batchQuery ----> " + batchQuery);
                                                batchDataSet.Next();
                                            }
                                            qtyDT.Next();
                                        }
                                    }
                                    catch (Exception e)
                                    {
                                        instance.Message("Failed to get qtyQuery:" + e.Message);
                                    }
                                } 
                                catch (Exception ex)
                                {
                                    instance.Message("Failed to get maxSeqQuery:" + ex.Message);
                                    //Failed to get maxSeqQuery:StartIndex cannot be less than zero.Parameter name: startIndex
                                }
                                instance.Message("Next Item Code");
                                instance.Message("maxSeqQuery --->" + maxSeqQuery);
                                instance.Message("qtyQuery --->" + qtyQuery);
                                instance.Message("batchQuery --->" + batchQuery);
                            }

                            logger.Broadcast("Batch List Count: " + batchList.Count);
                        }

                        string current_quantity_query = string.Empty;
                        string current_quantity_updateQuery = string.Empty;

                        if (current_quantity == "1")
                        {
                            current_quantity_query = " product_current_quantity, ";
                            current_quantity_updateQuery = " product_current_quantity = VALUES(product_current_quantity), ";
                        }

                        //query = "INSERT INTO cms_product(category_id, product_code, product_name, product_desc, product_status, " + current_quantity_query + " sequence_no, product_remark, search_filter) VALUES "; // ,search_filter

                        //updateQuery = " ON DUPLICATE KEY UPDATE category_id = VALUES(category_id), product_name = VALUES(product_name), product_desc = VALUES(product_desc), product_remark = VALUES(product_remark), product_status = VALUES(product_status), " + current_quantity_updateQuery + " search_filter = VALUES(search_filter);"; //, search_filter = VALUES(search_filter)


                        query = "INSERT INTO cms_product(category_id, product_code, product_name, product_desc, product_status, " + current_quantity_query + " product_available_quantity, sequence_no, product_remark, search_filter) VALUES "; // ,search_filter
                        updateQuery = " ON DUPLICATE KEY UPDATE category_id = VALUES(category_id), product_name = VALUES(product_name), product_desc = VALUES(product_desc), product_remark = VALUES(product_remark), product_status = VALUES(product_status), " + current_quantity_updateQuery + "  product_available_quantity = VALUES(product_available_quantity), search_filter = VALUES(search_filter);"; //, search_filter = VALUES(search_filter)

                        Console.WriteLine(query);
                        Console.WriteLine(updateQuery);

                        HashSet<string> queryList = new HashSet<string>();
                        HashSet<string> taxValueList = new HashSet<string>();

                        ArrayList ItemDisableBasedOnCategory = new ArrayList();
                        ItemDisableBasedOnCategory = DisableItemBasedOnCategory();
                        
                        ArrayList ItemAbleBasedOnCategory = new ArrayList();
                        ItemAbleBasedOnCategory = AbleItemBasedOnCategory();
                        
                        ArrayList ItemDisable = new ArrayList();
                        ItemDisable = DisableItem();
                        
                        ArrayList ItemEnable = new ArrayList();
                        ItemEnable = EnableItem();

                        ArrayList RemarkName = new ArrayList();
                        RemarkName = getRemarkName();

                        ArrayList hasUDF = new ArrayList();
                        hasUDF = HasUdf();

                        Dictionary<string, string> importPO = new Dictionary<string, string>();
                        Dictionary<string, string> getETA = new Dictionary<string, string>();

                        if (hasUDF.Count > 0)
                        {
                            importPO = importPOqty();
                            getETA = ETA();

                            logger.Broadcast("PO qty Count: " + importPO.Count);
                            logger.Broadcast("ETA Count: " + getETA.Count);
                        }

                        Dictionary<string, string> importSO = new Dictionary<string, string>();
                        if (avail_quantity == "1")
                        {
                            importSO = importSOQty();
                            importPO = importPOqty();
                        }

                        dynamic catDataSet;
                        string catQuery = "SELECT IC.*, C.CODE AS CATCODE, C.DESCRIPTION AS CATNAME FROM ST_ITEM_CATEGORY AS IC LEFT JOIN ST_CATEGORY AS C ON IC.CATEGORY = C.CODE WHERE C.CODE != 'NULL'";
                        string catCode, prodCode;
                        var catProdCodePair = new List<KeyValuePair<string, string>>();
                        ArrayList categoryStatusList = mysql.Select("SELECT * FROM cms_product_category WHERE category_status = 1");

                        try
                        {
                            catDataSet = ComServer.DBManager.NewDataSet(catQuery);
                            catDataSet.First();

                            if (name == "category")
                            {
                                while (!catDataSet.eof)
                                {
                                    prodCode = catDataSet.FindField("CODE").AsString;
                                    catCode = catDataSet.FindField("CATEGORY").AsString;

                                    catProdCodePair.Add(new KeyValuePair<string, string>(prodCode, catCode));

                                    catDataSet.Next();
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            try
                            {
                                //instance.Message(ex.Message);
                                DpprException exception = new DpprException()
                                {
                                    file_name = "SQLAccounting + JobProductSync",
                                    exception = ex.Message,
                                    time = DateTime.Now.ToString()
                                };
                                LocalDB.InsertException(exception);
                                instance.KillSQLAccounting();
                                goto CHECKAGAIN;
                            }
                            catch (Exception exc)
                            {
                                DpprException exception = new DpprException()
                                {
                                    file_name = "SQLAccounting + JobProductSync_getting the category",
                                    exception = exc.Message,
                                    time = DateTime.Now.ToString()
                                };
                                LocalDB.InsertException(exception);
                            }
                        }

                        dynamic barcodeDataSet;
                        string barcodeQuery = "SELECT DISTINCT BARCODE, CODE FROM ST_ITEM_BARCODE";
                        string barCode, productCode;
                        var barcodeProdCodePair = new List<KeyValuePair<string, string>>();
                        
                        string barCodeQuery = "INSERT INTO cms_product(product_code,QR_code) VALUES ";
                        string barCodeUpdateQuery = " ON DUPLICATE KEY UPDATE QR_code = VALUES(QR_code)";
                        int barCodeCount = 0;
                        HashSet<string> barCodeQueryList = new HashSet<string>();

                        try
                        {
                            barcodeDataSet = ComServer.DBManager.NewDataSet(barcodeQuery);
                            barcodeDataSet.First();

                            while (!barcodeDataSet.eof)
                            {
                                barCodeCount++;
                                productCode = barcodeDataSet.FindField("CODE").AsString;
                                barCode = barcodeDataSet.FindField("BARCODE").AsString;

                                Database.Sanitize(ref productCode);
                                Database.Sanitize(ref barCode);

                                string Values = string.Format("('{0}','{1}')", productCode, barCode);

                                barCodeQueryList.Add(Values);

                                if (barCodeQueryList.Count % 2000 == 0)
                                {
                                    string tmp_query = barCodeQuery;
                                    tmp_query += string.Join(", ", barCodeQueryList);
                                    tmp_query += barCodeUpdateQuery;

                                    mysql.Insert(tmp_query);
                                    mysql.Message("barCode tmp_query:" + tmp_query);

                                    barCodeQueryList.Clear();
                                    tmp_query = string.Empty;

                                    logger.message = string.Format("{0} product barcode records is inserted", barCodeCount);
                                    logger.Broadcast();
                                }
                                barcodeDataSet.Next();
                            }

                            if (barCodeQueryList.Count > 0)
                            {
                                barCodeQuery = barCodeQuery + string.Join(", ", barCodeQueryList) + barCodeUpdateQuery;

                                mysql.Insert(barCodeQuery);
                                mysql.Message("barCodeQuery:" + barCodeQuery);

                                logger.message = string.Format("{0} product barcode records is inserted", barCodeCount);
                                logger.Broadcast();
                            }
                        }
                        catch (Exception ex)
                        {
                            try
                            {
                                //instance.Message(ex.Message);
                                DpprException exception = new DpprException()
                                {
                                    file_name = "SQLAccounting + JobProductSync + BarCode Dataset",
                                    exception = ex.Message,
                                    time = DateTime.Now.ToString()
                                };
                                LocalDB.InsertException(exception);
                                goto CHECKAGAIN;
                            }
                            catch (Exception exc)
                            {
                                DpprException exception = new DpprException()
                                {
                                    file_name = "SQLAccounting + JobProductSync + BarCode Dataset",
                                    exception = exc.Message,
                                    time = DateTime.Now.ToString()
                                };
                                LocalDB.InsertException(exception);
                            }
                        }

                        string taxQuery, taxUpdateQuery, lSQLTax;
                        string taxCode, taxRate;
                        dynamic lTaxDataSet;
                        int taxCount = 0;

                        taxQuery = "INSERT INTO cms_product(product_code, sst_code, sst_amount) VALUES ";
                        taxUpdateQuery = " ON DUPLICATE KEY UPDATE sst_code = VALUES(sst_code), sst_amount = VALUES(sst_amount)";

                        lSQLTax = "SELECT CODE, TAXRATE FROM TAX";

                        try
                        {
                            lTaxDataSet = ComServer.DBManager.NewDataSet(lSQLTax);

                            Dictionary<string, string> taxList = new Dictionary<string, string>();
                            lTaxDataSet.First();

                            while (!lTaxDataSet.eof)
                            {
                                taxCode = lTaxDataSet.FindField("CODE").AsString;
                                taxRate = lTaxDataSet.FindField("TAXRATE").AsString;

                                taxList.Add(taxCode, taxRate);

                                lTaxDataSet.Next();
                            }

                            lRptVar = ComServer.RptObjects.Find("Stock.Item.RO");

                            lRptVar.Params.Find("SelectDate").Value = false;
                            lRptVar.Params.Find("AllItem").Value = true;
                            lRptVar.Params.Find("AllStockGroup").Value = true;
                            lRptVar.CalculateReport();

                            lMain = lRptVar.DataSets.Find("cdsMain");

                            lMain.First();

                            while (!lMain.eof)
                            {
                                RecordCount++;
                                int activeValue = 1;
                                Stockgroup = string.Empty;

                                Code = lMain.FindField("CODE").AsString;

                                if (ItemDisable.Count > 0)
                                {
                                    if (ItemDisable.Contains(Code))
                                    {
                                        activeValue = 0;
                                    }
                                }
                                else
                                {
                                    if (ItemEnable.Count > 0)
                                    {
                                        if (!ItemEnable.Contains(Code))
                                        {
                                            activeValue = 0;
                                        }
                                    }
                                }

                                Stockgroup = lMain.FindField("STOCKGROUP").AsString;
                                if (name == "category")
                                {
                                    if (catProdCodePair.Count > 0)
                                    {
                                        for (int i = 0; i < catProdCodePair.Count; i++)
                                        {
                                            string product_code = catProdCodePair.ElementAt(i).Key;
                                            string category = catProdCodePair.ElementAt(i).Value;

                                            if (Code == product_code)
                                            {
                                                if (categoryStatusList.Contains(category))
                                                {
                                                    Stockgroup = category;
                                                    break;
                                                }
                                                else
                                                {
                                                    Stockgroup = category;
                                                }
                                            }
                                        }
                                    }
                                }

                                Description = lMain.FindField("DESCRIPTION").AsString;
                                Description2 = lMain.FindField("DESCRIPTION2").AsString;
                                Remark = lMain.FindField("REMARK2").AsString;
                                Active = lMain.FindField("ISACTIVE").AsString;
                                SLTax = lMain.FindField("SLTAX").AsString;

                                if (current_quantity == "1")
                                {
                                    Balance = lMain.FindField("BALSQTY").AsString;
                                }

                                Database.Sanitize(ref Code);
                                Database.Sanitize(ref SLTax);

                                if (taxList.Count > 0)
                                {
                                    for (int i = 1; i < taxList.Count; i++)
                                    {
                                        string tax_code;
                                        string tax_amt = string.Empty;
                                        Dictionary<string, string> each = (Dictionary<string, string>)taxList;
                                        tax_code = each.ElementAt(i).Key;
                                        tax_amt = each.ElementAt(i).Value;

                                        Database.Sanitize(ref tax_code);

                                        if (SLTax == tax_code)
                                        {
                                            taxCount++;
                                            if (tax_amt == "A")
                                            {
                                                if (ItemTax.Count > 0)
                                                {
                                                    string _taxtype = string.Empty;
                                                    string _taxrate = string.Empty;

                                                    string taxtype = string.Empty;
                                                    string taxrate = string.Empty;

                                                    for (int itax = 0; itax < ItemTax.Count; itax++)
                                                    {
                                                        _taxtype = ItemTax.ElementAt(itax).Key;
                                                        _taxrate = ItemTax.ElementAt(itax).Value;
                                                        tax_amt = _taxrate;
                                                    }
                                                }
                                            }
                                            tax_amt = tax_amt.ReplaceAll("", "%", ";", "A");
                                            Database.Sanitize(ref tax_amt);

                                            string taxValue = string.Format("('{0}','{1}','{2}')", Code, tax_code, tax_amt);
                                            taxValueList.Add(taxValue);
                                            break;
                                        }
                                    }
                                }

                                RemarkField = string.Empty;

                                if (Active == "F")
                                {
                                    activeValue = 0;
                                }

                                string _categoryId = "0";

                                if (!categoryList.TryGetValue(Stockgroup, out _categoryId))
                                {
                                    _categoryId = "0";
                                }

                                if (ItemDisableBasedOnCategory.Count > 0)
                                {
                                    if (ItemDisableBasedOnCategory.Contains(_categoryId))
                                    {
                                        activeValue = 0;
                                    }
                                }
                                else
                                {
                                    if (ItemAbleBasedOnCategory.Count > 0)
                                    {
                                        if (!ItemAbleBasedOnCategory.Contains(_categoryId))
                                        {
                                            activeValue = 0;
                                        }
                                    }
                                }

                                int.TryParse(_categoryId, out int CategoryId);


                                if (hasUDF.Count > 0)
                                {
                                    PoQty = "0";
                                    ETADate = "";

                                    if (importPO.Count > 0)
                                    {
                                        string _poQty;
                                        if (importPO.ContainsKey(Code))
                                        {
                                            importPO.TryGetValue(Code, out _poQty);
                                            PoQty = _poQty;
                                        }
                                        else
                                        {
                                            PoQty = "0";
                                        }
                                    }

                                    if (getETA.Count > 0)
                                    {
                                        DateTime todayDate = DateTime.Now.Date;
                                        string _etaDate;
                                        if (getETA.ContainsKey(Code))
                                        {
                                            getETA.TryGetValue(Code, out _etaDate);
                                            DateTime etaDateTime = Convert.ToDateTime(_etaDate).Date;
                                            Console.WriteLine("etaDateTime.Date: " + etaDateTime.Date);
                                            Console.WriteLine("todayDate.Date: " + todayDate.Date);
                                            if (etaDateTime.Date >= todayDate.Date)
                                            {
                                                ETADate = _etaDate;
                                            }
                                            else
                                            {
                                                ETADate = "";
                                            }
                                        }
                                        else
                                        {
                                            ETADate = "";
                                        }
                                    }

                                    Remark = pad(String.Format("PO QTY:{0}", PoQty));
                                    if (ETADate != "")
                                    {
                                        Remark += pad(String.Format("ETA:{0}", ETADate));
                                    }

                                    for (int i = 0; i < hasUDF.Count; i++) /* GET WITHIN LOOP */
                                    {
                                        int noPadCount = 0;
                                        noPadCount = hasUDF.Count - 1;

                                        Remarks = hasUDF[i].ToString();
                                        RemarkFieldName = RemarkName[i].ToString();
                                        if (i != noPadCount)
                                        {
                                            Remark += pad(RemarkFieldName + ": " + lMain.FindField(Remarks).AsString);
                                        }
                                        else
                                        {
                                            Remark += RemarkFieldName + ": " + lMain.FindField(Remarks).AsString;
                                        }
                                    }
                                }

                                if (inDBproducts.Count > 0)
                                {
                                    if (inDBproducts.Contains(Code))
                                    {
                                        int index = inDBproducts.IndexOf(Code);
                                        if (index != -1)
                                        {
                                            inDBproducts.RemoveAt(index);
                                        }
                                    }
                                }

                                if (avail_quantity == "1") //QTY+PO QTY-SO QTY = Avail QTY
                                {
                                    Balance = lMain.FindField("BALSQTY").AsString;
                                    if (importPO.Count > 0)
                                    {
                                        string _poQty;
                                        if (importPO.ContainsKey(Code))
                                        {
                                            importPO.TryGetValue(Code, out _poQty);
                                            PoQty = _poQty;
                                        }
                                        else
                                        {
                                            PoQty = "0";
                                        }
                                    }

                                    if (importSO.Count > 0)
                                    {
                                        string _soQty;
                                        if (importSO.ContainsKey(Code))
                                        {
                                            importSO.TryGetValue(Code, out _soQty);
                                            soQty = _soQty;
                                        }
                                        else
                                        {
                                            soQty = "0";
                                        }
                                    }
                                    //QTY+PO QTY-SO QTY = Avail QTY
                                    int.TryParse(PoQty, out int intPOQTY);
                                    int.TryParse(soQty, out int intSOQTY);
                                    int.TryParse(Balance, out int intBalance);

                                    int _availQty = intBalance + intPOQTY - intSOQTY;
                                    availQty = _availQty.ToString();
                                    //Console.WriteLine(Code + ": " + intBalance + "+" + intPOQTY + "-" + intSOQTY + "=" + availQty);
                                }

                                if (batch_info == "1")
                                {
                                    if (batchList.Count > 0)
                                    {
                                        int idx = 0;
                                        Remark = string.Empty;

                                        foreach (var keyValue in batchList)
                                        {
                                            if (keyValue.Key == Code)
                                            {
                                                idx++;
                                                string batchinfo = keyValue.Value;
                                                Remark += idx == 1 ? "" + batchinfo + "" : " | " + batchinfo;
                                            }
                                        }
                                    }
                                }

                                if(current_quantity == "0")
                                {
                                    Balance = "0";
                                }

                                Database.Sanitize(ref Stockgroup);
                                Database.Sanitize(ref Description);
                                Database.Sanitize(ref Description2);
                                Database.Sanitize(ref Remark);
                                Database.Sanitize(ref Balance);

                                string searchFilter = string.Empty;

                                if (search_filter == "1")
                                {
                                    Description = Description.ReplaceAll("singlequote", "'");
                                    Remark = Remark.ReplaceAll("bracket", ">");

                                    var serializer = new JavaScriptSerializer();
                                    searchFilter = serializer.Serialize(new
                                    {
                                        code = Code,
                                        name = Description,
                                        description = Description2,
                                        remark = Remark
                                    });
                                    searchFilter = searchFilter.ReplaceAll(">", "bracket");
                                    searchFilter = searchFilter.ReplaceAll("'", "singlequote");

                                    Remark = Remark.ReplaceAll(">", "bracket");
                                    Description = Description.ReplaceAll("'", "singlequote");

                                    Database.Sanitize(ref searchFilter);
                                }

                                string Values = string.Format("('{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}','{8}')", CategoryId, Code, Description, Description2, activeValue, availQty, RecordCount, Remark, searchFilter);

                                if (current_quantity == "1")
                                {
                                    Values = string.Format("('{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}','{8}','{9}')", CategoryId, Code, Description, Description2, activeValue, Balance, availQty, RecordCount, Remark, searchFilter);
                                }

                                //string Values = string.Format("('{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}','{8}','{9}')", CategoryId, Code, Description, Description2, activeValue, Balance, availQty, RecordCount, Remark, searchFilter);

                                queryList.Add(Values);

                                if (queryList.Count % 2000 == 0)
                                {
                                    string tmp_query = query;
                                    tmp_query += string.Join(", ", queryList);
                                    tmp_query += updateQuery;

                                    mysql.Insert(tmp_query);

                                    queryList.Clear();
                                    tmp_query = string.Empty;

                                    logger.message = string.Format("{0} product records is inserted", RecordCount);
                                    logger.Broadcast();
                                }

                                lMain.Next();
                            }

                            if (queryList.Count > 0)
                            {
                                query = query + string.Join(", ", queryList) + updateQuery;

                                mysql.Insert(query);

                                logger.message = string.Format("{0} product records is inserted", RecordCount);
                                logger.Broadcast();
                            }

                            if (taxValueList.Count > 0)
                            {
                                taxQuery = taxQuery + string.Join(", ", taxValueList) + taxUpdateQuery;

                                mysql.Insert(taxQuery);

                                logger.message = string.Format("{0} product tax records is inserted", taxCount);
                                logger.Broadcast();
                            }

                            if (inDBproducts.Count > 0) /*inactivate products which no longer in SQLAcc*/
                            {
                                string inactive = "INSERT INTO cms_product (product_code, product_status) VALUES ";
                                string inactive_duplicate = "ON DUPLICATE KEY UPDATE product_status=VALUES(product_status);";
                                for (int i = 0; i < inDBproducts.Count; i++)
                                {
                                    string _code = inDBproducts[i].ToString();
                                    Database.Sanitize(ref _code);
                                    string _query = string.Format("('{0}',0)", _code);
                                    mysql.Message("Deactivate product query: " + _query);
                                    mysql.Insert(inactive + _query + inactive_duplicate);
                                }

                                logger.Broadcast(inDBproducts.Count + " products deactivated");
                                inDBproducts.Clear();
                            }
                        }
                        catch (Exception ex)
                        {
                            try
                            {
                                //instance.Message(ex.Message);
                                DpprException exception = new DpprException()
                                {
                                    file_name = "SQLAccounting + JobProductSync",
                                    exception = ex.Message,
                                    time = DateTime.Now.ToString()
                                };
                                LocalDB.InsertException(exception);
                                goto CHECKAGAIN;
                            }
                            catch (Exception exc)
                            {
                                DpprException exception = new DpprException()
                                {
                                    file_name = "SQLAccounting + JobProductSync",
                                    exception = exc.Message,
                                    time = DateTime.Now.ToString()
                                };
                                LocalDB.InsertException(exception);
                            }
                        }

                        ENDJOB:

                        slog.action_identifier = Constants.Action_ProductSync;
                        slog.action_details = Constants.Tbl_cms_product + Constants.Is_Finished + string.Format(" ({0}) records", RecordCount);
                        slog.action_failure = 0;
                        slog.action_failure_message = string.Empty;
                        slog.action_time = DateTime.Now.ToString();

                        DateTime endTime = DateTime.Now;
                        TimeSpan ts = endTime - startTime;
                        Console.WriteLine("Completed in: " + ts.TotalSeconds.ToString("F"));

                        LocalDB.InsertSyncLog(slog);

                        logger.message = "Product sync finished in (" + ts.TotalSeconds.ToString("F") + " seconds)";
                        logger.Broadcast();
                    });

                    thread.Start();
                    //thread.Join();

                }
                catch (ThreadAbortException e)
                {
                    DpprException ex = new DpprException
                    {
                        file_name = "JobProductSync",
                        exception = e.Message,
                        time = DateTime.Now.ToLongTimeString()
                    };
                    LocalDB.InsertException(ex);

                    Console.WriteLine(Constants.Thread_Exception + e.Message);
                }
            }
        }
    }