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
using Newtonsoft.Json;
using RestSharp;
using System.Net;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System.Net.Http;

namespace EasySales.Job
{
    [DisallowConcurrentExecution]
    public class JobUJ : IJob
    {
        private IRestClient _client;
        private IRestClient vimigo_client;
        private const string DevApiUrl = "https://api.xilnex.com/logic/v2/";
        //private const string ApiUrl = "https://api.xilnex.com/logic/v2/";
        private const string ApiUrl = "https://api.xilnex.com/logic/v2/";
        private const string ApiUrl_Vimigo = "http://admin.vimigoapp.com/";

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
                    int RecordCountSales = 0;
                    GlobalLogger logger = new GlobalLogger();

                    /**
                     * Here we will run SQLAccounting Codes
                     * */

                    DpprSyncLog slog = new DpprSyncLog();
                    slog.action_identifier = "Collection_Sync";
                    slog.action_failure = 0;
                    slog.action_failure_message = string.Empty;
                    slog.action_time = DateTime.Now.ToString();

                    DateTime startTime = DateTime.Now;

                    LocalDB.InsertSyncLog(slog);
                    logger.message = "----------------------------------------------------------------------------";
                    logger.Broadcast();
                    logger.message = "Collection Integration sync is running";
                    logger.Broadcast();
                    logger.message = "----------------------------------------------------------------------------";
                    logger.Broadcast();

                    //var req = new RestRequest("payments/search");
                    List<DpprMySQLconfig> mysql_list = LocalDB.GetRemoteDatabaseConfig().Distinct().ToList();
                    DpprMySQLconfig mysql_config = mysql_list[0];
                    string compName = mysql_config.config_database;
                    if (compName != "easysale_uvjoy")
                    {
                        goto ENDJOB;
                    }

                    string query = "INSERT INTO cms_receipt_v2 (sales_id, receipt_id, receipt_code, cust_code, sales_created_date, receipt_date, receipt_amount, salesperson, received_by, payment_method, receipt_ref, cancelled) VALUES  ";
                    string updateQuery = " ON DUPLICATE KEY UPDATE receipt_id = VALUES(receipt_id),receipt_code = VALUES(receipt_code),cust_code = VALUES(cust_code), sales_created_date = VALUES(sales_created_date), receipt_date = VALUES(receipt_date), receipt_amount = VALUES(receipt_amount), salesperson = VALUES(salesperson), payment_method = VALUES(payment_method), receipt_ref = VALUES(receipt_ref), cancelled = VALUES(cancelled), received_by = VALUES(received_by);";

                    string querySales = "INSERT INTO cms_sales (sales_id, client_id, sales_created_date, grand_total, paid_amount, cancelled, salesperson, cashier, payment_status) VALUES  ";
                    string updateQuerySales = " ON DUPLICATE KEY UPDATE client_id = VALUES(client_id), sales_created_date = VALUES(sales_created_date), grand_total = VALUES(grand_total), paid_amount = VALUES(paid_amount), cancelled = VALUES(cancelled), salesperson = VALUES(salesperson), cashier = VALUES(cashier), payment_status = VALUES(payment_status);";

                    HashSet<string> queryList = new HashSet<string>();
                    HashSet<string> queryListSales = new HashSet<string>();
                    Database mysql = new Database();

                    dynamic jsonRule = new CheckBackendRule().CheckTablesExist().GetSettingByTableName("collection");

                    int lastDate = 31;
                    int startDate = 1;
                    string dateFrom = string.Empty;
                    string dateTo = string.Empty;
                    int fromYear = 2021;
                    int toYear = 2021;
                    int toMonth = 12;
                    int fromMonth = 1;

                    if (jsonRule.Count > 0)
                    {
                        foreach (var _setting in jsonRule)
                        {
                            dynamic setting = _setting.setting;

                            dynamic _fromYear = setting.fromYear;
                            if (_fromYear != 2021)
                            {
                                fromYear = _fromYear;
                            }

                            dynamic _toYear = setting.toYear;
                            if (toYear != 2021)
                            {
                                toYear = _toYear;
                            }

                            dynamic _toMonth = setting.toMonth;
                            if (_toMonth != 12)
                            {
                                toMonth = _toMonth;
                            }

                            dynamic _fromMonth = setting.fromMonth;
                            if (_fromMonth != 1)
                            {
                                fromMonth = _fromMonth;
                            }

                            dynamic _startDate = setting.startDate;
                            if (_startDate != 1)
                            {
                                startDate = _startDate;
                            }

                            dynamic _lastDate = setting.lastDate;
                            if (_lastDate != 31)
                            {
                                lastDate = _lastDate;
                            }

                            dynamic _dateFrom = setting.dateFrom;
                            if (_dateFrom != string.Empty)
                            {
                                dateFrom = _dateFrom;
                            } 
                            
                            dynamic _dateTo = setting.dateTo;
                            if (_dateTo != string.Empty)
                            {
                                dateTo = _dateTo;
                            }
                        }
                    }

