using EosioSigningRequest;
using EosSharp.Core.Api.v1;

namespace Assets.Packages.AnchorLinkTransportSharp
{
    public class FuelRequest
    {
        public SigningRequest Request;
        public PermissionLevel Signer;
    }
}