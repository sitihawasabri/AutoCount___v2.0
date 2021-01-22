using EasySales.Object;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using System.ComponentModel;
using System.Data;
using System.Threading;
using EasySales.Model;
using Quartz;
using Quartz.Impl;
using Quartz.Logging;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;

namespace EasySales
{
    public class FTPServer
    {
        public ArrayList uploadImage(ArrayList pictureList, ArrayList codeList)
        {
            GlobalLogger logger = new GlobalLogger();

            List<DpprFTPServerConfig> list = LocalDB.GetFTPServerConfig();
            DpprFTPServerConfig config = list[0];

            ArrayList uploadedPicture = new ArrayList();

            try
            {
                for (int i = 0; i < pictureList.Count; i++)
                {
                    dynamic picture = pictureList[i];

                    Dictionary<string, string> pair = (Dictionary<string, string>)codeList[i];

                    using (WebClient client = new WebClient())                                  /*Save the Bitmap to a stream and upload the stream:*/
                    using (var ms = new MemoryStream())                                         /* ms.ToArray() - convert your stream to a byte[] */
                    {
                        Bitmap bmp = new Bitmap(picture);
                        bmp.Save(ms, ImageFormat.Png);

                        client.Credentials = new NetworkCredential(config.username, config.password);
                        byte[] picByte = ms.ToArray();
                        Console.WriteLine("pair.ElementAt(0).Key:" + pair.ElementAt(0).Key);
                        client.UploadData("ftp://" + config.username.Replace("@", "%2540") + "@easysales.asia/easysales/cms/images/product_img/" + pair.ElementAt(0).Key + ".png", picByte);
                        //"ftp://staging%2540@easysales.asia/staging/easysales/cms/images/product_img/"
                    }
                    Message("Succesfully uploaded " + pair.ElementAt(0).Key + " image!");
                    uploadedPicture.Add(pair.ElementAt(0).Key);             //save hashcode
                }
            }
            catch (Exception e)
            {
                logger.Broadcast("FTP catch:" + e.Message);
                Message(e.Message, true);
            }
            return uploadedPicture;                            
        }

        private void Message(string msg, bool error = false, bool show = false)
        {
            DpprException ex = new DpprException
            {
                exception = msg,
                file_name = "FTPServer",
                time = DateTime.Now.ToString()//DateTime.Now.ToLongTimeString()
            };
            LocalDB.InsertException(ex);
        }
    }
}