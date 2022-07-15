using System;
using System.Linq;
using System.Text;

namespace SonicLair.Lib.Infrastructure
{
    public static class TokenGenerator
    {
        private static Random random = new Random();

        public static string GetTokenizedPassword(string password, string salt)
        {
            return MD5($"{password}{salt}");
        }
        
        
        public static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray()).ToLower();
        }

        private static string MD5(string input)
        {
            // Use input string to calculate MD5 hash
            System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create();
            byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
            byte[] hashBytes = md5.ComputeHash(inputBytes);

            // Convert the byte array to hexadecimal string
            StringBuilder sb = new StringBuilder();
            foreach (var t in hashBytes)
            {
                sb.Append(t.ToString("X2"));
            }
            md5.Dispose();
            return sb.ToString().ToLower();

        }
    }
}