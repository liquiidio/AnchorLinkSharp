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
    public class LoggedView : ScreenBase
    {
        /*
         * Connected Views
         */

        public SigningTimerOverlayView SigningTimerOverlayView;
        public SuccessOverlayView SuccessOverlayView;
        public FailureOverlayView FailureOverlayView;


        /*
         * Child-Controls
         */

        private Button _transferTokenButton;

        private Label _accountLabel;
        private Label _versionLabel;
        private Label _loginTitleLabel;
        private Label _subtitleLabel;


        /*
         * Fields, Properties
         */
        private LinkSession _session;
        private AnchorLink _link;
        private EosSharp.Core.Api.v1.Action _action;

        // app identifier, should be set to the eosio contract account if applicable
        private const string Identifier = "example";


        void Start()
        {
            _transferTokenButton = Root.Q<Button>("transfer-token-button");
            _accountLabel = Root.Q<Label>("account-label");
            _versionLabel = Root.Q<Label>("version-label");
            _loginTitleLabel = Root.Q<Label>("anchor-link-title-label");
            _subtitleLabel = Root.Q<Label>("anchor-link-subtitle-label");

            _versionLabel.text = Version;

            BindButtons();
        }


        #region Button Binding
        private void BindButtons()
        {
            _transferTokenButton.clickable.clicked += async () =>
            {
                //await RestoreSession();

                //try
                //{
                //    // throws if the account doesn't have enough CPU
                //    await Transfer();
                //}
                //catch (Exception e)
                //{
                //    Debug.Log(JsonConvert.SerializeObject(e));
                //}

                //try
                //{
                //    Vote();
                //}
                //catch (Exception e)
                //{
                //    Debug.Log(e);
                //    throw;
                //}
                //SigningTimerOverlayView.Show();
                //SigningTimerOverlayView.StartCountdownTimer();



                try
                {
                    await Login();
                    await RestoreSession();
                    try
                    {
                        // throws if the account doesn't have enough CPU
                        await Transfer();
                    }
                    catch (Exception e)
                    {
                        Debug.Log(JsonConvert.SerializeObject(e));
                    }

                }
                catch (Exception e)
                {
                    Debug.Log(e);
                    throw;
                }
                Hide();
                SuccessOverlayView.Show();

                try
                {
                    Vote();
                }
                catch (Exception e)
                {
                    Debug.Log(e);
                    throw;
                }
                SigningTimerOverlayView.Show();
                SigningTimerOverlayView.StartCountdownTimer();
            };

            _versionLabel.RegisterCallback<ClickEvent>(evt =>
            {
                Application.OpenURL(VersionUrl);
            });
        }
        #endregion

        #region Rebind

        public void Rebind(LinkSession session)
        {
            _accountLabel.text = session.Auth.actor;
            _session = session;

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

        // tries to restore session, called when document is loaded
        public async Task RestoreSession()
        {
            var restoreSessionResult = await _link.RestoreSession(Identifier);
            _session = restoreSessionResult;
            if (_session != null)
                DidLogin();
        }

        // logout and remove session from storage
        public async Task Logout()
        {
            await Session.Remove();
        }

        // called when session was restored or created
        public void DidLogin()
        {
            Debug.Log($"{_session.Auth.actor} logged-in");
        }

        // transfer tokens using a session
        public async Task Transfer()
        {
            var action = new EosSharp.Core.Api.v1.Action()
            {
                account = "wax.token",
                name = "transfer",
                authorization = new List<PermissionLevel>() { _session.Auth },
                data = new Dictionary<string, object>()
                {
                    { "from", _session.Auth.actor},
                    { "to", "test3.liq" },
                    { "quantity", "0.0001 EOS" },
                    { "memo", "Anchor is the best! Thank you <3" }
                }
            };

            var transactResult = await _session.Transact(new TransactArgs() { Action = action });
            Debug.Log($"Transaction broadcast! {transactResult.Processed}");
        }

        // ask the user to sign the transaction and then broadcast to chain
        public void Vote()
        {
            _link.Transact(new TransactArgs() { Action = _action }).ContinueWith(transactTask =>
            {
                Debug.Log($"Thank you {transactTask.Result.Signer.actor}");
            });
        }

        // login and store session if successful
        private async Task Login()
        {
            var loginResult = await _link.Login(Identifier);
            Session = loginResult.Session;
            DidLogin();
        }

        #endregion
    }
}
