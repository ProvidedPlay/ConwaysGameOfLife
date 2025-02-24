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
    public ComputeBuffer changedCellsBuffer;
    public ComputeBuffer changedCellsCountBuffer;
    public ComputeBuffer livingCellsBuffer;

    private int kernelIndexForInitializeGameBoard;
    private int kernelIndexForUpdateGameBoard;
    private int kernelIndexForLoadLivingCellsFromCPU;
    private int kernelIndexForCompareCurrentCellsBufferToCPUSnapshot;

    private int totalCellsInMap;
    private int mapWidth;
    private int mapHeight;

    public uint[] allCellsData;
    public Cell[] changedCellsData;
    private bool gpuUpdateRequestInProgress = false;

    void Awake()
    {
        UnpackReferences();
    }

    void UnpackReferences()
    {
        kernelIndexForInitializeGameBoard = mapComputeShader.FindKernel("InitializeGameBoard");
        kernelIndexForUpdateGameBoard = mapComputeShader.FindKernel("UpdateGameBoard");
        kernelIndexForLoadLivingCellsFromCPU = mapComputeShader.FindKernel("LoadLivingCellsFromCPU");
        kernelIndexForCompareCurrentCellsBufferToCPUSnapshot = mapComputeShader.FindKernel("CompareCurrentCellsBufferToCPUSnapshot");

        gameOfLifeDisplayBoard = GameObject.FindGameObjectWithTag("GraphicsQuad");
        if(gameManager == null)
        {
            gameManager = GetComponent<GameManager>();
        }
        gameBoardTilemap = gameManager.tileMap;

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

    public void GenerateMap(int width, int height)
    {
        //update global map dimension variables
        mapWidth = width;
        mapHeight = height;
        totalCellsInMap = width * height;

        allCellsData = new uint[totalCellsInMap];

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
        changedCellsBuffer = new ComputeBuffer(totalCellsInMap, sizeof(int) * 3);           // This size corresponds to the size of a Cell struct (an int2 for position and an int for value)
        changedCellsCountBuffer = new ComputeBuffer(1, sizeof(uint), ComputeBufferType.Raw); //ComputeBufferType.raw stores a typeless R32 value (32 bit)
        livingCellsBuffer = new ComputeBuffer(totalCellsInMap, sizeof(int) * 2, ComputeBufferType.Structured);//set a livingCellsBuffer to the size of the total cells in the map, since that will be its max size

        //Create the render texture that the compute shader will display the grid on
        CreateMapRenderTexture();

        //Set Compute Shader Values
        mapComputeShader.SetInt("gameBoardWidth", mapWidth);
        mapComputeShader.SetInt("gameBoardHeight", mapHeight);

        //Align the Game Board Renderer Quad to the transform of the tilemap
        FitGameBoardGraphicsQuadToMatchTilemap();
        
        //Set Compute Shader Buffer for each kernel that uses it
        mapComputeShader.SetBuffer(kernelIndexForInitializeGameBoard, "AllCellsBufferCPUSnapshot", allCellsBufferCPUSnapshot);
        mapComputeShader.SetBuffer(kernelIndexForInitializeGameBoard, "AllCellsBufferCurrent", allCellsBufferCurrent);
        mapComputeShader.SetBuffer(kernelIndexForInitializeGameBoard, "LivingCellsBuffer", livingCellsBuffer);
        mapComputeShader.SetBuffer(kernelIndexForInitializeGameBoard, "ChangedCellsCountBuffer", changedCellsCountBuffer);

        mapComputeShader.SetBuffer(kernelIndexForUpdateGameBoard, "AllCellsBufferCPUSnapshot", allCellsBufferCPUSnapshot);
        mapComputeShader.SetBuffer(kernelIndexForUpdateGameBoard, "AllCellsBufferCurrent", allCellsBufferCurrent);
        mapComputeShader.SetBuffer(kernelIndexForUpdateGameBoard, "LivingCellsBuffer", livingCellsBuffer);
        mapComputeShader.SetBuffer(kernelIndexForUpdateGameBoard, "ChangedCellsBuffer", changedCellsBuffer);

        mapComputeShader.SetBuffer(kernelIndexForLoadLivingCellsFromCPU, "AllCellsBufferCPUSnapshot", allCellsBufferCPUSnapshot);
        mapComputeShader.SetBuffer(kernelIndexForLoadLivingCellsFromCPU, "AllCellsBufferCurrent", allCellsBufferCurrent);
        mapComputeShader.SetBuffer(kernelIndexForLoadLivingCellsFromCPU, "LivingCellsBuffer", livingCellsBuffer);

        mapComputeShader.SetBuffer(kernelIndexForCompareCurrentCellsBufferToCPUSnapshot, "ChangedCellsCountBuffer", changedCellsCountBuffer);
        mapComputeShader.SetBuffer(kernelIndexForCompareCurrentCellsBufferToCPUSnapshot, "AllCellsBufferCPUSnapshot", allCellsBufferCPUSnapshot);
        mapComputeShader.SetBuffer(kernelIndexForCompareCurrentCellsBufferToCPUSnapshot, "AllCellsBufferCurrent", allCellsBufferCurrent);
        mapComputeShader.SetBuffer(kernelIndexForCompareCurrentCellsBufferToCPUSnapshot, "ChangedCellsBuffer", changedCellsBuffer);

        //Pass the renderTexture to the Compute shader
        mapComputeShader.SetTexture(kernelIndexForInitializeGameBoard, "MapRenderTexture", mapRenderTexture);
        mapComputeShader.SetTexture(kernelIndexForUpdateGameBoard, "MapRenderTexture", mapRenderTexture);
        
        //Dispatch the InitializeGameBoard kernel
        mapComputeShader.Dispatch(kernelIndexForInitializeGameBoard, Mathf.CeilToInt(mapWidth / 16.0f), Mathf.CeilToInt(mapHeight / 16.0f), 1);//Mathf.CeilToInt(f) returns the smallest int greater than or equal to f. So if height is 10 ,then 10/8f= 1.25, this returns 2. Why use this? Because it rounds UP instead of DOWN, that way theres always a thread batch calculating the stragglers if there are more cells than the nearest perfect group of 8x8. (otherwise the remainder cells wont be calculated))

        //Set Up the GOL Map Quad


        Debug.Log("uint 2 size " + sizeof(int) + "int size: " + sizeof(uint));
    }

    public void TickConwaysGameOfLifeOnComputeShader()
    {
        //tick GOL once
        mapComputeShader.Dispatch(kernelIndexForUpdateGameBoard, Mathf.CeilToInt(mapWidth / 16.0f), Mathf.CeilToInt(mapHeight / 16.0f), 1);
    }
    public Cell[] GetChangedCellsDataFromComputeShader()
    {
        // Reset change count buffer
        uint[] resetCounter = new uint[1] { 0 };
        changedCellsCountBuffer.SetData(resetCounter);


        // Dispatch the compute shader
        mapComputeShader.Dispatch(kernelIndexForCompareCurrentCellsBufferToCPUSnapshot, Mathf.CeilToInt(mapWidth / 16f), Mathf.CeilToInt(mapHeight / 16f), 1);

        // Read back the count of changed cells after dispatch
        uint[] changedCount = new uint[1];
        changedCellsCountBuffer.GetData(changedCount);
        int numberOfChangedCells = (int)changedCount[0];

        // Prepare Cell array changedCellsData to recieve data from the compute shader's changedCellsBuffer after the async operation is complete, this is done by setting its array size to be equal to the changedCellsCount retrieved above
        changedCellsData = new Cell[numberOfChangedCells];
        changedCellsBuffer.GetData(changedCellsData);
        //RequestGOLDataFromComputeShader();            TODO get this code to work asynchronously to avoid delays
        ProcessChangedCellsFromComputeShader();

        //clear changedCellsBuffer
        Cell[] emptyCellBuffer = new Cell[0];
        changedCellsBuffer.SetData(emptyCellBuffer);

        return changedCellsData;
    }
    public void RequestGOLDataFromComputeShader()
    {
        if(!gpuUpdateRequestInProgress)
        {
            //tick GOL once (this code has been moved)
            //mapComputeShader.Dispatch(kernelIndexForUpdateGameBoard, Mathf.CeilToInt(mapWidth / 16.0f), Mathf.CeilToInt(mapHeight / 16.0f), 1);

            // Request async readback ; this will request data on the gpu by running the OnGPUDataRecieved method asyncronously. When the data is recieved (aka request.GetData<uint>().CopyTo(cellData), that method will run, and the tilemap will be updated
            gpuUpdateRequestInProgress = true;
            AsyncGPUReadback.Request(changedCellsBuffer, OnGPUDataRecieved);
        }
    }

    void OnGPUDataRecieved(AsyncGPUReadbackRequest request)
    {
        if(request.hasError)
        {
            Debug.Log("GPU Readback error!");
            gpuUpdateRequestInProgress=false;
            return;
        }

        //if the request has no error, copy the data from the GPU's AllCellsBuffer to the CPUs  changedCellsData[] array
        request.GetData<Cell>().CopyTo(changedCellsData);

        gpuUpdateRequestInProgress = false; //this line runs after the previous line is finished

        //allCellsData is now ready to be read by the CPU

        //run temporary test GetLivingCellsFromComputeShader
        ProcessChangedCellsFromComputeShader();
    }

    public void ProcessChangedCellsFromComputeShader()
    {
        //Create a temporary array of uint[] to store data from the compute shader buffer, write the data from the allCellsBuffer into the cell data uint[] to be read by the CPU
        //allCellsData = new uint[totalCellsInMap];
        //allCellsBuffer.GetData(allCellsData);

        
        int netCellsBirthed = 0;
        int netCellsKilled = 0;
        for (int i = 0; i < changedCellsData.Length; i++)
        {
            if (changedCellsData[i].cellValue == 1)
            {
                netCellsBirthed++;
            }
            if (changedCellsData[i].cellValue == 0)
                {
                    netCellsKilled++;
                }
            }
        


        Debug.Log("All cells: " + allCellsData.Length+ ".   Net Cells Birthed: " + netCellsBirthed + "      .Net Cells Killed: " + netCellsKilled);
    }

    public void GiveLivingCellsToComputeShader(Dictionary<Vector3Int, bool> livingCells)
    {
        mapComputeShader.Dispatch(kernelIndexForInitializeGameBoard, Mathf.CeilToInt(mapWidth / 16.0f), Mathf.CeilToInt(mapHeight / 16.0f), 1);

        if (livingCells.Count > totalCellsInMap)
        {
            Debug.Log("Warning, too many living cells! Increase buffer size!");
            return;
        }
        
        int[] livingCellsData = new int[livingCells.Count * 2]; //This will store x and y pairs efficiently
        int livingCellsDictionaryIndex = 0;
        foreach (var entry in livingCells)
        {
            // Access the x and y values from Vector3Int key and write it to the livingcellsdata array,
            livingCellsData[livingCellsDictionaryIndex * 2] = entry.Key.x; // this stores all the x values as all the odd indexes in the array
            livingCellsData[livingCellsDictionaryIndex * 2 + 1] = entry.Key.y; //this stores all the y values as all the even indexes in the array
            livingCellsDictionaryIndex++;
        }

        //send the living cells to the GPU
        livingCellsBuffer.SetData(livingCellsData);

        //First, dispatch the InitializeGameBoard dispatch to clear the game board (not expensive on GPU ever), then dispatch the compute shader kernel that will process the new living cells
        mapComputeShader.SetInt("numLivingCells", livingCells.Count);
        mapComputeShader.Dispatch(kernelIndexForLoadLivingCellsFromCPU, Mathf.CeilToInt(livingCells.Count + 1 / 256.0f), 1, 1);//run this in a single thread dimension of 256 threads
    }



    void OnDestroy()//prevents memory leak
    {
        if (allCellsBufferCPUSnapshot != null)
        {
            allCellsBufferCPUSnapshot.Release();

        }
        if (allCellsBufferCurrent != null)
        {
            allCellsBufferCurrent.Release();

        }
        if (changedCellsBuffer != null)
        {
            changedCellsBuffer.Release();

        }
        if (livingCellsBuffer != null)
        {
            livingCellsBuffer.Release();

        }
    }
}
