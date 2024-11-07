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
}
