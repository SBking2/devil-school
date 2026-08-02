
namespace EGame
{
    public interface ISerializable
    {
        public void Serialize(PacketWriter writer);
        public void Deserialize(PacketReader reader);
    }
}