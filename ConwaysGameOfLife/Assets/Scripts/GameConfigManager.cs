using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameConfigManager : MonoBehaviour
{
    public GameManager gameManager;

    public string configFileName;
    public string configFilePath;
    public string preLoadedLevelsFolderName;

    public GameConfigData gameConfigData;

    private void Awake()
    {
        UnpackObjectReferences();
    }
    public void UnpackObjectReferences()
    {
        gameManager = GetComponent<GameManager>();
    }
    public void LoadPresetLevelsIfNeeded()
    {
        SaveLoadManager.GenerateConfigFile(gameManager);
        gameConfigData = SaveLoadManager.LoadGameConfigData(configFilePath, gameManager);
        if(gameConfigData.presetSaveFilesUnpacked == false)
        {
            SaveLoadManager.UnpackPresetLevelsIntoSaveFolder(gameManager);
            gameConfigData.presetSaveFilesUnpacked = true;
            SaveLoadManager.UpdateGameConfigData(configFilePath, gameManager, gameConfigData);
        }
    }
}
