
using System.Threading.Tasks;

namespace EGame
{
    public class DelayAction : GameAction
    {
        protected override async Task GetActionTask()
        {
            //Log.Debug("Delay Action !");
            await Task.Delay(1000);
        }
    }
}