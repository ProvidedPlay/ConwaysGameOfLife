using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

[ExecuteInEditMode]
public static class TileMapGenerationHelper
{
    public static int GetTileMapWidth(int tileMapHeight, float tileMapWidthToHeightRatio)
    {
        return (int)Mathf.Round(tileMapHeight * tileMapWidthToHeightRatio);
    }
    public static void UpdateTileMapHeight(TileMapManager tileMapManager, int tileMapHeight)
    {
        tileMapManager.TileMapHeight = tileMapHeight;
    }
    public static LevelData GenerateBlankLevel(int tileMapHeight)
    {
        return new LevelData(tileMapHeight);
    }
    public static Dictionary<Vector3Int, bool> GenerateCellDictionaryFromPositionList(List<Vector3Int> positionList, bool defaultValue)
    {
        Dictionary<Vector3Int, bool> cellDict = positionList.ToDictionary(x => x, value => defaultValue);//turns a list of vector3ints(cell positions) into a dict (of cells with bool = parameter defaultValue)
        return cellDict;

    }
    public static bool CellIsWithinMapBounds(Vector3Int cellPosition, BoundsInt tileMapBounds)
    {
        return tileMapBounds.Contains(cellPosition);
    }
    public static void ColorTile(Vector3Int tilePosition, Tilemap tileMap, Color desiredColor)
    {
        tileMap.SetTileFlags(tilePosition, TileFlags.None);
        tileMap.SetColor(tilePosition, desiredColor);
    }
}
