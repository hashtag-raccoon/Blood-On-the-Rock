using UnityEngine;
using UnityEngine.UI;

namespace Merge
{
    /// <summary>
    /// 원형 진행바 UI 컴포넌트
    /// 편집 모드 활성화 대기 시간을 시각적으로 표시
    /// </summary>
    public class CircularProgressBar : MonoBehaviour
    {
        [Header("UI 컴포넌트")]
        [SerializeField] private Image fillImage; // 진행바 이미지
        [SerializeField] private CanvasGroup canvasGroup; // 페이드 인/아웃용
        
        [Header("애니메이션 설정")]
        [SerializeField] private float fadeSpeed = 10f; // 페이드 속도
        [SerializeField] private AnimationCurve progressCurve = AnimationCurve.Linear(0, 0, 1, 1); // 진행 곡선
        
        private bool isVisible = false;
        private float currentProgress = 0f;
        
        private void Awake()
        {
            // 컴포넌트 자동 탐색
            if (fillImage == null)
            {
                fillImage = GetComponentInChildren<Image>();
            }
            
            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                {
                    canvasGroup = gameObject.AddComponent<CanvasGroup>();
                }
            }
            
            // 초기 상태 설정
            if (fillImage != null)
            {
                fillImage.fillAmount = 0f;
                fillImage.type = Image.Type.Filled;
                fillImage.fillMethod = Image.FillMethod.Radial360;
                fillImage.fillOrigin = (int)Image.Origin360.Top; // 위에서 시계방향으로 채워짐
                fillImage.fillClockwise = true;
            }
            
            canvasGroup.alpha = 0f;
            gameObject.SetActive(false);
        }
        
        private void Update()
        {
            // 페이드 인/아웃 처리
            float targetAlpha = isVisible ? 1f : 0f;
            canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, targetAlpha, Time.deltaTime * fadeSpeed);
            
            // 완전히 투명해지면 비활성화
            if (!isVisible && canvasGroup.alpha < 0.01f)
            {
                gameObject.SetActive(false);
            }
        }
        
        /// <summary>
        /// 진행바 표시 시작
        /// </summary>
        public void Show()
        {
            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }
            isVisible = true;
            currentProgress = 0f;
            if (fillImage != null)
            {
                fillImage.fillAmount = 0f;
            }
        }
        
        /// <summary>
        /// 진행바 숨기기
        /// </summary>
        public void Hide()
        {
            isVisible = false;
            currentProgress = 0f;
            if (fillImage != null)
            {
                fillImage.fillAmount = 0f;
            }
        }
        
        /// <summary>
        /// 진행률 업데이트 (0 ~ 1)
        /// </summary>
        public void UpdateProgress(float progress)
        {
            currentProgress = Mathf.Clamp01(progress);
            
            if (fillImage != null)
            {
                // 애니메이션 곡선 적용
                float curvedProgress = progressCurve.Evaluate(currentProgress);
                fillImage.fillAmount = curvedProgress;
            }
        }
        
        public void SetWorldPosition(Vector3 worldPosition, Camera camera)
        {
            if (camera == null) return;
            
            // 월드 좌표를 스크린 좌표로 변환
            Vector3 screenPos = camera.WorldToScreenPoint(worldPosition);
            
            // RectTransform 위치 설정
            RectTransform rectTransform = transform as RectTransform;
            if (rectTransform != null)
            {
                rectTransform.position = screenPos;
            }
        }
        
        /// <summary>
        /// 진행바 색상 설정
        /// </summary>
        public void SetColor(Color color)
        {
            if (fillImage != null)
            {
                fillImage.color = color;
            }
        }
        
        /// <summary>
        /// 진행률 초기화
        /// </summary>
        public void ResetProgress()
        {
            currentProgress = 0f;
            if (fillImage != null)
            {
                fillImage.fillAmount = 0f;
            }
        }
    }
}
