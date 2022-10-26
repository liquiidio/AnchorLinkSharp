using System.Collections.Generic;
using EosSharp.Core.Api.v1;

namespace AnchorLinkSharp
{
    public class LinkSignatureProviderArgs
    {
        public Dictionary<string, Abi> Abis;
        public string ChainId;
        public string[] RequiredKeys;
        public byte[] SerializedContextFreeData;
        public byte[] SerializedTransaction;
    }
}