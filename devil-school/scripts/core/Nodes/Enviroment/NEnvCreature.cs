
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

			// 场景里内嵌的 Shape 资源默认是所有实例共享的（没勾 Local to Scene），
			// 这里 Duplicate 一份专属自己的，不然一个生物蹲下会连带改到其他生物的碰撞体高度
			_CapsuleShape = (CapsuleShape3D)_MoveCollider.Shape.Duplicate();
			_MoveCollider.Shape = _CapsuleShape;
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
		private const float CROUCH_HEIGHT_CHANGE_SPEED = 12.0f;

		// 站立时视角相对Origin的高度偏移，摄像机就读这个，不再是写死的常量
		private const float EYE_OFFSET_FROM_HEAD_TOP = 0.2f;

		private float _StandHeight;
		public bool IsCrouching { get; private set; }

		// 胶囊体围绕 Origin 对称，头顶相对 Origin 的距离永远是 ColliderHeight/2，
		// 直接拿这个算，不依赖"身体这一帧有没有精确下沉到位"，就不会因为漏掉某一帧的补偿而累积误差
		public float EyeOffset => ColliderHeight / 2f - EYE_OFFSET_FROM_HEAD_TOP;

		private void UpdateCrouch(double delta)
		{
			// 空中也允许蹲下（比如缩小体积做空中动作）；已经蹲着的时候（哪怕脚离地了）如果头顶没地方站，也不能强行弹起来
			bool already_crouched = !Mathf.IsEqualApprox(ColliderHeight, _StandHeight);
			bool should_crouch = Intent.WantsCrouch || (already_crouched && !CanStandUp());
			float target_height = should_crouch ? CROUCH_HEIGHT : _StandHeight;
			float cur_height = ColliderHeight;

			if (Mathf.IsEqualApprox(cur_height, target_height) == false)
			{
				float max_delta = CROUCH_HEIGHT_CHANGE_SPEED * (float)delta;
				float new_height = Mathf.MoveToward(cur_height, target_height, max_delta);
				float height_delta = new_height - cur_height;

				ColliderHeight = new_height;
				if (height_delta < 0f && IsGround)
				{
					var velocity = Velocity;
					velocity.Y = Mathf.Min(velocity.Y, height_delta / 2f / (float)delta);
					Velocity = velocity;
				}
			}

			bool was_crouching = IsCrouching;
			IsCrouching = Mathf.IsEqualApprox(ColliderHeight, CROUCH_HEIGHT);
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

		private static readonly float _Gravity = 15f;

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
