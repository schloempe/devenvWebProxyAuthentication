using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace devenvWebProxyAuthentication
{
    public interface ILogger
    {
        void BeginFunction(string functionName);

        void EndFunction(string functionName);

        void Message(string message);

        void Warning(string warning);

        void Exception(Exception exception);
    }
}
