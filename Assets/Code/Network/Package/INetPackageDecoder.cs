using System.Collections.Generic;

namespace MiniGame.Network
{
    public interface INetPackageDecoder
    {
        ByteRingBuffer Buffer { get; }
        // INetPackage Decode(ByteRingBuffer ringBuffer);
        INetPackage Decode();
    }
}