
using Godot;
using System.Collections.Generic;

namespace EGame
{
	/// <summary>
	/// 管理整个游戏的启动等等(应用级别)
	/// </summary>
	public partial class NGame : Node
	{
		public static NGame Instance { get; private set; }
		public NPlayer PlayerNode { get; private set; }

		public bool IsFlyCheatOn = false;

		public override void _EnterTree()
		{
			base._EnterTree();
			Instance = this;
			
			ModelDB.OnInit();
			Settings.LogLevel = Log.LogLevel.Debug;
		}

        public override void _Ready()
        {
            base._Ready();

			// TODO 临时：排查拉伸，用完删掉
			LogStretch();
			GetTree().Root.SizeChanged += LogStretch;

			InitCfg();
			InitManager();
            CreatePlayer();
        }

        public override void _Process(double delta)
        {
            base._Process(delta);
            Timer.Instance.Process(delta);
        }

		private void LogStretch()
		{
			var root = GetTree().Root;
			var ui_parent = GetNode<Control>("%UIParent");
			Log.Debug($"stretch mode={root.ContentScaleMode} aspect={root.ContentScaleAspect} factor={root.ContentScaleFactor} size={root.Size} scale_size={root.ContentScaleSize} ui={ui_parent.Size}");
		}

		private void InitManager()
		{
            var ui_parent = GetNode<Control>("%UIParent");
            UIManager.Instance.Init(ui_parent);

			var ui_sound_parent = GetNode<Node>("%Sound");
			SoundManager.Instance.Init(ui_sound_parent);

            var pool_parent = GetNode<Node>("%Pool");
			PoolManager.Instance.Init(pool_parent);
        }

		private void InitCfg()
		{
			UserData.Instance.SetFloat("volume", 0.1f);
		}

		private void CreatePlayer()
		{
			PlayerNode = NPlayer.Create(new Player());
			AddChild(PlayerNode);

			var spawn_point = GetNodeOrNull<Node3D>("%PlayerSpawnPoint");
			if (spawn_point != null)
			{
                PlayerNode.GlobalPosition = spawn_point.GlobalPosition;
                PlayerNode.GlobalRotation = spawn_point.GlobalRotation;
            }

			UIManager.Instance.Show(UIPanelType.HudPanel);
        }

		public void RebornPlayer()
		{
			PlayerNode.Data.HP = PlayerNode.Data.MaxHP;

            var spawn_point = GetNodeOrNull<Node3D>("%PlayerSpawnPoint");
			if (spawn_point != null)
			{
                PlayerNode.GlobalPosition = spawn_point.GlobalPosition;
				PlayerNode.GlobalRotation = spawn_point.GlobalRotation;
            }
			else
			{
                PlayerNode.GlobalPosition = Vector3.Zero;
				PlayerNode.GlobalRotation = Vector3.Zero;
            }
        }
	}
}
