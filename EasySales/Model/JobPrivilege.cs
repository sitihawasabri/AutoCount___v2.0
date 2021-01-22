using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasySales.Model
{
    public class JobPrivilege
    {
        private static JobPrivilege instance = null;
        private static bool isTransferring = false;

        private JobPrivilege()
        {

        }

        public static JobPrivilege Application()
        {
            if(instance == null)
            {
                instance = new JobPrivilege();
            }
            return instance;
        }

        public void Transferring()
        {
            isTransferring = true;
        }

        public bool Wait()
        {
            return isTransferring;
        }
    }
}
