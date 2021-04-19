using System;
using System.Numerics;

namespace Lykke.Job.EthereumCore.Workflow
{
    public static class EthServiceHelpers
    {
        public static BigInteger ConvertToContract(decimal amount, int multiplier, int accuracy)
        {
            if (accuracy > multiplier)
                throw new ArgumentException("accuracy > multiplier");

            amount *= (decimal)Math.Pow(10, accuracy);

            // hotfix for rounding problems
            if (amount < 1)
            {
                amount = Math.Round(amount, accuracy + 2);
            }

            multiplier -= accuracy;
            var res = (BigInteger)amount * BigInteger.Pow(10, multiplier);

            return res;
        }

        public static decimal ConvertFromContract(string amount, int multiplier, int accuracy)
        {
            if (accuracy > multiplier)
                throw new ArgumentException("accuracy > multiplier");

            multiplier -= accuracy;

            var val = BigInteger.Parse(amount);
            var res = (decimal)(val / BigInteger.Pow(10, multiplier));
            res /= (decimal)Math.Pow(10, accuracy);

            return res;
        }
    }
}