                    string salespersonName = string.Empty;
                    string url = string.Empty;

                    //Exclude these 3 salespersons from salespersonList
                    //Chin Yi Wen are part time, as for Angel Cheong, and Chong Even //set login_status = 0
                    //30073 - Nurul Afiqah, 30084 - Kor Kim Siew, 30135 - Quek Xin Yi, 30139 - Cheryl Choo

                    ArrayList salespersonList = mysql.Select("SELECT login_id, name FROM cms_login WHERE login_status = 1 ORDER BY login_id DESC");

                    Console.WriteLine("==========MONTH==============");
                    //Console.WriteLine(month);
                    Console.WriteLine("==========MONTH==============");

                    for (int i = 0; i < salespersonList.Count; i++)
                    {
                        Console.WriteLine("i:" + i);
                        Dictionary<string, string> each = (Dictionary<string, string>)salespersonList[i];
                        salespersonName = each["name"].ToString();

                        var req = new RestRequest("sales/search");

                        /* UNIVERSALJOY TOKEN */
                        //appid: SWMFzd6dFFwD7l4quvcBnAgb2IhpEmAu
                        //token: v5_8CRRyJP + hdcKYOzsS1nJ3NpFYyg07alcmfcpAYGDfsw =
                        //auth: 5

                        req.AddHeader("AppId", "SWMFzd6dFFwD7l4quvcBnAgb2IhpEmAu");
                        req.AddHeader("Token", "v5_8CRRyJP+hdcKYOzsS1nJ3NpFYyg07alcmfcpAYGDfsw=");
                        req.AddHeader("Auth", "5");

                        DateTime from = new DateTime(fromYear, fromMonth, startDate, 0, 0, 0, DateTimeKind.Utc);
                        string _dateFrom = from.ToUniversalTime().ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'");
                        DateTime to = new DateTime(toYear, toMonth, lastDate, 23, 59, 59, DateTimeKind.Utc);
                        string _dateTo = to.ToUniversalTime().ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'");

                        logger.Broadcast("==========dateFrom==============");
                        logger.Broadcast("dateFrom: " + _dateFrom);
                        logger.Broadcast("dateTo: " + _dateTo);
                        logger.Broadcast("==========dateTo==============");

                        _dateTo = _dateTo.Replace("000Z", "999Z");
                        req.AddParameter("dateFrom", _dateFrom);
                        req.AddParameter("dateTo", _dateTo);
                        req.AddParameter("salesperson", salespersonName);
                        logger.Broadcast("==========salespersonName==============");
                        logger.Broadcast(salespersonName);
                        logger.Broadcast("==========salespersonName==============");


                        _client = new RestClient(ApiUrl);
                        var resp = _client.Get(req);

                        if (resp.StatusCode == HttpStatusCode.OK)
                        {
                            Console.WriteLine("Success");
                            var token = JToken.Parse(resp.Content);
                            dynamic json = JsonConvert.DeserializeObject(resp.Content);
                            System.Type jsontype = json.GetType();
                            Console.WriteLine("JSON Type: " + jsontype);

                            if (jsontype.ToString() == "Newtonsoft.Json.Linq.JObject")
                            {
                                List<object> parsedFields = new List<object>();
                                parsedFields.Add(json);
                                json = parsedFields;                                  //add JSON JObject to the List<>
                            }

                            foreach (var item in json)
                            {
                                dynamic data = item.data; //data -- > sales []-- > collections[]

                                if (data != null)
                                {
                                    dynamic sales = data.sales;

                                    if (sales != null)
                                    {
                                        foreach (var col in sales)
                                        {
                                            RecordCountSales++;
                                            DateTime created_date = col.dateTime;
                                            string sales_created_date = Convert.ToDateTime(created_date).ToString("yyyy-MM-dd HH:mm:ss");
                                            string deleted = col.status, id = col.id, salesperson = col.salesPerson;
                                            string sales_id = col.id,
                                            client_id = col.clientId,
                                            grand_total = col.grandTotal,
                                            paid_amount = col.paid,
                                            cancelled = col.status,
                                            salespersonSales = col.salesPerson,
                                            cashier = col.cashier,
                                            payment_status = col.paymentStatus;

                                            if (cancelled == null)
                                            {
                                                cancelled = "F";
                                            }
                                            else
                                            {
                                                Console.WriteLine("cancelled: " + cancelled);
                                                //string cancelBy = col.cancellationInfo.cancelBy;
                                            }

                                            Database.Sanitize(ref sales_id);
                                            Database.Sanitize(ref client_id);
                                            Database.Sanitize(ref grand_total);
                                            Database.Sanitize(ref paid_amount);
                                            Database.Sanitize(ref cancelled);
                                            Database.Sanitize(ref salesperson);
                                            Database.Sanitize(ref cashier);
                                            Database.Sanitize(ref payment_status);
                                            Database.Sanitize(ref sales_created_date);

                                            string ValuesSales = string.Format("('{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}','{8}')", sales_id, client_id, sales_created_date, grand_total, paid_amount, cancelled, salespersonSales, cashier, payment_status);

                                            queryListSales.Add(ValuesSales);

                                            if (col.collections != null)
                                            {
                                                foreach (var dtl in col.collections)
                                                {
                                                    RecordCount++;
                                                    string payment_id = dtl.id,
                                                    receipt_code = dtl.invoiceId,
                                                    //salesperson = salespersonName,
                                                    received_by = dtl.receivedBy,
                                                    cust_code = dtl.clientId,
                                                    receipt_amount = dtl.amount,
                                                    payment_method = dtl.method,
                                                    receipt_ref = dtl.salesOrderId;
                                                    //deleted = dtl.deleted;

                                                    DateTime dateeee = dtl.paymentDate;

                                                    if (deleted == "Cancelled")
                                                    {
                                                        deleted = "T";
                                                    }
                                                    else
                                                    {
                                                        deleted = "F";
                                                    }

                                                    string dateeeeee = Convert.ToDateTime(dateeee).ToString("yyyy-MM-dd HH:mm:ss");

                                                    Database.Sanitize(ref payment_id);
                                                    Database.Sanitize(ref salesperson);
                                                    Database.Sanitize(ref cust_code);
                                                    Database.Sanitize(ref receipt_amount);
                                                    Database.Sanitize(ref payment_method);
                                                    Database.Sanitize(ref receipt_ref);
                                                    Database.Sanitize(ref deleted);
                                                    Database.Sanitize(ref receipt_code);
                                                    Database.Sanitize(ref sales_created_date);
                                                    Database.Sanitize(ref dateeeeee);
                                                    Database.Sanitize(ref received_by);

                                                    string Values = string.Format("('{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}','{8}','{9}','{10}','{11}')", id, payment_id, receipt_code, cust_code, sales_created_date, dateeeeee, receipt_amount, salesperson, received_by, payment_method, receipt_ref, deleted);

                                                    queryList.Add(Values);
                                                }
                                            }
                                        }
                                    }
                                }
                            }

                            if (queryList.Count > 0)
                            {
                                string tmp_query = query;
                                tmp_query += string.Join(", ", queryList);
                                tmp_query += updateQuery;

                                mysql.Insert(tmp_query);
                                mysql.Message("Collection Query: " + tmp_query);

                                string info = salespersonName + ": " + queryList.Count + " records";
                                logger.message = info;
                                logger.Broadcast();

                                queryList.Clear();
                                tmp_query = string.Empty;

                                logger.message = string.Format("{0} collection records is inserted", RecordCount);
                                logger.Broadcast();
                            }

                            if (queryListSales.Count > 0)
                            {
                                string tmp_query = querySales;
                                tmp_query += string.Join(", ", queryListSales);
                                tmp_query += updateQuerySales;

                                mysql.Insert(tmp_query);
                                mysql.Message("Sales Query: " + tmp_query);

                                string info = salespersonName + ": " + queryListSales.Count + " sales records";
                                logger.message = info;
                                logger.Broadcast();

                                Console.WriteLine("=============RECORDS PER SALESPERSON==============");
                                Console.WriteLine(info);
                                Console.WriteLine(tmp_query);
                                Console.WriteLine("=============RECORDS PER SALESPERSON==============");

                                queryListSales.Clear();
                                tmp_query = string.Empty;

                                logger.message = string.Format("{0} collection records is inserted", RecordCountSales);
                                logger.Broadcast();
                            }
                        }
                        else
                        {
                            Console.WriteLine("Failed");
                            Console.WriteLine(resp.StatusCode);
                        }
                    }

