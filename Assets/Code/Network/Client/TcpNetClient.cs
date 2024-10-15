using System;
using System.Net.Sockets;
using Cysharp.Threading.Tasks;

namespace MiniGame.Network
{
    public class TcpNetClient : NetClient
    {
        private TcpClient _tcpNetClient;
        public TcpNetClient(IClientMessageEncoder msgEncoder, IClientMessageDecoder msgDecoder) : base(msgEncoder, msgDecoder)
        {
        }
        
        public override async UniTask ConnectAsync(string add, int p)
        {
            address = add;
            port = p;
            State = ClientState.Connecting;
            try
            {
                _tcpNetClient = new TcpClient();
                Log($"Connecting to {address}:{port}");
                await _tcpNetClient.ConnectAsync(address, port);
            }
            catch (Exception)
            {
                State = ClientState.Disconnected;
                throw;
            }
        
            Log($"Connected success to {address}:{port}");
            netChannel = new TcpChannel(_tcpNetClient, pkgEncoder,
                pkgDecoder);
            netChannel.Open();
            SetConnected();
        }
        
        public override void Shutdown()
        {
            State = ClientState.Disconnecting;
            _tcpNetClient?.Close();
            netChannel?.Close();
            State = ClientState.Disconnected;
        }
    }
}