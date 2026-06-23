
using Godot;
using System.Collections.Generic;

namespace EGame
{
    public abstract class EncounterModel : AbstractModel
    {
        protected virtual string _EncounterSlotName => $"encounters/{ID.Entry.ToLowerInvariant()}";

        public Control CreateEncounterSlots()
        {
            return SceneHelper.LoadScene<Control>(_EncounterSlotName);
        }

        protected IReadOnlyList<(MonsterModel, string)> _MonstersWithSlots;

        public IReadOnlyList<(MonsterModel, string)> MonsterWithSlot
        {
            get
            {
                AssertMutable();
                return _MonstersWithSlots;
            }
        }

        /// <summary>
        /// 用于控制这个Encounter生成的怪物
        /// </summary>
        protected abstract IReadOnlyList<(MonsterModel, string)> GeneratorMonsters();

        public void GenerateMonsterWithSlost()
        {
            AssertMutable();
            _MonstersWithSlots = GeneratorMonsters();
        }
    }
}