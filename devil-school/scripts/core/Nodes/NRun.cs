
using Godot;

namespace EGame
{
	//开始游戏之后（进入存档之后）
	public partial class NRun : Control
	{
		private const string _ScenePath = "res://scenes/run.tscn";
		public override void _Ready()
		{
			base._Ready();
			Logger.Debug("NRun Start!");
		}
	}
}
