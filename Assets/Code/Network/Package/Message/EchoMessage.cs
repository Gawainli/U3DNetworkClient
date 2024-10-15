using System.Text;

namespace MiniGame.Network
{
    public class EchoMessage : IMessage
    {
        public int MsgId => 2;
        public string text;
    }

    public class EchoMessageEncoder : IClientMessageEncoder
    {
        public byte[] Encode(IMessage message)
        {
            if (message is EchoMessage strMessage)
            {
                return Encoding.UTF8.GetBytes(strMessage.text);
            }

            return new byte[] { };
        }
    }

    public class EchoMessageDecoder : IClientMessageDecoder
    {
        public IMessage Decode(byte[] bytes)
        {
            var str = Encoding.UTF8.GetString(bytes);
            return new EchoMessage() { text = str };
        }
    }
}