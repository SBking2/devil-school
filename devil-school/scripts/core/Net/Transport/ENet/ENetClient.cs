using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

namespace EGame
{
    public class ENetClient : AbstractNetClient
    {
        private const int ConnectTimeoutMsec = 10000;
        private const int HandShakeTimeoutMsec = 10000;
        private const int PollRateMsec = 100;

        public override ulong HostID => 1uL;
        public override ulong ClientID => _NetID;

        private ulong _NetID;
        private Log.Logger _Logger = new Log.Logger(Log.LogType.NetWork);
        private ENetConnection _Connection;
        private ENetPacketPeer _Peer;

        public ENetClient(INetClientHandler handler) : base(handler)
        {
        }

        public async Task<ENetConnectResult> ConnectToHost(ulong net_id)
        {
            CleanupConnection();

            _NetID = net_id;
            _Connection = new ENetConnection();
            Error createResult = _Connection.CreateHost();
            if(createResult != Error.Ok)
            {
                _Logger.Error($"failed to create client host! {createResult}");
                CleanupConnection();
                return ENetConnectResult.ConnectionFailed;
            }

            _Peer = _Connection.ConnectToHost("127.0.0.1", 8080);
            ENetConnectResult connectResult = await WaitForConnectEvent();
            if(connectResult != ENetConnectResult.Success)
            {
                CleanupConnection();
                return connectResult;
            }

            List<ENetServiceData> dataBuffer = new List<ENetServiceData>();
            ENetConnectResult handShakeResult = await SendHandShakeAndWait(dataBuffer);
            if(handShakeResult != ENetConnectResult.Success)
            {
                CleanupConnection();
                return handShakeResult;
            }

            foreach (ENetServiceData msgServiceData in dataBuffer)
                HandleReceiveMessage(msgServiceData);

            return ENetConnectResult.Success;
        }

        private async Task<ENetConnectResult> WaitForConnectEvent()
        {
            int totalDelay = 0;
            while(totalDelay <= ConnectTimeoutMsec)
            {
                if(_Connection.TryGetServiceData(out ENetServiceData? data))
                {
                    if(data.Value.Event == ENetConnection.EventType.Connect)
                        return ENetConnectResult.Success;

                    if(data.Value.Event == ENetConnection.EventType.Disconnect)
                    {
                        _Logger.Error("disconnected while connecting to host!");
                        return ENetConnectResult.ConnectionFailed;
                    }

                    _Logger.Error($"unexpected ENet event while connecting: {data.Value.Event}");
                    return ENetConnectResult.ConnectionFailed;
                }

                await Task.Delay(PollRateMsec);
                totalDelay += PollRateMsec;
            }

            _Logger.Error("failed to connect host!");
            return ENetConnectResult.Timeout;
        }

        //发送HandShake包，并等待Host的Ack
        private async Task<ENetConnectResult> SendHandShakeAndWait(List<ENetServiceData> messageBuffer)
        {
            ENetHandShakeRequest request = new ENetHandShakeRequest()
            {
                ClientID = _NetID
            };

            ENetPacket packet = ENetPacket.FromHandShakeRequest(request);
            _Peer.Send(0, packet.Data, ENetUtils.FlagsFromMode(NetTransferMode.Reliable));

            int totalDelay = 0;
            while(totalDelay <= HandShakeTimeoutMsec)
            {
                if(_Connection.TryGetServiceData(out ENetServiceData? data))
                {
                    if(data.Value.Event == ENetConnection.EventType.Receive)
                    {
                        ENetPacket receivePacket = new ENetPacket(data.Value.Data);
                        ENetPacketType packetType = receivePacket.PacketType;
                        if(packetType == ENetPacketType.HandShakeResponse)
                        {
                            ENetHandShakeResponse handShakeResponse = receivePacket.AsHandShakeResponse();
                            if(handShakeResponse.ResponseType == ENetHandShakeResponseType.Sucess)
                            {
                                _IsConnected = true;
                                _NetHandler.OnConnected();
                                return ENetConnectResult.Success;
                            }

                            _Logger.Error("client ID collision!");
                            _Peer.PeerDisconnectLater();
                            _Connection.Flush();
                            return ENetConnectResult.IdCollision;
                        }

                        //在接收到ack之前，服务器发的包可能会先到                                 
                        if(packetType == ENetPacketType.AppMessage)
                        {
                            messageBuffer.Add(data.Value);
                            continue;
                        }

                        _Logger.Error($"unexpected packet while waiting for handshake ack: {packetType}");
                        return ENetConnectResult.HandshakeFailed;
                    }

                    if(data.Value.Event == ENetConnection.EventType.Disconnect)
                    {
                        _Logger.Error("disconnected while waiting for host handshake ack!");
                        return ENetConnectResult.ConnectionFailed;
                    }

                    _Logger.Error($"unexpected ENet event while waiting for handshake ack: {data.Value.Event}");
                    return ENetConnectResult.HandshakeFailed;
                }

                await Task.Delay(PollRateMsec);
                totalDelay += PollRateMsec;
            }

            _Logger.Error("failed receive host handshake ack!");
            return ENetConnectResult.Timeout;
        }

