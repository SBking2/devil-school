
namespace EGame
{
    public class MonsterModel : AbstractModel
    {
        protected virtual string VisualsPath => "creature_visuals/" + ID.Entry.ToLower();

        public NCreatureVisual CreateVisual()
        {
            return SceneHelper.LoadScene<NCreatureVisual>(VisualsPath);
        }
    }
}