                    logger.Broadcast("-------------------------------------------------------------");
                    logger.Broadcast("-------------------------------------------------------------");
                    logger.Broadcast("------------ POSTING TO VIMIGO -------------");
                    logger.Broadcast("-------------------------------------------------------------");
                    logger.Broadcast("-------------------------------------------------------------");

                    ArrayList isColumExists = mysql.Select("SHOW COLUMNS FROM cms_setting LIKE 'transaction_id';");

                    string _transactionId = string.Empty;
                    int transactionId = 0;
                    if (isColumExists.Count == 1)
                    {
                        Console.WriteLine("Column exists!");
                        ArrayList getLatestId = mysql.Select("SELECT transaction_id+1 AS transactionID FROM cms_setting");

                        if (getLatestId.Count > 0)
                        {
                            Dictionary<string, string> id = (Dictionary<string, string>)getLatestId[0];
                            _transactionId = id["transactionID"];
                            Console.WriteLine(_transactionId);
                            int.TryParse(_transactionId, out transactionId);
                            Console.WriteLine("transactionId cms_setting: " + transactionId);
                        }
                    }

                    string getCollection = string.Empty;
                    int salespersonId = 0;
                    int totalCollectionCount = 0;

                    Dictionary<string, string> agentList = new Dictionary<string, string>();
                    ArrayList salespersonFromDb = mysql.Select("SELECT login_id, name FROM cms_login where login_status = 1");

