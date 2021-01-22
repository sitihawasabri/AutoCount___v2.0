using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasySales.Model
{
    public class GlobalEvent : EventArgs
    {
        public string message { get; set; }

        public GlobalEvent(string message)
        {
            this.message = message;
        }
    }
}