        public override void DisConnectFromHost(bool immediately)
        {
            if(!_IsConnected)
            {
                CleanupConnection();
                return;
            }

            if(immediately)
            {
                _Peer?.PeerDisconnectNow();
            }
            else
            {
                ENetDisconnect disconnect = new ENetDisconnect()
                {
                    Error = Error.Ok,
                    ClientID = _NetID
                };
                ENetPacket packet = ENetPacket.FromDisconnect(disconnect);
                _Peer?.Send(0, packet.Data, ENetUtils.FlagsFromMode(NetTransferMode.Reliable));
                _Peer?.PeerDisconnectLater();
                _Connection?.Flush();
            }

            NotifyDisconnected();
        }

        public override void SendMessage(byte[] data)
        {
            if(!_IsConnected || _Peer == null)
            {
                _Logger.Warn("client tried to send message before connected");
                return;
            }

            if(data == null)
            {
                _Logger.Warn("client tried to send null message");
                return;
            }

            ENetPacket packet = ENetPacket.FromAppMessage(new ENetAppMessage()
            {
                Message = data
            });

            _Peer.Send(0, packet.Data, ENetUtils.FlagsFromMode(NetTransferMode.Reliable));
            _Connection?.Flush();
        }

        public override void Update()
        {
            if(_Connection == null)
                return;

            ENetServiceData? data = null;
            while(_Connection != null && _Connection.TryGetServiceData(out data))
            {
                if(data.Value.Event == ENetConnection.EventType.Receive)
                {
                    HandleReceiveMessage(data.Value);
                }
                else if(data.Value.Event == ENetConnection.EventType.Disconnect)
                {
                    NotifyDisconnected();
                }
                else
                {
                    _Logger.Error($"unexpected ENet event on client update: {data.Value.Event}");
                    NotifyDisconnected();
                }
            }
        }

        private void HandleReceiveMessage(ENetServiceData data)
        {
            ENetPacket packet = new ENetPacket(data.Data);
            if(packet.PacketType == ENetPacketType.AppMessage)
            {
                _NetHandler.OnPacketReceived(HostID, packet.Message);
            }
            else if(packet.PacketType == ENetPacketType.Disconnect)
            {
                NotifyDisconnected();
            }
        }

        /// <summary>
        /// 已经连接成功后断开连接
        /// </summary>
        private void NotifyDisconnected()
        {
            bool wasConnected = _IsConnected;
            _IsConnected = false;
            _Connection?.Destroy();
            _Connection = null;
            _Peer = null;

            if(wasConnected)
                _NetHandler.OnDisconnected();
        }

        /// <summary>
        /// 连接成功前断开连接
        /// </summary>
        private void CleanupConnection()
        {
            _IsConnected = false;
            _Peer?.Reset();     //强制丢弃这个连接
            _Connection?.Destroy();
            _Connection = null;
            _Peer = null;
        }
    }
}
