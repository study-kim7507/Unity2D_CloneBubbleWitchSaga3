using UnityEngine;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(Camera))]
public class CameraStackHelper : MonoBehaviour
{
    private void Start()
    {
        var mainCamera = Camera.main;
        var mainCameraData = mainCamera.GetUniversalAdditionalCameraData();
        mainCameraData.renderType = CameraRenderType.Base;

        var camera = GetComponent<Camera>();
        var cameraData = camera.GetUniversalAdditionalCameraData();
        cameraData.renderType = CameraRenderType.Overlay;
        mainCameraData.cameraStack.Add(camera);

        var uiManagerCamera = UIManager.Instance.UICamera;
        var uiManagerCameraData = uiManagerCamera.GetUniversalAdditionalCameraData();
        uiManagerCameraData.renderType = CameraRenderType.Overlay;
        mainCameraData.cameraStack.Add(uiManagerCamera);
    }
}
