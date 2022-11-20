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
        public bool IsWhiteTheme;
        private readonly Vector3 _qrCurrentSize = new(1, 1);
        private SigningRequest _request;

        [SerializeField] internal StyleSheet DarkTheme;
        [SerializeField] internal StyleSheet WhiteTheme;

        [SerializeField] internal LoadingPanel LoadingPanel;
        [SerializeField] internal SigningTimerPanel SigningTimerPanel;

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

            if (IsWhiteTheme) _anchorLinkCopy.Hide();
                
            OnStart();
            BindButtons();
            CheckTheme();
        }

        #region Button Binding

        private void BindButtons()
        {
            _qrCodeBox.transform.scale = new Vector3(1, 1);

            _downloadNowLabel.RegisterCallback<ClickEvent>(evt => { UnityUiToolkitTransport.OpenDownloadAnchorLink(); });

            _anchorFootnote.RegisterCallback<ClickEvent>(evt => { UnityUiToolkitTransport.OpenDownloadAnchorLink(); });

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
                var esrLinkUri = _request.Encode(false, false);

                if (_request.IsIdentity())
                {
                    Application.OpenURL(esrLinkUri);
                }
                else
                {
                    StartCoroutine(UnityUiToolkitTransport.TransitionPanels(LoadingPanel));
                    Application.OpenURL(esrLinkUri);
                    StartCoroutine(UnityUiToolkitTransport.TransitionPanels(SigningTimerPanel));
                    SigningTimerPanel.StartCountdownTimer();
                    
                }
            };
        }

        #endregion

        #region Rebind

        public void Rebind(SigningRequest request, bool isLogin)
        {
            _request = request;
            var whiteQrCodeTexture = StringToQrCodeTexture2D(_request?.Encode(false, true), 512, 512,
                new Color32(0, 0, 0, 255), Color.white);

            var darkQrCodeTexture = StringToQrCodeTexture2D(_request?.Encode(false, true), 512, 512,
                new Color32(19, 27, 51, 255), Color.white);

            if (IsWhiteTheme)
                _qrCodeBox.style.backgroundImage = whiteQrCodeTexture;
            else _qrCodeBox.style.backgroundImage = darkQrCodeTexture;


            if (isLogin)
            {
                _loginTitleLabel.text = "Login";
                _subtitleLabel.text =
                    "Scan the QR-code with Anchor on another device or use the button to open it here.";
            }
            else
            {
                _loginTitleLabel.text = "Sign";
                _subtitleLabel.text =
                    "Scan the QR-code with Anchor on another device or use the button to open it here.";
            }
        }

        public void Rebind(bool isSignManually)
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

        public IEnumerator SetText(float counterDuration = 0.5f)
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
        ///     Call this to generate a QR code based on the parameters passed
        /// </summary>
        /// <param name="textForEncoding">The actual texture that will be encoded into a QRCode</param>
        /// <param name="textureWidth">How wide the new texture should be</param>
        /// <param name="textureHeight">How high the new texture should be</param>
        /// <returns></returns>
        public Texture2D StringToQrCodeTexture2D(string textForEncoding, int textureWidth,
            int textureHeight, Color32 baseColor = new(), Color32 pixelColor = new())
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
        ///     Puts the passed string into the clipboard buffer to be pasted elsewhere.
        /// </summary>
        /// <param name="targetString">Text to be copied to the buffer</param>
        public void CopyToClipboard(string targetString)
        {
            GUIUtility.systemCopyBuffer = targetString;
        }

        private void CheckTheme()
        {
            Root.styleSheets.Clear();

            if (IsWhiteTheme)
            {
                Root.styleSheets.Remove(DarkTheme);
                Root.styleSheets.Add(WhiteTheme);
            }
            else
            {

                Root.styleSheets.Remove(WhiteTheme);
                Root.styleSheets.Add(DarkTheme);
            }
        }

        #endregion
    }
}