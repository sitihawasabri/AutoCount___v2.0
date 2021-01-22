using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using System.Collections;
using EasySales.Object;
using EasySales.Model;
using System.Data;
//using Data;
//using AutoCount;

namespace EasySales
{
    class AutoCountServer
    {
        internal static Func<string, string, int, string> JoinServerInstance = (svr, inst, port)
            => string.Format("{0}{1}", svr, ServerInstance(inst, port));

        private static Func<string, int, string> ServerInstance = (s, p)
            => p == default(int) ? Instance(s) : InstanceWithPort(s, p);

        private static Func<string, string> Instance = s => $"\\{s}";

        private static Func<string, int, string> InstanceWithPort = (s, p)
            => string.IsNullOrEmpty(s) ? $",{p}" : $"\\{s},{p}";
    }
}