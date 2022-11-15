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

        public const string VersionUrl = "https://github.com/greymass/anchor-link";
        public const string DownloadAnchorUrl = "https://greymass.com/anchor/";
        public const string Version = "3.3.0 (3.4.1)";


        private ScreenBase _activeScreen;
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

        public IEnumerator<float> TransitionScreens(ScreenBase to)
        {
            if (_activeScreen == to)
                yield break;

            var i = 0;
            while (_transitioningScreens && i < 100)
            {
                yield return (0.1f);
                i++;
            }

            _transitioningScreens = true;

            _activeScreen?.Hide();
            to?.Show();

            _activeScreen = to;
            _transitioningScreens = false;

            if (to == null)
            {
                Debug.Log("missing the screen");
            }

        }


        //open anchor link version on chrome page
        public void OpenVersion()
        {
            Application.OpenURL(VersionUrl);
        }

        //open Download anchor on chrome page
        public void OpenDownloadAnchorLink()
        {
            Application.OpenURL(DownloadAnchorUrl);
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
        }

        // see https://github.com/greymass/anchor-link-browser-transport/blob/master/src/index.ts#L698
        public override void OnFailure(SigningRequest request, Exception exception)
        {
            Debug.Log("OnFailure");

            StartCoroutine(TransitionScreens(FailureOverlayView));
        }

        // see https://github.com/greymass/anchor-link-browser-transport/blob/master/src/index.ts#L264
        public override void DisplayRequest(SigningRequest request)
        {
            var esrLinkUri = request.Encode();

            if (request.IsIdentity())
            {
                StartCoroutine(TransitionScreens(QrCodeOverlayView));
                QrCodeOverlayView.Rebind(request, true);
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
