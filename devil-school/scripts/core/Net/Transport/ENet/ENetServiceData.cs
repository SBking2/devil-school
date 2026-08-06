
using Godot;
using Godot.Collections;

namespace EGame
{
    /// <summary>
    /// 用于封装ENet捕获到的数据
    /// </summary>
    public struct ENetServiceData
    {
        public ENetConnection.EventType Event;

        public ENetPacketPeer Peer;

        public uint Channel;

        public NetTransferMode Mode;

        public byte[] Data;

        public Error Error;

        public Array OriginalData;
    }
}