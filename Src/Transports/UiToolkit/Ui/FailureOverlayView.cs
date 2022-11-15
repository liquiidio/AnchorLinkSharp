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
    public class FailureOverlayView : ScreenBase
    {
        /*
         * Child-Controls
         */

        private Button _closeViewButton;

        private Label _versionLabel;
        private Label _titleLabel;
        private Label _subtitleLabel;

        /*
         * Fields, Properties
         */

        void Start()
        {
            _closeViewButton = Root.Q<Button>("close-view-button");
            _versionLabel = Root.Q<Label>("version-label");
            _titleLabel = Root.Q<Label>("anchor-link-title-label");
            _subtitleLabel = Root.Q<Label>("anchor-link-subtitle-label");

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
        }
        #endregion

        #region others
        public void ExceptionHandler(Exception exception)
        {
            _titleLabel.text = "Transaction Error";
            _subtitleLabel.text = exception.Message;
        }

        #endregion

    }
}
