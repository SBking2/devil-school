
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
        private Vector2 _BeginDragPos = Vector2.Zero;
        
        [Export(PropertyHint.None, "拖动距离阈值")] 
        protected float _DragThreshold = -1.0f;
        
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
            if(IsFocus)
            {
                PressHandler();
            }
        }

        private void OnMouseReleasedHandler(InputEventMouseButton _event)
        {
            if(IsFocus)
            {
                OnReleased();
            }    
        }

        private void PressHandler()
        {
            _IsPress = true;
            OnPressed();
        }

        private void ReleaseHanlder()
        {
            if(_IsPress)
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

        protected void CheckDragThreshold(InputEvent input_event)
        {
            if(_DragThreshold > 0)
            {
                if(_IsPress && input_event is InputEventMouseMotion motion_event && _BeginDragPos.DistanceTo(motion_event.GlobalPosition) >= _DragThreshold)
                    _IsPress = false;
            }
        }

        public bool IsFocus { get; private set; }

        protected virtual void OnPressed()
        {
            Logger.VeryDebug($"{Name} is OnPressed!");
        }
        
        protected virtual void OnReleased()
        {
            Logger.VeryDebug($"{Name} is OnReleased!");
        }

        protected virtual void OnFocused()
        {
            Logger.VeryDebug($"{Name} is OnFocused!");
        }

        protected virtual void OnUnFocused()
        {
            Logger.VeryDebug($"{Name} is OnUnFocused!");
        }
    }
}