                    for (int i = 0; i < salespersonFromDb.Count; i++)
                    {
                        Dictionary<string, string> each = (Dictionary<string, string>)salespersonFromDb[i];
                        agentList.Add(each["name"], each["login_id"]);
                    }
                    salespersonFromDb.Clear();

                    int collectionCount = 0;
                        getCollection = "SELECT s.sales_id, s.salesperson, s.cancelled, a.login_id, SUM(c.`receipt_amount`) AS total_accumulated_sales, DATE_FORMAT(NOW(),'%d/%m/%Y %H:%s:%i') AS order_date_format FROM cms_sales AS s LEFT JOIN cms_receipt_v2 AS c ON s.sales_id = c.sales_id LEFT JOIN cms_login AS a ON a.name = s.salesperson WHERE s.salesperson IN(SELECT NAME FROM cms_login WHERE login_status = 1) AND s.`cancelled` = 'Completed' AND s.sales_created_date >= '" + dateFrom + "' AND s.sales_created_date <= '" + dateTo+"' AND c.receipt_amount IS NOT NULL GROUP BY s.salesperson";
                    mysql.Message("getCollection: " + getCollection);

                    ArrayList collectionList = mysql.Select(getCollection);
                    for(int ixx = 0; ixx < collectionList.Count; ixx++)
                    {
                        Dictionary<string, string> colObj = (Dictionary<string, string>)collectionList[ixx]; //only have 1 row as we group by salesperson ---> Vimigo will view the latest posted transactions as salespersons' collection

                        //string salesId = colObj["sales_id"];
                        //string receiptId = colObj["receipt_id"];
                        //string _uniqueKey = colObj["sales_id"] + colObj["receipt_id"];
                        double.TryParse(colObj["total_accumulated_sales"], out double _receipt_amount);
                        decimal receipt_amount = Convert.ToDecimal(_receipt_amount);
                        string salesperson = colObj["salesperson"];
                        dynamic receiptDate = colObj["order_date_format"];
                        receiptDate = Database.NativeDateTime(receiptDate);
                        receiptDate = Convert.ToDateTime(receiptDate).ToString("dd/MM/yyyy HH:mm:ss");//01/12/2020 13:45:29
                        receiptDate = Convert.ToDateTime(receiptDate).ToString("yyyy-MM-dd HH:mm:ss");//"2020-12-01 13:45:29"

                        string _salespersonId = colObj["login_id"];

                        Console.WriteLine("receiptDate: " + receiptDate);

                        var req = new RestRequest("api/v2/commissions/personal/amount/update");
                        req.AddHeader("Authorization-Key", "test");
                        req.AddHeader("Authorization", "Bearer eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiIsImp0aSI6ImQ1YzQ5ZTk4MmVmNmUxYjAxYWM1MzkzMzM5YWM3ZDBmMzA2YWQzZDM3YjIzZjU2YmNiMjkwOWEyOTEyOThlZDI0M2E2MzM4MGUxYjk2Mzk1In0.eyJhdWQiOiIzIiwianRpIjoiZDVjNDllOTgyZWY2ZTFiMDFhYzUzOTMzMzlhYzdkMGYzMDZhZDNkMzdiMjNmNTZiY2IyOTA5YTI5MTI5OGVkMjQzYTYzMzgwZTFiOTYzOTUiLCJpYXQiOjE2MDY4MDY1MzMsIm5iZiI6MTYwNjgwNjUzMywiZXhwIjoxNjM4MzQyNTMzLCJzdWIiOiIxOTc2OSIsInNjb3BlcyI6W119.mdg6a_Nwem4XNunhp_PcPxFMrPFDPvDWlRi5zb_knfkF9tcCXV4GoA6PfCbSEpTTHymbBZgjp6JkapAXm3Rulw8MIhK9O5mV4rNhFzHFuoNnZwlTd5J4xii1G0GCoStoWoBW2Q-st10PEtJheq9FNrwIDS5cjjqIoOzZC2lV-DKMC8kSh6Fv4VYMWuLTHv9yxVXoljI7-YfRKJtpIo1b_YV1fAOCpDdNWXf92SDZHqWjnsf646TJMKLVlnzjs92HnoL4SGaGHXxYvfpdy4I67PA0iT8n6vSMAUFGh5RgEDBnSTgrrl3VE1Zk-lq5jTIejuP_yq4ROhVDF_qfWCh_Sazz-nSG84DSYsZEtPNnnG1qXjfta_M4458wO3YBns-Nr_eoxve5AmnKISa4noV2BK8x4XiAWqIqjiK4aJklVsqtYZf9iE2HAXRTO5wMjpc65faDs0yxyOw_dFRdfqj099Vf56AWwIjzFa4QyJFMosfj70zJuPsjFcKfHV-OJIB8VEBUwld_P5ifYCCQhDIdIn1N0uuRirC4W6alOH6QPn-ZBFmQuZAkAXH4SSt3egZ1oo7WCa3_JmR_RxyLNCHsxVLuASLb2bQWQSGFY6c_7aHg_DPYu8ZqnBCSWkDGluPQg5vf4m7N51veaknXQAlxlC5942kp87sv5DveSaqEY9o");
                        req.AddHeader("Accept", "application/json");
                        req.AddHeader("Content-Type", "application/json");

                        req.AddJsonBody(new
                        {
                            sales_person_id = _salespersonId,
                            transaction_id = transactionId.ToString(),
                            tag = "1",
                            type = "MONTHLY_COLLECTION",
                            sales_date = receiptDate,
                            sales_amount = receipt_amount
                        });
                        vimigo_client = new RestClient(ApiUrl_Vimigo);
                        var resp = this.vimigo_client.Post(req);
                        if (resp.StatusCode == HttpStatusCode.OK)
                        {
                            totalCollectionCount++;
                            collectionCount++;
                            logger.Broadcast("[" + salesperson + "] Collection Figure: RM " + receipt_amount + "(" + transactionId + ")");
                            //UPDATE cms_receipt_v2 SET remark = 'Posted', transaction_id = '"+_uniqueKey+"' WHERE sales_id = " + salesId + " AND receipt_id = " + receiptId + ""
                            //string updateRemark = "UPDATE cms_receipt_v2 SET remark = 'Posted', transaction_id = '" + _uniqueKey + "' WHERE sales_id = " + salesId + " AND receipt_id = " + receiptId + "";
                            //"UPDATE cms_receipt_v2 SET remark = 'Posted' WHERE sales_id = " + salesId + " AND receipt_id = " + receiptId + "";
                            //mysql.Insert(updateRemark);
                        }
                        mysql.Insert("UPDATE cms_setting SET transaction_id = " + transactionId + "");
                        transactionId++;
                    }

