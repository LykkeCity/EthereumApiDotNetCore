using System;
using System.Collections.Generic;
using System.Text;

namespace BusinessModels
{
    public class AddressHistoryModel
    {
        public ulong BlockNumber { get; set; }

        public string Value { get; set; }

        public string TransactionHash { get; set; }

        public string From { get; set; }

        public string To { get; set; }

        public uint BlockTimestamp { get; set; }

        public DateTime BlockTimeUtc { get; set; }

        public bool HasError { get; set; }
        public int TransactionIndexInBlock { get; set; }
        public int MessageIndex { get; set; }
    }
}
