
using System.Collections.Generic;

namespace EGame
{
    public class DebugEncounterModel : EncounterModel
    {
        protected override IReadOnlyList<(MonsterModel, string)> GeneratorMonsters()
        {
            return new (MonsterModel, string)[]
            {
                (ModelDB.Monster<FighterModel>(), "front"),
                (ModelDB.Monster<MagicModel>(), "back")
            };
        }
    }
}