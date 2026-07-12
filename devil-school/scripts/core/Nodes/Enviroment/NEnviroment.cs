
using System.Collections.Generic;
using Godot;

namespace EGame
{
    public partial class NEnviroment : Control
    {
        private static readonly string _NENVIROMENT_PATH = "enviroments/enviroment";
        public static NEnviroment Instance => NRun.Instance.EnviromentNode;
        public static NEnviroment Create(Enviroment data)
        {
            var n_enviroment = SceneHelper.LoadScene<NEnviroment>(_NENVIROMENT_PATH);
            n_enviroment.Data = data;
            return n_enviroment;
        }

        public Enviroment Data { get; private set; }

        public override void _Ready()
        {
            base._Ready();

            _WorldParent = GetNode<Node2D>("%WorldParent");
            _CreatureParent = GetNode<Node2D>("%CreatureParent");

            CreateWorld();

            //创建Creature
            var players = Data.EnvState.Players;
            foreach (var player in players)
                CreateCreature(player.Creature);

            AddController(_NCreatures[0]);
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////////
        ///////                                 场景管理
        /////////////////////////////////////////////////////////////////////////////////////////////////////////

        private Node2D _WorldParent;
        private void CreateWorld()
        {
            //创建场景
            var scene = Data.WorldModel.CreateWorld();
            _WorldParent.AddChild(scene);
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////////
        ///////                                 Creature管理
        /////////////////////////////////////////////////////////////////////////////////////////////////////////

        private Node2D _CreatureParent;

        private List<NEnvCreature> _NCreatures = new List<NEnvCreature>();

        private void CreateCreature(Creature creature)
        {
            var n_env_creature = NEnvCreature.Create(creature);
            _NCreatures.Add(n_env_creature);
            _CreatureParent.AddChild(n_env_creature);
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////////
        ///////                              Input Controller
        /////////////////////////////////////////////////////////////////////////////////////////////////////////

        private NEnviromentController _InputContorller;

        public void AddController(NEnvCreature controlled)
        {
            if (GodotObject.IsInstanceValid(_InputContorller))
                return;
            
            _InputContorller = NEnviromentController.Create(controlled);
            this.AddChild(_InputContorller);
        }
        
        public void RemoveController()
        {
            if (GodotObject.IsInstanceValid(_InputContorller) == false)
                return;

            _InputContorller.QueueFree();
            _InputContorller = null;
        }
    }
}