using System;
using System.Threading.Tasks;
using AnchorLinkSharp;
using Assets.Packages.AnchorLinkTransportSharp.Src.StorageProviders;
using EosioSigningRequest;
using UnityEngine;
using ZXing;
using ZXing.QrCode;

namespace Assets.Packages.AnchorLinkTransportSharp.Src
{
    public abstract class UnityTransport : MonoBehaviour, ILinkTransport
    {
        private readonly bool _requestStatus;
        private SigningRequest _activeRequest;
        private Action<object> _activeCancel;
        public ILinkStorage Storage { get; }

        public UnityTransport(TransportOptions options)
        {
            _requestStatus = options.RequestStatus != false;
            Storage = new PlayerPrefsStorage(options.StoragePrefix);
        }

        // see https://github.com/greymass/anchor-link-browser-transport/blob/master/src/index.ts#L374
        // and https://github.com/greymass/anchor-link-console-transport/blob/master/src/index.ts#L10
        public void OnRequest(SigningRequest request, Action<object> cancel)
        {
            _activeRequest = request;
            _activeCancel = cancel;
            var uri = request.Encode(false, true);
            Console.WriteLine(uri);

            DisplayRequest(request);
        }

        public void OnSessionRequest(LinkSession session, SigningRequest request, Action<object> cancel)
        {
            if (session is LinkFallbackSession)
            {
                OnRequest(request, cancel);
                return;
            }

            _activeRequest = request;
            _activeCancel = cancel;

            var subTitle = session.Metadata.ContainsKey("name")
                ? $"Please open Anchor Wallet on “${session.Metadata["name"]}” to review and sign the transaction."
                : "Please review and sign the transaction in the linked wallet.";
            var title = "Sign";
            ShowDialog(title, subTitle);
        }

        public async Task<SigningRequest> Prepare(SigningRequest request, LinkSession session = null)
        {
            return request;
        }

        #region Util methods
        /// <summary>
        /// Puts the passed string into the clipboard buffer to be pasted elsewhere.
        /// </summary>
        /// <param name="targetString">Text to be copied to the buffer</param>
        public void CopyToClipboard(string targetString) => GUIUtility.systemCopyBuffer = targetString;

        /// <summary>
        /// Call this to generate a QR code based on the parameters passed
        /// </summary>
        /// <param name="textForEncoding">The actual texture that will be encoded into a QRCode</param>
        /// <param name="textureWidth">How wide the new texture should be</param>
        /// <param name="textureHeight">How high the new texture should be</param>
        /// <returns></returns>
        public Texture2D StringToQRCodeTexture2D(string textForEncoding,
                                                 int textureWidth = 256, int textureHeight = 256,
                                                 Color32 baseColor = new Color32(), Color32 pixelColor = new Color32())
        {
            Texture2D newTexture2D = new Texture2D(textureWidth, textureHeight);

            var encodedData = StringEncoder(textForEncoding, newTexture2D.width, newTexture2D.height);

            for (int x = 0; x < encodedData.Length; x++)
            {
                // If there is an assigned base colour for each white "pixel" convert it to the base colour
                if (baseColor != Color.clear && encodedData[x] == Color.white)
                {
                    encodedData[x] = baseColor;
                }
                // If there is an assigned pixelColor colour for each black "pixel" convert it to the pixelColor colour
                else if (pixelColor != Color.clear && encodedData[x] == Color.black)
                {
                    encodedData[x] = pixelColor;
                }
            }
            newTexture2D.SetPixels32(encodedData);
            newTexture2D.Apply();

            return newTexture2D;
        }

        private Color32[] StringEncoder(string textForEncoding, int width, int height)
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

            return barcodeWriter.Write(textForEncoding);
        }
        #endregion

        // Future attempt to add an image overlay
        //public Bitmap GenerateQR(int width, int height, string text)
        //{
        //    var bw = new ZXing.BarcodeWriter();

        //    var encOptions = new ZXing.Common.EncodingOptions
        //    {
        //        Width = width,
        //        Height = height,
        //        Margin = 0,
        //        PureBarcode = false
        //    };

        //    encOptions.Hints.Add(EncodeHintType.ERROR_CORRECTION, ErrorCorrectionLevel.H);

        //    bw.Renderer = new BitmapRenderer();
        //    bw.Options = encOptions;
        //    bw.Format = ZXing.BarcodeFormat.QR_CODE;
        //    Bitmap bm = bw.Write(text);
        //    Bitmap overlay = new Bitmap(imagePath);

        //    int deltaHeigth = bm.Height - overlay.Height;
        //    int deltaWidth = bm.Width - overlay.Width;

        //    Graphics g = Graphics.FromImage(bm);
        //    g.DrawImage(overlay, new Point(deltaWidth / 2, deltaHeigth / 2));

        //    return bm;
        //}

        public abstract void ShowLoading();

        public abstract void OnSuccess(SigningRequest request, TransactResult result);

        public abstract void OnFailure(SigningRequest request, Exception exception);

        public abstract void DisplayRequest(SigningRequest request);

        public abstract void ShowDialog(string title = null, string subtitle = null, string type = null, Action action = null, object content = null);

    }
}