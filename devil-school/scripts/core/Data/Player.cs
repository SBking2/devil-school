
namespace EGame
{
    public class Player
    {
        public Player()
        {
            this.PlayerModel = ModelDB.Player<PlayerModel>().MutableClone() as PlayerModel;
        }

        public PlayerModel PlayerModel { get; private set; }
    }
}