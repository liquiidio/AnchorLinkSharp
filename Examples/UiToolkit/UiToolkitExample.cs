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
        /// <summary>
        /// App identifier, should be set to the eosio contract account if applicable
        /// </summary>
        private const string Identifier = "uitoolkitexample";

        /// <summary>
        /// Assign UnityTransport through the editor
        /// </summary>
        [SerializeField] internal UnityUiToolkitTransport Transport;

        /// <summary>
        /// Initialize the link
        /// </summary>
        private AnchorLink _anchorLink;

        /// <summary>
        /// The session instance, either restored using link.restoreSession() or created with link.login()
        /// </summary>
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

        /// <summary>
        /// Initialize a new session
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        /// Logout and remove session from storage
        /// </summary>
        /// <returns></returns>
        internal async Task Logout()
        {
            await LinkSession.Remove();
        }

        /// <summary>
        /// Tries to restore session, called when document is loaded
        /// </summary>
        /// <returns></returns>
        internal async Task RestoreSession()
        {
            var restoreSessionResult = await _anchorLink.RestoreSession(Identifier);
            LinkSession = restoreSessionResult;

            if (LinkSession != null)
                Debug.Log($"{LinkSession.Auth.actor} logged-in");
        }

        /// <summary>
        /// Transfer tokens using a session  
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        internal async Task Transfer(EosSharp.Core.Api.v1.Action action)
        {
            var transactResult = await LinkSession.Transact(new TransactArgs() { Action = action });

            print($"Transaction broadcast! {transactResult.Processed}");
        }

        /// <summary>
        /// Ask the user to sign the transaction and then broadcast to chain
        /// </summary>
        /// <param name="action"></param>
        internal void Vote(EosSharp.Core.Api.v1.Action action)
        {
            _anchorLink.Transact(new TransactArgs() { Action = action }).ContinueWith(transactTask =>
            {
                Debug.Log($"Thank you {transactTask.Result.Signer.actor}");
            });
        }

        /// <summary>
        /// Ask the user to sign the transaction and then broadcast to chain
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        internal async Task SellOrBuyRam(EosSharp.Core.Api.v1.Action action)
        {
            var transactResult = await LinkSession.Transact(new TransactArgs() { Action = action });

            print($"Transaction broadcast! {transactResult.Processed}");
        }
    }

}
