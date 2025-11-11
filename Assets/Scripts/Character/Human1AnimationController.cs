using UnityEngine;

/// <summary>
/// Human1 캐릭터의 애니메이션을 제어하는 스크립트
/// Animator Controller와 함께 사용됩니다.
/// </summary>
public class Human1AnimationController : MonoBehaviour
{
    [Header("애니메이션 설정")]
    [SerializeField] private Animator animator;
    
    [Header("이동 설정")]
    [SerializeField] private float moveSpeed = 3.5f;
    [SerializeField] private float stoppingDistance = 0.05f;
    
    [Header("애니메이션 파라미터 이름")]
    [SerializeField] private string speedParameter = "Speed";
    [SerializeField] private string isWalkingParameter = "IsWalking";
    [SerializeField] private string isRunningParameter = "IsRunning";
    
    private Vector3 targetPosition;
    private bool hasTarget = false;
    private float cameraDistanceZ;
    
    void Awake()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
        
        if (animator == null)
        {
            Debug.LogWarning("Human1AnimationController: Animator 컴포넌트를 찾을 수 없습니다.");
        }
    }
    
    void Start()
    {
        // 카메라 거리 계산
        if (Camera.main != null)
        {
            cameraDistanceZ = Camera.main.WorldToScreenPoint(transform.position).z;
        }
    }
    
    void Update()
    {
        HandleClickToMove();
        UpdateMovement();
        UpdateAnimation();
    }
    
    /// <summary>
    /// 마우스 우클릭으로 이동 목적지 설정
    /// </summary>
    private void HandleClickToMove()
    {
        if (Input.GetMouseButtonDown(1))
        {
            Vector3 mouseScreen = Input.mousePosition;
            Vector3 world = Vector3.zero;
            
            if (Camera.main != null)
            {
                world = Camera.main.ScreenToWorldPoint(new Vector3(mouseScreen.x, mouseScreen.y, cameraDistanceZ));
            }
            
            world.z = transform.position.z; // 2D: Z 고정
            targetPosition = world;
            hasTarget = true;
        }
    }
    
    /// <summary>
    /// 캐릭터 이동 처리
    /// </summary>
    private void UpdateMovement()
    {
        if (!hasTarget) return;
        
        Vector3 current = transform.position;
        Vector3 toTarget = targetPosition - current;
        float distance = toTarget.magnitude;
        
        // 이동 방향에 따라 캐릭터 뒤집기
        if (distance > stoppingDistance)
        {
            float directionX = targetPosition.x - current.x;
            Vector3 scale = transform.localScale;
            
            if (directionX < 0) // 왼쪽으로 이동
            {
                scale.x = -1f;
            }
            else if (directionX > 0) // 오른쪽으로 이동
            {
                scale.x = 1f;
            }
            // directionX == 0일 때는 이전 스케일 유지
            
            transform.localScale = scale;
        }
        
        if (distance <= stoppingDistance)
        {
            transform.position = targetPosition;
            hasTarget = false;
            return;
        }
        
        Vector3 next = Vector3.MoveTowards(current, targetPosition, moveSpeed * Time.deltaTime);
        transform.position = next;
    }
    
    /// <summary>
    /// 애니메이션 파라미터 업데이트
    /// </summary>
    private void UpdateAnimation()
    {
        if (animator == null) return;
        
        // 이동 속도 계산
        float speed = 0f;
        if (hasTarget)
        {
            Vector3 velocity = (targetPosition - transform.position) / Time.deltaTime;
            speed = velocity.magnitude;
        }
        
        // 애니메이션 파라미터 설정
        // animator.SetFloat(speedParameter, speed);
        // animator.SetBool(isWalkingParameter, hasTarget && speed > 0.1f);
        
        // // 달리기 여부 (속도가 일정 이상일 때)
        // bool isRunning = speed > moveSpeed * 0.7f;
        // animator.SetBool(isRunningParameter, isRunning);
    }
    
    /// <summary>
    /// 외부에서 이동 목적지 설정
    /// </summary>
    public void SetTargetPosition(Vector3 position)
    {
        targetPosition = position;
        hasTarget = true;
    }
    
    /// <summary>
    /// 이동 중지
    /// </summary>
    public void StopMovement()
    {
        hasTarget = false;
    }
    
    /// <summary>
    /// 특정 애니메이션 트리거
    /// </summary>
    public void TriggerAnimation(string triggerName)
    {
        if (animator != null)
        {
            animator.SetTrigger(triggerName);
        }
    }
}

