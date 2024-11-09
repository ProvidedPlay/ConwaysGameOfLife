using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SFB;
using System.IO;

public static class FileExplorerHelper
{
    public static string OpenFile(string saveFileDirectory)
    {
        ExtensionFilter[] extentionsToFilterFor = new[] { new ExtensionFilter("json") };
        string[] selectedFilePath = StandaloneFileBrowser.OpenFilePanel("Load Map File", saveFileDirectory, extentionsToFilterFor, false);
        return selectedFilePath[0];
    }
    public static string SaveFile(string saveFileDirectory)
    {
        string createdFilePath = StandaloneFileBrowser.SaveFilePanel("Save Map File", saveFileDirectory, "Conway's Game of Life Map", "json");
        return createdFilePath;
    }
    public static string SaveBrush(string saveFileDirectory)
    {
        string createdFilePath = StandaloneFileBrowser.SaveFilePanel("Save Brush File", saveFileDirectory, "Brush", "json");
        return createdFilePath;
    }
    public static string ExtractFileNameFromFilepath(string fileName)
    {
        return Path.GetFileNameWithoutExtension(fileName);
    }
}
