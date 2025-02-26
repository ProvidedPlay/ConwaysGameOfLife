using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Tilemaps;


public struct Cell // this, specifically, will store data about changed cells and their values
{
    public int2 cellPosition;
    public int cellValue;
}
public class MapComputeShaderManager : MonoBehaviour
{
    public GameManager gameManager;

    public RenderTexture mapRenderTexture; //note, this renderTexture is used for compute purposes only, not to actually render the board. Thats because the game board is resizeable and this texture maps one cell to exactly one pixel for compute purposes
    public GameObject gameOfLifeDisplayBoard;
    public Renderer gameBoardRenderer;
    public Tilemap gameBoardTilemap;

    public ComputeShader mapComputeShader;
    public ComputeBuffer allCellsBufferCPUSnapshot;
    public ComputeBuffer allCellsBufferCurrent;
    public ComputeBuffer allCellsBufferCurrentTemp;
    public ComputeBuffer changedCellsBuffer;
    public ComputeBuffer changedCellsCountBuffer;
    public ComputeBuffer allInputCellsBuffer;

    private int kernelIndexForInitializeGameBoard;
    private int kernelIndexForUpdateGameBoard;
    private int kernelIndexForWriteCurrentCellsBufferTempToCurrentCellsBuffer;
    private int kernelIndexForLoadInputCellsFromCPU;
    private int kernelIndexForCompareCurrentCellsBufferToCPUSnapshot;


    private int totalCellsInMap;
    private int mapWidth;
    private int mapHeight;

    public float4 defaultColourOff;
    public float4 defaultColourOn;

    public uint[] allCellsData;
    public Cell[] changedCellsData;

    void Awake()
    {
        UnpackReferences();
    }

    void UnpackReferences()
    {
        kernelIndexForInitializeGameBoard = mapComputeShader.FindKernel("InitializeGameBoard");
        kernelIndexForUpdateGameBoard = mapComputeShader.FindKernel("UpdateGameBoard");
        kernelIndexForLoadInputCellsFromCPU = mapComputeShader.FindKernel("LoadInputCellsFromCPU");
        kernelIndexForCompareCurrentCellsBufferToCPUSnapshot = mapComputeShader.FindKernel("CompareCurrentCellsBufferToCPUSnapshot");
        kernelIndexForWriteCurrentCellsBufferTempToCurrentCellsBuffer = mapComputeShader.FindKernel("WriteCurrentCellsBufferTempToCurrentCellsBuffer");

        gameOfLifeDisplayBoard = GameObject.FindGameObjectWithTag("GraphicsQuad");
        if(gameManager == null)
        {
            gameManager = GetComponent<GameManager>();
        }
        gameBoardTilemap = gameManager.tileMap;

        UpdateGraphicsColours();
    }

    public void CreateMapRenderTexture()
    {
        if (mapRenderTexture != null)
        {
            mapRenderTexture.Release();//if it already exists, destroy it
        }
        // Create the render texture that the compute shader will display the grid on
        mapRenderTexture = new RenderTexture(mapWidth, mapHeight, 0, RenderTextureFormat.ARGB32);
        mapRenderTexture.enableRandomWrite = true;
        mapRenderTexture.filterMode = FilterMode.Point;//pixel perfect
        mapRenderTexture.wrapMode = TextureWrapMode.Clamp;//dont repeat the texture
        mapRenderTexture.Create();

        if (gameBoardRenderer != null)
        {
            gameBoardRenderer.material.mainTexture = mapRenderTexture;
        }
    }

    public void FitGameBoardGraphicsQuadToMatchTilemap()
    {
        if (gameOfLifeDisplayBoard != null)
        {
            Vector3 tileSize = gameBoardTilemap.cellSize;
            
            float displayBoardWidth = mapWidth * tileSize.x;
            float displayBoardHeight = mapHeight * tileSize.y;

            gameOfLifeDisplayBoard.transform.localScale = new Vector3(displayBoardWidth, displayBoardHeight, 1f); //change size to match tilemap
            gameOfLifeDisplayBoard.transform.position = new Vector3(displayBoardWidth * 0.5f, displayBoardHeight * 0.5f, 0);
        }
    }

