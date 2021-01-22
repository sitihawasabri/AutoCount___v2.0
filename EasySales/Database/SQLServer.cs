using EasySales.Model;
using EasySales.Object;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;

namespace EasySales
{
    public class SQLServer
    {
        private SqlConnection connection = null;
        private string connectionString = null;
        private bool isInitialized = false;
        private bool checkServer = true;

        public SQLServer()
        {
            //this.connection = this.Connect(0);
        }

        public SqlConnection Instance()
        {
            return this.connection;
        }

        public SqlConnection Connect(string dbname)
        {
            if (connection == null)
            {
                Message("Trying to establish connection to MS SQL");
                try
                {
                    checkServer = InternetConnection.PingMSSQL();
                    if (!checkServer)
                    {
                        Message("Cannot connect to server at the moment", true);
                        goto ENDJOB;
                    }

                    bool fail = true;
                    List<DpprSQLServerconfig> list = LocalDB.GetRemoteSQLServerConfig();
                    for(int i = 0; i < list.Count; i++)
                    {
                        DpprSQLServerconfig config = list[i];
                        if(config.database_name == dbname)
                        {
                            string connectionString = string.Format("Data Source={0};Initial Catalog={1}; User ID={2}; Password={3};", config.data_source, config.database_name, config.user_id, config.password);
                            this.connectionString = connectionString;
                            connection = new SqlConnection(connectionString);
                            connection.Open();

                            isInitialized = true;

                            //Console.WriteLine("MS SQL connection is successful");

                            Message("MS SQL connection is successful");

                            fail = false;
                        }
                    }
                    if (fail)
                    {
                        Message("DB name from backend rule not found");
                        //throw new Exception("DB name from backend rule not found");
                    }
                }
                catch (SqlException e)
                {
                    Message(e.Message, true);
                }
            }
            ENDJOB:
            return connection;
        }

        public SqlConnection Connect(int index) //pass mssql index here 
        {
            if (connection == null)
            {
                Message("Trying to establish connection to MS SQL");
                try
                {
                    checkServer = InternetConnection.PingMSSQL();
                    if (!checkServer)
                    {
                        Message("Cannot connect to server at the moment", true);
                        goto ENDJOB;
                    }

                    List<DpprSQLServerconfig> list = LocalDB.GetRemoteSQLServerConfig();
                    DpprSQLServerconfig config = list[index]; 

                    string connectionString = string.Format("Data Source={0};Initial Catalog={1}; User ID={2}; Password={3}; CharSet=utf8", config.data_source, config.database_name, config.user_id, config.password);

                    connection = new SqlConnection(connectionString);
                    connection.Open();

                    isInitialized = true;

                    //Console.WriteLine("MS SQL connection is successful");

                    Message("MS SQL connection is successful");
                }
                catch (SqlException e)
                {
                    Message(e.Message, true);
                }
            }
            ENDJOB:
            return connection;
        }

        public bool Insert(string query)
        {
            if (isInitialized && !string.IsNullOrEmpty(query))
            {
                try
                {
                    checkServer = InternetConnection.PingMSSQL();
                    if (!checkServer)
                    {
                        Message("Cannot connect to server at the moment", true);
                        goto ENDJOB;
                    }
                    Message("checkServer: " + checkServer);

                    using (SqlConnection mConnection = new SqlConnection(this.connectionString))
                    {
                        mConnection.Open();
                        Message("Connection is open", true);
                        using (SqlCommand mCommand = new SqlCommand(query, mConnection))
                        {
                            mCommand.CommandType = CommandType.Text;
                            mCommand.CommandTimeout = 120;
                            try
                            {
                                Message("INSERT QUERY ----> " + query);
                                mCommand.ExecuteNonQuery();
                                mCommand.Dispose();
                            }
                            catch (SqlException e)
                            {
                                Message(e.Message + "----> " + query, true);
                                mConnection.Close();
                                Message("[MSSQL Insert] mConnection.State ---> " + mConnection.State.ToString());
                                mConnection.Dispose();
                                Message("Connection is closed", true);
                                return false;
                            }
                        }
                        mConnection.Close();
                        Message("[MSSQL Insert] mConnection.State ---> " + mConnection.State.ToString());
                        mConnection.Dispose();
                        Message("Connection is close", true);
                    };
                    return true;
                }
                catch (SqlException e)
                {
                    Message(e.Message + "---- [MSSQL] ---> " + query, true);
                    return false;
                }
            }
            ENDJOB:
            return false;
        }

