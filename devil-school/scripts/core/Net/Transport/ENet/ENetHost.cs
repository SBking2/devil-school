
using System;
using System.Threading.Tasks;
using Godot;

namespace EGame
{
    public class ENetHost : AbstractNetHost
    {
        private ENetConnection _Connection;
        private Log.Logger _Logger = new Log.Logger(Log.LogType.NetWork);

        public ENetHost(INetHostHandler handler) : base(handler)
        {
            
        }

        public void StartHost()
        {
            _Connection = new ENetConnection();
            var result = _Connection.CreateHostBound("0.0.0.0", 8080);

            if (result != Error.Ok)
            {
                _Logger.Warn($"ENetHost failed to create host! Info : {result.ToString()}");
                return;
            }

        }

        private async Task WaitClientHandShake()
        {
            //直接开始等待客户端的
            int total_delay = 0;
            ENetServiceData? data = null;
            while(_Connection.TryGetServiceData(out data))
            {
                await Task.Delay(100);
                total_delay += 100;

                ENetPacket packet = new ENetPacket(data.Value.Data);
                if (packet.PacketType == ENetPacketType.HandShake)
                {
                    ENetHandShake handshake = packet.AsHandShake();
                    ulong client_id = handshake.ClientID;
                    //TODO:记录Client
                    return;
                }
                else
                    data = null;

                if (total_delay > 10000)
                    _Logger.Error("Wait Client HandShake ovettime!");
            }
        }

        public override void DisConnectClient()
        {
            throw new System.NotImplementedException();
        }

        public override void SendMessage()
        {
            throw new System.NotImplementedException();
        }

        public override void SendMessageAll()
        {
            throw new System.NotImplementedException();
        }

        public override void Update()
        {
            ENetServiceData? data = null;
            if(_Connection.TryGetServiceData(out data))
            {
                switch(data.Value.Event)
                {
                    case ENetConnection.EventType.None:
                        break;
                    case ENetConnection.EventType.Connect:
                        TaskHelper.RunSafely(WaitClientHandShake());
                        break;
                    case ENetConnection.EventType.Receive:
                        break;
                    default:
                        throw new InvalidOperationException();
                }
            }
        }
    }
}