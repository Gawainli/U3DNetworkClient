using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace MiniGame.Network
{
    public class WebClient
    {
        private readonly IClientMessageEncoder _messageEncoder;
        private readonly IClientMessageDecoder _messageDecoder;
        private readonly string _address;

        public WebClient(IClientMessageEncoder msgEncoder, IClientMessageDecoder msgDecoder, string address)
        {
            _messageEncoder = msgEncoder;
            _messageDecoder = msgDecoder;
            _address = address;
            if (!_address.EndsWith("/"))
            {
                _address += "/";
            }
        }

        public async UniTask<IMessage> GetRequestAsync(IMessage message, string url)
        {
            using var request = new UnityWebRequest(_address + url, UnityWebRequest.kHttpVerbGET);
            return await RunRequest(request, message);
        }

        public async UniTask<IMessage> PostRequestAsync(IMessage message, string url)
        {
            using var request = new UnityWebRequest(_address + url, UnityWebRequest.kHttpVerbPOST);
            return await RunRequest(request, message);
        }
        
        public async UniTask<IMessage> PutRequestAsync(IMessage message, string url)
        {
            using var request = new UnityWebRequest(_address + url, UnityWebRequest.kHttpVerbPUT);
            return await RunRequest(request, message);
        }
        
        private async UniTask<IMessage> RunRequest(UnityWebRequest req, IMessage msg)
        {
            if (msg != null)
            {
                req.uploadHandler = new UploadHandlerRaw(_messageEncoder.Encode(msg));
            }
            
            req.downloadHandler = new DownloadHandlerBuffer();
            await req.SendWebRequest();
            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError(req.result);
                return null;
            }

            var resBytes = req.downloadHandler.data;
            return _messageDecoder.Decode(resBytes);
        }
    }
}