namespace MiniGame.Network
{
    public enum ChannelType
    {
        Tcp,
        WebSocket,
    }
    
    public interface IChannel
    {
        int HeartbeatMissCount { get; }
        bool Connected { get; }
        ChannelType Type { get; }
        void Send(INetPackage pkg);
        INetPackage Pick();
    }
}