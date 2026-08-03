
namespace EGame
{
    public abstract class AbstractNetClient
    {
        protected INetClientHandler _NetHandler;
        public AbstractNetClient(INetClientHandler handler)
        {
            _NetHandler = handler;
        }

        protected ulong _NetID;
        protected ulong _HostID;
        public abstract void SendMessage();
        public abstract void ConnectToHost();
        public abstract void DisConnectFromHost();
    }
}