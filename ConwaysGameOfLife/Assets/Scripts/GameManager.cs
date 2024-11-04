using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Tilemaps;
using TMPro;

public class GameManager : MonoBehaviour
{
    public Tilemap tileMap;
    public Tilemap overlayGridTileMap;
    public TilemapRenderer overlayGridTileMapRenderer;
    public TileBase defaultTile;
    public TileBase gridTile;

    public TileMapManager tileMapManager;
    public CameraToGameBoardResizer cameraController;
    public SettingsManager settingsManager;

    public bool proceedToNextGameState;
    public string proceedToNextGameStateShortcut;
    public string currentGameState;

    public Color onColor = Color.white;
    public Color offColor = Color.black;

    public int tilemapZAxisPosition;
    public int maxHorizontalCellsBeforeGridDefaultOff;

    public int maxGameSpeedFactor;
    public float minimumTickIntervalTime;
    public float maximumTickIntervalTime;
    public float tickIntervalTime;
    [Range(1,10)]
    [SerializeField]
    private int gameSpeedFactor;
    public int GameSpeedFactor
    {
        get { return gameSpeedFactor; }
        set 
        { 
            gameSpeedFactor = Mathf.Clamp(value, 1, maxGameSpeedFactor);
            UpdateGameSpeedToSpeedFactor();
        }
    }
    public Dictionary<Vector3Int, bool> allTiles = new(); //Dict key = transform, value = isLiving bool
    public Dictionary<Vector3Int, bool> selectedTiles = new();
    public Dictionary<Vector3Int, bool> livingCells = new();
    public Dictionary<Vector3Int, int> deadCellConsiderationDict = new();
    public Dictionary<Vector3Int, bool> cellsMarkedForLifeChange = new();

    private void Awake()
    {
        UnpackReferences();
    }
    void Start()
    {
        //SetUpGameBoard();
        GameStart();
    }
    void Update()
    {
        if (Input.GetButtonDown(proceedToNextGameStateShortcut))
        {
            ProceedToNextGameState();//sets the bool proceedToNextGameState to 'true'
        }
        if (Input.GetKeyDown(KeyCode.Escape))//restart from scratch
        {
            GameStart();
        }

    }
    private void OnValidate()
    {
        GameSpeedFactor = gameSpeedFactor;
    }
    void UnpackReferences()
    {
        tileMap = GameObject.FindGameObjectWithTag("Game Board").GetComponent<Tilemap>();
        overlayGridTileMap = GameObject.FindGameObjectWithTag("Overlay Grid").GetComponent<Tilemap>();
        overlayGridTileMapRenderer = GameObject.FindGameObjectWithTag("Overlay Grid").GetComponent<TilemapRenderer>();
        tileMapManager = GetComponent<TileMapManager>();
        cameraController = GetComponent<CameraToGameBoardResizer>();
        settingsManager = GameObject.FindGameObjectWithTag("UI").GetComponent<SettingsManager>();
    }


    /*
     * Utilities
     */


    void ColorTile(Vector3Int tilePosition, Tilemap tileMap, Color desiredColor)
    {
            tileMap.SetTileFlags(tilePosition, TileFlags.None);
            tileMap.SetColor(tilePosition, desiredColor);
    }
    /*
     * Commands
     */
    void InstantiateTilesPositionArray()
    {
        if (tileMapManager != null && tileMap != null && defaultTile != null && allTiles != null)
        {
            ClearAllTiles();
            foreach (var tilePosition in tileMapManager.tileMapBounds.allPositionsWithin)
            {
                //Debug.Log(tilePosition.ToString());
                allTiles.Add(tilePosition, false);
                tileMap.SetTile(tilePosition, defaultTile);
                overlayGridTileMap.SetTile(tilePosition, gridTile);
                ColorTile(tilePosition, tileMap, offColor);
            }
            tilemapZAxisPosition = (int)tileMap.transform.position.z;
        }
    }
    public void ClearAllTiles()
    {
        allTiles.Clear();
        livingCells.Clear();
        cellsMarkedForLifeChange.Clear();
        deadCellConsiderationDict.Clear();
        selectedTiles.Clear();

        tileMap.ClearAllTiles();
        overlayGridTileMap.ClearAllTiles();
    }
    [ContextMenu("forceSwitch")]
    void SetUpGameBoard()
    {
        InstantiateTilesPositionArray();
        cameraController.ResetCamera();

    }
    public void ProceedToNextGameState()
    {
        proceedToNextGameState = true;
    }
    public void ImportLivingCells(List<Vector3Int> livingCellLocations)
    {
        foreach(Vector3Int livingCellLocation in livingCellLocations)
        {
            KillOrBirthCell(livingCellLocation, true);
        }
    }

    /*
     * Game Start/Load/Save
     */
    public void GameStart()
    {
        StopAllCoroutines();
        SetUpGameBoard();
        StartCoroutine(GameLoop());
    }
    public void GameLoad(LevelData levelData)
    {
        StopAllCoroutines();
        TileMapGenerationHelper.UpdateTileMapHeight(tileMapManager, levelData.tileMapHeight);
        SetUpGameBoard();
        ImportLivingCells(levelData.livingCells);
        StartCoroutine(GameLoop());

    }

