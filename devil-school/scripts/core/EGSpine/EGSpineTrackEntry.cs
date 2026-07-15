
using Godot;

namespace EGame
{
    public class EGSpineTrackEntry : EGSpineBinding
    {
        public EGSpineTrackEntry(Variant obj) : base(obj)
        {
        }

        protected override string SpineClassName => "SpineTrackEntry";
    }
}