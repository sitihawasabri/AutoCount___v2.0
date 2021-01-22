using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.XPath;
using System.Linq;
using System.Text;
using System.IO;
using System.Web.Script.Serialization;
using System.Net.NetworkInformation;
using System.Net;

namespace EasySales.Model
{
    public static class LogicParser
    {
        // Logic
        // Keywords = default | join | empty
        // DataType string = ``
        // Operators + - / *  
        // String operators +
        // New line \n
        // Space _
        public static readonly ArrayList Keywords = new ArrayList()
        {
            "default",
            "join",
            "empty"
        };

        public static readonly ArrayList StringOperators = new ArrayList()
        {
            "+"
        };

        public static readonly ArrayList MathmeticOpertators = new ArrayList()
        {
            "+",
            "-",
            "/",
            "*",
            "(",
            ")",
            "%",
            "="
        };

        //public static Dictionary<string, string> Parse(string mysql_field, string ecode, Dictionary<string, string> data)
        public static Dictionary<string, string> Parse(string mysql_field, string ecode, Dictionary<string, string> data, string nullfield)
        {
            if(IsItCode(ecode) == false)
            {
                return data;
            }

            string keyword = GetKeyword(ecode);

            if (keyword != string.Empty && keyword == "empty")
            {
                string empty = " ";
                return new Dictionary<string, string>()
                {
                        { mysql_field, empty }
                };
                
            }

            if (keyword != string.Empty && keyword == "default")
            {
                string[] splitted = RemoveEmptyStr(ecode.Split(' '));
                if (splitted.Length > 2 || splitted.Length == 1)
                {
                    throw new Exception("default Keyword expect 1 identifier. Multiple provided");
                }
                if (splitted.Length > 1 && splitted.Length < 3)
                {
                    return new Dictionary<string, string>()
                    {
                        { mysql_field, splitted[1] }
                    };
                }
            }
            if (keyword != string.Empty && keyword == "join")
            {
                string[] splitted = RemoveEmptyStr(Separate(ecode));
                if (splitted.Length == 1)
                {
                    throw new Exception("join Keyword expect declaration. Null provided");
                }
                if (splitted.Length > 2)
                {
                    throw new Exception("join Keyword expect 1 declaration. Multiple provided");
                }
                string declaration = splitted[1];

                if (declaration.Contains("`"))
                {
                    List<string> tokens = MakeStringExpression(declaration);
                    string value = string.Empty;
                    foreach (string token in tokens)
                    {
                        if (IsCodeStr(token))
                        {
                            value += MakeString(token);
                        }
                        else
                        {
                            foreach (KeyValuePair<string, string> entry in data)
                            {
                                if (entry.Key == token) /* token = ItemCode/Desc/Brand/Model */
                                {
                                    value += entry.Value;
                                }
                            }
                        }
                    }
                    foreach (KeyValuePair<string, string> entry in data)
                    {
                        string entryValue = entry.Value;
                        if (entryValue == "")
                        {
                            //entryValue = /* nullfield of each field */;
                            entryValue = nullfield;
                        }

                        value = value.SafeReplaceAll(entryValue, true, entry.Key);
                    }
                    return new Dictionary<string, string>()
                    {
                        { mysql_field, value }
                    };
                }
                else
                {
                    foreach (KeyValuePair<string, string> entry in data)
                    {
                        declaration = declaration.ReplaceAll(entry.Value, entry.Key);
                    }

                    string value = MathmeticEvaluation(declaration) + "";
                    return new Dictionary<string, string>()
                    {
                        { mysql_field, value }
                    };
                }
            }
            return new Dictionary<string, string>()
            {
                { mysql_field, string.Empty }
            };
        }

        private static double MathmeticEvaluation(string ecode)
        {
            var xsltExpression =
                string.Format("number({0})",
                    new Regex(@"([\+\-\*])").Replace(ecode, " ${1} ")
                                            .Replace("/", " div ")
                                            .Replace("%", " mod "));
            return (double)new XPathDocument
                (new StringReader("<r/>"))
                    .CreateNavigator()
                    .Evaluate(xsltExpression);
        }

        public static string GetKeyword(string ecode) //private
        {
            if (ecode != string.Empty)
            {
                string[] splitted = RemoveEmptyStr(ecode.Split(' '));
                if (splitted.Length > 0 && Keywords.Contains(splitted[0]))
                {
                    return splitted[0];
                }
            }
            return string.Empty;
        }

