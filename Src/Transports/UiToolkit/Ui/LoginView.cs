using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using AnchorLinkSharp;
using Assets.Packages.AnchorLinkTransportSharp;
using Assets.Packages.AnchorLinkTransportSharp.Src.StorageProviders;
using Assets.Packages.AnchorLinkTransportSharp.Src.Transports.UiToolkit;
using Assets.Packages.AnchorLinkTransportSharp.UI.Example;
using EosSharp.Core.Api.v1;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.UIElements;

namespace Assets.Packages.AnchorLinkTransportSharp.Src.Transports.UiToolkit.Ui
{
    public class LoginView : ScreenBase
    {
        // app identifier, should be set to the eosio contract account if applicable
        private const string Identifier = "example";

        // Assign UnityTransport through the Editor
        [SerializeField] internal UnityTransport Transport;

        // initialize the link
        private AnchorLink _link;

        // the session instance, either restored using link.restoreSession() or created with link.login()
        private LinkSession _session;

        /*
         * Child-Controls
         */

        private Button _loginButton;

        private Label _versionLabel;


        /*
         * Fields, Properties
         */
        [SerializeField]public UnityUiToolkitTransport UnityUiToolkitTransport;
        [SerializeField]private LoggedView LoggedView;


        void Start()
        {
            _loginButton = Root.Q<Button>("login-button");
            _versionLabel = Root.Q<Label>("version-label");

            _versionLabel.text = Version;


            //Transport = UnityUiToolkitTransport;

            //_link = new AnchorLink(new LinkOptions()
            //{
            //    Transport = this.Transport,
            //    ChainId = "aca376f206b8fc25a6ed44dbdc66547c36c6c33e3a119ffbeaef943642f0e906",
            //    Rpc = "https://eos.greymass.com",
            //    ZlibProvider = new NetZlibProvider(),
            //    Storage = new JsonLocalStorage()
            //    //chains: [{
            //    //    chainId: 'aca376f206b8fc25a6ed44dbdc66547c36c6c33e3a119ffbeaef943642f0e906',
            //    //    nodeUrl: 'https://eos.greymass.com',
            //    //}]
            //});

            BindButtons();
            Show();
        }


        #region Button Binding
        private void BindButtons()
        {
            _loginButton.clickable.clicked +=  async () =>
            {
                try
                {
                   await UnityUiToolkitTransport.StartSession();
                   LoggedView.Show();
                   LoggedView.Rebind();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
                
                Hide();
            };

            _versionLabel.RegisterCallback<ClickEvent>(evt =>
            {
                Application.OpenURL(VersionUrl);
            });
        }
        #endregion

        #region Other


        // login and store session if sucessful
        public async Task Login()
        {
            var loginResult = await _link.Login(Identifier);
            _session = loginResult.Session;
            //DidLogin();
        }

        #endregion
    }
}
