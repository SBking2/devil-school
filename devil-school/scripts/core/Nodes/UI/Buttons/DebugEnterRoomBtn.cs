
namespace EGame
{
	public partial class DebugEnterRoomBtn : NButton
	{
		protected override void OnPressed()
		{
			base.OnPressed();
			RunManager.Instance.DebugEnterRoom();
		}
	}
}
