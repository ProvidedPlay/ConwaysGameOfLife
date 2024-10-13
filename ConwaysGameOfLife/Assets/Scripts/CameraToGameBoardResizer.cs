using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[ExecuteInEditMode]
public class CameraToGameBoardResizer : MonoBehaviour
{
    public Camera cam;
    public Tilemap tileMap;
    void Awake()
    {
        UnpackObjectReferences();   
    }
    void UnpackObjectReferences()
    {
        if (tileMap == null) {
            tileMap = GameObject.FindGameObjectWithTag("Game Board").GetComponent<Tilemap>();
        }
        if (cam == null) {
            cam = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>();
        }
    }
    public void UpdateCameraWidth(int width)
    {
        if (cam != null)
        {
            cam.orthographicSize = width;
        }
    }
}