                    logger.Broadcast("Total Collection posted to Vimigo: " + totalCollectionCount + " records");

                    ENDJOB:
                    slog.action_identifier = "Collection_Sync";
                    slog.action_failure = 0;
                    slog.action_failure_message = string.Empty;
                    slog.action_time = DateTime.Now.ToString();

                    DateTime endTime = DateTime.Now;
                    TimeSpan ts = endTime - startTime;
                    Console.WriteLine("Completed in: " + ts.TotalSeconds.ToString("F"));

                    LocalDB.InsertSyncLog(slog);

                    logger.message = "Collection sync finished in (" + ts.TotalSeconds.ToString("F") + " seconds)";
                    logger.Broadcast();
                });

                thread.Start();
            }
            catch (ThreadAbortException e)
            {
                DpprException ex = new DpprException
                {
                    file_name = "Collection Sync",
                    exception = e.Message,
                    time = DateTime.Now.ToString()
                };
                LocalDB.InsertException(ex);

                Console.WriteLine(Constants.Thread_Exception + e.Message);
            }
        }
    }
}


// string info = "[" + salespersonName + "] : " + collectionCount + " collection records posted to Vimigo";
//logger.message = info;
//logger.Broadcast();

//for (int index = 0; index < collectionList.Count; index++)
//{
//    Dictionary<string, string> colObj = (Dictionary<string, string>)collectionList[index];

