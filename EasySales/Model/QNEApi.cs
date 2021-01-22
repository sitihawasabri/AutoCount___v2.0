using EasySales.Object;
using EasySales.Object.QNE;
using Quartz.Logging;
using RestSharp;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System.Web.Script.Serialization;

namespace EasySales.Model
{
    public class QNEApi
    {
        private static QNEApi instance = null;
        private GlobalLogger logger = new GlobalLogger();
       
        private string DevApiUrl = "http://localhost:8003";
        private string ApiUrl = "http://localhost:8003";

        private string DbCode = string.Empty;
        private IRestClient Client;

        public QNEApi()
        {
            //create the connection
            List<DpprAccountingSoftware> configurationList = LocalDB.GetAccountingSoftwares();
            if (configurationList.Count == 0)
            {
                logger.message = "------------------------NO QNE SETTINGS FOUND-----------------------";
                logger.Broadcast();
                return;
            }
            DpprAccountingSoftware config = configurationList[0];

            this.DevApiUrl = config.software_link;
            this.ApiUrl = config.software_link;
            this.DbCode = config.software_db;

            this.Client = new RestClient(ApiUrl);
        }

        public List<object> GetByName(string api_name, Parameter parameter = null)
        {
            try
            {
                var req = new RestRequest("api/" + api_name);
                req.AddHeader("DbCode", this.DbCode);
                if (parameter != null)
                {
                    req.AddParameter(parameter);
                }

                var resp = this.Client.Get(req);
                //Console.WriteLine(resp.ResponseUri);

                if (resp.StatusCode == HttpStatusCode.OK)
                {
                    var token = JToken.Parse(resp.Content);
                    Message("resp.Content [" + resp.ResponseUri + "]:" + resp.Content);

                    if (token is JArray)
                    {
                        /** FOR ALL OTHER SYNC */
                        try
                        {
                            dynamic json = JsonConvert.DeserializeObject<IEnumerable<object>>(resp.Content);
                            return json;
                        }
                        catch (Exception exc)
                        {
                            Message(resp.ResponseUri + ": Failed to get content JArray token---> " + exc.Message);
                            Message("resp.Content [Failed to get content JArray token]:" + resp.Content);
                            return new List<object>();
                        }
                    }
                    else if (token is JObject)
                    {
                        /** FOR INVOICE DETAILS SYNC */
                        try
                        {
                            dynamic json = JsonConvert.DeserializeObject(resp.Content);
                            System.Type jsontype = json.GetType();
                            Console.WriteLine("JSON Type: " + jsontype);

                            if (jsontype.ToString() == "Newtonsoft.Json.Linq.JObject")
                            {
                                List<object> parsedFields = new List<object>();
                                parsedFields.Add(json);
                                return parsedFields;                                  //add JSON JObject to the List<>
                            }
                            else                                                      //"Newtonsoft.Json.Linq.List<>
                            {
                                return json;
                            }
                        }
                        catch (Exception exc)
                        {
                            Message(resp.ResponseUri + ": Failed to get content JObject token---> " + exc.Message);
                            Message("resp.Content [Failed to get content JObject token]:" + resp.Content);
                            return new List<object>();
                        }
                    }
                }

                if (resp.StatusCode == HttpStatusCode.BadRequest)
                {
                    try
                    {
                        var msg = JsonConvert.DeserializeObject<Object.RespMessage>(resp.Content);
                        return new List<object>();
                    }
                    catch
                    {
                        Message(resp.ResponseUri + ": Failed to get content - BadRequest---> " + resp.StatusCode);
                        Message("resp.Content [Failed to get content - BadRequest]:" + resp.Content);
                        return new List<object>();
                    }
                }

                if (!string.IsNullOrEmpty(resp.ErrorMessage))
                {
                    return new List<object>();
                }
                return new List<object>();
            }
            catch
            {
                Message("Failed to get returned from API");
            }
            return new List<object>();
        }
        public SalesOrdersResp PostSalesOrders(SalesOrdersPostParams @params)
        {
            var req = new RestRequest("api/SalesOrders");
            req.AddHeader("DbCode", this.DbCode);
            req.AddJsonBody(@params);

            GlobalLogger logger = new GlobalLogger();

            var resp = this.Client.Post(req);

            if (resp.StatusCode == HttpStatusCode.OK)
                return JsonConvert.DeserializeObject<SalesOrdersResp>(resp.Content);

            if (resp.StatusCode == HttpStatusCode.BadRequest)
            {
                var msg = JsonConvert.DeserializeObject<RespMessage>(resp.Content);

                if (msg?.Message != null)
                    throw new Exception(msg.Message);

                if (!string.IsNullOrEmpty(resp.Content))
                    throw new Exception(resp.Content);
            }

            if (!string.IsNullOrEmpty(resp.ErrorMessage))
                throw new Exception(resp.ErrorMessage);

            throw new Exception($"Unknown Error: {resp.StatusDescription}; Status Code={(int)resp.StatusCode}");
        }

