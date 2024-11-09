using System.IO;
using UnityEngine;


public static class SaveLoadManager
{
    public static string saveFolderDirectory = "/SaveData/";
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
            Debug.LogError("Error! No save file found at " + saveFilePath);
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

    public static BrushData LoadBrushFromFileExplorer(bool loadCustomBrush)
    {
        string brushFolderPath = loadCustomBrush ? Application.persistentDataPath + customBrushesFolderDirectory : presetBrushesFolderDirectory;
        string brushFilePath = FileExplorerHelper.OpenFile(brushFolderPath);
        if (File.Exists(brushFilePath))
        {
            //if the brush file exists at the given path, parse the json in that file from string to a new BrushData object
            string brushDataJson = File.ReadAllText(brushFilePath);
            BrushData loadedBrushData = JsonUtility.FromJson<BrushData>(brushDataJson);

            return loadedBrushData;
        }
        else
        {
            //error message
            Debug.LogError("Error! No save file found at " + brushFilePath);
            return null;
        }
    }
}
