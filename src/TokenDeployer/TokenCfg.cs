using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace TokenDeployer
{
    public class TokenCfg
    {
        public string HotwalletAddress { get; set; }
        public IEnumerable<Token> Tokens { get; set; }
        public IEnumerable<TokenTransfer> Transfers { get; set; }
    }

    public class Token
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public TokenType TokenType { get; set; }
        public string TokenName { get; set; }
        public string IssuerAddress { get; set; }
        public int Divisibility { get; set; }
        public string TokenSymbol { get; set; }
        public string Version { get; set; }
        public string InitialSupply { get; set; }
    }

    public class TokenTransfer
    {
        public string IssuerAddress { get; set; }
        public string TokenAddress { get; set; }
        public string Amount { get; set; }
    }

    public enum TokenType
    {
        Emissive = 1,
        NonEmissive = 2,
        LuCyToken = 3
    }
}
