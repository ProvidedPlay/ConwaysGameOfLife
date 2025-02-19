using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapComputeShaderManager : MonoBehaviour
{

    public ComputeShader mapComputeShader;
    public RenderTexture mapRenderTexture; //note, this renderTexture is used for compute purposes only, not to actually render the board. Thats because the game board is resizeable and this texture maps one cell to exactly one pixel for compute purposes

    private int kernelIndexForInitializeGameBoard;
    private int kernelIndexForUpdateGameBoard;
    void Start()
    {
        UnpackReferences();
    }

    void UnpackReferences()
    {
        kernelIndexForInitializeGameBoard = mapComputeShader.FindKernel("InitializeGameBoard");
        kernelIndexForUpdateGameBoard = mapComputeShader.FindKernel("UpdateGameBoard");
    }

    public void GenerateMap(int width, int height)
    {
        //Create a RenderTexture Grid of the appropriate Size
        mapRenderTexture = new RenderTexture(width, height, 1, RenderTextureFormat.ARGB32); //rendertextureformat.r8 stores the texture in the form of an 8-bit integer (max 256 values), which is fine because we're only using 0 and 1 for if the cell is alive or dead
        mapRenderTexture.enableRandomWrite = true;
        mapRenderTexture.Create();

        /*
        //Initialize Cell Value Data to be used in the Grid
        uint[] initialCellData = new uint[width * height];
        for (int i = 0; i < width * height; i++)
        {
            initialCellData[i] = 0; //Alive = 1, Dead = 0
        }
        */

        //Set this rendertexture to be the compute shader's AllCellsGrid; the texture must be individually bound to all kernels; note: upon binding, all changes to AllCellsGrid in the compute shader will also be made on the mapRenderTexture too
        mapComputeShader.SetTexture(kernelIndexForInitializeGameBoard, "AllCellsGrid", mapRenderTexture);
        mapComputeShader.SetTexture(kernelIndexForUpdateGameBoard, "AllCellsGrid", mapRenderTexture);

        //run the method of the compute shader called "InitializeGameBoard"
        mapComputeShader.Dispatch(kernelIndexForInitializeGameBoard, Mathf.CeilToInt(width / 8.0f), Mathf.CeilToInt(height / 8.0f), 1);//Mathf.CeilToInt(f) returns the smallest int greater than or equal to f. So if height is 10 ,then 10/8f= 1.25, this returns 2. Why use this? Because it rounds UP instead of DOWN, that way theres always a thread batch calculating the stragglers if there are more cells than the nearest perfect group of 8x8. (otherwise the remainder cells wont be calculated)
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
    */
    public void GetLivingCellsFromComputeShader()
    {
        int textureWidth = mapRenderTexture.width;
        int textureHeight = mapRenderTexture.height;

        // Create a CPU-readable Texture2D
        Texture2D outputMapRenderTexture = new Texture2D(textureWidth, textureHeight, TextureFormat.ARGB32, false);

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


    void Update()
    {
        
    }
}
