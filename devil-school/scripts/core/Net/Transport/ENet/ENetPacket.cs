
using Godot;
using System;
using System.Buffers.Binary;

namespace EGame
{
    /// <summary>
    /// 在消息包体的头部增加了一个用于辨别消息类型的字节
    /// </summary>
    public class ENetPacket
    {
        private readonly byte[] _Data;
        public ENetPacket(byte[] data)
        {
            _Data = data;
        }

        public byte[] Data => _Data;
        public byte[] Message => _Data[1..];
        public ENetPacketType PacketType => (ENetPacketType)_Data[0];
        public static ENetPacket FromHandShakeRequest(ENetHandShakeRequest handshake_request)
        {
            byte[] data = new byte[9];
            data[0] = (byte)ENetPacketType.HandShakeRequest;
            BinaryPrimitives.WriteUInt64BigEndian(data.AsSpan(1), handshake_request.ClientID);
            ENetPacket packet = new ENetPacket(data);
            return packet;
        }

        public ENetHandShakeRequest AsHandShakeRequest()
        {
            ENetHandShakeRequest ans = new ENetHandShakeRequest();
            ulong client_id = BinaryPrimitives.ReadUInt64BigEndian(_Data.AsSpan(1));
            ans.ClientID = client_id;
            return ans;
        }
        public static ENetPacket FromHandShakeResponse(ENetHandShakeResponse handshake_response)
        {
            byte[] data = new byte[10];
            data[0] = (byte)ENetPacketType.HandShakeResponse;
            data[1] = (byte)handshake_response.ResponseType;
            ENetPacket packet = new ENetPacket(data);
            return packet;
        }

        public ENetHandShakeResponse AsHandShakeResponse()
        {
            ENetHandShakeResponse ans = new ENetHandShakeResponse();
            ans.ResponseType = (ENetHandShakeResponseType)_Data[1];
            return ans;
        }

        public static ENetPacket FromDisconnect(ENetDisconnect disconnect)
        {
            byte[] data = new byte[10];
            data[0] = (byte)ENetPacketType.Disconnect;
            data[1] = (byte)disconnect.Error;
            BinaryPrimitives.WriteUInt64BigEndian(data.AsSpan(2), disconnect.ClientID);
            ENetPacket packet = new ENetPacket(data);
            return packet;
        }

        public ENetDisconnect AsDisconnect()
        {
            ENetDisconnect ans = new ENetDisconnect();
            ans.Error = (Error)_Data[1];
            ans.ClientID = BinaryPrimitives.ReadUInt64BigEndian(_Data.AsSpan(2));
            return ans;
        }

        public static ENetPacket FromAppMessage(ENetAppMessage message)
        {
            byte[] data = new byte[message.Message.Length + 1];
            data[0] = (byte)ENetPacketType.AppMessage;

            Array.Copy(message.Message, 0, data, 1, message.Message.Length);
            ENetPacket packet = new ENetPacket(data);
            return packet;
        }

        public ENetAppMessage AsENetAppMessage()
        {
            ENetAppMessage ans = new ENetAppMessage();
            ans.Message = _Data[1..];
            return ans;
        }
    }
}
