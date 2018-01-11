using System;
using System.Collections.Generic;
using System.Text;

namespace Lykke.Service.EthereumCore.Services.Utils
{
    public static class DateUtils
    {
        public static DateTime UnixTimeStampToDateTimeUtc(double unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddSeconds(unixTimeStamp).ToUniversalTime();
            return dtDateTime;
        }
    }
}
