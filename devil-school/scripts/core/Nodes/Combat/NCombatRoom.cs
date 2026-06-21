
using Godot;
using System.Collections.Generic;

namespace EGame
{
    public partial class NCombatRoom : Control
    {
        private static readonly string COMBAT_SCENE_PATH = "combat/combat_room";
        public CombatRoom Data { get; private set; }

        private Control _SceneContainer;
        private Control _AllyContainer;
        private Control _EnemyContainer;
        
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

            CreateAllCreatureNode();
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////
        ////////                               Creature
        ////////////////////////////////////////////////////////////////////////////////////////////////

        private List<NCreature> _CreatureNodes = new List<NCreature>();
        private void CreateAllCreatureNode()
        {
            foreach(var creature in Data.CombatState.Creatures)
                AddCreature(creature);
        }
        private void AddCreature(Creature creature)
        {
            var n_creature = NCreature.Create(creature);
            _CreatureNodes.Add(n_creature);

            if (creature.IsPlayer)
                _AllyContainer.AddChild(n_creature);
            else
                _EnemyContainer.AddChild(n_creature);
        }
    }
}