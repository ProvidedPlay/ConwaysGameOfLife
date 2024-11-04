using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public class LevelData
{

    public List<Vector3Int> livingCells;
    public int tileMapHeight;

    public LevelData(GameManager gameManager)
    {
        //data required to load a save state with living cells
        livingCells = gameManager.livingCells.Keys.ToList();
        tileMapHeight = gameManager.tileMapManager.TileMapWidth;
    }

    public LevelData(int generatedTileMapHeight)
    {
        tileMapHeight = generatedTileMapHeight;
        livingCells = new List<Vector3Int>();
    }
}
