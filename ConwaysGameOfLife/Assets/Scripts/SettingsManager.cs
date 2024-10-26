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


    private void Awake()
    {
        UnpackObjectReferences();
    }

    void UnpackObjectReferences()
    {
        gameManager = GameObject.FindGameObjectWithTag("GameController").GetComponent<GameManager>();
    }

    /*
     * Utilities
     */
    public void UpdateGameStateText(string gameState)
    {
        gameManager.currentGameState = gameState;
        gameManager.SetText(currentGameStateText, gameManager.currentGameState);

        string newPlayPauseButtonText;
        newPlayPauseButtonText = gameState == "EDIT MODE" ? "PLAY" : "PAUSE";
        gameManager.SetText(playPauseButtonText, newPlayPauseButtonText); //improve this!!! remove hardcoded values!
    }


    /*
     * Button Logic 
     */

    public void PressPlayPause()
    {
        if (gameManager != null)
        {
            gameManager.ProceedToNextGameState();  
        }
    }

    public void PressGenerateNewMap()
    {
        if (gameManager != null)
        {
            gameManager.GameStart();
        }
    }

    public void PressChangeZoomButton(bool increaseZoom)
    {
        if(gameManager != null)
        {
            gameManager.cameraController.ZoomCamera(increaseZoom ? 1 : -1, false);
        }


    }
}
