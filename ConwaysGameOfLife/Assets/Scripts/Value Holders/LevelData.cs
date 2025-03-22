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
        List<Vector3Int> newLivingCells = new List<Vector3Int>();
        Cell[] livingCellsArray;
        livingCellsArray = gameManager.mapComputeShaderManager.GetAllLivingCellsFromBounds(gameManager.tileMapManager.tileMapBounds.min, gameManager.tileMapManager.tileMapBounds.max);
        foreach (var cell in livingCellsArray)
        {
            newLivingCells.Add(new(cell.cellPosition.x, cell.cellPosition.y, 0));
        }
        livingCells = newLivingCells;
        tileMapHeight = gameManager.tileMapManager.TileMapHeight;
    }

    public LevelData(int generatedTileMapHeight)
    {
        tileMapHeight = generatedTileMapHeight;
        livingCells = new List<Vector3Int>();
    }

    public LevelData(int tileMapHeight, List<Vector3Int> livingCells)
    {
        this.tileMapHeight = tileMapHeight;
        this.livingCells = livingCells;
    }
}
