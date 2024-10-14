namespace MiniGame.Network
{
    public interface INetPackageEncoder
    {
        void Encode(RingBuffer ringBuffer, INetPackage encodePkg);
    }
}