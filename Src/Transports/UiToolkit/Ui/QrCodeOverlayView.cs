using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using AnchorLinkSharp;
using Assets.Packages.AnchorLinkTransportSharp;
using EosSharp.Core.Api.v1;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.UIElements;

namespace Assets.Packages.AnchorLinkTransportSharp.Src.Transports.UiToolkit.Ui
{
    [RequireComponent(typeof(QrCodeOverlayView))]
    public class QrCodeOverlayView : ScreenBase
    {
        /*
         * Child-Controls
         */

        private Button _closeViewButton;
        private Button _launchAnchorButton;

        private VisualElement _qrCodeBox;
        private VisualElement _alreadyCopied;
        private VisualElement _readyToCopy;

        private Label _downloadNowLabel;
        private Label _versionLabel;
        private Label _copyLabel;
        private Label _linkedCopiedLabel;
        private Label _loginTitleLabel;
        private Label _subtitleLabel;



        /*
         * Fields, Properties
         */
        [SerializeField] internal UnityUiToolkitTransport UiToolkitTransport;
        private readonly Vector3 _qrCurrentSize = new Vector3(1, 1);

        void Start()
        {
            _closeViewButton = Root.Q<Button>("close-view-button");
            _launchAnchorButton = Root.Q<Button>("launch-anchor-button");
            _downloadNowLabel = Root.Q<Label>("download-now-link-label");
            _versionLabel = Root.Q<Label>("version-label");
            _copyLabel = Root.Q<Label>("anchor-link-copy-label");
            _linkedCopiedLabel = Root.Q<Label>("anchor-linked-copied-label");
            _loginTitleLabel = Root.Q<Label>("anchor-link-title-label");
            _subtitleLabel = Root.Q<Label>("anchor-link-subtitle-label");
            _qrCodeBox = Root.Q<VisualElement>("qr-code-box");
            _alreadyCopied = Root.Q<VisualElement>("already-copied");
            _readyToCopy = Root.Q<VisualElement>("ready-to-copy");

            _versionLabel.text = UnityUiToolkitTransport.Version;

            BindButtons();
        }


        #region Button Binding
        private void BindButtons()
        {
            _qrCodeBox.transform.scale = new Vector3(1, 1);

            _closeViewButton.clickable.clicked += Hide;

            _downloadNowLabel.RegisterCallback<ClickEvent>(evt =>
            {
                UiToolkitTransport.OpenDownloadAnchorLink();
            });

            _versionLabel.RegisterCallback<ClickEvent>(evt =>
            {
                UiToolkitTransport.OpenVersion();
            });

            _copyLabel.RegisterCallback<ClickEvent>(evt =>
            {
                UiToolkitTransport.CopyToClipboard(UiToolkitTransport.ESRLink);
                StartCoroutine(SetText());
            });

            _qrCodeBox.RegisterCallback<ClickEvent>(evt =>
            {
                if (_qrCodeBox.style.scale.value.value == _qrCurrentSize )
                {
                    _qrCodeBox.transform.scale = new Vector3(2, 2);
                }
                else _qrCodeBox.transform.scale = new Vector3(1, 1);
            });

            //login to your anchor wallet and a session is created for your account.
            _launchAnchorButton.clickable.clicked +=() =>
            {
                UiToolkitTransport.StartAnchorDesktop();
            };
        }
        #endregion

        #region Rebind

        public void Rebind(Texture2D qrCodeTexture2D, bool isLogin, bool isSignManually)
        {
            _qrCodeBox.style.backgroundImage = qrCodeTexture2D;

            if (isLogin)
            {
                _loginTitleLabel.text = "Login";
                _subtitleLabel.text = "Scan the QR-code with Anchor on another device or use the button to open it here.";
            }
            if(isSignManually)
            {
                _loginTitleLabel.text = "Sign Manually";
                _subtitleLabel.text = "Want to sign with another device or didn’t get the signing request in your wallet, scan this QR or copy request and paste in app.";
            }
        }

        #endregion

        #region other

        public IEnumerator SetText(float counterDuration = 0.5f)
        {
            _readyToCopy.style.visibility = Visibility.Hidden;
            _readyToCopy.style.display = DisplayStyle.None;

            _alreadyCopied.style.visibility = Visibility.Visible;
            _alreadyCopied.style.display = DisplayStyle.Flex;

            _linkedCopiedLabel.text = "Link copied - Paste in Anchor";

            float _newCounter = 0;
            while (_newCounter < counterDuration * 2)
            {
                _newCounter += Time.deltaTime;
                yield return null;
            }

            _alreadyCopied.style.visibility = Visibility.Hidden;
            _alreadyCopied.style.display = DisplayStyle.None;

            _readyToCopy.style.visibility = Visibility.Visible;
            _readyToCopy.style.display = DisplayStyle.Flex;

            _copyLabel.text = "Copy request link";
        }
        #endregion
    }
}
