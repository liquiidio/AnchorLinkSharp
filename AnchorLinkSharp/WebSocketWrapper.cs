using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NativeWebSocket;
using UnityEngine;
using WebSocket = NativeWebSocket.WebSocket;
using WebSocketState = NativeWebSocket.WebSocketState;

namespace AnchorLinkSharp
{
    public class WebSocketWrapper : MonoBehaviour
    {
        //public Queue<string> messageQueue = new Queue<string>();

        //private const int ReceiveChunkSize = 1024;
        //private const int SendChunkSize = 1024;

        private static WebSocket _webSocket;

        public WebSocketState State => _webSocket?.State ?? WebSocketState.Closed;
        //private readonly Uri _uri;
        //private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        //private readonly CancellationToken _cancellationToken;

        public event Action OnOpen;
        public event Action<string> OnMessage;
        public event Action<WebSocketCloseCode> OnClose;
        public event Action<string> OnError;

        private string _uri;
        private bool _newRequest;

        //public WebSocketWrapper()
        //{
        //    //_webSocket = new WebSocket(url.Replace("http", "ws"));
        //    ////_ws.Options.KeepAliveInterval = TimeSpan.FromSeconds(20);
        //    ////_uri = new Uri(uri);
        //    //_cancellationToken = _cancellationTokenSource.Token;
        //}

        /// <summary>
        /// Creates a new instance.
        /// </summary>
        /// <param name="uri">The URI of the WebSocket server.</param>
        /// <returns></returns>
        public async Task Create(string uri)
        {
            OnOpen = null;
            OnMessage = null;
            OnClose = null;
            OnError = null;
            if (_webSocket != null && _webSocket.State != WebSocketState.Closed)
                await _webSocket.Close();

            _newRequest = true;
            _uri = uri;
        }

        void Update()
        {
            if (_newRequest)
            {
                _webSocket = new WebSocket(_uri);
                _newRequest = false;
            }

            if (_webSocket != null && _webSocket.State == WebSocketState.Open)
                _webSocket?.DispatchMessageQueue();
        }

        /// <summary>
        /// Connects to the WebSocket server.
        /// </summary>
        /// <returns></returns>
        //public WebSocketWrapper Connect()
        //{
        //    ConnectAsync();
        //    InvokeRepeating("SendWebSocketMessage", 0.0f, 0.5f);
        //    return this;
        //}

        public async Task ConnectAsync()
        {
            // wait max 10 seconds
            var i = 0;
            while (_webSocket == null && i < 100)
            {
                await Task.Delay(100);
                i++;
            }

            if (_webSocket != null)
            {
                _webSocket.OnClose += WebSocketOnOnClose;
                _webSocket.OnMessage += WebSocketOnMessageReceived;
                _webSocket.OnOpen += WebSocketOnOpen;
                _webSocket.OnError += WebSocketOnOnError;
                _webSocket.Connect(); // Do not await!
            }
        }

        private void WebSocketOnOnError(string errormsg)
        {
            OnError?.Invoke(errormsg);
        }

        private void WebSocketOnOpen()
        {
            OnOpen?.Invoke();
        }

        private void WebSocketOnOnClose(WebSocketCloseCode closeCode)
        {
            OnClose?.Invoke(closeCode);
        }

        private void WebSocketOnMessageReceived(byte[] data)
        {
            try
            {
                var message = Encoding.UTF8.GetString(data ?? throw new ApplicationException("data = null"));
                OnMessage?.Invoke(message);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        public async Task CloseAsync()
        {
            if (_webSocket != null && _webSocket.State != WebSocketState.Closing && _webSocket.State != WebSocketState.Closed)
                await _webSocket.Close();
        }

        private async void OnDisable()
        {
            _newRequest = false;
            await CloseAsync();
        }

        async void OnApplicationQuit()
        {
            _newRequest = false;
            await CloseAsync();
        }

        public void Clear()
        {
            _webSocket = null;
        }
    }
}