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

    public BrushData activeBrushData;
    public BrushData[] allBrushDataObjects;

    void Awake()
    {
        UnpackObjectReferences();
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
        SelectBrushType(0);
    }
    public void ToggleBrushCursor(bool cursorActive)
    {
        brushCursorActive = cursorActive;
        activeBrushTileMapRenderer.renderingLayerMask = cursorActive ? (uint)2 : (uint)0; //render mask '2' is 'default' aka it shows on camera, render mask 0 is 'nothing' aka it doesnt render on anything
        Debug.Log("Just applied activeBrushData's brush image to brsuh render sprite");
    }
    public void SelectBrushType(int brushIndex)
    {
        ClearBrushTileMap();
        activeBrushData = allBrushDataObjects[brushIndex];
        PopulateBrushTileMapWithActiveBrush();
        Debug.Log("just set the active brush data");
    }
    public void ShowBrushAtMousePosition(Vector3Int mousePosition)
    {
        if (brushCursorActive && activeBrushData.brushImage != null && activeBrushTileMap != null)
        {
            activeBrushObject.transform.position = mousePosition;
        }
    }
    public List<Vector3Int> GetBrushCellPositionsAtMousePosition(Vector3Int mousePosition)
    {
        List<Vector3Int> newLivingCellPositions = new();
        if (brushCursorActive && activeBrushData.livingCellsRelativeToBrushOrigin != null)
        {
            foreach (Vector3Int livingCellPosition in activeBrushData.livingCellsRelativeToBrushOrigin)
            {
                newLivingCellPositions.Add(new(livingCellPosition.x + mousePosition.x, livingCellPosition.y + mousePosition.y, mousePosition.z));
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
            Debug.Log("added cell to brush");
        }
    }
}
