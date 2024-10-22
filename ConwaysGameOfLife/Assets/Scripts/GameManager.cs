using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Tilemaps;

public class GameManager : MonoBehaviour
{
    public Tilemap tileMap;
    public Tilemap overlayGridTileMap;
    public TilemapRenderer overlayGridTileMapRenderer;
    public TileMapManager tileMapManager;
    public TileBase defaultTile;
    public TileBase gridTile;

    public bool proceedToNextGameState;
    public string proceedToNextGameStateShortcut;

    public Color onColor = Color.white;
    public Color offColor = Color.black;

    public int tilemapZAxisPosition;
    public float minimumTickIntervalTime;

    public Dictionary<Vector3Int, bool> allTiles = new(); //Dict key = transform, value = isLiving bool
    public Dictionary<Vector3Int, bool> selectedTiles = new();
    public Dictionary<Vector3Int, bool> livingCells = new();
    public Dictionary<Vector3Int, bool> deadCellConsiderationDict = new();
    public Dictionary<Vector3Int, bool> cellsMarkedForLifeChange = new();

    public enum GameState
    {
        GameSetup,
        GamePlaying,
        GameOver
    }
    public GameState gameState;

    [Range(-1, 1)]
    public float portionOfLivingStartCells;

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
        /*
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
        if(gameState.ToString() == "GamePlaying")
        {
            RunGame();
        }
        */

