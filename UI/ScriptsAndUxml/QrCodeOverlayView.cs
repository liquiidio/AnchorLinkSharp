using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using AnchorLinkSharp;
using Assets.Packages.AnchorLinkTransportSharp;
using EosSharp.Core.Api.v1;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.UIElements;

namespace Assets.Packages.AnchorLinkTransportSharp.UI.ScriptsAndUxml
{
    public class QrCodeOverlayView : ScreenBase
    {
        /*
         * Connected Views
         */

        public SigningTimerOverlayView SigningTimerOverlayView;
        public SuccessOverlayView SuccessOverlayView;
        public FailureOverlayView FailureOverlayView;
        public TimeoutOverlayView TimeoutOverlayView;
        public LoggedView LoggedView; 


        /*
         * Child-Controls
         */

        private Button _closeViewButton;
        private Button _launchAnchorButton;

        private Label _downloadNowLabel;
        private Label _versionLabel;
        private Label _loginTitleLabel;
        private Label _subtitleLabel;


        /*
         * Fields, Properties
         */
        private AnchorLink _link;
        private EosSharp.Core.Api.v1.Action _action;

        // app Identifier, should be set to the eosio contract account if applicable
        private const string Identifier = "example";


        void Start()
        {
            _closeViewButton = Root.Q<Button>("close-view-button");
            _launchAnchorButton = Root.Q<Button>("launch-anchor-button");
            _downloadNowLabel = Root.Q<Label>("download-now-link-label");
            _versionLabel = Root.Q<Label>("version-label");
            _loginTitleLabel = Root.Q<Label>("anchor-link-title-label");
            _subtitleLabel = Root.Q<Label>("anchor-link-subtitle-label");

            BindButtons();
        }


        #region Button Binding
        private void BindButtons()
        {
            _closeViewButton.clickable.clicked += Hide;

            _downloadNowLabel.RegisterCallback<ClickEvent>(evt =>
            {
                Application.OpenURL(DownloadAnchorUrl);
            });

            _versionLabel.RegisterCallback<ClickEvent>(evt =>
            {
                Application.OpenURL(VersionUrl);
            });

            //_launchAnchorButton.clickable.clicked += async () =>
            //{
            //    try
            //    {
            //        await Login();
            //        //try
            //        //{
            //        //    // throws if the account doesn't have enough CPU
            //        //    await Transfer();
            //        //}
            //        //catch (Exception e)
            //        //{
            //        //    Debug.Log(JsonConvert.SerializeObject(e));
            //        //}
            //        //// logout removes the session so it's not restorable
            //        //// await loginExample.logout();
            //        //await RestoreSession();
            //    }
            //    catch (Exception e)
            //    {
            //        Debug.Log(e);
            //        throw;
            //    }
            //    Hide();
            //    SuccessOverlayView.Show();

            //    try
            //    {
            //        Vote();
            //    }
            //    catch (Exception e)
            //    {
            //        Debug.Log(e);
            //        throw;
            //    }
            //    SigningTimerOverlayView.Show();
            //    SigningTimerOverlayView.StartCountdownTimer();
            //};


            //login to your anchor wallet and a session is created for your account.
            _launchAnchorButton.clickable.clicked += async () =>
            {
                try
                {
                    await Login();
                    //await RestoreSession();
                }
                catch (Exception e)
                {
                    Debug.Log(e);
                    throw;
                }
                SuccessOverlayView.Show();

                //try
                //{
                //    Vote();
                //}
                //catch (Exception e)
                //{
                //    Debug.Log(e);
                //    throw;
                //}
                Hide();
                LoggedView.Show();
                LoggedView.Rebind(Session);
                SuccessOverlayView.Hide();
            };
        }
        #endregion

        #region Rebind

        public void Rebind()
        {
            _versionLabel.text = Version;

            _loginTitleLabel.text = "Login";
            _subtitleLabel.text = "Scan the QR-code with Anchor on another device or use the button to open it here.";

            _link = new AnchorLink(new LinkOptions()
            {
                Transport = new UnityUiToolkitTransport(new TransportOptions()),
                ChainId = "aca376f206b8fc25a6ed44dbdc66547c36c6c33e3a119ffbeaef943642f0e906",
                Rpc = "https://eos.greymass.com",
                ZlibProvider = new NetZlibProvider(),
                Storage = new JsonLocalStorage()
            });

            _action = new EosSharp.Core.Api.v1.Action()
            {
                account = "eosio",
                name = "voteproducer",
                authorization = new List<PermissionLevel>()
                {
                    new PermissionLevel()
                    {
                        actor = "............1", // ............1 will be resolved to the signing accounts permission
                        permission = "............2" // ............2 will be resolved to the signing accounts authority
                    }
                },
                data = new Dictionary<string, object>()
                {
                    { "voter", "............1" },
                    { "proxy", "coredevproxy" },
                    { "producers", Array.Empty<object>() },
                }
            };
        }

        #endregion

        #region other


        // login and store session if successful
        private async Task Login()
        {
            var loginResult = await _link.Login(Identifier);
            Session = loginResult.Session;
            DidLogin();
        }

        // tries to restore session, called when document is loaded
        public async Task RestoreSession()
        {
            var restoreSessionResult = await _link.RestoreSession(Identifier);
            Session = restoreSessionResult;
            if (Session != null)
                DidLogin();
        }

        // transfer tokens using a session
        public async Task Transfer()
        {
            var action = new EosSharp.Core.Api.v1.Action()
            {
                account = "wax.token",
                name = "transfer",
                authorization = new List<PermissionLevel>() { Session.Auth },
                data = new Dictionary<string, object>()
                {
                    { "from", Session.Auth.actor },
                    { "to", "test3.liq" },
                    { "quantity", "0.0001 EOS" },
                    { "memo", "Anchor is the best! Thank you <3" }
                }
            };

            var transactResult = await Session.Transact(new TransactArgs() { Action = action });
            Debug.Log($"Transaction broadcast! {transactResult.Processed}");
        }

        // logout and remove session from storage
        //logout removes the session so it's not restorable
        public async Task Logout()
        {
            await Session.Remove();
        }

        // called when session was restored or created
        public void DidLogin()
        {
            Debug.Log($"{Session.Auth.actor} logged-in");
        }

        // ask the user to sign the transaction and then broadcast to chain
        public void Vote()
        {
            _link.Transact(new TransactArgs() { Action = _action }).ContinueWith(transactTask =>
            {
                Debug.Log($"Thank you {transactTask.Result.Signer.actor}");
            });
        }


        public void SignManual()
        {
            _loginTitleLabel.text = "Sign Manually";
            _subtitleLabel.text = "Want to sign with another device or didn’t get the signing request in your wallet, scan this QR or copy request and paste in app.";
        }
        #endregion
    }
}
