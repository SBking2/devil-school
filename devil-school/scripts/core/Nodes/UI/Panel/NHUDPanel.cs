
using Godot;

namespace EGame
{
    public partial class NHUDPanel : NAbstractPanel
    {
        private Control _HPBar;
        private Control _BGBar;

        private TextureProgressBar _DashTimerCDProgress;
        private Label _BulletCountLabel;
        private Label _TotalBulletCountLabel;

        public override void _Ready()
        {
            base._Ready();
            _HPBar = GetNode<Control>("%HPBar");
            _BGBar = GetNode<Control>("%BGBar");
            _DashTimerCDProgress = GetNode<TextureProgressBar>("%DashTimerProgress");
            _BulletCountLabel = GetNode<Label>("%BulletCountLabel");
            _TotalBulletCountLabel = GetNode<Label>("%TotalBulletCountLabel");
        }

        public override void OnInit()
        {
            base.OnInit();
            var player = NGame.Instance.PlayerNode;
            player.OnWeaponChanged += OnWeaponChanged;
            player.OnAmmoChanged += OnAmmoChanged;
            RefreshCurrentAmmo();    // 面板初始化时武器已经拿好了，要主动同步一次

            NGame.Instance.PlayerNode.Data.OnHPChanged += (int old_hp, int new_hp) =>
            {
                RefreshBar(new_hp, NGame.Instance.PlayerNode.Data.MaxHP);
            };
        }

        public override void _Process(double delta)
        {
            base._Process(delta);

            var player = NGame.Instance?.PlayerNode;
            if (player == null)
                return;

            RefreshDashTimer(player.DashCooldownRemaining, player.DashCooldownTime);
        }
        public override void OnDestry()
        {
            base.OnDestry();

            var player = NGame.Instance?.PlayerNode;
            if (player != null)
            {
                player.OnWeaponChanged -= OnWeaponChanged;
                player.OnAmmoChanged -= OnAmmoChanged;
            }
        }

        // 不缓存武器引用：换武器/弹药变化时只是收到一个通知，真正的数据每次都现从
        // NGame.Instance.PlayerNode 这个单例上现取，武器的生命周期完全交给 NPlayer 自己管
        private void OnWeaponChanged(NWeapon weapon)
        {
            RefreshWeaponVisible(weapon);
            RefreshCurrentAmmo();
        }

        private void OnAmmoChanged() => RefreshCurrentAmmo();

        private void RefreshWeaponVisible(NWeapon weapon)
        {
            var active = weapon.Data.Type != WeaponModel.WeaponType.Sword;
            _BulletCountLabel.SetActive(active);
            _TotalBulletCountLabel.SetActive(active);
        }

        private void RefreshCurrentAmmo()
        {
            var ranged = NGame.Instance?.PlayerNode?.CurrentWeapon?.RangedData;
            _BulletCountLabel.SetActive(ranged != null);
            _TotalBulletCountLabel.SetActive(ranged != null);
            if (ranged != null)
                RefreshBulletCount(ranged.Type, ranged.CurrentAmmo, ranged.MagazineSize, ranged.TotalAmmo);
        }

        private void RefreshBar(int hp, int max_hp)
        {
            float total_width = _BGBar.Size.X;
            float new_width = hp * 1.0f / max_hp * total_width;
            _HPBar.SetSize(new Vector2(new_width, _HPBar.Size.Y));
        }
        
        private void RefreshDashTimer(float rest_time, float max_time)
        {
            _DashTimerCDProgress.Value = ((max_time - rest_time) / max_time) * _DashTimerCDProgress.MaxValue;
        }

        private void RefreshBulletCount(WeaponModel.WeaponType type, int cur_bullet, int max_bullet, int total_bullet)
        {
            _BulletCountLabel.Text = $"{cur_bullet}/{max_bullet}";
            _TotalBulletCountLabel.Text = total_bullet.ToString();
        }
    }
}