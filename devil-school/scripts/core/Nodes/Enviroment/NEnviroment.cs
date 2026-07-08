
using Godot;

namespace EGame
{
    public partial class NEnviroment : Control
    {
        public Enviroment Data { get; private set; }
        public static NEnviroment Create(Enviroment data)
        {
            var n_enviroment = data.EnviromentModel.CreateEnviroment() as NEnviroment;
            n_enviroment.Data = data;
            return n_enviroment;
        }

        public override void _Ready()
        {
            base._Ready();

            //TODO:初始化组件

            var players = Data.EnvState.Players;
            foreach (var player in players)
                CreateCreature(player.Creature);
        }

        private void CreateCreature(Creature creature)
        {
            var n_env_creature = NEnvCreature.Create(creature);
            //TODO:放到特定的地点
        }
    }
}