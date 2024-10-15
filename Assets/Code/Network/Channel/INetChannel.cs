using System.Threading.Tasks;

namespace MiniGame.Network
{
    public enum ChannelType
    {
        Tcp,
        WebSocket,
    }

    public interface INetChannel
    {
        bool Connected { get; }
        void Open();
        void Close();
        void WritePkg(INetPackage pkg);
        bool PickPkg(out INetPackage pkg);
    }
}