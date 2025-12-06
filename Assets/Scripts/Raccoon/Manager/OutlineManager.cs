using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 캐릭터별 Outline 시스템 관리
/// - Outline 전용 카메라 자동 생성
/// - RenderTexture 자동 생성
/// - OutlineDisplay 오브젝트 자동 생성
/// - Outline On/Off 토글
/// </summary>
public class OutlineManager : MonoBehaviour
{
    [Header("Outline 설정")]
    [Tooltip("외곽선 활성화 여부")]
    public bool isOutlineEnabled = false;

    [Tooltip("RenderTexture 해상도")]
    public int renderTextureSize = 512;

    [Tooltip("외곽선 여유 공간")]
    [Range(0f, 1f)]
    public float padding = 0.2f;

    [Tooltip("외곽선 색상")]
    public Color outlineColor = Color.yellow;

    [Tooltip("외곽선 두께")]
    [Range(1f, 100f)]
    public float outlineWidth = 10f;

    [Header("자동 생성된 오브젝트 (읽기 전용)")]
    [SerializeField] private Camera outlineCamera;
    [SerializeField] private RenderTexture outlineRenderTexture;
    [SerializeField] private GameObject outlineDisplayObject;
    [SerializeField] private Canvas outlineCanvas;
    [SerializeField] private RawImage outlineRawImage;
    [SerializeField] private Material outlineMaterial;

    private OutlineCameraFollower cameraFollower;
    private bool isInitialized = false;

    void Start()
    {
        InitializeOutlineSystem();

        // 초기 상태 적용
        SetOutlineEnabled(isOutlineEnabled);
    }

    /// <summary>
    /// Outline 시스템 초기화 (오브젝트 자동 생성)
    /// </summary>
    private void InitializeOutlineSystem()
    {
        if (isInitialized)
        {
            return;
        }

        // 1. RenderTexture 생성
        CreateRenderTexture();

        // 2. Outline Camera 생성
        CreateOutlineCamera();

        // 3. Outline Display 오브젝트 생성 (Canvas + RawImage)
        CreateOutlineDisplay();

        // 4. Outline Material 설정
        SetupOutlineMaterial();

        isInitialized = true;
        Debug.Log($"[OutlineManager] '{gameObject.name}' Outline 시스템 초기화 완료");
    }

    /// <summary>
    /// RenderTexture 생성
    /// </summary>
    private void CreateRenderTexture()
    {
        outlineRenderTexture = new RenderTexture(renderTextureSize, renderTextureSize, 24);
        outlineRenderTexture.name = $"{gameObject.name}_OutlineRT";
        outlineRenderTexture.format = RenderTextureFormat.ARGB32;
        outlineRenderTexture.antiAliasing = 1;
    }

    /// <summary>
    /// Outline 전용 카메라 생성
    /// </summary>
    private void CreateOutlineCamera()
    {
        // 카메라 오브젝트 생성
        GameObject cameraObject = new GameObject($"{gameObject.name}_OutlineCamera");
        cameraObject.transform.SetParent(transform);
        cameraObject.transform.localPosition = new Vector3(0, 0, -10f);

        // Camera 컴포넌트 추가 및 설정
        outlineCamera = cameraObject.AddComponent<Camera>();
        outlineCamera.orthographic = true;
        outlineCamera.clearFlags = CameraClearFlags.SolidColor;
        outlineCamera.backgroundColor = new Color(0, 0, 0, 0); // 투명 배경
        outlineCamera.cullingMask = 1 << gameObject.layer; // 현재 오브젝트의 레이어만
        outlineCamera.targetTexture = outlineRenderTexture;
        outlineCamera.depth = -100; // 메인 카메라보다 먼저 렌더링
        outlineCamera.orthographicSize = 5f;

        // OutlineCameraFollower 추가
        cameraFollower = cameraObject.AddComponent<OutlineCameraFollower>();
        cameraFollower.outlineCam = outlineCamera;
        cameraFollower.target = transform;
        cameraFollower.padding = padding;
    }

