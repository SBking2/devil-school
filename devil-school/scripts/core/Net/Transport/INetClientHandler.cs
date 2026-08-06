
namespace EGame
{
    public interface INetClientHandler : INetHandler
    {
        public void OnConnected();
        public void OnDisconnected();
    }
}