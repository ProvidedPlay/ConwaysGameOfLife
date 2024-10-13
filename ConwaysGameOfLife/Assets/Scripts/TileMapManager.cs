using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class TileMapManager : MonoBehaviour
{
    public CameraToGameBoardResizer boardResizer;

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
            print("width changed to " + tileMapWidth);
            if (boardResizer != null)
            {
                boardResizer.UpdateCameraWidth(value);
            }
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
        }
    }

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
        boardResizer = GetComponent<CameraToGameBoardResizer>();
    }
}
