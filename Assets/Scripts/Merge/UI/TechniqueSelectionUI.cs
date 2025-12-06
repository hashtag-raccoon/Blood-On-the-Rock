using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 칵테일 제작 기법 선택 UI (Build, Floating, Shaking)
/// </summary>
public class TechniqueSelectionUI : MonoBehaviour
{
    [Header("Technique Buttons")]
    [SerializeField] private Button buildButton;
    [SerializeField] private Button floatingButton;
    [SerializeField] private Button shakingButton;

    [Header("Visual Feedback")]
    [SerializeField] private Color selectedColor = Color.green;
    [SerializeField] private Color normalColor = Color.white;

    private CocktailSystem cocktailSystem;
    private int selectedTechnique = -1;

    public void Initialize(CocktailSystem system)
    {
        cocktailSystem = system;

        if (buildButton != null)
            buildButton.onClick.AddListener(() => SelectTechnique(0));

        if (floatingButton != null)
            floatingButton.onClick.AddListener(() => SelectTechnique(1));

        if (shakingButton != null)
            shakingButton.onClick.AddListener(() => SelectTechnique(2));
    }

    private void SelectTechnique(int techniqueId)
    {
        selectedTechnique = techniqueId;
        cocktailSystem.SetSelectedTechnique(techniqueId);

        UpdateButtonVisuals();

        Debug.Log($"기법 선택: {GetTechniqueName(techniqueId)}");
    }

    private void UpdateButtonVisuals()
    {
        // 모든 버튼을 기본 색상으로
        SetButtonColor(buildButton, normalColor);
        SetButtonColor(floatingButton, normalColor);
        SetButtonColor(shakingButton, normalColor);

        // 선택된 버튼만 강조
        switch (selectedTechnique)
        {
            case 0:
                SetButtonColor(buildButton, selectedColor);
                break;
            case 1:
                SetButtonColor(floatingButton, selectedColor);
                break;
            case 2:
                SetButtonColor(shakingButton, selectedColor);
                break;
        }
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

    /// <summary>
    /// 외부에서 기법을 자동으로 설정합니다 (ToolSelectionUI에서 호출)
    /// </summary>
    /// <param name="techniqueId">설정할 기법 ID (0=Build, 1=Floating, 2=Shaking)</param>
    public void SetTechnique(int techniqueId)
    {
        if (techniqueId < 0 || techniqueId > 2)
        {
            Debug.LogWarning($"잘못된 기법 ID: {techniqueId}");
            return;
        }

        SelectTechnique(techniqueId);
    }

    public void ResetSelection()
    {
        selectedTechnique = -1;
        UpdateButtonVisuals();
    }

    private void OnDestroy()
    {
        if (buildButton != null)
            buildButton.onClick.RemoveAllListeners();
        if (floatingButton != null)
            floatingButton.onClick.RemoveAllListeners();
        if (shakingButton != null)
            shakingButton.onClick.RemoveAllListeners();
    }
}
