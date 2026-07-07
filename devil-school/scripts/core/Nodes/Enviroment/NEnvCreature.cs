
using Godot;

namespace EGame
{
    public partial class NEnvCreature : Node2D
    {
        private static readonly string N_ENV_CREATURE_PATH = "enviroment/n_env_creature";
        public Creature Data { get; private set; }

        private NCreatureVisual _Visual;

        public static NEnvCreature Create(Creature data)
        {
            var instance = SceneHelper.LoadScene<NEnvCreature>(N_ENV_CREATURE_PATH);
            instance.Data = data;
            instance._Visual = data.CreateVisuals();
            return instance;
        }

        public override void _Ready()
        {
            base._Ready();
        }
    }
}