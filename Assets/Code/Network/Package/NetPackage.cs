namespace MiniGame.Network
{
    public interface INetPackage
    {
    }

    public class DefaultNetPackage : INetPackage
    {
        public const int MsgIdSize = 4;
        public const int MsgIndexSize = 4;
        public const int MsgLengthSize = 4;
        
        public const int HeaderSize = MsgIdSize + MsgIndexSize + MsgLengthSize;
        public const int BodyMaxSize = 1024 * 1024;
        public const int PkgMaxSize = HeaderSize + BodyMaxSize;

        public int MsgId { set; get; }
        public int MsgIndex { set; get; }
        public byte[] BodyBytes { set; get; }
    }
}