using System.Threading.Tasks;

namespace AnchorLinkSharp
{
    /**
     * Interface storage adapters should implement.
     *
     * LinkStorage adapters are responsible for persisting [[LinkSession]]'s and can optionally be
     * passed to the [[AnchorLink]] constructor to auto-persist sessions.
     */
    public interface ILinkStorage
    {
        /** Write string to storage at key. Should overwrite existing values without error. */
        Task Write(string key, string data);

        /** Read key from storage. Should return `null` if key can not be found. */
        Task<string> Read(string key);

        /** Delete key from storage. Should not error if deleting non-existing key. */
        Task Remove(string key);
    }
}