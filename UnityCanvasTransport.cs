using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AnchorLinkSharp;
using EosioSigningRequest;
using EosSharp.Core.Api.v1;
using HyperionApiClient.Models;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Packages.AnchorLinkTransportSharp
{
    public class UnityCanvasTransport : UnityTransport
    {
        // BASE-CLASS HAS FOLLOWING FIELDS
        //private readonly bool _requestStatus;
        //private readonly bool _fuelEnabled;
        //private SigningRequest _activeRequest;
        //private object _activeCancel; //?: (reason: string | Error) => void
        //private Timer _countdownTimer;
        //private Timer _closeTimer;


        AnchorLink _anchorLink;

        LinkSession _linkSession;

        private const string Identifier = "example";

        #region Login-Screen
        [Header("Login Screen Panel Components")]
        public string ESRLink = ""; // Linke that will be converted to a QR code and can be copy from

        public GameObject LoginPanel;   // The holding panel for the login details
        public GameObject HyperlinkCopiedNotificationPanel; // Confirmation panel for when the link has been successfully copied

        //Buttons
        public Button CloseLoginScreenButton;
        public Button StaticQRCodeHolderTargetButton;
        public Button ResizableQRCodeHolderTargetButton;
        public Button HyperlinkCopyButton;
        public Button LaunchAnchorButton;

        #endregion

        #region Other panels
        [Header("Other panels")]
        public GameObject LoadingPanel;
        public GameObject SignPanel;
        public GameObject SuccessPanel;
        public GameObject FailurePanel;
        #endregion

        public UnityCanvasTransport(TransportOptions options) : base(options)
        {

        }

        public async void StartSession()
        {
            _anchorLink = new AnchorLink(new LinkOptions()
            {
                Transport = this,
                ChainId = "aca376f206b8fc25a6ed44dbdc66547c36c6c33e3a119ffbeaef943642f0e906",
                Rpc = "https://eos.greymass.com",
                ZlibProvider = new NetZlibProvider(),
                Storage = new JsonLocalStorage()
            });

            try
            {
                await CreateSession();
            }

            catch (Exception ex)
            {
                Debug.LogError(ex);
            }
        }

        public async Task CreateSession()
        {
            try
            {
                var loginResult = await _anchorLink.Login(Identifier);

                _linkSession = loginResult.Session;
            }
            catch(Exception ex)
            {
                Debug.LogError(ex);
            }

        }

        // tries to restore session, called when document is loaded
        public async Task RestoreSession()
        {
            var restoreSessionResult = await _anchorLink.RestoreSession(Identifier);
            _linkSession = restoreSessionResult;
            
            if (_linkSession != null)
                Debug.Log($"{_linkSession.Auth.actor} logged-in");
        }

        // transfer tokens using a session
        public async Task Transfer()
        {
            var action = new EosSharp.Core.Api.v1.Action()
            {
                account = "eosio.token",
                name = "transfer",
                authorization = new List<PermissionLevel>() { _linkSession.Auth },
                data = new Dictionary<string, object>()
                {
                    { "from", _linkSession.Auth.actor },
                    { "to", "teamgreymass" },
                    { "quantity", "0.0001 EOS" },
                    { "memo", "Anchor is the best! Thank you <3" }
                }
            };

            var transactResult = await _linkSession.Transact(new TransactArgs() { Action = action });
            Console.WriteLine($"Transaction broadcast! {transactResult.Processed}");
        }

        // see https://github.com/greymass/anchor-link-browser-transport/blob/master/src/index.ts#L361
        public override void ShowLoading()
        {
            LoadingPanel.SetActive(true);
        }

        // see https://github.com/greymass/anchor-link-browser-transport/blob/master/src/index.ts#L680
        public override void OnSuccess(SigningRequest request, TransactResult result)
        {
            SuccessPanel.SetActive(true);
        }

        // see https://github.com/greymass/anchor-link-browser-transport/blob/master/src/index.ts#L698
        public override void OnFailure(SigningRequest request, Exception exception)
        {
            Debug.LogWarning($"FailurePanel's name is { FailurePanel.name}");

           FailurePanel.SetActive(true);
        }

        // see https://github.com/greymass/anchor-link-browser-transport/blob/master/src/index.ts#L264
        public override void DisplayRequest(SigningRequest request)
        {
            LoginPanel.SetActive(true);
            
            ESRLink = request.Signature.Signature;  // Find out if this is the correct ESR link
        }

        // see https://github.com/greymass/anchor-link-browser-transport/blob/master/src/index.ts#L226
        public override void ShowDialog(string title = null, string subtitle = null, string type = null, System.Action action = null,
            object content = null)
        {
            throw new NotImplementedException();
        }


        #region Canvas function-calls

        public void OnLoginPanelCloseButtonPressed()
        {
            Debug.LogWarning("Close the login panel");
        }

        public void OnStaticQRCodeHolderTargetButtonPressed(RectTransform resizableQRCodePanel)
        {
            resizableQRCodePanel.gameObject.SetActive(true);

            resizableQRCodePanel.GetComponent<Animator>().SetTrigger("doZoomIn");
        }

        public void OnResizableQRCodeHolderTargetButtonPressed(RectTransform resizableQRCodePanel)
        {
            resizableQRCodePanel.GetComponent<Animator>().SetTrigger("doZoomOut");

            StartCoroutine(ResizableQRCodePanelZoomOut(resizableQRCodePanel));
        }

        private IEnumerator ResizableQRCodePanelZoomOut(RectTransform resizableQRCodePanel)
        {
            yield return new WaitForSeconds(0.1f);

            yield return new WaitUntil(() => resizableQRCodePanel.GetComponent<Animator>().GetCurrentAnimatorClipInfo(0).FirstOrDefault().clip?.name == "NormalState");

            resizableQRCodePanel.gameObject.SetActive(false);
        }

        public void OnQRHyperlinkButtonPressed()
        {
            HyperlinkCopyButton.gameObject.SetActive(false);

            HyperlinkCopiedNotificationPanel.gameObject.SetActive(true);

            CopyToClipboard(ESRLink);

            StartCoroutine(ToggleHyperlinkCopyButton_Delayed());
        }

        private IEnumerator ToggleHyperlinkCopyButton_Delayed()
        {
            yield return new WaitForSeconds(3.5f);
            HyperlinkCopyButton.gameObject.SetActive(true);

            HyperlinkCopiedNotificationPanel.gameObject.SetActive(false);
        }

        public void OnLaunchAnchorButtonPressed()
        {
            Debug.LogWarning("Call open Anchor function in UnityTransport.cs");


        }

        public void OnCloseLoadingScreenButtonPressed()
        {
            Debug.LogWarning("Close loading screen button has been pressed!");
        }

        public void OnCloseSignScreenButtonPressed()
        {
            Debug.LogWarning("Close sign screen button has been pressed!");
        }

        public void OnCloseSuccessScreenButtonPressed()
        {
            Debug.LogWarning("Close success screen button has been pressed!");
            
            SuccessPanel.SetActive(false);
        }

        public void OnCloseFailureScreenButtonPressed()
        {
            Debug.LogWarning("Close failure screen button has been pressed!");

            FailurePanel.SetActive(false);
        }
        #endregion
    }
}
