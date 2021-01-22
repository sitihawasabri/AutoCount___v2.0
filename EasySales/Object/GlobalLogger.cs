using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasySales.Object
{
    public class GlobalLogger
    {
        public GlobalLogger() { }

        public string message { get; set; }

        //public bool isTransferringSO { get; set; }

        public void Broadcast()
        {
            LocalDB.GlobalLog(this);
        }

        public void Broadcast(string message)
        {
            this.message = message;
            LocalDB.GlobalLog(this);
        }
    }
}
