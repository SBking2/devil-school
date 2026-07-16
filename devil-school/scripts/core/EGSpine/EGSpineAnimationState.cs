
using Godot;
using System.Collections.Generic;

namespace EGame
{
    public class EGSpineAnimationState : EGSpineBinding
    {
        public EGSpineAnimationState(Variant obj) : base(obj)
        {
        }
        protected override string SpineClassName => "SpineAnimationState";
        protected override IEnumerable<string> MethodNames => new string[] { "set_animation", "add_animation" };

        /// <summary>
        /// 播放动画
        /// </summary>
        /// <param name="track_id">动画轨道编号</param>
        /// <returns></returns>
        public EGSpineTrackEntry SetAnimation(string name, bool loop = false, int track_id = 0)
        {
            Variant? obj = Call("set_animation", name, loop, track_id);

            if (obj.HasValue == false || obj.Value.AsGodotObject() == null)
                return null;
            
            return new EGSpineTrackEntry(obj.Value);
        }

        public EGSpineTrackEntry AddAnimation(string name, bool loop = false, int track_id = 0, float delay = 0f)
        {
            var obj = Call("add_animation", name, delay, loop, track_id);

            if(obj.HasValue)
                return new EGSpineTrackEntry(obj.Value);

            return null;
        }
    }
}