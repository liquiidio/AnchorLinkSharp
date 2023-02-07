using System;
using System.Collections;
using System.Linq;
using AnchorLinkSharp;
using EosioSigningRequest;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AnchorLinkTransportSharp.Src.Transports.Canvas
{
    public class UnityCanvasTransport : UnityTransport
    {
        //! Used to manage the countdown timer
        private Coroutine counterCoroutine = null;
        //! The panel that is active/is being displayed
        public GameObject currentPanel;
        //! Toggle this to use light or dark theme
        [SerializeField] internal bool useLightTheme = false;

        #region Login-Panel
        [Header("Login Panel Panel Components")]
        //! The holding panel for the login details
        [SerializeField] public GameObject LoginPanel;
        //! Confirmation panel for when the link has been successfully copied
        [SerializeField] private GameObject HyperlinkCopiedNotificationPanel;
        //! Primary panel for the login prompt
        [SerializeField] private GameObject LoginSubpanel;
        //! Panel to show manual verification details (when the countdown reaches 0)
        [SerializeField] private GameObject ManuallySignSubpanel;
        //! Button to close the login panel (and execute further code)
        [SerializeField] private Button CloseLoginPanelButton;
        //! Button which displays the ESR link QR code and doubles as a way to scale up the QR code
        [SerializeField] private Button StaticQRCodeHolderTargetButton;
        //! Button which displays the ESR link QR code and doubles as a way to scale down the QR code
        [SerializeField] private Button ResizableQRCodeHolderTargetButton;
        //! Animator that transitions the QR code scale size
        [SerializeField] private Animator ResizableQRCode_Animator;
        //! Button used to copy the current ESR link
        [SerializeField] private Button HyperlinkCopyButton;
        //! Button used to open Anchor Wallet desktop
        [SerializeField] private Button LaunchAnchorButton;

        //! Link that will show the url for the version
        private const string VersionURL = "https://www.github.com/greymass/anchor-link";
        //! Link that will go to the download page for anchor
        private const string DownloadURL = "https://www.greymass.com/en/anchor/download";
        #endregion

        #region Sign and countdown timer
        [Header("Countdown timer")]
        //! Panel used to prompt for signing
        [SerializeField] public GameObject SignPanel;
        //! Countdown information in the SignPanel prompt
        [SerializeField] private TextMeshProUGUI CountdownTextGUI;
        //! Property that updates the UI with the relevant time during countdown when assigned to
        private string CountdownText
        {
            get => CountdownTextGUI.text;
            set => CountdownTextGUI.text = value;
        }

        #endregion

        #region Other panels
        [Header("Other panels")]
        [SerializeField] public GameObject LoadingPanel;
        [SerializeField] public GameObject SuccessPanel;
        [SerializeField] public GameObject FailurePanel;
        [SerializeField] public GameObject TimeoutPanel;
        #endregion

        /// <summary>
        /// Transport constructor
        /// </summary>
        /// <param name="options"></param>
        public UnityCanvasTransport(TransportOptions options) : base(options)
        {

        }

        private void Awake()
        {
            if (useLightTheme)
                SwitchToLightTheme();

            ClearAllLinks();

            DisableAllPanels();
        }

        /// <summary>
        /// Toggle between the light and dark theme (default is dark)
        /// </summary>
        private void SwitchToLightTheme()
        {
            foreach (var _childImage in gameObject.GetComponentsInChildren<Image>(true))
            {
                if (_childImage.gameObject.name == "HeaderBorder")
                    _childImage.color = new Color(241, 241, 241);

                else if (_childImage.GetComponent<Animator>())
                    _childImage.enabled = false;

                else
                    _childImage.color = Color.white;
            }

            foreach (var _childText in gameObject.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                if (!_childText.transform.parent.GetComponentInParent<Button>())
                    _childText.color = Color.black;
            }

            foreach (var _childButton in gameObject.GetComponentsInChildren<Button>(true))
            {
                if (_childButton.transition == Selectable.Transition.ColorTint)
                {
                    if (_childButton.targetGraphic.GetType() == typeof(TextMeshProUGUI) && _childButton.GetComponent<Image>())
                        _childButton.GetComponent<Image>().color = Color.clear;

                    else if (_childButton.targetGraphic.GetType() == typeof(Image))
                    {
                        _childButton.image.color = Color.clear;

                        _childButton.targetGraphic = _childButton.GetComponentInChildren<TextMeshProUGUI>(true);
                    }

                    if (_childButton.transform.GetChild(0).GetComponentInChildren<Image>(true))
                    {
                        _childButton.transform.GetChild(0).GetComponentInChildren<Image>(true).enabled = false;
                    }

                    var _clrs = _childButton.colors;

                    _clrs.normalColor = new Color(0.2352941f, 0.3104641f, 0.5686275f);
                    _clrs.highlightedColor = new Color(0.4386792f, 0.5750751f, 1.0f);

                    _childButton.colors = _clrs;
                }

                else
                {
                    if (!_childButton.name.Contains("QR") && _childButton.GetComponent<Image>())
                    {
                        _childButton.GetComponent<Image>().color = Color.clear;

                        _childButton.GetComponentInChildren<TextMeshProUGUI>().color = Color.black;

                        if (_childButton.name == "HyperlinkCopyButton")
                            _childButton.transform.Find("CopyHyperlinkImage").GetComponentInChildren<Image>(true).color = Color.black;
                    }
                }
                //LaunchAnchorButton.transform.GetChild(0).GetComponentInChildren<Image>(true).enabled = false;
            }
        }

        /// <summary>
        /// Method is invoked when a request is made and user signing on the wallet is required
        /// <para>Refer to https://github.com/greymass/anchor-link-browser-transport/blob/master/src/index.ts#L361.</para> 
        /// </summary>
        public override void ShowLoading()
        {
            SwitchToNewPanel(LoadingPanel);
        }

        /// <summary>
        /// Method is invoked when a succesful signing request is completed
        /// <para>Refer to https://github.com/greymass/anchor-link-browser-transport/blob/master/src/index.ts#L680.</para> 
        /// </summary>
        /// <param name="request">/></param>
        /// <param name="result"></param>
        public override void OnSuccess(SigningRequest request, TransactResult result)
        {
            if (counterCoroutine != null)
                StopCoroutine(counterCoroutine);

            SwitchToNewPanel(SuccessPanel);

            ClearAllLinks();
        }

        /// <summary>
        /// Method is invoked when a signing request fails or is cancelled
        /// <para>Refer to https://github.com/greymass/anchor-link-browser-transport/blob/master/src/index.ts#L698.</para> 
        /// </summary>
        /// <param name="request"></param>
        /// <param name="exception"></param>
        public override void OnFailure(SigningRequest request, Exception exception)
        {
            SwitchToNewPanel(FailurePanel);

            ClearAllLinks();
        }

        /// <summary>
        /// Method is invoked when a request to sign or login is made and the QR code and link are generated and displayed
        /// <para>Refer to https://github.com/greymass/anchor-link-browser-transport/blob/master/src/index.ts#L264.</para> 
        /// </summary>
        /// <param name="request"></param>
        public override void DisplayRequest(SigningRequest request)
        {
            var ESRLinkUrl = request.Encode(false, true);  // This returns ESR link to be converted

            if (request.IsIdentity())
            {
                LoginSubpanel.SetActive(true);
                ManuallySignSubpanel.SetActive(false);
                SwitchToNewPanel(LoginPanel);
                ResizableQRCode_Animator.SetTrigger("doZoomOut");

                Color _targetBaseColor = useLightTheme ? (Color32)Color.white : new Color32(19, 27, 51, 255);
                var _targetPixelColor = useLightTheme ? Color.black : Color.white;

                var _tex = StringToQRCodeTexture2D(ESRLinkUrl, 512, 512, _targetBaseColor, _targetPixelColor);

                StaticQRCodeHolderTargetButton.GetComponent<Image>().enabled =
                   ResizableQRCodeHolderTargetButton.GetComponent<Image>().enabled = true;

                StaticQRCodeHolderTargetButton.GetComponent<Image>().sprite =
                    ResizableQRCodeHolderTargetButton.GetComponent<Image>().sprite =
                        Sprite.Create(_tex, new Rect(0.0f, 0.0f, _tex.width, _tex.height), new Vector2(0.5f, 0.5f), 100.0f);
            }
            else
            {
                Color _targetBaseColor = useLightTheme ? (Color32)Color.white : new Color32(19, 27, 51, 255);
                var _targetPixelColor = useLightTheme ? Color.black : Color.white;

                var _tex = StringToQRCodeTexture2D(ESRLinkUrl, 512, 512, _targetBaseColor, _targetPixelColor);

                StaticQRCodeHolderTargetButton.GetComponent<Image>().sprite =
                    ResizableQRCodeHolderTargetButton.GetComponent<Image>().sprite =
                        Sprite.Create(_tex, new Rect(0.0f, 0.0f, _tex.width, _tex.height), new Vector2(0.5f, 0.5f), 100.0f);

                StartTimer();

                Application.OpenURL(ESRLinkUrl);

            }

            HyperlinkCopyButton.onClick.RemoveAllListeners();
            HyperlinkCopyButton.onClick.AddListener(delegate
            {
                CopyToClipboard(ESRLinkUrl);
            }
            );

            LaunchAnchorButton.onClick.RemoveAllListeners();
            LaunchAnchorButton.onClick.AddListener(delegate
            {
                Application.OpenURL(ESRLinkUrl);
            }
            );
        }

        #region Canvas function-calls
        /// <summary>
        /// Executed when the version button is interacted with (on all panels)
        /// </summary>
        public void OnVersionButtonPressed()
        {
            Application.OpenURL(VersionURL);
        }
        /// <summary>
        /// Executed when download Anchor button is interacted with
        /// </summary>
        public void OnDownloadAnchorButtonPressed()
        {
            Application.OpenURL(DownloadURL);
        }
        /// <summary>
        /// Executed when the close button on the login panel is interacted with (on available panels)
        /// </summary>
        public void OnLoginPanelCloseButtonPressed()
        {
            ResizableQRCode_Animator.SetTrigger("doZoomOut");

            DisableTargetPanel(LoginPanel);
        }
        /// <summary>
        /// Executed when the QR code (in the ZOOMED OUT state) is interacted with
        /// </summary>
        /// <param name="resizableQRCodePanel">Similar QR panel with the animator component</param>
        public void OnStaticQRCodeHolderTargetButtonPressed(RectTransform resizableQRCodePanel)
        {
            resizableQRCodePanel.gameObject.SetActive(true);

            resizableQRCodePanel.GetComponent<Animator>().SetTrigger("doZoomIn");
        }
        /// <summary>
        /// Executed when the QR code (in the ZOOMED IN state) is interacted with
        /// </summary>
        /// <param name="resizableQRCodePanel">QR Panel with the animator component</param>
        public void OnResizableQRCodeHolderTargetButtonPressed(RectTransform resizableQRCodePanel)
        {
            resizableQRCodePanel.GetComponent<Animator>().SetTrigger("doZoomOut");

            StartCoroutine(ResizableQRCodePanelZoomOut(resizableQRCodePanel));
        }
        /// <summary>
        /// Method used to manage the zoom out mechanic
        /// </summary>
        /// <param name="resizableQRCodePanel">QR Panel with the animator component</param>
        /// <returns></returns>
        private IEnumerator ResizableQRCodePanelZoomOut(RectTransform resizableQRCodePanel)
        {
            yield return new WaitForSeconds(0.1f);

            yield return new WaitUntil(() => resizableQRCodePanel.GetComponent<Animator>().GetCurrentAnimatorClipInfo(0).FirstOrDefault().clip?.name == "NormalState");

            resizableQRCodePanel.gameObject.SetActive(false);
            resizableQRCodePanel.gameObject.SetActive(false);
        }
        /// <summary>
        /// Executed when the hyperlink button containing the ESR link is interacted with
        /// </summary>
        public void OnQRHyperlinkButtonPressed()
        {
            HyperlinkCopyButton.gameObject.SetActive(false);

            HyperlinkCopiedNotificationPanel.SetActive(true);

            StopCoroutine(nameof(ToggleHyperlinkCopyButton_Delayed));
            StartCoroutine(ToggleHyperlinkCopyButton_Delayed());
        }
        /// <summary>
        /// Coroutine to display if the ESR hyperlink button is interacted with to show a success state and revert back to default state
        /// </summary>
        /// <returns></returns>
        private IEnumerator ToggleHyperlinkCopyButton_Delayed()
        {
            yield return new WaitForSeconds(3.5f);
            HyperlinkCopyButton.gameObject.SetActive(true);

            HyperlinkCopiedNotificationPanel.SetActive(false);
        }
        /// <summary>
        /// Executed when the launch Anchor button is interacted with (opens Anchor Wallet desktop if installed)
        /// </summary>
        public void OnLaunchAnchorButtonPressed()
        {
            Debug.LogWarning("Launch Anchor Button pressed!");
            //Application.OpenURL(ESRLinkUrl);
        }
        /// <summary>
        /// Executed when the close button on the loading panel is interacted with
        /// </summary>
        public void OnCloseLoadingPanelButtonPressed()
        {
            Debug.LogWarning("Close loading Panel button has been pressed!");
            DisableTargetPanel(LoadingPanel);
        }
        /// <summary>
        /// Executed when the close button on the timeout panel is interacted with
        /// </summary>
        public void OnCloseTimeoutPanelButtonPressed()
        {
            Debug.LogWarning("Close timeout Panel button has been pressed!");
            DisableTargetPanel(TimeoutPanel);
        }
        /// <summary>
        /// Begin the countdown timer to show how much time is left to authorize request
        /// </summary>
        private void StartTimer()
        {
            if (counterCoroutine != null)
                StopCoroutine(counterCoroutine);

            SwitchToNewPanel(SignPanel);
            CountdownTextGUI.text = $"Sign - {TimeSpan.FromMinutes(2):mm\\:ss}";
            counterCoroutine = StartCoroutine(CountdownTimer(2));
        }
        /// <summary>
        /// Coroutine to manage the countdown timer and update the panel with remaining time
        /// </summary>
        /// <param name="counterDuration">Starting time for the countdown timer</param>
        /// <returns></returns>
        private IEnumerator CountdownTimer(float counterDuration = 3.5f)
        {
            float _newCounter = 0;
            while (_newCounter < counterDuration * 60)
            {
                _newCounter += Time.deltaTime;

                CountdownText = $"Sign - {TimeSpan.FromSeconds((counterDuration * 60) - _newCounter):mm\\:ss}";
                yield return null;
            }

            SwitchToNewPanel(TimeoutPanel);
        }
        /// <summary>
        /// Executed whent he manually sign button is interacted with
        /// </summary>
        public void OnSignManuallyButtonPressed()
        {
            if (counterCoroutine != null)
                StopCoroutine(counterCoroutine);

            LoginSubpanel.SetActive(false);
            ManuallySignSubpanel.SetActive(true);

            SwitchToNewPanel(LoginPanel);
            ResizableQRCode_Animator.SetTrigger("doZoomOut");

            StaticQRCodeHolderTargetButton.GetComponent<Image>().enabled =
                ResizableQRCodeHolderTargetButton.GetComponent<Image>().enabled = true;
        }
        /// <summary>
        /// Executed when the close sign panel button is interacted with
        /// </summary>
        public void OnCloseSignPanelButtonPressed()
        {
            Debug.LogWarning("Close sign Panel button has been pressed!");
            DisableTargetPanel(SignPanel);
        }
        /// <summary>
        /// Executed when the close success panel button is interacted with
        /// </summary>
        public void OnCloseSuccessPanelButtonPressed()
        {
            Debug.LogWarning("Close success Panel button has been pressed!");

            DisableTargetPanel(SuccessPanel);
        }
        /// <summary>
        /// Executed when the failure panel button is interacted with
        /// </summary>
        public void OnCloseFailurePanelButtonPressed()
        {
            Debug.LogWarning("Close failure Panel button has been pressed!");

            DisableTargetPanel(FailurePanel);
        }

        /// <summary>
        /// Remove all links from the QR code to avoid double/wrong sign links/actions/transactions
        /// </summary>
        private void ClearAllLinks()
        {
            StaticQRCodeHolderTargetButton.GetComponent<Image>().sprite =
                   ResizableQRCodeHolderTargetButton.GetComponent<Image>().sprite = null;

            StaticQRCodeHolderTargetButton.GetComponent<Image>().enabled =
                   ResizableQRCodeHolderTargetButton.GetComponent<Image>().enabled = false;
        }

        /// <summary>
        /// Hide any displayed panel and switch to the new supplied one
        /// </summary>
        /// <param name="toPanel"></param>
        public void SwitchToNewPanel(GameObject toPanel)
        {
            currentPanel?.SetActive(false);
            DisableAllPanels();

            currentPanel = toPanel;

            currentPanel.SetActive(true);
        }

        /// <summary>
        /// If there is any panel being displayed, hide it
        /// </summary>
        /// <param name="fallbackPanel">A return panel to display if needed</param>
        public void DisableCurrentPanel(GameObject fallbackPanel = null)
        {
            currentPanel?.SetActive(false);
            fallbackPanel?.SetActive(true);

            currentPanel = fallbackPanel;
        }

        /// <summary>
        /// If there is a specific panel being displayed hide it and show the fallback one if supplied
        /// </summary>
        /// <param name="targetPanel">Panel to hide</param>
        /// <param name="fallbackPanel">A return panel to display if needed</param>
        public void DisableTargetPanel(GameObject targetPanel, GameObject fallbackPanel = null)
        {
            targetPanel.SetActive(false);

            if (fallbackPanel != null)
            {
                SwitchToNewPanel(fallbackPanel);
            }
        }

        /// <summary>
        /// Hide all panels that are declared
        /// </summary>
        public void DisableAllPanels()
        {
            LoginPanel.SetActive(false);
            SignPanel.SetActive(false);
            LoadingPanel.SetActive(false);
            SuccessPanel.SetActive(false);
            FailurePanel.SetActive(false);
            TimeoutPanel.SetActive(false);
        }

        #endregion
    }
}
