using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class CameraHelper
{
    public static float GetCurrentCameraWidth(CinemachineVirtualCamera virtualCamera)
    {

        float cameraWidth = (virtualCamera.m_Lens.OrthographicSize * 2) * virtualCamera.m_Lens.Aspect;//multiplies the camera height by the aspect ratio

        return cameraWidth;
    }
}
        
