using System;
using AnchorLinkSharp;
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


        public const string VersionUrl = "https://github.com/greymass/anchor-link";
        public const string DownloadAnchorUrl = "https://greymass.com/anchor/";

        // the session instance, either restored using link.restoreSession() or created with link.login()
        public LinkSession Session;

        public const string Version = "3.3.0 (3.4.1)";

        public UnityUiToolkitTransport(TransportOptions options) : base(options)
        {
            SuccessOverlayView = FindObjectOfType<SuccessOverlayView>();
            FailureOverlayView = FindObjectOfType<FailureOverlayView>();
            QrCodeOverlayView = FindObjectOfType<QrCodeOverlayView>();
            LoadingOverlayView = FindObjectOfType<LoadingOverlayView>();
            SigningTimerOverlayView = FindObjectOfType<SigningTimerOverlayView>();
            TimeoutOverlayView = FindObjectOfType<TimeoutOverlayView>();
        }

        // see https://github.com/greymass/anchor-link-browser-transport/blob/master/src/index.ts#L361
        // and https://github.com/greymass/anchor-link-console-transport/blob/master/src/index.ts#L10
        public override void ShowLoading()
        {
            Debug.Log("ShowLoading");
            LoadingOverlayView.Show();
        }

        // see https://github.com/greymass/anchor-link-browser-transport/blob/master/src/index.ts#L680
        public override void OnSuccess(SigningRequest request, TransactResult result)
        {
            Debug.Log("OnSuccess");
            SuccessOverlayView.Show();
            QrCodeOverlayView.Hide();
            // Any other View must be closed as well!
            // Had a few cases where multiple Views where active and shown at the same time
            // Add a Method that ensures that always only one View is shown!
        }

        // see https://github.com/greymass/anchor-link-browser-transport/blob/master/src/index.ts#L698
        public override void OnFailure(SigningRequest request, Exception exception)
        {
            Debug.Log("OnFailure");
            FailureOverlayView.Show();
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
            }
            else
            {
                Application.OpenURL(esrLinkUri);
                // SigningOverlayView or however it's called is shown
                // ( the one with the Timer)
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
