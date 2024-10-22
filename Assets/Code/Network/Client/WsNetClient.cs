using System;
using Cysharp.Threading.Tasks;
using UnityWebSocket;

namespace MiniGame.Network
{
    public class WsNetClient : NetClient
    {
        private WebSocket _wsClient;

        public WsNetClient(IClientMessageEncoder msgEncoder, IClientMessageDecoder msgDecoder) : base(msgEncoder,
            msgDecoder)
        {
        }

        public override async UniTask ConnectAsync(string add, int p = -1)
        {
            address = add;
            State = ClientState.Connecting;
            try
            {
                _wsClient = new WebSocket(address);
                Log($"ConnectAsync: {address}");
                _wsClient.ConnectAsync();
                await UniTask.WaitUntil(() => _wsClient.ReadyState == WebSocketState.Open).Timeout(TimeSpan.FromSeconds(5));
                if (_wsClient.ReadyState != WebSocketState.Open)
                {
                    throw new Exception($"ConnectAsync failed: {address}");
                }
            }
            catch (Exception)
            {
                State = ClientState.Disconnected;
                throw;
            }

            Log($"Connected success to {address} readyState:{_wsClient.ReadyState}");
            netChannel = new WsChannel(_wsClient, pkgEncoder, pkgDecoder);
            netChannel.Open();
            SetConnected();
        }

        public override void Shutdown()
        {
            State = ClientState.Disconnecting;
            _wsClient?.CloseAsync();
            netChannel?.Close();
            State = ClientState.Disconnected;
        }
    }
}