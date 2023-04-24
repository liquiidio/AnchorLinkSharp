using System;
using System.Threading.Tasks;
using AnchorLinkSharp;
using AnchorLinkTransportSharp.Src.StorageProviders;
using EosioSigningRequest;
using UnityEngine;
using ZXing;
using ZXing.QrCode;

namespace AnchorLinkTransportSharp.Src
{
    public abstract class UnityTransport : MonoBehaviour, ILinkTransport
    {
        //! The Status of the current Request
        private readonly bool _requestStatus;
        //! The current active Request
        private SigningRequest _activeRequest;
        //! The activeCancel Delegate (invoked when a Request is cancelled)
        private Action<object> _activeCancel;
        //! The Storage used for sessions and metadata
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

        /// <summary>
        /// Method is invoked when a request is made and user signing on the wallet is required
        /// </summary>
        public abstract void ShowLoading();

        /// <summary>
        /// Method is invoked when a succesful signing request is completed
        /// </summary>
        /// <param name="request"></param>
        /// <param name="result"></param>
        public abstract void OnSuccess(SigningRequest request, TransactResult result);

        /// <summary>
        /// Method is invoked when a signing request fails or is cancelled
        /// </summary>
        /// <param name="request"></param>
        /// <param name="exception"></param>
        public abstract void OnFailure(SigningRequest request, Exception exception);

        /// <summary>
        /// Method is invoked when a request to sign or login is made and the QR code and link are generated and displayed
        /// </summary>
        /// <param name="request"></param>
        public abstract void DisplayRequest(SigningRequest request);

    }
}