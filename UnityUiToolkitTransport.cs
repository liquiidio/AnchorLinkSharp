using System;
using AnchorLinkSharp;
using Assets.Packages.AnchorLinkTransportSharp.UI.ScriptsAndUxml;

using EosioSigningRequest;
using UnityEngine;

namespace Assets.Packages.AnchorLinkTransportSharp
{
    public class UnityUiToolkitTransport : UnityTransport
    {
        [SerializeField] internal SuccessOverlayView SuccessOverlayView;
        [SerializeField] internal FailureOverlayView FailureOverlayView;
        [SerializeField] internal QrCodeOverlayView QrCodeOverlayView;
        [SerializeField] internal LoadingOverlayView LoadingOverlayView;
        [SerializeField] internal SigningTimerOverlayView SigningTimerOverlayView;
        [SerializeField] internal TimeoutOverlayView TimeoutOverlayView;

        // BASE-CLASS HAS FOLLOWING FIELDS
        //private readonly bool _requestStatus;
        //private readonly bool _fuelEnabled;
        //private SigningRequest _activeRequest;
        //private object _activeCancel; //?: (reason: string | Error) => void
        //private Timer _countdownTimer;
        //private Timer _closeTimer;
        public UnityUiToolkitTransport(TransportOptions options) : base(options)
        {
            SuccessOverlayView = GameObject.FindObjectOfType<SuccessOverlayView>();
            FailureOverlayView = GameObject.FindObjectOfType<FailureOverlayView>();
            QrCodeOverlayView = GameObject.FindObjectOfType<QrCodeOverlayView>();
            LoadingOverlayView = GameObject.FindObjectOfType<LoadingOverlayView>();
            SigningTimerOverlayView = GameObject.FindObjectOfType<SigningTimerOverlayView>();
            TimeoutOverlayView = GameObject.FindObjectOfType<TimeoutOverlayView>();
        }

        // see https://github.com/greymass/anchor-link-browser-transport/blob/master/src/index.ts#L361
        // and https://github.com/greymass/anchor-link-console-transport/blob/master/src/index.ts#L10
        public override void ShowLoading()
        {
            LoadingOverlayView.Show();
        }

        // see https://github.com/greymass/anchor-link-browser-transport/blob/master/src/index.ts#L680
        public override void OnSuccess(SigningRequest request, TransactResult result)
        {
            SuccessOverlayView.Show();
            QrCodeOverlayView.Hide();
        }

        // see https://github.com/greymass/anchor-link-browser-transport/blob/master/src/index.ts#L698
        public override void OnFailure(SigningRequest request, Exception exception)
        {
            FailureOverlayView.Show();
        }

        // see https://github.com/greymass/anchor-link-browser-transport/blob/master/src/index.ts#L264
        public override void DisplayRequest(SigningRequest request)
        {
        }

        // see https://github.com/greymass/anchor-link-browser-transport/blob/master/src/index.ts#L226
        public override void ShowDialog(string title = null, string subtitle = null, string type = null, Action action = null,
            object content = null)
        {
        }
    }
}
