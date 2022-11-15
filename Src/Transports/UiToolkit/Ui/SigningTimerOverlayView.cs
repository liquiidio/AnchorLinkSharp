using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Assets.Packages.AnchorLinkTransportSharp;
using EosioSigningRequest;
using UnityEngine;
using UnityEngine.UIElements;

namespace Assets.Packages.AnchorLinkTransportSharp.Src.Transports.UiToolkit.Ui
{
    public class SigningTimerOverlayView : ScreenBase
    {
        /*
         * Child-Controls
         */

        private Button _closeViewButton;

        private Label _versionLabel;
        private Label _singingTimerLabel;
        private Label _signManualLabel;

        /*
         * Fields, Properties
         */
        internal Coroutine CounterCoroutine = null;

        public string CountdownText
        {
            get { return _singingTimerLabel.text; }

            set { _singingTimerLabel.text = value; }
        }

        [SerializeField] internal UnityUiToolkitTransport UiToolkitTransport;
        void Start()
        {
            _closeViewButton = Root.Q<Button>("close-view-button");

            _versionLabel = Root.Q<Label>("version-label");
            _singingTimerLabel = Root.Q<Label>("anchor-link-title-label");
            _signManualLabel = Root.Q<Label>("anchor-link-manual-label");

            _versionLabel.text = UnityUiToolkitTransport.Version;

            BindButtons();
        }

        #region Button Binding

        private void BindButtons()
        {
            _closeViewButton.clickable.clicked += Hide;

            _versionLabel.RegisterCallback<ClickEvent>(evt =>
            {
                UiToolkitTransport.OpenVersion();
            });

            _signManualLabel.RegisterCallback<ClickEvent>(evt =>
            {
                UiToolkitTransport.QrCodeOverlayView.Rebind(true);
                StartCoroutine(UiToolkitTransport.TransitionScreens(UiToolkitTransport.QrCodeOverlayView));
            });

        }
        #endregion

        #region other

        public void StartCountdownTimer()
        {
            if (CounterCoroutine != null)
                StopCoroutine(CounterCoroutine);

            _singingTimerLabel.text = $"Sign - {TimeSpan.FromMinutes(2):mm\\:ss}";
            CounterCoroutine = StartCoroutine(ScheduleTimer(2));
        }

        public IEnumerator ScheduleTimer(float counterDuration = 3.5f)
        {
            float _newCounter = 0;
            while (_newCounter < counterDuration * 60)
            {
                _newCounter += Time.deltaTime;

                CountdownText = $"Sign - {TimeSpan.FromSeconds((counterDuration * 60) - _newCounter):mm\\:ss}";
                yield return null;
            }
            StartCoroutine(UiToolkitTransport.TransitionScreens(UiToolkitTransport.TimeoutOverlayView));
        }
        #endregion

    }
}
