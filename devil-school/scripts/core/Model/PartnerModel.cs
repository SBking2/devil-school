
namespace EGame
{
    public class PartnerModel : AbstractModel
    {
        protected virtual string VisualsPath => "creature_visuals/" + ID.Entry.ToLower();
        public NCreatureVisual CreateVisual()
        {
            return SceneHelper.LoadScene<NCreatureVisual>(VisualsPath);
        }
    }
}