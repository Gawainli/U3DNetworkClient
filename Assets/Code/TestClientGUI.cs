using System;
// using System.Net;
using Cysharp.Threading.Tasks;
using IngameDebugConsole;
using MiniGame.Network;
using UnityEngine;

namespace Code
{
    public class TestClientGUI : MonoBehaviour
    {
        public string version = "1.0.0";
        public string address;
        public ClientState state;
        public string sendText = "Hello Echo Server!";

        private string _localAddress;
        private int _sendCount;
        private NetClient _client;
        private WsNetClient _wsClient;
        private TcpNetClient _tcpClient;
        private readonly EchoMessage _echoMessage = new EchoMessage();
        private bool _logMessage = true;
        private string _log = "";
        private int _receiveCount;
        private Vector2 _scrollPos;
        private bool Connected => state == ClientState.Connected || state == ClientState.BadConnection;

        private void Awake()
        {
            _localAddress = GetLocalIP();
        }

        //获取本地IP地址
        private string GetLocalIP()
        {
            string localIP = "";
#if !UNITY_EDITOR && UNITY_WEBGL
            localIP = "webgl client";
#else
            try
            {
                // 获取所有网络接口的IP地址
                var host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
                foreach (var ip in host.AddressList)
                {
                    // 过滤出IPv4地址，并且排除回环地址（127.0.0.1）
                    if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork && !System.Net.IPAddress.IsLoopback(ip))
                    {
                        localIP = ip.ToString();
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.Log("SocketException: " + ex.Message);
            }
            
            // 如果没有找到有效的IP地址，返回"未找到"
            if (string.IsNullOrEmpty(localIP))
            {
                localIP = "Local IP Address Not Found!";
            }
#endif
            return localIP;
        }

        private void ShutdownClient()
        {
            if (_client == null) return;
            _client.onStateChange -= OnClientOnStateChange;
            _client.onMessage -= OnClientOnMessage;
            _client.Shutdown();
            _client = null;
        }

        private void OnClientOnMessage(IMessage message)
        {
            if (message is not EchoMessage echoMessage) return;
            _receiveCount++;
            if (_logMessage)
            {
                AddLog($"Receive Server {_receiveCount} Msg:{echoMessage.text}");
            }
        }

        private void OnClientOnStateChange(ClientState lastState, ClientState currentState)
        {
            Debug.Log($"last:{lastState}, current:{currentState}");
            if (currentState == ClientState.BadConnection && lastState == ClientState.Connected)
            {
                _client.ReConnectAsync().Forget();
            }
        }

        private void OnGUI()
        {
            if (DebugLogManager.Instance.IsLogWindowVisible)
            {
                return;
            }

            var scale = Screen.width / 800f;
            GUI.matrix = Matrix4x4.TRS(new Vector3(0, 0, 0), Quaternion.identity, new Vector3(scale, scale, 1));
            var width = GUILayout.Width(Screen.width / scale - 10);

            // draw header
            GUILayout.BeginHorizontal();
            GUILayout.Label($"Local Address: {_localAddress}.    NetCode version: {version}",
                GUILayout.Width(Screen.width / scale - 100));
            GUI.color = Color.green;
            GUILayout.Label($"FPS: {_fps:F2}", GUILayout.Width(80));
            GUI.color = Color.white;
            GUILayout.EndHorizontal();

            // draw websocket state
            GUILayout.BeginHorizontal();
            GUILayout.Label("State: ", GUILayout.Width(36));
            GUI.color = !Connected ? Color.red : Connected ? Color.green : Color.gray;
            GUILayout.Label($"{state}", GUILayout.Width(120));
            GUI.color = Color.white;
            GUILayout.EndHorizontal();

            // draw address
            GUI.enabled = !Connected;
            GUILayout.Label("Address: ", width);
            address = GUILayout.TextField(address, width);

            // draw connect button
            GUILayout.BeginHorizontal();
            GUI.enabled = state == ClientState.Disconnected;
            if (GUILayout.Button(state == ClientState.Connecting ? "Connecting..." : "Connect"))
            {
                // _client.ConnectAsync(address).Forget();
                if (address.StartsWith("ws:"))
                {
                    _client = new WsNetClient(new EchoMessageEncoder(), new EchoMessageDecoder());
                    _client.onStateChange += OnClientOnStateChange;
                    _client.onMessage += OnClientOnMessage;
                    _client.ConnectAsync(address, -1);
                }
                else
                {
                    _client = new TcpNetClient(new EchoMessageEncoder(), new EchoMessageDecoder());
                    _client.onStateChange += OnClientOnStateChange;
                    _client.onMessage += OnClientOnMessage;
                    var portLastIndexOf = address.LastIndexOf(":", StringComparison.Ordinal);
                    var tcpAddress = address[..portLastIndexOf];
                    var port = int.Parse(address[(portLastIndexOf + 1)..]);
                    _client.ConnectAsync(tcpAddress, port);
                }
            }

            // draw close button
            GUI.enabled = Connected;
            if (GUILayout.Button(state == ClientState.Disconnecting ? "Closing..." : "Close"))
            {
                ShutdownClient();
            }

            GUILayout.EndHorizontal();

            // draw echo message
            GUI.enabled = true;
            GUILayout.Label("Message: ", width);
            sendText = GUILayout.TextField(sendText, width);

            // draw send message button
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Send") && !string.IsNullOrEmpty(sendText))
            {
                _echoMessage.text = sendText;
                _client.SendMessage(_echoMessage);
                _sendCount += 1;
            }

            if (GUILayout.Button("Send x100") && !string.IsNullOrEmpty(sendText))
            {
                for (int i = 0; i < 100; i++)
                {
                    _echoMessage.text = sendText;
                    _client.SendMessage(_echoMessage);
                    _sendCount += 1;
                }
            }

            if (GUILayout.Button("Request") && !string.IsNullOrEmpty(sendText))
            {
                _echoMessage.text = sendText;
                TestReq();
                _sendCount += 1;
            }

            if (GUILayout.Button("Request x100") && !string.IsNullOrEmpty(sendText))
            {
                for (int i = 0; i < 100; i++)
                {
                    _echoMessage.text = sendText;
                    TestReq();
                    _sendCount += 1;
                }
            }

            GUILayout.EndHorizontal();

            // draw message count
            GUI.enabled = true;
            GUILayout.BeginHorizontal();
            _logMessage = GUILayout.Toggle(_logMessage, "Log Message");
            GUILayout.Label($"Send Count: {_sendCount}");
            GUILayout.Label($"Receive Count: {_receiveCount}");
            GUILayout.EndHorizontal();

            // draw clear button
            if (GUILayout.Button("Clear"))
            {
                _log = "";
                _receiveCount = 0;
                _sendCount = 0;
            }

            // draw message content
            _scrollPos = GUILayout.BeginScrollView(_scrollPos, GUILayout.MaxHeight(Screen.height / scale - 270), width);
            GUILayout.Label(_log);
            GUILayout.EndScrollView();
        }

        private void AddLog(string str)
        {
            if (!_logMessage) return;
            if (str.Length > 100) str = str.Substring(0, 100) + "...";
            _log += str + "\n";
            if (_log.Length > 22 * 1024)
            {
                _log = _log.Substring(_log.Length - 22 * 1024);
            }

            _scrollPos.y = int.MaxValue;
        }

        private async void TestReq()
        {
            var respMsg = await _client.ReqMessage(_echoMessage);
            if (respMsg is not EchoMessage echoMessage) return;
            _receiveCount++;
            AddLog($"Receive Request Resp {_receiveCount} Msg: {echoMessage.text}");
        }

        private int _frame = 0;
        private float _time = 0;
        private float _fps = 0;

        private void Update()
        {
            _frame += 1;
            _time += Time.deltaTime;
            if (_time >= 0.5f)
            {
                _fps = _frame / _time;
                _frame = 0;
                _time = 0;
            }

            _client?.Tick(Time.unscaledDeltaTime);
            state = _client?.State ?? ClientState.Disconnected;
        }

        private void OnDestroy()
        {
            ShutdownClient();
        }
    }
}