    public void ReleaseOldComputeBuffers()
    {
        allCellsBufferCPUSnapshot?.Release();// if allCellsBufferSnapshot != null, run .Release()
        allCellsBufferCurrent?.Release();
        allCellsBufferCurrentTemp?.Release();
        changedCellsBuffer?.Release();
        changedCellsCountBuffer?.Release();
        allInputCellsBuffer?.Release();
        
    }
    public void GenerateMap(int width, int height)
    {
        //update global map dimension variables
        mapWidth = width;
        mapHeight = height;
        totalCellsInMap = width * height;

        allCellsData = new uint[totalCellsInMap];

        //Release old compute buffers
        ReleaseOldComputeBuffers();

        //Create computebuffers to store cell data
        allCellsBufferCPUSnapshot = new ComputeBuffer(totalCellsInMap, sizeof(uint));  //Creates a new Compute buffer for use by a compute shader
                                                                                             //Structure: ComputeBuffer(int <count>, int<stride>)
                                                                                             //Parameters:
                                                                                             //              count: the number of elements in the buffer
                                                                                             //              stride: size of one element in the buffer (in bytes)
                                                                                             //                      must:
                                                                                             //                          be a multiple of 4,
                                                                                             //                          be less than 2040,
                                                                                             //                          match the size of the buffer type in shader (can calculate this using sizeOf method)
        allCellsBufferCurrent= new ComputeBuffer(totalCellsInMap, sizeof(uint));
        allCellsBufferCurrentTemp= new ComputeBuffer(totalCellsInMap, sizeof(uint));
        changedCellsBuffer = new ComputeBuffer(totalCellsInMap, sizeof(int) * 3);           // This size corresponds to the size of a Cell struct (an int2 for position and an int for value)
        changedCellsCountBuffer = new ComputeBuffer(1, sizeof(uint), ComputeBufferType.Counter); //ComputeBufferType.raw stores a typeless R32 value (32 bit)
        allInputCellsBuffer = new ComputeBuffer(totalCellsInMap, sizeof(int) * 3);//set a livingCellsBuffer to the size of the total cells in the map, since that will be its max size

        //Create the render texture that the compute shader will display the grid on
        CreateMapRenderTexture();

        //Set Compute Shader Values
        mapComputeShader.SetInt("gameBoardWidth", mapWidth);
        mapComputeShader.SetInt("gameBoardHeight", mapHeight);

        //Align the Game Board Renderer Quad to the transform of the tilemap
        FitGameBoardGraphicsQuadToMatchTilemap();

        //Set Compute Shader Buffer for each kernel that uses it
        mapComputeShader.SetBuffer(kernelIndexForInitializeGameBoard, "ChangedCellsCountBuffer", changedCellsCountBuffer);
        mapComputeShader.SetBuffer(kernelIndexForInitializeGameBoard, "AllCellsBufferCPUSnapshot", allCellsBufferCPUSnapshot);
        mapComputeShader.SetBuffer(kernelIndexForInitializeGameBoard, "AllCellsBufferCurrent", allCellsBufferCurrent);
        mapComputeShader.SetBuffer(kernelIndexForInitializeGameBoard, "AllCellsBufferCurrentTemp", allCellsBufferCurrentTemp);

        mapComputeShader.SetBuffer(kernelIndexForUpdateGameBoard, "AllCellsBufferCurrent", allCellsBufferCurrent);
        mapComputeShader.SetBuffer(kernelIndexForUpdateGameBoard, "AllCellsBufferCurrentTemp", allCellsBufferCurrentTemp);

        mapComputeShader.SetBuffer(kernelIndexForWriteCurrentCellsBufferTempToCurrentCellsBuffer, "AllCellsBufferCurrent", allCellsBufferCurrent);
        mapComputeShader.SetBuffer(kernelIndexForWriteCurrentCellsBufferTempToCurrentCellsBuffer, "AllCellsBufferCurrentTemp", allCellsBufferCurrentTemp);

        mapComputeShader.SetBuffer(kernelIndexForLoadInputCellsFromCPU, "AllCellsBufferCPUSnapshot", allCellsBufferCPUSnapshot);
        mapComputeShader.SetBuffer(kernelIndexForLoadInputCellsFromCPU, "AllCellsBufferCurrent", allCellsBufferCurrent);
        mapComputeShader.SetBuffer(kernelIndexForLoadInputCellsFromCPU, "AllInputCellsBuffer", allInputCellsBuffer);

        mapComputeShader.SetBuffer(kernelIndexForCompareCurrentCellsBufferToCPUSnapshot, "ChangedCellsCountBuffer", changedCellsCountBuffer);
        mapComputeShader.SetBuffer(kernelIndexForCompareCurrentCellsBufferToCPUSnapshot, "AllCellsBufferCPUSnapshot", allCellsBufferCPUSnapshot);
        mapComputeShader.SetBuffer(kernelIndexForCompareCurrentCellsBufferToCPUSnapshot, "AllCellsBufferCurrent", allCellsBufferCurrent);
        mapComputeShader.SetBuffer(kernelIndexForCompareCurrentCellsBufferToCPUSnapshot, "ChangedCellsBuffer", changedCellsBuffer);

        //Pass the renderTexture to the Compute shader
        mapComputeShader.SetTexture(kernelIndexForInitializeGameBoard, "MapRenderTexture", mapRenderTexture);
        mapComputeShader.SetTexture(kernelIndexForWriteCurrentCellsBufferTempToCurrentCellsBuffer, "MapRenderTexture", mapRenderTexture);

        //Set up the texture colours as float4 values
        UpdateGraphicsColours();

        //Dispatch the InitializeGameBoard kernel
        mapComputeShader.Dispatch(kernelIndexForInitializeGameBoard, Mathf.CeilToInt(mapWidth / 16.0f), Mathf.CeilToInt(mapHeight / 16.0f), 1);//Mathf.CeilToInt(f) returns the smallest int greater than or equal to f. So if height is 10 ,then 10/8f= 1.25, this returns 2. Why use this? Because it rounds UP instead of DOWN, that way theres always a thread batch calculating the stragglers if there are more cells than the nearest perfect group of 8x8. (otherwise the remainder cells wont be calculated))

    }

