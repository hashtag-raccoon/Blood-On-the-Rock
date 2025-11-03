using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
public class CocktailManager : MonoBehaviour
{
    [Header("작업 상태 여부")]
    private bool isMaking = false; // 칵테일 제조 중인지 여부, 디버깅용
    [Header("Camera Manager 할당")]
    [SerializeField] private CameraManager cameraManager;
    [Header("플레이어, 작업공간 충돌감지 콜라이더")]
    [SerializeField] private BoxCollider2D playerCollider;
    [SerializeField] private PolygonCollider2D workspaceCollider;

    //[SerializeField] private List<GameObject> MakingIndex_obj = new List<GameObject>();
    //private int workIndex = 0;
    void Update()
    {
        cameraManager.isMaking = isMaking;
        // 작업대 근처에서 E키 누르면 칵테일 제조 시작
        if (playerCollider.bounds.Intersects(workspaceCollider.bounds) && Input.GetKeyDown(KeyCode.E))
        {
            isMaking = true;
        }

        if(isMaking)
        {
            /*
            // 작업 순번에 따라 오브젝트 활성화
            MakingIndex_obj[workIndex].SetActive(true);
            foreach(var item in MakingIndex_obj)
            {
                if(item != MakingIndex_obj[workIndex])
                {
                    item.SetActive(false);
                }
            }
            */
        }
    }
}