using System;
using System.Collections;
using System.Diagnostics;
using System.Threading.Tasks;
using AnchorLinkSharp;
using Assets.Packages.AnchorLinkTransportSharp.Src.StorageProviders;
using Assets.Packages.AnchorLinkTransportSharp.Src.Transports.Canvas;
using Assets.Packages.AnchorLinkTransportSharp.Src.Transports.UiToolkit;
using EosioSigningRequest;
using UnityEngine;
using ZXing;
using ZXing.QrCode;

namespace Assets.Packages.AnchorLinkTransportSharp.Src
{
    public abstract class UnityTransport : MonoBehaviour, ILinkTransport
    {
        private readonly bool _requestStatus;
        private readonly bool _fuelEnabled;
        private SigningRequest _activeRequest;
        private object _activeCancel; //?: (reason: string | Error) => void

        internal ProcessStartInfo Ps;
        //internal Timer _countdownTimer;
        //internal Timer _closeTimer;
        public ILinkStorage Storage { get; }

        internal Coroutine counterCoroutine = null;

        public UnityTransport(TransportOptions options)
        {
            this._requestStatus = options.RequestStatus != false;
            this._fuelEnabled = options.DisableGreymassFuel != true;
            this.Storage = new PlayerPrefsStorage(options.StoragePrefix);
        }

        // see https://github.com/greymass/anchor-link-browser-transport/blob/master/src/index.ts#L374
        // and https://github.com/greymass/anchor-link-console-transport/blob/master/src/index.ts#L10
        public void OnRequest(SigningRequest request, System.Action<object> cancel)
        {
            this._activeRequest = request;
            this._activeCancel = cancel;
            var uri = request.Encode(false, true);
            Console.WriteLine(uri);

            //Ps = new ProcessStartInfo(uri)
            //{
            //    UseShellExecute = true,
            //};
            // TODO
             //Application.OpenURL(uri); //has to be called instead

            DisplayRequest(request);
        }

        public void OnSessionRequest(LinkSession session, SigningRequest request, object cancel)
        {
            if (session is LinkFallbackSession)
            {
                this.OnRequest(request, null);  // TODO CancellationToken?
                return;
            }

            this._activeRequest = request;
            this._activeCancel = cancel;

            var subTitle = session.Metadata.ContainsKey("name")
                ? $"Please open Anchor Wallet on “${session.Metadata["name"]}” to review and sign the transaction."
                : "Please review and sign the transaction in the linked wallet.";
            var title = "Sign";
            this.ShowDialog(title, subTitle);
        }

        public async Task<SigningRequest> Prepare(SigningRequest request, LinkSession session = null)
        {
            //    this.showLoading()
            if (!this._fuelEnabled || session == null || request.IsIdentity())
            {
                // don't attempt to cosign id request or if we don't have a session attached
                return request;
            }
            try
            {
                var result = FuelSharp.Fuel(request, session /*, this.updatePrepareStatus.bind(this)*/);
                if (await Task.WhenAny(result, Task.Delay(3500)) != result)
                {
                    throw new Exception("Fuel API timeout after 3500ms");
                }

                return result.Result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Not applying fuel( {ex.Message})");
            }
            return request;
        }

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
            Texture2D _newTexture2D = new(textureWidth, textureHeight);

            if (baseColor == Color.clear)
                baseColor = Color.white;
            if (pixelColor == Color.clear)
                pixelColor = Color.black;

            _newTexture2D.SetPixels32(StringEncoder(textForEncoding, _newTexture2D.width, _newTexture2D.height, baseColor, pixelColor));
            _newTexture2D.Apply();

            return _newTexture2D;
        }

        private Color32[] StringEncoder(string textForEncoding, 
                                        int width, int height,
                                         Color32 baseColor, Color32 pixelColor)
        {
            var _barcodeWriter = new BarcodeWriter
            {
                Format = BarcodeFormat.QR_CODE,

                Options = new QrCodeEncodingOptions
                {
                    Width = width,
                    Height = height
                }
            };

            Color32[] _color32Array = _barcodeWriter.Write(textForEncoding);

            //foreach (var _c in _color32Array)
            //    print($"Color code is {_c}");
        
            for (int x = 0; x < _color32Array.Length; x++)
            {
                if (_color32Array[x] == Color.white)
                {
                    _color32Array[x] = baseColor;
                }

                else if (_color32Array[x] == Color.black)
                {
                    _color32Array[x] = pixelColor;
                }
            }


            return _color32Array;
        }

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

        public abstract void ShowDialog(string title = null, string subtitle = null, string type = null, System.Action action = null, object content = null);

    }
}