using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using EasySales.Model;
using EasySales.Object;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Quartz;
using RestSharp;
using Ubiety.Dns.Core.Records;

namespace EasySales.Job
{
    [DisallowConcurrentExecution]
    public class JobAPSStockSync : IJob
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
                    slog.action_identifier = Constants.Action_APSStockSync;
                    slog.action_details = Constants.Tbl_cms_product + Constants.Is_Starting;
                    slog.action_failure = 0;
                    slog.action_failure_message = string.Empty;
                    slog.action_time = DateTime.Now.ToString();

                    DateTime startTime = DateTime.Now;

                    LocalDB.InsertSyncLog(slog);

                    logger.message = "APS stock sync is running";
                    logger.Broadcast();

                    List<DpprMySQLconfig> mysql_list = LocalDB.GetRemoteDatabaseConfig().Distinct().ToList();
                    List<DpprSQLServerconfig> mssql_list = LocalDB.GetRemoteSQLServerConfig();

                    mysql_list.Iterate<DpprMySQLconfig>((mysql_config, index) =>
                    {
                        DpprMySQLconfig mysqlconfig = mysql_list[index];
                        Database mysql = new Database();
                        mysql.Connect(index: index);

                        CheckBackendRule checkDB = new CheckBackendRule(mysql: mysql);
                        dynamic jsonRule = checkDB.CheckTablesExist().GetSettingByTableName("cms_product");

                        Dictionary<string, string> cms_updated_time = mysql.GetUpdatedTime("cms_product");

                        ArrayList mssql_rule = new ArrayList();

                        if (jsonRule != null)
                        {
                            foreach (var key in jsonRule)
                            {
                                if (key.mssql != null && key.mssql.GetType().ToString() == "Newtonsoft.Json.Linq.JArray")
                                {
                                    //string query = "SELECT DISTINCT stk.intInvid as[BarcodeID], stk.charItemCode as [ItemCode] , stk.charStatus, stk.varOENo , stk.varModel as [Model], stk.varLongModel, stk.varDesc as [Desc], stk.varLongDesc, stk.varFullDesc,  brd.varBrandNm as [Brand] , grp.varGrpNm as [Group], itt.varItemTypeNm as [ItemType] , su.varStockUnitNm as [UOM] , varNote , varInnerDiameter , varOuterDiameter , varSize , varWeight , @warehouse_select stk.decsellingprice1 as [Price1], stk.decsellingprice2 as [Price2], stk.decsellingprice3 as [Price3], stk.decsellingprice4 as [Price4], stk.decsellingprice5 as [Price5] , po.decQtyOnOrder as [POQty] , stk.charDiscCode as [DiscCode] , stk.charLocation1 as [Location], isnull(pob.decPOBQty, 0) as [POBasket], pmd.decSP4Qty as [decPromoQty], pmd.decSP4 as [decPromoPrice] from inv_stocktbl stk inner join inv_brandtbl brd on stk.intBrandId = brd.intBrandid and stk.charItemCode != '' and stk.charItemCode not like '%deleted%' inner join inv_grouptbl grp on stk.intgrpid = grp.intgrpid inner join inv_itemTypetbl itt on stk.intItemTypeID = itt.intItemTypeID inner join inv_stockunittbl su on stk.intStockUnitID = su.intStockUnitID @warehouse_join left outer join Pur_POOffSetTbl po on stk.intinvid = po.intinvid left outer join( SELECT intinvid, isnull(sum(decQty), 0) as decPOBQty from PUR_POBASKETTBL POB WHERE charStatus = 'A' and blnisdelete <> 'TRUE' GROUP BY intinvid ) pob on pob.intinvid = stk.intinvid left outer join Sal_PromotionDetailsTbl pmd on stk.intInvID = pmd.intInvID left outer join Sal_PromotionTbl pm on pmd.intPromotionID = pm.intPromotionID and dtPromotionDate <= getdate() and dtPromotionEndDate >= getdate()";
                                    
                                    
                                    string query = "SELECT DISTINCT stk.intInvid as[BarcodeID], stk.charItemCode as [ItemCode] , stk.charStatus, stk.varOENo , stk.varModel as [Model], stk.varLongModel, stk.varDesc as [Desc], stk.varLongDesc, stk.varFullDesc,  brd.varBrandNm as [Brand] , grp.varGrpNm as [Group], itt.varItemTypeNm as [ItemType] , su.varStockUnitNm as [UOM] , varNote , varInnerDiameter , varOuterDiameter , varSize , varWeight , @warehouse_select stk.decsellingprice1 as [Price1], stk.decsellingprice2 as [Price2], stk.decsellingprice3 as [Price3], stk.decsellingprice4 as [Price4], stk.decsellingprice5 as [Price5] , @poQtyQuery stk.charDiscCode as [DiscCode] , stk.charLocation1 as [Location], isnull(pob.decPOBQty, 0) as [POBasket], pmd.decSP4Qty as [decPromoQty], pmd.decSP4 as [decPromoPrice] from inv_stocktbl stk inner join inv_brandtbl brd on stk.intBrandId = brd.intBrandid and stk.charItemCode != '' and stk.charItemCode not like '%deleted%' inner join inv_grouptbl grp on stk.intgrpid = grp.intgrpid inner join inv_itemTypetbl itt on stk.intItemTypeID = itt.intItemTypeID inner join inv_stockunittbl su on stk.intStockUnitID = su.intStockUnitID @warehouse_join @poQuery left outer join( SELECT intinvid, isnull(sum(decQty), 0) as decPOBQty from PUR_POBASKETTBL POB WHERE charStatus = 'A' and blnisdelete <> 'TRUE' GROUP BY intinvid ) pob on pob.intinvid = stk.intinvid left outer join Sal_PromotionDetailsTbl pmd on stk.intInvID = pmd.intInvID left outer join Sal_PromotionTbl pm on pmd.intPromotionID = pm.intPromotionID and dtPromotionDate <= getdate() and dtPromotionEndDate >= getdate()";

                                    //@poQuery = left outer join Pur_POOffSetTbl po on stk.intinvid = po.intinvid
                                    //@poQty = po.decQtyOnOrder as [POQty] , 

                                    string where_clause = " WHERE stk.blnIsDelete = 'false' order by stk.varModel asc;";
                                    if (cms_updated_time.Count > 0)
                                    {
                                        string updated_at = cms_updated_time["updated_at"].ToString().MSSQLdate();
                                        where_clause = string.Format(" WHERE stk.blnIsDelete = 'false' AND stk.dtModifyDate >='{0}' order by stk.varModel asc;", updated_at);
                                    }

                                    query += where_clause;

                                    var mssql_rules = key.mssql;
                                    foreach (var db in mssql_rules)
                                    {
                                        string poQtyQuery = db.poqtyquery;
                                        string poQuery = db.poquery;
                                        query = query.ReplaceAll(poQtyQuery, "@poQtyQuery");
                                        query = query.ReplaceAll(poQuery, "@poQuery");

                                        string wh_join = string.Empty;
                                        string wh_select = string.Empty;
                                        ArrayList warehouse_qty_names = new ArrayList();
                                        ArrayList include = new ArrayList();
                                        ArrayList exclude = new ArrayList();

                                        if (db.include.GetType().ToString() == "Newtonsoft.Json.Linq.JArray" && db.include.Count > 0)
                                        {
                                            foreach (var item in db.include)
                                            {
                                                Dictionary<string, string> pair = new Dictionary<string, string>
                                                {
                                                    { "mysql", item.mysql.ToString() },
                                                    { "mssql", item.mssql.ToString() },
                                                    { "nullfield", item.nullfield.ToString() }
                                                };
                                                include.Add(pair);
                                            }
                                        }

                                        if (db.exclude.GetType().ToString() == "Newtonsoft.Json.Linq.JArray" && db.exclude.Count > 0)
                                        {
                                            foreach (var item in db.exclude)
                                            {
                                                Dictionary<string, string> pair = new Dictionary<string, string>
                                                {
                                                    { "mysql", item.ToString() }
                                                };
                                                exclude.Add(pair);
                                            }
                                        }

                                        if (db.warehouse.GetType().ToString() == "Newtonsoft.Json.Linq.JArray" && db.warehouse.Count > 0)
                                        {
                                            string tmp_select = " isnull(@whname.decQtyOnHand, 0) as [@whqid] ";
                                            string tmp_join = " left outer join vwActiveInv_WarehouseStockTbl wh@wid on stk.intinvid = wh@wid.intinvid and wh@wid.intWarehouseID = @wid ";
                                            foreach (var warehouse in db.warehouse)
                                            {
                                                string warehouse_id = "" + warehouse;
                                                string warehouse_name = "wh" + warehouse_id;
                                                string wh_q_id = "QTY" + warehouse_id;

                                                string tmp = tmp_select.ReplaceAll(warehouse_name, "@whname");
                                                tmp = tmp.ReplaceAll(wh_q_id, "@whqid");

                                                wh_select = wh_select + tmp + ",";

                                                tmp = string.Empty;
                                                tmp = tmp_join.ReplaceAll(warehouse_id, "@wid");

                                                wh_join += tmp;

                                                warehouse_qty_names.Add(wh_q_id);
                                            }
                                        }

                                        query = query.ReplaceAll(wh_select, "@warehouse_select");
                                        query = query.ReplaceAll(wh_join, "@warehouse_join");

                                        APSRule aps_rule = new APSRule()
                                        {
                                            DBname = db.name,
                                            DisableProduct = db.disableproduct,
                                            WarehouseList = warehouse_qty_names,
                                            Include = include,
                                            Exclude = exclude,
                                            Query = query
                                        };

                                        mssql_rule.Add(aps_rule);
                                    }
                                }
                            }
                        }
                        else
                        {
                            throw new Exception("APS Product sync requires backend rules");
                        }

                        ArrayList categoriesInMySQL = mysql.Select("SELECT * FROM cms_product_category");
                        Dictionary<string, string> categories = new Dictionary<string, string>();
                        for (int i = 0; i < categoriesInMySQL.Count; i++)
                        {
                            Dictionary<string, string> map = (Dictionary<string, string>)categoriesInMySQL[i];
                            categories.Add(map["categoryIdentifierId"], map["category_id"]);
                        }

                        mssql_rule.Iterate<APSRule>((database, idx) =>
                        {
                            SQLServer mssql = new SQLServer();
                            mssql.Connect(dbname: database.DBname);

                                Console.WriteLine(database.Query);

                                ArrayList queryResult = mssql.Select(database.Query);
                                if (queryResult.Count == 0)
                                {
                                    goto ENDJOB;
                                }
                                //bool IsExcludeAll = database.IsExcludeAll();
                                //bool IsIncludeAll = database.IsIncludeAll();

                                string mysql_insert = string.Empty;
                                string mssql_insert = string.Empty;

                                string insertQuery = "INSERT INTO cms_product (@columns) VALUES @values ON DUPLICATE KEY UPDATE @update_columns";
                                string columns = string.Empty;
                                string update_columns = string.Empty;

                                database.Include.Iterate<Dictionary<string, string>>((include, inIdx) =>
                                {
                                    bool add = true;
                                    database.Exclude.Iterate<Dictionary<string, string>>((exclude, exIdx) =>
                                    {
                                        if (exclude["mysql"] == include["mysql"])
                                        {
                                            add = false;
                                        }
                                    });

                                    columns += include["mysql"];

                                    if (add)
                                    {
                                        update_columns += (include["mysql"] + "=VALUES(" + include["mysql"] + ")");
                                        if (inIdx != database.Include.Count - 1)
                                        {
                                            update_columns += ",";
                                        }
                                    }

                                    if (inIdx != database.Include.Count - 1)
                                    {
                                        columns += ",";
                                    }
                                });

                                insertQuery = insertQuery.ReplaceAll(columns, "@columns");
                                insertQuery = insertQuery.ReplaceAll(update_columns, "@update_columns");

                                ArrayList mysqlFieldList = new ArrayList(); /* get all mysql field column */
                                database.Include.Iterate<Dictionary<string, string>>((incDict, incindex) =>
                                {
                                    string mysqlField = incDict["mysql"].ToString();
                                    mysqlFieldList.Add(mysqlField);
                                });

                                HashSet<string> valueString = new HashSet<string>();
                                queryResult.Iterate<Dictionary<string, string>>((map, i) =>
                                {
                                    string row = string.Empty;
                                    double quantity = 0;
                                    database.Include.Iterate<Dictionary<string, string>>((include, inIdx) =>
                                    {
                                        string nullfield = include["nullfield"];
                                        string find_mssql_field = include["mssql"];
                                        string corr_mysql_field = include["mysql"];

                                        bool NoMssqlField = true;
                                        bool addedToRow = false;

                                        if (find_mssql_field == "quantity")
                                        {
                                            for (int w = 0; w < database.WarehouseList.Count; w++)
                                            {
                                                map.Iterate<KeyValuePair<string, string>>((mssql_fields, mssqIndx) =>
                                                {
                                                    string wh_name = database.WarehouseList[w].ToString();
                                                    if (wh_name == mssql_fields.Key)
                                                    {
                                                        quantity += double.Parse(mssql_fields.Value);
                                                    }
                                                });
                                            }
                                            row += inIdx == 0 ? "('" + quantity + "" : "','" + quantity;

                                            NoMssqlField = false;
                                            addedToRow = true;
                                        }

                                        if (find_mssql_field == "ItemType")
                                        {
                                            // get category id here
                                            string _categoryId = "0";
                                            string itemType = string.Empty;

                                            map.Iterate<KeyValuePair<string, string>>((mssql_fields, mssqIndx) =>
                                            {
                                                if (mssql_fields.Key == "ItemType")
                                                {
                                                    itemType = mssql_fields.Value;
                                                }
                                            });

                                            if (string.IsNullOrEmpty(itemType) || !categories.TryGetValue(itemType, out _categoryId))
                                            {
                                                _categoryId = "0";
                                            }

                                            int.TryParse(_categoryId, out int CategoryId);

                                            row += inIdx == 0 ? "('" + CategoryId + "" : "','" + CategoryId;

                                            NoMssqlField = false;
                                            addedToRow = true;
                                        }

                                        if (find_mssql_field == "ItemCode")
                                        {
                                            string itemCode = string.Empty;

                                            map.Iterate<KeyValuePair<string, string>>((mssql_fields, mssqIndx) =>
                                            {
                                                if (mssql_fields.Key == "ItemCode")
                                                {
                                                    itemCode = mssql_fields.Value;
                                                    RecordCount++;
                                                }
                                            });

                                            Database.Sanitize(ref itemCode);
                                            row += inIdx == 0 ? "('" + itemCode + "" : "','" + itemCode;

                                            NoMssqlField = false;
                                            addedToRow = true;
                                        }

                                        if (find_mssql_field == "charStatus")
                                        {
                                            string status = string.Empty;
                                            string itemStatus = "0";

                                            map.Iterate<KeyValuePair<string, string>>((mssql_fields, mssqIndx) =>
                                            {
                                                if (mssql_fields.Key == "charStatus")
                                                {
                                                    status = mssql_fields.Value;

                                                    if(status == "A")
                                                    {
                                                        itemStatus = "1";
                                                    }

                                                    //A - Active
                                                    //AS / AP - Suspended
                                                    //S - Suspended
                                                    //D - Deleted
                                                }
                                            });

                                            row += inIdx == 0 ? "('" + itemStatus + "" : "','" + itemStatus;

                                            NoMssqlField = false;
                                            addedToRow = true;
                                        }

                                        if (corr_mysql_field == "product_promo")
                                        {
                                            string promoPrice = string.Empty;
                                            string promoQty = string.Empty;
                                            string promo = string.Empty;

                                            map.Iterate<KeyValuePair<string, string>>((mssql_fields, mssqIndx) =>
                                            {
                                                if (mssql_fields.Key == "decPromoQty")
                                                {
                                                    promoQty = mssql_fields.Value;
                                                }

                                                if (mssql_fields.Key == "decPromoPrice")
                                                {
                                                    promoPrice = mssql_fields.Value;
                                                }
                                            });

                                            if (promoPrice != "" && promoQty != "")
                                            {
                                                promo = "Promotion: RM " + promoPrice + " | " + promoQty + " QTY";
                                            }
                                            else
                                            {
                                                promo = "";
                                            }
                                            row += inIdx == 0 ? "('" + promo + "" : "','" + promo;

                                            NoMssqlField = false;
                                            addedToRow = true;
                                        }

                                        if (corr_mysql_field == "sequence_no")
                                        {
                                            row += inIdx == 0 ? "('" + RecordCount + "" : "','" + RecordCount;

                                            NoMssqlField = false;
                                            addedToRow = true;
                                        }

                                        if (corr_mysql_field == "search_filter")
                                        {
                                            string itemCode = string.Empty;
                                            string varLongModel = string.Empty;
                                            string Model = string.Empty;
                                            string varLongDesc = string.Empty;
                                            string varOENo = string.Empty;
                                            string DiscCode = string.Empty;
                                            string Desc = string.Empty;
                                            string Brand = string.Empty;

                                            map.Iterate<KeyValuePair<string, string>>((mssql_fields, mssqIndx) =>
                                            {
                                                if (mssql_fields.Key == "ItemCode")
                                                {
                                                    itemCode = mssql_fields.Value;
                                                    itemCode = itemCode.ReplaceAll("", "N/A", "\\", "-", " ");
                                                    Database.Sanitize(ref itemCode);
                                                    itemCode = itemCode.Replace(Environment.NewLine, " ");
                                                }
                                                if (mssql_fields.Key == "varLongModel")
                                                {
                                                    varLongModel = mssql_fields.Value;
                                                    varLongModel = varLongModel.ReplaceAll("", "N/A", "\\");
                                                    varLongModel = varLongModel.ReplaceAll("bracket", ">");
                                                    Database.Sanitize(ref varLongModel);
                                                    varLongModel = varLongModel.Replace(Environment.NewLine, " ");
                                                }
                                                if (mssql_fields.Key == "Model")
                                                {
                                                    Model = mssql_fields.Value;
                                                    Model = Model.ReplaceAll("", "N/A", "\\");
                                                    Model = Model.ReplaceAll("bracket", ">");
                                                    Database.Sanitize(ref Model);
                                                    Model = Model.Replace(Environment.NewLine, " ");
                                                }
                                                if (mssql_fields.Key == "varLongDesc")
                                                {
                                                    varLongDesc = mssql_fields.Value;
                                                    varLongDesc = varLongDesc.ReplaceAll("", "N/A", "\\");
                                                    Database.Sanitize(ref varLongDesc);
                                                    varLongDesc = varLongDesc.Replace(Environment.NewLine, " ");
                                                }
                                                if (mssql_fields.Key == "varOENo")
                                                {
                                                    varOENo = mssql_fields.Value;
                                                    varOENo = varOENo.ReplaceAll("", "N/A", "\\");
                                                    Database.Sanitize(ref varOENo);
                                                    varOENo = varOENo.Replace(Environment.NewLine, " ");
                                                }
                                                if (mssql_fields.Key == "DiscCode")
                                                {
                                                    DiscCode = mssql_fields.Value;
                                                    DiscCode = DiscCode.ReplaceAll("", "N/A", "\\");
                                                    Database.Sanitize(ref DiscCode);
                                                    DiscCode = DiscCode.Replace(Environment.NewLine, " ");
                                                }
                                                if (mssql_fields.Key == "Desc")
                                                {
                                                    Desc = mssql_fields.Value;
                                                    Desc = Desc.ReplaceAll("", "N/A", "\\");
                                                    Database.Sanitize(ref Desc);
                                                    Desc = Desc.Replace(Environment.NewLine, " ");
                                                }
                                                if (mssql_fields.Key == "Brand")
                                                {
                                                    Brand = mssql_fields.Value;
                                                    Brand = Brand.ReplaceAll("", "N/A", "\\");
                                                    Database.Sanitize(ref Brand);
                                                    Brand = Brand.Replace(Environment.NewLine, " ");
                                                }
                                            });

                                            var serializer = new JavaScriptSerializer();
                                            string searchFilter = serializer.Serialize(new
                                            {
                                                code = itemCode,
                                                model = varLongModel + " " + Model,
                                                name = varLongDesc + " " + Desc,
                                                brand = Brand,
                                                oeno = varOENo,
                                                discode = DiscCode
                                            });

                                            searchFilter = searchFilter.ReplaceAll(">", "bracket");

                                            row += inIdx == 0 ? "('" + searchFilter + "" : "','" + searchFilter;

                                            NoMssqlField = false;
                                            addedToRow = true;
                                        }

                                        map.Iterate<KeyValuePair<string, string>>((mssql_fields, mssqIndx) =>
                                        {
                                            if (find_mssql_field.EcodeContains(mssql_fields.Key))
                                            {
                                                string tmp = string.Empty;
                                                tmp = LogicParser.Parse(mssql_fields.Key, find_mssql_field, map, nullfield)[mssql_fields.Key];
                                            //logger.Broadcast(mssql_fields.Key + "----------" + tmp);

                                            if (tmp.Contains(".00"))
                                                {
                                                    tmp = tmp.ReplaceAll("", ".00");
                                                }

                                            //do looping for mysql field
                                            for (int isql = 0; isql < mysqlFieldList.Count; isql++)
                                                {
                                                    string eachField = mysqlFieldList[isql].ToString();
                                                    if (!addedToRow)
                                                    {
                                                    /* all field except itemcode/itemtype && find_mssql_field which has string/join dont insert here */
                                                        if (corr_mysql_field == eachField && LogicParser.IsCodeStr(find_mssql_field) == false)
                                                        {
                                                            Database.Sanitize(ref tmp);
                                                            row += inIdx == 0 ? "('" + tmp + "" : "','" + tmp;
                                                            addedToRow = true;
                                                        }
                                                    }
                                                }

                                                if (!addedToRow)
                                                {
                                                    Database.Sanitize(ref tmp);
                                                    if (row.Contains(tmp) == false && corr_mysql_field != "category_id" && corr_mysql_field != "product_code")
                                                    {
                                                        row += inIdx == 0 ? "('" + tmp + "" : "','" + tmp;
                                                        addedToRow = true;
                                                    }
                                                }

                                                NoMssqlField = false;
                                            }
                                        });

                                        if (NoMssqlField)
                                        {
                                            string tmp = LogicParser.Parse(corr_mysql_field, find_mssql_field, map, nullfield)[corr_mysql_field];
                                            Database.Sanitize(ref tmp);
                                            row += inIdx == 0 ? "('" + tmp + "" : "','" + tmp;
                                        }
                                    });

                                    row += "')";

                                    valueString.Add(row);

                                    if (valueString.Count % 2000 == 0)
                                    {
                                        string values = valueString.Join(",");

                                        insertQuery = insertQuery.ReplaceAll(values, "@values");

                                        mysql.Insert(insertQuery);

                                        insertQuery = insertQuery.ReplaceAll("@values", values);
                                        valueString.Clear();

                                        logger.message = string.Format("{0} stock records is inserted into " + mysqlconfig.config_database, RecordCount);
                                        logger.Broadcast();
                                    }

                                });

                                if (valueString.Count > 0)
                                {
                                    string values = valueString.Join(",");

                                    insertQuery = insertQuery.ReplaceAll(values, "@values");

                                    mysql.Insert(insertQuery);

                                    insertQuery = insertQuery.ReplaceAll("@values", values);
                                    valueString.Clear();

                                    logger.message = string.Format("{0} stock records is inserted into " + mysqlconfig.config_database, RecordCount);
                                    logger.Broadcast();
                                }

                            if (database.DisableProduct == 1)
                            {
                                //ArrayList activeProducts = mysql.Select("SELECT * FROM cms_product WHERE product_status = 1;");
                                ArrayList activeProducts = new ArrayList();
                                ArrayList activeProductCount = mysql.Select("SELECT COUNT(*) AS active_product FROM cms_product WHERE product_status = 1;"); 
                                int activeInDBCount = 0;
                                if (activeProductCount.Count > 0)
                                {
                                    Dictionary<string, string> getCount = (Dictionary<string, string>)activeProductCount[0];
                                    string _activeInDB = getCount["active_product"];
                                    int.TryParse(_activeInDB, out activeInDBCount);
                                }
                                else
                                {
                                    goto ENDJOB;
                                }

                                int offset = 0;
                            RUNANOTHERBATCH:
                                ArrayList tmpActiveProducts = mysql.Select("SELECT * FROM cms_product WHERE product_status = 1 LIMIT 3000 OFFSET " + offset + ";");
                                if(tmpActiveProducts.Count > 0)
                                {
                                    activeProducts.AddRange(tmpActiveProducts);
                                }
                                
                                if (offset < activeInDBCount)
                                {
                                    offset = activeProducts.Count;
                                    Task.Delay(3000);
                                    goto RUNANOTHERBATCH;
                                }

                                ArrayList checkActiveProducts = new ArrayList();
                                List<HashSet<string>> chunkArrays = new List<HashSet<string>>();
                                HashSet<string> tmp1 = new HashSet<string>();

                                for (int i = 0; i < activeProducts.Count; i++)
                                {
                                    Dictionary<string, string> each = (Dictionary<string, string>)activeProducts[i];
                                    string eachCode = each["product_code"].ToString();
                                    /* no need to sanitize, result from mysql already sanitized */
                                    checkActiveProducts.Add(eachCode);
                                }

                                for (int i = 0; i < activeProducts.Count; i++)
                                {
                                    Dictionary<string, string> each = (Dictionary<string, string>)activeProducts[i];
                                    string prodCode = each["product_code"].ToString();
                                    /* no need to sanitize, result from mysql already sanitized */
                                    tmp1.Add(prodCode);

                                    if (tmp1.Count % 3000 == 0)
                                    {
                                        chunkArrays.Add(tmp1);
                                        tmp1 = new HashSet<string>();
                                    }
                                }

                                if (tmp1.Count > 0)
                                {
                                    chunkArrays.Add(tmp1);
                                }

                                for (int idd = 0; idd < chunkArrays.Count; idd++)
                                {
                                    HashSet<string> productCodeList = (HashSet<string>)chunkArrays[idd];
                                    string whereNotInCode = "'" + string.Join("','", productCodeList) + "'";

                                    string checkNotInCode = "SELECT charItemCode FROM inv_stocktbl WHERE blnIsDelete = 'false' AND charItemCode IN (" + whereNotInCode + ");";
                                    // it will return the items exists in mssql

                                    ArrayList activeInMSSQL = mssql.Select(checkNotInCode);
                                    if(activeInMSSQL.Count > 0)
                                    {
                                        for (int imssql = 0; imssql < activeInMSSQL.Count; imssql++)
                                        {
                                            Dictionary<string, string> pair = (Dictionary<string, string>)activeInMSSQL[imssql];
                                            string _code = pair.ElementAt(0).Value;
                                            // this _code should be active
                                            // check whether this _code exists in ArrayList activeProducts 

                                            if (checkActiveProducts.Contains(_code)) /* remove active products from list */
                                            {
                                                int iCode = checkActiveProducts.IndexOf(_code);
                                                if (iCode != -1)
                                                {
                                                    checkActiveProducts.RemoveAt(iCode); // if exists, remove that code from ArrayList activeProducts
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        goto ENDJOB;
                                    }
                                }

                                if (checkActiveProducts.Count > 0)/* the remaining codes in ArrayList activeProducts, are needed to be deactived - set 0 */
                                {
                                    HashSet<string> testList = new HashSet<string>(); /* checking purpose */
                                    logger.Broadcast("Deactivating product... [" + checkActiveProducts.Count + "]");
                                    string inactive = "INSERT INTO cms_product (product_code, product_status) VALUES ";
                                    string inactive_duplicate = "ON DUPLICATE KEY UPDATE product_status=VALUES(product_status);";

                                    for (int i = 0; i < checkActiveProducts.Count; i++)
                                    {
                                        string _code = checkActiveProducts[i].ToString();
                                        Database.Sanitize(ref _code);
                                        string _query = string.Format("('{0}',0)", _code);
                                        mysql.Insert(inactive + _query + inactive_duplicate);
                                        testList.Add(_code); /* checking purpose */
                                    }

                                    logger.message = string.Format("{0} stock records deactivated in " + mysqlconfig.config_database, checkActiveProducts.Count);
                                    logger.Broadcast();

                                    checkActiveProducts.Clear();

                                    string testCode = "'" + string.Join("','", testList) + "'";   /* checking purpose */
                                    mysql.Message("[" + mysqlconfig.config_database + "] Deactivating code : " + testCode);
                                }
                            }
                            ENDJOB:
                            RecordCount = 0; /* reset count for the next database */

                            if (cms_updated_time.Count > 0)
                            {
                                mysql.Insert("UPDATE cms_update_time SET updated_at = NOW() WHERE table_name = 'cms_product'");
                            }
                            else
                            {
                                mysql.Insert("INSERT INTO cms_update_time(table_name, updated_at) VALUES('cms_product', NOW())");
                            }
                        });
                    });
                    
                    slog.action_identifier = Constants.Action_APSStockSync;
                    slog.action_details = Constants.Tbl_cms_product + Constants.Is_Finished + string.Format(" ({0}) records", RecordCount);
                    slog.action_failure = 0;
                    slog.action_failure_message = string.Empty;
                    slog.action_time = DateTime.Now.ToString();

                    DateTime endTime = DateTime.Now;
                    TimeSpan ts = endTime - startTime;
                    Console.WriteLine("Completed in: " + ts.TotalSeconds.ToString("F"));

                    LocalDB.InsertSyncLog(slog);

                    logger.message = "APS stock sync finished in (" + ts.TotalSeconds.ToString("F") + " seconds)";
                    logger.Broadcast();

                });

                thread.Start();
                //thread.Join();

            }
            catch (ThreadAbortException e)
            {
                DpprException ex = new DpprException
                {
                    file_name = "JobAPSStockSync",
                    exception = e.Message,
                    time = DateTime.Now.ToString()
                };
                LocalDB.InsertException(ex);

                Console.WriteLine(Constants.Thread_Exception + e.Message);
            }
        }
    }
}