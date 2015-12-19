using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace devenvWebProxyAuthentication
{
    public sealed class LogSingleton : ILogger
    {
        private static readonly LogSingleton _logSingleton = new LogSingleton();
        private ILogger _logger = null;

        public static LogSingleton GetInstance
        {
            get
            {
                return _logSingleton;
            }
            private set {}
        }

        public void SetLogger(ILogger logger)
        {
            _logger = logger;
        }

        public void BeginFunction(string functionName)
        {
            if (_logger != null)
            {
                _logger.BeginFunction(functionName);
            }
        }

        public void EndFunction(string functionName)
        {
            if (_logger != null)
            {
                _logger.EndFunction(functionName);
            }
        }

        public void Message(string message)
        {
            if (_logger != null)
            {
                _logger.Message(message);
            }
        }

        public void Warning(string warning)
        {
            if (_logger != null)
            {
                _logger.Warning(warning);
            }
        }

        public void Exception(Exception exception)
        {
            if (_logger != null)
            {
                _logger.Exception(exception);
            }
        }
    }
}
