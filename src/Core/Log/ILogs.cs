using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Core.Log
{
    public interface ILog
    {
        Task WriteInfo(string component, string process, string context, string info, DateTime? dateTime = null);
        Task WriteWarning(string component, string process, string context, string info, DateTime? dateTime = null);
        Task WriteError(string component, string process, string context, Exception exeption, DateTime? dateTime = null);
        Task WriteFatalError(string component, string process, string context, Exception exeption, DateTime? dateTime = null);
    }
}
