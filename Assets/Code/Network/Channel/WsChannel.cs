using System;
using System.Threading;
using UnityWebSocket;

#if !UNITY_EDITOR && UNITY_WEBGL
using Task = Cysharp.Threading.Tasks.UniTask;
#else
using Task = System.Threading.Tasks.Task;
#endif


namespace MiniGame.Network
{
    public class WsChannel : INetChannel, IDisposable
    {
        public const int HeartBeatInterval = 1;

        // private readonly ClientWebSocket _wsClient;
        private readonly WebSocket _wsClient;
        private readonly INetPackageEncoder _encoder;
        private readonly INetPackageDecoder _decoder;
        private readonly CancellationTokenSource _cts;

        private readonly DefaultNetPackage _heartbeatPkg = new DefaultNetPackage
        {
            MsgId = 1,
            MsgIndex = -1,
            BodyBytes = System.Text.Encoding.UTF8.GetBytes("ping")
        };

        public WsChannel(WebSocket wsClient, INetPackageEncoder encoder, INetPackageDecoder decoder)
        {
            _wsClient = wsClient;
            _encoder = encoder;
            _decoder = decoder;
            _cts = new CancellationTokenSource();
        }

        private async Task SendProcess()
        {
            Log("SendProcess start");
            while (Connected && !_cts.Token.IsCancellationRequested)
            {
                await Task.Delay(1);
                if (_encoder.Buffer.Count > 0)
                {
                    Log($"SendProcess _encodeBuffer.ReadableBytes = {_encoder.Buffer.Count}");
                    var headBeforeSend = _encoder.Buffer.Head;
                    var bytes = _encoder.Buffer.ReadAllAvailable();
                    try
                    {
                        _wsClient.SendAsync(bytes);
                        Log($"WriteAsync succeed. send bytes: {bytes.Length}");
                    }
                    catch (Exception e)
                    {
                        _encoder.Buffer.Head = headBeforeSend;
                        Log($"WriteAsync Exception. unable to send bytes: {_encoder.Buffer.Count}");
                        Log($"Exception: {e.Message}");
                    }
                }
            }

            Log($"SendProcess exit. Connected: {Connected} cts: {_cts.Token.IsCancellationRequested}");
        }

        private void OnWebMessage(object sender, MessageEventArgs messageEventArgs)
        {
            Log($"OnWebMessage: {messageEventArgs.RawData.Length}");
            if (messageEventArgs.IsBinary)
            {
                _decoder.Buffer.Write(messageEventArgs.RawData, 0, messageEventArgs.RawData.Length);
            }
            else
            {
                Log($"OnWebMessage: {messageEventArgs.Data}");
            }
        }

        private async Task HeartBeatProcess()
        {
            Log("HeartBeatProcess start");
            while (Connected && !_cts.Token.IsCancellationRequested)
            {
                await Task.Delay(HeartBeatInterval * 1000);
                WritePkg(_heartbeatPkg);
            }

            Log("HeartBeatProcess exit");
        }

        [System.Diagnostics.Conditional("NET_CHANNEL_LOG")]
        private void Log(string msg)
        {
            var time = DateTime.Now.ToString("HH:mm:ss.fff");
            var thread = Thread.CurrentThread.ManagedThreadId;
            var log = $"[{time}][Ws Channel Log][T-{thread:D3}] {msg}";
#if UNITY_EDITOR
            UnityEngine.Debug.Log(log);
#else
            Console.WriteLine(log);
#endif
        }

        #region implement INetChannel

        public void Dispose()
        {
            Close();
        }

        // public bool Connected => _wsClient.State == WebSocketState.Open;
        public bool Connected => _wsClient.ReadyState == WebSocketState.Open;

        public void Open()
        {
            if (_wsClient == null)
            {
                return;
            }

            _ = SendProcess();
            _ = HeartBeatProcess();
            _wsClient.OnMessage += OnWebMessage;
        }

        public void Close()
        {
            _cts.Cancel();
        }

        public void WritePkg(INetPackage pkg)
        {
            _encoder.Encode(pkg);
        }

        public bool PickPkg(out INetPackage pkg)
        {
            pkg = _decoder.Decode();
            return pkg != null;
        }

        #endregion
    }
}