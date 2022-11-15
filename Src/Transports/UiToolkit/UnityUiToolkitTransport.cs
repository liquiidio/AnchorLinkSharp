using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AnchorLinkSharp;
using Assets.Packages.AnchorLinkTransportSharp.Src.StorageProviders;
using Assets.Packages.AnchorLinkTransportSharp.Src.Transports.UiToolkit.Ui;
using EosioSigningRequest;
using UnityEngine;

namespace Assets.Packages.AnchorLinkTransportSharp.Src.Transports.UiToolkit
{
    public class UnityUiToolkitTransport : UnityTransport
    {
        [SerializeField] internal SuccessOverlayView SuccessOverlayView;
        [SerializeField] internal FailureOverlayView FailureOverlayView;
        [SerializeField] internal QrCodeOverlayView QrCodeOverlayView;
        [SerializeField] internal LoadingOverlayView LoadingOverlayView;
        [SerializeField] internal SigningTimerOverlayView SigningTimerOverlayView;
        [SerializeField] internal TimeoutOverlayView TimeoutOverlayView;

        // app identifier, should be set to the eosio contract account if applicable
        private const string Identifier = "example";

        // Assign UnityTransport through the Editor
        //[SerializeField] internal UnityTransport Transport;

        // initialize the link
        private AnchorLink _anchorLink;

        // the session instance, either re


        public const string VersionUrl = "https://github.com/greymass/anchor-link";
        public const string DownloadAnchorUrl = "https://greymass.com/anchor/";

        // the session instance, either restored using link.restoreSession() or created with link.login()
        public LinkSession LinkSession;

        public string ESRLink = ""; // Link that will be converted to a QR code and can be copy from

        public const string Version = "3.3.0 (3.4.1)";

        public ScreenBase ActiveScreen;
        private bool _transitioningScreens;

        public UnityUiToolkitTransport(TransportOptions options) : base(options)
        {
            SuccessOverlayView = FindObjectOfType<SuccessOverlayView>();
            FailureOverlayView = FindObjectOfType<FailureOverlayView>();
            QrCodeOverlayView = FindObjectOfType<QrCodeOverlayView>();
            LoadingOverlayView = FindObjectOfType<LoadingOverlayView>();
            SigningTimerOverlayView = FindObjectOfType<SigningTimerOverlayView>();
            TimeoutOverlayView = FindObjectOfType<TimeoutOverlayView>();
        }


        public async Task StartSession()
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
                var loginResult = await _anchorLink.Login(Identifier);

                LinkSession = loginResult.Session;
                Debug.Log($"{LinkSession.Auth.actor} logged-in");
            }
            catch (Exception ex)
            {
                Debug.LogError(ex);
            }
        }

        public IEnumerator<float> TransitionScreens(ScreenBase to)
        {
            if (ActiveScreen == to)
                yield break;

            var i = 0;
            while (_transitioningScreens && i < 100)
            {
                yield return(0.1f);
                i++;
            }

            _transitioningScreens = true;

            ActiveScreen?.Hide();
            to?.Show();

            ActiveScreen = to;
            _transitioningScreens = false;

            if (to == null)
            {
                Debug.Log("missing the screen");
            }

        }

        // tries to restore session, called when document is loaded
        public async Task RestoreSession()
        {
            var restoreSessionResult = await _anchorLink.RestoreSession(Identifier);
            LinkSession = restoreSessionResult;

            if (LinkSession != null)
                Debug.Log($"{LinkSession.Auth.actor} logged-in");
        }


        // transfer tokens using a session  
        public async Task Transfer(EosSharp.Core.Api.v1.Action action)
        {
            var transactResult = await LinkSession.Transact(new TransactArgs() { Action = action });

            print($"Transaction broadcast! {transactResult.Processed}");
        }


        // see https://github.com/greymass/anchor-link-browser-transport/blob/master/src/index.ts#L361
        // and https://github.com/greymass/anchor-link-console-transport/blob/master/src/index.ts#L10
        public override void ShowLoading()
        {
            Debug.Log("ShowLoading");

            StartCoroutine(TransitionScreens(LoadingOverlayView));
        }

        // see https://github.com/greymass/anchor-link-browser-transport/blob/master/src/index.ts#L680
        public override void OnSuccess(SigningRequest request, TransactResult result)
        {
            Debug.Log("OnSuccess");

            StartCoroutine(TransitionScreens(SuccessOverlayView));
            SuccessOverlayView.CloseTimer();

            //QrCodeOverlayView.Hide();
            // Any other View must be closed as well!
            // Had a few cases where multiple Views where active and shown at the same time
            // Add a Method that ensures that always only one View is shown!
        }

        // see https://github.com/greymass/anchor-link-browser-transport/blob/master/src/index.ts#L698
        public override void OnFailure(SigningRequest request, Exception exception)
        {
            Debug.Log("OnFailure");

            StartCoroutine(TransitionScreens(FailureOverlayView));
            FailureOverlayView.ExceptionHandler(exception);
        }

        // see https://github.com/greymass/anchor-link-browser-transport/blob/master/src/index.ts#L264
        public override void DisplayRequest(SigningRequest request)
        {
            // QR-Code to be shown
            // Url to be opened via Application.OpenURL(requestUri);
            // Application.OpenURL(requestUri); automatically opens Anchor Wallet
            var esrLinkUri = request.Encode();

            if (request.IsIdentity())
            {
                // LOGIN!
                // Show View with QR-Code and "Launch Anchor" Button
                ESRLink = request.Encode(false, false);  // This returns ESR link to be converted
                var qrCodeTexture = StringToQRCodeTexture2D(ESRLink);

                StartCoroutine(TransitionScreens(QrCodeOverlayView));
                QrCodeOverlayView.Rebind(qrCodeTexture, true, false);

            }
            else
            {
                Application.OpenURL(esrLinkUri);
  
                StartCoroutine(TransitionScreens(LoadingOverlayView));

                StartCoroutine(TransitionScreens(SigningTimerOverlayView));
                SigningTimerOverlayView.StartCountdownTimer();
            }
            Debug.Log("DisplayRequest");
        }

        // see https://github.com/greymass/anchor-link-browser-transport/blob/master/src/index.ts#L226
        public override void ShowDialog(string title = null, string subtitle = null, string type = null, Action action = null,
            object content = null)
        {
            Debug.Log("ShowDialog");
        }
    }
}