    /*
     * 
     * COROUTINES
     *      WRITE COROUTINE FORM:   <accessmodifier> IEnumerator <CoroutineName>()
     *      CALL COROUTINE FORM:    StartCoroutine(<CoroutineName>())              
     *              
     *                                                              //'yield <condition/code>': stops coroutine, waits for 'condition/code' to be met, resumes coroutine
     */
    private IEnumerator GameLoop()
    {
        yield return StartCoroutine(GameSetup());
        yield return StartCoroutine(GamePlaying());
        //yield return StartCoroutine(RoundEnding());
        //GameStart();
        StartCoroutine(GameLoop());
    }

    private IEnumerator GameSetup()
    {
        Debug.Log("Coroutine state: game setup");
        settingsManager.UpdateGameStateText("EDIT MODE");
        ToggleOverlayGrid(CheckIfToggleGridOnByDefault());
        while (!proceedToNextGameState)
        {
            //code in here will run during this game state
            if (Input.GetKeyDown(KeyCode.LeftControl))
            {
                ToggleOverlayGridOnOff();
            }
            CustomizeGameBoard();
            yield return null;
        }
        yield return new WaitForEndOfFrame();
        proceedToNextGameState = false;
    }
    private IEnumerator GamePlaying()
    {
        Debug.Log("Coroutine state: game playing");
        settingsManager.UpdateGameStateText("PLAY MODE");
        ClearSelectedTiles();
        ToggleOverlayGrid(false);
        while (!proceedToNextGameState)
        {
            //code in here will run during this game state

            RunCellLifeCycleLoop();

            yield return new WaitForSeconds(tickIntervalTime);
        }
        Debug.Log("exited game playing");
        yield return new WaitForEndOfFrame();
        proceedToNextGameState = false;
    }
    private IEnumerator RoundEnding()
    {
        Debug.Log("Coroutine state: game ending");
        while (!proceedToNextGameState)
        {
            //code in here will run during this game state

            yield return null;
        }
        yield return new WaitForEndOfFrame();
        proceedToNextGameState = false;
    }

    /*
     * Tile Selector Methods
     */
    void CustomizeGameBoard()
    {
        if (Input.GetMouseButton(0))
        {
            Vector3Int mousePosition = GetTransformAtMousePosition();

            FlipTileAtPosition(mousePosition);
        }
        if (Input.GetMouseButtonUp(0))
        {
            ClearSelectedTiles();
        }
    }
    void FlipTileAtPosition(Vector3Int mousePosition)
    {
        //Debug.Log("attempting to flip at "+ mousePosition);
        if (!EventSystem.current.IsPointerOverGameObject() && !selectedTiles.ContainsKey(mousePosition) && allTiles.ContainsKey(mousePosition))
        {
            //Debug.Log(mousePosition + " flipped");
            //allTiles[mousePosition] = !allTiles[mousePosition]; OLD
            KillOrBirthCell(mousePosition, !allTiles[mousePosition]);
            AddToSelectedTiles(mousePosition, allTiles[mousePosition]);
            //Color desiredColor = allTiles[mousePosition] == true ? onColor : offColor; OLD
            //ColorTile(mousePosition, tileMap, desiredColor);OLD
        }

    }
    Vector3Int GetTransformAtMousePosition()
    {
        Vector3Int mousePosition = Vector3Int.FloorToInt(Camera.main.ScreenToWorldPoint(Input.mousePosition));
        mousePosition.z = tilemapZAxisPosition; //this is because the camera z axis is not the same value as the tile z axis, and we want the tile z axis;
        return mousePosition;
    }
    void AddToSelectedTiles(Vector3Int tilePosition, bool tileIsAlive)
    {
        selectedTiles.Add(tilePosition, tileIsAlive);
    }
    void ClearSelectedTiles()
    {
        selectedTiles.Clear();
    }

