using System;
using System.Collections.Generic;
using System.Text;

namespace ErcDepositFix.Csv
{
    public class EtherscanTx
    {
        public string Txhash { get; set; }
        public string DateTime { get; set; }
        public string From { get; set; }
        public string To { get; set; }
        public string ContractAddress { get; set; }
    }
}
