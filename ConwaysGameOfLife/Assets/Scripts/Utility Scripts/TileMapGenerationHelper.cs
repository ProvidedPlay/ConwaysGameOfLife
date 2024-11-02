using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class TileMapGenerationHelper
{
    public static int GetTileMapWidth(int tileMapHeight, float tileMapWidthToHeightRatio)
    {
        return (int)Mathf.Round(tileMapHeight * tileMapWidthToHeightRatio);
    }   
}
