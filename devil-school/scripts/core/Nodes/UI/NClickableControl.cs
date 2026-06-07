
using Godot;

namespace EGame
{
    /// <summary>
    /// 提供，让外部获取Prees、Focus等接口,godot原生的focus和horver是两个事件，此处要进行保证focus的对象和hoover一致
    /// </summary>
    public partial class NClickableControl : Control
    {
        [Signal] public delegate void MousePressedEventHandler(InputEvent input_event);
        [Signal] public delegate void MouseReleasedEventHandler(InputEvent input_event);

        public override void _Ready()
        {
            base._Ready();
            ConnectSignal();
        }

        protected virtual void ConnectSignal()
        {
            Connect(Control.SignalName.MouseEntered, Callable.From(OnHorveredHandler));
            Connect(Control.SignalName.MouseExited, Callable.From(OnUnHorveredHandler));

            Connect(Control.SignalName.FocusEntered, Callable.From(OnFocusedHandler));
            Connect(Control.SignalName.FocusExited, Callable.From(OnUnFocusedHandler));

            Connect(SignalName.MousePressed, Callable.From<InputEventMouseButton>(OnMousePressedHandler));
            Connect(SignalName.MouseReleased, Callable.From<InputEventMouseButton>(OnMouseReleasedHandler));
        }

        /////////////////////////////////////////////////////////////////////////
        //////                     监听Godot原生信号
        /////////////////////////////////////////////////////////////////////////

        private bool _IsControlFocus = false;
        private bool _IsControlHorver = false;
        private bool _IsPress = false;
        
        private void OnHorveredHandler()
        {
            _IsControlHorver = true;
            RefreshFocus();
        }

        private void OnUnHorveredHandler()
        {
            _IsControlHorver = false;
            RefreshFocus();
        }

        private void OnFocusedHandler()
        {
            _IsControlFocus = true;
            RefreshFocus();
        }

        private void OnUnFocusedHandler()
        {
            _IsControlFocus = false;
            RefreshFocus();
        }

        private void OnMousePressedHandler(InputEventMouseButton _event)
        {
            if(IsFocus && _event.ButtonIndex == MouseButton.Left)
            {
                _IsPress = true;
                OnPressed();
            }
        }

        private void OnMouseReleasedHandler(InputEventMouseButton _event)
        {
            if(IsFocus && _event.ButtonIndex == MouseButton.Left)
            {
                _IsPress = false;
                OnReleased();
            }    
        }

        private void RefreshFocus()
        {
            bool is_focus = _IsControlFocus || _IsControlHorver;
            if(IsFocus != is_focus)
            {
                IsFocus = is_focus;
                if (IsFocus)
                    OnFocused();
                else
                    OnUnFocused();
            }
        }

        public override void _GuiInput(InputEvent @event)
        {
            base._GuiInput(@event);

            if(@event is InputEventMouseButton mouse_event)
            {
                EmitSignal(mouse_event.IsPressed() ? SignalName.MousePressed : SignalName.MouseReleased, @event);
            }
        }

        /////////////////////////////////////////////////////////////////////////
        //////                      开放给项目使用
        /////////////////////////////////////////////////////////////////////////

        public bool IsFocus { get; private set; }

        protected virtual void OnPressed()
        {
            Logger.VeyDebug($"{Name} is OnPressed!");
        }
        
        protected virtual void OnReleased()
        {
            Logger.VeyDebug($"{Name} is OnReleased!");
        }

        protected virtual void OnFocused()
        {
            Logger.VeyDebug($"{Name} is OnFocused!");
        }

        protected virtual void OnUnFocused()
        {
            Logger.VeyDebug($"{Name} is OnUnFocused!");
        }
    }
}