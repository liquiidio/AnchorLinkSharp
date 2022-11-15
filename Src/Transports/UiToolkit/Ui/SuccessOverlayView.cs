using System;
using System.Collections;
using System.Collections.Generic;
using AnchorLinkSharp;
using Assets.Packages.AnchorLinkTransportSharp;
using EosioSigningRequest;
using UnityEngine;
using UnityEngine.UIElements;

namespace Assets.Packages.AnchorLinkTransportSharp.Src.Transports.UiToolkit.Ui
{
    [RequireComponent(typeof(SuccessOverlayView))]
    public class SuccessOverlayView : ScreenBase
    {
        /*
         * Child-Controls
         */

        private Button _closeViewButton;

        private Label _versionLabel;


        /*
         * Fields, Properties
         */
        [SerializeField] internal UnityUiToolkitTransport UiToolkitTransport;

        void Start()
        {
            _closeViewButton = Root.Q<Button>("close-view-button");
            _versionLabel = Root.Q<Label>("version-label");

            _versionLabel.text = UnityUiToolkitTransport.Version;

            BindButtons();
        }

        #region Button Binding

        private void BindButtons()
        {
            _closeViewButton.clickable.clicked += Hide;

            _versionLabel.RegisterCallback<ClickEvent>(evt =>
            {
                UiToolkitTransport.OpenVersion(); ;
            });

        }
        #endregion

        #region other

        public void CloseTimer()
        {
            StartCoroutine(SetTimeout());
        }

        public IEnumerator SetTimeout(float counterDuration = 0.5f)
        {
            float _newCounter = 0;
            while (_newCounter < counterDuration * 2)
            {
                _newCounter += Time.deltaTime;
                yield return null;
            }
            this.Hide();
        }
        #endregion
    }
}
