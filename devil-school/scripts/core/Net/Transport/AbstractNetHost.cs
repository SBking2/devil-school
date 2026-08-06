
using System.Collections.Generic;

namespace EGame
{
    public abstract class AbstractNetHost
    {
        protected INetHostHandler _NetHandler;
        public AbstractNetHost(INetHostHandler handler)
        {
            _NetHandler = handler;
        }

        public virtual ulong HostID { get; }

        protected List<ulong> _ClientIDs = new List<ulong>();

        public abstract void Update();
        public abstract void SendMessage();
        public abstract void SendMessageAll();
        public abstract void DisConnectClient();
    }
}