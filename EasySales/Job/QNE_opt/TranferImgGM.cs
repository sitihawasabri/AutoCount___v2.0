using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EasySales.Model;
using EasySales.Object;
using MySql.Data.MySqlClient;
using Quartz;
using Quartz.Impl;
using Quartz.Logging;
using RestSharp;

namespace EasySales.Job
{
    [DisallowConcurrentExecution]
    public class TranferImgGM : IJob
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

                    GlobalLogger logger = new GlobalLogger();

                    DpprSyncLog slog = new DpprSyncLog();
                    slog.action_identifier = Constants.Action_StockCategoriesSync;
                    slog.action_details = Constants.Tbl_cms_product_category + Constants.Is_Starting;
                    slog.action_failure = 0;
                    slog.action_failure_message = string.Empty;
                    slog.action_time = DateTime.Now.ToString();

                    LocalDB.InsertSyncLog(slog);

                    logger.message = "Img transfer sync is running";
                    logger.Broadcast();

                    HashSet<string> queryList = new HashSet<string>();
                    int RecordCount = 0;
                    string query = "INSERT INTO cms_product_image(product_id, image_url, sequence_no, product_default_image, active_status, product_image_created_date) VALUES ";

                    string updateQuery = " ON DUPLICATE KEY UPDATE image_url = VALUES(image_url),product_default_image = VALUES(product_default_image),active_status = VALUES(active_status),product_image_created_date = VALUES(product_image_created_date);";

                    Database mysqlHQ = new Database();
                    mysqlHQ.Connect(0);

                    List<DpprMySQLconfig> mysql_list = LocalDB.GetRemoteDatabaseConfig().Distinct().ToList();
                    DpprMySQLconfig mysql_configHQ = mysql_list[0];
                    string dbNameHQ = mysql_configHQ.config_database;
                    DpprMySQLconfig mysql_configJOHOR = mysql_list[1];
                    string dbNameJOHOR = mysql_configJOHOR.config_database;
                    if (dbNameJOHOR == "easysale_gmcommunication_jb")
                    {
                        Console.WriteLine(dbNameJOHOR);
                    }

                    if (dbNameHQ == "easysale_gmcommunication")
                    {
                        //ArrayList imgFromHQ = mysqlHQ.Select("SELECT p.product_id, p.product_code, img.image_url, img.product_default_image FROM cms_product_image AS img LEFT JOIN cms_product p ON img.product_id = p.product_id");
                        Dictionary<string, string> cms_updated_time = mysqlHQ.GetImageUpdatedTime();//{[updated_at, 28/06/2020 5:38:23 PM]}
                        string updated_at = string.Empty;

                        if (cms_updated_time.Count > 0)
                        {
                            updated_at = cms_updated_time["updated_at"].ToString().MSSQLdate();
                        }

                        string getImageFromHQ = "SELECT p.product_id, p.product_code, img.image_url, img.product_default_image, img.updated_at FROM cms_product_image AS img LEFT JOIN cms_product p ON img.product_id = p.product_id ";
                        if (cms_updated_time.Count > 0)
                        {
                            updated_at = cms_updated_time["updated_at"].ToString().MSSQLdate();
                            getImageFromHQ += " WHERE img.updated_at >= '" + updated_at + "'";
                        }

                        ArrayList imgFromHQ = mysqlHQ.Select(getImageFromHQ);
                        var codeImgHQPair = new List<KeyValuePair<string, string>>();
                        Dictionary<string, string> defaultImgList = new Dictionary<string, string>();

                        for (int i = 0; i < imgFromHQ.Count; i++)
                        {
                            Dictionary<string, string> each = (Dictionary<string, string>)imgFromHQ[i];
                            codeImgHQPair.Add(new KeyValuePair<string, string>(each["product_code"], each["image_url"]));
                            defaultImgList.Add(each["image_url"], each["product_default_image"]);
                        }

                        Database mysqlJB = new Database();
                        mysqlJB.Connect(1);
                        //string connectionString = "Server=easysales.asia; database=easysale_gmcommunication_jb; UID=easysale_gm; password=gmcommunication123@; MinimumPoolSize=5";
                        ArrayList prodCodeIdPair = mysqlJB.Select("SELECT product_id, product_code FROM cms_product;");
                        Dictionary<string, string> prodCodeIdListJB = new Dictionary<string, string>();

                        for (int i = 0; i < prodCodeIdPair.Count; i++)
                        {
                            Dictionary<string, string> each = (Dictionary<string, string>)prodCodeIdPair[i];
                            prodCodeIdListJB.Add(each["product_code"], each["product_id"]);
                        }

                        var prodIdImgUrlJB = new List<KeyValuePair<string, string>>();

                        for (int ixx = 0; ixx < codeImgHQPair.Count; ixx++)
                        {
                            string productCodeHQ = string.Empty;
                            string imgHQ = string.Empty;

                            productCodeHQ = codeImgHQPair.ElementAt(ixx).Key.ToString();
                            imgHQ = codeImgHQPair.ElementAt(ixx).Value.ToString();
                            if (prodCodeIdListJB.ContainsKey(productCodeHQ))
                            {
                                RecordCount++;

                                string defImg, activeStatus;
                                prodCodeIdListJB.TryGetValue(productCodeHQ, out string productIdJB); //the same product code as HQ, we get the product ID of JB
                                defaultImgList.TryGetValue(imgHQ, out defImg);
                                activeStatus = "1";
                                string Values = string.Format("('{0}','{1}','{2}','{3}','{4}',{5})", productIdJB, imgHQ, RecordCount, defImg, activeStatus, "NOW()");

                                queryList.Add(Values);

                                if (queryList.Count % 2000 == 0)
                                {
                                    string tmp_query = query;
                                    tmp_query += string.Join(", ", queryList);
                                    tmp_query += updateQuery;

                                    Console.WriteLine(tmp_query);
                                    if (dbNameJOHOR == "easysale_gmcommunication_jb")
                                    {
                                        logger.Broadcast("Migrating image to GM JOHOR...");
                                        mysqlJB.Insert(tmp_query);
                                        logger.message = string.Format("{0}  records is inserted", RecordCount);
                                        logger.Broadcast();
                                    }

                                    queryList.Clear();
                                    tmp_query = string.Empty;
                                }
                            }
                        }

                        if (queryList.Count > 0)
                        {
                            query = query + string.Join(", ", queryList) + updateQuery;

                            Console.WriteLine(query);
                            if (dbNameJOHOR == "easysale_gmcommunication_jb")
                            {
                                logger.Broadcast("Migrating image to GM JOHOR...");
                                mysqlJB.Insert(query);
                                logger.message = string.Format("{0} records is inserted", RecordCount);
                                logger.Broadcast();
                            }
                        }
                    }

                    slog.action_identifier = Constants.Action_ReadImageSync;
                    slog.action_details = Constants.Tbl_cms_product_image + Constants.Is_Finished + string.Format(" ({0}) records", RecordCount);
                    slog.action_failure = 0;
                    slog.action_failure_message = string.Empty;
                    slog.action_time = DateTime.Now.ToString();

                    LocalDB.InsertSyncLog(slog);

                    logger.message = "Image sync is finished";
                    logger.Broadcast();
                });

                thread.Start();
            }
            catch (ThreadAbortException e)
            {
                DpprException ex = new DpprException
                {
                    file_name = "JobStockCategoriesSync",
                    exception = e.Message,
                    time = DateTime.Now.ToString()
                };
                LocalDB.InsertException(ex);

                Console.WriteLine(Constants.Thread_Exception + e.Message);
            }
        }
    }
}