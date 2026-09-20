
using Godot;

namespace EGame
{
    public partial class NFailurePanel : NAbstractPanel
    {
        public override void _Input(InputEvent @event)
        {
            base._Input(@event);
            if (!Visible)
                return;

            if (@event is InputEventKey e && e.Keycode == Key.R && e.IsPressed())
            {
                Restart();
            }
        }

        private void Restart()
        {
            var player = NGame.Instance.PlayerNode;
            player.Data.HP = player.Data.MaxHP;
            player.GlobalPosition = Vector3.Zero;
            
            UIManager.Instance.Hide(UIPanelType.FailurePanel);
        }
    }
}