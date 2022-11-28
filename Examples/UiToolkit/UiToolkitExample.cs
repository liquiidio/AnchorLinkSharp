using System;
using System.Threading.Tasks;
using AnchorLinkSharp;
using Assets.Packages.AnchorLinkTransportSharp.Src;
using Assets.Packages.AnchorLinkTransportSharp.Src.StorageProviders;
using Assets.Packages.AnchorLinkTransportSharp.Src.Transports.UiToolkit;
using UnityEngine;

namespace Assets.Packages.AnchorLinkTransportSharp.Examples.UiToolkit
{
    public class UiToolkitExample : MonoBehaviour
    {
        // app identifier, should be set to the eosio contract account if applicable
        private const string Identifier = "uitoolkitexample";

        // Assign UnityTransport through the Editor
        [SerializeField] internal UnityUiToolkitTransport Transport;

        // initialize the link
        private AnchorLink _anchorLink;

        // the session instance, either restored using link.restoreSession() or created with link.login()
        public LinkSession LinkSession;


        public void Start()
        {
            _anchorLink = new AnchorLink(new LinkOptions()
            {
                Transport = this.Transport,

                //EOSIO.Token
                //ChainId = "aca376f206b8fc25a6ed44dbdc66547c36c6c33e3a119ffbeaef943642f0e906",
                //Rpc = "https://eos.greymass.com",

                //WAX.Token
                ChainId = "1064487b3cd1a897ce03ae5b6a865651747e2e152090f99c1d19d44e01aea5a4",
                Rpc = "https://wax.greymass.com",
                ZlibProvider = new NetZlibProvider(),
                Storage = new PlayerPrefsStorage()
                //chains: [{
                //    chainId: 'aca376f206b8fc25a6ed44dbdc66547c36c6c33e3a119ffbeaef943642f0e906',
                //    nodeUrl: 'https://eos.greymass.com',
                //}]

 
            });
        }

        public async Task StartSession()
        {
            try
            {
                var loginResult = await _anchorLink.Login(Identifier);

                LinkSession = loginResult.Session;
                Debug.Log($"{LinkSession.Auth.actor} logged-in");
            }
            catch (Exception ex)
            {
                Debug.LogError(ex.Message);
            }
        }


        // logout and remove session from storage
        public async Task Logout()
        {
            await LinkSession.Remove();
        }

        // tries to restore session, called when document is loaded
        public async Task RestoreSession()
        {
            var restoreSessionResult = await _anchorLink.RestoreSession(Identifier);
            LinkSession = restoreSessionResult;

            if (LinkSession != null)
                Debug.Log($"{LinkSession.Auth.actor} logged-in");
        }

        // transfer tokens using a session  
        public async Task Transfer(EosSharp.Core.Api.v1.Action action)
        {
            var transactResult = await LinkSession.Transact(new TransactArgs() { Action = action });

            print($"Transaction broadcast! {transactResult.Processed}");
        }

        // ask the user to sign the transaction and then broadcast to chain
        public void Vote(EosSharp.Core.Api.v1.Action action)
        {
            _anchorLink.Transact(new TransactArgs() { Action = action }).ContinueWith(transactTask =>
            {
                Debug.Log($"Thank you {transactTask.Result.Signer.actor}");
            });
        }

        // ask the user to sign the transaction and then broadcast to chain
        public async Task SellOrBuyRam(EosSharp.Core.Api.v1.Action action)
        {
            var transactResult = await LinkSession.Transact(new TransactArgs() { Action = action });
        }
    }

}
