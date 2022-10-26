using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using AnchorLinkSharp;
using EosioSigningRequest;

namespace AnchorLinkUnityTransportSharp
{
    public class UnityTransport : ILinkTransport
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

        public void OnRequest(SigningRequest request, Action<object> cancel)
        {
            this._activeRequest = request;
            this._activeCancel = cancel;
            var uri = request.Encode(false, true);
            Console.WriteLine(uri);

            var ps = new ProcessStartInfo(uri)
            {
                UseShellExecute = true,
            };
            Process.Start(ps);

            //Application.OpenURL(uri);
//            this.displayRequest(request).catch (cancel)
        }

        public void OnSuccess(SigningRequest request, TransactResult result)
        {
            if (request == this._activeRequest)
            {
                if (this._requestStatus)
                {
                    // TODO Timer, Visualization
                }
            }
        }

        public void OnFailure(SigningRequest request, Exception exception)
        {
            if (request == this._activeRequest && exception is LinkException linkException && linkException.Code != LinkErrorCode.ECancel)
            {
                if (this._requestStatus)
                { 
                    // TODO Timer, Visualization
                }
            }
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

        public void ShowLoading()
        {
            Console.WriteLine("loading ...");
        }


/*    private clearTimers()
{
    if (this.closeTimer)
    {
        clearTimeout(this.closeTimer)
            this.closeTimer = undefined
        }
    if (this.countdownTimer)
    {
        clearTimeout(this.countdownTimer)
            this.countdownTimer = undefined
        }*/
    }
}