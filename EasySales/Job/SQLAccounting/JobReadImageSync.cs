using EasySales.Model;
using EasySales.Object;
using Quartz;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EasySales.Job
{
    [DisallowConcurrentExecution]
    public class JobReadImageSync : IJob
    {
        public Bitmap ByteToImage(byte[] blob)
        {
            MemoryStream mStream = new MemoryStream();
            byte[] pData = blob;
            mStream.Write(pData, 0, Convert.ToInt32(pData.Length));
            Bitmap bm = new Bitmap(mStream, false);
            mStream.Dispose();
            return bm;
        }

        public string QuotedStr(string str)
        {
            return str.Replace("'", "''");
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

                    GlobalLogger logger = new GlobalLogger();

                    /**
                    * Here we will run SQLAccounting Codes
                    * */

                    DpprSyncLog slog = new DpprSyncLog();
                    slog.action_identifier = Constants.Action_ReadImageSync;
                    slog.action_details = Constants.Tbl_cms_product_image + Constants.Is_Starting;
                    slog.action_failure = 0;
                    slog.action_failure_message = string.Empty;
                    slog.action_time = DateTime.Now.ToString();

                    DateTime startTime = DateTime.Now;

                    LocalDB.InsertSyncLog(slog);
                    logger.message = "----------------------------------------------------------------------------";
                    logger.Broadcast();
                    logger.message = "Read Image sync is running";
                    logger.Broadcast();
                    logger.message = "----------------------------------------------------------------------------";
                    logger.Broadcast();

                    Database mysql = new Database();

                    List<DpprFTPServerConfig> list = LocalDB.GetFTPServerConfig();
                    DpprFTPServerConfig config = list[0];

                CHECKAGAIN:

                    SQLAccApi instance = SQLAccApi.getInstance();

                    dynamic ComServer = instance.GetComServer();

                    if (!instance.RPCAvailable())
                    {
                        goto CHECKAGAIN;
                    }

                    dynamic lSQL, lDataSet;
                    string query, updateQuery;

                    query = "INSERT INTO cms_product_image(product_id, image_url, sequence_no, product_default_image, active_status, product_image_created_date) VALUES ";
                    updateQuery = " ON DUPLICATE KEY UPDATE product_id = VALUES(product_id), image_url = VALUES(image_url), sequence_no = VALUES(sequence_no), product_default_image = VALUES(product_default_image), active_status = VALUES(active_status);";

                    ArrayList queryList = new ArrayList();

                    ArrayList itemWithImage = new ArrayList();

                    Dictionary<string, string> productList = new Dictionary<string, string>();
                    ArrayList activeProducts = mysql.Select("SELECT * FROM cms_product WHERE product_status = 1;");

                    List<HashSet<string>> chunkArrays = new List<HashSet<string>>();
                    HashSet<string> tmp = new HashSet<string>();

                    for (int i = 0; i < activeProducts.Count; i++)
                    {
                        Dictionary<string, string> each = (Dictionary<string, string>)activeProducts[i];
                        tmp.Add(QuotedStr(each["product_code"].ToString()));

                        if (tmp.Count % 500 == 0)
                        {
                            chunkArrays.Add(tmp);
                            tmp = new HashSet<string>();
                        }
                    }

                    if (tmp.Count > 0)
                    {
                        chunkArrays.Add(tmp);
                    }

                    int RecordCount = 0;
                    int codeIndex = -1;

                    ArrayList pictureList = new ArrayList();
                    ArrayList codeList = new ArrayList();

                    string Values = string.Empty;
                    string encoded = string.Empty;

                    for (int i = 0; i < activeProducts.Count; i++)
                    {
                        Dictionary<string, string> each = (Dictionary<string, string>)activeProducts[i];
                        productList.Add(each["product_code"], each["product_id"]);
                    }
                    activeProducts.Clear();

                    for (int idx = 0; idx < chunkArrays.Count; idx++)
                    {
                        HashSet<string> productCodeList = (HashSet<string>)chunkArrays[idx];
                        string whereInCode = "'" + string.Join("','", productCodeList) + "'";

                        lSQL = "SELECT Code, Picture FROM ST_ITEM WHERE Code IN (" + whereInCode + ") AND octet_length(Picture)>10 AND Picture IS NOT NULL";
                        mysql.Message("lSQL: " + lSQL);
                        lDataSet = ComServer.DBManager.NewDataSet(lSQL);

                        lDataSet.First();

                        //Console.WriteLine("Batch No ========>" + idx + "-------> size=======>" + productCodeList.Count);
                        mysql.Message("Batch No ========>" + idx + "-------> size=======>" + productCodeList.Count);
                        logger.Broadcast("Batch No ========>" + idx + "-------> size=======>" + productCodeList.Count);

                        //for (int i = 0; i < activeProducts.Count; i++)
                        //{
                        //    Dictionary<string, string> each = (Dictionary<string, string>)activeProducts[i];
                        //    productList.Add(each["product_code"], each["product_id"]);
                        //}
                        //activeProducts.Clear();

                        int xj = 0;
                        try
                        {
                            while (!lDataSet.eof)
                            {
                                RecordCount++;
                                codeIndex++;

                                dynamic Code = lDataSet.FindField("CODE").AsString;
                                byte[] _picture = lDataSet.FindField("Picture").Value;

                                byte[] encodedCode = new UTF8Encoding().GetBytes(Code);

                                byte[] hashCode = ((HashAlgorithm)CryptoConfig.CreateFromName("MD5")).ComputeHash(encodedCode);

                                encoded = BitConverter.ToString(hashCode)
                                .Replace("-", string.Empty)
                                .ToLower();

                                Type unknown = _picture.GetType();
                                if (unknown.Name != "Byte[]")
                                {
                                    lDataSet.Next();
                                    continue;
                                }

                                try
                                {
                                    //Bitmap Picture = ByteToImage(_picture);
                                    Bitmap _Picture = new Bitmap(ByteToImage(_picture));
                                    Bitmap Picture = (Bitmap)_Picture.Clone();
                                    _Picture.Dispose();
                                    if (Picture != null)
                                    {
                                        xj++;
                                        Dictionary<string, string> hashCodePair = new Dictionary<string, string>();
                                        hashCodePair.Add(encoded, Code);
                                        codeList.Add(hashCodePair);

                                        pictureList.Add(Picture);

                                        string _productId = "0";

                                        if (!productList.TryGetValue(Code, out _productId))
                                        {
                                            _productId = "0";
                                        }
                                        int.TryParse(_productId, out int ProductId);

                                        int defaultImage = 1;
                                        int activeStatus = 1;
                                        string imageURL;

                                        Dictionary<string, string> pair = (Dictionary<string, string>)codeList[codeIndex];
                                        //logger.Broadcast("codeList: " + codeList.Count);
                                        //logger.Broadcast("codeIndex: " + codeIndex);
                                        logger.Broadcast("Code: " + pair.ElementAt(0).Key);
                                        //mysql.Message("codeList: " + codeList.Count);
                                        //mysql.Message("codeIndex: " + codeIndex);
                                        mysql.Message("Code: " + pair.ElementAt(0).Key);
                                        /* https://easysales.asia/wls/easysales/cms/images/product_img/img_102.png */
                                        imageURL = "https://easysales.asia/" + config.company_name + "/easysales/cms/images/product_img/" + pair.ElementAt(0).Key + ".png";

                                        Database.Sanitize(ref imageURL);

                                        Values = string.Format("('{0}','{1}','{2}','{3}','{4}','{5}')", ProductId, imageURL, RecordCount, defaultImage, activeStatus, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                                        mysql.Message(Values);

                                        Dictionary<string, string> codeQueryPair = new Dictionary<string, string>();
                                        codeQueryPair.Add(Code, Values);
                                        queryList.Add(codeQueryPair);

                                        if(pictureList.Count % 100 == 0)
                                        {
                                            FTPServer ftp = new FTPServer();
                                            //FTPServer ftp = new FTPServer("easysales.asia", "staging@easysales.asia", "staging123@"); //later delete
                                            logger.Broadcast("Uploading the images...");
                                            logger.Broadcast("pictureList.Count: " + pictureList.Count);
                                            logger.Broadcast("codeList.Count: " + codeList.Count);

                                            ArrayList uploaded = ftp.uploadImage(pictureList, codeList);

                                            logger.message = string.Format("{0} images is uploaded to FTP Server", uploaded.Count);
                                            logger.Broadcast();

                                            HashSet<string> successImages = new HashSet<string>();
                                            for (int i = 0; i < uploaded.Count; i++)
                                            {
                                                string _encodedCode = uploaded[i].ToString();
                                                for (int j = 0; j < codeList.Count; j++)
                                                {
                                                    Dictionary<string, string> _hashCodePair = (Dictionary<string, string>)codeList[j];
                                                    if (_hashCodePair.ContainsKey(_encodedCode))
                                                    {
                                                        string code = _hashCodePair[_encodedCode];
                                                        if (code != string.Empty)
                                                        {
                                                            for (int k = 0; k < queryList.Count; k++)
                                                            {
                                                                Dictionary<string, string> queryCodePair = (Dictionary<string, string>)queryList[k];
                                                                if (queryCodePair.ContainsKey(code))
                                                                {
                                                                    string queryImage = queryCodePair[code];
                                                                    successImages.Add(queryImage);
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                            }

                                            pictureList.Clear();
                                            queryList.Clear();
                                            codeList.Clear();
                                            codeIndex = -1; //reset

                                            if (successImages.Count > 0)
                                            {
                                                //query = query + string.Join(", ", successImages) + updateQuery;
                                                string tmp_query = query;
                                                tmp_query += string.Join(", ", successImages);
                                                tmp_query += updateQuery;

                                                mysql.Insert(tmp_query);
                                                mysql.Message("Image Query: " + tmp_query);

                                                logger.message = string.Format("{0} image records is inserted", successImages.Count);
                                                logger.Broadcast();

                                                successImages.Clear();
                                            }
                                        }
                                    }
                                }
                                catch (Exception exx)
                                {
                                    logger.Broadcast("Bitmap Picture: " + exx.Message);
                                }

                                lDataSet.Next();
                            }
                        }
                        catch (Exception ex)
                        {
                            logger.Broadcast("Exception Msg: " + ex.Message);
                        }
                    }

                    if(pictureList.Count > 0)
                    {
                        FTPServer ftp = new FTPServer();
                        logger.Broadcast("Uploading the images...");
                        logger.Broadcast("pictureList.Count: " + pictureList.Count);
                        logger.Broadcast("codeList.Count: " + codeList.Count);

                        ArrayList uploaded = ftp.uploadImage(pictureList, codeList);

                        logger.message = string.Format("{0} images is uploaded to FTP Server", uploaded.Count);
                        logger.Broadcast();

                        HashSet<string> successImages = new HashSet<string>();
                        for (int i = 0; i < uploaded.Count; i++)
                        {
                            string encodedCode = uploaded[i].ToString();
                            for (int j = 0; j < codeList.Count; j++)
                            {
                                Dictionary<string, string> hashCodePair = (Dictionary<string, string>)codeList[j];
                                if (hashCodePair.ContainsKey(encodedCode))
                                {
                                    string code = hashCodePair[encodedCode];
                                    if (code != string.Empty)
                                    {
                                        for (int k = 0; k < queryList.Count; k++)
                                        {
                                            Dictionary<string, string> queryCodePair = (Dictionary<string, string>)queryList[k];
                                            if (queryCodePair.ContainsKey(code))
                                            {
                                                string queryImage = queryCodePair[code];
                                                successImages.Add(queryImage);
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        if (successImages.Count > 0)
                        {
                            query = query + string.Join(", ", successImages) + updateQuery;

                            mysql.Insert(query);
                            mysql.Message("Image Query: " + query);

                            logger.message = string.Format("{0} image records is inserted", successImages.Count);
                            logger.Broadcast();
                        }
                    }

                    //FTPServer ftp = new FTPServer();
                    ////FTPServer ftp = new FTPServer("easysales.asia", "staging@easysales.asia", "staging123@"); //later delete
                    //logger.Broadcast("Uploading the images...");
                    //logger.Broadcast("pictureList.Count: " + pictureList.Count);
                    //logger.Broadcast("codeList.Count: " + codeList.Count);

                    //ArrayList uploaded = ftp.uploadImage(pictureList, codeList);

                    //logger.message = string.Format("{0} images is uploaded to FTP Server", uploaded.Count);
                    //logger.Broadcast();

                    //HashSet<string> successImages = new HashSet<string>();
                    //for (int i = 0; i < uploaded.Count; i++)
                    //{
                    //    string encodedCode = uploaded[i].ToString();
                    //    for (int j = 0; j < codeList.Count; j++)
                    //    {
                    //        Dictionary<string, string> hashCodePair = (Dictionary<string, string>)codeList[j];
                    //        if (hashCodePair.ContainsKey(encodedCode))
                    //        {
                    //            string code = hashCodePair[encodedCode];
                    //            if (code != string.Empty)
                    //            {
                    //                for (int k = 0; k < queryList.Count; k++)
                    //                {
                    //                    Dictionary<string, string> queryCodePair = (Dictionary<string, string>)queryList[k];
                    //                    if (queryCodePair.ContainsKey(code))
                    //                    {
                    //                        string queryImage = queryCodePair[code];
                    //                        successImages.Add(queryImage);
                    //                    }
                    //                }
                    //            }
                    //        }
                    //    }
                    //}

                    tmp.Clear();
                    activeProducts.Clear();
                    /* 
                     * if (queryList.Count % 2000 == 0)
                      {
                            string tmp_query = query;
                            tmp_query += string.Join(", ", queryList);
                            tmp_query += updateQuery;

                            mysql.Insert(tmp_query);
                            mysql.Close();

                            queryList.Clear();
                            tmp_query = string.Empty;

                            logger.message = string.Format("{0} image records is inserted", RecordCount);
                            logger.Broadcast();
                     } */

                    //if (successImages.Count > 0)
                    //{
                    //    query = query + string.Join(", ", successImages) + updateQuery;

                    //    mysql.Insert(query);

                    //    logger.message = string.Format("{0} image records is inserted", successImages.Count);
                    //    logger.Broadcast();
                    //}

                    slog.action_identifier = Constants.Action_ReadImageSync;
                    slog.action_details = Constants.Tbl_cms_product_image + Constants.Is_Finished + string.Format(" ({0}) records", RecordCount);
                    slog.action_failure = 0;
                    slog.action_failure_message = string.Empty;
                    slog.action_time = DateTime.Now.ToString();

                    DateTime endTime = DateTime.Now;
                    TimeSpan ts = endTime - startTime;
                    Console.WriteLine("Completed in: " + ts.TotalSeconds.ToString("F"));

                    LocalDB.InsertSyncLog(slog);

                    logger.message = "Read Image sync finished in (" + ts.TotalSeconds.ToString("F") + " seconds)";
                    logger.Broadcast();
                });

                thread.Start();
                //thread.Join();
            }
            catch (ThreadAbortException e)
            {
                DpprException ex = new DpprException
                {
                    file_name = "JobReadImageSync",
                    exception = e.Message,
                    time = DateTime.Now.ToString()
                };
                LocalDB.InsertException(ex);

                Console.WriteLine(Constants.Thread_Exception + e.Message);
            }
        }
    }
}