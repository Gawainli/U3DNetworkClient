namespace MiniGame.Network
{
    public interface INetPackageEncoder
    {
        ByteRingBuffer Buffer { get; }
        // void Encode(ByteRingBuffer buffer, INetPackage pkg);
        void Encode(INetPackage pkg);
    }
}