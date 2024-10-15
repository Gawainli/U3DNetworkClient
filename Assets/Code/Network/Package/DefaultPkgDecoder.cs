using System;

namespace MiniGame.Network
{
    public class DefaultPkgDecoder : INetPackageDecoder
    {
        public ByteRingBuffer Buffer { get; } = new ( DefaultNetPackage.PkgMaxSize);

        public INetPackage Decode(ByteRingBuffer ringBuffer)
        {
            if (ringBuffer.Count < DefaultNetPackage.HeaderSize)
            {
                return null;
            }

            var headBeforeRead = ringBuffer.Head;
            var msgId = ringBuffer.ReadInt32();
            var msgIndex = ringBuffer.ReadInt32();
            var msgBodyLength = ringBuffer.ReadInt32();

            if (ringBuffer.Count < msgBodyLength)
            {
                ringBuffer.Head = headBeforeRead;
                return null;
            }

            if (msgBodyLength > DefaultNetPackage.BodyMaxSize)
            {
                throw new ArgumentOutOfRangeException();
            }

            return new DefaultNetPackage
            {
                MsgId = msgId,
                MsgIndex = msgIndex,
                BodyBytes = ringBuffer.Read(msgBodyLength)
            };
        }

        public INetPackage Decode()
        {
            return Decode(Buffer);
        }
    }
}