using System;
using System.Linq;
using EosSharp.Core.Helpers;
using EosSharp.Core.Providers;

namespace AnchorLinkSharp
{
    public static class LinkUtils
    {
        public static byte[] AbiEncode(object value, string typeName)
        {
            var a = new AbiSerializationProvider();
            return a.SerializeTypeData(typeName, value, LinkAbiData.Types);
        }

        /**
         * Helper to ABI decode data.
         * @internal
         */
        public static TResultType AbiDecode<TResultType>(byte[] bytes, string typeName, TResultType result)
        {
            string data = null;
            var type = LinkAbiData.Types.types.SingleOrDefault(t => t.type == typeName);
            if (type == null)
            {
                throw new Exception($"No such type: { typeName }");
            }

            if (result is string stringData)
            {
                data = stringData;
            } else if (!(result is byte[])) {
                data = "";    // TODO Convert to byte-array and then to hex-string?
            }
            var a = new AbiSerializationProvider();
            return a.DeserializeStructData<TResultType>(typeName, data, LinkAbiData.Types);
        }

        /**
         * Encrypt a message using AES and shared secret derived from given keys.
         * @internal
         */
        public static byte[] SealMessage(string message, string privateKey, string publicKey) {
            var res  = CryptoHelper.AesEncrypt(CryptoHelper.PubKeyStringToBytes(privateKey), message); // TOOD is that right ?
            var data = new SealedMessage()
            {
                From = publicKey,
                Nonce = 0,
                Ciphertext = message,
                Checksum = 0,
            };
            return AbiEncode(data, "sealed_message");
        }

        /**
         * Ensure public key is in new PUB_ format.
         * @internal
         */
        public static string NormalizePublicKey(string key)
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
        public static bool PublicKeyEqual(string keyA, string keyB)
        {
            return NormalizePublicKey(keyA) == NormalizePublicKey(keyB);
        }

        /**
         * Generate a random private key.
         * Uses browser crypto if available, otherwise falls back to slow eosjs-ecc.
         * @internal
         */
        public static string GeneratePrivateKey()
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