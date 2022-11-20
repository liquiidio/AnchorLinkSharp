using System;
using System.Collections.Generic;
using AnchorLinkSharp;
using Assets.Packages.AnchorLinkTransportSharp.Src.Transports.UiToolkit.Ui;
using EosioSigningRequest;
using UnityEngine;
using UnityEngine.UIElements;

namespace Assets.Packages.AnchorLinkTransportSharp.Src.Transports.UiToolkit
{
    public class UnityUiToolkitTransport : UnityTransport
    {
        public const string VersionUrl = "https://github.com/greymass/anchor-link";
        public const string DownloadAnchorUrl = "https://greymass.com/anchor/";
        public const string Version = "3.3.0 (3.4.1)";

        private static ScreenBase _activeScreen;
        private static bool _transitioningPanel;

        [SerializeField] internal FailurePanel FailurePanel;
        [SerializeField] internal LoadingPanel LoadingPanel;
        [SerializeField] internal QrCodePanel QrCodePanel;
        [SerializeField] internal SigningTimerPanel SigningTimerPanel;
        [SerializeField] internal SuccessPanel SuccessPanel;
        [SerializeField] internal TimeoutPanel TimeoutPanel;


        public UnityUiToolkitTransport(TransportOptions options) : base(options)
        {
            SuccessPanel = FindObjectOfType<SuccessPanel>();
            FailurePanel = FindObjectOfType<FailurePanel>();
            QrCodePanel = FindObjectOfType<QrCodePanel>();
            LoadingPanel = FindObjectOfType<LoadingPanel>();
            SigningTimerPanel = FindObjectOfType<SigningTimerPanel>();
            TimeoutPanel = FindObjectOfType<TimeoutPanel>();
        }

        public static IEnumerator<float> TransitionPanels(ScreenBase to)
        {
            if (_activeScreen == to)
                yield break;

            var i = 0;
            while (_transitioningPanel && i < 100)
            {
                yield return 0.1f;
                i++;
            }

            _transitioningPanel = true;

            _activeScreen?.Hide();
            to?.Show();

            _activeScreen = to;
            _transitioningPanel = false;

            if (to == null) Debug.Log("missing the panel");
        }

        //open anchor link version on chrome page
        public static void OpenVersion()
        {
            Application.OpenURL(VersionUrl);
        }

        //open Download anchor on chrome page
        public static void OpenDownloadAnchorLink()
        {
            Application.OpenURL(DownloadAnchorUrl);
        }

        // see https://github.com/greymass/anchor-link-browser-transport/blob/master/src/index.ts#L361
        // and https://github.com/greymass/anchor-link-console-transport/blob/master/src/index.ts#L10
        public override void ShowLoading()
        {
            Debug.Log("ShowLoading");

            StartCoroutine(TransitionPanels(LoadingPanel));
        }

        // see https://github.com/greymass/anchor-link-browser-transport/blob/master/src/index.ts#L680
        public override void OnSuccess(SigningRequest request, TransactResult result)
        {
            Debug.Log("OnSuccess");

            StartCoroutine(TransitionPanels(SuccessPanel));
            SuccessPanel.Rebind(request);
        }

        // see https://github.com/greymass/anchor-link-browser-transport/blob/master/src/index.ts#L698
        public override void OnFailure(SigningRequest request, Exception exception)
        {
            Debug.Log("OnFailure");

            StartCoroutine(TransitionPanels(FailurePanel));
        }

        // see https://github.com/greymass/anchor-link-browser-transport/blob/master/src/index.ts#L264
        public override void DisplayRequest(SigningRequest request)
        {
            Debug.Log("DisplayRequest");

            StartCoroutine(TransitionPanels(QrCodePanel));
            QrCodePanel.Rebind(request, request.IsIdentity());
        }

        // see https://github.com/greymass/anchor-link-browser-transport/blob/master/src/index.ts#L226
        public override void ShowDialog(string title = null, string subtitle = null, string type = null,
            Action action = null,
            object content = null)
        {
            Debug.Log("ShowDialog");
        }
    }
}