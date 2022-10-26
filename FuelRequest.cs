using EosioSigningRequest;
using EosSharp.Core.Api.v1;

namespace AnchorLinkUnityTransportSharp
{
    public class FuelRequest
    {
        public SigningRequest Request;
        public PermissionLevel Signer;
    }
}