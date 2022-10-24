using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using AnchorLinkSharp;
using Newtonsoft.Json;

namespace AnchorLinkUnityTransportSharp
{
    public class JsonLocalStorage : ILinkStorage
    {
        private string _prefix;
        private string _filePath;
        public JsonLocalStorage(string prefix = "default-prefix")
        {
            this._prefix = prefix;
            _filePath = $"{Environment.CurrentDirectory}/{_prefix}_{Environment.UserName}.json";
        }

        private async Task<Dictionary<string, string>> ReadStorage()
        {
            if (File.Exists(_filePath))
                return JsonConvert.DeserializeObject<Dictionary<string, string>>(
                    await File.ReadAllTextAsync(_filePath)); 
            return new Dictionary<string, string>();
        }

        private async Task WriteStorage(Dictionary<string, string> storage)
        {
            await File.WriteAllTextAsync(_filePath, JsonConvert.SerializeObject(storage));
        }

        public async Task write(string key, string data)
        {
            var storage = await ReadStorage();
            storage[key] = data;
            await WriteStorage(storage);
        }

        public async Task<string> read(string key)
        {
            var storage = await ReadStorage();
            return storage.TryGetValue(key, out var value) ? value : string.Empty;
        }

        public async Task remove(string key)
        {
            var storage = await ReadStorage();
            if (storage.ContainsKey(key))
                storage.Remove(key);
            await WriteStorage(storage);
        }
    }
}
