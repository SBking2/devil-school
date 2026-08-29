
using Godot;

namespace EGame
{
    public partial class MonsterSpawnPoint : Node3D
    {
        [Export] private string MonsterName;

        public override void _Ready()
        {
            base._Ready();
            var model = ModelDB.Monster(MonsterName);
            if(model == null)
            {
                Log.Warn($"Unknown Monster : {MonsterName}!");
                return;
            }

            var monster = NAgent.Create(model.MutableClone() as MonsterModel);
            AddChild(monster);

            monster.Position = this.Position;
            monster.Quaternion = this.Quaternion;
        }
    }
}