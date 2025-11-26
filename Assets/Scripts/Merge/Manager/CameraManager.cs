using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CameraManager : MonoBehaviour
{
    public static CameraManager instance;

    [Header("카메라 할당(가상카메라)")]
    public CinemachineVirtualCamera virtualCamera;
    public float OriginSize;
    [Header("Confiner 할당(가상카메라)")]
    [SerializeField] private CinemachineConfiner2D confiner;

    private bool isMouse = false;
    [HideInInspector] public bool isMaking = false;
    [Header("섬 씬 여부")]
    [SerializeField] private bool isIsland = false;
    [Header("마우스 시점 오브젝트")]
    [SerializeField] private GameObject MouseFollowingObj;
    public GameObject mouseFollowingObj => MouseFollowingObj; // public 접근자 추가
    [Header("플레이어 시점 오브젝트")]
    [SerializeField] private GameObject PlayerObj;
    [Header("작업 시점 오브젝트")]
    [SerializeField] private GameObject WorkspaceObj;

    [Header("평소 시점 카메라 콜라이더")]
    [SerializeField] private PolygonCollider2D defaultCollider;

    [Header("카메라 댐핑 값")]
    [SerializeField] private float DampingValue = 0.2f;

    [Header("섬 씬/최대 줌아웃 값")]
    public float MaxZoomIn = 50;

    [Header("섬 씬/최소 줌인 값")]
    public float MinZoomOut = 1;

    [Header("섬 씬/줌 속도")]
    [SerializeField] private float ZoomSpeed = 2f;
    [Header("오브젝트 클릭 시 무시할 레이어")]
    public LayerMask ignoreLayerMask; // CameraBoundary 등

    private Vector3 _tmpClickPos;
    private Vector3 _tmpCameraPos;

    private bool _isDragging = false;
    
    [HideInInspector] public bool isBuildingUIActive = false; // 건물 UI 활성화 상태
    
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
        virtualCamera.m_Lens.OrthographicSize = OriginSize;
    }
    
    private void Start()
    {
        confiner.m_Damping = 0.2f; // 카메라 댐핑
        if (isIsland)
        {
            IslandInit();
        }
    }

    void Update()
    {        
        if (!isIsland)
        {
            Vector3 mouseScreenPos = Input.mousePosition;
            
            // 화면 범위 체크
            if (mouseScreenPos.x >= 0 && mouseScreenPos.x <= Screen.width && 
                mouseScreenPos.y >= 0 && mouseScreenPos.y <= Screen.height)
            {
                // 카메라부터 평면까지의 거리
                mouseScreenPos.z = Camera.main.nearClipPlane + 1f;
                
                Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(mouseScreenPos);
                mouseWorldPos.z = 0;

                MouseFollowingObj.transform.position = Vector3.Lerp(
                    MouseFollowingObj.transform.position, 
                    mouseWorldPos, 
                    DampingValue
                );
            }

            if (Input.GetKeyDown(KeyCode.T) && isMaking == false)
            {
                isMouse = !isMouse; // 간소화
            }

            if (isMaking == true)
            {
                isMouse = false;
                confiner.m_BoundingShape2D = null;
                virtualCamera.Follow = WorkspaceObj.transform;
                virtualCamera.OnTargetObjectWarped(
                    WorkspaceObj.transform,
                    WorkspaceObj.transform.position - virtualCamera.Follow.position
                );
                virtualCamera.PreviousStateIsValid = false;
            }
            else
            {
                confiner.m_BoundingShape2D = defaultCollider;
                virtualCamera.PreviousStateIsValid = true;
                virtualCamera.Follow = isMouse ? MouseFollowingObj.transform : PlayerObj.transform;
            }
        }
        else // IslandScene
        {
            DragToCameramMove();
            WheelToZoom();
            confiner.m_BoundingShape2D = isBuildingUIActive ? null : defaultCollider;
        }
    }

    // 섬 초기 카메라 설정
    private void IslandInit()
    {
        PlayerObj = null;
        WorkspaceObj = null;

        virtualCamera.Follow = MouseFollowingObj.transform; // 마우스 시점
        confiner.m_BoundingShape2D = defaultCollider; // 평소 시점 콜라이더
    }


    // 드래그로 카메라 이동
    private void DragToCameramMove()
    {
        if (MouseFollowingObj == null)
        {
            Debug.LogWarning("CameraManager: MouseFollowingObj가 할당되지 않았습니다. Inspector에서 할당하세요.");
            return;
        }
        
        // 건물 UI가 활성화되어 있으면 드래그 불가
        if (isBuildingUIActive)
        {
            _isDragging = false;
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            bool isOverUI = IsPointerOverUI();
            if (!isOverUI)
            {
                _isDragging = true;
                _tmpClickPos = Input.mousePosition;
                _tmpCameraPos = MouseFollowingObj.transform.position;
            }
        }
        else if (Input.GetMouseButton(0) && _isDragging)
        {
            Vector3 movePos = Camera.main.ScreenToViewportPoint(_tmpClickPos - Input.mousePosition);
            movePos.z = 0;
            Vector2 clampedPos = ClampToPolygon(_tmpCameraPos +
                new Vector3(movePos.x * virtualCamera.m_Lens.OrthographicSize * 2,
                movePos.y * virtualCamera.m_Lens.OrthographicSize * 2, 0));
            MouseFollowingObj.transform.position = clampedPos;
        }
        else if (Input.GetMouseButtonUp(0))
        {
            if (_isDragging)
            {

            }
            _isDragging = false;
        }
    }

    // 휠로 줌 인 / 줌 아웃
    private void WheelToZoom()
    {
        // 화면 범위 체크를 먼저
        bool isInCamera = (Input.mousePosition.x >= 0 && Input.mousePosition.x <= Screen.width)
                && (Input.mousePosition.y >= 0 && Input.mousePosition.y <= Screen.height);
        
        if (!isInCamera || isBuildingUIActive)
        {
            return;
        }

        float scrollWheelInput = Input.GetAxis("Mouse ScrollWheel");

        if (scrollWheelInput != 0)
        { 
            virtualCamera.m_Lens.OrthographicSize += (1 * -Mathf.Sign(scrollWheelInput)) * ZoomSpeed;
            
            // Clamp 처리
            virtualCamera.m_Lens.OrthographicSize = Mathf.Clamp(
                virtualCamera.m_Lens.OrthographicSize, 
                MinZoomOut, 
                MaxZoomIn
            );
        }
    }


    private Vector2 ClampToPolygon(Vector2 targetPosition)
    {
        if (defaultCollider == null)
        { return targetPosition; }

        // PolygonCollider의 가장 가까운 점 찾기
        Vector2 closestPoint = defaultCollider.ClosestPoint(targetPosition);

        // 타겟 위치가 Collider 내부에 있는지 확인
        if (defaultCollider.OverlapPoint(targetPosition))
        {
            // 내부에 있으면 그대로 반환
            return targetPosition;
        }
        else
        {
            // 외부에 있으면 가장 가까운 경계 지점 반환
            return closestPoint;
        }
    }

    // UI 위에 마우스가 있는지 확인 (상호작용 가능한 UI만 감지)
    private bool IsPointerOverUI()
    {
        PointerEventData eventDataCurrentPosition = new PointerEventData(EventSystem.current);
        eventDataCurrentPosition.position = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventDataCurrentPosition, results);
        
        // 실제 상호작용 가능한 UI 요소만 필터링
        foreach (RaycastResult result in results)
        {
            // Graphic 컴포넌트가 있고 raycastTarget이 true인 경우만 체크
            Graphic graphic = result.gameObject.GetComponent<Graphic>();
            if (graphic != null && graphic.raycastTarget)
            {
                // Button, ScrollRect 등 실제 상호작용 컴포넌트가 있는지 확인
                if (result.gameObject.GetComponent<Button>() != null ||
                    result.gameObject.GetComponent<ScrollRect>() != null ||
                    result.gameObject.GetComponentInParent<Button>() != null ||
                    result.gameObject.GetComponentInParent<ScrollRect>() != null)
                {
                    return true;
                }
            }
        }
        
        return false;
    }
}
