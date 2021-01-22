using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
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
    public class JobAPSWarehouseQtySync : IJob
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
                    int TotalCountInserted = 0;
                    
                    GlobalLogger logger = new GlobalLogger();

                    DpprSyncLog slog = new DpprSyncLog();
                    slog.action_identifier = Constants.Action_APSWarehouseQtySync;                                 /*check again */
                    slog.action_details = Constants.Tbl_cms_warehouse_stock + Constants.Is_Starting;                   /*check again */
                    slog.action_failure = 0;
                    slog.action_failure_message = string.Empty;
                    slog.action_time = DateTime.Now.ToString();

                    DateTime startTime = DateTime.Now;

                    LocalDB.InsertSyncLog(slog);

                    logger.message = "APS warehouse stock sync is running";
                    logger.Broadcast();

                    List<DpprMySQLconfig> mysql_list = LocalDB.GetRemoteDatabaseConfig().Distinct().ToList();
                    List<DpprSQLServerconfig> mssql_list = LocalDB.GetRemoteSQLServerConfig();

                    //before make changes for lbpartner
                    //string deactivateWhQuery = "SELECT stk.intInvID, charlocation1, stk.charItemCode as [ItemCode], su.varStockUnitNm as [UOM], po.decQtyOnOrder as [POQty] from inv_stocktbl stk inner join inv_stockunittbl su on stk.intStockUnitID = su.intStockUnitID and stk.charItemCode != '' and stk.charItemCode not like '%deleted%' @warehouse_join left outer join Pur_POOffSetTbl po on stk.intinvid = po.intinvid WHERE stk.blnIsDelete = 'false'"; 
                    
                    string deactivateWhQuery = "SELECT stk.intInvID, charlocation1, charlocation2, charlocation3, charlocation4, charlocation5, charlocation6, charlocation7, stk.charItemCode as [ItemCode], su.varStockUnitNm as [UOM] @poQtyQuery from inv_stocktbl stk inner join inv_stockunittbl su on stk.intStockUnitID = su.intStockUnitID and stk.charItemCode != '' and stk.charItemCode not like '%deleted%' @warehouse_join @poQuery WHERE stk.blnIsDelete = 'false'";
                    //, po.decQtyOnOrder as [POQty]

                    mysql_list.Iterate<DpprMySQLconfig>((mysql_config, index) =>
                    {
                        DpprMySQLconfig mysqlconfig = mysql_list[index];
                        Database mysql = new Database();
                        mysql.Connect(index: index);

                        CheckBackendRule checkDB = new CheckBackendRule(mysql: mysql);
                        dynamic jsonRule = checkDB.CheckTablesExist().GetSettingByTableName("cms_warehouse_stock");

                        Dictionary<string, string> cms_updated_time = mysql.GetUpdatedTime("cms_warehouse_stock");

                        ArrayList mssql_rule = new ArrayList();

                        int whLoopCount = 7;

                        if (jsonRule != null)
                        {
                            foreach (var key in jsonRule)
                            {
                                if (key.mssql != null && key.mssql.GetType().ToString() == "Newtonsoft.Json.Linq.JArray")
                                {
                                    //string query = "SELECT stk.intInvID, charlocation1, stk.charItemCode as [ItemCode], su.varStockUnitNm as [UOM], @warehouse_null  po.decQtyOnOrder as [POQty] from inv_stocktbl stk inner join inv_stockunittbl su on stk.intStockUnitID = su.intStockUnitID and stk.charItemCode != '' and stk.charItemCode not like '%deleted%' @warehouse_join left outer join Pur_POOffSetTbl po on stk.intinvid = po.intinvid ";
                                    
                                    string query = "SELECT stk.intInvID, charlocation1, charlocation2, charlocation3, charlocation4, charlocation5, charlocation6, charlocation7, stk.charItemCode as [ItemCode], su.varStockUnitNm as [UOM], @warehouse_null  @poQtyQuery from inv_stocktbl stk inner join inv_stockunittbl su on stk.intStockUnitID = su.intStockUnitID and stk.charItemCode != '' and stk.charItemCode not like '%deleted%' @warehouse_join @poQuery ";

                                    //@poQtyQuery = po.decQtyOnOrder as [POQty]
                                    //@poQuery = left outer join Pur_POOffSetTbl po on stk.intinvid = po.intinvid

                                    string where_clause = " WHERE stk.blnIsDelete = 'false' order by stk.varModel asc;";

                                    var mssql_rules = key.mssql;
                                    foreach (var db in mssql_rules)
                                    {
                                        string poQtyQuery = db.poqtyquery;
                                        string poQuery = db.poquery;
                                        query = query.ReplaceAll(poQtyQuery, "@poQtyQuery");
                                        query = query.ReplaceAll(poQuery, "@poQuery");

                                        string wh_date = string.Empty;
                                        string wh_null = string.Empty;
                                        string wh_join = string.Empty;
                                        string whereClause = string.Empty;
                                        ArrayList include = new ArrayList();
                                        ArrayList exclude = new ArrayList();
                                        ArrayList whList = new ArrayList();
                                        Dictionary<string, string> whListPair = new Dictionary<string, string>();
                                        Dictionary<string, string> whLocationPair = new Dictionary<string, string>();

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
                                            foreach (var item in db.warehouse)
                                            {
                                                Dictionary<string, string> pair = new Dictionary<string, string>
                                                {
                                                    { "id", item.id.ToString() },
                                                    { "name", item.name.ToString() },
                                                    { "location", item.location.ToString() }
                                                };
                                                string warehouse_id = pair.ElementAt(0).Value;
                                                string warehouse_name = pair.ElementAt(1).Value;
                                                string warehouse_location = pair.ElementAt(2).Value;

                                                whListPair.Add(warehouse_id, warehouse_name);
                                                whLocationPair.Add(warehouse_id, warehouse_location);
                                            }
                                            //isnull(whhq.decQtyOnHand, 0) as [Qty1], isnull(whkl.decQtyOnHand, 0) as [Qty2], isnull(whbw.decQtyOnHand, 0) as [Qty3], isnull(whjb.decQtyOnHand, 0) as [Qty4], isnull(whpi.decQtyOnHand, 0) as [Qty5], isnull(whkj.decQtyOnHand, 0) as [Qty6],
                                            string whnullquery = " isnull(wh@whname.decQtyOnHand, 0) as [Qty@wid] ";
                                            string tmp_join = " left outer join vwActiveInv_WarehouseStockTbl wh@whname on stk.intinvid = wh@whname.intinvid and wh@whname.intWarehouseID = @wid ";
                                            string tmp_date = " wh@whname.dtModifyDate >='{0}' ";

                                            for (int iwh = 0; iwh < whListPair.Count; iwh++)
                                            {
                                                string whid = whListPair.ElementAt(iwh).Key;
                                                string whname = whListPair.ElementAt(iwh).Value;

                                                string warehouse_id = "" + whid;
                                                string warehouse_name = "" + whname;

                                                string tmp = tmp_join.ReplaceAll(warehouse_name, "@whname");
                                                string datetmp = tmp_date.ReplaceAll(warehouse_name, "@whname");
                                                tmp = tmp.ReplaceAll(warehouse_id, "@wid");
                                                wh_join += tmp;
                                                wh_date += iwh == 0 ? "" + datetmp + "" : " or " + datetmp;
                                            }

                                            for (int iwh = 0; iwh < whListPair.Count; iwh++)
                                            {
                                                int i = iwh + 1;
                                                string whid = i.ToString();
                                                string whname = whListPair.ElementAt(iwh).Value;

                                                string warehouse_id = "" + whid;
                                                string warehouse_name = "" + whname;

                                                string nulltmp = whnullquery.ReplaceAll(warehouse_name, "@whname");

                                                nulltmp = nulltmp.ReplaceAll(warehouse_id, "@wid");
                                                wh_null += iwh == 0 ? "" + nulltmp + "," : "" + nulltmp + ",";
                                            }
                                        }

                                        if (poQtyQuery == string.Empty || poQtyQuery == null)
                                        {
                                            wh_null = wh_null.Remove(wh_null.Length - 1, 1);
                                            Console.WriteLine(wh_null);
                                        }

                                        query = query.ReplaceAll(wh_join, "@warehouse_join");
                                        query = query.ReplaceAll(wh_null, "@warehouse_null");

                                        //if(poQtyQuery != string.Empty)
                                        //{

                                        //}

                                        deactivateWhQuery = deactivateWhQuery.ReplaceAll(wh_join, "@warehouse_join");
                                        deactivateWhQuery = deactivateWhQuery.ReplaceAll(poQuery, "@poQuery");
                                        if(poQtyQuery != string.Empty)
                                        {
                                            deactivateWhQuery = deactivateWhQuery.ReplaceAll(", " + poQtyQuery, "@poQtyQuery");
                                        }

                                        if (cms_updated_time.Count > 0)
                                        {
                                            string updated_at = cms_updated_time["updated_at"].ToString().MSSQLdate();
                                            //where_clause = string.Format(" WHERE stk.blnIsDelete = 'false' AND (" + wh_date + ") order by stk.varModel asc;", updated_at);
                                        }

                                        query += where_clause;
                                        string whloopcount = db.whloopcount;
                                        if(whloopcount != null)
                                        {
                                            int.TryParse(whloopcount, out whLoopCount);
                                        }

                                        APSRule aps_rule = new APSRule()
                                        {
                                            DBname = db.name,
                                            Include = include,
                                            Exclude = exclude,
                                            WarehousePairList = whListPair,
                                            WarehouseLocationList = whLocationPair,
                                            Query = query
                                        };

                                        mssql_rule.Add(aps_rule);
                                    }
                                }
                            }
                        }
                        else
                        {
                            throw new Exception("APS Warehouse Stock sync requires backend rules");
                        }

                        //SELECT whbw.dtModifyDate, whhq.dtModifyDate, stk.intInvID, charlocation1, stk.charItemCode as [ItemCode], su.varStockUnitNm as [UOM], isnull(whhq.decQtyOnHand, 0) as [Qty1], isnull(whkl.decQtyOnHand, 0) as [Qty2], isnull(whbw.decQtyOnHand, 0) as [Qty3], isnull(whjb.decQtyOnHand, 0) as [Qty4], isnull(whpi.decQtyOnHand, 0) as [Qty5], isnull(whkj.decQtyOnHand, 0) as [Qty6], po.decQtyOnOrder as [POQty] from inv_stocktbl stk inner join inv_stockunittbl su on stk.intStockUnitID = su.intStockUnitID and stk.charItemCode != '' and stk.charItemCode not like '%deleted%' left outer join vwActiveInv_WarehouseStockTbl whhq on stk.intinvid = whhq.intinvid and whhq.intWarehouseID = 1 left outer join vwActiveInv_WarehouseStockTbl whkl on stk.intinvid = whkl.intinvid and whkl.intWarehouseID = 2 left outer join vwActiveInv_WarehouseStockTbl whbw on stk.intinvid = whbw.intinvid and whbw.intWarehouseID = 3 left outer join vwActiveInv_WarehouseStockTbl whjb on stk.intinvid = whjb.intinvid and whjb.intWarehouseID = 4 left outer join vwActiveInv_WarehouseStockTbl whpi on stk.intinvid = whpi.intinvid and whpi.intWarehouseID = 5 left outer join vwActiveInv_WarehouseStockTbl whkj on stk.intinvid = whkj.intinvid and whkj.intWarehouseID = 10003 left outer join Pur_POOffSetTbl po on stk.intinvid = po.intinvid WHERE stk.blnIsDelete = 'false' and whhq.dtModifyDate >= '2020-09-01' or whkl.dtModifyDate >= '2020-09-01' or whbw.dtModifyDate >= '2020-09-01' or whjb.dtModifyDate >= '2020-09-01' or whpi.dtModifyDate >= '2020-09-01' or whkj.dtModifyDate >= '2020-09-01'

                        ArrayList warehouseItemInMySQL = mysql.Select("SELECT wh_name, wh_code FROM cms_warehouse");
                        Dictionary<string, string> warehouseItemList = new Dictionary<string, string>();

                        for (int i = 0; i < warehouseItemInMySQL.Count; i++)
                        {
                            Dictionary<string, string> each = (Dictionary<string, string>)warehouseItemInMySQL[i];
                            warehouseItemList.Add(each["wh_name"], each["wh_code"]);
                        }
                        warehouseItemInMySQL.Clear();

                        mssql_rule.Iterate<APSRule>((database, idx) =>
                        {
                            SQLServer mssql = new SQLServer();
                            mssql.Connect(dbname: database.DBname);

                            logger.Broadcast(database.Query);

                            ArrayList queryResult = mssql.Select(database.Query);
                            if (queryResult.Count == 0)
                            {
                                goto ENDJOB;
                            }
                            Console.WriteLine("queryResult.Count: " + queryResult.Count);

                            string mysql_insert = string.Empty;
                            string mssql_insert = string.Empty;

                            string insertQuery = "INSERT INTO cms_warehouse_stock (@columns) VALUES @values ON DUPLICATE KEY UPDATE @update_columns";
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
                                int iwh = 0;
                                for (int ixx = 1; ixx < whLoopCount; ixx++) //7
                                { 
                                    string row = string.Empty;
                                    string whCode = string.Empty;

                                    database.Include.Iterate<Dictionary<string, string>>((include, inIdx) =>
                                    {
                                        string nullfield = include["nullfield"];
                                        string find_mssql_field = include["mssql"];
                                        string corr_mysql_field = include["mysql"];

                                        bool NoMssqlField = true;
                                        bool addedToRow = false;

                                        if(find_mssql_field == "warehouseCode")
                                        {
                                            string wh = string.Empty;
                                            string warehouseCode = string.Empty;

                                            if (ixx == 1)
                                            {
                                                wh = database.WarehousePairList.ElementAt(iwh).Value;
                                                wh = wh.ToUpper();
                                                //database.DBname == "ULSAN" : wh = "SB";
                                                //others: wh = "HQ";
                                            }
                                            else if (ixx == 2)
                                            {
                                                wh = database.WarehousePairList.ElementAt(iwh).Value;
                                                wh = wh.ToUpper();
                                                //wh = "KL";
                                            }
                                            else if (ixx == 3)
                                            {
                                                wh = database.WarehousePairList.ElementAt(iwh).Value;
                                                wh = wh.ToUpper();
                                                //wh = "BW";
                                            }
                                            else if (ixx == 4)
                                            {
                                                wh = database.WarehousePairList.ElementAt(iwh).Value;
                                                wh = wh.ToUpper();
                                                //wh = "JB";
                                            }
                                            else if (ixx == 5)
                                            {
                                                wh = database.WarehousePairList.ElementAt(iwh).Value;
                                                wh = wh.ToUpper();
                                                //wh = "PI";
                                            }
                                            else if (ixx == 6)
                                            {
                                                wh = database.WarehousePairList.ElementAt(iwh).Value;
                                                wh = wh.ToUpper();
                                                //wh = "KJ";
                                            }

                                            if (string.IsNullOrEmpty(wh) || !warehouseItemList.TryGetValue(wh, out warehouseCode))
                                            {
                                                warehouseCode = "0";
                                            }

                                            Database.Sanitize(ref warehouseCode);
                                            
                                            row += inIdx == 0 ? "('" + warehouseCode + "" : "','" + warehouseCode;
                                            NoMssqlField = false;
                                            addedToRow = true;

                                            whCode = warehouseCode;
                                        }
                                        //item_location

                                        if (corr_mysql_field == "item_location")
                                        {
                                            string item_location = string.Empty;
                                            string fieldToGet = string.Empty;

                                            if(database.WarehouseLocationList.ContainsKey(whCode))
                                            {
                                                var value = database.WarehouseLocationList.Where(pair => pair.Key == whCode)
                                           .Select(pair => pair.Value)
                                           .FirstOrDefault();
                                                if (value != null)
                                                {
                                                    map.Iterate<KeyValuePair<string, string>>((mssql_fields, mssqIndx) =>
                                                    {
                                                        if (mssql_fields.Key == value)
                                                        {
                                                            item_location = mssql_fields.Value;
                                                            Database.Sanitize(ref item_location);
                                                            row += inIdx == 0 ? "('" + item_location + "" : "','" + item_location;
                                                        }
                                                    });
                                                }
                                            }

                                            NoMssqlField = false;
                                            addedToRow = true;
                                        }

                                        if (find_mssql_field == "quantity")
                                        {
                                            string qtyNo = string.Empty;
                                            string qty = string.Empty;

                                            qtyNo = "Qty" + ixx;
                                            map.Iterate<KeyValuePair<string, string>>((mssql_fields, mssqIndx) =>
                                            {
                                                if (mssql_fields.Key == qtyNo)
                                                {
                                                    qty = mssql_fields.Value;
                                                    if (qty == "0.00")
                                                    {
                                                        qty = "0";
                                                    }
                                                    Database.Sanitize(ref qty);
                                                    row += inIdx == 0 ? "('" + qty + "" : "','" + qty;
                                                }
                                            });

                                            NoMssqlField = false;
                                            addedToRow = true;
                                        }

                                        if (find_mssql_field == "POQty")
                                        {
                                            string poStQty = string.Empty;
                                            map.Iterate<KeyValuePair<string, string>>((mssql_fields, mssqIndx) =>
                                            {
                                                if (mssql_fields.Key == "POQty")
                                                {
                                                    poStQty = mssql_fields.Value;
                                                    if (poStQty == "0.00")
                                                    {
                                                        poStQty = "0";
                                                    }
                                                    Database.Sanitize(ref poStQty);
                                                    row += inIdx == 0 ? "('" + poStQty + "" : "','" + poStQty;
                                                }
                                            });

                                            NoMssqlField = false;
                                            addedToRow = true;
                                        }

                                        if (find_mssql_field == "DateTime.Now")
                                        {
                                            string updated_at = string.Empty;
                                            DateTime date = DateTime.Now;
                                            updated_at = date.ToString("s"); //2020-09-08 15:30:36

                                            Database.Sanitize(ref updated_at);

                                            if (row != string.Empty)
                                            {
                                                row += inIdx == 0 ? "('" + updated_at + "" : "','" + updated_at;
                                            }

                                            NoMssqlField = false;
                                            addedToRow = true;
                                        }

                                        map.Iterate<KeyValuePair<string, string>>((mssql_fields, mssqIndx) =>
                                        {
                                            if (find_mssql_field.EcodeContains(mssql_fields.Key))
                                            {
                                                string tmp = string.Empty;

                                                tmp = LogicParser.Parse(mssql_fields.Key, find_mssql_field, map, nullfield)[mssql_fields.Key];

                                                //do looping for mysql field
                                                for (int isql = 0; isql < mysqlFieldList.Count; isql++)
                                                {
                                                    string eachField = mysqlFieldList[isql].ToString();

                                                    if(!addedToRow)
                                                    {
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
                                                    if (row.Contains(tmp) == false)
                                                    {
                                                        Database.Sanitize(ref tmp);
                                                        row += inIdx == 0 ? "('" + tmp + "" : "','" + tmp;
                                                        addedToRow = true;
                                                    }
                                                }

                                                NoMssqlField = false;
                                            }
                                        });

                                        if (NoMssqlField)
                                        {
                                            if(!addedToRow)
                                            {
                                                string tmp = LogicParser.Parse(corr_mysql_field, find_mssql_field, map, nullfield)[corr_mysql_field];
                                                Database.Sanitize(ref tmp);
                                                row += inIdx == 0 ? "('" + tmp + "" : "','" + tmp;
                                            }
                                        }
                                    });

                                    row += "')";
                                    valueString.Add(row);
                                    RecordCount++;

                                    iwh++;

                                    if (valueString.Count % 6000 == 0)
                                    {
                                        string values = valueString.Join(",");

                                        insertQuery = insertQuery.ReplaceAll(values, "@values");

                                        mysql.Insert(insertQuery);
                                        mysql.Message("Wh Stock: " + insertQuery);
                                        Task.Delay(2000);

                                        insertQuery = insertQuery.ReplaceAll("@values", values);
                                        valueString.Clear();

                                        logger.message = string.Format("{0} warehouse quantity records is inserted into " + mysqlconfig.config_database, RecordCount);
                                        logger.Broadcast();
                                    }
                                }

                            });

                            if (valueString.Count > 0) 
                            {
                                 string values = valueString.Join(",");

                                insertQuery = insertQuery.ReplaceAll(values, "@values");

                                mysql.Insert(insertQuery);
                                mysql.Message("Wh Stock: " + insertQuery);
                                Task.Delay(2000);
                                //mysql.Insert("UPDATE cms_product p JOIN cms_warehouse_stock ws ON p.product_code = ws.product_code SET p.updated_at = ws.updated_at WHERE ws.updated_at > p.updated_at"); //got trigger for this
                                insertQuery = insertQuery.ReplaceAll("@values", values);
                                valueString.Clear();

                                logger.message = string.Format("{0} warehouse quantity records is inserted into " + mysqlconfig.config_database, RecordCount);
                                logger.Broadcast();
                            }

                            if (cms_updated_time.Count > 0)
                            {
                                mysql.Insert("UPDATE cms_update_time SET updated_at = NOW() WHERE table_name = 'cms_warehouse_stock'");
                            }
                            else
                            {
                                mysql.Insert("INSERT INTO cms_update_time(table_name, updated_at) VALUES('cms_warehouse_stock', NOW())");
                            }

                            //ArrayList activeWhQty = mysql.Select("SELECT * FROM cms_warehouse_stock WHERE active_status = 1;");
                            ArrayList activeWhQty = new ArrayList();
                            ArrayList activeWhQtyCount = mysql.Select("SELECT COUNT(*) AS active_wh_qty FROM cms_warehouse_stock WHERE active_status = 1;");
                            int activeInDBCount = 0;
                            if (activeWhQtyCount.Count > 0)
                            {
                                Dictionary<string, string> getCount = (Dictionary<string, string>)activeWhQtyCount[0];
                                string _activeInDB = getCount["active_wh_qty"];
                                int.TryParse(_activeInDB, out activeInDBCount);
                            }
                            else
                            {
                                goto ENDJOB;
                            }

                            int offset = 0;
                        RUNANOTHERBATCH:
                            ArrayList tmpActiveWhQty = mysql.Select("SELECT * FROM cms_warehouse_stock WHERE active_status = 1 LIMIT 3000 OFFSET " + offset + ";");
                            if(tmpActiveWhQty.Count > 0) 
                            {
                                activeWhQty.AddRange(tmpActiveWhQty);
                            }
                            
                            if (offset < activeInDBCount)
                            {
                                offset = activeWhQty.Count; //ok
                                Task.Delay(3000);
                                goto RUNANOTHERBATCH;
                            }

                            ArrayList idList = new ArrayList();

                            ArrayList checkActiveWhQty = new ArrayList();   /*contains productCode, Uom, Whcode */
                            ArrayList prodCodeList = new ArrayList();       /*contains productCode only - get to test in mssql */
                            List<HashSet<string>> chunkArrays = new List<HashSet<string>>();
                            HashSet<string> tmp1 = new HashSet<string>();

                            for (int i = 0; i < activeWhQty.Count; i++)
                            {
                                Dictionary<string, string> each = (Dictionary<string, string>)activeWhQty[i];
                                string eachCode = each["product_code"].ToString(); //from mysql db ---> already like this ("PF-14\"")

                                /* no need to sanitize, result from mysql already sanitized */

                                string eachUom = each["uom_name"].ToString();
                                string eachWhCode = each["wh_code"].ToString();

                                string _uniqueKey = eachCode + eachUom + eachWhCode;
                                string uniqueKey = _uniqueKey.ToLower();
                                checkActiveWhQty.Add(uniqueKey);

                                string eachId = each["id"].ToString();
                                idList.Add(eachId);
                            }

                            for (int i = 0; i < activeWhQty.Count; i++)
                            {
                                    Dictionary<string, string> each = (Dictionary<string, string>)activeWhQty[i];
                                    string prodCodeOnly = each["product_code"].ToString();

                                    Database.Sanitize(ref prodCodeOnly);
                                    prodCodeList.Add(prodCodeOnly);
                            }

                            for (int i = 0; i < activeWhQty.Count; i++)
                            {
                                Dictionary<string, string> each = (Dictionary<string, string>)activeWhQty[i];
                                string prodCodeOnly = each["product_code"].ToString();  /* no need to sanitize, result from mysql already sanitized */
                                tmp1.Add(prodCodeOnly);

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
                                HashSet<string> whItemList = (HashSet<string>)chunkArrays[idd];
                                string whereNotInCode = "'" + string.Join("','", whItemList) + "'";

                                Console.WriteLine(deactivateWhQuery);
                                //string checkNotInCode = "SELECT stk.intInvID, charlocation1, stk.charItemCode as [ItemCode], su.varStockUnitNm as [UOM] from inv_stocktbl stk inner join inv_stockunittbl su on stk.intStockUnitID = su.intStockUnitID and stk.charItemCode != '' and stk.charItemCode not like '%deleted%' left outer join vwActiveInv_WarehouseStockTbl whhq on stk.intinvid = whhq.intinvid and whhq.intWarehouseID = 1 left outer join vwActiveInv_WarehouseStockTbl whkl on stk.intinvid = whkl.intinvid and whkl.intWarehouseID = 2  left outer join vwActiveInv_WarehouseStockTbl whbw on stk.intinvid = whbw.intinvid and whbw.intWarehouseID = 3  left outer join vwActiveInv_WarehouseStockTbl whjb on stk.intinvid = whjb.intinvid and whjb.intWarehouseID = 4  left outer join vwActiveInv_WarehouseStockTbl whpi on stk.intinvid = whpi.intinvid and whpi.intWarehouseID = 5  left outer join vwActiveInv_WarehouseStockTbl whkj on stk.intinvid = whkj.intinvid and whkj.intWarehouseID = 10003  left outer join Pur_POOffSetTbl po on stk.intinvid = po.intinvid WHERE stk.blnIsDelete = 'false' AND stk.charItemCode IN (" + whereNotInCode + ");";
                                string checkNotInCode = deactivateWhQuery + " AND stk.charItemCode IN (" + whereNotInCode + ");";
                                ArrayList activeInMSSQL = mssql.Select(checkNotInCode);

                                if(activeInMSSQL.Count > 0)
                                {
                                    ArrayList checkInMssql = new ArrayList();
                                    for (int i = 0; i < activeInMSSQL.Count; i++)
                                    {
                                        string whCode = string.Empty;
                                        string warehouseCode = string.Empty;
                                        string wh = string.Empty;

                                        int iwh = 0;
                                        for (int ixx = 1; ixx < whLoopCount; ixx++)
                                        {
                                            Dictionary<string, string> each = (Dictionary<string, string>)activeInMSSQL[i];
                                            string eachCode = each["ItemCode"].ToString(); //from mssql return pf-14\"
                                            string eachUom = each["UOM"].ToString();
                                            string eachWhCode = string.Empty;

                                            if (ixx == 1)
                                            {
                                                wh = database.WarehousePairList.ElementAt(iwh).Value;
                                                wh = wh.ToUpper();
                                                //database.DBname == "ULSAN" : wh = "SB";
                                                //others: wh = "HQ";
                                            }
                                            else if (ixx == 2)
                                            {
                                                wh = database.WarehousePairList.ElementAt(iwh).Value;
                                                wh = wh.ToUpper();
                                                //wh = "KL";
                                            }
                                            else if (ixx == 3)
                                            {
                                                wh = database.WarehousePairList.ElementAt(iwh).Value;
                                                wh = wh.ToUpper();
                                                //wh = "BW";
                                            }
                                            else if (ixx == 4)
                                            {
                                                wh = database.WarehousePairList.ElementAt(iwh).Value;
                                                wh = wh.ToUpper();
                                                //wh = "JB";
                                            }
                                            else if (ixx == 5)
                                            {
                                                wh = database.WarehousePairList.ElementAt(iwh).Value;
                                                wh = wh.ToUpper();
                                                //wh = "PI";
                                            }
                                            else if (ixx == 6)
                                            {
                                                wh = database.WarehousePairList.ElementAt(iwh).Value;
                                                wh = wh.ToUpper();
                                                //wh = "KJ";
                                            }

                                            if (string.IsNullOrEmpty(wh) || !warehouseItemList.TryGetValue(wh, out warehouseCode))
                                            {
                                                warehouseCode = "0";
                                            }

                                            eachWhCode = warehouseCode;

                                            string _uniqueKey = eachCode + eachUom + eachWhCode;
                                            string uniqueKey = _uniqueKey.ToLower(); /* no need to sanitize again - if not will differ from mysql result */
                                            checkInMssql.Add(uniqueKey);

                                            iwh++;
                                        }
                                    }

                                    for (int imssql = 0; imssql < checkInMssql.Count; imssql++)
                                    {
                                        string _uniqueCode = checkInMssql[imssql].ToString();

                                        if (checkActiveWhQty.Contains(_uniqueCode))
                                        {
                                            int iCode = checkActiveWhQty.IndexOf(_uniqueCode);
                                            if (iCode != -1)
                                            {
                                                checkActiveWhQty.RemoveAt(iCode);
                                                idList.RemoveAt(iCode);
                                                prodCodeList.RemoveAt(iCode);
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    goto ENDJOB;
                                }
                            }

                            if (idList.Count > 0)
                            {
                                /* deactivate all at once */
                                HashSet<string> testList = new HashSet<string>(); /* checking purpose */

                                Console.WriteLine("idList.Count: " + idList.Count);
                                Console.WriteLine("prodCodeList.Count: " + prodCodeList.Count);

                                HashSet<string> idToBeDeactivateList = new HashSet<string>();
                                for (int i = 0; i < idList.Count; i++)
                                {
                                    string id = idList[i].ToString();
                                    idToBeDeactivateList.Add(id);
                                }

                                string idToBeDeactivate = "'" + string.Join("','", idToBeDeactivateList) + "'";
                                Console.WriteLine(idToBeDeactivate);

                                string inactive = "UPDATE cms_warehouse_stock SET active_status = 0 WHERE id IN (" + idToBeDeactivate + ")";
                                mysql.Insert(inactive);
                                logger.message = string.Format("{0} warehouse stock records deactivated in " + mysqlconfig.config_database, idList.Count);
                                logger.Broadcast();

                                idList.Clear();

                                //string inactive = "UPDATE cms_warehouse_stock SET active_status = 0 WHERE id =";
                                //for (int i = 0; i < idList.Count; i++)
                                //{
                                //    string id = idList[i].ToString();
                                //    mysql.Insert(inactive + id);
                                //    string code = prodCodeList[i].ToString(); /* checking purpose */
                                //    testList.Add(code);
                                //}
                                //logger.message = string.Format("{0} warehouse stock records deactivated in " + mysqlconfig.config_database, idList.Count);
                                //logger.Broadcast();

                                //string testCode = "'" + string.Join("','", testList) + "'";
                                //Console.WriteLine(testCode); /* checking purpose */

                                //idList.Clear();
                            }
                            ENDJOB:
                            TotalCountInserted = RecordCount;
                            RecordCount = 0; /* reset count for the next database */

                        });
                        //mysql.Close();
                    });

                    slog.action_identifier = Constants.Action_APSWarehouseQtySync;
                    slog.action_details = Constants.Tbl_cms_warehouse_stock + Constants.Is_Finished + string.Format(" ({0}) records", TotalCountInserted);
                    slog.action_failure = 0;
                    slog.action_failure_message = string.Empty;
                    slog.action_time = DateTime.Now.ToString();

                    DateTime endTime = DateTime.Now;
                    TimeSpan ts = endTime - startTime;
                    Console.WriteLine("Completed in: " + ts.TotalSeconds.ToString("F"));

                    LocalDB.InsertSyncLog(slog);
                    logger.message = "APS warehouse quantity sync finished in (" + ts.TotalSeconds.ToString("F") + " seconds)";
                    logger.Broadcast();

                });

                thread.Start();

            }
            catch (ThreadAbortException e)
            {
                DpprException ex = new DpprException
                {
                    file_name = "JobAPSWarehouseQtySync",
                    exception = e.Message,
                    time = DateTime.Now.ToString()
                };
                LocalDB.InsertException(ex);

                Console.WriteLine(Constants.Thread_Exception + e.Message);
            }
        }
    }
}
