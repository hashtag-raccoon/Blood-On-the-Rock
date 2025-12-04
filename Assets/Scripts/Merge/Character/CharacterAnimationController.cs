using UnityEngine;

/// <summary>
/// 공용 캐릭터 이동/애니메이션 컨트롤러
/// - GuestController, ArbeitController, PlayerableController의 이동 상태를 감지하여 애니메이션 작동
/// - 이동 방향에 따른 좌우 반전
/// - Animator 파라미터 업데이트
/// 모든 캐릭터 프리팹에 공통으로 붙여 사용 가능합니다.
/// </summary>
[DisallowMultipleComponent]
public class CharacterAnimationController : MonoBehaviour
{
	[Header("Animator")]
	[SerializeField] private Animator animator;
	[SerializeField] private bool forceDisableRootMotion = true;
	[SerializeField] private bool updateAnimatorParameters = true;
	[SerializeField] private string isWalkingParameter = "IsWalking";
	[SerializeField] private string moveYParameter = "MoveY";

	[Header("Controller References")]
	[SerializeField] private Transform movementRoot; // 위치 이동 대상 (기본값: 자기 자신)

	// 컨트롤러 참조 (자동 감지하여 감지된 컨트롤러를 사용함)
	private GuestController guestController;
	private ArbeitController arbeitController;
	private PlayerableController playerableController;

	// 이전 프레임 위치 (방향 계산용)
	private Vector3 previousPosition;

	private void Awake()
	{
		if (animator == null)
		{
			animator = GetComponent<Animator>();
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

		// 컨트롤러 자동 감지하여 감지된 컨트롤러를 사용함
		guestController = GetComponent<GuestController>();
		arbeitController = GetComponent<ArbeitController>();
		playerableController = GetComponent<PlayerableController>();
	}

	private void Start()
	{
		previousPosition = movementRoot.position;
	}

	private void Update()
	{
		UpdateAnimation();
	}

	/// <summary>
	/// 애니메이션 파라미터 업데이트
	/// GuestController, ArbeitController, PlayerableController의 이동 상태를 감지하여 애니메이션 작동함
	/// </summary>
	private void UpdateAnimation()
	{
		if (!updateAnimatorParameters || animator == null)
			return;

		// 현재 위치와 이전 위치를 비교하여 이동 방향 계산
		Vector3 currentPosition = movementRoot.position;
		Vector3 velocity = (currentPosition - previousPosition) / Mathf.Max(Time.deltaTime, 0.0001f);

		// 이동 중인지 판단 (속도가 임계값 이상인지 확인)
		bool isMoving = velocity.magnitude > 0.01f;

		// 이동 방향에 따라 캐릭터 좌우 반전
		if (isMoving && Mathf.Abs(velocity.x) > 0.01f)
		{
			Vector3 scale = movementRoot.localScale;
			if (velocity.x < 0)
				scale.x = -Mathf.Abs(scale.x);
			else if (velocity.x > 0)
				scale.x = Mathf.Abs(scale.x);
			movementRoot.localScale = scale;
		}

		// Y축 방향 값 계산 (위/아래 이동)
		float directionY = isMoving ? velocity.y : 0f;

		// Animator 파라미터 업데이트
		animator.SetBool(isWalkingParameter, isMoving);
		animator.SetFloat(moveYParameter, directionY);

		// 이전 위치 업데이트
		previousPosition = currentPosition;
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


