
using Godot;
using System;
using static EGame.Log;

namespace EGame
{
    /// <summary>
    /// 支持Spine和AnimationPlayer
    /// </summary>
    public class CreatureAnimator
    {
        private AnimationPlayer _AnimPlayer;

        private AnimState _CurrentState;

        private readonly AnimState _AnyState;   //CallTrigger的时候优先查这个状态
        private Log.Logger _Logger = new Log.Logger(Log.LogType.World);

        public CreatureAnimator(AnimState init_state)
        {
            _CurrentState = init_state;
            _AnyState = new AnimState("any");
            PlayAnimation(_CurrentState);
        }

        public void SetPlayer(AnimationPlayer player)
        {
            if (_AnimPlayer != null)
                throw new InvalidOperationException("Creature Animator already has animation-player!");

            _AnimPlayer = player;
            _AnimPlayer.AnimationFinished += OnAnimPlayerCompleted;
        }

        public void AddAnyBranch(string trigger, AnimState state, Func<bool> condition = null)
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
            if(state == null)
                return;

            if (TryGetAnimation(state, out Animation anim) == false)
                return;

            _CurrentState = state;
            _AnimPlayer.Play(_CurrentState.ID, _CurrentState.MixDuration);
            anim.LoopMode = _CurrentState.IsLoop ? Animation.LoopModeEnum.Linear : Animation.LoopModeEnum.None;

            //递归添加下一状态
            if (_CurrentState.NextState != null)
                AddNextAnimation(state.NextState);
        }
        
        private void AddNextAnimation(AnimState state)
        {
            if(state == null)
                return;

            if (TryGetAnimation(state, out Animation anim) == false)
                return;

            _AnimPlayer.Queue(state.ID);
            anim.LoopMode = state.IsLoop ? Animation.LoopModeEnum.Linear : Animation.LoopModeEnum.None;

            //递归添加下一状态
            if (state.NextState != null)
                AddNextAnimation(state.NextState);
        }

        private bool TryGetAnimation(AnimState state, out Animation anim)
        {
            anim = null;

            if(_AnimPlayer == null || state == null)
                return false;
            
            if(_AnimPlayer.HasAnimation(state.ID) == false)
            {
                _Logger.Warn($"AnimationPlayer missing animation: {state.ID}");
                return false;
            }

            anim = _AnimPlayer.GetAnimation(state.ID);
            if(anim == null)
            {
                _Logger.Warn($"AnimationPlayer animation is null: {state.ID}");
                return false;
            }

            return true;
        }

        private void OnAnimPlayerCompleted(StringName anim_name)
        {
            if(_CurrentState.IsLoop == false && _CurrentState.NextState != null)
            {
                _CurrentState = _CurrentState.NextState;
            }
        }
    }
}
