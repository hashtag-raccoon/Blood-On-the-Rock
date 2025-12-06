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

        // GlassRepository에서 소유한 잔 목록만 가져오기
        if (GlassRepository.Instance != null)
        {
            var glasses = GlassRepository.Instance.GetOwnedGlasses();
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

        // 잔 아이콘 표시 - "Icon"이라는 이름의 자식 오브젝트를 먼저 찾기
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

        if (icon != null && glassData.Icon != null)
        {
            icon.sprite = glassData.Icon;
            Debug.Log($"잔 아이콘 설정됨: {glassData.Glass_name}");
        }
        else if (glassData.Icon == null)
        {
            Debug.LogWarning($"잔 '{glassData.Glass_name}'의 Icon이 null입니다. ScriptableObject에서 Icon을 할당해주세요.");
        }

        TextMeshProUGUI nameText = button.GetComponentInChildren<TextMeshProUGUI>();
        if (nameText != null)
        {
            nameText.text = glassData.Glass_name;
        }
    }

    public event System.Action<Glass> OnGlassSelectedEvent;

    private void OnGlassSelected(int glassId)
    {
        selectedGlassId = glassId;
        cocktailSystem.SetSelectedGlass(glassId);
        Debug.Log($"잔 선택: ID {glassId}");

        // 선택된 잔 정보를 이벤트로 알림
        if (GlassRepository.Instance != null)
        {
            Glass glass = GlassRepository.Instance.GetGlassById(glassId);
            if (glass != null)
            {
                OnGlassSelectedEvent?.Invoke(glass);
            }
        }
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