        private static string[] Separate(string ecode)
        {
            List<string> _ = new List<string>();
            string tmp = string.Empty;
            foreach (char c in ecode)
            {
                if (c == ' ')
                {
                    _.Add(tmp.Trim());
                    tmp = "";
                }
                tmp += c;
            }
            if (tmp.Length > 0)
            {
                _.Add(tmp.Trim());
            }
            return _.ToArray();
        }

        public static string[] RemoveEmptyStr(string[] arr) //private
        {
            List<string> final = new List<string>();
            for (int i = 0; i < arr.Length; i++)
            {
                if (arr[i].Trim().Length != 0)
                {
                    final.Add(arr[i].Trim());
                }
            }
            string[] temp = final.ToArray();
            return temp;
        }

        private static List<string> MakeStringExpression(string ecode)
        {
            List<string> _ = new List<string>();
            for (int i = 0; i < StringOperators.Count; i++)
            {
                string _operator = StringOperators[i].ToString();
                string[] tokens = ecode.Split(_operator.ToCharArray());
                for (int j = 0; j < tokens.Length; j++)
                {
                    if (tokens[j].Trim().Length > 0)
                    {
                        _.Add(tokens[j]);
                        if (j != tokens.Length - 1)
                        {
                            _.Add(_operator);
                        }
                    }
                }
            }
            return _;
        }

        private static string MakeString(string str)
        {
            string final = string.Empty;
            foreach (char c in str)
            {
                if (c != '`')
                {
                    final += (c == '_' ? ' ' : c);
                }
            }
            return final;
        }

        public static bool IsCodeStr(string str)
        {
            return str.Contains("`");
        }

        private static bool IsItCode(string ecode)
        {
            foreach (string token in Separate(ecode))
            {
                if(Keywords.Contains(token))
                {
                    return true;
                }
            }
            foreach (char c in ecode)
            {
                if(StringOperators.Contains(c.ToString()))
                {
                    return true;
                }
                if(MathmeticOpertators.Contains(c.ToString()))
                {
                    return true;
                }
            }
            return false;
        }

        public static string filterOrderUDFbyKey(dynamic udf, string key)
        {
            /* key = "refId" (LB) */

            foreach (var item in udf)
            {
                if(item.type == "select") 
                {
                    if (item.code == key)
                    {
                        return item.value; /* ORDER */
                    }
                }
                else //input
                {
                    if (item.item.name == key)
                    {
                        return item.value;
                    }
                }
            }
            return string.Empty;
        }

        public static string client_ID()
        {
            Database _mysql = new Database();
            ArrayList isExists = _mysql.Select("SHOW COLUMNS FROM cms_setting LIKE 'so_running_prefix';");

            string formattedSOId = string.Empty;

            if (isExists.Count == 1)
            {
                Console.WriteLine("Column exists!");

                string prefix = string.Empty;
                string format = string.Empty;
                string id = string.Empty;
                string y = string.Empty;
                string d = string.Empty;
                string m = string.Empty;
                string year = string.Empty;
                string month = string.Empty;
                string day = string.Empty;
                DateTime orderDate = DateTime.Now;

                ArrayList idFormat = _mysql.Select("SELECT so_running_prefix, so_running_format, so_running_id FROM cms_setting");

                for (int i = 0; i < idFormat.Count; i++)
                {
                    Dictionary<string, string> idObj = (Dictionary<string, string>)idFormat[i];
                    prefix = idObj["so_running_prefix"];
                    format = idObj["so_running_format"]; //should be yyMMdd
                    id = idObj["so_running_id"];
                }

                if (format.Contains("yy"))
                {
                    y = "yy";

                    year = orderDate.ToString(y);
                    format = format.Replace(y, year);
                }

                if (format.Contains("yyyy"))
                {
                    y = "yyyy";

                    year = orderDate.ToString(y);
                    format = format.Replace(y, year);
                }

                if (format.Contains("MM"))
                {
                    m = "MM";
                    month = orderDate.ToString(m);
                    format = format.Replace(m, month);
                }

                if (format.Contains("dd"))
                {
                    d = "dd";
                    day = orderDate.ToString(d);

                    format = format.Replace(d, day);
                }

                formattedSOId = prefix + format + id;
                Console.WriteLine(formattedSOId);
            }
            else
            {
                Console.WriteLine("Column NOT exists!");
                return string.Empty;
            }
            return formattedSOId;
        }

        public static string QuotedStr(string str)
        {
            return "'" + str.Replace("'", "''") + "'";
        }
    }
}
