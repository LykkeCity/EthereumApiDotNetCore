using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Common
{
    public class IdCheckResult
    {
        public bool IsFree { get; set; }
        public Guid ProposedId { get; set; }
    }
}
