
using Godot;
using System.Collections.Generic;

namespace EGame
{
    public partial class NCombatRoom : Control
    {
        private const string COMBAT_SCENE_PATH = "combat/combat_room";
        public CombatRoom Data { get; private set; }

        private Control _SceneContainer;
        private Control _AllyContainer;
        private Control _EnemyContainer;
        
        public static NCombatRoom Instance => NRun.Instance.CombatRoomNode;

        public static NCombatRoom Create(CombatRoom data)
        {
            var combat_room = SceneHelper.LoadScene<NCombatRoom>(COMBAT_SCENE_PATH);
            combat_room.Data = data;
            return combat_room;
        }
        
        public override void _Ready()
        {
            base._Ready();

            _SceneContainer = GetNode<Control>("%SceneContainer");
            _AllyContainer = GetNode<Control>("%AllyContainer");
            _EnemyContainer = GetNode<Control>("%EnemyContainer");

            CreateAllyNode();
            CreateEnemyNode();
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////
        ////////                               Creature
        ////////////////////////////////////////////////////////////////////////////////////////////////

        private List<NCreature> _AllyCreatureNodes = new List<NCreature>();
        private List<NCreature> _EnemyCreatureNodes = new List<NCreature>();

        ////////////////////////////////////////////////////////////////////////////////////////////////
        ////////                               创建NCreature并插入指定位置
        ////////////////////////////////////////////////////////////////////////////////////////////////

        private Control _EncounterSlots = null;

        private void CreateAllyNode()
        {
            var allies = Data.CombatState.Allies;
            foreach(var ally in allies)
            {
                var ncreature = NCreature.Create(ally);
                _AllyCreatureNodes.Add(ncreature);
            }

            PositionAlly();
        }

        private void PositionAlly()
        {
            foreach(var ally in _AllyCreatureNodes)
                _AllyContainer.AddChild(ally);
        }

        private void CreateEnemyNode()
        {
            var enemies = Data.CombatState.Enemies;
            foreach (var enemy in enemies)
            {
                var ncreature = NCreature.Create(enemy);
                _EnemyCreatureNodes.Add(ncreature);
            }

            if (this.Data.Encounter != null)
            {
                _EncounterSlots = this.Data.Encounter.CreateEncounterSlots();
                if (_EncounterSlots != null)
                    _EnemyContainer.AddChild(_EncounterSlots);
            }

            //使用了标记位置
            if (_EncounterSlots != null)
            {
                foreach (var enemy in _EnemyCreatureNodes)
                {
                    _EncounterSlots.AddChild(enemy);

                    var slot = _EncounterSlots.GetNode<Marker2D>(enemy.Data.SlotName);
                    if (slot != null)
                        enemy.GlobalPosition = slot.GlobalPosition;
                }
            }
            else
            {
                foreach (var enemy in _EnemyCreatureNodes)
                {
                    _EnemyContainer.AddChild(enemy);
                }
            }
        }
    }
}