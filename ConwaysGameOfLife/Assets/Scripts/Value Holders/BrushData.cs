using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public class BrushData
{
    public string brushName;

    public bool canDrag;

    public Vector3Int[] livingCellsRelativeToBrushOrigin;
    public Vector3Int brushCenter;

    public BrushData(string brushName, bool canDrag, List<Vector3Int> livingCells, Vector3Int brushCenter)
    {
        this.brushName = brushName;
        this.canDrag = canDrag;
        this.brushCenter = brushCenter;

        livingCellsRelativeToBrushOrigin = livingCells.ToArray();
    }
}
