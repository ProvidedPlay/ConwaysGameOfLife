using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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

        //create a new list, add all living cells in your selection bounds to it (zero the living cell's position to the startpoint of the selection box)
        List<Vector3Int> selectedLivingCells = new List<Vector3Int>();

        Cell[] livingCellsInSelectionBox = gameManager.mapComputeShaderManager.GetAllLivingCellsFromBounds(selectionBounds.min, selectionBounds.max);
        foreach (Cell cell in livingCellsInSelectionBox)
        {
                selectedLivingCells.Add(new(cell.cellPosition.x, cell.cellPosition.y, 0));
        }
        Vector3Int brushCenter = Vector3Int.FloorToInt(selectionBounds.center);

        return new BrushData(brushName, false, selectedLivingCells, brushCenter);
    }

    public static void GenerateSelectionBox(Vector3Int selectionBoxStartPoint, Vector3Int selectionBoxEndPoint, GameManager gameManager, CanvasScaler uiCanvasScaler)
    {
        RectTransform selectionBox = gameManager.uiManager.selectionBox;

        //Finc the minimum vertex and maximum vertex of the two input vertices (start and end point), save them as vector
        Vector3 selectionBoxMinScreenPosition = gameManager.gameCamera.WorldToScreenPoint(Vector3.Min(selectionBoxStartPoint, selectionBoxEndPoint));
        Vector3 selectionBoxMaxScreenPosition = gameManager.gameCamera.WorldToScreenPoint(Vector3.Max(selectionBoxStartPoint, selectionBoxEndPoint));

        float selectionBoxWidth = Mathf.Abs(selectionBoxMaxScreenPosition.x - selectionBoxMinScreenPosition.x);
        float selectionBoxHeight = Mathf.Abs(selectionBoxMaxScreenPosition.y - selectionBoxMinScreenPosition.y);

        //set the box position to the prev established min vector, set the width and height to the difference between the min and max vectors (x and y respectively). Correct for the increased UI scaling (ie if localscale is "*1.1", this will be "/1.1")
        selectionBox.anchoredPosition = new Vector2(selectionBoxMinScreenPosition.x, selectionBoxMinScreenPosition.y)/uiCanvasScaler.transform.localScale;
        selectionBox.sizeDelta = new Vector2(selectionBoxWidth, selectionBoxHeight)/uiCanvasScaler.transform.localScale;

    }
    public static void AddCustomBrushDropdownMenuItem(string brushName, SettingsManager settingsManager)
    {
        settingsManager.customBrushesDropdown.options.Add(new TMP_Dropdown.OptionData() { text = brushName });
    }
}
