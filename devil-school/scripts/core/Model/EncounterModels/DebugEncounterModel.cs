
using System.Collections.Generic;

namespace EGame
{
    public class DebugEncounterModel : EncounterModel
    {
        protected override IReadOnlyList<(MonsterModel, string)> GeneratorMonsters()
        {
            return new (MonsterModel, string)[]
            {
                (ModelDB.Monster<FighterModel>().MutableClone() as MonsterModel, "front"),
                (ModelDB.Monster<MagicModel>().MutableClone() as MonsterModel, "back")
            };
        }
    }
}