//    string salesId = colObj["sales_id"];
//    string receiptId = colObj["receipt_id"];
//    string _uniqueKey = colObj["sales_id"] + colObj["receipt_id"];
//    double.TryParse(colObj["receipt_amount"], out double _receipt_amount);
//    decimal receipt_amount = Convert.ToDecimal(_receipt_amount);

//    dynamic receiptDate = colObj["order_date_format"];
//    receiptDate = Database.NativeDateTime(receiptDate);
//    receiptDate = Convert.ToDateTime(receiptDate).ToString("dd/MM/yyyy HH:mm:ss");//01/12/2020 13:45:29
//    receiptDate = Convert.ToDateTime(receiptDate).ToString("yyyy-MM-dd HH:mm:ss");//"2020-12-01 13:45:29"

//    //receiptDate.ToString("yyyy’-‘MM’-‘dd’T’HH’:’mm’:’ss");

//    Console.WriteLine("receiptDate: " + receiptDate);
//    //Convert.ToDateTime(receiptDate).ToString("yyyy-MM-dd");

//    var req = new RestRequest("api/v2/commissions/personal/amount/update");
//    req.AddHeader("Authorization-Key", "test");
//    req.AddHeader("Authorization", "Bearer eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiIsImp0aSI6ImQ1YzQ5ZTk4MmVmNmUxYjAxYWM1MzkzMzM5YWM3ZDBmMzA2YWQzZDM3YjIzZjU2YmNiMjkwOWEyOTEyOThlZDI0M2E2MzM4MGUxYjk2Mzk1In0.eyJhdWQiOiIzIiwianRpIjoiZDVjNDllOTgyZWY2ZTFiMDFhYzUzOTMzMzlhYzdkMGYzMDZhZDNkMzdiMjNmNTZiY2IyOTA5YTI5MTI5OGVkMjQzYTYzMzgwZTFiOTYzOTUiLCJpYXQiOjE2MDY4MDY1MzMsIm5iZiI6MTYwNjgwNjUzMywiZXhwIjoxNjM4MzQyNTMzLCJzdWIiOiIxOTc2OSIsInNjb3BlcyI6W119.mdg6a_Nwem4XNunhp_PcPxFMrPFDPvDWlRi5zb_knfkF9tcCXV4GoA6PfCbSEpTTHymbBZgjp6JkapAXm3Rulw8MIhK9O5mV4rNhFzHFuoNnZwlTd5J4xii1G0GCoStoWoBW2Q-st10PEtJheq9FNrwIDS5cjjqIoOzZC2lV-DKMC8kSh6Fv4VYMWuLTHv9yxVXoljI7-YfRKJtpIo1b_YV1fAOCpDdNWXf92SDZHqWjnsf646TJMKLVlnzjs92HnoL4SGaGHXxYvfpdy4I67PA0iT8n6vSMAUFGh5RgEDBnSTgrrl3VE1Zk-lq5jTIejuP_yq4ROhVDF_qfWCh_Sazz-nSG84DSYsZEtPNnnG1qXjfta_M4458wO3YBns-Nr_eoxve5AmnKISa4noV2BK8x4XiAWqIqjiK4aJklVsqtYZf9iE2HAXRTO5wMjpc65faDs0yxyOw_dFRdfqj099Vf56AWwIjzFa4QyJFMosfj70zJuPsjFcKfHV-OJIB8VEBUwld_P5ifYCCQhDIdIn1N0uuRirC4W6alOH6QPn-ZBFmQuZAkAXH4SSt3egZ1oo7WCa3_JmR_RxyLNCHsxVLuASLb2bQWQSGFY6c_7aHg_DPYu8ZqnBCSWkDGluPQg5vf4m7N51veaknXQAlxlC5942kp87sv5DveSaqEY9o");
//    req.AddHeader("Accept", "application/json");
//    req.AddHeader("Content-Type", "application/json");

