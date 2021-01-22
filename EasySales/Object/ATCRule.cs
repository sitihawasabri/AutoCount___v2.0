using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasySales.Object
{
    public class ATCRule
    {
        public string DBname { get; set; } 
        public string CategoryField { get; set; }
        public int loopCount { get; set; }
        public Dictionary<string,string> WarehousePairList { get; set; }
        public string Query { get; set; }
        public ArrayList Include { get; set; }
        public ArrayList Exclude { get; set; }
        public ATCRule() { }

        public bool IsExcludeAll()
        {
            for (int i = 0; i < Exclude.Count; i++)
            {
                Dictionary<string, string> item = (Dictionary<string, string>)Exclude[i];
                if(item["mysql"] == "*" && item["mssql"] == "*")
                {
                    return true;
                }
            }
            return false;
        }

        public bool IsIncludeAll()
        {
            for (int i = 0; i < Include.Count; i++)
            {
                Dictionary<string, string> item = (Dictionary<string, string>)Include[i];
                if (item["mysql"] == "*" && item["mssql"] == "*")
                {
                    return true;
                }
            }
            return false;
        }
    }
}
