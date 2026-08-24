
namespace EGame
{
    public class Player
    {
        public Player()
        {
            this.PlayerModel = ModelDB.Player<PlayerModel>();
            CreatureData = new Creature(this.PlayerModel);
        }

        public Creature CreatureData { get; private set; }
        public PlayerModel PlayerModel { get; private set; }
    }
}