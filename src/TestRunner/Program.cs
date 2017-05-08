using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TestRunner
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var ethKey = Nethereum.Signer.EthECKey.GenerateKey();
            string privateKey = ethKey.GetPrivateKey();
            
            Console.WriteLine(privateKey);

            Console.ReadLine();
        }
    }
}
