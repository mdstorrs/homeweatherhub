using System;
using System.IO;
using System.Text;
using System.Security.Cryptography;

namespace Home_Weather_Hub.App_Code
{
    public class Encryption
    {
        public static string MD5Hash(string data)
        {
            MD5 md5 = MD5.Create();
            byte[] hashData = md5.ComputeHash(Encoding.Default.GetBytes(data));
            return BytesToHexString(hashData);
        }

        public static string SHA256Hash(string data)
        {
            SHA256 md5 = SHA256.Create();
            byte[] hashData = md5.ComputeHash(Encoding.Default.GetBytes(data));

            return BytesToHexString(hashData);
        }

        public static string Encrypt(string text)
        {
            string result = null;

            result = TripleDES.Encrypt(text);

            return result;
        }

        public static string Decrypt(string text)
        {
            string result = null;

            result = TripleDES.Decrypt(text);

            return result;
        }

        private static string BytesToHexString(byte[] bytes)
        {
            StringBuilder sb = new StringBuilder(bytes.Length * 2);

            foreach (byte b in bytes)
                sb.AppendFormat("{0:x2}", b);

            return sb.ToString().ToUpper();
        }

        private static byte[] HexStrinToBytes(string hexString)
        {
            int length = hexString.Length;
            int upperBound = length / 2;

            if (length % 2 == 0)
                upperBound -= 1;
            else
                hexString = "0" + hexString;

            byte[] bytes = new byte[upperBound + 1];
            for (int i = 0; i <= upperBound; i++)
                bytes[i] = Convert.ToByte(hexString.Substring(i * 2, 2), 16);

            return bytes;
        }


        internal class TripleDES
        {
            private static byte[] m_key = new[] { (byte)1, (byte)2, (byte)3, (byte)4, (byte)5, (byte)6, (byte)7, (byte)8, (byte)9, (byte)10, (byte)11, (byte)12, (byte)13, (byte)14, (byte)15, (byte)16, (byte)17, (byte)18, (byte)19, (byte)20, (byte)21, (byte)22, (byte)23, (byte)24 };
            private static byte[] m_iv = new[] { (byte)1, (byte)2, (byte)3, (byte)4, (byte)5, (byte)6, (byte)7, (byte)8 };

            public static string Encrypt(string text)
            {
                using (TripleDESCryptoServiceProvider tripleDES = new TripleDESCryptoServiceProvider())
                {
                    byte[] input = UTF8Encoding.UTF8.GetBytes(text);
                    byte[] output = Transform(input, tripleDES.CreateEncryptor(m_key, m_iv));
                    return Convert.ToBase64String(output);
                }
            }

            public static string Decrypt(string text)
            {
                using (TripleDESCryptoServiceProvider tripleDES = new TripleDESCryptoServiceProvider())
                {
                    byte[] input = Convert.FromBase64String(text);
                    byte[] output = Transform(input, tripleDES.CreateDecryptor(m_key, m_iv));

                    return UTF8Encoding.UTF8.GetString(output);
                }
            }

            private static byte[] Transform(byte[] input, ICryptoTransform CryptoTransform)
            {

                // create the necessary streams
                using (MemoryStream memStream = new MemoryStream())
                using (CryptoStream cryptStream = new CryptoStream(memStream, CryptoTransform, CryptoStreamMode.Write)
    )
                {

                    // transform the bytes as requested
                    cryptStream.Write(input, 0, input.Length);
                    cryptStream.FlushFinalBlock();

                    // Read the memory stream and convert it back into byte array
                    memStream.Position = 0;
                    byte[] result = new byte[System.Convert.ToInt32(memStream.Length - 1) + 1];
                    memStream.Read(result, 0, System.Convert.ToInt32(result.Length));

                    // close and release the streams
                    memStream.Close();
                    cryptStream.Close();

                    // hand back the encrypted buffer
                    return result;
                }
            }
        }
    }
}