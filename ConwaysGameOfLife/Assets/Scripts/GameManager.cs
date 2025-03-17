using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Tilemaps;
using TMPro;
using Cinemachine;
using UnityEngine.UI;
using System;

public class GameManager : MonoBehaviour
{
    public Camera gameCamera;
    public Tilemap tileMap;
    public Tilemap overlayGridTileMap;
    public TilemapRenderer overlayGridTileMapRenderer;
    public TileBase defaultTile;
    public TileBase gridTile;

    public TileMapManager tileMapManager;
    public CameraToGameBoardResizer cameraController;
    public SettingsManager settingsManager;
    public BrushManager brushManager;
    public UIManager uiManager;
    public MapComputeShaderManager mapComputeShaderManager;

    public bool proceedToNextGameState;
    public string proceedToNextGameStateShortcut;
    public string currentGameState;

    public Color onColor = Color.white;
    public Color offColor = Color.black;
    public Color deadColor = Color.black;

    public int tilemapZAxisPosition;
    public int maxHorizontalCellsBeforeGridDefaultOff;

    public int maxGameSpeedFactor;
    public float minimumTickIntervalTime;
    public float maximumTickIntervalTime;
    public float tickIntervalTime;
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
    public float importedRLEPaddingMultiplier;
    public Dictionary<Vector3Int, bool> allTiles = new(); //Dict key = transform, value = isLiving bool
    public Dictionary<Vector3Int, bool> selectedTiles = new();
    public Dictionary<Vector3Int, bool> livingCells = new();
    public Dictionary<Vector3Int, int> deadCellConsiderationDict = new();
    public Dictionary<Vector3Int, bool> cellsMarkedForLifeChange = new();
    public TileArrayData[] adjacentTiles = new TileArrayData[8];

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
            GameRestart();
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
        uiManager = GameObject.FindGameObjectWithTag("UI").GetComponent<UIManager>();
        brushManager = GetComponent<BrushManager>();
        gameCamera = Camera.main;
        mapComputeShaderManager = GetComponent<MapComputeShaderManager>();
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

