namespace AnchorLinkUnityTransportSharp
{
    public class TransportOptions
    {
        /** Whether to display request success and error messages, defaults to true */
        public bool RequestStatus;
        /** Local storage prefix, defaults to `anchor-anchorLink`. */
        public string StoragePrefix;

        /**
         * Whether to use Greymass Fuel for low resource accounts, defaults to false.
         * Note that this service is not available on all networks.
         * Visit https://greymass.com/en/fuel for more information.
         */
        public bool DisableGreymassFuel;
    }
}