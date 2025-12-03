using DG.Tweening.Core.Easing;
using System.Collections.Generic;
using System.IO;
using UnityEngine;


public static class SaveLoadManager
{
    public static string saveFolderDirectory = "/SaveData/";
    public static string rleFilesFolderDirectory = "/RLEData";
    public static string customBrushesFolderDirectory = "/Brushes/CustomBrushes";
    public static string presetBrushesFolderDirectory = "/Brushes/PresetBrushes";
    public static string gameConfigDirectory = "/Config";

    public static void GenerateConfigFile(GameManager gameManager)
    {
        //create game config directory if it doesn't exist
        string configFolderPath = Application.persistentDataPath + gameConfigDirectory;
        if (!Directory.Exists(configFolderPath))
        {
            Directory.CreateDirectory(configFolderPath);
        }
        //create the config file path via the name specified in gameManager's gameConfigManager; if theres nothing set, then no file will be made
        if (string.IsNullOrWhiteSpace(gameManager.gameConfigManager.configFileName))
        {
            Debug.Log("No config name specified in game manager at "+ configFolderPath);
            return;
        }
        string configFilePath = gameManager.gameConfigManager.configFileName + ".json";

        // check if there is already a config file present at file path. If one exists, stop. Otherwise, create one with default values
        string fullConfigFilePath = Path.Combine(configFolderPath,configFilePath);
        if (File.Exists(fullConfigFilePath))
        {
            Debug.Log("Config file already exists at stated filepath "+ fullConfigFilePath);
            gameManager.gameConfigManager.configFilePath = fullConfigFilePath;
            return;
        }

        //create an object of type GameConfigData (with default values), turn it into a JSON string
        GameConfigData gameConfigData = new GameConfigData();
        string gameConfigDataJSON = JsonUtility.ToJson(gameConfigData);

        //create a config file at specified path with specified config file name
        File.WriteAllText(fullConfigFilePath, gameConfigDataJSON);

        //save game file path to GameConfigManager
        gameManager.gameConfigManager.configFilePath = fullConfigFilePath;
    }
    public static GameConfigData LoadGameConfigData(string configFilePath, GameManager gameManager)
    {
        //Check if theres a game config file in the given path. If not, add one
        if (!File.Exists(configFilePath))
        {
            GenerateConfigFile(gameManager);
            configFilePath = gameManager.gameConfigManager.configFilePath;
        }

        //load data from JSON file into a string, then turn that string into a new GameConfigData file
        string gameConfigJSON = File.ReadAllText(configFilePath);
        GameConfigData gameConfigData = JsonUtility.FromJson<GameConfigData>(gameConfigJSON);

        //check if the json file exists: if so, return it; if not, return a new config file instead
        if(gameConfigData == null)
        {
            return new GameConfigData();
        }
        return gameConfigData;
    }
    public static void UpdateGameConfigData(string configFilePath, GameManager gameManager, GameConfigData newGameConfigData)
    {
        //Check if theres a game config file in the given path. If not, add one
        if (!File.Exists(configFilePath))
        {
            GenerateConfigFile(gameManager);
            configFilePath = gameManager.gameConfigManager.configFilePath;
        }

        string newGameConfigDataJSON = JsonUtility.ToJson(newGameConfigData);

        //create a config file at specified path with specified config file name
        File.WriteAllText(configFilePath, newGameConfigDataJSON);
    }
    public static void UnpackPresetLevelsIntoSaveFolder(GameManager gameManager)
    {
        //Grab the paths for both the Pre loaded Levels folder, and the save file folder
        string preLoadedLevelsFolderPath = Path.Combine(Application.streamingAssetsPath, gameManager.gameConfigManager.preLoadedLevelsFolderName);
        string saveFolderPath = Application.persistentDataPath + SaveLoadManager.saveFolderDirectory;

        //If the save directory doesnt exist yet, create one
        if (!Directory.Exists(saveFolderPath))
        {
            Directory.CreateDirectory(saveFolderPath);
        }

        
        foreach(string preLoadedLevelOriginPath in Directory.GetFiles(preLoadedLevelsFolderPath))
        {
            //for each file path in the pre loaded level directory, get the name of the file, and append it to the save folder to make a new file path
            string preLoadedLevelName = Path.GetFileName(preLoadedLevelOriginPath);
            string newSaveFilePath = Path.Combine(saveFolderPath, preLoadedLevelName);

            //copy the source file into the destination file path. Overwrite anything already there
            File.Copy(preLoadedLevelOriginPath, newSaveFilePath, overwrite: true);
        }
    }
    public static void SaveLevelWithName(GameManager gameManager)
    {
        //create save data directory if it doesnt exist
        string saveFolderPath = Application.persistentDataPath + saveFolderDirectory; 
        if (!Directory.Exists(saveFolderPath))
        {
            Directory.CreateDirectory(saveFolderPath);
        }
        //create a save file path via an explorer window, your save file will be written to this path later
        string saveFilePath = FileExplorerHelper.SaveFile(saveFolderPath);
        //if the above function doesnt produce a path (aka the player cancelled the save operation and returned an empty string), cancel the cave operation
        if (saveFilePath == "") 
        {
            Debug.Log("File not saved" + saveFilePath);
            return;
        }

        //create a save game object of class LevelData, turn it into a JSON string
        LevelData levelDataObject = new LevelData(gameManager);
        string levelDataJSON = JsonUtility.ToJson(levelDataObject);

        //create save file at specified path with specified save file name
        File.WriteAllText(saveFilePath, levelDataJSON);
    }
    public static LevelData LoadLevel()
    {
        //find the path of the given save file
        string saveFolderPath = Application.persistentDataPath + saveFolderDirectory;
        string saveFilePath = FileExplorerHelper.OpenFile(saveFolderPath);
        if (File.Exists(saveFilePath))
        {
            //if the save file exists at the given path, parse the json in that file from string to a new LevelData object
            string levelDataJson = File.ReadAllText(saveFilePath); 
            LevelData loadedLevelData = JsonUtility.FromJson<LevelData>(levelDataJson);

            return loadedLevelData;
        }
        else
        {
            //error message
            Debug.Log("Error! No save file found at " + saveFilePath);
            return null;
        }
    }
    public static LevelData ParseAndImportRLELevelFromFileExplorer(GameManager gameManager)
    {
        //find the path of the given RLE file
        string rleLevelFolderPath = Application.persistentDataPath + rleFilesFolderDirectory;
        string rleLevelFilePath = FileExplorerHelper.ImportRLELevel(rleLevelFolderPath);
        if (File.Exists(rleLevelFilePath))
        {
            //if the rle file exists at the given path, parse the RLE into a LevelData object
            string rleContent = File.ReadAllText(rleLevelFilePath);
            LevelData loadedLevelData = RLEParser.ParseLevelDataFromRLEFile(rleContent, gameManager);

            //save the generated level object to your save folder directory
            string saveFolderPath = Application.persistentDataPath + saveFolderDirectory;
            if (!Directory.Exists(saveFolderPath))
            {
                Directory.CreateDirectory(saveFolderPath);
            }
            string saveFilePath = Application.persistentDataPath + saveFolderDirectory + Path.GetFileNameWithoutExtension(rleLevelFilePath) + ".json";//creates a save file path in the save folder with the name of the imported rle file
            SaveLevelToFilePath(loadedLevelData, saveFilePath);

            return loadedLevelData;
        }
        else
        {
            //error message
            Debug.Log("Error! No RLE file found at " + rleLevelFilePath);
            return null;
        }
    }
    public static string[] CreateBrushFilePathAndName(bool isCustomBrush) //returns an array of 2 strings: index 0 = brush folder path, 1 = brush folder name
    {
        //create custom/preset brush directory if it doesnt exist
        string brushFolderPath = isCustomBrush ? Application.persistentDataPath + customBrushesFolderDirectory : Application.persistentDataPath + presetBrushesFolderDirectory;
        if(!Directory.Exists(brushFolderPath))
        {
            Directory.CreateDirectory(brushFolderPath);
        }
        
        //create a file path via an explorer window, your brush file will be written to this path later
        string brushFilePath = FileExplorerHelper.SaveBrush(brushFolderPath);

        //Get the name of the saved brush file (will be used to name your custom brush)
        string brushName = FileExplorerHelper.ExtractFileNameFromFilepath(brushFilePath);

        //index 0 = brush file path, 1 = brush folder name
        return new[] { brushFilePath, brushName };
    }
    public static void SaveBrushToFilePath(BrushData brushDataObject, string brushSaveFilePath)
    {
        //turn the brush data object into a JSON string
        string levelDataJSON = JsonUtility.ToJson(brushDataObject);

        //create save file at specified path with specified save file name
        File.WriteAllText(brushSaveFilePath, levelDataJSON);
    }
    public static void SaveLevelToFilePath(LevelData levelDataObject, string levelSaveFilePath)
    {
        //turn the level data object into a JSON string
        string levelDataJSON = JsonUtility.ToJson(levelDataObject);

        //create save file at specified path with specified save file name
        File.WriteAllText(levelSaveFilePath, levelDataJSON);
    }
    public static List<BrushData> LoadBrushesFromFileExplorer(bool loadCustomBrush)
    {
        List<BrushData> loadedBrushDataObjects = new List<BrushData>();
        string brushFolderPath = loadCustomBrush ? Application.persistentDataPath + customBrushesFolderDirectory : presetBrushesFolderDirectory;
        string[] brushFilePaths = FileExplorerHelper.ImportBrushes(brushFolderPath);
        foreach(string brushFilePath in brushFilePaths)
        {
            if (File.Exists(brushFilePath))
            {
                //if the brush file exists at the given path, parse the json in that file from string to a new BrushData object
                string brushDataJson = File.ReadAllText(brushFilePath);
                BrushData loadedBrushData = JsonUtility.FromJson<BrushData>(brushDataJson);

                loadedBrushDataObjects.Add(loadedBrushData);
            }
            else
            {
                //error message
                Debug.LogError("Error! No brush file found at " + brushFilePath);
                return null;
            }
        }
        return loadedBrushDataObjects;
    }
    public static List<BrushData> LoadAllBrushesFromDirectory(bool loadCustomBrush)//returns an array of all brushes from the default directory
    {
        List<BrushData> loadedBrushDataObjects = new List<BrushData>();
        string brushFolderPath = loadCustomBrush ? Application.persistentDataPath + customBrushesFolderDirectory : presetBrushesFolderDirectory;
        string[] brushFilePaths = Directory.GetFiles(brushFolderPath);
        foreach (string brushFilePath in brushFilePaths)
        {
            if (File.Exists(brushFilePath))
            {
                //if the brush file exists at the given path, parse the json in that file from string to a new BrushData object
                string brushDataJson = File.ReadAllText(brushFilePath);
                BrushData loadedBrushData = JsonUtility.FromJson<BrushData>(brushDataJson);

                loadedBrushDataObjects.Add(loadedBrushData);
            }
            else
            {
                //error message
                Debug.LogError("Error! No save file found at " + brushFilePath);
                return null;
            }
        }
        return loadedBrushDataObjects;
    }
}
