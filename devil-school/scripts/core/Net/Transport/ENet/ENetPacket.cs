
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
        public ENetPacketType PacketType => (ENetPacketType)_Data[0];

        public static ENetPacket FromHandShake(ENetHandShake handshake)
        {
            byte[] data = new byte[9];
            data[0] = (byte)ENetPacketType.HandShake;
            BinaryPrimitives.WriteUInt64BigEndian(data[1..].AsSpan(), handshake.ClientID);
            ENetPacket packet = new ENetPacket(data);
            return packet;
        }

        public ENetHandShake AsHandShake()
        {
            ENetHandShake ans = new ENetHandShake();
            ulong client_id = BinaryPrimitives.ReadUInt64BigEndian(_Data[1..].AsSpan());
            ans.ClientID = client_id;
            return ans;
        }

        public static ENetPacket FromDisconnect(ENetDisconnect disconnect)
        {
            byte[] data = new byte[9];
            data[0] = (byte)ENetPacketType.Disconnect;
            BinaryPrimitives.WriteUInt64BigEndian(data[1..].AsSpan(), disconnect.ClientID);
            ENetPacket packet = new ENetPacket(data);
            return packet;
        }

        public ENetDisconnect AsDisconnect()
        {
            ENetDisconnect ans = new ENetDisconnect();
            ulong client_id = BinaryPrimitives.ReadUInt64BigEndian(_Data[1..].AsSpan());
            ans.ClientID = client_id;
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