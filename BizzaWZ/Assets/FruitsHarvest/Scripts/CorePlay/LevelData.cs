using System;

namespace CorePlay
{
	[Serializable]
	public class LevelData
	{
		public int attempCount;

		public string levelID;

		public bool isWin;

		public bool isLose;

		public bool hasEffectiveSave;
		public int difficultyRevision;

		public GameSnapshot gameSnapshot;

		public bool hasShownHardLevelTip;

		public int levelTipContentType;

		public int levelTipTextIndex;

		public int levelTipPercentage;

		public bool levelTipWasShown;

		public string entryType;

		public int eliminatedItemCount;

		public string itemUseRecord;

		public int totalItemCount;

		public float sessionTime;

		public bool timerRunning;

		public int undoUseCount;

		public int shuffleUseCount;

		public int magicUseCount;

		public int extraUseCount;

		public int goldSpendCount;

		public string reviveRecord;

		public int elimGroupCount;

		public int clutchSavePhase;

		public int lastClutchElimGroupCount;

		public int GetSessionDuration()
		{
			return (int)sessionTime;
		}

		public void TickSessionTimer(float deltaTime)
		{
			if (timerRunning)
			{
				sessionTime += deltaTime;
			}
		}

		public void Reset(bool replay)
		{
			isWin = false;
			isLose = false;
			hasEffectiveSave = false;
			gameSnapshot = null;
			eliminatedItemCount = 0;
			itemUseRecord = null;
			totalItemCount = 0;
			sessionTime = 0f;
			timerRunning = false;
			undoUseCount = 0;
			shuffleUseCount = 0;
			magicUseCount = 0;
			extraUseCount = 0;
			goldSpendCount = 0;
			reviveRecord = null;
			elimGroupCount = 0;
			clutchSavePhase = 0;
			lastClutchElimGroupCount = 0;
			if (!replay)
			{
				attempCount = 0;
				levelID = null;
			}
		}
	}
}
