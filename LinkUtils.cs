using System;
using System.Linq;
using EosSharp.Core.Helpers;
using EosSharp.Core.Providers;

namespace AnchorLinkSharp
{
    public static class LinkUtils
    {
        public static byte[] abiEncode(object value, string structName)
        {
            AbiSerializationProvider a = new AbiSerializationProvider();
            return a.SerializeStructData(structName, value, LinkAbiData.Types);
        }

        /**
         * Helper to ABI decode data.
         * @internal
         */
        public static TResultType abiDecode<TResultType>(byte[] Bytes, string typeName, TResultType data)
        {
            string _data = null;
            var type = LinkAbiData.Types.types.SingleOrDefault(t => t.type == typeName);
            if (type == null)
            {
                throw new Exception($"No such type: { typeName }");
            }

            if (data is string stringData)
            {
                _data = stringData;
            } else if (!(data is byte[])) {
                _data = "";    // TODO Convert to byte-array and then to hex-string?
            }
            AbiSerializationProvider a = new AbiSerializationProvider();
            return a.DeserializeStructData<TResultType>(typeName, _data, LinkAbiData.Types);
        }

        /**
         * Encrypt a message using AES and shared secret derived from given keys.
         * @internal
         */
        public static byte[] sealMessage(string message, string privateKey, string publicKey) {
            var res  = CryptoHelper.AesEncrypt(CryptoHelper.PubKeyStringToBytes(privateKey), message); // TOOD is that right ?
            var data = new SealedMessage()
            {
                from = publicKey,
                nonce = 0,
                ciphertext = message,
                checksum = 0,
            };
            return abiEncode(data, "sealed_message");
        }

        /**
         * Ensure public key is in new PUB_ format.
         * @internal
         */
        public static string normalizePublicKey(string key)
        {
            if (key.StartsWith("PUB_"))
            {
                return key;
            }

            return "";
// TODO            return Numeric.publicKeyToString(Numeric.stringToPublicKey('EOS' + key.substr(-50)))
        }

        /**
         * Return true if given public keys are equal.
         * @internal
         */
        public static bool publicKeyEqual(string keyA, string keyB)
        {
            return normalizePublicKey(keyA) == normalizePublicKey(keyB);
        }

        /**
         * Generate a random private key.
         * Uses browser crypto if available, otherwise falls back to slow eosjs-ecc.
         * @internal
         */
        public static string generatePrivateKey()
        {
            return CryptoHelper.GenerateKeyPair().PrivateKey;
/*            if (typeof window != = 'undefined' && window.crypto) {
                const data  = new Uint32Array(32)
                window.crypto.getRandomValues(data)
                return ecc.PrivateKey.fromBuffer(Buffer.from(data)).toString()
            } else {
                return await ecc.randomKey()
            }*/
        }
    }
}