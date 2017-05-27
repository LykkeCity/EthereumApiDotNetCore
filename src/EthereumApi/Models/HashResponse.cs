﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EthereumApi.Models
{
    public class HashResponse
    {
        public string HashHex { get; set; }
    }

    public class HashResponseWithId : HashResponse
    {
        public Guid OperationId { get; set; }
    }
}
