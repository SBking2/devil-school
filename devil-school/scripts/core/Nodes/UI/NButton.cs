
using Godot;
using System;

namespace EGame
{
    public partial class NButton : NClickableControl
    {
        public event Action<NButton> OnPressedAction;
        public event Action<NButton> OnReleasedAction;
        public event Action<NButton> OnFocusedAction;
        public event Action<NButton> OnUnFocusedAction;

        protected virtual string[] HotKeys => Array.Empty<string>();
        protected virtual string ControllerIconHotKey
        {
            get
            {
                if (HotKeys.Length == 0)
                    return null;
                return HotKeys[0];
            }
        }

        public override void _Input(InputEvent @event)
        {
            base._Input(@event);
            CheckDragThreshold(@event);
        }

        protected override void OnPressed()
        {
            base.OnPressed();
            OnPressedAction?.Invoke(this);
        }

        protected override void OnReleased()
        {
            base.OnReleased();
            OnReleasedAction?.Invoke(this);
        }

        protected override void OnFocused()
        {
            base.OnFocused();
            OnFocusedAction?.Invoke(this);
        }

        protected override void OnUnFocused()
        {
            base.OnUnFocused();
            OnUnFocusedAction?.Invoke(this);
        }
    }
}