
using Godot;

namespace EGame
{
    public partial class NEnvCreature : CharacterBody2D
    {
        private const string N_ENV_CREATURE_PATH = "enviroments/envcreature";
        public Creature Data { get; private set; }

        private NCreatureVisual _Visual;

        private Node2D _VisualParent;

        private CollisionShape2D _CollisionShape;

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
            
            _VisualParent = GetNode<Node2D>("%VisualParent");
            _CollisionShape = GetNode<CollisionShape2D>("%CollisionShape");

            //创建Visual
            _Visual = Data.CreateVisuals();
            if (_Visual != null)
            {
                var parent = _VisualParent == null ? this : _VisualParent;
                parent.AddChild(_Visual);
                parent.MoveChild(_Visual, 0);
            }
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////////
        ///////                                 移动相关
        /////////////////////////////////////////////////////////////////////////////////////////////////////////

        public Vector2 TargetMoveDir { get; private set; }

        public void SetMoveDir(Vector2 dir)
        {
            TargetMoveDir = dir;
        }

        public override void _PhysicsProcess(double delta)
        {
            base._PhysicsProcess(delta);
            ProcessMove();
        }

        private void ProcessMove()
        {
            var move = TargetMoveDir;
            Velocity = move.Normalized() * Data.Player.Character.MoveSpeed;
            MoveAndSlide();
        }
    }
}