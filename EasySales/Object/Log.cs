using System;
using System.Collections;
using System.IO;

namespace EasySales.Object
{
    class Log
    {
        private static Log instance = null;
        private ArrayList stream = null;
        private bool isDebugMode = false;
        private static readonly string FILE_NAME = "log.txt";

        public Log()
        {
            this.stream = new ArrayList();
        }

        public bool CurrentEnvironmentMode()
        {
            return this.isDebugMode;
        }

        public void SetCurrentEnvironmentDebugMode(bool mode)
        {
            this.isDebugMode = mode;
        }

        public static Log Instance()
        {
            if (instance == null)
            {
                instance = new Log();
            }
            return instance;
        }

        public void Message(string msg, bool error = false, bool forceShow = false)
        {
            if (forceShow)
            {
                Console.WriteLine(msg);
            }

            string timestamp = DateTime.Now.ToString("yyyy-MM-dd h:mm:ss tt");

            if (isDebugMode)
            {
                if (error)
                {
                    msg = "[!!ERROR!!] " + timestamp + " > " + msg;
                    Console.Error.WriteLine(msg);
                }
                else
                {
                    msg = "[EasyTech] " + timestamp + " > " + msg;
                    Console.WriteLine(msg);
                }
            }
            else
            {
                if (error)
                {
                    msg = "[!!ERROR!!] " + timestamp + " > " + msg;
                }
                else
                {
                    msg = "[EasyTech] " + timestamp + " > " + msg;
                }

                this.stream.Add(msg);
            }
        }

        public void Execute()
        {
            if (!isDebugMode)
            {
                string path = string.Format(@"{0}\{1}", Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase), FILE_NAME);
                path = new Uri(path).LocalPath;

                if (!File.Exists(path))
                {
                    File.Create(path);
                    TextWriter tw = new StreamWriter(path);
                    foreach (string msg in this.stream)
                    {
                        tw.WriteLine(msg);
                    }
                    tw.Close();
                }
                else if (File.Exists(path))
                {
                    using (var tw = new StreamWriter(path, true))
                    {
                        foreach (string msg in this.stream)
                        {
                            tw.WriteLine(msg);
                        }
                    }
                }
            }
        }

        ~Log()
        {
            Execute();
        }
    }
}