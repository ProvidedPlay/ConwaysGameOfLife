using System.IO;
using UnityEngine;


public static class SaveLoadManager
{
    public static string saveFolderDirectory = "/SaveData/";
    public static void SaveLevelWithName(GameManager gameManager, string saveFileName)
    {
        //create save data directory if it doesnt exist
        string saveFolderDirectoryPath = Application.persistentDataPath + saveFolderDirectory; 
        if (!Directory.Exists(saveFolderDirectoryPath))
        {
            Directory.CreateDirectory(saveFolderDirectoryPath);
        }

        //create a save game object of class LevelData, turn it into a JSON string
        LevelData levelDataObject = new LevelData(gameManager);
        string levelDataJSON = JsonUtility.ToJson(levelDataObject);

        //create save file at specified path with specified save file name
        File.WriteAllText(saveFolderDirectoryPath + saveFileName, levelDataJSON);
    }
    public static LevelData LoadLevel(string saveFileName)
    {
        //find the path of the given save file
        string saveFilePath = Application.persistentDataPath + saveFolderDirectory + saveFileName;
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
