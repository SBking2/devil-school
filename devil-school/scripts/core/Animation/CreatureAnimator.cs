
using Godot;
using System;

namespace EGame
{
    public class CreatureAnimator
    {
        private readonly EGSpineSprite _SpineController;
        private AnimState _CurrentState;

        private readonly AnimState _AnyState;   //CallTrigger的时候优先查这个状态

        public CreatureAnimator(EGSpineSprite owner, AnimState init_state)
        {
            _SpineController = owner;
            _CurrentState = init_state;
            _AnyState = new AnimState("any");

            _SpineController.ConnectAnimationCompleted(Callable.From<GodotObject, GodotObject, GodotObject>(OnAnimationCompleted));
            PlayAnimation(_CurrentState);
        }

        public void AddAnyBrach(string trigger, AnimState state, Func<bool> condition = null)
        {
            _AnyState.AddBranch(trigger, state, condition);
        }

        public void CallTrigger(string trigger)
        {
            var anim = _AnyState.CallTrigger(trigger);
            if(anim == null)
                anim = _CurrentState.CallTrigger(trigger);
            
            if (anim != null)
                PlayAnimation(anim);
        }

        private void PlayAnimation(AnimState state)
        {
            _CurrentState = state;
            var anim_state = _SpineController.GetAnimationState();
            anim_state.SetAnimation(_CurrentState.ID, _CurrentState.IsLoop);

            //递归添加下一状态
            if (_CurrentState.NextState != null)
                AddNextAnimation(state.NextState);
        }
        
        private void AddNextAnimation(AnimState state)
        {
            var anim_state = _SpineController.GetAnimationState();
            anim_state.AddAnimation(state.ID, state.IsLoop);

            //递归添加下一状态
            if(state.NextState != null)
                AddNextAnimation(state.NextState);
        }

        /// <summary>
        /// 动画由SpineSprite继续播，但是状态得手动更新
        /// </summary>
        private void OnAnimationCompleted(GodotObject _, GodotObject __, GodotObject ___)
        {
            if(_CurrentState.IsLoop == false && _CurrentState.NextState != null)
            {
                _CurrentState = _CurrentState.NextState;
            }
        }
    }
}