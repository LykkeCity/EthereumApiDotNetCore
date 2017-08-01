using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Services.Model
{
    public class OperationEstimationResult
    {
        public bool IsAllowed { get; set; }
        public BigInteger GasAmount { get; set; }
    }
}
