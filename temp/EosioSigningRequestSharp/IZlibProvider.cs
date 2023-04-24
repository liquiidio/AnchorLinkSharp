namespace EosioSigningRequest
{
    /** Interface that should be implemented by zlib implementations. */
    public interface IZlibProvider
    {
        /** Deflate data w/o adding zlib header. */
        byte[] DeflateRaw(byte[] data);

        /** Inflate data w/o requiring zlib header. */
        byte[] InflateRaw(byte[] data);
    }
}