using System.IO;
using System.IO.Compression;
using EosioSigningRequest;

namespace Assets.Packages.AnchorLinkTransportSharp
{
    public class NetZlibProvider : IZlibProvider
    {
        public byte[] DeflateRaw(byte[] data)
        {
            using (var inputStream = new MemoryStream(data))
            {
                using (var outputStream = new MemoryStream())
                {
                    using (var compressor = new DeflateStream(outputStream, CompressionMode.Compress))
                    {
                        inputStream.CopyTo(compressor);
                    }

                    return outputStream.ToArray();
                }
            }

        }

        public byte[] InflateRaw(byte[] data)
        {
            using (var inputStream = new MemoryStream(data))
            {
                using (var outputStream = new MemoryStream())
                {
                    using (var decompressor = new DeflateStream(inputStream, CompressionMode.Decompress))
                    {
                        decompressor.CopyTo(outputStream);
                    }

                    return outputStream.ToArray();
                }
            }
        }
    }
}
