
using Godot;
using System;

namespace EGame
{
	public partial class NEnvCreature : CharacterBody3D
	{
		private const string N_ENV_CREATURE_PATH = "enviroments/envcreature";
		public Creature Data { get; private set; }

		private NCreatureVisual _Visual;

		private CreatureAnimator _Animator;

		private Node3D _VisualParent;

		public static NEnvCreature Create(Creature data)
		{
			var instance = SceneHelper.LoadScene<NEnvCreature>(N_ENV_CREATURE_PATH);
			instance.Data = data;
			return instance;
		}
		
		public override void _Ready()
		{
			base._Ready();
			
			_VisualParent = GetNode<Node3D>("%VisualParent");

			//创建Visual
			GenerateVisual();
			GenerateAnimator();
		}

		private void GenerateVisual()
		{
			if (_Visual != null)
				throw new InvalidOperationException($"{Name} already has CreatureVisual!");

			if (Data.IsPlayer)
				_Visual = Data.Player.Character.CreateVisual();
			else
				_Visual = Data.MonsterModel.CreateVisual();

			if (_Visual != null)
			{
				var parent = _VisualParent == null ? this : _VisualParent;
				parent.AddChild(_Visual);
				parent.MoveChild(_Visual, 0);
			}
		}

		private void GenerateAnimator()
		{
			if (_Visual != null)
			{
				if (_Visual.AnimPlayer != null)
				{	
					if (Data.IsPlayer == false)
						_Animator = Data.MonsterModel.CreateAnimator(_Visual.AnimPlayer);
					else
						_Animator = Data.Player.Character.CreateAnimator(_Visual.AnimPlayer);
				}
			}
		}

		/////////////////////////////////////////////////////////////////////////////////////////////////////////
		///////                                 移动相关
		/////////////////////////////////////////////////////////////////////////////////////////////////////////

		public Vector3 TargetMoveDir { get; private set; }

		public void SetMoveDir(Vector3 dir)
		{
			TargetMoveDir = dir;
			
            if (TargetMoveDir.Length() > 0.1f)
                _Animator.CallTrigger("walk");
            else
                _Animator.CallTrigger("idle");
        }

		public override void _PhysicsProcess(double delta)
		{
			base._PhysicsProcess(delta);
			ProcessMove();
			ProcessRotation((float)delta);
        }

		private void ProcessMove()
		{
			var move = TargetMoveDir;
			Velocity = move.Normalized() * Data.Player.Character.MoveSpeed;
			MoveAndSlide();
		}

		private void ProcessRotation(float delta)
		{
			if(TargetMoveDir != Vector3.Zero)
			{
                var basis = Basis.LookingAt(-TargetMoveDir, Vector3.Up);
                Quaternion = Quaternion.Slerp(basis.GetRotationQuaternion(), delta * 5.0f);
            }
		}
	}
}
