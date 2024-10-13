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
    public void UpdateCamera(float height, float width)
    {
        UpdateCameraWidth(height);
        UpdateCameraPosition(height, width);
    }
    void UpdateCameraWidth(float height)
    {
        if (cam != null)
        {
            cam.orthographicSize = height / 2;
        }
    }

    void UpdateCameraPosition(float height, float width)
    {
        cam.transform.position = new Vector3(width / 2, height / 2, cam.transform.position.z);
    }
}
