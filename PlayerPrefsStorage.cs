using System.Threading.Tasks;
using AnchorLinkSharp;

namespace AnchorLinkUnityTransportSharp
{
    public class PlayerPrefsStorage : ILinkStorage
    {
        private readonly string _keyPrefix;
        public PlayerPrefsStorage(string keyPrefix = "default-prefix")
        {
            this._keyPrefix = keyPrefix;
        }

        string StorageKey(string key)
        {
            return $"{this._keyPrefix}{key}";
        }

        public async Task Write(string key, string data)
        {
            await Task.Run(() => { }); // PlayerPrefs.SetString(this.storageKey(key), data.ToString()); });
        }

        public async Task<string> Read(string key)
        {
            return await Task.Run(() => { return "";/*PlayerPrefs.GetString(this.storageKey(key))*/; });
        }

        public async Task Remove(string key)
        {
            await Task.Run(() => { /*PlayerPrefs.DeleteKey(this.storageKey(key));*/ });
        }
    }
}