
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

        public virtual IEnumerable<ulong> ClientIDs => new List<ulong>();

        public abstract void Update();
        public abstract void SendMessage(ulong client_id, byte[] data);
        public abstract void SendMessageAll(byte[] data);
        public abstract void DisConnectClient(ulong client_id, bool immediately);
    }
}