
namespace EGame
{
    public abstract class AbstractNetClient
    {
        protected bool _IsConnected = false;

        protected INetClientHandler _NetHandler;
        public AbstractNetClient(INetClientHandler handler)
        {
            _NetHandler = handler;
        }

        public virtual ulong ClientID { get; }
        public virtual ulong HostID { get; }
        public abstract void Update();
        public abstract void SendMessage();
        public abstract void DisConnectFromHost();
    }
}