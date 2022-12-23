using EosSharp;

namespace EosioSigningRequest
{
    public class SigningRequestEncodingOptions
    {
        /** UTF-8 text encoder, required when using node.js. */
        //textEncoder?: any
        /** UTF-8 text decoder, required when using node.js. */
        //textDecoder?: any
        
        /** Optional zlib, if provided the request will be compressed when encoding. */
        public IZlibProvider Zlib;

        /** Abi provider, required if the arguments contain un-encoded actions. */
        public IAbiSerializationProvider AbiSerializationProvider;
        
        /** Optional signature provider, will be used to create a request signature if provided. */
        public ISignProvider SignatureProvider;
    }
}