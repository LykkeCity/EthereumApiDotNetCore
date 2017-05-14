using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Utils
{
    public class QueueMessageBase
    {
        public int DequeueCount { get; set; }
        public string LastError { get; set; }
    }
}
