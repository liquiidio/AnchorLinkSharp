using EosioSigningRequest;

namespace AnchorLinkSharp
{
    /**
     * Available options when creating a new [[AnchorLink]] instance.
     */
    public interface ILinkOptions
    {
        /**
         * AnchorLink transport responsible for presenting signing requests to user, required.
         */
        ILinkTransport Transport { get; set; }

        /**
         * ChainID or esr chain name alias for which the anchorLink is valid.
         * Defaults to EOS (aca376f206b8fc25a6ed44dbdc66547c36c6c33e3a119ffbeaef943642f0e906).
         */
        string ChainId { get; set; }

        /**
         * URL to EOSIO node to communicate with or e EosApi instance.
         * Defaults to https://eos.greymass.com
         */
        object Rpc { get; set; }

        /**
         * URL to anchorLink callback service.
         * Defaults to https://cb.anchor.link.
         */
        string Service { get; set; }

        /**
         * Optional storage adapter that will be used to persist sessions if set.
         * If not storage adapter is set but the given transport provides a storage, that will be used.
         * Explicitly set this to `null` to force no storage.
         */
        ILinkStorage Storage { get; set; }

        IZlibProvider ZlibProvider { get; set; }
    }
}