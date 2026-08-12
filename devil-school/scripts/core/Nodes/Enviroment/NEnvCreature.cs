
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

		private Vector3 _TargetMoveDir = Vector3.Zero;
		public Vector3 TargetMoveDir
		{
			get
			{
				return _TargetMoveDir;
			}

			set
			{
                _TargetMoveDir = value;
            }
		}

		public void SetAnimTrigger(string trigger)
		{
			_Animator?.CallTrigger(trigger);
		}

		public override void _PhysicsProcess(double delta)
		{
			base._PhysicsProcess(delta);
			Data.OnWorldPhysicalProcess(delta);
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
