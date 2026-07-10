
using System.Collections.Generic;
using Godot;

namespace EGame
{
    public partial class NEnviroment : Control
    {
        private static readonly string _NENVIROMENT_PATH = "enviroments/enviroment";
        public Enviroment Data { get; private set; }

        private Node2D _CreatureParent;

        private Node2D _WorldParent;

        private List<NEnvCreature> _NCreatures = new List<NEnvCreature>();

        public static NEnviroment Create(Enviroment data)
        {
            var n_enviroment = SceneHelper.LoadScene<NEnviroment>(_NENVIROMENT_PATH);
            n_enviroment.Data = data;
            return n_enviroment;
        }
        
        public override void _Ready()
        {
            base._Ready();

            _WorldParent = GetNode<Node2D>("%WorldParent");
            _CreatureParent = GetNode<Node2D>("%CreatureParent");

            //创建场景
            var scene = Data.WorldModel.CreateWorld();
            _WorldParent.AddChild(scene);

            //创建Creature
            var players = Data.EnvState.Players;
            foreach (var player in players)
                CreateCreature(player.Creature);
        }

        private void CreateCreature(Creature creature)
        {
            var n_env_creature = NEnvCreature.Create(creature);
            _NCreatures.Add(n_env_creature);
            _CreatureParent.AddChild(n_env_creature);
        }
    }
}