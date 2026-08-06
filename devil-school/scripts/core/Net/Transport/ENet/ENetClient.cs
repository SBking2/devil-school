
using System.Threading.Tasks;
using Godot;

namespace EGame
{
    public class ENetClient : AbstractNetClient
    {        
        public override ulong HostID => 1uL;

        private ulong _NetID;
        public override ulong ClientID => _NetID;

        private Log.Logger _Logger = new Log.Logger(Log.LogType.NetWork);

        private ENetConnection _Connection;
        private ENetPacketPeer _Peer;
        public ENetClient(INetClientHandler handler) : base(handler)
        {

        }

        public async Task ConnectToHost()
        {
            _Connection = new ENetConnection();
            _Peer = _Connection.ConnectToHost("0.0.0.0", 8080);     //向Host发送请求，此时Host会响应Connect事件

            //向Host发送握手请求包,并等待Host的Ack
            RequestHandShake();
            await WaitHandShakeAck();
        }

        private void RequestHandShake()
        {
            ENetHandShake handshake = new ENetHandShake();
            handshake.ClientID = this.ClientID;
            var data = ENetPacket.FromHandShake(handshake);
            _Peer.Send(0, data.Data, (int)NetTransferMode.Reliable);
        }

        private async Task WaitHandShakeAck()
        {
            ENetServiceData? data = null;
            int total_dely = 0;
            while(data.HasValue == false)
            {
                await Task.Delay(100);
                total_dely += 100;

                _Connection.TryGetServiceData(out data);

                if (total_dely > 10000)
                {
                    _Logger.Error("ENet Client wait host ack timeout!", false);
                    //TODO:关闭客户端连接
                    return;
                }
            }

            _IsConnected = true;
        }

        public override void DisConnectFromHost()
        {
            throw new System.NotImplementedException();
        }

        public override void SendMessage()
        {
            throw new System.NotImplementedException();
        }

        public override void Update()
        {
            throw new System.NotImplementedException();
        }
    }
}