//    req.AddJsonBody(new
//    {
//        sales_person_id = salespersonId.ToString(),
//        transaction_id = _uniqueKey,
//        tag = "1",
//        type = "MONTHLY_COLLECTION",
//        sales_date = receiptDate,
//        sales_amount = receipt_amount
//    });
//    vimigo_client = new RestClient(ApiUrl_Vimigo);
//    var resp = this.vimigo_client.Post(req); 
//    if (resp.StatusCode == HttpStatusCode.OK)
//    {
//        totalCollectionCount++;
//        collectionCount++;
//        //UPDATE cms_receipt_v2 SET remark = 'Posted', transaction_id = '"+_uniqueKey+"' WHERE sales_id = " + salesId + " AND receipt_id = " + receiptId + ""
//        string updateRemark = "UPDATE cms_receipt_v2 SET remark = 'Posted', transaction_id = '" + _uniqueKey + "' WHERE sales_id = " + salesId + " AND receipt_id = " + receiptId + "";
//        //"UPDATE cms_receipt_v2 SET remark = 'Posted' WHERE sales_id = " + salesId + " AND receipt_id = " + receiptId + "";
//        mysql.Insert(updateRemark);
//    }
//}


//for (int i = 0; i < salespersonList.Count; i++)
//{
//    Console.WriteLine("i:" + i);
//    Dictionary<string, string> each = (Dictionary<string, string>)salespersonList[i];
//    salespersonName = each["name"].ToString();

//    var req = new RestRequest("sales/search");

//    /* UNIVERSALJOY TOKEN */
//    //appid: SWMFzd6dFFwD7l4quvcBnAgb2IhpEmAu
//    //token: v5_8CRRyJP + hdcKYOzsS1nJ3NpFYyg07alcmfcpAYGDfsw =
//    //auth: 5

//    req.AddHeader("AppId", "SWMFzd6dFFwD7l4quvcBnAgb2IhpEmAu");
//    req.AddHeader("Token", "v5_8CRRyJP+hdcKYOzsS1nJ3NpFYyg07alcmfcpAYGDfsw=");
//    req.AddHeader("Auth", "5");
//    //data-- > sales-- > collections
//    DateTime from = new DateTime(2020, 8, 1, 0, 0, 0,
//                         DateTimeKind.Utc);
//    string dateFrom = from.ToUniversalTime().ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'");
//    //Console.WriteLine("{0} ({1}) --> {0:O}", from, from.Kind);
//    Console.WriteLine("from: " + dateFrom);
//    DateTime to = new DateTime(2020, 8, 31, 23, 59, 59,
//                     DateTimeKind.Utc);
//    string dateTo = to.ToUniversalTime().ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'");
//    dateTo = dateTo.Replace("000Z", "999Z");
//    req.AddParameter("dateFrom", dateFrom);
//    req.AddParameter("dateTo", dateTo);
//    req.AddParameter("salesperson", salespersonName);

//    _client = new RestClient(ApiUrl);
//    var resp = _client.Get(req);

//    if (resp.StatusCode == HttpStatusCode.OK)
//    {
//        Console.WriteLine("Success");
//        var token = JToken.Parse(resp.Content);
//        dynamic json = JsonConvert.DeserializeObject(resp.Content);
//        System.Type jsontype = json.GetType();
//        Console.WriteLine("JSON Type: " + jsontype);

//        if (jsontype.ToString() == "Newtonsoft.Json.Linq.JObject")
//        {
//            List<object> parsedFields = new List<object>();
//            parsedFields.Add(json);
//            json = parsedFields;                                  //add JSON JObject to the List<>
//        }

//        foreach (var item in json)
//        {
//            dynamic data = item.data; //data -- > sales []-- > collections[]

//            if (data != null)
//            {
//                dynamic sales = data.sales;

//                if (sales != null)
//                {
//                    foreach (var col in sales)
//                    {
//                        //RecordCount++;
//                        DateTime created_date = col.dateTime;
//                        string sales_created_date = Convert.ToDateTime(created_date).ToString("yyyy-MM-dd HH:mm:ss");
//                        string deleted = col.status, id = col.id, salesperson = col.salesPerson;
//                        //string sales_id = col.id, 
//                        //client_id = col.clientId, 
//                        //grand_total = col.grandTotal,
//                        //paid_amount = col.paid,
//                        //cancelled = col.status,
//                        //salesperson = col.salesPerson,
//                        //cashier = col.cashier,
//                        //payment_status = col.paymentStatus;

//                        //if(cancelled == null)
//                        //{
//                        //    cancelled = "F";
//                        //}
//                        //else
//                        //{
//                        //    Console.WriteLine("cancelled: " + cancelled);
//                        //    //string cancelBy = col.cancellationInfo.cancelBy;
//                        //}

//                        //Database.Sanitize(ref sales_id);
//                        //Database.Sanitize(ref client_id);
//                        //Database.Sanitize(ref grand_total);
//                        //Database.Sanitize(ref paid_amount);
//                        //Database.Sanitize(ref cancelled);
//                        //Database.Sanitize(ref salesperson);
//                        //Database.Sanitize(ref cashier);
//                        //Database.Sanitize(ref payment_status);
//                        //Database.Sanitize(ref sales_created_date);

