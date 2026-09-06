
using Godot;

namespace EGame
{
    public partial class NHUDPanel : NAbstractPanel
    {
        private Control _HPBar;
        private Control _BGBar;

        public override void _Ready()
        {
            base._Ready();
            _HPBar = GetNode<Control>("%HPBar");
            _BGBar = GetNode<Control>("%BGBar");
        }

        public override void OnInit()
        {
            base.OnInit();
            NGame.Instance.PlayerNode.Data.OnHPChanged += (int old_hp, int new_hp) =>
            {
                RefreshBar(new_hp, NGame.Instance.PlayerNode.Data.MaxHP);
            };
        }

        private void RefreshBar(int hp, int max_hp)
        {
            float total_width = _BGBar.Size.X;
            float new_width = hp * 1.0f / max_hp * total_width;
            _HPBar.SetSize(new Vector2(new_width, _HPBar.Size.Y));
        }
    }
}