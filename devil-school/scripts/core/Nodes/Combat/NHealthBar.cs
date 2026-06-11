using Godot;
namespace EGame
{
	public partial class NHealthBar : Control
	{
		private Control _MiddleHP;
		private Control _ForegroundHP;

		public float MaxForegroundWidth 
		{
			get
			{
				return _ForegroundHP.Size.X;
			}
		}

		public override void _Ready()
		{
			base._Ready();
			_MiddleHP = GetNode<Control>("%MiddleHP");
			_ForegroundHP = GetNode<Control>("%ForegroundHP");

			RefreshForegroundWidth();
		}

		private void RefreshForegroundWidth()
		{
			if(_ForegroundHP != null)
				_ForegroundHP.OffsetRight = GetFGWidth(10, 50) - MaxForegroundWidth;
		}

		private float GetFGWidth(int current_hp, int max_hp)
		{
			float aspect = (float) current_hp / max_hp;
			float width = aspect * MaxForegroundWidth;
			if(aspect > 0)
				width = Mathf.Max(width, 12.0f);
			return width;
		}
	}
}
