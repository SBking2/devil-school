
using Godot;

namespace EGame
{
    public partial class NMonsterSpwanPoint : Node3D
    {
        [Export] public string MonsterID;
        [Export] public int CombatSide;

        public override void _Ready()
        {
            base._Ready();

            var model = ModelDB.Monster(MonsterID);
            var creature = new Creature(model.MutableClone() as MonsterModel, (CombatSide)CombatSide, "");

            //创建NEnvCreature在这个地方
            var n_creature = NEnvCreature.Create(creature);
            NEnviroment.Instance.AddMonsterCreature(n_creature);
            n_creature.GlobalPosition = this.GlobalPosition;
            n_creature.Quaternion = this.Quaternion;
        }
    }
}