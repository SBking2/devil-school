
using Godot;

namespace EGame
{
    public partial class NFollowCamera : Camera2D
    {
        private NEnvCreature _Target;

        public static NFollowCamera Create(NEnvCreature target)
        {
            var instance = new NFollowCamera();
            instance._Target = target;
            return instance;
        }

        private float _LerpSpeed = 5.0f;

        public override void _Process(double delta)
        {
            base._Process(delta);

            if(_Target != null)
            {
                var target_pos = _Target.GlobalPosition;
                this.GlobalPosition = this.GlobalPosition.Lerp(target_pos, (float)(delta * _LerpSpeed));
            }
        }
    }
}