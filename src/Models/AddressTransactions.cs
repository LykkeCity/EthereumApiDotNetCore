using System;
using System.Collections.Generic;
using System.Text;

namespace BusinessModels
{
    public class AddressTransaction
    {
        public string Address { get; set; }
        public int Start { get; set; }
        public int Count { get; set; }
    }

    public class TokenTransaction : AddressTransaction
    {
        public string TokenAddress { get; set; }
    }
}