            mapComputeShaderManager.GenerateMap(tileMapManager.TileMapWidth, tileMapManager.TileMapHeight);//generate map in compute shader
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
        brushManager.InitializeBrushManager();
        StartCoroutine(GameLoop());
    }
    public void GameRestart()
    {
        StopAllCoroutines();
        SetUpGameBoard();
        StartCoroutine(GameLoop());
    }
    public void GameLoad(LevelData levelData)
    {
        StopAllCoroutines();
        if (levelData != null){ TileMapGenerationHelper.UpdateTileMapHeight(tileMapManager, levelData.tileMapHeight); }
        SetUpGameBoard();
        if (levelData != null) 
        {
            ImportLivingCells(levelData.livingCells);
        }
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
        tileMapManager.ToggleHideTilemap(false);
        ToggleOverlayGrid(CheckIfToggleGridOnByDefault());
        brushManager.ToggleBrushCursor(true);
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
        brushManager.ToggleSelectionCursor(false);
        yield return new WaitForEndOfFrame();
        proceedToNextGameState = false;
    }
    private IEnumerator GamePlaying()
    {
        Debug.Log("Coroutine state: game playing");
        settingsManager.UpdateGameStateText("PLAY MODE");
        mapComputeShaderManager.GiveInputCellsToComputeShader(allTiles);
        tileMapManager.ToggleHideTilemap(true);
        ToggleOverlayGrid(false);
        brushManager.ToggleBrushCursor(false);
        brushManager.ToggleSelectionCursor(false);
        while (!proceedToNextGameState)
        {
            //code in here will run during this game state

            RunCellLifeCycleLoop();

            yield return new WaitForSeconds(tickIntervalTime);
        }
        Debug.Log("exited game playing");
        //mapComputeShaderManager.GetChangedCellsDataFromComputeShader();
        UpdateBoardStateFromComputeShader();

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
        if(Input.GetMouseButton(0) || Input.GetMouseButton(1) || brushManager.brushCursorActive || brushManager.selectionCursorActive){
            Vector3Int mousePosition = GetTransformAtMousePosition();
            if(brushManager.brushCursorActive)
            {
                brushManager.ShowBrushAtMousePosition(mousePosition);
                if (brushManager.activeBrushData.canDrag)
                {
                    if (Input.GetMouseButton(0) && !Input.GetMouseButton(1))
                    {
                        FlipTileAtPosition(brushManager.GetBrushCellPositionsAtMousePosition(mousePosition), true);
                    }
                }
                if (!brushManager.activeBrushData.canDrag)
                {
                    if (Input.GetMouseButtonDown(0) && !Input.GetMouseButton(1))
                    {
                        FlipTileAtPosition(brushManager.GetBrushCellPositionsAtMousePosition(mousePosition), true);
                    }
                }
                if (Input.GetMouseButton(1) && !Input.GetMouseButton(0))
                {
                    FlipTileAtPosition(brushManager.GetBrushCellPositionsAtMousePosition(mousePosition), false);
                }
            }
            else if(brushManager.selectionCursorActive)
            {
                if(Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
                {
                    brushManager.StartSelectionBoxDrag(mousePosition);
                }
                if (Input.GetMouseButton(0) && !EventSystem.current.IsPointerOverGameObject())
                {
                    brushManager.DragSelectionBox(mousePosition);
                }
                if (Input.GetMouseButtonUp(0))
                {
                    if(Input.GetButton(brushManager.saveQuickBrushShortcut))
                    {
                        brushManager.EndSelectionBoxDrag(false, true);
                    }
                    else
                    {
                        brushManager.EndSelectionBoxDrag(false, false);
                    }
                }
            }
        }
    }
    void FlipTileAtPosition(List<Vector3Int> brushCellPositions, bool flipTileOn)
    {
        //Debug.Log("attempting to flip at "+ mousePosition);
        if (!EventSystem.current.IsPointerOverGameObject())
        {
            foreach (Vector3Int brushCellPosition in brushCellPositions)
            {
                if (allTiles.ContainsKey(brushCellPosition) && allTiles[brushCellPosition] != flipTileOn)
                {
                    KillOrBirthCell(brushCellPosition, flipTileOn);
                }
            }
        }
    }
    Vector3Int GetTransformAtMousePosition()
    {
        Vector3Int mousePosition = Vector3Int.FloorToInt(gameCamera.ScreenToWorldPoint(Input.mousePosition));
        mousePosition.z = tilemapZAxisPosition; //this is because the camera z axis is not the same value as the tile z axis, and we want the tile z axis;
        return mousePosition;
    }

    /*
     * Cell Consideration
     */
    void KillOrBirthCell(Vector3Int tilePosition, bool isCellAlive)
    {
        ColorTile(tilePosition, tileMap, isCellAlive ? onColor : offColor);//Set to DeadColour when you implement it again

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
    /*
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
    */
    void PopulateAdjacentTilesDict(Vector3Int targetCell)
    {
        {
            adjacentTiles[0].tilePosition.Set(targetCell.x - 1, targetCell.y - 1, tilemapZAxisPosition);
            allTiles.TryGetValue(new Vector3Int(targetCell.x - 1, targetCell.y - 1, tilemapZAxisPosition), out bool isActive);
            adjacentTiles[0].isActive = isActive;
        }

        {
            adjacentTiles[1].tilePosition.Set(targetCell.x - 1, targetCell.y, tilemapZAxisPosition);
            allTiles.TryGetValue(new Vector3Int(targetCell.x - 1, targetCell.y, tilemapZAxisPosition), out bool isActive);
            adjacentTiles[1].isActive = isActive;
        }

        {
            adjacentTiles[2].tilePosition.Set(targetCell.x - 1, targetCell.y + 1, tilemapZAxisPosition);
            allTiles.TryGetValue(new Vector3Int(targetCell.x - 1, targetCell.y + 1, tilemapZAxisPosition), out bool isActive);
            adjacentTiles[2].isActive = isActive;
        }

        {
            adjacentTiles[3].tilePosition.Set(targetCell.x, targetCell.y - 1, tilemapZAxisPosition);
            allTiles.TryGetValue(new Vector3Int(targetCell.x, targetCell.y - 1, tilemapZAxisPosition), out bool isActive);
            adjacentTiles[3].isActive = isActive;
        }

        {
            adjacentTiles[4].tilePosition.Set(targetCell.x, targetCell.y + 1, tilemapZAxisPosition);
            allTiles.TryGetValue(new Vector3Int(targetCell.x, targetCell.y + 1, tilemapZAxisPosition), out bool isActive);
            adjacentTiles[4].isActive = isActive;
        }

        {
            adjacentTiles[5].tilePosition.Set(targetCell.x + 1, targetCell.y - 1, tilemapZAxisPosition);
            allTiles.TryGetValue(new Vector3Int(targetCell.x + 1, targetCell.y - 1, tilemapZAxisPosition), out bool isActive);
            adjacentTiles[5].isActive = isActive;
        }

        {
            adjacentTiles[6].tilePosition.Set(targetCell.x + 1, targetCell.y, tilemapZAxisPosition);
            allTiles.TryGetValue(new Vector3Int(targetCell.x + 1, targetCell.y, tilemapZAxisPosition), out bool isActive);
            adjacentTiles[6].isActive = isActive;
            //Debug.Log(adjacentTiles[6].tilePosition.ToString() + isActive6);
        }

        {
            adjacentTiles[7].tilePosition.Set(targetCell.x + 1, targetCell.y + 1, tilemapZAxisPosition);
            allTiles.TryGetValue(new Vector3Int(targetCell.x + 1, targetCell.y + 1, tilemapZAxisPosition), out bool isActive);
            adjacentTiles[7].isActive = isActive;
        }
    }
    void ClearAdjacentTilesDict()
    {
        
    }

    void ConsiderLivingCells()
    {
        foreach (var livingCell in livingCells)
        {
            //Dictionary<Vector3Int, bool> adjacentTiles = GetAdjacentTiles(livingCell.Key);
            PopulateAdjacentTilesDict(livingCell.Key);
            int livingAdjacentCells = 0;
            //Debug.Log("now measuring " + livingCell);
            foreach (TileArrayData adjacentTile in adjacentTiles)
            {
                if (adjacentTile.isActive == true)
                {
                    livingAdjacentCells += 1;
                }
                else if (adjacentTile.isActive == false && allTiles.ContainsKey(adjacentTile.tilePosition))//this is the second time we lookup if the value exists (first is getvalueordefault) OPTIMIZETHIS
                {
                    deadCellConsiderationDict.TryGetValue(adjacentTile.tilePosition, out int deadCellLivingAdjacentCells);
                    deadCellConsiderationDict[adjacentTile.tilePosition] = deadCellLivingAdjacentCells + 1;
                    //Debug.Log("dead cell to consider: " + cell.Key);
                }
            }
            if (livingAdjacentCells < 2 || livingAdjacentCells > 3)
            {
                MarkCellForLifeChange(livingCell.Key, false);
                //Debug.Log("Cell marked for death: " +  livingCell.Key + "living adjacent cells: " + livingAdjacentCells);
            }
        }
    }

    /*
    void ConsiderLivingCells()
    {
        foreach(var livingCell in livingCells)
        {
            //Dictionary<Vector3Int, bool> adjacentTiles = GetAdjacentTiles(livingCell.Key);
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
    */

    /*
    void ConsiderLivingCells()
    {
        foreach (var livingCell in livingCells)
        {
            int livingAdjacentCells = 0;
            Vector3Int keyToConsider = new Vector3Int(0,0,0);
            //Debug.Log("now measuring " + livingCell);
            for (int xPosition = livingCell.Key.x-1; xPosition <= livingCell.Key.x+1; xPosition++)//pair of loops iterates through every cell above and below, left and right of the current 'livingKey', and runs the appropriate code based on the cells value
            {
                for (int yPosition = livingCell.Key.y - 1; yPosition <= livingCell.Key.y + 1; yPosition++)
                {
                    keyToConsider.Set(xPosition,  yPosition, livingCell.Key.z);//
                    if((keyToConsider.x != livingCell.Key.x || keyToConsider.y != livingCell.Key.y) && allTiles.ContainsKey(keyToConsider))
                    {
                        if (allTiles[keyToConsider] == true)
                        {
                            livingAdjacentCells += 1;
                        }
                        else if (allTiles[keyToConsider] == false)//this is the second time we lookup if the value exists (first is getvalueordefault) OPTIMIZETHIS
                        {
                            deadCellConsiderationDict.TryGetValue(keyToConsider, out int deadCellLivingAdjacentCells);
                            deadCellConsiderationDict[keyToConsider] = deadCellLivingAdjacentCells + 1;
                            //Debug.Log("dead cell to consider: " + cell.Key);
                        }
                    }
                }
            }
            if (livingAdjacentCells < 2 || livingAdjacentCells > 3)
            {
                MarkCellForLifeChange(livingCell.Key, false);
                //Debug.Log("Cell marked for death: " +  livingCell.Key + "living adjacent cells: " + livingAdjacentCells);
            }
        }
    }
    */
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

    void UpdateBoardStateFromComputeShader()
    {
        Cell[] changedCellsSinceLastRoundStarted = mapComputeShaderManager.GetChangedCellsDataFromComputeShader();
        foreach (var cell in changedCellsSinceLastRoundStarted)
        {
            KillOrBirthCell(new Vector3Int(cell.cellPosition.x, cell.cellPosition.y, 0), cell.cellValue == 1 ? true : false);
        }

    }
    void RunCellLifeCycleLoop()
    {
        mapComputeShaderManager.TickConwaysGameOfLifeOnComputeShader();
        //ConsiderLivingCells();
        //ConsiderDeadCells();
        //UpdateCellLifeCycle();
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

