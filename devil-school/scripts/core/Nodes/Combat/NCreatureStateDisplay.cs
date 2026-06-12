using Godot;
namespace EGame
{
    public partial class NCreatureStateDisplay : Control
    {
        private NHealthBar _HealthBar;
        public override void _Ready()
        {
            base._Ready();
            _HealthBar = GetNode<NHealthBar>("%HealthBar");
        }
    }
}