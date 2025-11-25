using UnityEngine;

/// <summary>
/// 공용 캐릭터 이동/애니메이션 컨트롤러
/// - 우클릭 이동(옵션)
/// - 이동 방향에 따른 좌우 반전
/// - Animator 파라미터 업데이트(옵션)
/// 모든 캐릭터 프리팹에 공통으로 붙여 사용 가능합니다.
/// </summary>
[DisallowMultipleComponent]
public class CharacterAnimationController : MonoBehaviour
{
	[Header("Animator")]
	[SerializeField] private Animator animator;
	[SerializeField] private bool forceDisableRootMotion = true;
	[SerializeField] private bool updateAnimatorParameters = false;
	[SerializeField] private string isWalkingParameter = "IsWalking";
	[SerializeField] private string moveYParameter = "MoveY";

	[Header("Movement")]
	[SerializeField] private Transform movementRoot; // 위치 이동 대상 (기본: this.transform)
	[SerializeField] private bool enableRightClickMove = true;
	[SerializeField] private float moveSpeed = 3.5f;
	[SerializeField] private float stoppingDistance = 0.05f;

	[Header("Guest Controller Integration")]
	[Tooltip("GuestController가 있으면 자동 이동을 사용합니다. 없으면 수동 이동을 사용합니다.")]
	[SerializeField] private GuestController guestController;

	private Vector3 targetPosition;
	private bool hasTarget = false;
	private float cameraDistanceZ;
	private Vector3 previousPosition; // 이전 프레임 위치 (이동 방향 계산용)

	private void Awake()
	{
		if (animator == null)
		{
			animator = GetComponent<Animator>();
		}
		if (animator == null)
		{
			animator = GetComponentInChildren<Animator>();
		}
		if (animator == null)
		{
			Debug.LogWarning($"{nameof(CharacterAnimationController)}: Animator 컴포넌트를 찾을 수 없습니다.");
		}

		if (forceDisableRootMotion && animator != null)
		{
			animator.applyRootMotion = false;
		}

		if (movementRoot == null)
		{
			movementRoot = this.transform;
		}

		// GuestController 자동 찾기 (Inspector에서 할당되지 않은 경우)
		if (guestController == null)
		{
			guestController = GetComponent<GuestController>();
		}
		if (guestController == null)
		{
			guestController = GetComponentInParent<GuestController>();
		}

		// 이전 위치 초기화
		previousPosition = movementRoot.position;
	}

	private void Start()
	{
		if (Camera.main != null)
		{
			cameraDistanceZ = Camera.main.WorldToScreenPoint(transform.position).z;
		}
	}

	private void Update()
	{
		// GuestController가 있으면 자동 이동 모드, 없으면 수동 이동 모드
		if (guestController != null)
		{
			// GuestController를 사용한 자동 이동 모드
			UpdateAnimationFromGuestController();
		}
		else
		{
			// 기존 수동 이동 모드
			if (enableRightClickMove)
				HandleClickToMove();

			UpdateMovement();
			UpdateAnimation();
		}
	}

	/// <summary>
	/// 마우스 우클릭으로 이동 목적지 설정
	/// </summary>
	private void HandleClickToMove()
	{
		if (!Input.GetMouseButtonDown(1))
			return;

		Vector3 mouseScreen = Input.mousePosition;
		Vector3 world = movementRoot.position;

		if (Camera.main != null)
		{
			world = Camera.main.ScreenToWorldPoint(new Vector3(mouseScreen.x, mouseScreen.y, cameraDistanceZ));
		}

		world.z = movementRoot.position.z;
		targetPosition = world;
		hasTarget = true;
	}

	/// <summary>
	/// 캐릭터 이동 처리
	/// </summary>
	private void UpdateMovement()
	{
		if (!hasTarget) return;

		Vector3 current = movementRoot.position;
		Vector3 toTarget = targetPosition - current;
		float distance = toTarget.magnitude;

		// 이동 방향에 따라 캐릭터 좌우 반전
		if (distance > stoppingDistance)
		{
			float directionX = targetPosition.x - current.x;
			Vector3 scale = movementRoot.localScale;
			if (directionX < 0)
				scale.x = -Mathf.Abs(scale.x);
			else if (directionX > 0)
				scale.x = Mathf.Abs(scale.x);
			movementRoot.localScale = scale;
		}

		if (distance <= stoppingDistance)
		{
			movementRoot.position = targetPosition;
			hasTarget = false;
			return;
		}

		Vector3 next = Vector3.MoveTowards(current, targetPosition, moveSpeed * Time.deltaTime);
		movementRoot.position = next;
	}

	/// <summary>
	/// GuestController를 사용한 애니메이션 파라미터 업데이트
	/// </summary>
	private void UpdateAnimationFromGuestController()
	{
		if (!updateAnimatorParameters || animator == null || guestController == null)
			return;

		// GuestController의 이동 상태 확인
		bool isMoving = guestController.IsMoving;
		
		// Y축 이동 방향 계산
		float directionY = 0f;
		if (isMoving)
		{
			// GuestController의 CurrentVelocity 사용 (더 정확함)
			Vector3 velocity = guestController.CurrentVelocity;
			if (velocity.magnitude > 0.001f)
			{
				// 이동 방향 벡터를 정규화하여 Y축 성분 추출
				Vector3 normalizedDirection = velocity.normalized;
				directionY = normalizedDirection.y;
			}
			else
			{
				// CurrentVelocity가 없으면 위치 변화로 계산 (폴백)
				Vector3 currentPosition = movementRoot.position;
				Vector3 movementDelta = currentPosition - previousPosition;
				
				if (movementDelta.magnitude > 0.001f)
				{
					Vector3 normalizedDirection = movementDelta.normalized;
					directionY = normalizedDirection.y;
				}
				
				previousPosition = currentPosition;
			}
		}
		else
		{
			// 이동하지 않을 때는 이전 위치 업데이트만 수행
			previousPosition = movementRoot.position;
		}

		// Animator 파라미터 설정
		animator.SetBool(isWalkingParameter, isMoving);
		animator.SetFloat(moveYParameter, directionY);
	}

	/// <summary>
	/// 애니메이션 파라미터 업데이트 (수동 이동 모드용)
	/// </summary>
	private void UpdateAnimation()
	{
		if (!updateAnimatorParameters || animator == null)
			return;

        float directionY = 0f; // Y축 방향 값 저장 변수
		if (hasTarget)
		{
			Vector3 velocity = (targetPosition - movementRoot.position) / Mathf.Max(Time.deltaTime, 0.0001f);
			// Y축 방향 계산 (수정된 부분)
            // velocity.y가 0.01보다 크면 '위로', -0.01보다 작으면 '아래로'
            directionY = velocity.y;
		}

		bool isWalking = hasTarget;
		animator.SetBool(isWalkingParameter, isWalking);

		animator.SetFloat(moveYParameter, directionY);
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
		if (animator != null && !string.IsNullOrEmpty(triggerName))
		{
			animator.SetTrigger(triggerName);
		}
	}
}


