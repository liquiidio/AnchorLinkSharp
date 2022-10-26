using EosSharp.Core.Api.v1;

namespace AnchorLinkSharp
{
    public abstract class LinkSessionDataBase
    {
        /** App identifier that owns the session. */
        public string Identifier { get; set; }

        /** Authenticated user permission. */
        public PermissionLevel Auth { get; set; }

        /** Public key of authenticated user */
        public string PublicKey { get; set; }
    }
}