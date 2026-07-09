
using Godot;

namespace EGame
{
	public partial class Example : Node
	{
        public override void _Process(double delta)
        {
            base._Process(delta);
            if (Input.IsActionPressed(EGInput.UP))
                Logger.Debug($"Pressed {EGInput.UP}");
        }
	}
}
