using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Tilemaps;

public class GameManager : MonoBehaviour
{
    public Tilemap tileMap;

    public Color onColor = Color.white;
    public Color offColor = Color.black;

    public enum GameState
    {
        GameSetup,
        GamePlaying,
        GameOver
    }
    public GameState state;

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
        
    }
    void UnpackReferences()
    {
        tileMap = GameObject.FindGameObjectWithTag("Game Board").GetComponent<Tilemap>();
    }

    void SetUpGameBoard()
    {
        
    }

    void ColorTile(Vector3Int tilePosition, Tilemap tileMap)
    {
            tileMap.SetTileFlags(tilePosition, TileFlags.None);
            tileMap.SetColor(tilePosition, offColor);
    }
}
