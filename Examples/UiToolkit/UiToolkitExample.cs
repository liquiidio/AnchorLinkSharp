using System;
using System.Threading.Tasks;
using AnchorLinkSharp;
using AnchorLinkTransportSharp.Src;
using AnchorLinkTransportSharp.Src.StorageProviders;
using AnchorLinkTransportSharp.Src.Transports.UiToolkit;
using UnityEngine;

namespace AnchorLinkTransportSharp.Examples.UiToolkit
{
    public class UiToolkitExample : MonoBehaviour
    {
        // app identifier, should be set to the eosio contract account if applicable
        private const string Identifier = "uitoolkitexample";

        // assign UnityTransport through the editor
        [SerializeField] internal UnityUiToolkitTransport Transport;

        // initialize the link
        private AnchorLink _anchorLink;

        // the session instance, either restored using link.restoreSession() or created with link.login()
        internal LinkSession LinkSession;

        private void Start()
        {
            // create a new anchor link instance
            _anchorLink = new AnchorLink(new LinkOptions()
            {
                Transport = this.Transport,
                ChainId = "1064487b3cd1a897ce03ae5b6a865651747e2e152090f99c1d19d44e01aea5a4",
                Rpc = "https://api.wax.liquidstudios.io",
                ZlibProvider = new NetZlibProvider(),
                Storage = new PlayerPrefsStorage()
            });
        }

        // initialize a new session
        internal async Task StartSession()
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
        internal async Task Logout()
        {
            await LinkSession.Remove();
        }

        // tries to restore session, called when document is loaded
        internal async Task RestoreSession()
        {
            var restoreSessionResult = await _anchorLink.RestoreSession(Identifier);
            LinkSession = restoreSessionResult;

            if (LinkSession != null)
                Debug.Log($"{LinkSession.Auth.actor} logged-in");
        }

        // transfer tokens using a session  
        internal async Task Transfer(EosSharp.Core.Api.v1.Action action)
        {
            var transactResult = await LinkSession.Transact(new TransactArgs() { Action = action });

            print($"Transaction broadcast! {transactResult.Processed}");
        }

        // ask the user to sign the transaction and then broadcast to chain
        internal void Vote(EosSharp.Core.Api.v1.Action action)
        {
            _anchorLink.Transact(new TransactArgs() { Action = action }).ContinueWith(transactTask =>
            {
                Debug.Log($"Thank you {transactTask.Result.Signer.actor}");
            });
        }

        // ask the user to sign the transaction and then broadcast to chain
        internal async Task SellOrBuyRam(EosSharp.Core.Api.v1.Action action)
        {
            var transactResult = await LinkSession.Transact(new TransactArgs() { Action = action });

            print($"Transaction broadcast! {transactResult.Processed}");
        }
    }

}
