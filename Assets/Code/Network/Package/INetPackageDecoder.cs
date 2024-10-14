using System.Collections.Generic;

namespace MiniGame.Network
{
    public interface INetPackageDecoder
    {
        void Decode(RingBuffer ringBuffer, List<INetPackage> outNetPackages);
    }
}