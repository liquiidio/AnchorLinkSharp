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
        private string _timeFormat = "mm\\:ss";
        private DateTime _pendingUntil;
        private TimeSpan _remainingTime;

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
                Application.OpenURL(UnityUiToolkitTransport.VersionUrl);
            });

            _signManualLabel.RegisterCallback<ClickEvent>(evt =>
            {
                Hide();
                //UiToolkitTransport.QrCodeOverlayView.Rebind();
            });

        }
        #endregion

        #region other

        public void StartCountdownTimer()
        {
            _pendingUntil = DateTime.UtcNow.AddSeconds(120);
            _singingTimerLabel.schedule.Execute((ts) => ScheduleTimer(ts, _singingTimerLabel));
        }
        
        private void ScheduleTimer(TimerState timerState, VisualElement element)
        {
            if (_pendingUntil >= DateTime.UtcNow)
            {
                _remainingTime = _pendingUntil - DateTime.UtcNow;
                _singingTimerLabel.text = $"Sign - {_remainingTime.ToString("mm\\:ss")}";
            }
            else
               UiToolkitTransport.TimeoutOverlayView.Show();

            _singingTimerLabel.schedule.Execute((ts) => ScheduleTimer(ts, _singingTimerLabel));
        }

        #endregion

    }
}
