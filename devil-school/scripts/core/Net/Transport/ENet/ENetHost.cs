
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

namespace EGame
{
    public class ENetHost : AbstractNetHost
    {
        /// <summary>
        /// 发送了HandShakeRequest，等待Host响应
        /// </summary>
        private struct HandShakeRequestWait
        {
            public ulong ClinetID;
            public ENetPacketPeer Peer;
        }

        private struct ClientConnection
        {
            public ENetPacketPeer Peer;
            public ulong ClientID;
        }

        public override ulong HostID => 1uL;
        public override IEnumerable<ulong> ClientIDs
        {
            get
            {
                foreach (var connection in _ClientConnections)
                {
                    yield return connection.ClientID;
                }
            }
        }

        private List<ClientConnection> _ClientConnections = new List<ClientConnection>();
        private ENetConnection _Connection;
        private Log.Logger _Logger = new Log.Logger(Log.LogType.NetWork);

        //因为Host是一对多，需要容器把Request存起来一一处理
        private List<HandShakeRequestWait> _WaitHandShakeRequests = new List<HandShakeRequestWait>();

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

        /// <summary>
        /// 与Client建立连接之后，就需要开一个异步处理HandShake,一个线程处理一个ID，因为存在容器里了，不用担心会吃掉其他Client的HandShake,通过Peer来辨识
        /// </summary>
        private async Task WaitClientHandShake(ENetPacketPeer peer)
        {
            //直接开始等待客户端的
            int total_delay = 0;
            HandShakeRequestWait? wait_response = null;
            while(wait_response.HasValue == false)
            {
                foreach(var wait in _WaitHandShakeRequests)
                {
                    if (peer == wait.Peer)
                    {
                        wait_response = wait;
                        break;
                    }
                }
                
                if(wait_response.HasValue == false)
                {
                    await Task.Delay(100);
                    total_delay += 100;
                    if (total_delay > 10000)
                        _Logger.Error("failed receive client handshake!");
                }
            }

            ENetHandShakeResponse response = new ENetHandShakeResponse();
            response.ClientID = wait_response.Value.ClinetID;
            if (GetPeerByID(wait_response.Value.ClinetID) != null)
            {
                //ID撞了，让它滚
                response.ResponseType = ENetHandShakeResponseType.IdCollision;
                var packet = ENetPacket.FromHandShakeResponse(response);
                wait_response.Value.Peer.Send(0, packet.Data, ENetUtils.FlagsFromMode(NetTransferMode.Reliable));
            }
            else
            {
                //确认，返回Ack包
                response.ResponseType = ENetHandShakeResponseType.Sucess;
                var packet = ENetPacket.FromHandShakeResponse(response);
                wait_response.Value.Peer.Send(0, packet.Data, ENetUtils.FlagsFromMode(NetTransferMode.Reliable));
                _ClientConnections.Add(new ClientConnection()
                {
                    ClientID = wait_response.Value.ClinetID,
                    Peer = wait_response.Value.Peer
                });
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
                        TaskHelper.RunSafely(WaitClientHandShake(data.Value.Peer));
                        break;
                    case ENetConnection.EventType.Receive:
                        break;
                    default:
                        throw new InvalidOperationException();
                }
            }
        }

        private void HandleServiceDataReceive(ENetServiceData data)
        {
            ENetPacket packet = new ENetPacket(data.Data);
            if(packet.PacketType == ENetPacketType.HandShakeRequest)
            {
                ENetHandShakeRequest request = packet.AsHandShakeRequest();
                _WaitHandShakeRequests.Add(new HandShakeRequestWait()
                {
                    ClinetID = request.ClientID,
                    Peer = data.Peer
                });
            }
        }

        private ENetPacketPeer GetPeerByID(ulong id)
        {
            foreach(var connection in _ClientConnections)
            {
                if (connection.ClientID == id)
                    return connection.Peer;
            }
            return null;
        }
    }
}