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
    public class LoginView : ScreenBase
    {
        /*
         * Connected Views
         */

        public QrCodeOverlayView QrCodeOverlayView;

        /*
         * Cloneable Controls
         */


        /*
         * Child-Controls
         */

        private Button _loginButton;

        private Label _versionLabel;


        /*
         * Fields, Properties
         */


        void Start()
        {
            _loginButton = Root.Q<Button>("login-button");
            _versionLabel = Root.Q<Label>("version-label");

            //_versionLabel.text = Version;

            BindButtons();
            Show(); 
        }


        #region Button Binding
        private void BindButtons()
        {
            _loginButton.clickable.clicked += () =>
            { 
                QrCodeOverlayView.Show();
                QrCodeOverlayView.Rebind();
                this.Hide();
            };

            _versionLabel.RegisterCallback<ClickEvent>(evt =>
            {
                Application.OpenURL(VersionUrl);
            });
        }
        #endregion

        #region Rebind

        public void Rebind()
        {
        }

        #endregion

        #region other


        #endregion
    }
}
