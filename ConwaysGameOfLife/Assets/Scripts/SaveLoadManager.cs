using System.Collections.Generic;
using System.IO;
using UnityEngine;


public static class SaveLoadManager
{
    public static string saveFolderDirectory = "/SaveData/";
    public static string rleFilesFolderDirectory = "/RLEData";
    public static string customBrushesFolderDirectory = "/Brushes/CustomBrushes";
    public static string presetBrushesFolderDirectory = "/Brushes/PresetBrushes";
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
