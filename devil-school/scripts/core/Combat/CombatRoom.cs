
using System.Collections.Generic;

namespace EGame
{
    public class CombatRoom
    {
        public CombatState CombatState { get; private set; }
        public IReadOnlyList<Creature> Allies => CombatState.Allies;
        public IReadOnlyList<Creature> Enemies => CombatState.Enemies;

        public CombatRoom()
        {
            CombatState = new CombatState();

            var creature1 = new Creature(ModelDB.Monster<FighterModel>(), CombatSide.Enemy);
            CombatState.AddCreature(creature1);

            var creature2 = new Creature(ModelDB.Monster<MagicModel>(), CombatSide.Enemy);
            CombatState.AddCreature(creature2);
        }
    }
}