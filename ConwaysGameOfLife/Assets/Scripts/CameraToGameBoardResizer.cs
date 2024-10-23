using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Schema;
using UnityEngine;
using UnityEngine.Tilemaps;

[ExecuteInEditMode]
public class CameraToGameBoardResizer : MonoBehaviour
{
    public Camera cam;
    public CinemachineVirtualCamera virtualCamera;
    public Tilemap tileMap;

    public Vector2 cameraBounds;

    public float cameraEdgeBuffer;

    public float minimumCameraHeight;
    public float currentCameraHeight;

    [Range(0, 10)]
    [SerializeField]
    private int zoomFactor;
    public int ZoomFactor
    {
        get
        {
            return zoomFactor;
        }
        set
        {
            zoomFactor = Mathf.Clamp(value, 0, 10);
            float newCamHeight = Mathf.Lerp(minimumCameraHeight, cameraBounds.y, (float)zoomFactor/10);
            UpdateCameraHeight(newCamHeight);
        }
    }

    private void OnValidate()
    {
        ZoomFactor = zoomFactor;
    }
    void Awake()
    {
        UnpackObjectReferences();   
    }

    private void Update()
    {
        if(Input.GetAxisRaw("Mouse ScrollWheel") != 0)
        {
            ZoomCamera(Input.GetAxis("Mouse ScrollWheel"), true);
        }
    }
    void UnpackObjectReferences()
    {
        if (tileMap == null) {
            tileMap = GameObject.FindGameObjectWithTag("Game Board").GetComponent<Tilemap>();
        }
        if (cam == null) {
            cam = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>();
        }
        if(virtualCamera == null)
        {
            virtualCamera = GameObject.FindGameObjectWithTag("Virtual Camera").GetComponent<CinemachineVirtualCamera>();
        }
    }
    /*
     * Utilities
     */
    void UpdateCameraHeight(float height)
    {
        if (virtualCamera != null)
        {
            currentCameraHeight = (height / 2) - cameraEdgeBuffer;
            virtualCamera.m_Lens.OrthographicSize = currentCameraHeight;
            
        }
    }
    void UpdateCameraPosition(float xAxisCenter, float yAxisCenter)
    {
        virtualCamera.transform.position = new Vector3(xAxisCenter, yAxisCenter, cam.transform.position.z);
    }

    public void UpdateCameraBounds(float maxX, float maxY)
    {
        cameraBounds.x = maxX;
        cameraBounds.y = maxY;
    }
    void AdjustZoomFactor(float scrollValue)
    {
        int currentZoomFactor = ZoomFactor;
        ZoomFactor = currentZoomFactor + (1 * (int)Mathf.Sign(-scrollValue));
    }


    /*
     * Commands
     */
    public void ResetCamera()
    {
        CenterCameraToBoard();
        UpdateCameraHeight(cameraBounds.y);
    }
    void CenterCameraToBoard()
    {
        UpdateCameraPosition(cameraBounds.x / 2, cameraBounds.y / 2);
    }
    void ZoomInOnMouse()
    {
        Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        UpdateCameraPosition(mousePosition.x, mousePosition.y);
    }
    void ZoomCamera(float scrollValue, bool zoomInOnMouse)
    {
        AdjustZoomFactor(scrollValue);
        if (zoomInOnMouse)
        {
            ZoomInOnMouse();
        }
    }
}
