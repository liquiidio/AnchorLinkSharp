using System;
using System.Collections;
using System.Linq;
using AnchorLinkSharp;
using Assets.Packages.AnchorLinkTransportSharp.Src.Transports.UiToolkit;
using EosioSigningRequest;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Packages.AnchorLinkTransportSharp.Src.Transports.Canvas
{
    public class UnityCanvasTransport : UnityTransport
    {
        //private string ESRLinkUrl = "";

        internal GameObject currentPanel;

        #region Login-Panel
        [Header("Login Panel Panel Components")]
        public GameObject LoginPanel;   // The holding panel for the login details
        public GameObject HyperlinkCopiedNotificationPanel; // Confirmation panel for when the link has been successfully copied

        public GameObject LoginSubpanel;
        public GameObject ManuallySignSubpanel;

        //Buttons
        public Button CloseLoginPanelButton;
        public Button StaticQRCodeHolderTargetButton;
        public Button ResizableQRCodeHolderTargetButton;
        public Button HyperlinkCopyButton;
        public Button LaunchAnchorButton;

        const string VersionURL = "https://www.github.com/greymass/anchor-link";    // Link that will show the url for the version
        const string DownloadURL = "https://www.greymass.com/en/anchor/download";   // Link that will go to the download page for anchor
        #endregion

        #region Sign and countdown timer
        [Header("Countdown timer")]
        public GameObject SignPanel;
        public TextMeshProUGUI CountdownTextGUI;
        public string CountdownText
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
        public GameObject LoadingPanel;
        public GameObject SuccessPanel;
        public GameObject FailurePanel;
        public GameObject TimeoutPanel;
        #endregion

        private void Awake()
        {
            ClearAllLinks();

            DisableAllPanels();
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
            var ESRLinkUrl = request.Encode(false, false);  // This returns ESR link to be converted

            if (request.IsIdentity())
            {
                LoginSubpanel.SetActive(true);
                ManuallySignSubpanel.SetActive(false);
                SwitchToNewPanel(LoginPanel);

                var _tex = StringToQRCodeTexture2D(ESRLinkUrl, 512, 512, new Color32(19, 27, 51, 255), Color.white);

                StaticQRCodeHolderTargetButton.GetComponent<Image>().enabled =
                   ResizableQRCodeHolderTargetButton.GetComponent<Image>().enabled = true;

                StaticQRCodeHolderTargetButton.GetComponent<Image>().sprite =
                    ResizableQRCodeHolderTargetButton.GetComponent<Image>().sprite =
                        Sprite.Create(_tex, new Rect(0.0f, 0.0f, _tex.width, _tex.height), new Vector2(0.5f, 0.5f), 100.0f);
            }
            else
            {
                Application.OpenURL(ESRLinkUrl);

                var _tex = StringToQRCodeTexture2D(ESRLinkUrl, 512, 512, new Color32(19, 27, 51, 255), Color.white);

                StaticQRCodeHolderTargetButton.GetComponent<Image>().sprite =
                    ResizableQRCodeHolderTargetButton.GetComponent<Image>().sprite =
                        Sprite.Create(_tex, new Rect(0.0f, 0.0f, _tex.width, _tex.height), new Vector2(0.5f, 0.5f), 100.0f);

                StartTimer();
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

            //CopyToClipboard(ESRLinkUrl);

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

        public void StartTimer()
        {
            if (counterCoroutine != null)
                StopCoroutine(counterCoroutine);

            SwitchToNewPanel(SignPanel);
            CountdownTextGUI.text = $"Sign - {TimeSpan.FromMinutes(2):mm\\:ss}";
            counterCoroutine = StartCoroutine(CountdownTimer(2));
        }

        public IEnumerator CountdownTimer(float counterDuration = 3.5f)
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
            Debug.LogWarning("Sign manually button has been pressed!");

            LoginSubpanel.SetActive(false);
            ManuallySignSubpanel.SetActive(true);
            SwitchToNewPanel(LoginPanel);

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

        private void ClearAllLinks()
        {
            //ESRLinkUrl = "";

            StaticQRCodeHolderTargetButton.GetComponent<Image>().sprite =
                   ResizableQRCodeHolderTargetButton.GetComponent<Image>().sprite = null;

            StaticQRCodeHolderTargetButton.GetComponent<Image>().enabled =
                   ResizableQRCodeHolderTargetButton.GetComponent<Image>().enabled = false;
        }

        internal void SwitchToNewPanel(GameObject toPanel)
        {
            currentPanel?.SetActive(false);
            DisableAllPanels();

            currentPanel = toPanel;

            currentPanel.SetActive(true);
        }

        internal void DisableCurrentPanel(GameObject fallbackPanel = null)
        {
            currentPanel?.SetActive(false);
            fallbackPanel?.SetActive(true);

            currentPanel= fallbackPanel;
        }


        private void DisableTargetPanel(GameObject targetPanel) => targetPanel.SetActive(false);

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
