
namespace EGame
{
    /// <summary>
    /// 一局游戏里的数据
    /// </summary>
    public class Player
    {
        private ulong _NetID;
        public Creature Creature { get; }
        public PlayerModel PlayerModel { get; }

        /// <summary>
        /// 一场战斗里的临时状态
        /// </summary>
        public PlayerCombatState PlayerCombatState { get; private set; }
        
        private Player(PlayerModel model, ulong net_id, int max_hp)
        {
            _NetID = net_id;
            PlayerModel = model;
            Creature = new Creature(this, max_hp);
        }

        /// <summary>
        /// 新开一个档
        /// </summary>
        public static Player CreatureForNewRun(PlayerModel model, ulong net_id, int max_hp)
        {
            return new Player(model, net_id, max_hp);
        }

        public void ResetPlayerCombatState()
        {
            PlayerCombatState = new PlayerCombatState(this);
        }
    }
}