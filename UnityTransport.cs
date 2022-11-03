using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using AnchorLinkSharp;
using EosioSigningRequest;
using UnityEngine;

namespace Assets.Packages.AnchorLinkTransportSharp
{
    public abstract class UnityTransport : MonoBehaviour, ILinkTransport
    {
        private readonly bool _requestStatus;
        private readonly bool _fuelEnabled;
        private SigningRequest _activeRequest;
        private object _activeCancel; //?: (reason: string | Error) => void
        private Timer _countdownTimer;
        private Timer _closeTimer;
        public ILinkStorage Storage { get; }

        public UnityTransport(TransportOptions options)
        {
            this._requestStatus = options.RequestStatus != false;
            this._fuelEnabled = options.DisableGreymassFuel != true;
            this.Storage = new PlayerPrefsStorage(options.StoragePrefix);
        }

        // see https://github.com/greymass/anchor-link-browser-transport/blob/master/src/index.ts#L374
        // and https://github.com/greymass/anchor-link-console-transport/blob/master/src/index.ts#L10
        public void OnRequest(SigningRequest request, Action<object> cancel)
        {
            this._activeRequest = request;
            this._activeCancel = cancel;
            var uri = request.Encode(false, true);
            Console.WriteLine(uri);

            // possible that this doesn't work in Unity and
            // Application.OpenURL(uri); has to be called instead

            var ps = new ProcessStartInfo(uri)
            {
                UseShellExecute = true,
            };
            Process.Start(ps);

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

        public abstract void ShowLoading();

        public abstract void OnSuccess(SigningRequest request, TransactResult result);

        public abstract void OnFailure(SigningRequest request, Exception exception);

        public abstract void DisplayRequest(SigningRequest request);

        public abstract void ShowDialog(string title = null, string subtitle = null, string type = null, Action action = null, object content = null);
    }
}