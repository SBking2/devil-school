
namespace EGame
{
    public class Player
    {
        private ulong _NetID;
        public Creature Creature { get; }
        public CharacterModel Character { get; }

        private Player(CharacterModel model, ulong net_id, int max_hp)
        {
            _NetID = net_id;
            Character = model;
            Creature = new Creature(this, max_hp);
        }

        /// <summary>
        /// 新开一个档
        /// </summary>
        public static Player CreatureForNewRun(CharacterModel model, ulong net_id, int max_hp)
        {
            return new Player(model, net_id, max_hp);
        }
    }
}