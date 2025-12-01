using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerableController : MonoBehaviour
{
    [Header("이동속도")]
    [Range(1.0f, 10.0f)]
    [SerializeField] private float moveSpeed; // 이동속도

    [HideInInspector]
    public bool PlzStop = false;

    // 충돌 감지 거리 (플레이어 크기에 맞게 조절)
    [Header("충돌감지범위")]
    [Range(0.0f, 1.0f)]
    [SerializeField] private float collisionCheckDistance = 0.6f;

    void Update()
    {
        if (!PlzStop) // 플레이어블로 선택되었을때, 이동 가능
        {
            Move();
        }

        if(Input.GetKeyDown(KeyCode.P))
        {
           if (CocktailMakingManager._instance.GetActiveObject() == false) // P키 입력 시 칵테일 제작 UI 오픈
            {
                OpenCocktailMakingUI();
            }
            else
            {
                CloseCocktailMakingUI();
            }
        }
        
    }

    private void Move() // 플레이어블 캐릭터 이동 함수
    {
        Vector3 moveDirection = Vector3.zero;

        if (Input.GetKey(KeyCode.W))
        {
            moveDirection += new Vector3(0.0f, 1.0f, 0.0f);
        }
        if (Input.GetKey(KeyCode.S))
        {
            moveDirection += new Vector3(0.0f, -1.0f, 0.0f);
        }
        if (Input.GetKey(KeyCode.A))
        {
            moveDirection += new Vector3(-1.0f, 0.0f, 0.0f);
        }
        if (Input.GetKey(KeyCode.D))
        {
            moveDirection += new Vector3(1.0f, 0.0f, 0.0f);
        }

        if (moveDirection != Vector3.zero)
        {
            moveDirection.Normalize();

            // Raycast로 이동 방향에 벽이 있는지 확인, 끼임 방지
            RaycastHit2D hit = Physics2D.Raycast(transform.position, moveDirection, collisionCheckDistance);
            if (hit.collider != null && hit.collider.tag.Equals("Wall"))
            {
                // 벽이 있으면 이동하지 않음
                return;
            }

            transform.position += moveDirection * moveSpeed * Time.deltaTime;
        }
    }

    /// <summary>
    /// 칵테일 제작 작업을 여는 함수
    /// </summary>
    private void OpenCocktailMakingUI()
    {
        CocktailMakingManager._instance.MakingStart();
    }

    private void CloseCocktailMakingUI()
    {
        CocktailMakingManager._instance.MakingStop();
    }
}