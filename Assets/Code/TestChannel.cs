using System;
using MiniGame.Network;
using UnityEngine;
using UnityWebSocket;

namespace Code
{
    public class TestChannel : MonoBehaviour
    {
        private IClientMessageEncoder _msgEncoder;
        private IClientMessageDecoder _msgDecoder;

        private WebSocket _webSocket;
        private WsChannel _wsChannel;

        private DefaultPkgEncoder _pkgEncoder;
        private DefaultPkgDecoder _pkgDecoder;

        private WsNetClient _wsNetClient;
        private void Awake()
        {
            _msgEncoder = new EchoMessageEncoder();
            _msgDecoder = new EchoMessageDecoder();
            _webSocket = new WebSocket("");
            Debug.Log($"{_msgEncoder}/{_msgDecoder}");
            Debug.Log($"_ws:{_webSocket}");

            _pkgEncoder = new DefaultPkgEncoder();
            _pkgDecoder = new DefaultPkgDecoder();
            Debug.Log($"pkg {_pkgEncoder}/{_pkgDecoder}");

            _wsChannel = new WsChannel(_webSocket, _pkgEncoder, _pkgDecoder);
            Debug.Log($"ws channel {_wsChannel}");

            _wsNetClient = new WsNetClient(_msgEncoder, _msgDecoder);
            Debug.Log($"ws client {_wsNetClient}");
        }

        private async void Start()
        {
            await _wsNetClient.ConnectAsync("ws://192.168.30.3:8993/ws/");
        }
    }
}