    /*
     * Cell Consideration
     */
    void KillOrBirthCell(Vector3Int tilePosition, bool isCellAlive)
    {
        ColorTile(tilePosition, tileMap, isCellAlive ? onColor : offColor);

        if (isCellAlive)
        {
            livingCells[tilePosition] = true;
            allTiles[tilePosition] = true;
        }
        else if (!isCellAlive)
        {
            livingCells.Remove(tilePosition);
            allTiles[tilePosition] = false;
        }
    }
    public Dictionary<Vector3Int, bool> GetAdjacentTiles(Vector3Int targetCell)
    {
        Dictionary<Vector3Int, bool> adjacentTiles= new Dictionary<Vector3Int, bool>()
        {
            {new Vector3Int(targetCell.x-1, targetCell.y-1, tilemapZAxisPosition), allTiles.GetValueOrDefault(new Vector3Int (targetCell.x-1, targetCell.y-1, tilemapZAxisPosition) )},
            {new Vector3Int(targetCell.x-1, targetCell.y, tilemapZAxisPosition), allTiles.GetValueOrDefault(new Vector3Int (targetCell.x-1, targetCell.y, tilemapZAxisPosition)) },
            {new Vector3Int(targetCell.x-1, targetCell.y+1, tilemapZAxisPosition), allTiles.GetValueOrDefault(new Vector3Int (targetCell.x-1, targetCell.y+1, tilemapZAxisPosition)) },
            {new Vector3Int(targetCell.x, targetCell.y-1, tilemapZAxisPosition), allTiles.GetValueOrDefault(new Vector3Int (targetCell.x, targetCell.y-1, tilemapZAxisPosition)) },
            {new Vector3Int(targetCell.x, targetCell.y+1, tilemapZAxisPosition), allTiles.GetValueOrDefault(new Vector3Int (targetCell.x, targetCell.y+1, tilemapZAxisPosition)) },
            {new Vector3Int(targetCell.x+1, targetCell.y-1, tilemapZAxisPosition), allTiles.GetValueOrDefault(new Vector3Int (targetCell.x+1, targetCell.y-1, tilemapZAxisPosition)) },
            {new Vector3Int(targetCell.x+1, targetCell.y, tilemapZAxisPosition), allTiles.GetValueOrDefault(new Vector3Int (targetCell.x+1, targetCell.y, tilemapZAxisPosition)) },
            {new Vector3Int(targetCell.x+1, targetCell.y+1, tilemapZAxisPosition), allTiles.GetValueOrDefault(new Vector3Int (targetCell.x+1, targetCell.y+1, tilemapZAxisPosition)) },
        };
        return adjacentTiles;
    }

    void ConsiderLivingCells()
    {
        foreach(var livingCell in livingCells)
        {
            Dictionary<Vector3Int, bool> adjacentTiles = GetAdjacentTiles(livingCell.Key);
            int livingAdjacentCells = 0;
            //Debug.Log("now measuring " + livingCell);
            foreach (var cell in adjacentTiles)
            {
                if (cell.Value == true)
                {
                    livingAdjacentCells += 1;
                }
                else if (cell.Value == false && allTiles.ContainsKey(cell.Key))//this is the second time we lookup if the value exists (first is getvalueordefault) OPTIMIZETHIS
                {
                    deadCellConsiderationDict.TryGetValue(cell.Key, out int deadCellLivingAdjacentCells);
                    deadCellConsiderationDict[cell.Key] = deadCellLivingAdjacentCells + 1;
                    //Debug.Log("dead cell to consider: " + cell.Key);
                }
            }
            if(livingAdjacentCells <2 || livingAdjacentCells > 3)
            {
                MarkCellForLifeChange(livingCell.Key, false);
                //Debug.Log("Cell marked for death: " +  livingCell.Key + "living adjacent cells: " + livingAdjacentCells);
            }
        }
    }
    void ConsiderDeadCells()
    {
        foreach (var deadCell in deadCellConsiderationDict)
        {
            if (deadCell.Value == 3)
            {
                MarkCellForLifeChange(deadCell.Key, true);
                //Debug.Log("cell marked for birth: " +  deadCell.Key);
            }
        }
        deadCellConsiderationDict.Clear();
    }

    void MarkCellForLifeChange(Vector3Int targetCell, bool markedToLive)
    {
        cellsMarkedForLifeChange.Add(targetCell, markedToLive);
    }

    void UpdateCellLifeCycle()
    {
        foreach(var cell in cellsMarkedForLifeChange)
        {
            KillOrBirthCell(cell.Key, cell.Value);
        }
        cellsMarkedForLifeChange.Clear();
    }

    void RunCellLifeCycleLoop()
    {
        ConsiderLivingCells();
        ConsiderDeadCells();
        UpdateCellLifeCycle();
    }
    /*
     * Show/Hide overlay grid
     */

    void ToggleOverlayGrid(bool toggleOn)
    {
        overlayGridTileMapRenderer.enabled = toggleOn;
    }

    bool CheckIfToggleGridOnByDefault ()
    {
        bool toggleGridOnByDefault = tileMapManager.TileMapWidth < maxHorizontalCellsBeforeGridDefaultOff ? true : false;
        return toggleGridOnByDefault;
    }

    void ToggleOverlayGridOnOff()
    {
        ToggleOverlayGrid(!overlayGridTileMapRenderer.enabled);
    }
    /*
     * Manage Game Speed
     */
    public void UpdateGameSpeedToSpeedFactor()
    {
        tickIntervalTime = 1f /Mathf.Pow(2,GameSpeedFactor);
    }
    public void IncrementGameSpeedFactor(int gameSpeedFactorChange)
    {
        int newGameSpeedFactor = gameSpeedFactor +(1 * (int)Mathf.Sign(gameSpeedFactorChange));
        SetGameSpeedFactor(newGameSpeedFactor);
    }
    public void SetGameSpeedFactor(int newGameSpeedFactor)
    {
        GameSpeedFactor = newGameSpeedFactor;
        if (settingsManager != null)
        {
            settingsManager.UpdateSpeedFactorText();
        }
    }
    /*
     * Scene Manager
     */
    public void QuitGame()
    {
        StopAllCoroutines();
        Application.Quit();
    }
}

