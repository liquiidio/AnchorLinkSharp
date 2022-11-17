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
    public class MainView : ScreenBase
    {

        /*
         * Child-Controls
         */

        private Button _changeToTransferButton;
        private Button _changeToVoteButton;
        private Button _changeToSellRamButton;
        private Button _changeToBuyRamButton;
        private Button _changeToBidNameButton;

        private Button _transferTokenButton;
        private Button _buyRamButton;
        private Button _sellRamButton;
        private Button _bidButton;
        private Button _voteButton;
        private Button _logoutButton;

        private Label _accountLabel;
        private Label _loginTitleLabel;
        private Label _subtitleLabel;

        private TextField _toTextField;
        private TextField _fromTextField;
        private TextField _memoTextField;
        private TextField _quantityTextField;
        private TextField _userAccountTextField;
        private TextField _amountWaxTextField;
        private TextField _nameToBidTextField;
        private TextField _bidAmountTextField;

        private VisualElement _transferTokenBox;
        private VisualElement _voteBox;
        private VisualElement _sellRamBox;
        private VisualElement _buyRamBox;
        private VisualElement _bidNameBox;

        /*
         * Fields, Properties
         */
        [SerializeField] internal UiToolkitExample UiToolkitExample;
        [SerializeField] internal LoginView LoginView;


        void Start()
        {
            _changeToTransferButton = Root.Q<Button>("change-to-transfer-button");
            _changeToVoteButton = Root.Q<Button>("change-to-vote-button");
            _changeToSellRamButton = Root.Q<Button>("change-to-sell-ram-button");
            _changeToBuyRamButton= Root.Q<Button>("change-to-buy-ram-button");
            _changeToBidNameButton = Root.Q<Button>("change-to-bid-button");

            _transferTokenButton = Root.Q<Button>("transfer-token-button");
            _voteButton = Root.Q<Button>("vote-button");
            _sellRamButton = Root.Q<Button>("sell-ram-button");
            _buyRamButton = Root.Q<Button>("buy-ram-button");
            _bidButton = Root.Q<Button>("bid-button");
            _logoutButton = Root.Q<Button>("log-out-button");

            _accountLabel = Root.Q<Label>("account-label");
            _loginTitleLabel = Root.Q<Label>("anchor-link-title-label");
            _subtitleLabel = Root.Q<Label>("anchor-link-subtitle-label");
            _toTextField = Root.Q<TextField>("to-account-text-field");
            _fromTextField = Root.Q<TextField>("from-account-text-field");
            _memoTextField = Root.Q<TextField>("memo-text-field");
            _quantityTextField = Root.Q<TextField>("quantity-text-field");
            _userAccountTextField = Root.Q<TextField>("user-account");
            _amountWaxTextField = Root.Q<TextField>("amount-text-field");
            _nameToBidTextField = Root.Q<TextField>("name-to-bid-text-field");
            _bidAmountTextField = Root.Q<TextField>("bid-amount-text-field");

            _transferTokenBox = Root.Q<VisualElement>("transfer-token-box");
            _voteBox = Root.Q<VisualElement>("vote-box");
            _sellRamBox = Root.Q<VisualElement>("sell-ram-box");
            _buyRamBox = Root.Q<VisualElement>("buy-ram-box");
            _bidNameBox = Root.Q<VisualElement>("bid-name-box");

            BindButtons();
            SetTransferAccountText();
            //SetSellRamText();
            //SetBuyRamText();
            SetBidNameText();
        }

        #region Button Binding
        private void BindButtons()
        {
            _changeToTransferButton.clickable.clicked += () =>
            {
                _transferTokenBox.Show();
                _voteBox.Hide();
                _sellRamBox.Hide();
                _buyRamBox.Hide();
                _bidNameBox.Hide();
            };

            _changeToVoteButton.clickable.clicked += () =>
            {
                _transferTokenBox.Hide();
                _voteBox.Show();
                _sellRamBox.Hide();
                _buyRamBox.Hide();
                _bidNameBox.Hide();
            };

            _changeToSellRamButton.clickable.clicked += () =>
            {
                _transferTokenBox.Hide();
                _voteBox.Hide();
                _sellRamBox.Show();
                _buyRamBox.Hide();
                _bidNameBox.Hide();
            };

            _changeToBuyRamButton.clickable.clicked += () =>
            {
                _transferTokenBox.Hide();
                _voteBox.Hide();
                _sellRamBox.Hide();
                _buyRamBox.Show();
                _bidNameBox.Hide();
            };

            _changeToBidNameButton.clickable.clicked += () =>
            {
                _transferTokenBox.Hide();
                _voteBox.Hide();
                _sellRamBox.Hide();
                _buyRamBox.Hide();
                _bidNameBox.Show();
            };

            _transferTokenButton.clickable.clicked += async () =>
            {
                var action = new EosSharp.Core.Api.v1.Action()
                {
                    account = "eosio.token",
                    name = "transfer",
                    authorization = new List<PermissionLevel>() { UiToolkitExample.LinkSession.Auth },
                    data = new Dictionary<string, object>()
                    {
                        { "from", UiToolkitExample.LinkSession.Auth.actor },
                        { "to", _toTextField.value },
                        { "quantity", _quantityTextField.value},
                        { "memo", _memoTextField.value }
                    }
                };
                try
                {
                    await UiToolkitExample.Transfer(action);
                }
                catch (Exception e)
                {
                    Debug.Log(e);
                    throw;
                }
            };

            _buyRamButton.clickable.clicked += async () =>
            {
                var action = new EosSharp.Core.Api.v1.Action()
                {
                    account = "eosio",
                    name = "buyram",
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
                        { "payer", "............1" },
                        { "quant", "20.00000000 WAX" },
                        { "receiver","............1" },
                    }
                };
                try
                {
                    await UiToolkitExample.SellOrBuyRam(action);
                }
                catch (Exception e)
                {
                    Debug.Log(e);
                    throw;
                }
            };

            _sellRamButton.clickable.clicked += async () =>
            {
                var action = new EosSharp.Core.Api.v1.Action()
                {
                    account = "eosio",
                    name = "sellram",

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
                        { "account", "............1" },
                        { "bytes", "20" }
                    }
                };
                try
                {
                    await UiToolkitExample.SellOrBuyRam(action);
                }
                catch (Exception e)
                {
                    Debug.Log(e);
                    throw;
                }
            };

            _bidButton.clickable.clicked += async () =>
            {
                var action = new EosSharp.Core.Api.v1.Action()
                {
                    account = "eosio",
                    name = "bidname",

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
                        { "newname", _nameToBidTextField.value },
                        { "bidder", UiToolkitExample.LinkSession.Auth.actor },
                        { "bid", _bidAmountTextField.value }
                    }
                };
                try
                {
                    await UiToolkitExample.SellOrBuyRam(action);
                }
                catch (Exception e)
                {
                    Debug.Log(e);
                    throw;
                }
            };

            _voteButton.clickable.clicked += () =>
            {
                var producers = new List<string>() { "liquidstudio" };

                var action = new EosSharp.Core.Api.v1.Action()
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
                        { "producers", producers.ToArray() },
                    }
                };

                try
                {
                    UiToolkitExample.Vote(action);
                }
                catch (Exception e)
                {
                    Debug.Log(e);
                    throw;
                }
            };

            _logoutButton.clickable.clicked += async () =>
            {
                try
                {
                    await UiToolkitExample.Logout();
                    this.Hide();
                    LoginView.Show();
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
            _fromTextField.value = UiToolkitExample.LinkSession.Auth.actor;
            _accountLabel.text = _fromTextField.value;
            _userAccountTextField.value = _fromTextField.value;

        }

        #endregion

        #region other
        private void SetTransferAccountText()
        {
            string toName = "???";
            string memoComment = "Anchor is the best! Thank you.";
            string quantityAmount = "0.0000 EOS OR WAX";
            
            _toTextField.SetValueWithoutNotify(toName);
            _memoTextField.SetValueWithoutNotify(memoComment);
            _quantityTextField.SetValueWithoutNotify(quantityAmount);
        }

        private void SetSellRamText()
        {
            string toName = "???";
            string memoComment = "Anchor is the best! Thank you.";
            string quantityAmount = "0.0000 EOS OR WAX";

            _toTextField.SetValueWithoutNotify(toName);
            _memoTextField.SetValueWithoutNotify(memoComment);
            _quantityTextField.SetValueWithoutNotify(quantityAmount);
        }

        private void SetBuyRamText()
        {
            string toName = "???";
            string memoComment = "Anchor is the best! Thank you.";
            string quantityAmount = "0.0000 EOS OR WAX";

            _toTextField.SetValueWithoutNotify(toName);
            _memoTextField.SetValueWithoutNotify(memoComment);
            _quantityTextField.SetValueWithoutNotify(quantityAmount);
        }

        private void SetBidNameText()
        {
            string name = "bid a new name";
            string amount = "0 WAX";

            _nameToBidTextField.SetValueWithoutNotify(name);
            _bidAmountTextField.SetValueWithoutNotify($"{amount}");
        }
        #endregion
    }
}
