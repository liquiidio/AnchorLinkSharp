using System;
using System.Threading;
using System.Threading.Tasks;
using AnchorLinkSharp;
using EosioSigningRequestSharp;
using EosSharp.Core.Helpers;
using UnityEngine;

namespace AnchorLinkUnityTransportSharp
{
    public class TransportOptions
    {
        /** Whether to display request success and error messages, defaults to true */
        public bool requestStatus;
        /** Local storage prefix, defaults to `anchor-link`. */
        public string storagePrefix;

        /**
         * Whether to use Greymass Fuel for low resource accounts, defaults to false.
         * Note that this service is not available on all networks.
         * Visit https://greymass.com/en/fuel for more information.
         */
        public bool disableGreymassFuel;
    }

    public class Storage : ILinkStorage
    {
        private string keyPrefix;
        public Storage(string keyPrefix)
        {
            this.keyPrefix = keyPrefix;
        }

        string storageKey(string key)
        {
            return $"{this.keyPrefix}{key}";
        }

        public void write(string key, string data)
        {
            PlayerPrefs.SetString(this.storageKey(key), data.ToString());
        }

        public string read(string key)
        {
            return PlayerPrefs.GetString(this.storageKey(key));
        }

        public void remove(string key)
        {
            PlayerPrefs.DeleteKey(this.storageKey(key));
        }
    }

    class AnchorLinkUnityTransportSharp
    {
        private bool requestStatus;
        private bool fuelEnabled;
        private SigningRequest activeRequest;
        private object activeCancel; //?: (reason: string | Error) => void
        private Timer countdownTimer;
        private Timer closeTimer;
        public ILinkStorage storage;

        public AnchorLinkUnityTransportSharp(TransportOptions options)
        {
            this.requestStatus = options.requestStatus != false;
            this.fuelEnabled = options.disableGreymassFuel != true;
            this.storage = new Storage(options.storagePrefix ?? "anchor-link");
        }

        public void onRequest(SigningRequest request, Action<object> cancel)
        {
            this.activeRequest = request;
            this.activeCancel = cancel;
//            this.displayRequest(request).catch (cancel)
        }

        public void onSuccess(SigningRequest request, TransactResult result)
        {
            if (request == this.activeRequest)
            {
//                this.clearTimers()
                if (this.requestStatus)
                {
//                    this.setupElements()
/*                    const infoEl  = this.createEl({class: 'info'
                    })
                    const logoEl  = this.createEl({class: 'logo'
                    })
                    logoEl.classList.add('success')
                    const infoTitle  = this.createEl({class: 'title', tag:
                        'span', text:
                        'Success!'
                    })
                    const subtitle  = request.isIdentity() ? 'Identity signed.' : 'Transaction signed.'
                    const infoSubtitle  = this.createEl({class: 'subtitle', tag:
                        'span', text:
                        subtitle
                    })
                    infoEl.appendChild(infoTitle)
                    infoEl.appendChild(infoSubtitle)
                    emptyElement(this.requestEl)
                    this.requestEl.appendChild(logoEl)
                    this.requestEl.appendChild(infoEl)
                    this.show()
                    this.closeTimer = setTimeout(() =>
                    {
                        this.hide()
                    }, 1.5 * 1000)
                }
                else
                {
                    this.hide()
                }*/
                }
            }
        }

        public void onFailure(SigningRequest request, Exception exception)
        {
            if (request == this.activeRequest && exception is LinkException linkException && linkException.code != LinkErrorCode.E_CANCEL)
            {
//              this.clearTimers()
                if (this.requestStatus)
                {
                }
            }
        }

        public void onSessionRequest(LinkSession session, SigningRequest request, object cancel)
        {
            if (session.type == "fallback")
            {
                this.onRequest(request, null);  // TODO CancellationToken?
                return;
            }

            this.activeRequest = request;
            this.activeCancel = cancel;
        }

        public async Task<SigningRequest> prepare(SigningRequest request, LinkSession session = null)
        {
            //    this.showLoading()
            if (!this.fuelEnabled || session == null || request.isIdentity())
            {
                // don't attempt to cosign id request or if we don't have a session attached
                return request;
            }
            try
            {
                var result = FuelSharp.fuel(request, session /*, this.updatePrepareStatus.bind(this)*/);
                if (await Task.WhenAny(result, Task.Delay(3500)) != result)
                {
                    throw new Exception("Fuel API timeout after 3500ms");
                }

                return result.Result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Not applying fuel( {ex.Message})"); // TODO Debug Log
            }
            return request;
        }

        public void showLoading()
        {
            throw new NotImplementedException();
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