//                        //string Values = string.Format("('{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}','{8}')", sales_id, client_id, sales_created_date, grand_total, paid_amount, cancelled, salesperson, cashier, payment_status);

//                        //queryList.Add(Values);

//                        if (col.collections != null)
//                        {
//                            foreach (var dtl in col.collections)
//                            {
//                                RecordCount++;
//                                string payment_id = dtl.id,
//                                receipt_code = dtl.invoiceId,
//                                //salesperson = salespersonName,
//                                received_by = dtl.receivedBy,
//                                cust_code = dtl.clientId,
//                                receipt_amount = dtl.amount,
//                                payment_method = dtl.method,
//                                receipt_ref = dtl.salesOrderId;
//                                //deleted = dtl.deleted;

//                                DateTime dateeee = dtl.paymentDate;

//                                //if (deleted == "False")
//                                //{
//                                //    deleted = "F";
//                                //}
//                                //else
//                                //{
//                                //    deleted = "T";
//                                //}

//                                if (deleted == "Cancelled")
//                                {
//                                    deleted = "T";
//                                }
//                                else
//                                {
//                                    deleted = "F";
//                                }

//                                //if (receipt_code == "") //if empty take salesOrderId
//                                //{
//                                //    receipt_code = dtl.salesOrderId;
//                                //}

//                                string dateeeeee = Convert.ToDateTime(dateeee).ToString("yyyy-MM-dd HH:mm:ss");

//                                Database.Sanitize(ref payment_id);
//                                Database.Sanitize(ref salesperson);
//                                Database.Sanitize(ref cust_code);
//                                Database.Sanitize(ref receipt_amount);
//                                Database.Sanitize(ref payment_method);
//                                Database.Sanitize(ref receipt_ref);
//                                Database.Sanitize(ref deleted);
//                                Database.Sanitize(ref receipt_code);
//                                Database.Sanitize(ref sales_created_date);
//                                Database.Sanitize(ref dateeeeee);
//                                Database.Sanitize(ref received_by);

//                                string Values = string.Format("('{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}','{8}','{9}','{10}','{11}')", id, payment_id, receipt_code, cust_code, sales_created_date, dateeeeee, receipt_amount, salesperson, received_by, payment_method, receipt_ref, deleted);

//                                queryList.Add(Values);
//                            }
//                        }
//                    }
//                }
//            }
//        }

//        if (queryList.Count > 0)
//        {
//            string tmp_query = query;
//            tmp_query += string.Join(", ", queryList);
//            tmp_query += updateQuery;

//            mysql.Insert(tmp_query);

//            string info = salespersonName + ": " + queryList.Count + " records";
//            logger.message = info;
//            logger.Broadcast();

//            Console.WriteLine(info);
//            Console.WriteLine(tmp_query);

//            queryList.Clear();
//            tmp_query = string.Empty;

//            logger.message = string.Format("{0} collection records is inserted", RecordCount);
//            logger.Broadcast();
//        }
//    }
//    else
//    {
//        Console.WriteLine("Failed");
//        Console.WriteLine(resp.StatusCode);
//    }
//}

//"id": 660212,
//"clientId": "660153",
//"invoiceId": "660592",
//"amount": 106.8000,
//"method": "Cash",
//"xCard": null,
//"reference": null,
//"outlet": "NT - PEN - Head Office",
//"paymentDate": "2020-03-20T16:36:29.000Z",
//"isVoid": false,
//"creditCardRate": 0,
//"siteId": 660,
//"cardAppCode": null,
//"cardType": null,
//"status": "Saved",
//"receivedBy": "janice@agift.my",
//"cardExpiry": null,
//"traceNumber": null,
//"remark": "",
//"tenderAmount": 106.8000,
//"change": 0.0000,
//"declarationSessionId": 6601,
//"eodLogId": 6601,
//"isDeposit": false,
//"salesOrderId": "",
//"cardType2": null,
//"cardType3": null,
//"businessDate": "2018-12-04T00:00:00.000Z",
//"internalReferenceId": null,
//"availableBalance": 0.0000,
//"usedDate": null,
//"prepaidCardNumber": null,
//"prepaidReferenceNumber": null,
//"exchangeRate": 1.0000,
//"currencyCode": "",
//"foreignAmount": 106.8000,
//"foreignGain": null,
//"cardLookup": null,
//"receivedByCashierName": "Z - Janice Ooi",
//"deleted": false