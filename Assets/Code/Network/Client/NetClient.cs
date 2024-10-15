using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace MiniGame.Network
{
    public enum ClientState
    {
        Disconnected,
        Connecting,
        Connected,
        BadConnection,
        Disconnecting,
    }
    
    public abstract class NetClient
    {
        private const int MaxReConnectCount = 5;
        //second
        private const float CheckHeartbeatInterval = 1f;
        //millisecond
        private const int ReConnectDelay = 1000;

        public ClientState State
        {
            get => _currentState;
            set
            {
                if (value != _currentState)
                {
                    _lastState = _currentState;
                    _currentState = value;
                    onStateChange?.Invoke(_lastState, _currentState);
                }
            }
        }
        
        private ClientState _currentState;
        private ClientState _lastState;

        public Action<IMessage> onMessage;
        public Action<ClientState, ClientState> onStateChange;

        protected INetChannel netChannel;
        protected string address;
        protected int port;
        protected readonly INetPackageEncoder pkgEncoder;
        protected readonly INetPackageDecoder pkgDecoder;
        
        private int _heartbeatMissCount;
        private int _reconnectCount;
        private float _checkHeartbeatWaitTime;

        private readonly IClientMessageEncoder _messageEncoder;
        private readonly IClientMessageDecoder _messageDecoder;

        public NetClient(IClientMessageEncoder msgEncoder, IClientMessageDecoder msgDecoder)
        {
            State = ClientState.Disconnected;
            pkgEncoder = new DefaultPkgEncoder();
            pkgDecoder = new DefaultPkgDecoder();

            _messageEncoder = msgEncoder;
            _messageDecoder = msgDecoder;
        }

        public abstract UniTask ConnectAsync(string address, int port);
        public abstract void Shutdown();

        public async UniTask ReConnectAsync()
        {
            State = ClientState.Connecting;
            if (netChannel == null)
            {
                return;
            }

            netChannel.Close();

            while (State == ClientState.Connecting)
            {
                try
                {
                    await ConnectAsync(address, port);
                }
                catch (Exception e)
                {
                    Log($"Reconnect failed: {e.Message}");
                    _reconnectCount++;
                    State = ClientState.Connecting;
                    if (_reconnectCount > MaxReConnectCount)
                    {
                        Log($"Reconnect reached maximum.");
                        State = ClientState.Disconnected;
                        break;
                    }

                    await UniTask.Delay(ReConnectDelay);
                }
            }
        }

        public void SendMessage(IMessage message)
        {
            if (netChannel == null)
            {
                return;
            }

            var pkg = new DefaultNetPackage
            {
                MsgId = message.MsgId,
                BodyBytes = _messageEncoder.Encode(message)
            };
            netChannel.WritePkg(pkg);
        }

        public void Tick(float unscaledDeltaTime)
        {
            _checkHeartbeatWaitTime += unscaledDeltaTime;
            if (_checkHeartbeatWaitTime > CheckHeartbeatInterval)
            {
                _checkHeartbeatWaitTime = 0;
                _heartbeatMissCount++;
            }

            CheckConnection();
            PickMessage();
        }

        public void Disconnect()
        {
            netChannel?.Close();
        }
        
        private void CheckConnection()
        {
            if (State != ClientState.Connected)
            {
                return;
            }

            State = _heartbeatMissCount > 5 ? ClientState.BadConnection : ClientState.Connected;
        }

        protected void SetConnected()
        {
            State = ClientState.Connected;
            _checkHeartbeatWaitTime = 0;
            _reconnectCount = 0;
            _heartbeatMissCount = 0;
        }

        private void PickMessage()
        {
            if (netChannel == null)
            {
                return;
            }

            while (netChannel.PickPkg(out var pkg))
            {
                if (pkg is DefaultNetPackage netPkg)
                {
                    if (netPkg.MsgId == 1)
                    {
                        _heartbeatMissCount = 0;
                    }
                    else
                    {
                        onMessage?.Invoke(_messageDecoder?.Decode(netPkg.BodyBytes));
                    }
                }
            }
        }

        protected void Log(string msg)
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
    }
}