        public ArrayList Select(string query)
        {
            ArrayList result = new ArrayList();

            if (isInitialized && !string.IsNullOrEmpty(query))
            {
                try
                {
                    checkServer = InternetConnection.PingMSSQL();
                    if (!checkServer)
                    {
                        Message("No internet connection at the moment", true);
                        goto ENDJOB;
                    }
                    Message("checkServer: " + checkServer);

                    using (SqlConnection mConnection = new SqlConnection(this.connectionString))
                    {
                        mConnection.Open();
                        Message("Connection is open", true);
                        try
                        {
                            using (SqlCommand mCommand = new SqlCommand(query, mConnection))
                            {
                                mCommand.CommandType = CommandType.Text;
                                mCommand.CommandTimeout = 120;
                                using (SqlDataReader mReader = mCommand.ExecuteReader())
                                {
                                    while (mReader.Read())
                                    {
                                        Dictionary<string, string> map = new Dictionary<string, string>();
                                        for (int i = 0, size = mReader.FieldCount; i < size; i++)
                                        {
                                            string key = mReader.GetName(i).ToString();
                                            map[key] = mReader[key].ToString();
                                        }
                                        result.Add(map);
                                    }
                                    mReader.Close();
                                };
                                mCommand.Dispose();
                            };
                        }
                        catch (SqlException e)
                        {
                            Message(e.Message, true);
                            mConnection.Close();
                            Message("[MSSQL Select] mConnection.State: ---> " + mConnection.State.ToString());
                            mConnection.Dispose();
                            Message("Connection is closed", true);
                        }
                        mConnection.Close();
                        Message("[MSSQL Select] mConnection.State: ---> " + mConnection.State.ToString());
                        mConnection.Dispose();
                        Message("Connection is close", true);
                    };

                    this.connection.Close();
                    Message("[MSSQL Select] this.connection.State: ---> " + this.connection.State.ToString());
                }
                catch (SqlException e)
                {
                    Message(e.Message, true);
                }
            }
            ENDJOB:
            return result;
        }

