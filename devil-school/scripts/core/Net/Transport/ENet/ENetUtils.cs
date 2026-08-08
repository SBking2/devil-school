using Godot;
using Godot.Collections;

namespace EGame
{
    public enum ENetPacketType : byte
    {
        HandShakeRequest = 0,
        HandShakeResponse = 1,
        Disconnect = 2,
        AppMessage = 3
    }

    public struct ENetHandShakeRequest
    {
        public ulong ClientID;
    }

    public enum ENetHandShakeResponseType : byte
    {
        Sucess = 0,
        IdCollision = 1
    }

    public enum ENetConnectResult
    {
        Success,
        ConnectionFailed,
        Timeout,
        IdCollision,
        HandshakeFailed
    }

    public struct ENetHandShakeResponse
    {
        public ENetHandShakeResponseType ResponseType;
    }

    public struct ENetDisconnect
    {
        public ulong ClientID;
        public Error Error;
    }

    public struct ENetAppMessage
    {
        public byte[] Message;
    }

    public static class ENetConnectionExtension
    {
        public static bool TryGetServiceData(this ENetConnection connection, out ENetServiceData? data)
        {
            Array array = connection.Service();
            data = null;
            if (array == null)
                return false;

            ENetConnection.EventType eventType = array[0].As<ENetConnection.EventType>();
            if (eventType == ENetConnection.EventType.None)
                return false;

            ENetServiceData value = new ENetServiceData
            {
                Event = eventType,
                Peer = array[1].As<ENetPacketPeer>(),
                OriginalData = array
            };

            if (eventType == ENetConnection.EventType.Receive)
            {
                value.Channel = array[3].As<int>();
                value.Data = value.Peer.GetPacket();
                value.Error = value.Peer.GetPacketError();
                value.Mode = NetTransferMode.None;
            }

            data = value;
            return true;
        }
    }

    public static class ENetUtils
    {
        public static int FlagsFromMode(NetTransferMode mode)
        {
            return mode switch
            {
                NetTransferMode.UnReliable => 8,
                NetTransferMode.Reliable => 1,
                _ => throw new System.ArgumentOutOfRangeException("mode", mode, null),
            };
        }

        public static NetTransferMode ModeFromFlags(int flags)
        {
            if ((long)((ulong)flags & 1uL) > 0L)
                return NetTransferMode.Reliable;

            if ((long)((ulong)flags & 8uL) > 0L)
                return NetTransferMode.UnReliable;

            throw new System.ArgumentOutOfRangeException($"Flags {flags} cannot be mapped to NetTransferMode!");
        }
    }
}
