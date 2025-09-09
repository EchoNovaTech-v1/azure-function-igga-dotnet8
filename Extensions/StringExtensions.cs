using System.Security.Cryptography;
using System.Text;

namespace Extensions.Cryptography
{
    public static class StringExtensions
    {
        public static string CreateMD5(this string str)
        {
            MD5 md5 = MD5.Create();
            byte[] bytes = Encoding.ASCII.GetBytes(str);
            byte[] hash = md5.ComputeHash(bytes);
            StringBuilder sb = new StringBuilder();
            foreach (var b in hash)
            {
                sb.AppendFormat("{0:x2}", b);
            }
            return sb.ToString();
        }
    }
}
