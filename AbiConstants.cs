namespace EosioSigningRequest
{
    //! Static Class containing Constants used with the SigningRequest Package
    public static class AbiConstants
    {
        //! Constant to set the RequestFlags to None
        public const byte RequestFlagsNone = 0;
        //! Constant to set the RequestFlags to Broadcast
        public const byte RequestFlagsBroadcast = 1 << 0;
        //! Constant to set the RequestFlags to Background
        public const byte RequestFlagsBackground = 1 << 1;
    }
}