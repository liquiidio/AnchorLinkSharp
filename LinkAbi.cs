using System;

namespace AnchorLinkSharp
{
    public class SealedMessage
    {
        public string from;
        public long nonce;
        public string ciphertext;
        public int checksum;
    }

    public class LinkCreate
    {
        public string session_name;
        public string request_key;
    }

    public class LinkInfo
    {
        public DateTime expiration;
    }
}