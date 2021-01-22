using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using EasySales.Object;
using System.Net;

namespace EasySales.Model
{
    public static class _
    {
        public static string ReplaceAll(this String main,string replacement,params string[] search)
        {
            foreach(string find in search)
            {
                main = main.Replace(find, replacement);
            }
            return main;

            /* for varLongModel and varLongDesc, 
             * when it replace Desc and Model at first, 
             * they also replaced Desc and Model in varLongModel and varLongDesc */
        }

        public static string SafeReplaceAll(this string main, dynamic replacement, bool matchWholeWord, params string[] search)
        {
            foreach (string find in search)
            {
                string textTofind = matchWholeWord ? string.Format(@"\b{0}\b", find) : find;
                main = Regex.Replace(main, textTofind, replacement);
            }
            return main;
        }

        public static bool IsJArray(this String main)
        {
            try
            {
                JArray arr = (JArray)JToken.Parse(main);
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
            return false;
        }

        public static string RemoveLast(this String main)
        {
            if(main.Length > 0)
            {
                main = main.Trim();
                main = main.Remove(main.Length - 1);
                return main;
            }
            return string.Empty;
        }

        public static bool IsEmpty(this String main)
        {
            return main.Trim().Length == 0;
        }

        public static string IIF(this String main,bool condition, string _else)
        {
            return condition ? main : _else;
        }

        public static string MSSQLdate(this String date)
        {
            if (date.Length > 0)
            {
                Thread.CurrentThread.CurrentCulture = new CultureInfo("en-GB");
                //return DateTime.ParseExact(date, "dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture).ToString("yyyy-MM-dd");
                date = Convert.ToDateTime(date).ToString("yyyy-MM-dd");
                return date;
            }
            return string.Empty;
        }

        public static void Iterate<T>(this IEnumerable<T> enumrable, Action<T, int> callback)
        {
            int index = 0;
            foreach(T template in enumrable)
            {
                callback(template, index++);
            }
        }

        public static void Iterate<T>(this ArrayList enumrable, Action<T,int> callback)
        {
            int index = 0;
            foreach (T template in enumrable)
            {
                callback(template, index++);
            }
        }

        public static bool EcodeContains(this String main, string needle)
        {
            foreach (string keyword in LogicParser.Keywords)
            {
                main = main.ReplaceAll("", keyword);
            }
            string compare = string.Empty;
            int counter = 0;
            foreach (char c in main)
            {
                if(counter == 0 && c == '`')
                {
                    return true;
                }
                if(c == '`' || LogicParser.MathmeticOpertators.Contains(c.ToString()))
                {
                    break;
                }
                if(c.ToString().Trim().Length > 0)
                {
                    compare += c;
                }
            }
            return compare == needle;
        }
    }
}
