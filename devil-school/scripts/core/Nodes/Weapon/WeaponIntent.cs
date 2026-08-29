
namespace EGame
{
    public class WeaponIntent
    {
        public bool Pressing { get; set; }
        public bool JustPressed { get; set; }

        public void Reset()
        {
            Pressing = false;
            JustPressed = false;
        }
    }
}