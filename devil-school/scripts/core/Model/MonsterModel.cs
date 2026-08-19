
using System;

namespace EGame
{
	[ModelCategory]
	public abstract class MonsterModel : CharacterModel
	{
		public virtual float VisualLength => 10f;
		public virtual float VisualAngle => 90f;
    }
}
