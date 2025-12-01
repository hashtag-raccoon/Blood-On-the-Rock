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

    private CocktailSystem cocktailSystem;
    private List<GameObject> instantiatedButtons = new List<GameObject>();

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

        // 재료 정보 표시 (이름, 아이콘 등)
        TextMeshProUGUI nameText = button.GetComponentInChildren<TextMeshProUGUI>();
        if (nameText != null)
        {
            nameText.text = ingredient.Ingridiant_name;
        }
    }

    private void OnIngredientSelected(int ingredientId, Ingridiant ingredient)
    {
        cocktailSystem.AddIngredient(ingredientId, ingredient);
        Debug.Log($"재료 추가: {ingredient.Ingridiant_name}");
    }

    private void ClearIngredients()
    {
        foreach (GameObject btn in instantiatedButtons)
        {
            Destroy(btn);
        }
        instantiatedButtons.Clear();
    }
}
