using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using AnchorLinkSharp;
using Assets.Packages.AnchorLinkTransportSharp.Src;
using Assets.Packages.AnchorLinkTransportSharp.Src.Transports.UiToolkit;
using Assets.Packages.AnchorLinkTransportSharp.Src.Transports.UiToolkit.Ui;
using EosSharp.Core.Api.v1;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.UIElements;

namespace Assets.Packages.AnchorLinkTransportSharp.Examples.UiToolkit
{
    public class LoginView : ScreenBase
    {
        /*
         * Child-Controls
         */

        private Button _loginButton;

        private Label _versionLabel;


        /*
         * Fields, Properties
         */
        [SerializeField]internal UnityUiToolkitTransport UnityUiToolkitTransport;
        [SerializeField]internal UiToolkitExample UiToolkitExample;
        [SerializeField]internal TransferView TransferView;


        void Start()
        {
            _loginButton = Root.Q<Button>("login-button");
            _versionLabel = Root.Q<Label>("version-label");

            _versionLabel.text = UnityUiToolkitTransport.Version;

            BindButtons();
            Show();
        }

        #region Button Binding
        private void BindButtons()
        {
            _versionLabel.RegisterCallback<ClickEvent>(evt =>
            {
                UnityUiToolkitTransport.OpenVersion();
            });

            _loginButton.clickable.clicked +=  async () =>
            {
                try
                { 
                    await UiToolkitExample.StartSession(); 
                    TransferView.Show();
                    TransferView.Rebind();
                    Hide();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
            };
        }
        #endregion

    }
}
