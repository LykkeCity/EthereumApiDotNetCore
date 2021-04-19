using System;

namespace TestRunner
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var ethKey = Nethereum.Signer.EthECKey.GenerateKey();
            string privateKey = ethKey.GetPrivateKey();
            string publickKey = Nethereum.Signer.EthECKey.GetPublicAddress("0x1149984b590c0bcd88ca4e7ef80d2f4aa7b0bc0f52ac7895068e89262c8733c6");
            Console.WriteLine(privateKey);
            Console.WriteLine(publickKey);

            Console.ReadLine();
        }
    }
}
