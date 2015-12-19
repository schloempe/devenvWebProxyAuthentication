using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace devenvWebProxyAuthentication
{
    public class FileLogger : ILogger
    {
        private string _logFilename;

        private FileLogger() { }

        public FileLogger(string filename)
        {
            _logFilename = string.Empty;
            try
            {
                if (Directory.Exists(Path.GetDirectoryName(filename)))
                {
                    _logFilename = filename;
                }
            }
            catch (ArgumentException)
            {
                _logFilename = string.Empty;
            }
        }

        public void BeginFunction(string functionName)
        {
            WriteToFile(string.Format("{0} - BEGIN FUNCTION: {1} ...", DateTime.UtcNow, functionName));
        }

        public void EndFunction(string functionName)
        {
            WriteToFile(string.Format("{0} - ... END FUNCTION: {1}", DateTime.UtcNow, functionName));
        }

        public void Message(string message)
        {
            WriteToFile(string.Format("{0} -     {1}", DateTime.UtcNow, message));
        }

        public void Warning(string warning)
        {
            WriteToFile(string.Format("{0} -     !WARNING! {1}", DateTime.UtcNow, warning));
        }

        public void Exception(Exception exception)
        {
            WriteToFile(string.Format("{0} -     !EXCEPTION! {1}", DateTime.UtcNow, exception));
        }

        private void WriteToFile(string message)
        {
            if (string.IsNullOrEmpty(_logFilename))
            {
                return;
            }

            int maxCounter = 100;

            while (true)
            {
                try
                {
                    File.AppendAllText(_logFilename, string.Format("{0}\r\n", message));
                    break;
                }
                catch (Exception)
                {
                    if (--maxCounter < 0)
                    {
                        break;
                    }
                    Thread.Sleep(100);
                }
            }
        }
    }
}
