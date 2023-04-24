using System;
using System.Collections.Generic;
using System.Linq;
using EosSharp.Core.Api.v1;

namespace EosioSigningRequest
{
    //! Static Class containing Constants used with the SigningRequest Package
    public static class SigningRequestConstants
    {
        //! Dictionary with Chain-Names and Chain-IDs
        public static readonly Dictionary<ChainName, string> ChainIdLookup = new Dictionary<ChainName, string>() {
            {ChainName.Eos, "aca376f206b8fc25a6ed44dbdc66547c36c6c33e3a119ffbeaef943642f0e906"},
            {ChainName.Telos, "4667b205c6838ef70ff7988f6e8257e8be0e1284a2f59699054a018f743b1d11"},
            {ChainName.Jungle, "e70aaab8997e1dfce58fbfac80cbbb8fecec7b99cf982a9444273cbc64c41473"},
            {ChainName.Kylin, "5fff1dae8dc8e2fc4d5b23b2c7665c97f9e9d8edf2b6485a86ba311c25639191"},
            {ChainName.Worbli, "73647cde120091e0a4b85bced2f3cfdb3041e266cbbe95cee59b73235a1b3b6f"},
            {ChainName.Bos, "d5a3d18fbb3c084e3b1f3fa98c21014b5f3db536cc15d08f9f6479517c6a3d86"},
            {ChainName.Meetone, "cfe6486a83bad4962f232d48003b1824ab5665c36778141034d75e57b956e422"},
            {ChainName.Insights, "b042025541e25a472bffde2d62edd457b7e70cee943412b1ea0f044f88591664"},
            {ChainName.Beos, "b912d19a6abd2b1b05611ae5be473355d64d95aeff0c09bedc8c166cd6468fe4"},
            {ChainName.Wax, "1064487b3cd1a897ce03ae5b6a865651747e2e152090f99c1d19d44e01aea5a4"},
            {ChainName.Proton, "384da888112027f0321850a169f737c33e53b388aad48b5adace4bab97f437e0"},
            {ChainName.Fio, "21dcae42c0182200e93f954a074011f9048a7624c6fe81d3c9541a614a88bd1c"} 
        };

        //! Placeholder for an Account-Name, can be used if no Account-Name is specified
        public const string PlaceholderName = "............1";

        //! Placeholder for a Permission-Name, can be used if no Permission-Name is specified
        public const string PlaceholderPermission = "............2";

        //! Version of the Protocol implemented
        public static byte ProtocolVersion = 2;

        //! Placeholder for a Authorization, can be used if no Authorization is specified
        public static readonly PermissionLevel PlaceholderAuth = new PermissionLevel()
        {
            actor = PlaceholderName,
            permission = PlaceholderPermission
        };

        /// <param name="action">The action to be checked.</param>
        /// <summary>Checks if an action is an IdentityRequest.</summary>
        /// <returns>Returns true if the action passed is an IdentityRequest, returns false if not</returns>
        public static bool IsIdentity(EosSharp.Core.Api.v1.Action action)
        {
            return action.account == "" && action.name == "identity";
        }

        /// <param name="tx">The transaction to be checked.</param>
        /// <summary>Checks if an transaction has TAPOS (Transaction as Proof of Stake) properties set.</summary>
        /// <returns>Returns true if the transaction passed has TAPOS, returns false if not</returns>
        public static bool HasTapos(Transaction tx)
        {
            return !(tx.expiration == new DateTime(1970, 1, 1) && tx.ref_block_num == 0 && tx.ref_block_prefix == 0);
        }

        /// <param name="chainId">The chainId-Object to be converted, can either be of type string (Chain-Id or Chain-Name), ChainName-Enum, byte.</param>
        /// <summary>Converts the chainId-Object to a KeyValuePair<string, object>.</summary>
        /// <returns>Returns a KeyValuePair<string, object> defining the variant-name and the object</returns>
        public static KeyValuePair<string, object> VariantId(object chainId){
            if (chainId == null)
            {
                chainId = (byte)ChainName.Eos;
            }
            if (chainId is byte)
            {
                return new KeyValuePair<string, object>("chain_alias", chainId);
            }
            if (chainId is string chainIdString && ChainIdLookup.ContainsValue(chainIdString))
            {
                return new KeyValuePair<string, object>("chain_alias",
                    (byte)ChainIdLookup.FirstOrDefault(c => c.Value == chainIdString).Key);
            }
            if(chainId is string chainIdShortString && Enum.TryParse(chainIdShortString, out ChainName chainIdEnum))
            {
                // resolve known chain id's to their aliases
                if (chainIdEnum != ChainName.Unknown)
                    return new KeyValuePair<string, object>("chain_alias", (byte) chainIdEnum);
                return new KeyValuePair<string, object>("chain_id", ChainIdLookup[(ChainName)chainIdEnum]);
            }
            return new KeyValuePair<string, object>("chain_alias", chainId);
        }
    }
}