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

    public SlideToHideMenu sideMenu;
    public SlideToHideMenu showMenuButton;


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
        Debug.Log("code run");
        if (hideSideMenu)
        {
            sideMenu.SlideToHide(true, showMenuButton);
        }
        else
        {
            showMenuButton.SlideToHide(true, sideMenu);
        }
    }

    /*
     * Button Logic (Generate Map Menu)
     */
    public void OnUpdateSettingsMenuYAxisValue()
    {
        int yAxisValueInt = int.Parse(gridSizeYAxisInputField.text);
        string xAxisText = TileMapGenerationHelper.GetTileMapWidth(yAxisValueInt, gameManager.tileMapManager.tileMapWidthToHeightRatio).ToString();
        Debug.Log(xAxisText);

        SetUIText.SetInputFieldText(gridSizeXAxisText, xAxisText);
    }
}
