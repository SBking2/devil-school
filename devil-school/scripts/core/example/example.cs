
using Godot;

namespace EGame
{
	public partial class Example : Node
	{
        public override void _Process(double delta)
        {
            base._Process(delta);
            if (Input.IsActionPressed(MegaInput.UP))
                Logger.Debug($"Pressed {MegaInput.UP}");
        }
	}
}
