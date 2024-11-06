using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class BrushData
{
    public string brushName;

    public Sprite brushImage;

    public List<Vector3Int> livingCellsRelativeToBrushOrigin;
    public Vector2Int brushSize;
}
