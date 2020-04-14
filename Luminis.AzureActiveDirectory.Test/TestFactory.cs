using System;
using System.Security.Cryptography;
using System.Text;

namespace Luminis.AzureActiveDirectory.Test
{
    public static class TestFactory
    {
        public static string StringToGUID(string value)
        {
            // Create a new instance of the MD5CryptoServiceProvider object.
            var md5Hasher = MD5.Create();
            // Convert the input string to a byte array and compute the hash.
            var data = md5Hasher.ComputeHash(Encoding.Default.GetBytes(value));
            return new Guid(data).ToString();
        }
    }
}