        public ArrayList getAndUploadImageMSSQL(string query, string path, string columnName, int dbindex)
        {
            GlobalLogger logger = new GlobalLogger();
            ArrayList CodeUrlPairList = new ArrayList();
            logger.Broadcast("Uploading images...");

            if (isInitialized && !string.IsNullOrEmpty(query))
            {
                try
                {
                    using (SqlConnection mConnection = new SqlConnection(this.connectionString))
                    {
                        mConnection.Open();
                        Message("Connection is open", true);
                        try
                        {
                            using (SqlCommand mCommand = new SqlCommand(query, mConnection))
                            {
                                mCommand.CommandType = CommandType.Text;
                                mCommand.CommandTimeout = 120;
                                try
                                {
                                    using (SqlDataReader mReader = mCommand.ExecuteReader())
                                    {
                                        while (mReader.Read())
                                        {
                                            for (int ixx = 1; ixx < 9; ixx++)
                                            {
                                                columnName = "image";
                                                columnName = columnName + ixx.ToString();
                                                byte[] picData = mReader[columnName] as byte[] ?? null;
                                                string itemCode = mReader["ItemCode"] as string ?? null;

                                                string imageExtensionNo = "_" + ixx.ToString();
                                                string itemCodeInUrl = itemCode + imageExtensionNo;

                                                if (picData != null)
                                                {
                                                    List<DpprFTPServerConfig> ftplist = LocalDB.GetFTPServerConfig();
                                                    DpprFTPServerConfig ftpconfig = ftplist[dbindex];

                                                    try
                                                    {
                                                        using (WebClient client = new WebClient())
                                                        using (MemoryStream str = new MemoryStream())
                                                        {
                                                            if (picData.Length != 0)
                                                            {
                                                                str.Write(picData, 0, picData.Length);
                                                                Bitmap bit = new Bitmap(str);
                                                                var jpgEncoder = GetEncoder(ImageFormat.Jpeg);

                                                                if (bit.Width > 1000 || bit.Height > 1000 || str.Length > 100000)
                                                                {
                                                                    /* get half of original size */
                                                                    int newWidth = bit.Width / 2;
                                                                    int newHeight = bit.Height / 2;

                                                                    Bitmap resizedImg = ResizeBitmap(bit, newWidth, newHeight);
                                                                    MemoryStream myResult = new MemoryStream(); //new byte[]

                                                                    Encoder myEncoder = Encoder.Quality;
                                                                    EncoderParameters myEncoderParameters = new EncoderParameters(1);
                                                                    EncoderParameter myEncoderParameter = new EncoderParameter(myEncoder, 30L);
                                                                    myEncoderParameters.Param[0] = myEncoderParameter;

                                                                    itemCodeInUrl = Regex.Replace(itemCodeInUrl, @"[^\w\d]", "");

                                                                    //resizedImg.Save(path + "before_" + itemCodeInUrl + ".jpeg", ImageFormat.Jpeg); 

                                                                    resizedImg.Save(myResult, jpgEncoder, myEncoderParameters); //compressed

                                                                    //resizedImg.Save(path + "after_" + itemCodeInUrl + ".jpeg", jpgEncoder, myEncoderParameters);
                                                                    resizedImg.Save(path + itemCodeInUrl + ".jpeg", jpgEncoder, myEncoderParameters); //save local
                                                                    //Console.WriteLine(ftpconfig.username + " " + ftpconfig.password);
                                                                    client.Credentials = new NetworkCredential(ftpconfig.username, ftpconfig.password);
                                                                    byte[] picByte = myResult.ToArray();
                                                                    client.UploadData("ftp://" + ftpconfig.username.Replace("@", "%2540") + "@easysales.asia/easysales/cms/images/product_img/" + itemCodeInUrl + ".jpeg", picByte);

                                                                    string imageUrl = "https://easysales.asia/" + ftpconfig.company_name + "/easysales/cms/images/product_img/" + itemCodeInUrl + ".jpeg";
                                                                    CodeUrlPairList.Add(new KeyValuePair<string, string>(itemCode, imageUrl));
                                                                }
                                                                else
                                                                {
                                                                    //bit.Save(str, ImageFormat.Jpeg);
                                                                    itemCodeInUrl = Regex.Replace(itemCodeInUrl, @"[^\w\d]", ""); //remove suspicious characters
                                                                    bit.Save(path + itemCodeInUrl + ".jpeg", ImageFormat.Jpeg);
                                                                    client.Credentials = new NetworkCredential(ftpconfig.username, ftpconfig.password);
                                                                    byte[] picByte = str.ToArray();
                                                                    client.UploadData("ftp://" + ftpconfig.username.Replace("@", "%2540") + "@easysales.asia/easysales/cms/images/product_img/" + itemCodeInUrl + ".jpeg", picByte);

                                                                    string imageUrl = "https://easysales.asia/" + ftpconfig.company_name + "/easysales/cms/images/product_img/" + itemCodeInUrl + ".jpeg";
                                                                    CodeUrlPairList.Add(new KeyValuePair<string, string>(itemCode, imageUrl));
                                                                }
                                                            }
                                                        }
                                                    }
                                                    catch (WebException ex)
                                                    {
                                                        Message("[WebClient Exception] ---->" + ex.Message, true);
                                                    }
                                                }
                                            }
                                        }
                                        mReader.Close();
                                        this.connection.Close();
                                        Message("[MSSQL getAndUploadImageMSSQL] this.connection.State: ---->" + this.connection.State.ToString());
                                    };
                                }
                                catch (SqlException e)
                                {
                                    Message(e.Message, true);
                                    mCommand.Dispose();
                                    Message("[MSSQL command] mConnection.State: ---->" + this.connection.State.ToString());
                                    mConnection.Dispose();
                                    Message("Connection is closed", true);
                                }
                                mCommand.Dispose();
                                mConnection.Dispose();
                                return CodeUrlPairList;
                            };
                        }
                        catch (SqlException e)
                        {
                            Message(e.Message, true);
                            mConnection.Close();
                            Message("[MSSQL getAndUploadImageMSSQL] mConnection.State: ---->" + this.connection.State.ToString());
                            mConnection.Dispose();
                            Message("Connection is closed", true);
                        }
                        mConnection.Close();
                        Message("[MSSQL getAndUploadImageMSSQL] mConnection.State: ---->" + this.connection.State.ToString());
                        mConnection.Dispose();
                        Message("Connection is close", true);
                    };

                    this.connection.Close();
                    Message("[MSSQL getAndUploadImageMSSQL] this.connection.Close: ---->" + this.connection.State.ToString());
                }
                catch (SqlException e)
                {
                    Message(e.Message, true);
                }
            }
            return CodeUrlPairList;
        }
        private ImageCodecInfo GetEncoder(ImageFormat format)
        {
            var codecs = ImageCodecInfo.GetImageDecoders();
            foreach (var codec in codecs)
            {
                if (codec.FormatID == format.Guid)
                {
                    return codec;
                }
            }

            return null;
        }

        public Bitmap ResizeBitmap(Bitmap bmp, int width, int height)
        {
            Bitmap result = new Bitmap(width, height);
            using (Graphics g = Graphics.FromImage(result))
            {
                g.DrawImage(bmp, 0, 0, width, height);
            }

            return result;
        }

        public void Message(string msg, bool error = false, bool show = false)
        {
            DpprException ex = new DpprException
            {
                exception = msg,
                file_name = "MSSQL",
                time = DateTime.Now.ToLongTimeString()
            };
            LocalDB.InsertException(ex);
        }

        public void Close()
        {
            if (connection != null)
            {
                connection.Close();
                Message("[MSSQL Close()] ---> " + connection.State.ToString());
                isInitialized = false;
            }
        }
    }
}