    public void UpdateGraphicsColours()
    {
        defaultColourOn = (Vector4)gameManager.onColor;
        defaultColourOff = (Vector4)gameManager.offColor;

        mapComputeShader.SetVector("defaultColourOff", defaultColourOff);
        mapComputeShader.SetVector("defaultColourOn", defaultColourOn);
    }

    public void TickConwaysGameOfLifeOnComputeShader()
    {
        //tick GOL once
        mapComputeShader.Dispatch(kernelIndexForUpdateGameBoard, Mathf.CeilToInt(mapWidth / 16.0f), Mathf.CeilToInt(mapHeight / 16.0f), 1);

        mapComputeShader.Dispatch(kernelIndexForWriteCurrentCellsBufferTempToCurrentCellsBuffer, Mathf.CeilToInt(mapWidth / 16.0f), Mathf.CeilToInt(mapHeight / 16.0f), 1);
    }
    public Cell[] GetChangedCellsDataFromComputeShader()
    {
        //Reset the change count buffer to 0
        changedCellsCountBuffer.SetCounterValue(0);


        // Dispatch the compute shader
        mapComputeShader.Dispatch(kernelIndexForCompareCurrentCellsBufferToCPUSnapshot, Mathf.CeilToInt(mapWidth * mapHeight / 256f), 1, 1);

        // Read back the count of changed cells after dispatch
        uint[] changedCount = new uint[1];
        changedCellsCountBuffer.GetData(changedCount);
        int numberOfChangedCells = (int)changedCount[0];

        // Prepare Cell array changedCellsData to recieve data from the compute shader's changedCellsBuffer after the async operation is complete, this is done by setting its array size to be equal to the changedCellsCount retrieved above
        changedCellsData = new Cell[numberOfChangedCells];
        changedCellsBuffer.GetData(changedCellsData);
        //RequestGOLDataFromComputeShader();            TODO get this code to work asynchronously to avoid delays
        //ProcessChangedCellsFromComputeShader();

        //clear changedCellsBuffer
        Cell[] emptyCellBuffer = new Cell[0];
        changedCellsBuffer.SetData(emptyCellBuffer);

        return changedCellsData;
    }

    public void GiveInputCellsToComputeShader(Dictionary<Vector3Int, bool> allInputCells)
    {
        //First, dispatch the InitializeGameBoard dispatch to clear the game board (not expensive on GPU ever),
        mapComputeShader.Dispatch(kernelIndexForInitializeGameBoard, Mathf.CeilToInt(mapWidth / 16.0f), Mathf.CeilToInt(mapHeight / 16.0f), 1);

        if (allInputCells.Count > totalCellsInMap)
        {
            Debug.Log("Warning, too many living cells! Increase buffer size!");
            return;
        }
        
        Cell[] allInputCellsData = new Cell[allInputCells.Count]; //This will store x and y pairs efficiently
        int allInputCellsDataIndex = 0;
        foreach(var cell in allInputCells)
        {

            allInputCellsData[allInputCellsDataIndex].cellPosition.x = cell.Key.x;
            allInputCellsData[allInputCellsDataIndex].cellPosition.y = cell.Key.y;

            allInputCellsData[allInputCellsDataIndex].cellValue = cell.Value == true? 1 : 0;

            allInputCellsDataIndex++;
        }

        //send the input cells to the GPU
        allInputCellsBuffer.SetData(allInputCellsData);

        //dispatch the compute shader kernel that will process the new living cells
        mapComputeShader.SetInt("numInputCells", allInputCells.Count);
        mapComputeShader.Dispatch(kernelIndexForLoadInputCellsFromCPU, Mathf.CeilToInt((allInputCells.Count + 1) / 256.0f), 1, 1);//run this in a single thread dimension of 256 threads
    }

    void OnDestroy()//prevents memory leak
    {
        ReleaseOldComputeBuffers();
    }
}
