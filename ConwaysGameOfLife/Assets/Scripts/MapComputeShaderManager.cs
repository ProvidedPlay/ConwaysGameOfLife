using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;


public struct Cell // this, specifically, will store data about changed cells and their values
{
    public int2 cellPosition;
    public int cellValue;
}
public class MapComputeShaderManager : MonoBehaviour
{

    public ComputeShader mapComputeShader;
    //public RenderTexture mapRenderTexture; //note, this renderTexture is used for compute purposes only, not to actually render the board. Thats because the game board is resizeable and this texture maps one cell to exactly one pixel for compute purposes
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
    }

     
    public void GenerateMapRenderTexture(int width, int height)
    {
        //Create a RenderTexture Grid of the appropriate Size
       // mapRenderTexture = new RenderTexture(width, height, 1, RenderTextureFormat.R8); //rendertextureformat.r8 stores the texture in the form of an 8-bit integer (max 256 values), which is fine because we're only using 0 and 1 for if the cell is alive or dead
       // mapRenderTexture.enableRandomWrite = true;
        //mapRenderTexture.Create();

        /*
        //Initialize Cell Value Data to be used in the Grid
        uint[] initialCellData = new uint[width * height];
        for (int i = 0; i < width * height; i++)
        {
            initialCellData[i] = 0; //Alive = 1, Dead = 0
        }
        */

        //Set this rendertexture to be the compute shader's AllCellsGrid; the texture must be individually bound to all kernels; note: upon binding, all changes to AllCellsGrid in the compute shader will also be made on the mapRenderTexture too
        //mapComputeShader.SetTexture(kernelIndexForInitializeGameBoard, "AllCellsGrid", mapRenderTexture);
        //mapComputeShader.SetTexture(kernelIndexForUpdateGameBoard, "AllCellsGrid", mapRenderTexture);

        //run the method of the compute shader called "InitializeGameBoard"
        //mapComputeShader.Dispatch(kernelIndexForInitializeGameBoard, Mathf.CeilToInt(width / 8.0f), Mathf.CeilToInt(height / 8.0f), 1);//Mathf.CeilToInt(f) returns the smallest int greater than or equal to f. So if height is 10 ,then 10/8f= 1.25, this returns 2. Why use this? Because it rounds UP instead of DOWN, that way theres always a thread batch calculating the stragglers if there are more cells than the nearest perfect group of 8x8. (otherwise the remainder cells wont be calculated)
    }

    public void GenerateMap(int width, int height)
    {
        //update global map dimension variables
        mapWidth = width;
        mapHeight = height;
        totalCellsInMap = width * height;

        allCellsData = new uint[totalCellsInMap];

        //Create a computebuffer to store all cells
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


        //Set Compute Shader Values
        mapComputeShader.SetInt("gameBoardWidth", mapWidth);
        mapComputeShader.SetInt("gameBoardHeight", mapHeight);
        
        //Set Compute Shader Buffer for each kernel that uses it
        mapComputeShader.SetBuffer(kernelIndexForInitializeGameBoard, "AllCellsBufferCPUSnapshot", allCellsBufferCPUSnapshot);
        mapComputeShader.SetBuffer(kernelIndexForInitializeGameBoard, "AllCellsBufferCurrent", allCellsBufferCurrent);
        mapComputeShader.SetBuffer(kernelIndexForInitializeGameBoard, "LivingCellsBuffer", livingCellsBuffer);

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

        //Dispatch the InitializeGameBoard kernel
        mapComputeShader.Dispatch(kernelIndexForInitializeGameBoard, Mathf.CeilToInt(mapWidth / 16.0f), Mathf.CeilToInt(mapHeight / 16.0f), 1);//Mathf.CeilToInt(f) returns the smallest int greater than or equal to f. So if height is 10 ,then 10/8f= 1.25, this returns 2. Why use this? Because it rounds UP instead of DOWN, that way theres always a thread batch calculating the stragglers if there are more cells than the nearest perfect group of 8x8. (otherwise the remainder cells wont be calculated))

        Debug.Log("uint 2 size " + sizeof(int) + "int size: " + sizeof(uint));
    }

    public void TickConwaysGameOfLifeOnComputeShader()
    {
        //tick GOL once
        mapComputeShader.Dispatch(kernelIndexForUpdateGameBoard, Mathf.CeilToInt(mapWidth / 16.0f), Mathf.CeilToInt(mapHeight / 16.0f), 1);
    }
    public void GetChangedCellsDataFromComputeShader()
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
        mapComputeShader.Dispatch(kernelIndexForInitializeGameBoard, Mathf.CeilToInt(mapWidth / 16.0f), Mathf.CeilToInt(mapHeight / 16.0f), 1);
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
