using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// 칵테일 제작 시 잔 선택 UI
/// </summary>
public class GlassSelectionUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Transform glassListContent;
    [SerializeField] private GameObject glassButtonPrefab;

    private CocktailSystem cocktailSystem;
    private List<GameObject> instantiatedButtons = new List<GameObject>();
    private int selectedGlassId = -1;

    public void Initialize(CocktailSystem system)
    {
        cocktailSystem = system;
    }

    public void ShowAvailableGlasses()
    {
        ClearGlasses();

        // GlassRepository에서 사용 가능한 잔 목록 가져오기
        if (GlassRepository.Instance != null)
        {
            var glasses = GlassRepository.Instance.GetAllGlasses();
            foreach (var glass in glasses)
            {
                CreateGlassButton(glass);
            }
        }
    }

    private void CreateGlassButton(Glass glassData)
    {
        if (glassButtonPrefab == null || glassListContent == null) return;

        GameObject button = Instantiate(glassButtonPrefab, glassListContent);
        instantiatedButtons.Add(button);

        // 버튼 설정
        Button btn = button.GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.AddListener(() => OnGlassSelected(glassData.Glass_id));
        }

        // 잔 정보 표시
        Image icon = button.GetComponentInChildren<Image>();
        if (icon != null && glassData.Icon != null)
        {
            icon.sprite = glassData.Icon;
        }

        TextMeshProUGUI nameText = button.GetComponentInChildren<TextMeshProUGUI>();
        if (nameText != null)
        {
            nameText.text = glassData.Glass_name;
        }
    }

    private void OnGlassSelected(int glassId)
    {
        selectedGlassId = glassId;
        cocktailSystem.SetSelectedGlass(glassId);
        Debug.Log($"잔 선택: ID {glassId}");
    }

    private void ClearGlasses()
    {
        foreach (GameObject btn in instantiatedButtons)
        {
            Destroy(btn);
        }
        instantiatedButtons.Clear();
    }

    public void ResetSelection()
    {
        selectedGlassId = -1;
    }
}
