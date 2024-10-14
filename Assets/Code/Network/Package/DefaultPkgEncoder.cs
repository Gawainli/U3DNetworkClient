namespace MiniGame.Network
{
    public class DefaultPkgEncoder : INetPackageEncoder
    {
        public void Encode(RingBuffer ringBuffer, INetPackage? encodePkg)
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

            ringBuffer.WriteInt(pkg.MsgId);
            ringBuffer.WriteInt(pkg.MsgIndex);
            ringBuffer.WriteInt(pkg.BodyBytes.Length);
            ringBuffer.WriteBytes(pkg.BodyBytes, 0, pkg.BodyBytes.Length);
        }
    }
}