        public SalesInvoiceResp PostSalesInvoice(SalesInvoicePostParams @invparams)
        {
            var req = new RestRequest("api/SalesInvoices");
            req.AddHeader("DbCode", this.DbCode);
            req.AddJsonBody(@invparams);

            var resp = this.Client.Post(req);

            if(resp.ErrorException != null)
            {
                Message("ErrorException ---> " + resp.ErrorException.ToString());
                Message("ErrorMessage ---> " + resp.ErrorMessage.ToString());
            }

            Message("StatusCode ---> " + resp.StatusCode.ToString());
            Message("Content ---> " + resp.Content.ToString());
            Message("IsSuccessful ---> " + resp.IsSuccessful.ToString());
            Message("ResponseStatus ---> " + resp.ResponseStatus.ToString());
            Message("ResponseUri ---> " + resp.ResponseUri.ToString());
            Message("Server ---> " + resp.Server.ToString());

            if (resp.StatusCode == HttpStatusCode.OK)
            {
                //return JsonConvert.DeserializeObject<SalesInvoiceResp>(resp.Content);
                //add try catch?
                try
                {
                    Message("HttpStatusCode.OK --->" + resp.Content.ToString());
                    return JsonConvert.DeserializeObject<SalesInvoiceResp>(resp.Content);
                }
                catch
                {
                    Message("Failed to return SalesInvoiceResp ---> " + resp.Content.ToString());
                }
            } 

            if (resp.StatusCode == HttpStatusCode.BadRequest)
            {
                //var msg = JsonConvert.DeserializeObject<RespMessage>(resp.Content);
                try
                {
                    var msg = JsonConvert.DeserializeObject<RespMessage>(resp.Content);
                    if (msg?.Message != null)
                    {
                        Message("HttpStatusCode.BadRequest ---->" + msg.Message); //just added 14092020
                        throw new Exception(msg.Message);
                    }

                    if (!string.IsNullOrEmpty(resp.Content))
                    {
                        Message("HttpStatusCode.BadRequest ---->" + resp.Content);
                        throw new Exception(resp.Content);
                    }
                }
                catch
                {
                    Message("Failed to return RespMessage ---> " + resp.Content.ToString());
                }
            }

            if (!string.IsNullOrEmpty(resp.ErrorMessage))
            {
                Message("IsNullOrEmpty ---->" + resp.ErrorMessage);
                throw new Exception(resp.ErrorMessage);
            }

            Message($"Unknown Error: {resp.StatusDescription}; Status Code={(int)resp.StatusCode}");
            throw new Exception($"Unknown Error: {resp.StatusDescription}; Status Code={(int)resp.StatusCode}");
        }

        public SalesCNsResp PostSalesCNs(SalesCNsPostParams @cnparams)
        {
            var req = new RestRequest("api/SalesCNs");
            req.AddHeader("DbCode", this.DbCode);
            req.AddJsonBody(@cnparams);

            var resp = this.Client.Post(req);

            if (resp.StatusCode == HttpStatusCode.OK)
                return JsonConvert.DeserializeObject<SalesCNsResp>(resp.Content);

            if (resp.StatusCode == HttpStatusCode.BadRequest)
            {
                var msg = JsonConvert.DeserializeObject<RespMessage>(resp.Content);

                if (msg?.Message != null)
                {
                    Message(msg.Message);
                    throw new Exception(msg.Message);
                }
                    
                if (!string.IsNullOrEmpty(resp.Content))
                {
                    Message(resp.Content);
                    throw new Exception(resp.Content);
                }
                    
            }

            if (!string.IsNullOrEmpty(resp.ErrorMessage))
            {
                Message(resp.Content);
                throw new Exception(resp.ErrorMessage);
            }

            Message($"Unknown Error: {resp.StatusDescription}; Status Code={(int)resp.StatusCode}");
            throw new Exception($"Unknown Error: {resp.StatusDescription}; Status Code={(int)resp.StatusCode}");
        }

        public void Message(string msg)
        {
            DpprException ex = new DpprException
            {
                exception = msg,
                file_name = "QNEAPI.cs",
                time = DateTime.Now.ToLongTimeString()
            };
            LocalDB.InsertException(ex);
        }
    }
}