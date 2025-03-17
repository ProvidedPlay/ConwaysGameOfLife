using Cinemachine;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Schema;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Tilemaps;

[ExecuteInEditMode]
public class CameraToGameBoardResizer : MonoBehaviour
{
    public Camera cam;
    public CinemachineVirtualCamera virtualCamera;
    public Tilemap tileMap;
    public GameManager gameManager;

    public Vector2 cameraBounds;

    public float cameraEdgeBuffer;
    public float cameraEdgeBufferPercentOfHeight;
    public double cameraAspectRatio;

    public float minimumCameraOrthographicHeight;
    public float currentCameraOrthographicHeight;
    public float currentCameraOrthographicWidth;

    public int maxZoomFactor;
    //[Range(0, 10)]
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
            zoomFactor = Mathf.Clamp(value, 0, maxZoomFactor);
            float newCamHeight = Mathf.Lerp(minimumCameraOrthographicHeight, cameraBounds.y, (float)zoomFactor/maxZoomFactor);
            UpdateCameraHeight(newCamHeight);
        }
    }
    public int cameraPanMouseButton;
    private Vector3 mouseDragOriginPosition;

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
        if(Input.GetAxisRaw("Mouse ScrollWheel") != 0 && !EventSystem.current.IsPointerOverGameObject())
        {
            ZoomCamera(Input.GetAxis("Mouse ScrollWheel"), true);
        }
        PanCamera();
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
        if (gameManager == null)
        {
            gameManager = GetComponent<GameManager>();
        }
    }
    /*
     * Utilities
     */
    void UpdateCameraHeight(float height)
    {
        if (virtualCamera != null)
        {
            //corrects for an error caused by camera aspect ratio initializing to 1
            if(virtualCamera.m_Lens.Aspect != 1)
            {
                currentCameraOrthographicWidth = (height * virtualCamera.m_Lens.Aspect / 2) - cameraEdgeBuffer;
            }
            else
            {
                currentCameraOrthographicWidth = (height * (float)cameraAspectRatio/ 2) - cameraEdgeBuffer;
            }

            currentCameraOrthographicHeight = (height / 2) - cameraEdgeBuffer;
            virtualCamera.m_Lens.OrthographicSize = currentCameraOrthographicHeight;            
        }
    }
    void UpdateCameraPosition(float xAxisCenter, float yAxisCenter)
    {
        //clamps the new camera positions value into the camera bounds
        Vector3 newCameraPosition = new(Mathf.Clamp(xAxisCenter, currentCameraOrthographicWidth, cameraBounds.x - currentCameraOrthographicWidth), Mathf.Clamp(yAxisCenter, currentCameraOrthographicHeight, cameraBounds.y - currentCameraOrthographicHeight), cam.transform.position.z);
        
        //virtualCamera.transform.position = new Vector3(xAxisCenter, yAxisCenter, cam.transform.position.z);
        virtualCamera.transform.position = newCameraPosition;
    }

    public void UpdateCameraBounds(float maxX, float maxY)
    {
        cameraBounds.x = maxX;
        cameraBounds.y = maxY;
        cameraEdgeBuffer = maxY * cameraEdgeBufferPercentOfHeight;
    }
    void AdjustZoomFactor(float scrollValue)
    {
        int currentZoomFactor = ZoomFactor;
        ZoomFactor = currentZoomFactor + (1 * (int)Mathf.Sign(-scrollValue));
    }
    void SetZoomFactor(int newZoomFactor)
    {
        ZoomFactor = newZoomFactor;
    }

    /*
     * Commands
     */
    public void ResetCamera()
    {
        CenterCameraToBoard();
        ZoomCamera(maxZoomFactor);
    }
    void CenterCameraToBoard()
    {
        UpdateCameraPosition(cameraBounds.x / 2, cameraBounds.y / 2);
    }
    void ZoomInOnMouse()
    {
        Vector3 mousePosition = cam.ScreenToWorldPoint(Input.mousePosition);
        UpdateCameraPosition(mousePosition.x, mousePosition.y);
    }
    public void ZoomCamera(float scrollValue, bool zoomInOnMouse)
    {
        AdjustZoomFactor(scrollValue);
        if (zoomInOnMouse)
        {
            ZoomInOnMouse();
        }
        gameManager.settingsManager.UpdateZoomMultiplierText();
    }
    public void ZoomCamera(int newZoomFactor)
    {
        SetZoomFactor(newZoomFactor);
        gameManager.settingsManager.UpdateZoomMultiplierText();
    }
    /*
     * Pan Camera
     */
    public void PanCamera()
    {
        if(virtualCamera != null && cam != null)
        {
            if (Input.GetMouseButtonDown(cameraPanMouseButton))
            {
                //get the mouse position when the drag starts (initialClick)
                mouseDragOriginPosition = cam.ScreenToWorldPoint(Input.mousePosition);
            }
            if (Input.GetMouseButton(cameraPanMouseButton))
            {
                //get distance mouse has moved since the button was first clicked
                Vector3 mousePositionDifferenceFromOrigin = mouseDragOriginPosition - cam.ScreenToWorldPoint(Input.mousePosition);

                //move the camera by that distance (using the update camera method)
                Vector3 newPosition = cam.transform.position + mousePositionDifferenceFromOrigin;
                UpdateCameraPosition(newPosition.x, newPosition.y);
            }
        }
    }
}
