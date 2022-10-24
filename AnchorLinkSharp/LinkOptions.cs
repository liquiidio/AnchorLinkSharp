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
        ILinkTransport transport { get; set; }

        /**
         * ChainID or esr chain name alias for which the anchorLink is valid.
         * Defaults to EOS (aca376f206b8fc25a6ed44dbdc66547c36c6c33e3a119ffbeaef943642f0e906).
         */
        object chainId { get; set; }

        /**
         * URL to EOSIO node to communicate with or e EosApi instance.
         * Defaults to https://eos.greymass.com
         */
        object rpc { get; set; }

        /**
         * URL to anchorLink callback service.
         * Defaults to https://cb.anchor.link.
         */
        string service { get; set; }

        /**
         * Optional storage adapter that will be used to persist sessions if set.
         * If not storage adapter is set but the given transport provides a storage, that will be used.
         * Explicitly set this to `null` to force no storage.
         */
        ILinkStorage storage { get; set; }

        IZlibProvider ZlibProvider { get; set; }
    }

    public class LinkOptions : ILinkOptions
    {
        public ILinkTransport transport { get; set; }
        public object chainId { get; set; }
        public object rpc { get; set; }
        public string service { get; set; } = "https://cb.anchor.link";
        public ILinkStorage storage { get; set; }
        public IZlibProvider ZlibProvider { get; set; }
    }

    public static class Defaults
    {
        public static string chainId = "aca376f206b8fc25a6ed44dbdc66547c36c6c33e3a119ffbeaef943642f0e906";
        public static string rpc = "https://eos.greymass.com";
        public static string service = "https://cb.anchor.link";
    }
}