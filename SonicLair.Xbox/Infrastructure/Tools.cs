using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Web;

namespace SonicLair.Infrastructure
{
    public static class Tools
    {


        public static string ToUrlEncodedParams(Dictionary<string, string> p)
        {

            if (!p.Values.Any())
            {
                return String.Empty;
            }

            List<string> strings = new List<string>();
            foreach (var key in p.Keys)
            {
                strings.Add($"{HttpUtility.UrlEncode(key)}={HttpUtility.UrlEncode(p[key])}");
            }

            return $"?{string.Join("&", strings)}";
        }

        public static string MillisecondsToTime(decimal value)
        {
            var s = value * 1000;
            var seconds = s % 60;
            var minutes = s / 60 - s % 60;
            return $"{minutes:00}:{seconds:00}";
        }

    }
}