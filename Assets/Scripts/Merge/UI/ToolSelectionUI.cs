using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 칵테일 제작 도구 선택 UI (쉐이커, 바스푼)
/// 도구 선택 시 자동으로 레시피에 맞는 기법을 설정합니다.
/// 도구 ID:
/// - 0 = 쉐이커 (Shaker) → Shaking(2) 전용
/// - 1 = 바스푼 (BarSpoon) → Build(0), Floating(1) 전용
/// </summary>
public class ToolSelectionUI : MonoBehaviour
{
    [Header("Tool Buttons")]
    [SerializeField] private Button shakerButton;    // 쉐이커
    [SerializeField] private Button barSpoonButton;  // 바스푼

    [Header("Visual Feedback")]
    [SerializeField] private Color selectedColor = Color.green;
    [SerializeField] private Color normalColor = Color.white;

    private CocktailSystem cocktailSystem;
    private TechniqueSelectionUI techniqueSelectionUI;
    private CocktailRecipeScript currentRecipe;
    private int selectedTool = -1;

    public void Initialize(CocktailSystem system, TechniqueSelectionUI techniqueUI)
    {
        cocktailSystem = system;
        techniqueSelectionUI = techniqueUI;

        if (shakerButton != null)
            shakerButton.onClick.AddListener(() => SelectTool(0));

        if (barSpoonButton != null)
            barSpoonButton.onClick.AddListener(() => SelectTool(1));
    }

    public void SetCurrentRecipe(CocktailRecipeScript recipe)
    {
        currentRecipe = recipe;
    }

    private void SelectTool(int toolId)
    {
        selectedTool = toolId;
        cocktailSystem.SetSelectedTool(toolId);

        AutoSelectTechnique();  // 자동으로 기법 설정
        UpdateButtonVisuals();

        Debug.Log($"도구 선택: {(toolId == 0 ? "쉐이커" : "바스푼")}");
    }

    private void AutoSelectTechnique()
    {
        if (currentRecipe == null || techniqueSelectionUI == null) return;

        int recipeTechnique = currentRecipe.Technique;

        // Shaker (0) → Shaking(2)
        if (selectedTool == 0 && recipeTechnique == 2)
        {
            techniqueSelectionUI.SetTechnique(2);
            Debug.Log("쉐이커 선택 → Shaking 기법 자동 설정");
        }
        // BarSpoon (1) → Build(0) or Floating(1)
        else if (selectedTool == 1 && (recipeTechnique == 0 || recipeTechnique == 1))
        {
            techniqueSelectionUI.SetTechnique(recipeTechnique);
            Debug.Log($"바스푼 선택 → {GetTechniqueName(recipeTechnique)} 기법 자동 설정");
        }
        else
        {
            Debug.LogWarning($"선택한 도구({(selectedTool == 0 ? "쉐이커" : "바스푼")})가 레시피 기법({GetTechniqueName(recipeTechnique)})과 호환되지 않습니다.");
        }
    }

    private void UpdateButtonVisuals()
    {
        // 모든 버튼을 기본 색상으로
        SetButtonColor(shakerButton, normalColor);
        SetButtonColor(barSpoonButton, normalColor);

        // 선택된 버튼만 강조
        if (selectedTool == 0)
            SetButtonColor(shakerButton, selectedColor);
        else if (selectedTool == 1)
            SetButtonColor(barSpoonButton, selectedColor);
    }

    private void SetButtonColor(Button button, Color color)
    {
        if (button != null)
        {
            Image img = button.GetComponent<Image>();
            if (img != null)
            {
                img.color = color;
            }
        }
    }

    private string GetTechniqueName(int techniqueId)
    {
        switch (techniqueId)
        {
            case 0: return "Build";
            case 1: return "Floating";
            case 2: return "Shaking";
            default: return "Unknown";
        }
    }

    public void ResetSelection()
    {
        selectedTool = -1;
        UpdateButtonVisuals();
    }

    private void OnDestroy()
    {
        if (shakerButton != null)
            shakerButton.onClick.RemoveAllListeners();
        if (barSpoonButton != null)
            barSpoonButton.onClick.RemoveAllListeners();
    }
}
