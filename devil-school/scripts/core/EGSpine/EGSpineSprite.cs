using Godot;
using System.Collections.Generic;

namespace EGame
{
    public class EGSpineSprite : EGSpineBinding
    {
        protected override string SpineClassName => "SpineSprite";
        public EGSpineSprite(Variant obj) : base(obj) { }

        protected override IEnumerable<string> MethodNames => new string[] { "get_animation_state" };
        protected override IEnumerable<string> SignalNames => new string[] { "animation_started", "animation_interrupted", "animation_completed", "animation_ended"};

        public EGSpineAnimationState GetAnimationState()
        {
            var obj = Call("get_animation_state");
            if (obj.HasValue == false || obj.Value.AsGodotObject() == null)
                return null;
            return new EGSpineAnimationState(obj.Value);
        }

        public Error ConnectAnimationStarted(Callable callback)
        {
            return Connect("animation_started", callback);
        }

        public Error ConnectAnimationInterrupted(Callable callback)
        {
            return Connect("animation_interrupted", callback);
        }

        public Error ConnectAnimationEnded(Callable callback)
        {
            return Connect("animation_ended", callback);
        }

        public Error ConnectAnimationCompleted(Callable callback)
        {
            return Connect("animation_completed", callback);
        }
    }
}