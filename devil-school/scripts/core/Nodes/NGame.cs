
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
	
		public override void _EnterTree()
		{
			base._EnterTree();
			Instance = this;
			
			ModelDB.OnInit();
			Settins.LogLevel = Log.LogLevel.Debug;
		}

        public override void _Ready()
        {
            base._Ready();
			CreatePlayer();
        }

		private void CreatePlayer()
        {
            var creautre_parent = GetNode<Node3D>("%CreatureParent");
            PlayerNode = NPlayer.Create(new Player());
            creautre_parent.AddChild(PlayerNode);
        }
	}
}
