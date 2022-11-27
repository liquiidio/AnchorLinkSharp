using EosioSigningRequest;
using EosSharp.Core.Api.v1;

namespace AnchorLinkSharp
{
    public static class LinkConstants
    {
        /**
         * Format a EOSIO permission level in the format `actor@permission` taking placeholders into consideration.
         * @internal
         */
        public static string FormatAuth(PermissionLevel auth)
        {
            var actor = auth.actor;
            var permission = auth.permission;

            if (actor == SigningRequestConstants.PlaceholderName)
            {
                actor = "<any>";
            }
            if (permission == SigningRequestConstants.PlaceholderName || permission == SigningRequestConstants.PlaceholderPermission)
            {
                permission = "<any>";
            }

            return $"{actor}@{permission}";
        }
    }
}