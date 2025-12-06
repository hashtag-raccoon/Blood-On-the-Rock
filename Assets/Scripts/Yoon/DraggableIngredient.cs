using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// 테이블 위에 표시되는 드래그 가능한 재료 UI 컴포넌트
/// IBeginDragHandler, IDragHandler, IEndDragHandler 인터페이스를 구현하여
/// 마우스/터치로 UI 요소를 자유롭게 이동할 수 있습니다.
/// </summary>
public class DraggableIngredient : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("UI References")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI nameText;

    private Ingridiant ingredientData;
    private RectTransform rectTransform;
    private Canvas parentCanvas;
    private CanvasGroup canvasGroup;
    private Vector2 originalPosition;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();

        // CanvasGroup이 없으면 추가 (드래그 중 투명도 조절용)
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
    }

    private void Start()
    {
        // 부모 Canvas 찾기 (좌표 변환에 필요)
        parentCanvas = GetComponentInParent<Canvas>();
    }

    /// <summary>
    /// 재료 데이터를 설정하고 UI를 업데이트합니다.
    /// </summary>
    /// <param name="ingredient">표시할 재료 데이터</param>
    public void SetIngredient(Ingridiant ingredient)
    {
        ingredientData = ingredient;

        if (ingredient == null) return;

        // iconImage가 할당되지 않은 경우 자동으로 찾기
        if (iconImage == null)
        {
            iconImage = GetComponent<Image>();
            // 자신에게 없으면 자식에서 찾기
            if (iconImage == null)
            {
                iconImage = GetComponentInChildren<Image>();
            }
        }

        // 아이콘 이미지 설정
        if (iconImage != null)
        {
            if (ingredient.Icon != null)
            {
                iconImage.sprite = ingredient.Icon;
                iconImage.gameObject.SetActive(true);
                Debug.Log($"재료 아이콘 설정됨: {ingredient.Ingridiant_name}");
            }
            else
            {
                Debug.LogWarning($"재료 '{ingredient.Ingridiant_name}'의 Icon이 null입니다. ScriptableObject에서 Icon을 할당해주세요.");
            }
        }
        else
        {
            Debug.LogWarning("DraggableIngredient: iconImage를 찾을 수 없습니다. Image 컴포넌트가 있는지 확인하세요.");
        }

        // 이름 텍스트 설정 (선택사항)
        if (nameText != null)
        {
            nameText.text = ingredient.Ingridiant_name;
        }
    }

    /// <summary>
    /// 재료 데이터를 반환합니다.
    /// </summary>
    public Ingridiant GetIngredient()
    {
        return ingredientData;
    }

    #region Drag Handlers

    /// <summary>
    /// 드래그 시작 시 호출됩니다.
    /// </summary>
    public void OnBeginDrag(PointerEventData eventData)
    {
        // 원래 위치 저장 (필요 시 복원용)
        originalPosition = rectTransform.anchoredPosition;

        // 드래그 중 반투명하게 표시
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0.7f;
            canvasGroup.blocksRaycasts = false; // 드래그 중 다른 UI 요소가 이벤트를 받을 수 있도록
        }

        Debug.Log($"드래그 시작: {ingredientData?.Ingridiant_name}");
    }

    /// <summary>
    /// 드래그 중 매 프레임 호출됩니다.
    /// </summary>
    public void OnDrag(PointerEventData eventData)
    {
        if (rectTransform == null) return;

        // Canvas 스케일을 고려한 이동
        if (parentCanvas != null)
        {
            rectTransform.anchoredPosition += eventData.delta / parentCanvas.scaleFactor;
        }
        else
        {
            rectTransform.anchoredPosition += eventData.delta;
        }
    }

    /// <summary>
    /// 드래그 종료 시 호출됩니다.
    /// </summary>
    public void OnEndDrag(PointerEventData eventData)
    {
        // 투명도 복원
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
        }

        Debug.Log($"드래그 종료: {ingredientData?.Ingridiant_name}");
    }

    #endregion

    /// <summary>
    /// 원래 위치로 복원합니다. (필요 시 사용)
    /// </summary>
    public void ResetPosition()
    {
        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = originalPosition;
        }
    }
}
