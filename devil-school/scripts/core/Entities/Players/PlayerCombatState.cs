
using System.Collections.Generic;

namespace EGame
{
    //一场战斗中的临时状态
    public class PlayerCombatState
    {
        private Player _Player;
        private readonly List<Creature> _pets = new List<Creature>();
        public IReadOnlyList<Creature> Pets => _pets;

        public PlayerCombatState(Player player)
        {
            this._Player = player;
        }

        public void AddPet(Creature creature)
        {
            creature.PetOwner = this._Player;
            _pets.Add(creature);
        }
    }
}