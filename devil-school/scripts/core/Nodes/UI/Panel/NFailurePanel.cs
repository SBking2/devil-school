
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
            NGame.Instance.RebornPlayer();   
            UIManager.Instance.Hide(UIPanelType.FailurePanel);
        }
    }
}