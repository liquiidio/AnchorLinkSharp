using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using AnchorLinkSharp;
using EosioSigningRequest;
using Newtonsoft.Json;

namespace Assets.Packages.AnchorLinkTransportSharp
{
    public static class FuelSharp
    {
        public static readonly Dictionary<string, string> SupportedChains = new Dictionary<string, string>()
        {
            {"aca376f206b8fc25a6ed44dbdc66547c36c6c33e3a119ffbeaef943642f0e906", "https://eos.greymass.com"},
            {"2a02a0053e5a8cf73a56ba0fda11e4d92e0238a4a2aa74fccf46d5a910746840", "https://jungle3.greymass.com"},
            {"4667b205c6838ef70ff7988f6e8257e8be0e1284a2f59699054a018f743b1d11", "https://telos.greymass.com"},
        };

        public static async Task<FuelResponse> ApiCall(string url, object body)
        {
            using (HttpClient httpClient = new HttpClient())
            {
                var response = await httpClient.PostAsync(url, new StringContent(JsonConvert.SerializeObject(body)));
                return JsonConvert.DeserializeObject<FuelResponse>(await response.Content.ReadAsStringAsync());
            }
        }

        public static async Task<SigningRequest> Fuel(SigningRequest request, LinkSession session/*, void updatePrepareStatus*/) {
//            updatePrepareStatus('Detecting if Fuel is required.')
            var cloned = request.Clone();
            var chainId = cloned.GetChainId().ToLower();
            var nodeUrl = SupportedChains[chainId];
            if (nodeUrl == null)
            {
                throw new Exception("Chain does not support Fuel.");
            }
            var result = await ApiCall(nodeUrl + "/v1/cosigner/sign", new FuelRequest() {
                Request = cloned,
                Signer = session.Auth,
            });
            if (result.Signatures?.Count > 0)
            {
                cloned.SetInfoKey("fuel_sig", result.Signatures[0]);
            }
            else
            {
                throw new Exception("No signature returned from Fuel");
            }

            cloned.Data.Req = result.Request;
            return cloned;
        }
    }
}
