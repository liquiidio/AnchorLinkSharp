using System.Collections;
using EosioSigningRequest;
using UnityEngine;
using UnityEngine.UIElements;
using ZXing;
using ZXing.QrCode;

namespace Assets.Packages.AnchorLinkTransportSharp.Src.Transports.UiToolkit.Ui
{
    [RequireComponent(typeof(QrCodePanel))]
    public class QrCodePanel : PanelBase
    {
        /*
         * Fields, Properties
         */
        private readonly Vector3 _qrCurrentSize = new(1, 1);
        private SigningRequest _request;

        /*
         * Child-Controls
         */

        private Button _launchAnchorButton;

        private VisualElement _qrCodeBox;
        private VisualElement _readyToCopy;
        private VisualElement _alreadyCopied;
        private VisualElement _anchorFootnote;
        private VisualElement _anchorLinkCopy;

        private Label _subtitleLabel;
        private Label _copyLabel;
        private Label _downloadNowLabel;
        private Label _linkedCopiedLabel;
        private Label _loginTitleLabel;

        private void Start()
        {
            _launchAnchorButton = Root.Q<Button>("launch-anchor-button");
            _downloadNowLabel = Root.Q<Label>("download-now-link-label");
            _copyLabel = Root.Q<Label>("anchor-link-copy-label");
            _linkedCopiedLabel = Root.Q<Label>("anchor-linked-copied-label");
            _loginTitleLabel = Root.Q<Label>("anchor-link-title-label");
            _subtitleLabel = Root.Q<Label>("anchor-link-subtitle-label");
            _qrCodeBox = Root.Q<VisualElement>("qr-code-box");
            _alreadyCopied = Root.Q<VisualElement>("already-copied");
            _readyToCopy = Root.Q<VisualElement>("ready-to-copy");
            _anchorFootnote = Root.Q<VisualElement>("anchor-link-footnote");
            _anchorLinkCopy = Root.Q<VisualElement>("anchor-link-copy");

            OnStart();
            BindButtons();
        }

        #region Button Binding
        // assign UI toolkit interaction events
        private void BindButtons()
        {
            _qrCodeBox.transform.scale = new Vector3(1, 1);

            _downloadNowLabel.RegisterCallback<ClickEvent>(evt => { UnityUiToolkitTransport.OpenDownloadAnchorLink(); });

            _copyLabel.RegisterCallback<ClickEvent>(evt =>
            {
                CopyToClipboard(_request.Encode());
                StartCoroutine(SetText());
            });

            _qrCodeBox.RegisterCallback<ClickEvent>(evt =>
            {
                if (_qrCodeBox.style.scale.value.value == _qrCurrentSize)
                    _qrCodeBox.transform.scale = new Vector3(2, 2);
                else _qrCodeBox.transform.scale = new Vector3(1, 1);
            });

            _launchAnchorButton.clickable.clicked += () =>
            {
                var esrLinkUri = _request.Encode(false, true);
                Application.OpenURL(esrLinkUri);
            };
        }

        #endregion

        #region Rebind

        internal void Rebind(SigningRequest request, bool isLogin, bool isWhiteTheme)
        {
            _request = request;
            // create and get the QR code image
            _qrCodeBox.style.backgroundImage = isWhiteTheme
                ? (StyleBackground)StringToQrCodeTexture2D(_request?.Encode(false, true), 512, 512,
                new Color32(0, 0, 0, 255), Color.white)
                : (StyleBackground)StringToQrCodeTexture2D(_request?.Encode(false, true), 512, 512,
                new Color32(19, 27, 51, 255), Color.white);

            // user logging in
            if (isLogin)
            {
                _loginTitleLabel.text = "Login";
                _subtitleLabel.text =
                    "Scan the QR-code with Anchor on another device or use the button to open it here.";
            }
            // user trying to sign
            else
            {
                _loginTitleLabel.text = "Sign";
                _subtitleLabel.text =
                    "Scan the QR-code with Anchor on another device or use the button to open it here.";
            }
        }
        // if the user needs to sign manually
        internal void Rebind(bool isSignManually)
        {
            if (isSignManually)
            {
                _loginTitleLabel.text = "Sign Manually";
                _subtitleLabel.text =
                    "Want to sign with another device or didn’t get the signing request in your wallet, scan this QR or copy request and paste in app.";
            }
        }

        #endregion

        #region other
        // when the copy ESRLinkUrl button is interacted with
        private IEnumerator SetText(float counterDuration = 0.5f)
        {
            _readyToCopy.Hide();
            _alreadyCopied.Show();
            _linkedCopiedLabel.text = "Link copied - Paste in Anchor";

            float _newCounter = 0;
            while (_newCounter < counterDuration * 2)
            {
                _newCounter += Time.deltaTime;
                yield return null;
            }

            _alreadyCopied.Hide();
            _readyToCopy.Show();
            _copyLabel.text = "Copy request link";
        }


        /// <summary>
        /// Call this to generate a QR code based on the parameters passed
        /// </summary>
        /// <param name="textForEncoding">The actual texture that will be encoded into a QRCode</param>
        /// <param name="textureWidth">How wide the new texture should be</param>
        /// <param name="textureHeight">How high the new texture should be</param>
        /// <returns></returns>
        private Texture2D StringToQrCodeTexture2D(string textForEncoding, int textureWidth,
            int textureHeight, Color32 baseColor = new Color32(), Color32 pixelColor = new Color32())
        {
            Texture2D newTexture2D = new(textureWidth, textureHeight);

            if (baseColor == Color.clear)
                baseColor = Color.white;
            if (pixelColor == Color.clear)
                pixelColor = Color.black;

            newTexture2D.SetPixels32(StringEncoder(textForEncoding, newTexture2D.width, newTexture2D.height, baseColor,
                pixelColor));
            newTexture2D.Apply();

            return newTexture2D;
        }
        // convert string information into a color32 array which is used to create the QR code
        private Color32[] StringEncoder(string textForEncoding, int width, int height, Color32 baseColor,
            Color32 pixelColor)
        {
            var barcodeWriter = new BarcodeWriter
            {
                Format = BarcodeFormat.QR_CODE,

                Options = new QrCodeEncodingOptions
                {
                    Width = width,
                    Height = height
                }
            };

            var color32Array = barcodeWriter.Write(textForEncoding);

            for (var x = 0; x < color32Array.Length; x++)
                if (color32Array[x] == Color.white)
                    color32Array[x] = baseColor;

                else if (color32Array[x] == Color.black) color32Array[x] = pixelColor;


            return color32Array;
        }


        /// <summary>
        /// Puts the passed string into the clipboard buffer to be pasted elsewhere.
        /// </summary>
        /// <param name="targetString">Text to be copied to the buffer</param>
        private void CopyToClipboard(string targetString)
        {
            GUIUtility.systemCopyBuffer = targetString;
        }

        #endregion
    }
}