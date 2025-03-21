using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;


public class SettingsManager : MonoBehaviour
{

    public GameManager gameManager;

    public TextMeshProUGUI currentGameStateText;
    public TextMeshProUGUI playPauseButtonText;
    public TextMeshProUGUI zoomMultiplierText;
    public TextMeshProUGUI gameSpeedFactorText;

    public TMP_InputField gridSizeXAxisText;
    public TMP_InputField gridSizeYAxisInputField;

    public CellColourData blankCellColourData;
    public CellColourData deadCellColourData;
    public CellColourData aliveCellColourData;

    public TMP_Dropdown presetBrushesDropdown;
    public TMP_Dropdown customBrushesDropdown;

    public SlideToHideMenu sideMenu;
    public SlideToHideMenu showMenuButton;
    public SlideToHideMenu brushMenu;
    public SlideToHideMenu showBrushMenuButton;



    private void Awake()
    {
        UnpackObjectReferences();
    }

    void UnpackObjectReferences()
    {
        gameManager = GameObject.FindGameObjectWithTag("GameController").GetComponent<GameManager>();
    }

    /*
     * UI Utilities
     */
    public void UpdateGameStateText(string gameState)
    {
        gameManager.currentGameState = gameState;
        SetUIText.SetText(currentGameStateText, gameManager.currentGameState);

        string newPlayPauseButtonText;
        newPlayPauseButtonText = gameState == "EDIT MODE" ? "PLAY" : "PAUSE";
        SetUIText.SetText(playPauseButtonText, newPlayPauseButtonText); //improve this!!! remove hardcoded values!
    }
    public void UpdateZoomMultiplierText()
    {
        SetUIText.SetText(zoomMultiplierText, (gameManager.cameraController.maxZoomFactor - gameManager.cameraController.ZoomFactor).ToString() + "X");
    }

    public void UpdateSpeedFactorText()
    {
        SetUIText.SetText(gameSpeedFactorText, gameManager.GameSpeedFactor.ToString()+ "X");
    }

    public static void ClampColourInputValueTo255(TMP_InputField colourInputField)
    {
        int newColourValue = Mathf.Clamp(int.Parse(colourInputField.text), 0, 255);

        colourInputField.text = newColourValue.ToString();
    }
    /*
     * Button Logic (Side Menu)
     */

    public void PressPlayPause()
    {
        if (gameManager != null)
        {
            gameManager.ProceedToNextGameState();  
        }
    }

    public void PressChangeZoomButton(bool increaseZoom)
    {
        if(gameManager != null)
        {
            gameManager.cameraController.ZoomCamera(increaseZoom ? 1 : -1, false);
        }
    }
    public void PressChangeGameSpeedFactorButton(bool increaseSpeedFactor)
    {
        if (gameManager != null)
        {
            gameManager.IncrementGameSpeedFactor(increaseSpeedFactor ? 1 : -1);
        }
    }
    public void PressShowHideSideMenu(bool hideSideMenu)
    {
        if (hideSideMenu)
        {
            sideMenu.SlideToHide(true, showMenuButton);
        }
        else
        {
            showMenuButton.SlideToHide(true, sideMenu);
        }
    }
    public void OnClickQuitGame()
    {
        gameManager.QuitGame();
    }

    /*
     * Button Logic (Generate Map Menu)
     */
    public void OnUpdateSettingsMenuYAxisValue()
    {
        int yAxisValueInt = int.Parse(gridSizeYAxisInputField.text);
        string xAxisText = TileMapGenerationHelper.GetTileMapWidth(yAxisValueInt, gameManager.tileMapManager.tileMapWidthToHeightRatio).ToString();

        SetUIText.SetInputFieldText(gridSizeXAxisText, xAxisText);
    }

    public void OnClickGenerateButton(TMP_InputField gridSizeYAxisInputField)
    {
        int newLevelHeight = int.Parse(gridSizeYAxisInputField.text);
        LevelData newLevelData = TileMapGenerationHelper.GenerateBlankLevel(newLevelHeight);

        gameManager.GameLoad(newLevelData);
    }

    public void OnClickSaveGame()
    {
        SaveLoadManager.SaveLevelWithName(gameManager);
    }

    public void OnClickLoadGame()
    {
        LevelData newLevelData = SaveLoadManager.LoadLevel();
        if (newLevelData != null)
        {
            gameManager.GameLoad(newLevelData);
        }
    }
    /*
     * Button Logic (Brush Menu)
     */
    public void PressShowHideBrushMenu(bool hideBrushMenu)
    {
        if (hideBrushMenu)
        {
            brushMenu.SlideToHide(true, showBrushMenuButton);
        }
        else
        {
            showBrushMenuButton.SlideToHide(true, brushMenu);
        }
    }

    public void SelectPresetBrushesDropdownItem()
    {
        if (presetBrushesDropdown != null && presetBrushesDropdown.value != -1)//only run if the dropdown isnt being cleared (aka value changed to -1)
        {
            int dropdownIndex = presetBrushesDropdown.value;
            gameManager.brushManager.SelectBrushType(dropdownIndex, true);

            customBrushesDropdown.value = -1; //This clears the custom brush dropdown's active selection 
        }
    }
    public void SelectCustomBrushesDropdownItem()
    {
        if (customBrushesDropdown != null && customBrushesDropdown.value != -1)
        {
            int dropdownIndex = customBrushesDropdown.value;
            gameManager.brushManager.SelectBrushType(dropdownIndex, false);

            presetBrushesDropdown.value = -1;
        }
    }
    public void OnClickCopyButton()
    {
        gameManager.brushManager.ToggleSelectionCursor(true);
    }

    public void OnClickImportButton()
    {
        List<BrushData> newBrushDataObjects = SaveLoadManager.LoadBrushesFromFileExplorer(true);
        foreach (BrushData newBrushData in newBrushDataObjects)
        {
            gameManager.brushManager.ImportBrush(newBrushData, true);
        }
    }

    public void OnClickImportRLEButton()
    {
        //enter logic here
        LevelData newLevelDataObject = SaveLoadManager.ParseAndImportRLELevelFromFileExplorer(gameManager);
        if(newLevelDataObject != null)
        {
            gameManager.GameLoad(newLevelDataObject);

        }
    }

    /*
     * Advanced Settings Menu Buttons
     */

    public void OnClickApplyAdvancedSettings()
    {
        //Apply color settings for each cell color (rgb)
        gameManager.onColor = SettingsHelper.GenerateColourFromCellColourDataInputs(aliveCellColourData);
        gameManager.offColor = SettingsHelper.GenerateColourFromCellColourDataInputs(blankCellColourData);
        gameManager.deadColor = SettingsHelper.GenerateColourFromCellColourDataInputs(deadCellColourData);

        gameManager.GameRestart();
    }

}
