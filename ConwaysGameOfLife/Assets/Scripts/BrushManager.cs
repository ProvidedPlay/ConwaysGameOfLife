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

    public List<Vector3Int> brushTileMapColoredTiles;

    public bool brushCursorActive;
    public int defaultBrushIndex;

    public BrushData activeBrushData;
    public BrushData[] allBrushDataObjects;
    public BrushData[] customBrushDataObjects;

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
    }
    public void InitializeBrushManager()
    {
        SelectBrushType(defaultBrushIndex, true);
    }
    public void ToggleBrushCursor(bool cursorActive)
    {
        brushCursorActive = cursorActive;
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
}
