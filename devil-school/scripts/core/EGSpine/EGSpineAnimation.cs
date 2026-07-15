
using Godot;

namespace EGame
{
    public class EGSpineAnimation : EGSpineBinding
    {
        public EGSpineAnimation(Variant obj) : base(obj)
        {
        }

        protected override string SpineClassName => "SpineAnimation";
    }
}