using Godot;
namespace EGame
{
	/// <summary>
	/// Visual部分只关心纯渲染的组件和功能
	/// </summary>
	public partial class NCreatureVisual : Node3D
	{
		private Node3D _ModelRoot;

		private AnimationPlayer _AnimPlayer;
		public Node3D ModelRoot => _ModelRoot;
		public AnimationPlayer AnimPlayer => _AnimPlayer;
		
		public override void _Ready()
		{
			base._Ready();
			_ModelRoot = GetNodeOrNull<Node3D>("%ModelRoot");
			
			if(_ModelRoot != null)
			{
				_AnimPlayer = _ModelRoot.GetNode<AnimationPlayer>("AnimationPlayer");
			}
		}
	}
}
