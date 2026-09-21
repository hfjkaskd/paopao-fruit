using System;

namespace CorePlay
{
	[Serializable]
	public struct UndoRecord
	{
		public int itemId;

		public int siblingIndex;

		public int savedPriority;

		public float anchoredX;

		public float anchoredY;
	}
}
