using System.Threading.Tasks;
using AnchorLinkSharp;
using UnityEngine;

namespace Assets.Packages.AnchorLinkTransportSharp
{
    public class PlayerPrefsStorage : ILinkStorage
    {
        private readonly string _keyPrefix;
        public PlayerPrefsStorage(string keyPrefix = "default-prefix")
        {
            this._keyPrefix = keyPrefix;
        }

        private string StorageKey(string key)
        {
            return $"{this._keyPrefix}{key}";
        }

        public async Task Write(string key, string data)
        {
            await Task.Run(() => { PlayerPrefs.SetString(this.StorageKey(key), data); });
        }

        public async Task<string> Read(string key)
        {
            return await Task.Run(() => PlayerPrefs.HasKey(this.StorageKey(key))
                ? PlayerPrefs.GetString(this.StorageKey(key))
                : null);
        }

        public async Task Remove(string key)
        {
            if(PlayerPrefs.HasKey(this.StorageKey(key)))
                await Task.Run(() => { PlayerPrefs.DeleteKey(this.StorageKey(key)); });
        }
    }
}