using UnityEngine;

public class Shaker : MonoBehaviour
{
    [Header("쉐이커 매니저 오브젝트")]
    public GameObject Manager; // 쉐이커 매니저 오브젝트

    private RectTransform rectTransform;

    private bool isDragging = false;

    private float lastMouseY;
    private float lastMouseX;
    private int dragDirection = 0;      // 1: 위로, -1: 아래로, 0: 초기값
    private float dragDistance = 0f;

    private int horizontalDirection = 0; // 1: 오른쪽, -1: 왼쪽, 0: 초기값
    [Header("최대 회전 각도")]
    [Range(0f, 90f)]
    public float maxRotationAngle = 30f;    // 최대 회전 각도
    [Header("회전 보간 계수")]
    [Range(0f, 20f)]
    public float rotationSmooth = 10f;      // 회전 보간 계수

    [Header("건들지 말것, 각도 확인용")]
    [SerializeField]
    private float targetAngle = 0f;
    [SerializeField]
    private float currentAngle = 0f;

    // 쉐이킹 감지용 변수 추가
    private float prevY;
    private int lastDir = 0; // 1=위, -1=아래, 0=초기값
    public float shakeThreshold = 10f; // 마우스 Y 이동 기준 (픽셀 단위)

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    void Start()
    {
        prevY = Input.mousePosition.y;
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector2 mouseScreenPos = Input.mousePosition;
            if (RectTransformUtility.RectangleContainsScreenPoint(rectTransform, mouseScreenPos, null))
            {
                isDragging = true;
                lastMouseY = mouseScreenPos.y;
                lastMouseX = mouseScreenPos.x;
                dragDirection = 0;
                dragDistance = 0f;
                horizontalDirection = 0;
                prevY = mouseScreenPos.y; // 드래그 시작 시 prevY 초기화
                lastDir = 0;
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            isDragging = false;
            targetAngle = 0f;
        }
    }

    void FixedUpdate()
    {
        if (isDragging)
        {
            float currentMouseY = Input.mousePosition.y;
            float currentMouseX = Input.mousePosition.x;
            float deltaY = currentMouseY - lastMouseY;
            float deltaX = currentMouseX - lastMouseX;

            // 쉐이킹 감지 로직
            float shakeDeltaY = currentMouseY - prevY;
            int curDir = 0;
            if (shakeDeltaY > shakeThreshold) curDir = 1;
            else if (shakeDeltaY < -shakeThreshold) curDir = -1;

            if (curDir != 0 && lastDir != 0 && curDir != lastDir)
            {
                OnShakingDetected();
                lastDir = 0;
            }
            else if (curDir != 0)
            {
                lastDir = curDir;
            }
            prevY = currentMouseY;

            if (Mathf.Abs(deltaY) > 0.1f)
            {
                int currentDirection = deltaY > 0 ? 1 : -1;

                if (Mathf.Abs(deltaX) > 0.1f)
                {
                    horizontalDirection = deltaX > 0 ? 1 : -1;
                }

                if (dragDirection == 0)
                {
                    dragDirection = currentDirection;
                    dragDistance = Mathf.Abs(deltaY);
                }
                else if (currentDirection == dragDirection)
                {
                    dragDistance += Mathf.Abs(deltaY);
                }
                else
                {
                    UpdateTargetAngle(dragDistance, horizontalDirection);
                    dragDirection = currentDirection;
                    dragDistance = Mathf.Abs(deltaY);
                }

                UpdateTargetAngle(dragDistance, horizontalDirection);
            }

            // 부드럽게 현재 각도를 목표 각도로 보간
            currentAngle = Mathf.Lerp(currentAngle, targetAngle, rotationSmooth * Time.fixedDeltaTime);

            // RectTransform 회전 적용
            rectTransform.rotation = Quaternion.Euler(0, 0, currentAngle);

            lastMouseY = currentMouseY;
            lastMouseX = currentMouseX;

            // 마우스 위치를 월드 좌표로 변환하여 UI 이동
            Vector2 mouseScreenPos = Input.mousePosition;
            RectTransform parentRect = rectTransform.parent as RectTransform;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentRect,
                mouseScreenPos,
                null,
                out Vector2 uiPos
            );

            // workspace UI의 영역 내로 제한
            Vector2 min = parentRect.rect.min;
            Vector2 max = parentRect.rect.max;
            Vector2 size = rectTransform.rect.size;
            float halfWidth = size.x * 0.5f;
            float halfHeight = size.y * 0.5f;

            // 회전 시 실제 차지하는 크기 계산
            float angleRad = Mathf.Abs(currentAngle) * Mathf.Deg2Rad;
            float rotatedHalfWidth = Mathf.Abs(halfWidth * Mathf.Cos(angleRad)) + Mathf.Abs(halfHeight * Mathf.Sin(angleRad));
            float rotatedHalfHeight = Mathf.Abs(halfHeight * Mathf.Cos(angleRad)) + Mathf.Abs(halfWidth * Mathf.Sin(angleRad));

            uiPos.x = Mathf.Clamp(uiPos.x, min.x + rotatedHalfWidth, max.x - rotatedHalfWidth);
            uiPos.y = Mathf.Clamp(uiPos.y, min.y + rotatedHalfHeight, max.y - rotatedHalfHeight);

            // 제한된 위치로 부드럽게 이동
            rectTransform.anchoredPosition = Vector2.Lerp(rectTransform.anchoredPosition, uiPos, 0.7f);
        }
        else
        {
            // 드래그 끝나면 회전 원위치로 보간
            currentAngle = Mathf.Lerp(currentAngle, 0f, rotationSmooth * Time.fixedDeltaTime);
            rectTransform.rotation = Quaternion.Euler(0, 0, currentAngle);
        }
    }

    void UpdateTargetAngle(float distance, int hDirection)
    {
        // 거리 비례 회전 각도 산출, 최소 5도 이상, 최대 maxRotationAngle 이하
        float angleMagnitude = Mathf.Clamp(distance * 0.1f, 5f, maxRotationAngle);

        // 가로 방향에 따라 회전 방향 결정 (오른쪽: 시계방향, 왼쪽: 반시계방향)
        if (hDirection == 0)
            hDirection = 1; // 기본값(오른쪽) 처리

        targetAngle = (-hDirection) * angleMagnitude;
    }

    void OnShakingDetected()
    {
        // 쉐이킹 1회 감지 시 원하는 동작 실행
        Manager.GetComponent<CocktailManager>().ShakingNumber += 1;
    }
}