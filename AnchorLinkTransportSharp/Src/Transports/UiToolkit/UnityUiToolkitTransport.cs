using System;
using System.Collections.Generic;
using AnchorLinkSharp;
using AnchorLinkTransportSharp.Src.Transports.UiToolkit.Ui;
using EosioSigningRequest;
using UnityEngine;
using UnityEngine.UIElements;

namespace AnchorLinkTransportSharp.Src.Transports.UiToolkit
{
    public class UnityUiToolkitTransport : UnityTransport
    {
        //! link to anchor version
        private const string VersionUrl = "https://github.com/greymass/anchor-link";
        //! link to anchor download
        private const string DownloadAnchorUrl = "https://greymass.com/anchor/";
        //! current version number
        internal const string Version = "3.5.1 (3.5.1)";

        //! current screen that is being displayed
        private static ScreenBase _activeScreen;
        //! screen that we want to load into
        private static bool _transitioningPanel;

        //! Toggle dark and light themes
        [SerializeField] internal bool IsWhiteTheme;

        //! Panel to show Failure
        [SerializeField] internal FailurePanel FailurePanel;
        //! Panel to show Loading
        [SerializeField] internal LoadingPanel LoadingPanel;
        //! Panel to show QrCode
        [SerializeField] internal QrCodePanel QrCodePanel;
        //! Panel to show SigningTimer
        [SerializeField] internal SigningTimerPanel SigningTimerPanel;
        //! Panel to show Success
        [SerializeField] internal SuccessPanel SuccessPanel;
        //! Panel to show Timeout
        [SerializeField] internal TimeoutPanel TimeoutPanel;

        //! stylesheet for displaying DarkTheme
        [SerializeField] internal StyleSheet DarkTheme;
        //! stylesheet for displaying WhiteTheme
        [SerializeField] internal StyleSheet WhiteTheme;

        /// <summary>
        /// Transport constructor
        /// </summary>
        /// <param name="options"></param>
        public UnityUiToolkitTransport(TransportOptions options) : base(options)
        {
            SuccessPanel = FindObjectOfType<SuccessPanel>();
            FailurePanel = FindObjectOfType<FailurePanel>();
            QrCodePanel = FindObjectOfType<QrCodePanel>();
            LoadingPanel = FindObjectOfType<LoadingPanel>();
            TimeoutPanel = FindObjectOfType<TimeoutPanel>();
            SigningTimerPanel = FindObjectOfType<SigningTimerPanel>();
        }

        //! check which theme to use (light and dark)
        private void CheckTheme()
        {
            if (_activeScreen != null)
            {
                _activeScreen.Root.styleSheets.Clear();

                if (IsWhiteTheme) _activeScreen.Root.styleSheets.Add(WhiteTheme);
                else _activeScreen.Root.styleSheets.Add(DarkTheme);
            }
            else Debug.Log("screen is null");
        }

        /// <summary>
        /// Switch from one panel to a new one
        /// </summary>
        /// <param name="to"></param>
        /// <returns></returns>
        internal static IEnumerator<float> TransitionPanels(ScreenBase to)
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

        //! open anchor link version on browser page
        internal static void OpenVersion()
        {
            Application.OpenURL(VersionUrl);
        }

        //! open Download anchor on browser page
        internal static void OpenDownloadAnchorLink()
        {
            Application.OpenURL(DownloadAnchorUrl);
        }

        /// <summary>
        /// Method is invoked when a request is made and user signing on the wallet is required
        /// <para>Refer to https://github.com/greymass/anchor-link-browser-transport/blob/master/src/index.ts#L361.</para> 
        /// </summary>
        public override void ShowLoading()
        {
            StartCoroutine(TransitionPanels(LoadingPanel));
            CheckTheme();
        }

        /// <summary>
        /// Method is invoked when a succesful signing request is completed
        /// <para>Refer to https://github.com/greymass/anchor-link-browser-transport/blob/master/src/index.ts#L680.</para> 
        /// </summary>
        /// <param name="request">/></param>
        /// <param name="result"></param>
        public override void OnSuccess(SigningRequest request, TransactResult result)
        {
            StartCoroutine(TransitionPanels(SuccessPanel));
            CheckTheme();
            SuccessPanel.Rebind(request);
        }

        /// <summary>
        /// Method is invoked when a signing request fails or is cancelled
        /// <para>Refer to https://github.com/greymass/anchor-link-browser-transport/blob/master/src/index.ts#L698.</para> 
        /// </summary>
        /// <param name="request"></param>
        /// <param name="exception"></param>
        public override void OnFailure(SigningRequest request, Exception exception)
        {
            StartCoroutine(TransitionPanels(FailurePanel));
            CheckTheme();
        }

        /// <summary>
        /// Method is invoked when a request to sign or login is made and the QR code and link are generated and displayed
        /// <para>Refer to https://github.com/greymass/anchor-link-browser-transport/blob/master/src/index.ts#L264.</para> 
        /// </summary>
        /// <param name="request"></param>
        public override void DisplayRequest(SigningRequest request)
        {
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
    }
}