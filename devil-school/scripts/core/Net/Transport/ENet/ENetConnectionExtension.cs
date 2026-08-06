
using Godot;

namespace EGame
{
    /// <summary>
    /// 把ENet.Service()读到的数据转换成自己的结构体
    /// </summary>
    public static class ENetConnectionExtension
    {
        public static bool TryGetServiceData(this ENetConnection connection, out ENetServiceData? data)
        {
            var service_data = connection.Service();
            if (service_data == null)
            {
                data = null;
                return false;
            }

            ENetServiceData ans = new ENetServiceData();
            ans.OriginalData = service_data;
            ans.Event = service_data[0].As<ENetConnection.EventType>();
            ans.Peer = service_data[1].As<ENetPacketPeer>();
            ans.Channel = service_data[2].As<uint>();
            ans.Data = ans.Peer.GetPacket();
            ans.Error = ans.Peer.GetPacketError();
            data = ans;

            return true;
        }
    }

}