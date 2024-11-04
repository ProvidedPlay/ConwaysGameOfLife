using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public static class TileMapGenerationHelper
{
    public static int GetTileMapWidth(int tileMapHeight, float tileMapWidthToHeightRatio)
    {
        return (int)Mathf.Round(tileMapHeight * tileMapWidthToHeightRatio);
    }
    public static LevelData GenerateBlankLevel(int tileMapHeight)
    {
        return new LevelData(tileMapHeight);
    }
    public static void UpdateTileMapHeight(TileMapManager tileMapManager, int tileMapHeight)
    {
        tileMapManager.TileMapHeight = tileMapHeight;
    }
}
