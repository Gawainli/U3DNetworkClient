using System;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Threading;

namespace MiniGame.Network
{
    public class WsChannel : INetChannel, IDisposable
    {
        private ClientWebSocket _socket;
        private CancellationTokenSource _cts;
        private bool _sending;
        private bool _receiving;
        private INetPackageDecoder _decoder;
        private INetPackageEncoder _encoder;

        private ConcurrentQueue<DefaultNetPackage> _sendQueue = new ConcurrentQueue<DefaultNetPackage>();
        private ConcurrentQueue<DefaultNetPackage> _receiveQueue = new ConcurrentQueue<DefaultNetPackage>();

        private RingBuffer _encodeBuffer = new RingBuffer(DefaultNetPackage.PkgMaxSize * 4);
        private RingBuffer _decodeBuffer = new RingBuffer(DefaultNetPackage.PkgMaxSize * 4);

        public WsChannel(INetPackageEncoder encoder, INetPackageDecoder decoder, ClientWebSocket socket)
        {
            _encoder = encoder;
            _decoder = decoder;
            _socket = socket;
        }

        public void Dispose()
        {
        }

        public bool Connected => _socket.State == WebSocketState.Open;

        public void Update(float unscaledDeltaTime)
        {
            if (_socket.State == WebSocketState.Open)
            {
                ProcessSend();
                ProcessReceive();
            }
        }

        public void SendPkg(INetPackage pkg)
        {
            // _sendQueue.Enqueue(pkg);
        }

        public INetPackage PickPkg()
        {
            throw new NotImplementedException();
        }

        private async void ProcessSend()
        {
            if (_sending || _sendQueue.Count == 0)
            {
                return;
            }

            _sending = true;
            while (_sendQueue.TryDequeue(out var pkg))
            {
                if (_encodeBuffer.WriteableBytes < DefaultNetPackage.PkgMaxSize)
                {
                    break;
                }

                _encoder.Encode(_encodeBuffer, pkg);
                await _socket.SendAsync(_encodeBuffer.ReadBytes(_encodeBuffer.ReadableBytes),
                    WebSocketMessageType.Binary, true, _cts.Token);
            }

            _sending = false;
        }

        private async void ProcessReceive()
        {
            if (_receiving || _socket.State != WebSocketState.Open || _cts.IsCancellationRequested)
            {
                return;
            }
            
            _receiving = true;
            var segment = new ArraySegment<byte>(new byte[DefaultNetPackage.PkgMaxSize]);
            while (!_cts.IsCancellationRequested)
            {
                var result = await _socket.ReceiveAsync(segment, _cts.Token);
                _decodeBuffer.WriteBytes(segment.Array, 0, result.Count);
            }
        }
    }
}