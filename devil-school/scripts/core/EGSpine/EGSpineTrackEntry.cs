
using Godot;
using System.Collections.Generic;

namespace EGame
{
    public class EGSpineTrackEntry : EGSpineBinding
    {
        public EGSpineTrackEntry(Variant obj) : base(obj)
        {
        }

        protected override string SpineClassName => "SpineTrackEntry";
        protected override IEnumerable<string> MethodNames => new string[] { "set_mix_duration" };

        public void SetMixDuration(float time)
        {
            Call("set_mix_duration", time);
        }
    }
}