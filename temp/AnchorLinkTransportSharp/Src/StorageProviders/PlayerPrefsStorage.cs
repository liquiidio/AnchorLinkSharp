using System.Threading.Tasks;
using AnchorLinkSharp;
using UnityEngine;

namespace AnchorLinkTransportSharp.Src.StorageProviders
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

        public Task Write(string key, string data)
        {
            PlayerPrefs.SetString(this.StorageKey(key), data);
            return Task.CompletedTask;
        }

        public Task<string> Read(string key)
        {
            return Task.FromResult(PlayerPrefs.HasKey(this.StorageKey(key))
                ? PlayerPrefs.GetString(this.StorageKey(key))
                : null);
        }

        public Task Remove(string key)
        {
            if(PlayerPrefs.HasKey(this.StorageKey(key)))
               PlayerPrefs.DeleteKey(this.StorageKey(key));
            return Task.CompletedTask;
        }
    }
}