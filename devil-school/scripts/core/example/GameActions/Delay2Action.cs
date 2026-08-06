
using System.Threading.Tasks;

namespace EGame
{
    public class Delay2Action : GameAction
    {
        protected override async Task GetActionTask()
        {
            await Task.Delay(500);
        }
    }
}