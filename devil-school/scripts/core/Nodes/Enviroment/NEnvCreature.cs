
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

		private Node3D _SensorParent;

		private CollisionShape3D _MoveCollider;

		private CapsuleShape3D _CapsuleShape;

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
            _SensorParent = GetNode<Node3D>("%SensorParent");

			_MoveCollider = GetNode<CollisionShape3D>("MoveCollider");
			_CapsuleShape = (CapsuleShape3D)_MoveCollider.Shape;

			//创建Visual
			GenerateVisual();
			GenerateAnimator();

			Data.SetUpForWorld(this);
		}

		private void GenerateVisual()
		{
			if (_Visual != null)
				throw new InvalidOperationException($"{Name} already has CreatureVisual!");

            _Visual = Data.CreateVisual();

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
					_Animator = Data.CreateAnimator(_Visual.AnimPlayer);
                }
			}
		}

		/////////////////////////////////////////////////////////////////////////////////////////////////////////
		///////                                 移动相关
		/////////////////////////////////////////////////////////////////////////////////////////////////////////

		public MovementIntent Intent { get; } = new MovementIntent();

		public void SetAnimTrigger(string trigger)
		{
			_Animator?.CallTrigger(trigger);
		}

		public override void _PhysicsProcess(double delta)
		{
			base._PhysicsProcess(delta);
			UpdateGravity(delta);
			Data.OnWorldPhysicalProcess(delta);
        }

		/////////////////////////////////////////////////////////////////////////////////////////////////////////
		///////                                 碰撞体相关
		/////////////////////////////////////////////////////////////////////////////////////////////////////////

		// 通用的胶囊体高度读写接口，谁需要改身高（比如蹲下状态）自己调用，NEnvCreature 不关心"蹲下"这个概念本身
		public float ColliderHeight
		{
			get => _CapsuleShape.Height;
			set => _CapsuleShape.Height = value;
		}

		/////////////////////////////////////////////////////////////////////////////////////////////////////////
		///////                                 重力相关
		/////////////////////////////////////////////////////////////////////////////////////////////////////////

		// 贴地时给一个很小的向下速度而不是直接清零，这样下一帧 IsOnFloor() 才能持续判定为真，避免在台阶/斜坡交界处反复起跳
		private const float GROUND_STICK_SPEED = -0.5f;

		private static readonly float _Gravity = (float)ProjectSettings.GetSetting("physics/3d/default_gravity");

		public bool IsGround { get; private set; }

		private void UpdateGravity(double delta)
		{
			IsGround = IsOnFloor();

			var velocity = Velocity;
			if (IsGround)
			{
				if (velocity.Y < 0f)
					velocity.Y = GROUND_STICK_SPEED;
			}
			else
			{
				velocity.Y -= _Gravity * (float)delta;
			}

			Velocity = velocity;
		}

        public override void _Process(double delta)
        {
            base._Process(delta);
			Data.OnWolrdProcess(delta);
        }

		public void SetVisualParent(Node3D parent)
		{
			if(_Visual != null)
			{
				_VisualParent.RemoveChild(_Visual);
				parent.AddChild(_Visual);
				_Visual.Position = Vector3.Zero;
				_Visual.Quaternion = Quaternion.Identity;
			}
		}

        /////////////////////////////////////////////////////////////////////////////////////////////////////////
        ///////                                 敌人AI
        /////////////////////////////////////////////////////////////////////////////////////////////////////////
		
		public void AddSensor(INSensor sensor)
		{
			if (Data.IsPlayer == true)
				throw new InvalidOperationException("Player doesn't need sensor!");

			sensor.Bind(_SensorParent);
		}
    }
}
