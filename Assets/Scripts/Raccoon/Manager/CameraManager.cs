using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
    [Header("카메라 할당(가상카메라)")]
    public CinemachineVirtualCamera virtualCamera;
    [Header("Confiner 할당(가상카메라)")]
    [SerializeField] private CinemachineConfiner2D confiner;

    private bool isMouse = false;
    [HideInInspector] public bool isMaking = false;
    [Header("섬 씬 여부")]
    [SerializeField] private bool isIsland = false;
    [Header("마우스 시점 오브젝트")]
    [SerializeField] private GameObject MouseFollowingObj;
    [Header("플레이어 시점 오브젝트")]
    [SerializeField] private GameObject PlayerObj;
    [Header("작업 시점 오브젝트")]
    [SerializeField] private GameObject WorkspaceObj;

    [Header("평소 시점 카메라 콜라이더")]
    [SerializeField] private PolygonCollider2D defaultCollider;

    [Header("카메라 댐핑 값")]
    [SerializeField] private float DampingValue = 0.2f;

    [Header("섬 씬/최대 줌아웃 값")]
    [SerializeField] private float MaxZoomIn = 50;

    [Header("섬 씬/최소 줌인 값")]
    [SerializeField] private float MinZoomOut = 1;

    [Header("섬 씬/줌 속도")]
    [SerializeField] private float ZoomSpeed = 2f;

    private Vector3 _tmpClickPos;
    private Vector3 _tmpCameraPos;

    private void Start()
    {
        confiner.m_Damping = 0.2f; // 카메라 댐핑
        if(isIsland)
        {
            IslandInit();
        }
    }
    // Update is called once per frame
    void Update()
    {
        // 마우스 위치를 월드 좌표로 변환
        Vector3 mouseScreenPos = Input.mousePosition;
        mouseScreenPos.z = Mathf.Abs(Camera.main.transform.position.z); // 카메라와의 거리 지정

        if (!isIsland) // BarScene 일때
        {
            Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(mouseScreenPos);
            mouseWorldPos.z = 0; // 2D 환경일 경우 z값 고정

            // 댐핑
            MouseFollowingObj.transform.position = Vector3.Lerp(MouseFollowingObj.transform.position, mouseWorldPos, DampingValue);

            if ((Input.GetKeyDown(KeyCode.T) && isMaking == false)) // 바 시점 X 일때, T키로 시점 전환
            {
                switch (isMouse)
                {
                    case true:
                        isMouse = false;
                        break;
                    case false:
                        isMouse = true;
                        break;
                }
            }

            if (isMaking == true) // 바 시점 O 일때, 무조건 바 시점
            {
                isMouse = false;
                confiner.m_BoundingShape2D = null; // 바 시점 콜라이더 초기화
                virtualCamera.Follow = WorkspaceObj.transform; // 바 시점
                virtualCamera.OnTargetObjectWarped(
                    WorkspaceObj.transform,
                    WorkspaceObj.transform.position - virtualCamera.Follow.position
                );
                virtualCamera.PreviousStateIsValid = false;
            }
            else
            {
                confiner.m_BoundingShape2D = defaultCollider; // 평소 시점 콜라이더
                virtualCamera.PreviousStateIsValid = true;
                if (isMouse == true)
                {
                    virtualCamera.Follow = MouseFollowingObj.transform; // 마우스 시점

                }
                else
                {
                    virtualCamera.Follow = PlayerObj.transform; // 플레이어 시점
                }
            }
        }
        else // IslandScene 일때
        {
            DragToCameramMove();

            WheelToZoom();
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
        if (Input.GetMouseButtonDown(0))
        {
            _tmpClickPos = Input.mousePosition;
            _tmpCameraPos = MouseFollowingObj.transform.position; // virtualCamera → MouseFollowingObj
        }
        else if (Input.GetMouseButton(0))
        {
            Vector3 movePos = Camera.main.ScreenToViewportPoint(_tmpClickPos - Input.mousePosition);
            movePos.z = 0;
            Vector2 clampedPos = ClampToPolygon(_tmpCameraPos +
                new Vector3(movePos.x * virtualCamera.m_Lens.OrthographicSize * 2,
                movePos.y * virtualCamera.m_Lens.OrthographicSize * 2, 0));
            MouseFollowingObj.transform.position = clampedPos;
        }
    }

    // 휠로 줌 인 / 줌 아웃
    private void WheelToZoom()
    {
        float scrollWheelInput = Input.GetAxis("Mouse ScrollWheel");
        if (scrollWheelInput != 0)
        { virtualCamera.m_Lens.OrthographicSize += (1 * -Mathf.Sign(scrollWheelInput)) * ZoomSpeed; }

        if (virtualCamera.m_Lens.OrthographicSize < MinZoomOut)
        {
            virtualCamera.m_Lens.OrthographicSize = MinZoomOut;
        }
        if (virtualCamera.m_Lens.OrthographicSize > MaxZoomIn)
        {
            virtualCamera.m_Lens.OrthographicSize = MaxZoomIn;
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
}