        if (Input.GetButtonDown(proceedToNextGameStateShortcut))
        {
            proceedToNextGameState = true;
        }
    }
    void UnpackReferences()
    {
        tileMap = GameObject.FindGameObjectWithTag("Game Board").GetComponent<Tilemap>();
        overlayGridTileMap = GameObject.FindGameObjectWithTag("Overlay Grid").GetComponent<Tilemap>();
        overlayGridTileMapRenderer = GameObject.FindGameObjectWithTag("Overlay Grid").GetComponent<TilemapRenderer>();
        tileMapManager = GetComponent<TileMapManager>();

    }

    void SetUpGameBoard()
    {
        InstantiateTilesPositionArray();
        /*
        if (gameState.ToString() == "GameSetup")
        {
            InstantiateTilesPositionArray();
        }
        */
    }

    void InstantiateTilesPositionArray()
    {
        if(tileMapManager != null && tileMap != null && defaultTile != null &&allTiles!=null)
        {
            allTiles.Clear();
            foreach(var tilePosition in tileMapManager.tileMapBounds.allPositionsWithin)
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
    void ColorTile(Vector3Int tilePosition, Tilemap tileMap, Color desiredColor)
    {
            tileMap.SetTileFlags(tilePosition, TileFlags.None);
            tileMap.SetColor(tilePosition, desiredColor);
    }

    void RandomizeAllColors()
    {
        foreach(var tile in allTiles)
        {
            //ColorTile(tile.Key, tileMap, Random.Range(-1f, 1f) > portionOfLivingStartCells ? offColor : onColor);
            KillOrBirthCell(tile.Key, Random.Range(-1f, 1f) < portionOfLivingStartCells);
        }
    }
    void KillOrBirthCell(Vector3Int tilePosition, bool isCellAlive)
    {
        //allTiles[tilePosition] = isCellAlive;
        ColorTile(tilePosition, tileMap, isCellAlive ? onColor : offColor);

        if (isCellAlive )
        {
            livingCells[tilePosition] = true;
            allTiles[tilePosition] = true;
        }
        else if(!isCellAlive)
        {
            livingCells.Remove(tilePosition);
            allTiles[tilePosition ] = false;
        }
    }
    /*
     * 
     * COROUTINES
     *      WRITE COROUTINE FORM:   <accessmodifier> IEnumerator <CoroutineName>()
     *      CALL COROUTINE FORM:    StartCoroutine(<CoroutineName>())              
     *              
     *                                                              //'yield <condition/code>': stops coroutine, waits for 'condition/code' to be met, resumes coroutine
     */

    private void GameStart()
    {
        SetUpGameBoard();
        StartCoroutine(GameLoop());
    }

    private IEnumerator GameLoop()
    {
        yield return StartCoroutine(GameSetup());
        yield return StartCoroutine(GamePlaying());
        yield return StartCoroutine(RoundEnding());
        Debug.Log("It all ended");
        GameStart();
    }

    private IEnumerator GameSetup()
    {
        Debug.Log("Coroutine state: game setup");
        ToggleOverlayGrid(true);
        while (!proceedToNextGameState)
        {
            //code in here will run during this game state

            CustomizeGameBoard();
            yield return null;
        }
        //ConsiderLivingCells();//this is a test, remove when actually implementing
        yield return new WaitForEndOfFrame();
        proceedToNextGameState = false;
    }
    private IEnumerator GamePlaying()
    {
        Debug.Log("Coroutine state: game playing");
        ClearSelectedTiles();
        ToggleOverlayGrid(false);
        while (!proceedToNextGameState)
        {
            //code in here will run during this game state
            //RandomizeAllColors();
            /*
            if (Input.GetKeyDown(KeyCode.LeftControl)){
                RunCellLifeCycleLoop();
            }
            */
            RunCellLifeCycleLoop();

            yield return new WaitForSeconds(minimumTickIntervalTime);
            //yield return null;
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
     * 
     * Game State Methods
     * 
     */
    void CustomizeGameBoard()
    {
        if (Input.GetMouseButton(0))
        {
            Vector3Int mousePosition = GetTransformAtMousePosition();

            FlipTileAtPosition(mousePosition);
        }
        if(Input.GetMouseButtonUp(0))
        {
            ClearSelectedTiles();
        }
    }

    void FlipTileAtPosition(Vector3Int mousePosition)
    {
        //Debug.Log("attempting to flip at "+ mousePosition);
        if (!selectedTiles.ContainsKey(mousePosition) && allTiles.ContainsKey(mousePosition))
        {
            //Debug.Log(mousePosition + " flipped");
            //allTiles[mousePosition] = !allTiles[mousePosition]; OLD
            KillOrBirthCell(mousePosition, !allTiles[mousePosition]);
            AddToSelectedTiles(mousePosition, allTiles[mousePosition]);
            //Color desiredColor = allTiles[mousePosition] == true ? onColor : offColor; OLD
            //ColorTile(mousePosition, tileMap, desiredColor);OLD
        }

    }
    /*
     * Tile Selector Methods
     */
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
     * create a dict of all living cells (livingCellConsiderationDict)
     * for each cell in LivingCellConsiderationDict
     *      for each neighboring cell (consider all cells around it)
     *          if living: 
     *              add to livingAdjacentCellCount
     *          if dead: 
     *              add to deadCellConsiderationDict
     *      determineIfCurrentCellSurvives(livingAdjacentCellCount)
     *          if livingAdjacentCellCount != 2,3
     *              MarkCellForLifeChange(false) aka kill the cell
     * clear livingCellConsiderationDict
     * 
     * for each cell in deadCellConsiderationDict
     *      for each neighboring cell (consider all cells around it)
     *          if living:
     *              add to livingAdjacentCellCount
     *      determineIfCurrentCellIsBorn(livingAdjacentCellCount)
     *          if livingAdjacentCellCount = 3
     *              MarkCellForLifeChange(true)aka birth the cell
     *clear deadCellConsiderationDict
     *              
     *MarkCellForLifeChange(bool markedForLife, Vector3Int cellPosition)
     *  cellsMarkedForLifeChange[cellPosition] = markedForLife
     * 
     *UpdateCellLifeCycle
     *  forEach cellMarkedForLifeChange in cellsMarkedForLifeChange
     *      allTiles[cellMarkedForLifeChange] = cellsMarkedForLifeChange[cellMarkedForLifeChange]
     *      changeColor(cellMarkedForLifeChange, tilemap, cellsMarkedForLifeChange[cellMarkedForLifeChanged] == true? onColor : offColor
     *cellsMarkedForLifeChange.Clear()
     *      
     *  
     *              
     *      
     */

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
                    deadCellConsiderationDict[cell.Key] = true;
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
            Dictionary<Vector3Int, bool> adjacentTiles = GetAdjacentTiles(deadCell.Key);
            int livingAdjacentCells = 0;
            foreach (var cell in adjacentTiles)
            {
                if (cell.Value == true)
                {
                    livingAdjacentCells += 1;
                }
            }
            if (livingAdjacentCells == 3)
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
}

