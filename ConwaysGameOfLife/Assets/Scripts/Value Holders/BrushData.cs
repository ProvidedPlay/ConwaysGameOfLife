using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class BrushData
{
    public string brushName;

    public bool canDrag;

    public Vector3Int[] livingCellsRelativeToBrushOrigin;
    public Vector3Int brushCenter;
}
