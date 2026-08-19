
using System;
using System.Collections.Generic;

namespace EGame
{
	public static class AbstractModelSubtypes
	{
		public static IReadOnlyList<Type> AllSubTypes => _ModelSubtypes;

		private static Type[] _ModelSubtypes = new Type[]
		{
		};
	}
}
