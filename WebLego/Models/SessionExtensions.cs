using Microsoft.AspNetCore.Http;
using System;
using System.IO;

namespace WebLego.Models
{
    public static class SessionExtensions
    {
        public static void SetDecimal(this ISession session, string key, decimal value)
        {
            var bytes = DecimalToBytes(value);
            session.Set(key, bytes);
        }

        public static decimal? GetDecimal(this ISession session, string key)
        {
            var data = session.Get(key);
            if (data == null || data.Length != 16)
            {
                return null;
            }
            return BytesToDecimal(data);
        }

        private static byte[] DecimalToBytes(decimal value)
        {
            var bits = decimal.GetBits(value); // Trả về 4 int
            var bytes = new byte[16];
            for (int i = 0; i < 4; i++)
            {
                Array.Copy(BitConverter.GetBytes(bits[i]), 0, bytes, i * 4, 4);
            }
            return bytes;
        }

        private static decimal BytesToDecimal(byte[] bytes)
        {
            int[] bits = new int[4];
            for (int i = 0; i < 4; i++)
            {
                bits[i] = BitConverter.ToInt32(bytes, i * 4);
            }
            return new decimal(bits);
        }
    }
}
