using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EasySales.Model;
using EasySales.Object;
using Quartz;
using System.Linq;
using Microsoft.EntityFrameworkCore.Internal;
using System.Net.Sockets;
using System.IO;
using System.Drawing;
using System.Text;
using System.Net;
using System.Data.SqlClient;
using System.Data;

namespace EasySales.Job
{
    [DisallowConcurrentExecution]
    class JobAPSImageSync : IJob
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
                    slog.action_identifier = Constants.Action_APSImageSync;
                    slog.action_details = Constants.Tbl_cms_product_image + Constants.Is_Starting;
                    slog.action_failure = 0;
                    slog.action_failure_message = string.Empty;
                    slog.action_time = DateTime.Now.ToString();

                    DateTime startTime = DateTime.Now;

                    LocalDB.InsertSyncLog(slog);

                    logger.message = "APS image sync is running";
                    logger.Broadcast();

                    List<DpprMySQLconfig> mysql_list = LocalDB.GetRemoteDatabaseConfig().Distinct().ToList();
                    List<DpprSQLServerconfig> mssql_list = LocalDB.GetRemoteSQLServerConfig();
                    List<DpprFTPServerConfig> ftplist = LocalDB.GetFTPServerConfig();
                    
                    mysql_list.Iterate<DpprMySQLconfig>((mysql_config, index) =>
                    {
                        DpprMySQLconfig mysqlconfig = mysql_list[index];
                        Database mysql = new Database();
                        mysql.Connect(index: index);

                        CheckBackendRule checkDB = new CheckBackendRule(mysql: mysql);
                        dynamic jsonRule = checkDB.CheckTablesExist().GetSettingByTableName("cms_product_image");

                        Dictionary<string, string> cms_updated_time = mysql.GetUpdatedTime("cms_product_image");

                        ArrayList mssql_rule = new ArrayList();

                        if (jsonRule != null)
                        {
                            foreach (var key in jsonRule)
                            {
                                if (key.mssql != null && key.mssql.GetType().ToString() == "Newtonsoft.Json.Linq.JArray")
                                {
                                    //string query = "SELECT stk.charItemCode as [ItemCode], isnull(img1.imgimagedata,'') as [image1] , isnull(img2.imgimagedata,'') as [image2] , isnull(img3.imgimagedata,'') as [image3] , isnull(img4.imgimagedata,'') as [image4] , isnull(img5.imgimagedata,'') as [image5] , isnull(img6.imgimagedata,'') as [image6] , isnull(img7.imgimagedata,'') as [image7] , isnull(img8.imgimagedata,'') as [image8] from inv_stocktbl stk";
                                    
                                    string query = "SELECT stk.charItemCode as [ItemCode],  CONVERT(VARBINARY(MAX), img1.imgimagedata)  as [image1], CONVERT(VARBINARY(MAX), img2.imgimagedata) as [image2] , CONVERT(VARBINARY(MAX), img3.imgimagedata) as [image3] , CONVERT(VARBINARY(MAX), img4.imgimagedata) as [image4] , CONVERT(VARBINARY(MAX), img5.imgimagedata) as [image5] , CONVERT(VARBINARY(MAX), img6.imgimagedata) as [image6] , CONVERT(VARBINARY(MAX), img7.imgimagedata) as [image7] , CONVERT(VARBINARY(MAX), img8.imgimagedata) as [image8] from inv_stocktbl stk";
                                    if (cms_updated_time.Count > 0)
                                    {
                                        string updated_at = cms_updated_time["updated_at"].ToString().MSSQLdate();

                                        query += " left outer join inv_StockImageTbl img1 on stk.intinvid = img1.intinvid and img1.intseat = 1 AND img1.dtModifyDate >'"+ updated_at +"' left outer join inv_StockImageTbl img2 on stk.intinvid = img2.intinvid and img2.intseat = 2 AND img2.dtModifyDate >'"+ updated_at +"' left outer join inv_StockImageTbl img3 on stk.intinvid = img3.intinvid and img3.intseat = 3 AND img3.dtModifyDate >'"+ updated_at +"' left outer join inv_StockImageTbl img4 on stk.intinvid = img4.intinvid and img4.intseat = 4 AND img4.dtModifyDate >'"+ updated_at +"' left outer join inv_StockImageTbl img5 on stk.intinvid = img5.intinvid and img5.intseat = 5 AND img5.dtModifyDate >'"+ updated_at +"' left outer join inv_StockImageTbl img6 on stk.intinvid = img6.intinvid and img6.intseat = 6 AND img6.dtModifyDate >'"+ updated_at +"' left outer join inv_StockImageTbl img7 on stk.intinvid = img7.intinvid and img7.intseat = 7 AND img7.dtModifyDate >'"+ updated_at +"' left outer join inv_StockImageTbl img8 on stk.intinvid = img8.intinvid and img8.intseat = 8 AND img8.dtModifyDate >'"+ updated_at +"'";
                                        }
                                    else
                                    {
                                        query += " left outer join inv_StockImageTbl img1 on stk.intinvid = img1.intinvid and img1.intseat = 1  left outer join inv_StockImageTbl img2 on stk.intinvid = img2.intinvid and img2.intseat = 2 left outer join inv_StockImageTbl img3 on stk.intinvid = img3.intinvid and img3.intseat = 3 left outer join inv_StockImageTbl img4 on stk.intinvid = img4.intinvid and img4.intseat = 4 left outer join inv_StockImageTbl img5 on stk.intinvid = img5.intinvid and img5.intseat = 5 left outer join inv_StockImageTbl img6 on stk.intinvid = img6.intinvid and img6.intseat = 6 left outer join inv_StockImageTbl img7 on stk.intinvid = img7.intinvid and img7.intseat = 7 left outer join inv_StockImageTbl img8 on stk.intinvid = img8.intinvid and img8.intseat = 8";
                                    }

                                    query += " where stk.blnisdelete = 'false' AND stk.charItemCode not like '%deleted%' and stk.charItemCode !='' AND (CONVERT(VARBINARY(MAX), img1.imgimagedata) <> 0x or CONVERT(VARBINARY(MAX), img2.imgimagedata) <> 0x or CONVERT(VARBINARY(MAX), img3.imgimagedata) <> 0x or CONVERT(VARBINARY(MAX), img4.imgimagedata) <> 0x or CONVERT(VARBINARY(MAX), img5.imgimagedata) <> 0x or CONVERT(VARBINARY(MAX),img6.imgimagedata) <> 0x or CONVERT(VARBINARY(MAX), img7.imgimagedata) <> 0x or CONVERT(VARBINARY(MAX), img8.imgimagedata) <> 0x) order by stk.charItemCode OFFSET @offset ROWS FETCH NEXT @eachBatch ROWS ONLY";

                                    //Console.WriteLine(query);

                                    var mssql_rules = key.mssql;
                                    foreach (var db in mssql_rules)
                                    {
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

                                        APSRule aps_rule = new APSRule()
                                        {
                                            DBname = db.name,
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
                            throw new Exception("APS Image sync requires backend rules");
                        }

                        ArrayList productsInMySQL = mysql.Select("SELECT product_id, product_code FROM cms_product");
                        Dictionary<string, string> productList = new Dictionary<string, string>();
                        for (int i = 0; i < productsInMySQL.Count; i++)
                        {
                            Dictionary<string, string> map = (Dictionary<string, string>)productsInMySQL[i];
                            productList.Add(map["product_code"], map["product_id"]);
                        }
                        
                        ArrayList productSequence = mysql.Select("SELECT MAX(sequence_no) as sequence_no FROM cms_product_image");
                        int sequence = 0;

                        for (int i = 0; i < productSequence.Count; i++)
                        {
                            Dictionary<string, string> map = (Dictionary<string, string>)productSequence[i];

                            if(map["sequence_no"] != "")
                            {
                                int intseq = 0;
                                int.TryParse(map["sequence_no"], out intseq);
                                sequence = intseq + 1;
                            }
                            else
                            {
                                sequence = 1;
                            }
                        }

                        mssql_rule.Iterate<APSRule>((database, idx) =>
                        {
                            SQLServer mssql = new SQLServer();
                            mssql.Connect(dbname: database.DBname);

                            string eachBatch = "100";
                            string offset = "0";  //Change offset here to decide start from where

                            int.TryParse(offset, out int intoffset);
                            int.TryParse(eachBatch, out int intEachBatch);

                            string totalRecordsQuery = "SELECT COUNT(*) as total from inv_stocktbl stk";

                            if (cms_updated_time.Count > 0)
                            {
                                string updated_at = cms_updated_time["updated_at"].ToString().MSSQLdate();
                                totalRecordsQuery += " left outer join inv_StockImageTbl img1 on stk.intinvid = img1.intinvid and img1.intseat = 1 AND img1.dtModifyDate >'" + updated_at + "' left outer join inv_StockImageTbl img2 on stk.intinvid = img2.intinvid and img2.intseat = 2 AND img2.dtModifyDate >'" + updated_at + "' left outer join inv_StockImageTbl img3 on stk.intinvid = img3.intinvid and img3.intseat = 3 AND img3.dtModifyDate >'" + updated_at + "' left outer join inv_StockImageTbl img4 on stk.intinvid = img4.intinvid and img4.intseat = 4 AND img4.dtModifyDate >'" + updated_at + "' left outer join inv_StockImageTbl img5 on stk.intinvid = img5.intinvid and img5.intseat = 5 AND img5.dtModifyDate >'" + updated_at + "' left outer join inv_StockImageTbl img6 on stk.intinvid = img6.intinvid and img6.intseat = 6 AND img6.dtModifyDate >'" + updated_at + "' left outer join inv_StockImageTbl img7 on stk.intinvid = img7.intinvid and img7.intseat = 7 AND img7.dtModifyDate >'" + updated_at + "' left outer join inv_StockImageTbl img8 on stk.intinvid = img8.intinvid and img8.intseat = 8 AND img8.dtModifyDate >'" + updated_at + "'";
                            }
                            else
                            {
                                totalRecordsQuery += " left outer join inv_StockImageTbl img1 on stk.intinvid = img1.intinvid and img1.intseat = 1 left outer join inv_StockImageTbl img2 on stk.intinvid = img2.intinvid and img2.intseat = 2 left outer join inv_StockImageTbl img3 on stk.intinvid = img3.intinvid and img3.intseat = 3 left outer join inv_StockImageTbl img4 on stk.intinvid = img4.intinvid and img4.intseat = 4 left outer join inv_StockImageTbl img5 on stk.intinvid = img5.intinvid and img5.intseat = 5 left outer join inv_StockImageTbl img6 on stk.intinvid = img6.intinvid and img6.intseat = 6 left outer join inv_StockImageTbl img7 on stk.intinvid = img7.intinvid and img7.intseat = 7 left outer join inv_StockImageTbl img8 on stk.intinvid = img8.intinvid and img8.intseat = 8";
                            }

                            totalRecordsQuery += " where stk.charItemCode not like '%deleted%' and stk.charItemCode !='' AND (CONVERT(VARBINARY(MAX),img1.imgimagedata) <> 0xFDD8 or CONVERT(VARBINARY(MAX), img2.imgimagedata) <> 0xFDD8 or CONVERT(VARBINARY(MAX), img3.imgimagedata) <> 0xFDD8 or CONVERT(VARBINARY(MAX), img4.imgimagedata) <> 0xFDD8 or CONVERT(VARBINARY(MAX), img5.imgimagedata) <> 0xFDD8 or CONVERT(VARBINARY(MAX), img6.imgimagedata) <> 0xFDD8 AND CONVERT(VARBINARY(MAX), img7.imgimagedata) <> 0xFDD8 or CONVERT(VARBINARY(MAX), img8.imgimagedata) <> 0xFDD8)";

                            ArrayList numRecords = mssql.Select(totalRecordsQuery);
                            string _numOfRecords = string.Empty;
                            int numOfRecords = 0;

                            Dictionary<string, string> each = (Dictionary<string, string>)numRecords[0];
                            _numOfRecords = each["total"];
                            int.TryParse(_numOfRecords, out numOfRecords);

                            string mysql_insert = string.Empty;
                            string mssql_insert = string.Empty;

                            string insertQuery = "INSERT INTO cms_product_image (@columns) VALUES @values ON DUPLICATE KEY UPDATE @update_columns";

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

                            string path = "../../images/tmp/" + database.DBname + "/";
                            string fileNameOnly = string.Empty;

                            if (Directory.Exists(path))//@"../../images/tmp/"
                            {
                                //Console.WriteLine("Path exists/created");
                                //if exists, delete all files.
                                int fileCount = 0;
                                string[] files = Directory.GetFiles(path);
                                foreach (string file in files)
                                {
                                    //fileNameOnly = file.ReplaceAll("", "../../images/tmp/", ".txt");
                                    //File.Delete(file);
                                    //Console.WriteLine($"{file} is deleted.");
                                    
                                    fileCount++;
                                }
                                Console.WriteLine("Total of deleted file in " + path + " : " + fileCount);
                            }
                            else
                            {
                                Directory.CreateDirectory(path);//"../../images/tmp/LBFORCEKL/"
                            }

                        RUNANOTHERBATCH:
                            DpprFTPServerConfig ftpconfig = ftplist[index];

                            database.Query = database.Query.ReplaceAll(eachBatch, "@eachBatch");
                            database.Query = database.Query.ReplaceAll(offset, "@offset");

                            ArrayList queryResult = mssql.Select(database.Query);

                            ArrayList pictureList = new ArrayList();
                            ArrayList codeUrlList = new ArrayList();

                            ArrayList itemCodeImageUrlPair = mssql.getAndUploadImageMSSQL(database.Query, path, "image", index);

                            queryResult.Iterate<Dictionary<string, string>>((map, i) =>
                            {
                                for (int ixx = 1; ixx < 9; ixx++)
                                {
                                    string row = string.Empty;
                                    database.Include.Iterate<Dictionary<string, string>>((include, inIdx) =>
                                    {
                                        string nullfield = include["nullfield"];
                                        string find_mssql_field = include["mssql"];
                                        string corr_mysql_field = include["mysql"];

                                        bool NoMssqlField = true;
                                        bool addedToRow = false;

                                        string code = string.Empty;

                                        map.Iterate<KeyValuePair<string, string>>((mssql_fields, mssqIndx) =>
                                        {
                                            if (mssql_fields.Key == "ItemCode")
                                            {
                                                code = mssql_fields.Value;
                                            }
                                        });

                                        if (find_mssql_field == "imageUrl")
                                        {
                                            string img = string.Empty;
                                            string image = string.Empty;

                                            //img = "image" + ixx;
                                            if (img != "image")
                                            {
                                                string _ixx = "_" + ixx;
                                                string imageUrl = string.Empty;
                                                for (int icc = 0; icc < itemCodeImageUrlPair.Count; icc++)
                                                {
                                                    KeyValuePair<string, string> search = (KeyValuePair<string, string>)itemCodeImageUrlPair[icc];

                                                    if (search.Key == code && search.Value.Contains(_ixx))
                                                    {
                                                        imageUrl = search.Value;
                                                        //Console.WriteLine("imageUrl: " + imageUrl);
                                                        //Console.WriteLine("code: " + code);
                                                        break;
                                                    }
                                                }
                                                if(imageUrl != string.Empty)
                                                {
                                                    row += inIdx == 0 ? "('" + imageUrl + "" : "','" + imageUrl;
                                                }
                                                addedToRow = true;
                                                NoMssqlField = false;
                                            }
                                        }

                                        if (find_mssql_field == "sequenceNo")
                                        {
                                            row += inIdx == 0 ? "('" + sequence + "" : "','" + sequence;
                                            sequence++;
                                            addedToRow = true;
                                            NoMssqlField = false;
                                        }

                                        if (find_mssql_field == "currentDate")
                                        {
                                            DateTime _currentDate = DateTime.Now;
                                            string currentDate = Convert.ToDateTime(_currentDate).ToString("yyyy-MM-dd");
                                            //Console.WriteLine(currentDate);
                                            row += inIdx == 0 ? "('" + currentDate + "" : "','" + currentDate;
                                            addedToRow = true;
                                            NoMssqlField = false;
                                        }

                                        if (find_mssql_field == "defaultImage")
                                        {
                                            if(ixx == 1)
                                            {
                                                row += inIdx == 0 ? "('" + 1 + "" : "','" + 1;
                                            }
                                            else
                                            {
                                                row += inIdx == 0 ? "('" + 0 + "" : "','" + 0;
                                            }
                                            addedToRow = true;
                                            NoMssqlField = false;
                                        }

                                        if (find_mssql_field == "ItemCode")
                                        {
                                            string _productId = "0";

                                            if (string.IsNullOrEmpty(code) || !productList.TryGetValue(code, out _productId))
                                            {
                                                _productId = "0";
                                            }

                                            int.TryParse(_productId, out int productId);
                                            row += inIdx == 0 ? "('" + _productId + "" : "','" + _productId;

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

                                                //do looping for mysql field
                                                for (int isql = 0; isql < mysqlFieldList.Count; isql++)
                                                {
                                                    string eachField = mysqlFieldList[isql].ToString();

                                                    if (corr_mysql_field == eachField && LogicParser.IsCodeStr(find_mssql_field) == false)
                                                    {
                                                        if(!addedToRow)
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
                                                        row += inIdx == 0 ? "('" + tmp + "" : "','" + tmp;
                                                        addedToRow = true;
                                                    }
                                                }

                                                NoMssqlField = false;
                                            }
                                        });

                                        if (NoMssqlField)
                                        {
                                            if (!addedToRow)
                                            {
                                                string tmp = LogicParser.Parse(corr_mysql_field, find_mssql_field, map, nullfield)[corr_mysql_field];
                                                Database.Sanitize(ref tmp);
                                                row += inIdx == 0 ? "('" + tmp + "" : "','" + tmp;
                                            }
                                        }
                                    });
                                    row += "')";

                                    if(row.Contains("images"))
                                    {
                                        RecordCount++;
                                        valueString.Add(row);
                                    }
                                    
                                }

                                if (valueString.Count % 2000 == 0)
                                {
                                    string values = valueString.Join(",");

                                    insertQuery = insertQuery.ReplaceAll(values, "@values");

                                    mysql.Insert(insertQuery);

                                    insertQuery = insertQuery.ReplaceAll("@values", values);
                                    valueString.Clear();

                                    logger.message = string.Format("{0} image records is inserted into " + mysqlconfig.config_database, RecordCount);
                                    logger.Broadcast();
                                }
                            });

                            logger.Broadcast("Total images uploaded: " + itemCodeImageUrlPair.Count);

                            if (valueString.Count > 0)
                            {
                                string values = valueString.Join(",");

                                insertQuery = insertQuery.ReplaceAll(values, "@values");

                                mysql.Insert(insertQuery);

                                insertQuery = insertQuery.ReplaceAll("@values", values);
                                valueString.Clear();

                                logger.message = string.Format("{0} image records is inserted into " + mysqlconfig.config_database, RecordCount);
                                logger.Broadcast();
                            }

                            intoffset += intEachBatch;

                            string offsetToBeReplaced = "OFFSET " + offset;
                            database.Query = database.Query.SafeReplaceAll("OFFSET @offset", true, offsetToBeReplaced);

                            offset = intoffset.ToString();
                            eachBatch = intEachBatch.ToString();

                            logger.Broadcast(offset + "<=" + numOfRecords);
                            logger.Broadcast("If true, run another batch");

                            if (intoffset <= numOfRecords)
                            {
                                goto RUNANOTHERBATCH;
                            }
                            else
                            {
                                mysql.Insert("INSERT INTO cms_update_time(table_name, updated_at) VALUES ('cms_product_image', NOW()) ON DUPLICATE KEY UPDATE table_name = VALUES(table_name), updated_at = VALUES(updated_at)");
                            }
                        });
                        RecordCount = 0; /* reset count */
                    });

                    slog.action_identifier = Constants.Action_APSImageSync;
                    slog.action_details = Constants.Tbl_cms_product_image + Constants.Is_Finished + string.Format(" ({0}) records", RecordCount);
                    slog.action_failure = 0;
                    slog.action_failure_message = string.Empty;
                    slog.action_time = DateTime.Now.ToString();

                    DateTime endTime = DateTime.Now;
                    TimeSpan ts = endTime - startTime;
                    Console.WriteLine("Completed in: " + ts.TotalSeconds.ToString("F"));

                    LocalDB.InsertSyncLog(slog);

                    logger.message = "APS image sync finished in (" + ts.TotalSeconds.ToString("F") + " seconds)";
                    logger.Broadcast();

                });

                thread.Start();
                //thread.Join();

            }
            catch (ThreadAbortException e)
            {
                DpprException ex = new DpprException
                {
                    file_name = "JobAPSImageSync",
                    exception = e.Message,
                    time = DateTime.Now.ToString()
                };
                LocalDB.InsertException(ex);

                Console.WriteLine(Constants.Thread_Exception + e.Message);
            }
        }
    }
}
