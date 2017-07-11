using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace BusinessModels
{
    public class InternalMessageModel
    {
        public string TransactionHash { get; set; }
        public ulong BlockNumber { get; set; }
        public string FromAddress { get; set; }
        public string ToAddress { get; set; }
        public int Depth { get; set; }
        public BigInteger Value { get; set; }
        public int MessageIndex { get; set; }
        public string Type { get; set; }
        public uint BlockTimestamp { get; set; }
        public DateTime BlockTimeUtc { get; set; }
    }
}
