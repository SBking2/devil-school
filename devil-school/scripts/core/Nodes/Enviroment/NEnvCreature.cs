
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
			_StandHeight = _CapsuleShape.Height;

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
			UpdateCrouch(delta);
			Data.OnWorldPhysicalProcess(delta);
        }

		/////////////////////////////////////////////////////////////////////////////////////////////////////////
		///////                                 碰撞体/蹲伏相关
		/////////////////////////////////////////////////////////////////////////////////////////////////////////

		public float ColliderHeight
		{
			get => _CapsuleShape.Height;
			set => _CapsuleShape.Height = value;
		}

		//谁想知道"现在是不是蹲着"直接查 IsCrouching
		public const float CROUCH_SPEED_MULTIPLIER = 0.5f;
		private const float CROUCH_HEIGHT = 1.2f;
		private const float CROUCH_HEIGHT_CHANGE_SPEED = 8.0f;

		// 站立时视角相对Origin的高度偏移，摄像机就读这个，不再是写死的常量
		private const float STAND_EYE_OFFSET = 0.15f;

		private float _StandHeight;
		public bool IsCrouching { get; private set; }
		public float EyeOffset { get; private set; } = STAND_EYE_OFFSET;

		private void UpdateCrouch(double delta)
		{
			float target_height = (Intent.WantsCrouch || !CanStandUp()) ? CROUCH_HEIGHT : _StandHeight;

			float cur_height = ColliderHeight;
			if (Mathf.IsEqualApprox(cur_height, target_height) == false)
			{
				float max_delta = CROUCH_HEIGHT_CHANGE_SPEED * (float)delta;
				float new_height = Mathf.MoveToward(cur_height, target_height, max_delta);
				float height_delta = new_height - cur_height;

				ColliderHeight = new_height;

				// 胶囊体是围绕自身中心对称收缩的，蹲下时收缩量有一半会体现为"底部往上抬"，
				// 重力是逐帧累加的追不上这个收缩速度，不补偿的话脚底会悬空一下再被拽回地面。
				// 但这个补偿只在贴地的时候有意义——在空中根本没有地面可贴，硬加这个向下速度
				// 只会变成"蹲一下就加速下坠"，所以只在 IsGround 为真时才补偿
				if (height_delta < 0f && IsGround)
				{
					var velocity = Velocity;
					velocity.Y = Mathf.Min(velocity.Y, height_delta / 2f / (float)delta);
					Velocity = velocity;
				}
			}

			// 胶囊体围绕Origin对称收缩，头顶相对Origin掉了半个收缩量，视角跟着头顶走
			EyeOffset = STAND_EYE_OFFSET - (_StandHeight - ColliderHeight) / 2f;

			bool was_crouching = IsCrouching;
			IsCrouching = Mathf.IsEqualApprox(ColliderHeight, CROUCH_HEIGHT);

			if (IsCrouching && was_crouching == false)
				SetAnimTrigger("crouch");
			else if (IsCrouching == false && was_crouching)
				SetAnimTrigger(Intent.MoveDir.Length() > 0.1f ? (Intent.WantsRun ? "run" : "walk") : "idle");
		}

		private bool CanStandUp()
		{
			var space_state = GetWorld3D().DirectSpaceState;

			var from = GlobalPosition + Vector3.Up * CROUCH_HEIGHT;
			var to = GlobalPosition + Vector3.Up * _StandHeight;

			var query = PhysicsRayQueryParameters3D.Create(from, to);
			query.Exclude = new Godot.Collections.Array<Rid> { GetRid() };

			var result = space_state.IntersectRay(query);
			return result.Count == 0;
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
