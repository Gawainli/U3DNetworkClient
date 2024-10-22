using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace MiniGame.Network
{
    public class NetMessageWaiter
    {
        public int WaitMsgId { get; }
        private IMessage _resultMessage;

        public NetMessageWaiter(IMessage reqMsg)
        {
            WaitMsgId = reqMsg.RespId;
        }

        public void SetResultMsg(IMessage msg)
        {
            if (msg.MsgId != WaitMsgId)
            {
                Debug.Log($"Message ID does not match {msg.MsgId} != {WaitMsgId}");
                throw new ArgumentException("Message ID does not match");
            }

            _resultMessage = msg;
        }

        public async UniTask<IMessage> WaitAsync(float timeout)
        {
            await UniTask.WaitUntil(() => _resultMessage != null).Timeout(TimeSpan.FromSeconds(timeout));
            return _resultMessage;
        }
    }
}