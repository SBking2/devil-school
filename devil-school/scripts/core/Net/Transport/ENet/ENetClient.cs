
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

namespace EGame
{
    public class ENetClient : AbstractNetClient
    {        
        public override ulong HostID => 1uL;
        public override ulong ClientID => _NetID;

        private ulong _NetID;
        private Log.Logger _Logger = new Log.Logger(Log.LogType.NetWork);
        private ENetConnection _Connection;
        private ENetPacketPeer _Peer;
        public ENetClient(INetClientHandler handler) : base(handler)
        {

        }

        public async Task ConnectToHost(ulong net_id)
        {
            _NetID = net_id;
            _Connection = new ENetConnection();
            _Peer = _Connection.ConnectToHost("0.0.0.0", 8080);     //向Host发送请求，此时Host会响应Connect事件

            //发送了底层连接之后，并不会立马连接成功，需要等到连接建立了才能发送HandShakeRequest
            ENetServiceData? data = null;
            int total_delay = 0;
            while(data.HasValue == false || data.Value.Event != ENetConnection.EventType.Connect)
            {
                await Task.Delay(100);
                total_delay += 100;

                if(total_delay > 10000)
                {
                    //TODO:关闭Client
                    _Logger.Error("failed to connect host!");
                    return;
                }
            }

            //到这里算是连接成功了，要开始HandShake环节了
            List<ENetServiceData> data_buffer = new List<ENetServiceData>();
            await SendHandShakeAndWait(data_buffer);

            //处理提前到来的包
            if(_IsConnected)
            {
                foreach (var msg_service_data in data_buffer)
                    HandleServiceDataReceive(msg_service_data);
            }
        }

        /// <summary>
        /// 发送HandShake包，并等到Host给出请求
        /// </summary>
        private async Task SendHandShakeAndWait(List<ENetServiceData> message_buffer)
        {
            ENetHandShakeRequest request = new ENetHandShakeRequest()
            {
                ClientID = _NetID
            };

            var packet = ENetPacket.FromHandShakeRequest(request);
            _Peer.Send(0, packet.Data, ENetUtils.FlagsFromMode(NetTransferMode.Reliable));
            
            //包发出去后，要开始等Ack
            ENetServiceData? data = null;
            int total_delay = 0;
            bool receive_ack = false;
            while(!receive_ack)
            {
                if(_Connection.TryGetServiceData(out data) && data.Value.Event == ENetConnection.EventType.Receive)
                {
                    ENetPacket receive_packet = new ENetPacket(data.Value.Data);
                    var packet_type = receive_packet.PacketType;
                    if(packet_type == ENetPacketType.HandShakeResponse)
                    {
                        receive_ack = true;
                        break;
                    }
                    //此处存储，是因为HandShakeRequest到达后，Ack到达前，Host可以向Client发信息
                    else if(packet_type == ENetPacketType.AppMessage)
                    {
                        message_buffer.Add(data.Value);
                    }    
                }

                await Task.Delay(100);
                total_delay += 100;
                if(total_delay > 100)
                {
                    //TODO : 关闭Client
                    _Logger.Error("failed recive host handshake ack!");
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

        private void HandleServiceDataReceive(ENetServiceData data)
        {

        }
    }
}