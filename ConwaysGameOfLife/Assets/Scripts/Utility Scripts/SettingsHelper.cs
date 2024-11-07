using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public static class SettingsHelper
{
    public static Color GenerateColourFromCellColourDataInputs(CellColourData cellColourData)
    {
        //converts a 255 color code to a float between 0 and 1, how unity reads color values
        Color generatedColour = new(float.Parse(cellColourData.redValueInputField.text)/255, float.Parse(cellColourData.greenValueInputField.text)/255, float.Parse(cellColourData.blueValueInputField.text)/255);
        return generatedColour;
    }

    public static BrushData GenerateBrushFromSelection(Vector3Int selectionStartPoint, Vector3Int selectionEndPoint, string brushName, GameManager gameManager)
    {

        //create a selectionbounds bounds int out of start/end points, get all vectors in those bounds
        BoundsInt selectionBounds = new BoundsInt();
        selectionEndPoint.z = 1;//this allows the bounds to have a thickness of one, otherwise it wont look for any tiles
        selectionBounds.SetMinMax(selectionStartPoint, selectionEndPoint);
        Debug.Log(selectionBounds.ToString());
        //grab a reference to game manager's existing livingcells dict
        Dictionary<Vector3Int, bool> livingCellsInMap = gameManager.livingCells;

        //create a new list, add all living cells in your selection bounds to it (zero the living cell's position to the startpoint of the selection box)
        List<Vector3Int> selectedLivingCells = new List<Vector3Int>();
        foreach (Vector3Int tilePosition in selectionBounds.allPositionsWithin)
        {
            if (livingCellsInMap.ContainsKey(tilePosition))
            {
                selectedLivingCells.Add(new (tilePosition.x, tilePosition.y, tilePosition.z));
            }
        }
        Debug.Log(selectedLivingCells.Count);
        Vector3Int brushCenter = Vector3Int.FloorToInt(selectionBounds.center);

        return new BrushData(brushName, false, selectedLivingCells, brushCenter);
    }

    public static void AddCustomBrushDropdownMenuItem(string brushName, SettingsManager settingsManager)
    {
        settingsManager.customBrushesDropdown.options.Add(new TMP_Dropdown.OptionData() { text = brushName });
    }
}
