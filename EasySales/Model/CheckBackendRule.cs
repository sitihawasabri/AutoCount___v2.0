using EasySales.Object;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using System.Data;
using Newtonsoft.Json.Linq;
using System.Dynamic;
using EasySales.Job;

namespace EasySales.Model
{
    public class CheckBackendRule
    {
        private Database mysql = null;
        private GlobalLogger logger;

        public bool checkTable = false;

        public string So_Id = string.Empty;
        public string DeliveryDate = string.Empty;
        public int dbIndex = 0;

        public CheckBackendRule(int dbIndex)
        {
            this.dbIndex = dbIndex;
            this.logger = new GlobalLogger();
        }

        public CheckBackendRule(Database mysql)
        {
            this.mysql = mysql;
            this.logger = new GlobalLogger();
        }

        public CheckBackendRule()
        {
            this.mysql = new Database();
            this.logger = new GlobalLogger();
        }

        private Database GetConnection()
        {
            if (this.mysql == null)
            {
                this.mysql = new Database();
                this.mysql.Connect(this.dbIndex);
                return this.mysql;
            }
            else
            {
                return this.mysql;
            }
        }

        public CheckBackendRule CheckTablesExist()
        {
            List<DpprMySQLconfig> dbList = LocalDB.GetRemoteDatabaseConfig();
            DpprMySQLconfig configDb = dbList[this.dbIndex];

            Database _mysql = GetConnection();

            ArrayList isExists = _mysql.Select("SELECT EXISTS(SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE (TABLE_NAME = 'cms_backend_rules') AND (TABLE_SCHEMA = '" + configDb.config_database + "'));");

            if (isExists.Count == 1)
            {
                Console.WriteLine("Table exists!");
                this.checkTable = true;
            }
            else
            {
                Console.WriteLine("Table NOT exists!");
                this.checkTable = false;
            }
            return this;
        }

        public List<object> GetSettingByTableName(string tableName)
        {
            if (this.checkTable)
            {
                Database _mysql = GetConnection();

                ArrayList moduleSetting = _mysql.Select("SELECT CAST(setting AS CHAR(10000) CHARACTER SET utf8) as setting FROM cms_backend_rules WHERE module = '" + tableName + "';");

                if (moduleSetting.Count > 0)
                {
                    Dictionary<string, string> settingList = (Dictionary<string, string>)moduleSetting[0];
                    string setting = settingList["setting"].ToString();

                    var token = JToken.Parse(setting);

                    if (token is JArray)
                    {
                        dynamic jsonRule = JsonConvert.DeserializeObject<IEnumerable<object>>(setting); /* parse json array*/
                        return jsonRule;
                    }
                    else if (token is JObject)
                    {
                        dynamic jsonRule = JsonConvert.DeserializeObject(setting);                      /* parse json object*/

                        System.Type jsontype = jsonRule.GetType();
                        Console.WriteLine("JSON Type: " + jsontype);

                        if (jsontype.ToString() == "Newtonsoft.Json.Linq.JObject")
                        {
                            List<object> parsedFields = new List<object>();
                            parsedFields.Add(jsonRule);                                                     /* add JSON JObject to the List<>*/
                            return parsedFields;
                        }
                        else                                                                                /* Newtonsoft.Json.Linq.List<> */
                        {
                            return jsonRule;
                        }
                    }
                }

            }
            return new List<object>();
        }

        public JArray getAppRemark()
        {
            ArrayList remark_options = mysql.Select("SELECT * FROM cms_mobile_module WHERE module = 'app_remark_options'");

            if (remark_options.Count > 0)
            {
                Dictionary<string, string> settingList = (Dictionary<string, string>)remark_options[0];
                string setting = settingList["status"].ToString();

                Console.WriteLine(setting);

                JArray remarkJArray = setting.IsJArray() ? (JArray)JToken.Parse(setting) : new JArray();

                return remarkJArray;
            }
            return new JArray();

        }

        public void SetDbIndex(int idb)
        {
            this.dbIndex = idb;
        }

        public int GetDbIndex()
        {
            return this.dbIndex;
        }

    }
}