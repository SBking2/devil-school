using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

namespace EGame
{
    public class ENetHost : AbstractNetHost
    {
        private const int HandShakeTimeoutMsec = 10000;
        private const int PollRateMsec = 100;

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
                foreach (ClientConnection connection in _ClientConnections)
                    yield return connection.ClientID;
            }
        }

        private List<ClientConnection> _ClientConnections = new List<ClientConnection>();
        private ENetConnection _Connection;
        private Log.Logger _Logger = new Log.Logger(Log.LogType.NetWork);
        private List<HandShakeRequestWait> _WaitHandShakeRequests = new List<HandShakeRequestWait>();

        public ENetHost(INetHostHandler handler) : base(handler)
        {
        }

        public Error StartHost()
        {
            _Connection = new ENetConnection();
            Error result = _Connection.CreateHostBound("0.0.0.0", 8080);

            if (result != Error.Ok)
            {
                _Logger.Warn($"ENetHost failed to create host! Info : {result}");
                _Connection.Destroy();
                _Connection = null;
                return result;
            }

            return Error.Ok;
        }

        private async Task WaitClientHandShake(ENetPacketPeer peer)
        {
            int totalDelay = 0;
            HandShakeRequestWait? waitResponse = null;
            while(waitResponse.HasValue == false)
            {
                foreach(HandShakeRequestWait wait in _WaitHandShakeRequests)
                {
                    if (peer == wait.Peer)
                    {
                        waitResponse = wait;
                        break;
                    }
                }

                if(waitResponse.HasValue == false)
                {
                    await Task.Delay(PollRateMsec);
                    totalDelay += PollRateMsec;
                    if (totalDelay > HandShakeTimeoutMsec)
                    {
                        _Logger.Error("failed receive client handshake!");
                        RemoveWaitingHandshake(peer);
                        peer.Reset();
                        return;
                    }
                }
            }

            ENetHandShakeResponse response = new ENetHandShakeResponse();
            if (GetConnectionByID(waitResponse.Value.ClinetID) != null)
            {
                response.ResponseType = ENetHandShakeResponseType.IdCollision;
                ENetPacket packet = ENetPacket.FromHandShakeResponse(response);
                waitResponse.Value.Peer.Send(0, packet.Data, ENetUtils.FlagsFromMode(NetTransferMode.Reliable));
                waitResponse.Value.Peer.PeerDisconnectLater();
                _Connection?.Flush();
                RemoveWaitingHandshake(peer);
                return;
            }

            response.ResponseType = ENetHandShakeResponseType.Sucess;
            ENetPacket successPacket = ENetPacket.FromHandShakeResponse(response);
            waitResponse.Value.Peer.Send(0, successPacket.Data, ENetUtils.FlagsFromMode(NetTransferMode.Reliable));
            _ClientConnections.Add(new ClientConnection()
            {
                ClientID = waitResponse.Value.ClinetID,
                Peer = waitResponse.Value.Peer
            });
            RemoveWaitingHandshake(peer);
            _NetHandler.OnClientConnected();
        }

        public override void DisConnectClient(ulong client_id, bool immediately)
        {
            ClientConnection? connection = GetConnectionByID(client_id);
            if(connection == null)
                return;

            if(immediately)
            {
                connection.Value.Peer.PeerDisconnectNow();
            }
            else
            {
                ENetDisconnect disconnect = new ENetDisconnect()
                {
                    ClientID = client_id,
                    Error = Error.Ok
                };

                ENetPacket packet = ENetPacket.FromDisconnect(disconnect);
                connection.Value.Peer.Send(0, packet.Data, ENetUtils.FlagsFromMode(NetTransferMode.Reliable));
                connection.Value.Peer.PeerDisconnectLater();
                _Connection?.Flush();
            }

            HandleClientDisconnected(connection.Value);
        }

        public void StopHost()
        {
            List<ClientConnection> connections = new List<ClientConnection>(_ClientConnections);
            foreach(ClientConnection connection in connections)
            {
                connection.Peer.PeerDisconnectNow();
                HandleClientDisconnected(connection);
            }

            _WaitHandShakeRequests.Clear();
            _Connection?.Destroy();
            _Connection = null;
        }

        public override void SendMessage(ulong client_id, byte[] data)
        {
            if(data == null)
            {
                _Logger.Warn("host tried to send null message");
                return;
            }

            ClientConnection? connection = GetConnectionByID(client_id);
            if(connection != null)
            {
                ENetPacket packet = ENetPacket.FromAppMessage(new ENetAppMessage()
                {
                    Message = data
                });

                connection.Value.Peer.Send(0, packet.Data, ENetUtils.FlagsFromMode(NetTransferMode.Reliable));
                _Connection?.Flush();
                return;
            }

            _Logger.Warn($"host tried to send message to unknown client: {client_id}");
        }

        public override void SendMessageAll(byte[] data)
        {
            if(data == null)
            {
                _Logger.Warn("host tried to broadcast null message");
                return;
            }

            ENetPacket packet = ENetPacket.FromAppMessage(new ENetAppMessage()
            {
                Message = data
            });

            foreach(ClientConnection connection in _ClientConnections)
            {
                connection.Peer.Send(0, packet.Data, ENetUtils.FlagsFromMode(NetTransferMode.Reliable));
            }

            _Connection?.Flush();
        }

        public override void Update()
        {
            if(_Connection == null)
                return;

            ENetServiceData? data = null;
            while(_Connection != null && _Connection.TryGetServiceData(out data))
            {
                switch(data.Value.Event)
                {
                    case ENetConnection.EventType.Disconnect:
                    {
                        ENetPacketPeer peer = data.Value.Peer;
                        ClientConnection? connection = GetConnectionByPeer(peer);
                        if(connection != null)
                            HandleClientDisconnected(connection.Value);
                        else
                            RemoveWaitingHandshake(peer);

                        continue;
                    }
                    case ENetConnection.EventType.Connect:
                        TaskHelper.RunSafely(WaitClientHandShake(data.Value.Peer));
                        continue;
                    case ENetConnection.EventType.Receive:
                        HandleReceiveMessage(data.Value);
                        continue;
                    default:
                        _Logger.Error($"unexpected ENet event on host update: {data.Value.Event}");
                        continue;
                }
            }
        }

        private void HandleReceiveMessage(ENetServiceData data)
        {
            ENetPacket packet = new ENetPacket(data.Data);
            if(packet.PacketType == ENetPacketType.HandShakeRequest)
            {
                ENetHandShakeRequest request = packet.AsHandShakeRequest();
                RemoveWaitingHandshake(data.Peer);
                _WaitHandShakeRequests.Add(new HandShakeRequestWait()
                {
                    ClinetID = request.ClientID,
                    Peer = data.Peer
                });
            }
            else if(packet.PacketType == ENetPacketType.AppMessage)
            {
                var connection = GetConnectionByPeer(data.Peer);
                if (connection != null)
                    _NetHandler.OnPacketReceived(connection.Value.ClientID, packet.Message);
                else
                    _Logger.Warn("received app message from peer before handshake completed");
            }
            else if (packet.PacketType == ENetPacketType.Disconnect)
            {
                ClientConnection? connection = GetConnectionByPeer(data.Peer);
                if(connection != null)
                    HandleClientDisconnected(connection.Value);
                else
                    RemoveWaitingHandshake(data.Peer);
            }
        }

        private void HandleClientDisconnected(ClientConnection connection)
        {
            RemoveWaitingHandshake(connection.Peer);
            _ClientConnections.Remove(connection);
            _NetHandler.OnClientDisconnected();
        }

        private void RemoveWaitingHandshake(ENetPacketPeer peer)
        {
            _WaitHandShakeRequests.RemoveAll(wait => wait.Peer == peer);
        }

        private ClientConnection? GetConnectionByID(ulong id)
        {
            foreach(ClientConnection connection in _ClientConnections)
            {
                if (connection.ClientID == id)
                    return connection;
            }
            return null;
        }

        private ClientConnection? GetConnectionByPeer(ENetPacketPeer peer)
        {
            foreach(ClientConnection connection in _ClientConnections)
            {
                if (connection.Peer == peer)
                    return connection;
            }
            return null;
        }
    }
}
