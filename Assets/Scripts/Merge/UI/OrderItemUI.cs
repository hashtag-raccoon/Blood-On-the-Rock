using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 주문 목록에서 개별 주문 아이템을 표시하고 클릭 처리하는 UI 컴포넌트
/// </summary>
public class OrderItemUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image cocktailIcon;
    [SerializeField] private TextMeshProUGUI cocktailNameText;
    [SerializeField] private GameObject completedCheckmark; // 완료 체크 표시

    private CocktailRecipeScript recipe;
    private System.Action<CocktailRecipeScript> onClickCallback;
    private Button itemButton;

    private void Awake()
    {
        itemButton = GetComponent<Button>();
        if (itemButton != null)
        {
            itemButton.onClick.AddListener(OnItemClicked);
        }
    }

    /// <summary>
    /// 주문 데이터 설정 및 UI 업데이트
    /// </summary>
    public void SetData(CocktailRecipeScript cocktailRecipe, System.Action<CocktailRecipeScript> clickCallback)
    {
        recipe = cocktailRecipe;
        onClickCallback = clickCallback;

        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        if (recipe == null) return;

        // 칵테일 이름 설정
        if (cocktailNameText != null)
        {
            cocktailNameText.text = recipe.CocktailName;
        }

        // 칵테일 아이콘 설정 (CocktailRepository에서 조회)
        if (cocktailIcon != null)
        {
            CocktailData data = CocktailRepository.Instance?.GetCocktailDataById(recipe.CocktailId);
            if (data != null && data.Icon != null)
            {
                cocktailIcon.sprite = data.Icon;
            }
        }

        // 완료 체크 표시 업데이트
        UpdateCompletedStatus();
    }

    /// <summary>
    /// 완료 상태 업데이트 (외부에서 호출 가능)
    /// </summary>
    public void UpdateCompletedStatus()
    {
        bool isCompleted = OrderingManager.Instance.CompletedCocktails.Contains(recipe);
        if (completedCheckmark != null)
        {
            completedCheckmark.SetActive(isCompleted);
        }
    }

    private void OnItemClicked()
    {
        onClickCallback?.Invoke(recipe);
    }

    private void OnDestroy()
    {
        if (itemButton != null)
        {
            itemButton.onClick.RemoveListener(OnItemClicked);
        }
    }
}
