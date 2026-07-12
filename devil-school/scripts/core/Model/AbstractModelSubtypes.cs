
using System;
using System.Collections.Generic;

namespace EGame
{
	public static class AbstractModelSubtypes
	{
		public static IReadOnlyList<Type> AllSubTypes => _ModelSubtypes;

		private static Type[] _ModelSubtypes = new Type[]
		{
			//Monster
			typeof(FighterModel),
			typeof(MagicModel),

			//Character
			typeof(PlayerDebugModel),

			//Encounter
			typeof(DebugEncounterModel),
			
			//World
			typeof(InitWorldModel)
		};
	}
}
