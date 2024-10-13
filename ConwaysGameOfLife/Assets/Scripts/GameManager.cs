using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Tilemaps;

public class GameManager : MonoBehaviour
{
    public Tilemap tileMap;
    public TileMapManager tileMapManager;
    public TileBase defaultTile;

    public Color onColor = Color.white;
    public Color offColor = Color.black;

    public List<Vector3Int> allTileLocations; //Tilebase is the base class for a tile, parent to all child tile objects/methods/parameters

    public enum GameState
    {
        GameSetup,
        GamePlaying,
        GameOver
    }
    public GameState gameState;

    private void Awake()
    {
        UnpackReferences();
    }
    void Start()
    {
        SetUpGameBoard();
    }
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            InstantiateTilesPositionArray();
        }
        if (Input.GetKeyDown(KeyCode.Return))
        {
            {
                RandomizeAllColors();
            }
        }
    }
    void UnpackReferences()
    {
        tileMap = GameObject.FindGameObjectWithTag("Game Board").GetComponent<Tilemap>();
        tileMapManager = GetComponent<TileMapManager>();

    }

    void SetUpGameBoard()
    {
        if (gameState.ToString() == "GameSetup")
        {
            InstantiateTilesPositionArray();
        }
    }

    void InstantiateTilesPositionArray()
    {
        if(tileMapManager != null && tileMap != null && defaultTile != null)
        {
            allTileLocations.Clear();
            foreach(var tilePosition in tileMapManager.tileMapBounds.allPositionsWithin)
            {
                allTileLocations.Add(tilePosition);
                tileMap.SetTile(tilePosition, defaultTile);
                ColorTile(tilePosition, tileMap, offColor);
            }

        }
    }
    void ColorTile(Vector3Int tilePosition, Tilemap tileMap, Color desiredColor)
    {
            tileMap.SetTileFlags(tilePosition, TileFlags.None);
            tileMap.SetColor(tilePosition, desiredColor);
            print(tilePosition);
    }

    void RandomizeAllColors()
    {
        foreach(var tilePosition in allTileLocations)
        {
            ColorTile(tilePosition, tileMap, Random.Range(-1f, 1f) > 0 ? offColor : onColor);
        }
    }
}
