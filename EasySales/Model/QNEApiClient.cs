using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RestSharp;
using System.Net;
using Microsoft.Extensions.Logging;
using Quartz.Logging;

namespace EasySales.Model
{
    public interface IQNEApiClient
    {
        void GetCustomer();
    }

    public class QNEApiClient : IQNEApiClient
    {
        private readonly string _dbCode;
        private const string DevApiUrl = "http://localhost:8003";
        private const string ApiUrl = "http://localhost:8003";

        //private const string DevApiUrl = "https://dev-api.qne.cloud";
        //private const string ApiUrl = "https://api.qne.cloud";

        private readonly IRestClient _client;
        public QNEApiClient(string dbCode, bool isDevVer)
        {
            _dbCode = dbCode;
            _client = isDevVer ? new RestClient(DevApiUrl) : new RestClient(ApiUrl);
        }

        public void GetCustomer()
        {
            //System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
            var req = new RestRequest("api/Customers");
            req.AddHeader("DbCode", _dbCode);

            var resp = _client.Get(req);

            if (resp.StatusCode == HttpStatusCode.OK)
            { 
                Console.WriteLine(resp.Content);

                dynamic json = JsonConvert.DeserializeObject(resp.Content);
                foreach (var item in json)
                {
                    Console.WriteLine("{0} {1} {2} {3} {4} {5} {6} {7} {8} {9} {10} {11} {12} {13} {14} {15} {16} {17} {18} {19} {20} {21} {22} {23} {24} {25} {26}\n", item.id, item.companyCode, item.companyName, item.companyName2, item.controlAccount, item.registrationNo,
                        item.gstRegNo, item.category, item.address1, item.address2,
                        item.address3, item.address4,
                        item.contactPerson, item.email,
                        item.phoneNo1, item.phoneNo2,
                        item.faxNo1, item.faxNo2, item.homepage, item.area, item.term, item.salesPerson,
                        item.currency, item.defaultTaxCode,
                        item.currentBalance, item.sourceOfLead, item.status);
                }

                foreach (var prop in resp.GetType().GetProperties())
                {
                    Console.WriteLine(prop.Name);
                }
            }

            if (resp.StatusCode == HttpStatusCode.BadRequest)
            {
                var msg = JsonConvert.DeserializeObject<Object.RespMessage>(resp.Content);

                if (msg?.Message != null)
                    throw new Exception(msg.Message);

                if (!string.IsNullOrEmpty(resp.Content))
                    throw new Exception(resp.Content);
            }

            if (!string.IsNullOrEmpty(resp.ErrorMessage))
                throw new Exception(resp.ErrorMessage);

        }
        public static string ObjectToString<T>(T typeObject) where T : class
        {
            string line = string.Empty;

            foreach (var prop in typeObject.GetType().GetProperties())
            {
                line += string.Format("{1} ", prop.Name, prop.GetValue(typeObject, null));
            }
            return line;
        }

    }
}