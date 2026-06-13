using Godot;
namespace EGame
{
    public partial class NCreatureStateDisplay : Control
    {
        public NCreature OwnerCreature { get; private set; }

        /// <summary>
        /// 子节点永远先于父节点Ready()
        /// </summary>
        public override void _Ready()
        {
            base._Ready();
            _MiddleHP = GetNode<Control>("%MiddleHP");
            _ForegroundHP = GetNode<Control>("%ForegroundHP");
        }

        public void SetCreature(NCreature nCreature)
        {
            OwnerCreature = nCreature;
        }

        public void OnCombatStateChanged(CombatState state)
        {
            RefreshHealthBar();
        }

        ///////////////////////////////////////////////////////////////////////////////////////
        ///////////                        HealthBar
        ///////////////////////////////////////////////////////////////////////////////////////

        private Control _MiddleHP;
        private Control _ForegroundHP;
        public float MaxForegroundWidth
        {
            get
            {
                return _ForegroundHP.Size.X;
            }
        }

        private void RefreshHealthBar()
        {
            if (_ForegroundHP != null)
            {
                var cur_hp = OwnerCreature.Data.HP;
                var max_hp = OwnerCreature.Data.MaxHP;
                _ForegroundHP.OffsetRight = GetFGWidth(cur_hp, max_hp) - MaxForegroundWidth;
            }    
        }
        private float GetFGWidth(int current_hp, int max_hp)
        {
            float aspect = (float)current_hp / max_hp;
            float width = aspect * MaxForegroundWidth;
            if (aspect > 0)
                width = Mathf.Max(width, 12.0f);
            return width;
        }
    }
}