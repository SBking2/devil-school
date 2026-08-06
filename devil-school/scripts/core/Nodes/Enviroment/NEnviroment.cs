
using System.Collections.Generic;
using Godot;

namespace EGame
{
    public partial class NEnviroment : Node3D
    {
        private const string _NENVIROMENT_PATH = "enviroments/enviroment";
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

            _WorldParent = GetNode<Node3D>("%WorldParent");
            _CreatureParent = GetNode<Node3D>("%CreatureParent");

            CreateWorld();
            
            //创建Creature
            var players = Data.EnvState.Players;
            foreach (var player in players)
                CreateCreature(player.Creature);
            
            AddController(_NCreatures[0]);
            NRun.Instance.CameraController.SetCamera(NThirdPersonCamera.Create(_NCreatures[0]));
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////////
        ///////                                 场景管理
        /////////////////////////////////////////////////////////////////////////////////////////////////////////

        private Node3D _WorldParent;
        private Node3D _PlayerStartPoint;
        private void CreateWorld()
        {
            //创建场景
            var scene = Data.WorldModel.CreateWorld();
            _WorldParent.AddChild(scene);
            _PlayerStartPoint = scene.GetNode<Node3D>("%PlayerStartPoint");
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////////
        ///////                                 Creature管理
        /////////////////////////////////////////////////////////////////////////////////////////////////////////

        private Node3D _CreatureParent;

        private List<NEnvCreature> _NCreatures = new List<NEnvCreature>();

        private void CreateCreature(Creature creature)
        {
            var n_env_creature = NEnvCreature.Create(creature);
            _NCreatures.Add(n_env_creature);
            _CreatureParent.AddChild(n_env_creature);

            if (_PlayerStartPoint != null)
                n_env_creature.GlobalPosition = _PlayerStartPoint.GlobalPosition;

            var model_id = creature.IsPlayer ? creature.Player.Character.ID : creature.MonsterModel.ID;
        }

        public void AddMonsterCreature(NEnvCreature creature)
        {
            _CreatureParent.AddChild(creature);
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////////
        ///////                              Input Controller
        /////////////////////////////////////////////////////////////////////////////////////////////////////////

        private NEnviromentInput _EnviromentInput;
        
        public void AddController(NEnvCreature controlled)
        {
            if (_EnviromentInput != null)
                return;

            _EnviromentInput = NEnviromentInput.Create(controlled);
            this.AddChild(_EnviromentInput);
        }
        
        public void RemoveController()
        {
            if (_EnviromentInput == null)
                return;

            _EnviromentInput.QueueFree();
            _EnviromentInput = null;
        }
    }
}
