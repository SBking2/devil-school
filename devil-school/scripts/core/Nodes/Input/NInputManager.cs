
using Godot;
using System.Collections.Generic;

namespace EGame
{
    public partial class NInputManager : Node
    {
        private static Dictionary<StringName, Key> _KeyMap = new Dictionary<StringName, Key>()
        {
            { EGInput.UP,               Key.W     },
            { EGInput.DOWN,             Key.S     },
            { EGInput.LEFT,             Key.A     },
            { EGInput.RIGHT,            Key.D     },
            { EGInput.CROUCH,           Key.Ctrl  },
            { EGInput.RUN,              Key.Shift },
            { EGInput.JUMP,             Key.Space },
            { EGInput.EXIT,             Key.Escape },
        };
        
        private void ProcessKeyInput(InputEvent e)
        {
            if(e is InputEventKey key_e)
            {
                foreach(var item in _KeyMap)
                {
                    if(item.Value == key_e.Keycode)
                    {
                        var input_action = new InputEventAction
                        {
                            Action = item.Key,
                            Pressed = key_e.IsPressed()
                        };
                        Input.ParseInputEvent(input_action);
                    }
                }
            }
        }

        public override void _UnhandledInput(InputEvent @event)
        {
            ProcessKeyInput(@event);
        }
    }
}