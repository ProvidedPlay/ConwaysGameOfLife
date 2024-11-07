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
        Debug.Log(saveFolderPath + saveFilePath);
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
}
