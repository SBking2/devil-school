
namespace EGame
{
    public interface INetHostHandler : INetHandler
    {
        public void OnClientDisconnected();
        public void OnClientConnected();
    }
}