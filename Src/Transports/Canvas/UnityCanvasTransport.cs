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
        private Coroutine counterCoroutine = null;

        internal GameObject currentPanel;

        [SerializeField] internal bool useLightTheme = false;

        #region Login-Panel
        [Header("Login Panel Panel Components")]
        [SerializeField] internal GameObject LoginPanel;   // The holding panel for the login details
        [SerializeField] private GameObject HyperlinkCopiedNotificationPanel; // Confirmation panel for when the link has been successfully copied

        [SerializeField] private GameObject LoginSubpanel;
        [SerializeField] private GameObject ManuallySignSubpanel;

        //Buttons
        [SerializeField] private Button CloseLoginPanelButton;
        [SerializeField] private Button StaticQRCodeHolderTargetButton;
        [SerializeField] private Button ResizableQRCodeHolderTargetButton;
        [SerializeField] private Button HyperlinkCopyButton;
        [SerializeField] private Button LaunchAnchorButton;

        private const string VersionURL = "https://www.github.com/greymass/anchor-link";    // Link that will show the url for the version
        private const string DownloadURL = "https://www.greymass.com/en/anchor/download";   // Link that will go to the download page for anchor
        #endregion

        #region Sign and countdown timer
        [Header("Countdown timer")]
        [SerializeField] internal GameObject SignPanel;
        [SerializeField] private TextMeshProUGUI CountdownTextGUI;
        
        private string CountdownText
        {
            get
            {
                return CountdownTextGUI.text;
            }

            set
            {
                CountdownTextGUI.text = value;
            }
        }

        #endregion

        #region Other panels
        [Header("Other panels")]
        [SerializeField] internal GameObject LoadingPanel;
        [SerializeField] internal GameObject SuccessPanel;
        [SerializeField] internal GameObject FailurePanel;
        [SerializeField] internal GameObject TimeoutPanel;
        #endregion

        private void Awake()
        {
            if (useLightTheme)
                SwitchToLightTheme();

            ClearAllLinks();

            DisableAllPanels();
        }

        // Toggle between the light and dark theme (default is dark)
        private void SwitchToLightTheme()
        {
            foreach (var _childImage in GetComponentsInChildren<Image>(true))
            {
                if (_childImage.gameObject.name == "HeaderBorder")
                    _childImage.color = new Color(241, 241, 241);

                else if (_childImage.GetComponent<Animator>())
                    _childImage.enabled = false;

                else
                    _childImage.color = Color.white;
            }

            foreach (var _childText in GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                if (!_childText.GetComponentInParent<Button>(true))
                    _childText.color = Color.black;
            }

            foreach (var _childButton in GetComponentsInChildren<Button>(true))
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

        public UnityCanvasTransport(TransportOptions options) : base(options)
        {

        }


        // see https://github.com/greymass/anchor-link-browser-transport/blob/master/src/index.ts#L361
        public override void ShowLoading()
        {
           SwitchToNewPanel(LoadingPanel);
        }

        // see https://github.com/greymass/anchor-link-browser-transport/blob/master/src/index.ts#L680
        public override void OnSuccess(SigningRequest request, TransactResult result)
        {
            if (counterCoroutine != null)
                StopCoroutine(counterCoroutine);

            SwitchToNewPanel(SuccessPanel);

            ClearAllLinks();
        }

        // see https://github.com/greymass/anchor-link-browser-transport/blob/master/src/index.ts#L698
        public override void OnFailure(SigningRequest request, Exception exception)
        {
            SwitchToNewPanel(FailurePanel);

            ClearAllLinks();
        }

        // see https://github.com/greymass/anchor-link-browser-transport/blob/master/src/index.ts#L264
        public override void DisplayRequest(SigningRequest request)
        {
            var ESRLinkUrl = request.Encode(false, true);  // This returns ESR link to be converted

            if (request.IsIdentity())
            {
                LoginSubpanel.SetActive(true);
                ManuallySignSubpanel.SetActive(false);
                SwitchToNewPanel(LoginPanel);
                ResizableQRCodeHolderTargetButton.GetComponentInParent<Animator>(true).SetTrigger("doZoomOut");

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
                var _targetPixelColor = useLightTheme ? Color.black :  Color.white;

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

        // see https://github.com/greymass/anchor-link-browser-transport/blob/master/src/index.ts#L226
        public override void ShowDialog(string title = null, string subtitle = null, string type = null, System.Action action = null,
            object content = null)
        {
            Debug.Log("ShowDialog");
        }


        #region Canvas function-calls
        public void OnVersionButtonPressed()
        {
            Application.OpenURL(VersionURL);
        }

        public void OnDownloadAnchorButtonPressed()
        {
            Application.OpenURL(DownloadURL);
        }

        public void OnLoginPanelCloseButtonPressed()
        {
            ResizableQRCodeHolderTargetButton.GetComponentInParent<Animator>(true).SetTrigger("doZoomOut");

            DisableTargetPanel(LoginPanel);
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
            resizableQRCodePanel.gameObject.SetActive(false);
        }

        public void OnQRHyperlinkButtonPressed()
        {
            HyperlinkCopyButton.gameObject.SetActive(false);

            HyperlinkCopiedNotificationPanel.SetActive(true);

            StopCoroutine(nameof(ToggleHyperlinkCopyButton_Delayed));
            StartCoroutine(ToggleHyperlinkCopyButton_Delayed());
        }

        private IEnumerator ToggleHyperlinkCopyButton_Delayed()
        {
            yield return new WaitForSeconds(3.5f);
            HyperlinkCopyButton.gameObject.SetActive(true);

            HyperlinkCopiedNotificationPanel.SetActive(false);
        }

        public void OnLaunchAnchorButtonPressed()
        {
            Debug.LogWarning("Launch Anchor Button pressed!");
            //Application.OpenURL(ESRLinkUrl);
        }

        public void OnCloseLoadingPanelButtonPressed()
        {
            Debug.LogWarning("Close loading Panel button has been pressed!");
            DisableTargetPanel(LoadingPanel);
        }

        public void OnCloseTimeoutPanelButtonPressed()
        {
            Debug.LogWarning("Close timeout Panel button has been pressed!");
            DisableTargetPanel(TimeoutPanel);
        }

        private void StartTimer()
        {
            if (counterCoroutine != null)
                StopCoroutine(counterCoroutine);

            SwitchToNewPanel(SignPanel);
            CountdownTextGUI.text = $"Sign - {TimeSpan.FromMinutes(2):mm\\:ss}";
            counterCoroutine = StartCoroutine(CountdownTimer(2));
        }

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

        public void OnSignManuallyButtonPressed()
        {
            if (counterCoroutine != null)
                StopCoroutine(counterCoroutine);

            LoginSubpanel.SetActive(false);
            ManuallySignSubpanel.SetActive(true);

            SwitchToNewPanel(LoginPanel);
            ResizableQRCodeHolderTargetButton.GetComponentInParent<Animator>(true).SetTrigger("doZoomOut");

            StaticQRCodeHolderTargetButton.GetComponent<Image>().enabled =
                ResizableQRCodeHolderTargetButton.GetComponent<Image>().enabled = true;
        }

        public void OnCloseSignPanelButtonPressed()
        {
            Debug.LogWarning("Close sign Panel button has been pressed!");
            DisableTargetPanel(SignPanel);
        }

        public void OnCloseSuccessPanelButtonPressed()
        {
            Debug.LogWarning("Close success Panel button has been pressed!");

            DisableTargetPanel(SuccessPanel);
        }

        public void OnCloseFailurePanelButtonPressed()
        {
            Debug.LogWarning("Close failure Panel button has been pressed!");

            DisableTargetPanel(FailurePanel);
        }

        // remove all links from the QR code to avoid double/wrong sign links/actions/transactions
        private void ClearAllLinks()
        {
            StaticQRCodeHolderTargetButton.GetComponent<Image>().sprite =
                   ResizableQRCodeHolderTargetButton.GetComponent<Image>().sprite = null;

            StaticQRCodeHolderTargetButton.GetComponent<Image>().enabled =
                   ResizableQRCodeHolderTargetButton.GetComponent<Image>().enabled = false;
        }

        // hide any displayed panel and switch to the new supplied one
        internal void SwitchToNewPanel(GameObject toPanel)
        {
            currentPanel?.SetActive(false);
            DisableAllPanels();

            currentPanel = toPanel;

            currentPanel.SetActive(true);
        }

        // if there is any panel being displayed, hide it
        internal void DisableCurrentPanel(GameObject fallbackPanel = null)
        {
            currentPanel?.SetActive(false);
            fallbackPanel?.SetActive(true);

            currentPanel= fallbackPanel;
        }

        // if there is a specific panel being displayed hide it and show the fallback one if supplied
        internal void DisableTargetPanel(GameObject targetPanel, GameObject fallbackPanel = null)
        {
            targetPanel.SetActive(false);

            if (fallbackPanel != null)
            {
                SwitchToNewPanel(fallbackPanel);
            }
        }

        // Hide all panels
        internal void DisableAllPanels()
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
