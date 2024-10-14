using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace MiniGame.Network
{
    public class TcpChannel : IChannel, IDisposable
    {
        public const int HeartBeatInterval = 2;

        private readonly TcpClient _tcpClient;
        private readonly NetworkStream _stream;
        private readonly CancellationTokenSource _cts;
        private readonly ConcurrentQueue<DefaultNetPackage> _sendQueue = new ConcurrentQueue<DefaultNetPackage>();
        private readonly ConcurrentQueue<DefaultNetPackage> _receiveQueue = new ConcurrentQueue<DefaultNetPackage>();

        private readonly RingBuffer _encodeBuffer = new RingBuffer(DefaultNetPackage.PkgMaxSize * 4);
        private readonly RingBuffer _decodeBuffer = new RingBuffer(DefaultNetPackage.PkgMaxSize * 4);
        private readonly ConcurrentQueue<byte[]> _unsentQueue = new ConcurrentQueue<byte[]>();

        private readonly INetPackageEncoder _encoder;
        private readonly INetPackageDecoder _decoder;
        private int _packageIndex = 1;

        private readonly DefaultNetPackage _heartbeatPkg = new DefaultNetPackage
        {
            MsgId = 1,
            MsgIndex = -1,
            BodyBytes = System.Text.Encoding.UTF8.GetBytes("ping")
        };

        private int _heartBeatMissCount = 0;

        public TcpChannel(TcpClient tcpClient, INetPackageEncoder encoder,
            INetPackageDecoder decoder)
        {
            _tcpClient = tcpClient;
            _stream = _tcpClient.GetStream();
            _encoder = encoder;
            _decoder = decoder;
            _cts = new CancellationTokenSource();
        }

        public void StartProcess()
        {
            _ = SendProcess();
            _ = ReceiveProcess();
            _ = SendHeartBeatProcess();
            _ = CheckHeartBeatProcess();
        }
        
        private async Task SendHeartBeatProcess()
        {
            while (_tcpClient.Connected && !_cts.Token.IsCancellationRequested)
            {
                await Task.Delay(HeartBeatInterval * 1000);
                Send(_heartbeatPkg);
            }
        }
        
        private async Task CheckHeartBeatProcess()
        {
            while (_tcpClient.Connected && !_cts.Token.IsCancellationRequested)
            {
                await Task.Delay(HeartBeatInterval * 1000 * 2);
                _heartBeatMissCount++;
            }
        }

        private async Task SendProcess()
        {
            while (_tcpClient.Connected && !_cts.Token.IsCancellationRequested)
            {
                await Task.Delay(1);
                while (_sendQueue.Count > 0)
                {
                    if (_sendQueue.TryDequeue(out var pkg))
                    {
                        if (_encodeBuffer.WriteableBytes < DefaultNetPackage.PkgMaxSize)
                        {
                            break;
                        }

                        Log($"SendProcess pkg.MsgId = {pkg.MsgId}");
                        if (pkg.MsgId == 1)
                        {
                            _encoder.Encode(_encodeBuffer, pkg);
                        }
                        else
                        {
                            pkg.MsgIndex = _packageIndex++;
                            _encoder.Encode(_encodeBuffer, pkg);
                        }
                    }
                }

                if (_encodeBuffer.ReadableBytes > 0)
                {
                    Log($"SendProcess _encodeBuffer.ReadableBytes = {_encodeBuffer.ReadableBytes}");
                    var bytes = _encodeBuffer.ReadBytes(_encodeBuffer.ReadableBytes);
                    try
                    {
                        await _stream.WriteAsync(bytes, 0, bytes.Length);
                        Log($"WriteAsync succeed. send bytes: {bytes.Length}");
                    }
                    catch (Exception e)
                    {
                        Log($"WriteAsync Exception: {e.Message}");
                        _unsentQueue.Enqueue(bytes);
                    }
                }
            }
        }

        private async Task ReceiveProcess()
        {
            try
            {
                var buffer = new byte[DefaultNetPackage.PkgMaxSize * 4];
                var tempPackages = new List<INetPackage>();
                while (_tcpClient.Connected && !_cts.Token.IsCancellationRequested)
                {
                    await Task.Delay(1);
                    if (!_stream.DataAvailable) continue;
                    var recvBytesCount = await _stream.ReadAsync(buffer, 0, buffer.Length, _cts.Token);
                    if (recvBytesCount == 0 || !_decodeBuffer.IsWriteable(recvBytesCount))
                    {
                        Log("ReceiveProcess: recvBytesCount == 0");
                        continue;
                    }

                    _decodeBuffer.WriteBytes(buffer, 0, recvBytesCount);
                    _decoder.Decode(_decodeBuffer, tempPackages);
                    foreach (var pkg in tempPackages)
                    {
                        var netPkg = (DefaultNetPackage)pkg;
                        Log(
                            $"Receive pkg. msgId: {netPkg.MsgId} time: {DateTime.Now:HH:mm:ss.fff}");
                        if (netPkg.MsgId == 1)
                        {
                            _heartBeatMissCount = 0;
                            continue;
                        }

                        _receiveQueue.Enqueue(netPkg);
                    }

                    tempPackages.Clear();
                }
            }
            catch (Exception e)
            {
                Log($"ReceiveProcess Exception: {e.Message}");
            }
        }

        public void Dispose()
        {
            _cts.Cancel();
            _stream?.Close();
            _tcpClient?.Close();

            _encodeBuffer.Clear();
            _decodeBuffer.Clear();

            _sendQueue.Clear();
            _receiveQueue.Clear();
            _unsentQueue.Clear();
        }

        private void Log(string msg)
        {
#if UNITY_EDITOR
            Debug.Log(msg);
#else
            Console.WriteLine(msg);
#endif
        }

        #region IChannel implementation

        public int HeartbeatMissCount => _heartBeatMissCount;
        public bool Connected => _tcpClient is { Connected: true };
        public ChannelType Type => ChannelType.Tcp;

        public void Send(INetPackage pkg)
        {
            _sendQueue.Enqueue((DefaultNetPackage)pkg);
        }

        public INetPackage Pick()
        {
            return _receiveQueue.TryDequeue(out var pkg) ? pkg : null;
        }

        #endregion
    }
}