using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace SonicLair.Lib.Infrastructure
{
    public class StaticHelpers
    {
        protected StaticHelpers()
        { }

        public static Tuple<int, int> GetRowCol(int index, List<int> sizes)
        {
            int i = 0;
            while (sizes[i] <= index)
            {
                i++;
            }
            var row = i;
            var col = index - ((i == 0) ? 0 : sizes[i - 1]);
            return new Tuple<int, int>(row, col);
        }

        public static JsonSerializerSettings GetJsonSerializerSettings()
        {
            DefaultContractResolver contractResolver = new DefaultContractResolver
            {
                NamingStrategy = new CamelCaseNamingStrategy(),
            };

            return new JsonSerializerSettings()
            {
                ContractResolver = contractResolver,
                Culture = new System.Globalization.CultureInfo("en-US")
            };
        }

        public static byte[] GetLocalIpAsArray(string localIp)
        {
            var strings = localIp.Split('.');
            if (strings.Length != 4)
            {
                return new byte[0];
            }
            var ret = new byte[4];
            for (int i = 0; i < 4; i++)
            {
                ret[i] = (byte)int.Parse(strings[i]);
            }
            return ret;
        }

        public static byte[] GetLocalIpAsArray()
        {
            var strings = GetLocalIp().Split('.');
            if (strings.Length != 4)
            {
                return new byte[0];
            }
            var ret = new byte[4];
            for (int i = 0; i < 4; i++)
            {
                ret[i] = (byte)int.Parse(strings[i]);
            }
            return ret;
        }

        public static string GetLocalIp()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork &&
                      (ip.ToString().StartsWith("192.168")
                    || ip.ToString().StartsWith("10.")
                    || ip.ToString().StartsWith("172."))) 
                {
                    return ip.ToString();
                }
            }
            return "127.0.0.1";
        }
    }
}