    /// <summary>
    /// Outline Display 오브젝트 생성 (World Space Canvas + RawImage)
    /// </summary>
    private void CreateOutlineDisplay()
    {
        // Canvas 오브젝트 생성
        outlineDisplayObject = new GameObject($"{gameObject.name}_OutlineDisplay");
        outlineDisplayObject.transform.SetParent(transform);
        outlineDisplayObject.transform.localPosition = Vector3.zero;

        // Canvas 설정 (World Space)
        outlineCanvas = outlineDisplayObject.AddComponent<Canvas>();
        outlineCanvas.renderMode = RenderMode.WorldSpace;

        // CanvasScaler 추가
        CanvasScaler scaler = outlineDisplayObject.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 10;

        // RectTransform 설정 (캐릭터 크기에 맞춤)
        RectTransform canvasRect = outlineDisplayObject.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(renderTextureSize / 10f, renderTextureSize / 10f);

        // RawImage 오브젝트 생성
        GameObject rawImageObject = new GameObject("RawImage");
        rawImageObject.transform.SetParent(outlineDisplayObject.transform, false);

        // RawImage 설정
        outlineRawImage = rawImageObject.AddComponent<RawImage>();
        outlineRawImage.texture = outlineRenderTexture;

        // RectTransform 설정 (Canvas 전체 채우기)
        RectTransform imageRect = rawImageObject.GetComponent<RectTransform>();
        imageRect.anchorMin = Vector2.zero;
        imageRect.anchorMax = Vector2.one;
        imageRect.sizeDelta = Vector2.zero;
        imageRect.anchoredPosition = Vector2.zero;

        // Sorting Order 설정 (캐릭터 뒤에 표시)
        outlineCanvas.sortingOrder = -1;
    }

    /// <summary>
    /// Outline Material 설정
    /// </summary>
    private void SetupOutlineMaterial()
    {
        // Outline 쉐이더를 사용하는 Material 생성
        Shader outlineShader = Shader.Find("Sprites/Outline");

        if (outlineShader == null)
        {
            Debug.LogWarning("[OutlineManager] 'Sprites/Outline' 쉐이더를 찾을 수 없습니다. 기본 UI 쉐이더를 사용합니다.");
            return;
        }

        outlineMaterial = new Material(outlineShader);
        outlineMaterial.name = $"{gameObject.name}_OutlineMat";

        // 쉐이더 프로퍼티 설정
        outlineMaterial.SetFloat("_OutlineEnabled", 1f);
        outlineMaterial.SetFloat("_Thickness", outlineWidth);
        outlineMaterial.SetColor("_SolidOutline", outlineColor);
        outlineMaterial.SetFloat("_OutlineMode", 0f); // Solid
        outlineMaterial.SetFloat("_OutlineShape", 0f); // Contour

        // RawImage에 Material 적용
        if (outlineRawImage != null)
        {
            outlineRawImage.material = outlineMaterial;
        }
    }

    /// <summary>
    /// Outline On/Off 토글
    /// </summary>
    public void ToggleOutline()
    {
        SetOutlineEnabled(!isOutlineEnabled);
    }

    /// <summary>
    /// Outline 활성화/비활성화
    /// </summary>
    public void SetOutlineEnabled(bool enabled)
    {
        isOutlineEnabled = enabled;

        if (!isInitialized)
        {
            InitializeOutlineSystem();
        }

        if (outlineDisplayObject != null)
        {
            outlineDisplayObject.SetActive(enabled);
        }

        if (outlineCamera != null)
        {
            outlineCamera.gameObject.SetActive(enabled);
        }
    }

    /// <summary>
    /// Outline 색상 변경
    /// </summary>
    public void SetOutlineColor(Color color)
    {
        outlineColor = color;

        if (outlineMaterial != null)
        {
            outlineMaterial.SetColor("_SolidOutline", color);
        }
    }

    /// <summary>
    /// Outline 두께 변경
    /// </summary>
    public void SetOutlineWidth(float width)
    {
        outlineWidth = width;

        if (outlineMaterial != null)
        {
            outlineMaterial.SetFloat("_Thickness", width);
        }
    }

    void OnDestroy()
    {
        // 생성된 리소스 정리
        if (outlineRenderTexture != null)
        {
            outlineRenderTexture.Release();
            Destroy(outlineRenderTexture);
        }

        if (outlineMaterial != null)
        {
            Destroy(outlineMaterial);
        }

        if (outlineDisplayObject != null)
        {
            Destroy(outlineDisplayObject);
        }

        if (outlineCamera != null)
        {
            Destroy(outlineCamera.gameObject);
        }
    }

    // 디버깅용: 인스펙터에서 Outline 상태 확인
    void OnValidate()
    {
        if (Application.isPlaying && isInitialized)
        {
            SetOutlineEnabled(isOutlineEnabled);

            if (outlineMaterial != null)
            {
                outlineMaterial.SetFloat("_Thickness", outlineWidth);
                outlineMaterial.SetColor("_SolidOutline", outlineColor);
            }

            if (cameraFollower != null)
            {
                cameraFollower.padding = padding;
            }
        }
    }
}
