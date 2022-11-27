using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

namespace Assets.Packages.AnchorLinkTransportSharp.Src.Transports.UiToolkit.Ui
{
    public class SigningTimerPanel : PanelBase
    {
        /*
         * Child-Controls
         */
        private Label _signManualLabel;
        private Label _singingTimerLabel;

        /*
         * Fields, Properties
         */
        public QrCodePanel QrCodePanel;
        public TimeoutPanel TimeoutPanel;
        private Coroutine _counterCoroutine;

        public string CountdownText
        {
            get => _singingTimerLabel.text;

            set => _singingTimerLabel.text = value;
        }

        private void Start()
        {
            _singingTimerLabel = Root.Q<Label>("anchor-link-title-label");
            _signManualLabel = Root.Q<Label>("anchor-link-manual-label");

            OnStart();
            BindButtons();
        }

        #region Button Binding

        private void BindButtons()
        {
            _signManualLabel.RegisterCallback<ClickEvent>(evt =>
            {
                QrCodePanel.Rebind(true);
                StartCoroutine(UnityUiToolkitTransport.TransitionPanels(QrCodePanel));
            });
        }

        #endregion

        #region other

        public void StartCountdownTimer()
        {
            if (_counterCoroutine != null)
                StopCoroutine(_counterCoroutine);

            _singingTimerLabel.text = $"Sign - {TimeSpan.FromMinutes(2):mm\\:ss}";
            _counterCoroutine = StartCoroutine(ScheduleTimer(2));
        }

        private IEnumerator ScheduleTimer(float counterDuration = 3.5f)
        {
            float _newCounter = 0;
            while (_newCounter < counterDuration * 60)
            {
                _newCounter += Time.deltaTime;

                CountdownText = $"Sign - {TimeSpan.FromSeconds(counterDuration * 60 - _newCounter):mm\\:ss}";
                yield return null;
            }

            StartCoroutine(UnityUiToolkitTransport.TransitionPanels(TimeoutPanel));
        }
        #endregion
    }
}