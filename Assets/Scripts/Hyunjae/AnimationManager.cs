using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationManager : MonoBehaviour
{
    [Header("Legacy Animation component 제어")]
    public string idleClipName = "idle";
    public string walkClipName = "walk";
    public float crossFadeDuration = 0.1f;

    private Animation animationComponent;
    private Rigidbody2D rigidbody2DComponent;

    [Header("이동 설정")]
    public float moveSpeed = 3.5f;
    public float stoppingDistance = 0.05f;

    private Vector3 targetPosition;
    private bool hasTarget = false;
    private float cameraDistanceZ;

    void Awake()
    {
        animationComponent = GetComponent<Animation>();
        rigidbody2DComponent = GetComponent<Rigidbody2D>();
        if (animationComponent == null)
        {
            Debug.LogError("AnimationManager: Animation 컴포넌트가 필요합니다.");
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        if (animationComponent == null) return;

        // 루프 설정(없으면 무시)
        if (animationComponent[idleClipName] != null)
        {
            animationComponent[idleClipName].wrapMode = WrapMode.Loop;
        }
        if (animationComponent[walkClipName] != null)
        {
            animationComponent[walkClipName].wrapMode = WrapMode.Loop;
        }

        // 시작 시 idle 보장
        if (animationComponent[idleClipName] != null)
        {
            animationComponent.Play(idleClipName);
        }

        // 카메라-플레이어 거리(스크린→월드 변환 안정화)
        if (Camera.main != null)
        {
            cameraDistanceZ = Camera.main.WorldToScreenPoint(transform.position).z;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (animationComponent == null) return;

        HandleClickToMove();
        if (rigidbody2DComponent == null)
        {
            UpdateMovement();
        }
    }

    void FixedUpdate()
    {
        // 물리 기반 이동일 때는 FixedUpdate에서 처리
        if (rigidbody2DComponent != null)
        {
            PhysicsUpdateMovement();
        }
    }

    private void HandleClickToMove()
    {
        // 우클릭 시 목적지 설정
        if (Input.GetMouseButtonDown(1))
        {
            Vector3 mouseScreen = Input.mousePosition;
            Vector3 world = Vector3.zero;
            if (Camera.main != null)
            {
                // 현재 오브젝트가 있는 깊이를 기준으로 스크린→월드 변환
                world = Camera.main.ScreenToWorldPoint(new Vector3(mouseScreen.x, mouseScreen.y, cameraDistanceZ));
            }
            world.z = transform.position.z; // 2D: Z 고정

            targetPosition = world;
            hasTarget = true;

            // 이동 시작: walk로 전환
            if (animationComponent[walkClipName] != null)
            {
                animationComponent.CrossFade(walkClipName, crossFadeDuration);
            }
        }
    }

    private void UpdateMovement()
    {
        if (!hasTarget) return;

        Vector3 current = transform.position;
        Vector3 toTarget = targetPosition - current;
        float distance = toTarget.magnitude;

        if (distance <= stoppingDistance)
        {
            transform.position = targetPosition;
            hasTarget = false;

            // 도착: idle로 전환
            if (animationComponent[idleClipName] != null)
            {
                animationComponent.CrossFade(idleClipName, crossFadeDuration);
            }
            return;
        }

        Vector3 next = Vector3.MoveTowards(current, targetPosition, moveSpeed * Time.deltaTime);
        transform.position = next;
    }

    private void PhysicsUpdateMovement()
    {
        if (!hasTarget) return;

        Vector3 current = transform.position;
        Vector3 toTarget = targetPosition - current;
        float distance = toTarget.magnitude;

        if (distance <= stoppingDistance)
        {
            rigidbody2DComponent.MovePosition(targetPosition);
            hasTarget = false;
            if (animationComponent[idleClipName] != null)
            {
                animationComponent.CrossFade(idleClipName, crossFadeDuration);
            }
            return;
        }

        Vector3 next = Vector3.MoveTowards(current, targetPosition, moveSpeed * Time.fixedDeltaTime);
        rigidbody2DComponent.MovePosition(next);
    }
}
