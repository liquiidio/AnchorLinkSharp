using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using AnchorLinkSharp;
using Assets.Packages.AnchorLinkTransportSharp;
using Assets.Packages.AnchorLinkTransportSharp.Src.Transports.UiToolkit;
using Assets.Packages.AnchorLinkTransportSharp.Src.Transports.UiToolkit.Ui;
using EosSharp.Core.Api.v1;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.UIElements;
using Action = EosSharp.Core.Api.v1.Action;

namespace Assets.Packages.AnchorLinkTransportSharp.Examples.UiToolkit
{
    public class TransferView : ScreenBase
    {

        /*
         * Child-Controls
         */

        private Button _transferTokenButton;

        private Label _accountLabel;
        private Label _versionLabel;
        private Label _loginTitleLabel;
        private Label _subtitleLabel;

        private TextField _toTextField;
        private TextField _fromTextField;
        private TextField _memoTextField;
        private TextField _quantityTextField;

        /*
         * Fields, Properties
         */
        [SerializeField] internal UnityUiToolkitTransport UiToolkitTransport;


        void Start()
        {
            _transferTokenButton = Root.Q<Button>("transfer-token-button");
            _accountLabel = Root.Q<Label>("account-label");
            _versionLabel = Root.Q<Label>("version-label");
            _loginTitleLabel = Root.Q<Label>("anchor-link-title-label");
            _subtitleLabel = Root.Q<Label>("anchor-link-subtitle-label");
            _toTextField = Root.Q<TextField>("to-account-text-field");
            _fromTextField = Root.Q<TextField>("from-account-text-field");
            _memoTextField = Root.Q<TextField>("memo-text-field");
            _quantityTextField = Root.Q<TextField>("quantity-text-field");

            _versionLabel.text = UnityUiToolkitTransport.Version;

            BindButtons();
            SetTransferAccountText();
        }

        #region Button Binding
        private void BindButtons()
        {
            _versionLabel.RegisterCallback<ClickEvent>(evt =>
            {
                UiToolkitTransport.OpenVersion();
            });

            _transferTokenButton.clickable.clicked += async () =>
            {
                var action = new EosSharp.Core.Api.v1.Action()
                {
                    account = "eosio.token",
                    name = "transfer",
                    authorization = new List<PermissionLevel>() { UiToolkitTransport.LinkSession.Auth },
                    data = new Dictionary<string, object>()
                    {
                        { "from", UiToolkitTransport.LinkSession.Auth.actor },
                        { "to", _toTextField.value },
                        { "quantity", _quantityTextField.value},
                        { "memo", _memoTextField.value }
                    }
                };
                try
                {
                    await UiToolkitTransport.Transfer(action);
                }
                catch (Exception e)
                {
                    Debug.Log(e);
                    throw;
                }
            };

        }
        #endregion

        #region Rebind

        public void Rebind()
        {
            _fromTextField.value = UiToolkitTransport.LinkSession.Auth.actor;
            _accountLabel.text = _fromTextField.value;
        }

        #endregion

        #region other
        private void SetTransferAccountText()
        {
            string toName = "???";
            string memoComment = "Anchor is the best! Thank you.";
            string quantityAmount = "0.0000 EOS";
            
            _toTextField.SetValueWithoutNotify(toName);
            _memoTextField.SetValueWithoutNotify(memoComment);
            _quantityTextField.SetValueWithoutNotify(quantityAmount);
        }
        #endregion
    }
}
