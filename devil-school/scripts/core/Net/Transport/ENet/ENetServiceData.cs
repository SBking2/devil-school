
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

        //通道是指包体的通道，可以自己指定
        public int Channel;

        public NetTransferMode Mode;

        public byte[] Data;

        public Error Error;

        public Array OriginalData;
    }
}