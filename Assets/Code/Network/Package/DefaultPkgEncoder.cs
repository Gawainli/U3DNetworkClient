namespace MiniGame.Network
{
    public class DefaultPkgEncoder : INetPackageEncoder
    {
        public ByteRingBuffer Buffer { get; } = new(DefaultNetPackage.PkgMaxSize);

        public void Encode(ByteRingBuffer ringBuffer, INetPackage encodePkg)
        {
            if (encodePkg == null)
            {
                return;
            }

            var pkg = (DefaultNetPackage)encodePkg;
            if (pkg.BodyBytes.Length is 0 or >= DefaultNetPackage.BodyMaxSize)
            {
                return;
            }

            ringBuffer.WriteInt32(pkg.MsgId);
            ringBuffer.WriteInt32(pkg.MsgIndex);
            ringBuffer.WriteInt32(pkg.BodyBytes.Length);
            ringBuffer.Write(pkg.BodyBytes);
        }

        public void Encode(INetPackage encodePkg)
        {
            Encode(Buffer, encodePkg);
        }
    }
}