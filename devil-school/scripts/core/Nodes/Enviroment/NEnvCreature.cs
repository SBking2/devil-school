
using Godot;

namespace EGame
{
    public partial class NEnvCreature : CharacterBody2D
    {
        private static readonly string N_ENV_CREATURE_PATH = "enviroment/envcreature";
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

            _Visual = Data.CreateVisuals();
            if (_Visual != null)
            {
                AddChild(_Visual);
                MoveChild(_Visual, 0);
            }
        }

        public override void _PhysicsProcess(double delta)
        {
            base._PhysicsProcess(delta);
            ProcessMove();
        }

        private void ProcessMove()
        {
            var move = GetMoveDir();
            Velocity = move.Normalized() * Data.Player.Character.MoveSpeed;
            MoveAndSlide();
        }

        private Vector2 GetMoveDir()
        {
            var dir = Vector2.Zero;

            if (Input.IsActionPressed(MegaInput.UP))
                dir.Y -= 1f;

            if (Input.IsActionPressed(MegaInput.DOWN))
                dir.Y += 1f;

            if (Input.IsActionPressed(MegaInput.RIGHT))
                dir.X += 1f;

            if (Input.IsActionPressed(MegaInput.LEFT))
                dir.X -= 1f;

            return dir;
        }

        public void AddPlayerController()
        {
        }

        public void RemovePlayerController()
        {
        }
    }
}