using System;

namespace EosioSigningRequest
{
    public static class Base64EncodingUtility
    {

        private static readonly char[] padding = { '=' };

        /// <summary>
        /// Encodes the binary to URL safe base64.
        /// </summary>
        /// <param name="buffer">The binary to encode</param>
        /// <returns>Returns the URL safe base64 encoded string</returns>
        public static string ToBase64UrlSafe(byte[] buffer)
        {
            return Convert.ToBase64String(buffer).TrimEnd(padding).Replace('+', '-').Replace('/', '_');
        }

        /// <summary>
        /// Decodes the URL safe base64 encoded string to binary.
        /// </summary>
        /// <param name="base54Text">The URL safe base64 string</param>
        /// <returns>Returns the decoded binary data</returns>
        public static byte[] FromBase64UrlSafe(string base54Text)
        {
            var incoming = base54Text.Replace('_', '/').Replace('-', '+');
            switch (base54Text.Length % 4)
            {
                case 2: incoming += "=="; break;
                case 3: incoming += "="; break;
            }
            return Convert.FromBase64String(incoming);
        }

        /// <summary>
        /// Convert a base64 encoded string with `fc` prefix to binary.
        /// </summary>
        /// <param name="str">The base64 encoded string with `fc` prefix</param>
        /// <returns>Returns the binary data</returns>
        public static byte[] FromBase64FcString(string str)
        {
            if ((str.Length & 3) == 1 && str[str.Length - 1] == '=')
            {
                return Convert.FromBase64String(str.Substring(0, str.Length - 1));
            }

            return Convert.FromBase64String(str);
        }

    }
}