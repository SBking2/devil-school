
using Godot;

namespace EGame
{
    public class EGSpineAnimationState : EGSpineBinding
    {
        public EGSpineAnimationState(Variant obj) : base(obj)
        {
        }

        protected override string SpineClassName => "SpineAnimationState";
    }
}