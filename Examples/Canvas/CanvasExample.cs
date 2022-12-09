using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AnchorLinkSharp;
using Assets.Packages.AnchorLinkTransportSharp.Src;
using Assets.Packages.AnchorLinkTransportSharp.Src.StorageProviders;
using Assets.Packages.AnchorLinkTransportSharp.Src.Transports.Canvas;
using Assets.Packages.eossharp.EosSharp.EosSharp.Unity3D;
using EosSharp.Core.Api.v1;
using Newtonsoft.Json.Bson;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Packages.AnchorLinkTransportSharp.Examples.Canvas
{
    public class CanvasExample : MonoBehaviour
    {
        // Assign UnityTransport through the Editor
        [SerializeField] private UnityCanvasTransport Transport;

        [Header("Panels")]
        [SerializeField] private GameObject CustomActionsPanel;
        [SerializeField] private GameObject CustomTransferPanel;

        [Header("Buttons")]
        [SerializeField] private Button LoginButton;
        [SerializeField] private Button RestoreSessionButton;
        [SerializeField] private Button TransferButton;
        [SerializeField] private Button LogoutButton;

        private Coroutine waitCoroutine;
        // app identifier, should be set to the eosio contract account if applicable
        private const string Identifier = "example";

        // initialize the link
        private AnchorLink _link;

        // the session instance, either restored using link.restoreSession() or created with link.login()
        private LinkSession _session;

        private void Start()
        {
            Transport.DisableTargetPanel(CustomTransferPanel, CustomActionsPanel);
            Transport.FailurePanel.GetComponentsInChildren<Button>(true).First(_btn => _btn.name == "CloseFailurePanelButton").onClick.AddListener(delegate
            {
                Transport.SwitchToNewPanel(CustomActionsPanel);
            }
            );

            Transport.SuccessPanel.GetComponentsInChildren<Button>(true).First(_btn => _btn.name == "CloseSuccessPanelButton").onClick.AddListener(delegate
            {
                if (waitCoroutine != null)
                    StopCoroutine(waitCoroutine);

                Transport.SwitchToNewPanel(CustomTransferPanel);
            }
           );
        }

        // initialize a new session
        public async void StartSession()
        {
            _link = new AnchorLink(new LinkOptions()
            {
                Transport = this.Transport,
                // Uncomment this for and EOS session
                //ChainId = "aca376f206b8fc25a6ed44dbdc66547c36c6c33e3a119ffbeaef943642f0e906",
                //Rpc = "https://eos.greymass.com",

                // WAX session
                ChainId = "1064487b3cd1a897ce03ae5b6a865651747e2e152090f99c1d19d44e01aea5a4",
                Rpc = "https://api.wax.liquidstudios.io",
                ZlibProvider = new NetZlibProvider(),
                Storage = new PlayerPrefsStorage()
            });

            await Login();
        }

        // login and store session if sucessful
        private async Task Login()
        {
            var loginResult = await _link.Login(Identifier);
            _session = loginResult.Session;
            DidLogin();
        }

        // call from UI button
        public async void RestoreASession()
        {
            await RestoreSession();
        }

        // tries to restore session, called when document is loaded
        private async Task RestoreSession()
        {
            var restoreSessionResult = await _link.RestoreSession(Identifier);
            _session = restoreSessionResult;
            if (_session != null)
                DidLogin();
        }

        // call from UI button
        public async void DoLogout()
        {
            await Logout();
        }
        // logout and remove session from storage
        private async Task Logout()
        {
            await _session.Remove();
        }

        // called when session was restored or created
        private void DidLogin()
        {
            Debug.Log($"{_session.Auth.actor} logged-in");

            waitCoroutine = StartCoroutine(SwitchPanels(CustomActionsPanel, CustomTransferPanel, 1.5f));
        }

        // use this to toggle on a new rect (or a gameobject) in the canvas
        public void ShowTargetPanel(GameObject targetPanel)
        {
            Transport.SwitchToNewPanel(targetPanel);
        }

        // Gather data from the custom transfer UI panel
        public async void TryTransferTokens(GameObject TransferDetailsPanel)
        {
            string _frmAcc = "";
            string _toAcc = "";
            string _qnty = "";
            string _memo = "";

            foreach (var _inputField in TransferDetailsPanel.GetComponentsInChildren<TMP_InputField>())
            {
                if (_inputField.name == "FromAccountInputField(TMP)")
                    _frmAcc = _inputField.text;

                else if (_inputField.name == "ToAccountInputField(TMP)")
                    _toAcc = _inputField.text;

                else if (_inputField.name == "QuantityAccountInputField(TMP)")
                {
                    _qnty = $"{_inputField.text} WAX";

                    _qnty = _qnty.Replace(",", ".");
                }
                else if (_inputField.name == "MemoAccountInputField(TMP)")
                    _memo = _inputField.text;
            }

            await Transfer
            (
                _frmAcc,
                _toAcc,
                _qnty,
                _memo
            );
        }

        // transfer tokens using a session
        private async Task Transfer(string frmAcc, string toAcc, string qnty, string memo)
        {
            var action = new EosSharp.Core.Api.v1.Action()
            {
                account = "eosio.token",
                name = "transfer",
                authorization = new List<PermissionLevel>() { _session.Auth },
                data = new Dictionary<string, object>
                {
                    {"from", frmAcc},
                    {"to", toAcc},
                    {"quantity", qnty},
                    {"memo", memo}
                }
            };

            //Debug.Log($"Session {_session.Identifier}");
            //Debug.Log($"Link: {_link.ChainId}");

            try
            {
                var transactResult = await _link.Transact(new TransactArgs() { Action = action });
                // OR (see next line)
                //var transactResult = await _session.Transact(new TransactArgs() { Action = action });
                Debug.Log($"Transaction broadcast! {transactResult.Processed}");

                waitCoroutine = StartCoroutine(SwitchPanels(Transport.currentPanel, CustomActionsPanel, 1.5f));

            }
            catch (Exception e)
            {
                Debug.Log(e);
                throw;
            }
        }

        private IEnumerator SwitchPanels(GameObject fromPanel, GameObject toPanel, float SecondsToWait = 0.1f)
        {
            Debug.Log("Start counter");
            yield return new WaitForSeconds(SecondsToWait);

            Transport.DisableTargetPanel(fromPanel, toPanel);
        }
    }
}
