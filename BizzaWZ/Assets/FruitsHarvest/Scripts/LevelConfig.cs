using System;
using System.Collections.Generic;

[Serializable]
public class LevelConfig
{
	public string levelID;

	public string mapID;

	public string mapChildID;

	public int eleClassCount;

	public int totalGroupCount;

	public List<SingleNormalTile> normalTiles;
}
