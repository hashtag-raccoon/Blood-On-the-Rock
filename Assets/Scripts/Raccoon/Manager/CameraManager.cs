using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
    [Header("카메라 할당(가상카메라)")]
    [SerializeField] private CinemachineVirtualCamera virtualCamera;

    private bool isMouse = false;
    [Header("마우스 시점 오브젝트")]
    [SerializeField] private GameObject MouseFollowingObj;
    [Header("플레이어 시점 오브젝트")]
    [SerializeField] private GameObject PlayerObj;
    [Header("카메라 댐핑 값")]
    [SerializeField] private float DampingValue = 0.2f;

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

        if (Input.GetKeyDown(KeyCode.T))
        {
            switch(isMouse)
            {
                case true:
                    isMouse = false;
                    virtualCamera.Follow = MouseFollowingObj.transform;
                    break;
                case false:
                    isMouse = true;
                    virtualCamera.Follow = PlayerObj.transform;
                    break;
            }
        }
    }
}
