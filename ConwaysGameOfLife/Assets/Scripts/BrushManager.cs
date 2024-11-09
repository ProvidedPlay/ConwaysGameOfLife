using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class BrushManager : MonoBehaviour
{
    
    public Tilemap activeBrushTileMap;
    public TilemapRenderer activeBrushTileMapRenderer;
    public TileBase brushTile;
    public GameObject activeBrushObject;
    public GameManager gameManager;
    RectTransform selectionBox;

    public List<Vector3Int> brushTileMapColoredTiles;

    public bool brushCursorActive;
    public bool selectionCursorActive;
    public bool selectionCursorIsDragging;
    public int defaultBrushIndex;

    public string defaultCustomBrushName;
    public bool saveBrushAsPreset;

    public Vector3Int selectionBoxStartPoint;
    public Vector3Int selectionBoxEndPoint;

    public BrushData activeBrushData;
    public BrushData[] allBrushDataObjects;
    public List <BrushData> customBrushDataObjects;

    void Awake()
    {
        UnpackObjectReferences();
    }
    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.R))
        {
            FlipBrushAxis(true,false);
        }
        if (Input.GetKeyDown(KeyCode.F))
        {
            FlipBrushAxis(false, true);
        }
        if(selectionCursorIsDragging && !selectionCursorActive)
        {
            EndSelectionBoxDrag(true);
        }
        /*
        if (selectionCursorActive && selectionCursorIsDragging && selectionBoxStartPoint != Vector3Int.zero)
        {
            DrawSelectionBox();
        }
        */
    }
    void UnpackObjectReferences()
    {
        if(activeBrushObject == null)
        {
            activeBrushObject = GameObject.FindGameObjectWithTag("Tile Brush");
        }
        if(activeBrushTileMap == null)
        {
            activeBrushTileMap = GameObject.FindGameObjectWithTag("Tile Brush Renderer").GetComponent<Tilemap>();
        }
        if(activeBrushTileMapRenderer == null)
        {
            activeBrushTileMapRenderer = GameObject.FindGameObjectWithTag("Tile Brush Renderer").GetComponent<TilemapRenderer>();
        }
        if (activeBrushTileMap == null)
        {
            gameManager = GetComponent<GameManager>();
        }
        if (selectionBox == null && gameManager.uiManager.selectionBox != null)
        {
            selectionBox = gameManager.uiManager.selectionBox;
        }
    }
    public void InitializeBrushManager()
    {
        SelectBrushType(defaultBrushIndex, true);
    }
    public void ToggleBrushCursor(bool cursorActive)
    {
        brushCursorActive = cursorActive;
        if (cursorActive)
        {
            ToggleSelectionCursor(false);
        }
        activeBrushTileMapRenderer.renderingLayerMask = cursorActive ? (uint)2 : (uint)0; //render mask '2' is 'default' aka it shows on camera, render mask 0 is 'nothing' aka it doesnt render on anything
    }
    public void SelectBrushType(int brushIndex, bool isPreset)
    {
        ClearBrushTileMap();
        activeBrushData = isPreset ? allBrushDataObjects[brushIndex] : customBrushDataObjects[brushIndex];
        PopulateBrushTileMapWithActiveBrush();
    }
    public void ShowBrushAtMousePosition(Vector3Int mousePosition)
    {
        if (brushCursorActive && activeBrushTileMap != null)
        {
            activeBrushObject.transform.position = mousePosition - activeBrushData.brushCenter;
        }
    }
    public List<Vector3Int> GetBrushCellPositionsAtMousePosition(Vector3Int mousePosition)
    {
        List<Vector3Int> newLivingCellPositions = new();
        if (brushCursorActive && activeBrushData.livingCellsRelativeToBrushOrigin != null)
        {
            foreach (Vector3Int livingCellPosition in activeBrushData.livingCellsRelativeToBrushOrigin)
            {
                newLivingCellPositions.Add(new(livingCellPosition.x + mousePosition.x - activeBrushData.brushCenter.x, livingCellPosition.y + mousePosition.y - activeBrushData.brushCenter.y, mousePosition.z));
            }
        }
        return newLivingCellPositions;
    }
    /*
     * Brush TileMap Renderer Code
     */
    void ClearBrushTileMap()
    {
        activeBrushTileMap.ClearAllTiles();
        brushTileMapColoredTiles.Clear();
    }
    void PopulateBrushTileMapWithActiveBrush()
    {
        foreach(Vector3Int livingCellRelativeToBrushOrigin in activeBrushData.livingCellsRelativeToBrushOrigin)
        {
            activeBrushTileMap.SetTile(livingCellRelativeToBrushOrigin, brushTile);
            TileMapGenerationHelper.ColorTile(livingCellRelativeToBrushOrigin, activeBrushTileMap, gameManager.onColor);
        }
    }
    void RerenderBrushTileMap()
    {
        ClearBrushTileMap();
        PopulateBrushTileMapWithActiveBrush();
    }
    /*
     * TileMap Brush Commands
     */
    public void FlipBrushAxis(bool flipX, bool flipY)
    {
        for(int i = 0; i < activeBrushData.livingCellsRelativeToBrushOrigin.Length; i++)
        {
            if (flipX)
            {
                activeBrushData.livingCellsRelativeToBrushOrigin[i].x = -activeBrushData.livingCellsRelativeToBrushOrigin[i].x;
            }
            if (flipY)
            {
                activeBrushData.livingCellsRelativeToBrushOrigin[i].y = -activeBrushData.livingCellsRelativeToBrushOrigin[i].y;
            }
        }
        if (flipX)
        {
            activeBrushData.brushCenter.x = -activeBrushData.brushCenter.x;
        }
        if (flipY)
        {
            activeBrushData.brushCenter.y = -activeBrushData.brushCenter.y;
        }
        RerenderBrushTileMap();
    }
    /*
     * Selection Box Commands
     */
    public void StartSelectionBoxDrag(Vector3Int mousePosition)
    {
        selectionBoxStartPoint = mousePosition;
        selectionBoxEndPoint = mousePosition;
        selectionCursorIsDragging = true;
    }
    public void DragSelectionBox(Vector3Int mousePosition)
    {
        if (selectionCursorIsDragging)
        {
            selectionBoxEndPoint = mousePosition;
            DrawSelectionBox();
        }
    }
    public void EndSelectionBoxDrag(bool cancelSelect)
    {
        if (selectionCursorIsDragging)
        {
            Debug.Log("clicked");
            selectionCursorIsDragging = false;
            if (!cancelSelect)
            {
                BrushData newBrush = GenerateAndSaveBrush(!saveBrushAsPreset);
                if (newBrush != null)
                {
                    AddBrushToCustomBrushes(newBrush);
                }
                ToggleBrushCursor(true);
            }
        }
        selectionBoxStartPoint = Vector3Int.zero;
        selectionBoxEndPoint = Vector3Int.zero;
        DrawSelectionBox();
    }
    public void ToggleSelectionCursor(bool cursorActive)
    {
        selectionCursorActive = cursorActive;
        if(cursorActive)
        {
            ToggleBrushCursor(false);
        }
    }
    public void DrawSelectionBox()
    {
        //get the current mouse position
        /*
        Vector3 currentMouseScreenPosition = Input.mousePosition;
        Vector3 selectionBoxStartScreenPosition = gameManager.gameCamera.WorldToScreenPoint(selectionBoxStartPoint);

        float selectionBoxWidth = currentMouseScreenPosition.x - selectionBoxStartScreenPosition.x;
        float selectionBoxHeight = selectionBoxStartScreenPosition.y - currentMouseScreenPosition.y;
        float selectionBoxXPosition = selectionBoxStartScreenPosition.x;
        float selectionBoxYPosition = Screen.height - selectionBoxStartScreenPosition.y;

        Rect selectionBoxRect = new(selectionBoxXPosition, selectionBoxYPosition, selectionBoxWidth, selectionBoxHeight);
        GUI.Box(selectionBoxRect, "");
        

        Vector3 boxStartScreenPosition = gameManager.gameCamera.WorldToScreenPoint(selectionBoxStartPoint);
        Vector3 boxEndScreenPosition = gameManager.gameCamera.WorldToScreenPoint(selectionBoxEndPoint);

        float selectionBoxWidth = Mathf.Abs(boxEndScreenPosition.x - boxStartScreenPosition.x);
        float selectionBoxHeight = Mathf.Abs(boxEndScreenPosition.y - boxStartScreenPosition.y);
        selectionBox.position = boxStartScreenPosition;
        selectionBox.sizeDelta = new(selectionBoxWidth, selectionBoxHeight);
        */
        SettingsHelper.GenerateSelectionBox(selectionBoxStartPoint, selectionBoxEndPoint, gameManager, gameManager.uiManager.uiCanvasScaler);
    }
    /*
     * Custom Brush Import/Export Commands
     */
    public void AddBrushToCustomBrushes(BrushData brushData)
    {
        customBrushDataObjects.Add(brushData);
        SettingsHelper.AddCustomBrushDropdownMenuItem(brushData.brushName, gameManager.settingsManager);
    }
    public void AddBrushToPresetBrushes(BrushData brushData)
    {
        //add code here
    }
    public BrushData GenerateAndSaveBrush(bool isCustomBrush)
    {
        //create and name the new brush save file path
        string[] brushNameAndPath = SaveLoadManager.CreateBrushFilePathAndName(isCustomBrush);//runs code where cmr types in new brush name, returns empty string if cancelled

        string brushPath = brushNameAndPath[0];
        string brushName = brushNameAndPath[1];

        Debug.Log("brush path: " + brushPath + " brush name: " + brushName);

        if(brushName != "")
        {
            BrushData newBrush = SettingsHelper.GenerateBrushFromSelection(selectionBoxStartPoint, selectionBoxEndPoint, brushName, gameManager);
            SaveLoadManager.SaveBrushToFilePath(newBrush, brushPath);
            return newBrush;
        }
        else
        {
            return null;
        }
    }
    public void ImportBrush(BrushData brushData, bool isCustomBrush)
    {
        if(isCustomBrush)
        {
            AddBrushToCustomBrushes(brushData);
        }
    }
}
