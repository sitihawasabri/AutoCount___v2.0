using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EasySales.Object;
using Quartz.Logging;

namespace EasySales.Model
{
    public class JobQueueLogger : ILogProvider
    {
        public Logger GetLogger(string name)
        {
            return (level, func, exception, parameters) =>
            {
                if (level >= LogLevel.Info && func != null)
                {
                    Console.WriteLine("[JobQueueLog] [" + DateTime.Now.ToLongTimeString() + "] [" + level + "] " + func(), parameters);

                    DpprJobQueueLog logqueue = new DpprJobQueueLog
                    {
                        job_name = func().ToString(),
                        job_level = level.ToString(),
                        job_param = parameters.ToString(),
                        job_exec_time = DateTime.Now.ToLongTimeString()
                    };

                    LocalDB.InsertJobLog(logqueue);
                }
                return true;
            };
        }

        public IDisposable OpenMappedContext(string key, string value)
        {
            throw new NotImplementedException();
        }

        public IDisposable OpenNestedContext(string message)
        {
            throw new NotImplementedException();
        }
    }
}
