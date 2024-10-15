using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace MiniGame.Network
{
    public class TcpChannel : INetChannel, IDisposable
    {
        public const int HeartBeatInterval = 1;

        private readonly TcpClient _tcpClient;

        private readonly INetPackageEncoder _encoder;
        private readonly INetPackageDecoder _decoder;
        private readonly CancellationTokenSource _cts;
        private NetworkStream _stream;

        private readonly DefaultNetPackage _heartbeatPkg = new DefaultNetPackage
        {
            MsgId = 1,
            MsgIndex = -1,
            BodyBytes = System.Text.Encoding.UTF8.GetBytes("0")
        };

        public TcpChannel(TcpClient tcpClient, INetPackageEncoder encoder, INetPackageDecoder decoder)
        {
            _tcpClient = tcpClient;
            _encoder = encoder;
            _decoder = decoder;
            _cts = new CancellationTokenSource();
        }

        private async Task SendProcess()
        {
            Log("SendProcess start");
            while (_tcpClient.Connected && !_cts.Token.IsCancellationRequested)
            {
                await Task.Delay(1);
                if (_encoder.Buffer.Count > 0)
                {
                    Log($"SendProcess _encodeBuffer.ReadableBytes = {_encoder.Buffer.Count}");
                    var headBeforeSend = _encoder.Buffer.Head;
                    var bytes = _encoder.Buffer.ReadAllAvailable();
                    try
                    {
                        await _stream.WriteAsync(bytes, 0, bytes.Length);
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

            Log("SendProcess exit");
        }

        private async Task ReceiveProcess()
        {
            Log("ReceiveProcess start");
            try
            {
                var buffer = new byte[DefaultNetPackage.PkgMaxSize * 4];
                while (_tcpClient.Connected && !_cts.Token.IsCancellationRequested)
                {
                    await Task.Delay(1);
                    var receiveCount = await _stream.ReadAsync(buffer, 0, buffer.Length, _cts.Token);
                    if (receiveCount == 0)
                    {
                        break;
                    }

                    _decoder.Buffer.Write(buffer, 0, receiveCount);
                }
            }
            catch (Exception e)
            {
                Log($"ReceiveProcess Exception: {e.Message} ReceiveProcess exit");
                throw;
            }

            Log("ReceiveProcess exit");
        }

        private async Task HeartBeatProcess()
        {
            Log("HeartBeatProcess start");
            while (_tcpClient.Connected && !_cts.Token.IsCancellationRequested)
            {
                await Task.Delay(HeartBeatInterval * 1000);
                WritePkg(_heartbeatPkg);
            }
            
            Log("HeartBeatProcess exit");
        }

        [System.Diagnostics.Conditional("NET_CHANNEL_LOG")]
        private void Log(string msg)
        {
#if UNITY_EDITOR
            UnityEngine.Debug.Log(msg);
#else
            Console.WriteLine(msg);
#endif
        }

        #region implement INetChannel

        public void Dispose()
        {
            Close();
            _stream.Dispose();
            _tcpClient.Dispose();
        }

        public bool Connected => _tcpClient.Connected;

        public void Open()
        {
            if (_tcpClient == null)
            {
                return;
            }

            Log("Open Channel Process");
            _stream = _tcpClient.GetStream();
            _ = SendProcess();
            _ = ReceiveProcess();
            _ = HeartBeatProcess();
        }

        public void Close()
        {
            _cts.Cancel();
            _stream?.Close();
            _tcpClient?.Close();
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