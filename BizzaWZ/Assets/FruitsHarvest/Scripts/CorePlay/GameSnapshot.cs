using System;
using System.Collections.Generic;

namespace CorePlay
{
	[Serializable]
	public class GameSnapshot
	{
		public LevelConfig levelConfig;

		public bool ifUseAddOneItem;

		public List<GameItemData> totalItems;

		public List<CollectAreaData> collectItems;

		public List<UndoRecord> undoRecords;
	}
}
