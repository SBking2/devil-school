
namespace EGame
{
    /// <summary>
    /// 这是传输层提供给上层服务层的接口
    /// </summary>
    public interface INetHandler
    {
        public void OnPacketReceived(ulong sender_id, byte[] data);
    }
}