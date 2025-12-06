using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// 칵테일 제작 시 재료 선택 UI
/// </summary>
public class IngredientSelectionUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Transform ingredientListContent;
    [SerializeField] private GameObject ingredientButtonPrefab;

    [Header("Category Filter Buttons")]
    [SerializeField] private Button alcoholButton;   // 술 버튼
    [SerializeField] private Button drinkButton;     // 음료 버튼
    [SerializeField] private Button iceButton;       // 얼음 버튼
    [SerializeField] private Button garnishButton;   // 가니쉬 버튼
    [SerializeField] private Button allButton;       // 전체 보기 버튼 (선택사항)

    [Header("Scroll View Toggle")]
    [SerializeField] private Button toggleScrollViewButton;  // 스크롤뷰 토글 버튼
    [SerializeField] private GameObject scrollViewObject;     // 토글할 스크롤뷰 오브젝트

    private CocktailSystem cocktailSystem;
    private List<GameObject> instantiatedButtons = new List<GameObject>();
    private IngridiantSO cachedIngridiantSO; // 캐싱하여 재사용

    /// <summary>
    /// 재료가 선택되었을 때 발생하는 이벤트 (GlassSelectionUI 패턴 재활용)
    /// </summary>
    public event System.Action<Ingridiant> OnIngredientSelectedEvent;

    private void Awake()
    {
        // 종류별 필터 버튼 이벤트 등록
        alcoholButton?.onClick.AddListener(() => ShowIngredientsByCategory("Alchol"));
        drinkButton?.onClick.AddListener(() => ShowIngredientsByCategory("Drink"));
        iceButton?.onClick.AddListener(() => ShowIngredientsByCategory("Ice"));
        garnishButton?.onClick.AddListener(() => ShowIngredientsByCategory("Garnish"));
        allButton?.onClick.AddListener(() => ShowAvailableIngredients());

        // 스크롤뷰 토글 버튼 이벤트 등록
        toggleScrollViewButton?.onClick.AddListener(ToggleScrollView);
    }

    public void Initialize(CocktailSystem system)
    {
        cocktailSystem = system;
    }

    public void ShowAvailableIngredients()
    {
        ClearIngredients();

        // DataManager에서 IngridiantSO를 가져와 모든 재료를 표시
        IngridiantSO ingridiantSO = DataManager.Instance?.GetIngridiantSO();
        if (ingridiantSO == null)
        {
            Debug.LogWarning("DataManager 또는 IngridiantSO가 null입니다.");
            return;
        }

        // 술 재료 표시
        foreach (var ingredient in ingridiantSO.ingridiants_Alchol)
        {
            if (ingredient != null)
            {
                CreateIngredientButton(ingredient.Ingridiant_id, ingredient);
            }
        }

        // 음료 재료 표시
        foreach (var ingredient in ingridiantSO.ingridiants_Drink)
        {
            if (ingredient != null)
            {
                CreateIngredientButton(ingredient.Ingridiant_id, ingredient);
            }
        }

        // 얼음 재료 표시
        foreach (var ingredient in ingridiantSO.ingridiants_Ice)
        {
            if (ingredient != null)
            {
                CreateIngredientButton(ingredient.Ingridiant_id, ingredient);
            }
        }

        // 가니쉬 재료 표시
        foreach (var ingredient in ingridiantSO.ingridiants_Garnish)
        {
            if (ingredient != null)
            {
                CreateIngredientButton(ingredient.Ingridiant_id, ingredient);
            }
        }
    }

    private void CreateIngredientButton(int ingredientId, Ingridiant ingredient)
    {
        if (ingredientButtonPrefab == null || ingredientListContent == null) return;

        GameObject button = Instantiate(ingredientButtonPrefab, ingredientListContent);
        instantiatedButtons.Add(button);

        // 버튼 설정
        Button btn = button.GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.AddListener(() => OnIngredientSelected(ingredientId, ingredient));
        }

        // 재료 아이콘 표시 - "Icon"이라는 이름의 자식 오브젝트를 먼저 찾기
        Image icon = null;
        Transform iconTransform = button.transform.Find("Icon");
        if (iconTransform != null)
        {
            icon = iconTransform.GetComponent<Image>();
        }

        // "Icon" 이름의 오브젝트가 없으면 자식들 중에서 버튼 자신이 아닌 Image 찾기
        if (icon == null)
        {
            Image[] images = button.GetComponentsInChildren<Image>();
            foreach (var img in images)
            {
                // 버튼 자체의 Image가 아닌 자식 Image 찾기
                if (img.gameObject != button)
                {
                    icon = img;
                    break;
                }
            }
        }

        if (icon != null && ingredient.Icon != null)
        {
            icon.sprite = ingredient.Icon;
            Debug.Log($"재료 아이콘 설정됨: {ingredient.Ingridiant_name}");
        }
        else if (ingredient.Icon == null)
        {
            Debug.LogWarning($"재료 '{ingredient.Ingridiant_name}'의 Icon이 null입니다. ScriptableObject에서 Icon을 할당해주세요.");
        }

        // 재료 이름 표시 - "Name", "NameText", "Text" 등의 이름 찾기
        TextMeshProUGUI nameText = null;
        string[] textNames = { "Name", "NameText", "Text", "Label" };
        foreach (var textName in textNames)
        {
            Transform textTransform = button.transform.Find(textName);
            if (textTransform != null)
            {
                nameText = textTransform.GetComponent<TextMeshProUGUI>();
                if (nameText != null) break;
            }
        }

        // 이름으로 못 찾으면 GetComponentInChildren으로 찾기
        if (nameText == null)
        {
            nameText = button.GetComponentInChildren<TextMeshProUGUI>();
        }

        if (nameText != null)
        {
            nameText.text = ingredient.Ingridiant_name;
            Debug.Log($"재료 이름 설정됨: {ingredient.Ingridiant_name}");
        }
        else
        {
            Debug.LogWarning($"재료 '{ingredient.Ingridiant_name}' 버튼에서 TextMeshProUGUI를 찾을 수 없습니다.");
        }
    }

    private void OnIngredientSelected(int ingredientId, Ingridiant ingredient)
    {
        cocktailSystem.AddIngredient(ingredientId, ingredient);
        Debug.Log($"재료 추가: {ingredient.Ingridiant_name}");

        // 외부에 이벤트 알림 (CocktailMakingUI에서 테이블 위에 재료 표시)
        OnIngredientSelectedEvent?.Invoke(ingredient);
    }

    private void ClearIngredients()
    {
        foreach (GameObject btn in instantiatedButtons)
        {
            Destroy(btn);
        }
        instantiatedButtons.Clear();
    }

    /// <summary>
    /// 종류별 재료 표시 (술/음료/얼음/가니쉬)
    /// </summary>
    /// <param name="category">Alchol, Drink, Ice, Garnish 중 하나</param>
    public void ShowIngredientsByCategory(string category)
    {
        ClearIngredients();

        // IngridiantSO 캐싱
        if (cachedIngridiantSO == null)
        {
            cachedIngridiantSO = DataManager.Instance?.GetIngridiantSO();
        }

        if (cachedIngridiantSO == null)
        {
            Debug.LogWarning("DataManager 또는 IngridiantSO가 null입니다.");
            return;
        }

        // 종류에 따라 해당 리스트만 표시 (기존 CreateIngredientButton 재활용)
        List<Ingridiant> ingredientsToShow = null;

        switch (category)
        {
            case "Alchol":
                ingredientsToShow = cachedIngridiantSO.ingridiants_Alchol;
                break;
            case "Drink":
                ingredientsToShow = cachedIngridiantSO.ingridiants_Drink;
                break;
            case "Ice":
                ingredientsToShow = cachedIngridiantSO.ingridiants_Ice;
                break;
            case "Garnish":
                ingredientsToShow = cachedIngridiantSO.ingridiants_Garnish;
                break;
            default:
                Debug.LogWarning($"알 수 없는 재료 종류: {category}");
                return;
        }

        if (ingredientsToShow != null)
        {
            foreach (var ingredient in ingredientsToShow)
            {
                if (ingredient != null)
                {
                    CreateIngredientButton(ingredient.Ingridiant_id, ingredient);
                }
            }
        }

        Debug.Log($"재료 종류 '{category}' 표시 완료. 총 {instantiatedButtons.Count}개");
    }

    /// <summary>
    /// 스크롤뷰 토글 (보이기/숨기기)
    /// </summary>
    public void ToggleScrollView()
    {
        if (scrollViewObject != null)
        {
            bool isActive = scrollViewObject.activeSelf;
            scrollViewObject.SetActive(!isActive);
            Debug.Log($"스크롤뷰 {(isActive ? "숨김" : "표시")}");
        }
        else
        {
            Debug.LogWarning("scrollViewObject가 할당되지 않았습니다.");
        }
    }

    /// <summary>
    /// 스크롤뷰 보이기
    /// </summary>
    public void ShowScrollView()
    {
        if (scrollViewObject != null)
        {
            scrollViewObject.SetActive(true);
        }
    }

    /// <summary>
    /// 스크롤뷰 숨기기
    /// </summary>
    public void HideScrollView()
    {
        if (scrollViewObject != null)
        {
            scrollViewObject.SetActive(false);
        }
    }
}
