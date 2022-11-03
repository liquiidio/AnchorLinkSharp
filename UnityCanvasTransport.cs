using System;
using System.Collections;
using System.Linq;
using AnchorLinkSharp;
using EosioSigningRequest;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Packages.AnchorLinkTransportSharp
{
    public class UnityCanvasTransport : UnityTransport
    {
        // BASE-CLASS HAS FOLLOWING FIELDS
        //private readonly bool _requestStatus;
        //private readonly bool _fuelEnabled;
        //private SigningRequest _activeRequest;
        //private object _activeCancel; //?: (reason: string | Error) => void
        //private Timer _countdownTimer;
        //private Timer _closeTimer;



        #region Login-Screen
        [Header("Login Screen Panel Components")]
        public string ESRLink = ""; // Linke that will be converted to a QR code and can be copy from

        public GameObject LoginPanel;   // The holding panel for the login details
        public GameObject HyperlinkCopiedNotificationPanel; // Confirmation panel for when the link has been successfully copied

        //Buttons
        public Button CloseLoginScreenButton;
        public Button StaticQRCodeHolderTargetButton;
        public Button ResizableQRCodeHolderTargetButton;
        public Button HyperlinkCopyButton;
        public Button LaunchAnchorButton;

        #endregion

        public UnityCanvasTransport(TransportOptions options) : base(options)
        {

        }

        // see https://github.com/greymass/anchor-link-browser-transport/blob/master/src/index.ts#L361
        public override void ShowLoading()
        {
            throw new NotImplementedException();
        }

        // see https://github.com/greymass/anchor-link-browser-transport/blob/master/src/index.ts#L680
        public override void OnSuccess(SigningRequest request, TransactResult result)
        {
            throw new NotImplementedException();
        }

        // see https://github.com/greymass/anchor-link-browser-transport/blob/master/src/index.ts#L698
        public override void OnFailure(SigningRequest request, Exception exception)
        {
            throw new NotImplementedException();
        }

        // see https://github.com/greymass/anchor-link-browser-transport/blob/master/src/index.ts#L264
        public override void DisplayRequest(SigningRequest request)
        {
            throw new NotImplementedException();
        }

        // see https://github.com/greymass/anchor-link-browser-transport/blob/master/src/index.ts#L226
        public override void ShowDialog(string title = null, string subtitle = null, string type = null, Action action = null,
            object content = null)
        {
            throw new NotImplementedException();
        }


        #region Canvas function-calls
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

            yield return new WaitUntil(() => resizableQRCodePanel.GetComponent<Animator>().GetCurrentAnimatorClipInfo(0).FirstOrDefault().clip.name == "NormalState");

            resizableQRCodePanel.gameObject.SetActive(false);
        }

        public void OnQRHyperlinkButtonPressed()
        {
            HyperlinkCopyButton.gameObject.SetActive(false);

            HyperlinkCopiedNotificationPanel.gameObject.SetActive(true);

            CopyToClipboard(ESRLink);

            StartCoroutine(ToggleHyperlinkCopyButton_Delayed());
        }

        private IEnumerator ToggleHyperlinkCopyButton_Delayed()
        {
            yield return new WaitForSeconds(3.5f);
            HyperlinkCopyButton.gameObject.SetActive(true);

            HyperlinkCopiedNotificationPanel.gameObject.SetActive(false);
        }

        public void OnLaunchAnchorButtonPressed()
        {
            Debug.LogWarning("Call open Anchor function in UnityTransport.cs");
        }
        #endregion
    }
}
