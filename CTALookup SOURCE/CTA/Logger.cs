using System;
using System.IO;
using System.Reflection;
using System.Text;

namespace CTALookup
{
    public static class Logger
    {
        private static object _lockMe = new object();
        private static string _logFilename = Assembly.GetExecutingAssembly().GetName().Name + ".log";

        public static void Log(string text)
        {
            lock (_lockMe) {
                using (var w = new StreamWriter(FileName, true, Encoding.UTF8)) {
                    w.WriteLine(string.Format("{0}: {1}", DateTime.Now, text));
                }
            }
        }

        public static void LogException(Exception ex)
        {
            lock (_lockMe) {
                Log("--- Exception ---");
                Log(ex.ToString());
                Log("---/ Exception /---");
            }
        }

        public static string FileName
        {
            get { return _logFilename; }
            set
            {
                lock (_lockMe) {
                    var dir = Path.GetDirectoryName(value);
                    if (!Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }

                    using (var w = new StreamWriter(value, true, Encoding.UTF8))
                    {
                        w.WriteLine("------------------------------");
                        w.WriteLine(string.Format("{0}: Application started", DateTime.Now));
                    }
                    _logFilename = value;
                }
            }
        }

        public static void LogMessageCodeAndThrowException(string msg, string code = null, Exception ex = null)
        {
            Log(msg);
            if (ex != null)
            {
                LogException(ex);
            }
            if (code != null)
            {
                LogCode(code);
            }
            throw new Exception(msg);
        }

        public static void LogCode(string code)
        {
            lock (_lockMe) {
                string file = GetTempFilename();
                using (var writer = new StreamWriter(file))
                {
                    writer.Write(code);
                }
                Log(string.Format("New source file saved:: {0}", file));
            }
        }

        private static string GetTempFilename()
        {
            var directory = Path.GetDirectoryName(_logFilename);
            string file = DateTime.Now.ToString("dd.MM.yyyy.HH.mm.ss.tt");
            return Path.Combine(directory, file + ".source");
        }

        public static void Close()
        {
            lock (_lockMe) {
                using (var w = new StreamWriter(FileName, true, Encoding.UTF8))
                {
                    w.WriteLine(string.Format("{0}: Application closed", DateTime.Now));
                    w.WriteLine("------------------------------");
                    w.WriteLine("------------------------------");
                }
            }
        }
    }
}