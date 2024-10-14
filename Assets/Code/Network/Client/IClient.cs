namespace MiniGame.Network
{
    public enum ClientType
    {
        Tcp,
        WebSocket,
    }
    
    public enum ClientState
    {
        Disconnected,
        Connecting,
        Connected,
        BadConnection,
        Disconnecting,
    }
    
    public interface IClient
    {
        
    }
}