using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Assets.Packages.AnchorLinkTransportSharp.Src.Transports.UiToolkit.Ui
{
    public class QrCodeOverlayView : ScreenBase
    {
        /*
         * Connected Views
         */

        /*
         * Child-Controls
         */

        private Button _closeViewButton;
        private Button _launchAnchorButton;

        private Label _downloadNowLabel;
        private Label _versionLabel;
        private Label _loginTitleLabel;
        private Label _subtitleLabel;


        /*
         * Fields, Properties
         */

        void Start()
        {
            _closeViewButton = Root.Q<Button>("close-view-button");
            _launchAnchorButton = Root.Q<Button>("launch-anchor-button");
            _downloadNowLabel = Root.Q<Label>("download-now-link-label");
            _versionLabel = Root.Q<Label>("version-label");
            _loginTitleLabel = Root.Q<Label>("anchor-link-title-label");
            _subtitleLabel = Root.Q<Label>("anchor-link-subtitle-label");

            BindButtons();
        }


        #region Button Binding
        private void BindButtons()
        {
            _closeViewButton.clickable.clicked += Hide;

            _downloadNowLabel.RegisterCallback<ClickEvent>(evt =>
            {
                Application.OpenURL(UnityUiToolkitTransport.DownloadAnchorUrl);
            });

            _versionLabel.RegisterCallback<ClickEvent>(evt =>
            {
                Application.OpenURL(UnityUiToolkitTransport.VersionUrl);
            });


            _launchAnchorButton.clickable.clicked += async () =>
            {
                // TODO
                // Open esrLink via
                // Application.OpenURL(esrLink);
                // here

                // You need to pass the esrLink to the View
                // from UnityUiToolkitTransport
                // in ShowRequest
                // first for sure.

                // Then SigningOverlayView or however it's called is shown
                // ( the one with the Timer)
                // and this view is hidden.
            };
        }
        #endregion

        #region Rebind

        public void Rebind()
        {
            _versionLabel.text = UnityUiToolkitTransport.Version;

            _loginTitleLabel.text = "Login";
            _subtitleLabel.text = "Scan the QR-code with Anchor on another device or use the button to open it here.";
        }

        #endregion

        #region other

        public void SignManually()
        {
            _loginTitleLabel.text = "Sign Manually";
            _subtitleLabel.text = "Want to sign with another device or didn’t get the signing request in your wallet, scan this QR or copy request and paste in app.";
        }
        #endregion
    }
}
