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
    [Header("마우스 시점 오브젝트")]
    [SerializeField] private GameObject MouseFollowingObj;
    [Header("플레이어 시점 오브젝트")]
    [SerializeField] private GameObject PlayerObj;
    [Header("바 시점 오브젝트")]
    [SerializeField] private GameObject WorkspaceObj;

    [Header("평소 시점 카메라 콜라이더")]
    [SerializeField] private PolygonCollider2D defaultCollider;

    [Header("카메라 댐핑 값")]
    [SerializeField] private float DampingValue = 0.2f;

    private void Start()
    {
        confiner.m_Damping = 0.2f; // 카메라 댐핑
    }
    // Update is called once per frame
    void Update()
    {
        // 마우스 위치를 월드 좌표로 변환
        Vector3 mouseScreenPos = Input.mousePosition;
        mouseScreenPos.z = Mathf.Abs(Camera.main.transform.position.z); // 카메라와의 거리 지정
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(mouseScreenPos);
        mouseWorldPos.z = 0; // 2D 환경일 경우 z값 고정

        // 댐핑
        MouseFollowingObj.transform.position = Vector3.Lerp(MouseFollowingObj.transform.position, mouseWorldPos, DampingValue);

        if ((Input.GetKeyDown(KeyCode.T) && isMaking == false)) // 바 시점 X 일때, T키로 시점 전환
        {
            switch(isMouse)
            {
                case true:
                    isMouse = false;
                    break;
                case false:
                    isMouse = true;
                    break;
            }
        }

        if(isMaking == true) // 바 시점 O 일때, 무조건 바 시점
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
}
