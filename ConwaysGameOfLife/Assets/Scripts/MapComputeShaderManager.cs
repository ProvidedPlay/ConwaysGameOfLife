using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class MapComputeShaderManager : MonoBehaviour
{

    public ComputeShader mapComputeShader;
    //public RenderTexture mapRenderTexture; //note, this renderTexture is used for compute purposes only, not to actually render the board. Thats because the game board is resizeable and this texture maps one cell to exactly one pixel for compute purposes
    public ComputeBuffer allCellsBuffer;
    public ComputeBuffer livingCellsBuffer;

    private int kernelIndexForInitializeGameBoard;
    private int kernelIndexForUpdateGameBoard;
    private int kernelIndexForLoadLivingCellsFromCPU;

    private int totalCellsInMap;
    private int mapWidth;
    private int mapHeight;

    public uint[] allCellsData;
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
        allCellsBuffer = new ComputeBuffer(totalCellsInMap, sizeof(uint));  //Creates a new Compute buffer for use by a compute shader
                                                                                             //Structure: ComputeBuffer(int <count>, int<stride>)
                                                                                             //Parameters:
                                                                                             //              count: the number of elements in the buffer
                                                                                             //              stride: size of one element in the buffer (in bytes)
                                                                                             //                      must:
                                                                                             //                          be a multiple of 4,
                                                                                             //                          be less than 2040,
                                                                                             //                          match the size of the buffer type in shader (can calculate this using sizeOf method)
        livingCellsBuffer = new ComputeBuffer(totalCellsInMap, sizeof(int) * 2, ComputeBufferType.Structured);//set a livingCellsBuffer to the size of the total cells in the map, since that will be its max size

        //Set Compute Shader Values
        mapComputeShader.SetInt("gameBoardWidth", mapWidth);
        mapComputeShader.SetInt("gameBoardHeight", mapHeight);
        
        //Set Compute Shader Buffer for each kernel that uses it
        mapComputeShader.SetBuffer(kernelIndexForInitializeGameBoard, "AllCellsBuffer", allCellsBuffer);
        mapComputeShader.SetBuffer(kernelIndexForInitializeGameBoard, "LivingCellsBuffer", livingCellsBuffer);

        mapComputeShader.SetBuffer(kernelIndexForUpdateGameBoard, "AllCellsBuffer", allCellsBuffer);
        mapComputeShader.SetBuffer(kernelIndexForUpdateGameBoard, "LivingCellsBuffer", livingCellsBuffer);

        mapComputeShader.SetBuffer(kernelIndexForLoadLivingCellsFromCPU, "AllCellsBuffer", allCellsBuffer);
        mapComputeShader.SetBuffer(kernelIndexForLoadLivingCellsFromCPU, "LivingCellsBuffer", livingCellsBuffer);

        //Dispatch the InitializeGameBoard kernel
        mapComputeShader.Dispatch(kernelIndexForInitializeGameBoard, Mathf.CeilToInt(mapWidth / 16.0f), Mathf.CeilToInt(mapHeight / 16.0f), 1);//Mathf.CeilToInt(f) returns the smallest int greater than or equal to f. So if height is 10 ,then 10/8f= 1.25, this returns 2. Why use this? Because it rounds UP instead of DOWN, that way theres always a thread batch calculating the stragglers if there are more cells than the nearest perfect group of 8x8. (otherwise the remainder cells wont be calculated))
    }

    /*
    public void GetLivingCellsFromComputeShader()
    {
        int textureWidth = mapRenderTexture.width;
        int textureHeight = mapRenderTexture.height;

        //create a CPU readable Texture2D to store data (it will exactly match the RenderTexture data)
        Texture2D outputMapRenderTexture = new Texture2D(textureWidth, textureHeight, TextureFormat.ARGB32, false); // the bool 'false' is for the mipchain, which determines if mipmaps should be generated for the texture; "TextureFormat.R8" means the texture will be single channel (only one color channel) in an 8-bit format (uses least data since we only want the bool value of the cell aka 0/1)

        //Copy GPU AllCellsGrid to the CPU-accessible outputMapRenderTexture
        Graphics.CopyTexture(mapRenderTexture, outputMapRenderTexture);

        //Read the texture's picture data in the form of an array of bytes (bytes are 8-bit, very basic data form that takes up less space than an int)
        var rawTextureData = outputMapRenderTexture.GetRawTextureData();

        //Convert the byte array into grid values (0=dead, 1=alive); add all living cells to a Vector2 list called LivingCellsList
        List<Vector2Int> livingCellCoords = new List<Vector2Int>();
        for (int y = 0; y < textureHeight; y++)
        {
            for (int x = 0; x < textureWidth; x++)
            {
                int cellIndex = y * textureWidth + x;
                byte cellState = rawTextureData[cellIndex];

                if (cellState == 0)
                {
                    livingCellCoords.Add(new Vector2Int(x, y));
                }
                Debug.Log(cellIndex + " : " + cellState);
            }
        }

    }
    public void GetLivingCellsFromComputeShader()
    {
        int textureWidth = mapRenderTexture.width;
        int textureHeight = mapRenderTexture.height;

        // Create a CPU-readable Texture2D
        Texture2D outputMapRenderTexture = new Texture2D(textureWidth, textureHeight, TextureFormat.R8, false);

        // Bind the RenderTexture before reading
        RenderTexture.active = mapRenderTexture;
        outputMapRenderTexture.ReadPixels(new Rect(0, 0, textureWidth, textureHeight), 0, 0);//reads pixels from the currently active render texture (mapRenderTexture) and writes them to a target texture (outputMapRenderTexture). The rect defines the portion of the texture being read/copied to (in this case the whole thing)
        outputMapRenderTexture.Apply(); // Apply to store changes in CPU memory
        RenderTexture.active = null; // Unbind the RenderTexture

        // Read raw texture data as bytes
        Color32[] rawTextureData = outputMapRenderTexture.GetPixels32();

        // Convert the byte array into grid values
        List<Vector2Int> livingCellCoords = new List<Vector2Int>();
        for (int y = 0; y < textureHeight; y++)
        {
            for (int x = 0; x < textureWidth; x++)
            {
                int cellIndex = y * textureWidth + x;
                Color32 cellStateColor = rawTextureData[cellIndex]; // 0 or 255

                if (cellStateColor.r == 255)
                {
                    livingCellCoords.Add(new Vector2Int(x, y));
                }
                Debug.Log(cellIndex + " : " + cellStateColor);
            }
        }

        Debug.Log(livingCellCoords.Count);
    }
   
    public void GetLivingCellsFromComputeShader()
    {
        int textureWidth = mapRenderTexture.width;
        int textureHeight = mapRenderTexture.height;

        // Create a CPU-readable Texture2D
        Texture2D outputMapRenderTexture = new Texture2D(textureWidth, textureHeight, TextureFormat.R8, false);

        // Bind the RenderTexture before reading
        
        RenderTexture.active = mapRenderTexture;
        outputMapRenderTexture.ReadPixels(new Rect(0, 0, textureWidth, textureHeight), 0, 0);//reads pixels from the currently active render texture (mapRenderTexture) and writes them to a target texture (outputMapRenderTexture). The rect defines the portion of the texture being read/copied to (in this case the whole thing)
        outputMapRenderTexture.Apply(); // Apply to store changes in CPU memory
        RenderTexture.active = null; // Unbind the RenderTexture
        
        //Graphics.CopyTexture(mapRenderTexture, outputMapRenderTexture);
       // outputMapRenderTexture.Apply();
        // Read raw texture data as bytes
        byte[] rawTextureData = outputMapRenderTexture.GetRawTextureData();

        // Convert the byte array into grid values
        List<Vector2Int> livingCellCoords = new List<Vector2Int>();
        for (int y = 0; y < textureHeight; y++)
        {
            for (int x = 0; x < textureWidth; x++)
            {
                int cellIndex = y * textureWidth + x;
                byte cellState = rawTextureData[cellIndex]; // 0 or 255

                if (cellState == 255)
                {
                    livingCellCoords.Add(new Vector2Int(x, y));
                }
                Debug.Log(cellIndex + " : " + cellState);
            }
        }

        Debug.Log(livingCellCoords.Count);
    }
    */
    public void TickConwaysGameOfLifeOnComputeShader()
    {
        if(!gpuUpdateRequestInProgress)
        {
            //tick GOL once
            mapComputeShader.Dispatch(kernelIndexForUpdateGameBoard, Mathf.CeilToInt(mapWidth / 16.0f), Mathf.CeilToInt(mapHeight / 16.0f), 1);

            // Request async readback ; this will request data on the gpu by running the OnGPUDataRecieved method asyncronously. When the data is recieved (aka request.GetData<uint>().CopyTo(cellData), that method will run, and the tilemap will be updated
            gpuUpdateRequestInProgress = true;
            AsyncGPUReadback.Request(allCellsBuffer, OnGPUDataRecieved);
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

        //if the request has no error, copy the data from the GPU's AllCellsBuffer to the CPUs  allCellsData[] array
        request.GetData<uint>().CopyTo(allCellsData);

        gpuUpdateRequestInProgress = false; //this line runs after the previous line is finished

        //allCellsData is now ready to be read by the CPU

        //run temporary test GetLivingCellsFromComputeShader
        GetLivingCellsFromComputeShader();
    }

    public void GetLivingCellsFromComputeShader()
    {
        //Create a temporary array of uint[] to store data from the compute shader buffer, write the data from the allCellsBuffer into the cell data uint[] to be read by the CPU
        //allCellsData = new uint[totalCellsInMap];
        //allCellsBuffer.GetData(allCellsData);

        
        int livingCellsCount = 0;
        for (int i = 0; i < allCellsData.Length; i++)
        {
            if (allCellsData[i] == 1)
            {
                livingCellsCount++;
            }
        }

        Debug.Log("All cells: " + allCellsData.Length);
        Debug.Log("Living cells: " + livingCellsCount);
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
        if (allCellsBuffer != null)
        {
            allCellsBuffer.Release();

        }
    }
}
