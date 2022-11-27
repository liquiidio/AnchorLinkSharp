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
        public const string Version = "3.5.1 (3.5.1)";

        private static ScreenBase _activeScreen;
        private static bool _transitioningPanel;

        [SerializeField] public bool IsWhiteTheme;

        [SerializeField] internal FailurePanel FailurePanel;
        [SerializeField] internal LoadingPanel LoadingPanel;
        [SerializeField] internal QrCodePanel QrCodePanel;
        [SerializeField] internal SigningTimerPanel SigningTimerPanel;
        [SerializeField] internal SuccessPanel SuccessPanel;
        [SerializeField] internal TimeoutPanel TimeoutPanel;

        [SerializeField] internal StyleSheet DarkTheme;
        [SerializeField] internal StyleSheet WhiteTheme;


        public UnityUiToolkitTransport(TransportOptions options) : base(options)
        {
            SuccessPanel = FindObjectOfType<SuccessPanel>();
            FailurePanel = FindObjectOfType<FailurePanel>();
            QrCodePanel = FindObjectOfType<QrCodePanel>();
            LoadingPanel = FindObjectOfType<LoadingPanel>();
            TimeoutPanel = FindObjectOfType<TimeoutPanel>();
            SigningTimerPanel = FindObjectOfType<SigningTimerPanel>();
            
        }

        private void CheckTheme()
        {
            if (_activeScreen != null)
            {
                _activeScreen.Root.styleSheets.Clear();

                if (IsWhiteTheme)
                {
                    _activeScreen.Root.styleSheets.Remove(DarkTheme);
                    _activeScreen.Root.styleSheets.Add(WhiteTheme);
                }
                else
                {
                    _activeScreen.Root.styleSheets.Remove(WhiteTheme);
                    _activeScreen.Root.styleSheets.Add(DarkTheme);
                }
            }
            else Debug.Log("screen is null");
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

        //open anchor link version on browser page
        public static void OpenVersion()
        {
            Application.OpenURL(VersionUrl);
        }

        //open Download anchor on browser page
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
            CheckTheme();
        }

        // see https://github.com/greymass/anchor-link-browser-transport/blob/master/src/index.ts#L680
        public override void OnSuccess(SigningRequest request, TransactResult result)
        {
            Debug.Log("OnSuccess");

            StartCoroutine(TransitionPanels(SuccessPanel));
            CheckTheme();
            SuccessPanel.Rebind(request);
        }

        // see https://github.com/greymass/anchor-link-browser-transport/blob/master/src/index.ts#L698
        public override void OnFailure(SigningRequest request, Exception exception)
        {
            Debug.Log("");

            StartCoroutine(TransitionPanels(FailurePanel));
            CheckTheme();
        }

        // see https://github.com/greymass/anchor-link-browser-transport/blob/master/src/index.ts#L264
        public override void DisplayRequest(SigningRequest request)
        {

            
            Debug.Log("DisplayRequest");

            var esrLinkUri = request.Encode(false, true);

            if (request.IsIdentity())
            {
                StartCoroutine(TransitionPanels(LoadingPanel));
                StartCoroutine(TransitionPanels(QrCodePanel));
                CheckTheme();
                QrCodePanel.Rebind(request, request.IsIdentity(), IsWhiteTheme);
            }
            else
            {
                StartCoroutine(TransitionPanels(LoadingPanel));
                Application.OpenURL(esrLinkUri);
                StartCoroutine(TransitionPanels(SigningTimerPanel));
                SigningTimerPanel.StartCountdownTimer();
                CheckTheme();
                QrCodePanel.Rebind(request, request.IsIdentity(), IsWhiteTheme);

            }
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