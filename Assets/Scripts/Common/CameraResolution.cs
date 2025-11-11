using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(Camera))]
public class CameraResolution : MonoBehaviour
{
    [Header("기준 해상도 (예: 1080x1920 기준)")]
    public float referenceWidth = 1080f;
    public float referenceHeight = 1920f;

    private Camera cam;

    void Awake()
    {
        cam = GetComponent<Camera>();
    }

    void Start()
    {
        AdjustCameraSize();
    }

    void AdjustCameraSize()
    {
        float targetAspect = referenceWidth / referenceHeight;
        float currentAspect = (float)Screen.width / Screen.height;

        // 기본 세로 해상도 기준 orthographic size
        float baseOrthographicSize = cam.orthographicSize;

        if (currentAspect < targetAspect)
        {
            // 세로로 더 긴 화면: 위아래가 잘릴 수 있으니 카메라 확대
            float scaleFactor = targetAspect / currentAspect;
            cam.orthographicSize = baseOrthographicSize * scaleFactor;
        }
        else
        {
            // 가로가 더 넓은 화면은 기본 사이즈 유지
            cam.orthographicSize = baseOrthographicSize;
        }
    }
}
