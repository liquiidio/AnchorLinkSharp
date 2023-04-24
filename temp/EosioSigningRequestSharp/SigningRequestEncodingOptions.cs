using EosSharp;
using EosSharp.Core.Interfaces;
using EosSharp.Core.Providers;

namespace EosioSigningRequest
{
    public class SigningRequestEncodingOptions
    {
        /** Optional zlib, if provided the request will be compressed when encoding. */
        public IZlibProvider Zlib;

        /** Abi provider, required if the arguments contain un-encoded actions. */
        public IAbiSerializationProvider AbiSerializationProvider;
        
        /** Optional signature provider, will be used to create a request signature if provided. */
        public ISignProvider SignatureProvider;
    }
}