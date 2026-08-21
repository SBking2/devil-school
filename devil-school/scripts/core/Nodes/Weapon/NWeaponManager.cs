
using System.Collections.Generic;
using Godot;

namespace EGame
{
    public partial class NWeaponManager : Node3D
    {
        private List<NWeapon> _Weapons = new List<NWeapon>();
        private int _CurrentWeaponIndex = -1;

        private void SetWeapon(int index)
        {
            if(index >= 0 && index < _Weapons.Count)
            {
                if (_CurrentWeaponIndex >= 0 && _CurrentWeaponIndex < _Weapons.Count)
                {
                    var cur_weapon = _Weapons[_CurrentWeaponIndex];
                    cur_weapon.UnEquip();
                }

                _CurrentWeaponIndex = index;
                var weapon = _Weapons[_CurrentWeaponIndex];
                weapon.Equip();
            }
        }
        
        public override void _Process(double delta)
        {
            base._Process(delta);

            if (Input.IsActionJustPressed(EGInput.SWITCHLEFT))
                SetWeapon((_CurrentWeaponIndex - 1 + _Weapons.Count) % _Weapons.Count);
            else
                SetWeapon((_CurrentWeaponIndex + 1) % _Weapons.Count);
        }
    }
}