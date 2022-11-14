using System;
using System.Collections;
using System.Linq;
using AnchorLinkSharp;
using EosioSigningRequest;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Packages.AnchorLinkTransportSharp.Src.Transports.Canvas
{
    public class UnityCanvasTransport : UnityTransport
    {
        #region Login-Panel
        [Header("Login Panel Panel Components")]
        public GameObject LoginPanel;   // The holding panel for the login details
        public GameObject HyperlinkCopiedNotificationPanel; // Confirmation panel for when the link has been successfully copied

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

        public UnityCanvasTransport(TransportOptions options) : base(options)
        {

        }


        // see https://github.com/greymass/anchor-link-browser-transport/blob/master/src/index.ts#L361
        public override void ShowLoading()
        {
            Debug.Log("ShowLoading");

            LoadingPanel.SetActive(true);
        }

        // see https://github.com/greymass/anchor-link-browser-transport/blob/master/src/index.ts#L680
        public override void OnSuccess(SigningRequest request, TransactResult result)
        {
            Debug.Log("OnSuccess");

            SuccessPanel.SetActive(true);
        }

        // see https://github.com/greymass/anchor-link-browser-transport/blob/master/src/index.ts#L698
        public override void OnFailure(SigningRequest request, Exception exception)
        {
            Debug.Log("OnFailure");

            Debug.LogWarning($"FailurePanel's name is {FailurePanel.name}");

            FailurePanel.SetActive(true);
        }

        // see https://github.com/greymass/anchor-link-browser-transport/blob/master/src/index.ts#L264
        public override void DisplayRequest(SigningRequest request)
        {
            Debug.Log("DisplayRequest");
            var esrLinkUri = request.Encode(false, false);  // This returns ESR link to be converted

            if (request.IsIdentity())
            {
                LoginPanel.SetActive(true);

                var _tex = StringToQRCodeTexture2D(esrLinkUri);

                StaticQRCodeHolderTargetButton.GetComponent<Image>().sprite =
                    ResizableQRCodeHolderTargetButton.GetComponent<Image>().sprite =
                        Sprite.Create(_tex, new Rect(0.0f, 0.0f, _tex.width, _tex.height), new Vector2(0.5f, 0.5f), 100.0f);
            }
            else
            {
                Application.OpenURL(esrLinkUri);
            }
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
            resizableQRCodePanel.gameObject.SetActive(false);
        }

        public void OnQRHyperlinkButtonPressed()
        {
            HyperlinkCopyButton.gameObject.SetActive(false);

            HyperlinkCopiedNotificationPanel.SetActive(true);

            // TODO, pass this string from other View, don't work with class-level variable
            //CopyToClipboard(ESRLink);

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
            Application.OpenURL("");// TODO
        }

        public void OnCloseLoadingPanelButtonPressed()
        {
            Debug.LogWarning("Close loading Panel button has been pressed!");
            LoadingPanel.gameObject.SetActive(false);
        }

        public void OnCloseTimeoutPanelButtonPressed()
        {
            Debug.LogWarning("Close timeout Panel button has been pressed!");
            TimeoutPanel.gameObject.SetActive(false);
        }

        public void StartTimer()
        {

            if (counterCoroutine != null)
                StopCoroutine(counterCoroutine);

            SignPanel.SetActive(true);
            CountdownTextGUI.text = $"Sign - {TimeSpan.FromMinutes(2):mm\\:ss}";
            counterCoroutine = StartCoroutine(CountdownTimer(this, 2));
        }

        public void OnSignManuallyButtonPressed()
        {
            Debug.LogWarning("Sign manually button has been pressed!");
        }

        public void OnCloseSignPanelButtonPressed()
        {
            Debug.LogWarning("Close sign Panel button has been pressed!");
            SignPanel.gameObject.SetActive(false);
        }

        public void OnCloseSuccessPanelButtonPressed()
        {
            Debug.LogWarning("Close success Panel button has been pressed!");

            SuccessPanel.SetActive(false);

            //LoginPanel.SetActive(false);
        }

        public void OnCloseFailurePanelButtonPressed()
        {
            Debug.LogWarning("Close failure Panel button has been pressed!");

            FailurePanel.SetActive(false);
        }
        #endregion
    }
}
