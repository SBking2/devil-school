
namespace EGame
{
	public abstract class MonsterModel : AbstractModel
	{
		protected virtual string VisualsPath => $"creature_visuals/" + ID.Entry.ToLowerInvariant();

		public NCreatureVisual CreateVisual()
		{
			return SceneHelper.LoadScene<NCreatureVisual>(VisualsPath);
		}
	}
}
