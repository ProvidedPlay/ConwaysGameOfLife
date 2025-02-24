using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[ExecuteInEditMode]
public class TileMapManager : MonoBehaviour
{
    public CameraToGameBoardResizer cameraResizer;
    public GameManager gameManager;
    public BoxCollider2D gameBorderBox;
    public TilemapRenderer tilemapRenderer;

    [SerializeField]//shows this in inspector
    //Check out this link for a refresher on why/how I did this: https://sj-jason-liu.medium.com/properties-c-skill-in-unity-4-adedd3959dc0#:~:text=To%20set%20a%20property%2C%20first,user%20can%20change%20the%20value.
    private int tileMapWidth;
    public int TileMapWidth
    {
        get {
            return tileMapWidth;
        }
        set {
            tileMapWidth = value;
        }
    }
    [SerializeField]
    private int tileMapHeight;
    public int TileMapHeight
    {
        get
        {
            return tileMapHeight;
        }
        set { 
            tileMapHeight=value;
            tileMapWidth = (int)Mathf.Round(tileMapHeight * tileMapWidthToHeightRatio);
            UpdateTileMapBounds(tileMapWidth, tileMapHeight);
            UpdateGameBorderBox();
            if (cameraResizer != null)
            {
                cameraResizer.UpdateCameraBounds(tileMapWidth, TileMapHeight);
                cameraResizer.ResetCamera();
            }
            UpdateGameBorderBox();
        }
    }

    public float tileMapWidthToHeightRatio;

    public BoundsInt tileMapBounds;
    public float cameraGameBoundsLeeway;
    void Awake()
    {
        UnpackObjectReferences();
    }

    //OnValidate executes whenever a value is changed in editor; allows changes in editor to activate whatever code is in each property's 'set{}' code block
    private void OnValidate()
    {
        TileMapWidth = tileMapWidth;
        TileMapHeight = tileMapHeight;
    }

    void UnpackObjectReferences()
    {
        cameraResizer = GetComponent<CameraToGameBoardResizer>();
        gameManager = GetComponent<GameManager>();
        gameBorderBox = GameObject.FindGameObjectWithTag("Game Bounds").GetComponent<BoxCollider2D>();
        tilemapRenderer = gameManager.tileMap.GetComponent<TilemapRenderer>();
    }

    void UpdateTileMapBounds(int width, int height)
    {
        Vector3Int minPosition = new(0, 0, 0);
        Vector3Int maxPosition = new(width, height, 1);
        tileMapBounds.SetMinMax(minPosition, maxPosition);
    }

    void UpdateGameBorderBox()
    {
        gameBorderBox.offset = new ((float)tileMapBounds.xMax/2 +(cameraGameBoundsLeeway/2), (float)tileMapBounds.yMax/2 + (cameraGameBoundsLeeway/2));
        gameBorderBox.size = new((float)tileMapBounds.xMax + cameraGameBoundsLeeway, (float)tileMapBounds.yMax + cameraGameBoundsLeeway);
    }
    public void ToggleHideTilemap(bool hide)
    {
        if (hide)
        {
            tilemapRenderer.enabled = false;
        }
        else
        {
            tilemapRenderer.enabled = true;
        }
    }
}
