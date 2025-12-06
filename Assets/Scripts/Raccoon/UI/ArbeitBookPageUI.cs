using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 알바 도감 페이지의 단일 페이지 UI (왼쪽 또는 오른쪽 페이지)
/// NPC의 정보를 표시하고 배치 버튼을 제공합니다.
/// </summary>
public class ArbeitBookPageUI : MonoBehaviour
{
    [Header("NPC 정보 UI")]
    [SerializeField] private Image portraitImage; // 초상화
    [SerializeField] private TextMeshProUGUI nameText; // 이름
    [SerializeField] private TextMeshProUGUI raceText; // 종족
    [SerializeField] private TextMeshProUGUI personalityText; // 성격
    [SerializeField] private TextMeshProUGUI specificityText; // 특징

    [Header("능력치 UI (JobCenterButtonUI 방식)")]
    [SerializeField] private Transform servingSlotContainer;
    [SerializeField] private Transform cookingSlotContainer;
    [SerializeField] private Transform cleaningSlotContainer;
    [SerializeField] private GameObject abilitySlotPrefab; // 능력치 슬롯 프리팹

    [Header("능력치 색상")]
    [SerializeField] private Color servingColor = Color.red;
    [SerializeField] private Color cookingColor = Color.yellow;
    [SerializeField] private Color cleaningColor = Color.blue;

    [Header("배치 버튼")]
    [SerializeField] private Button deployButton;

    private npc currentNpc;
    private PageUI parentPageUI;

    private void Awake()
    {
        if (deployButton != null)
        {
            deployButton.onClick.AddListener(OnDeployButtonClicked); // 리스너 추가
        }
    }

    /// <summary>
    /// NPC 데이터 설정 및 UI 업데이트
    /// </summary>
    public void SetNpcData(npc npcData, PageUI pageUI)
    {
        currentNpc = npcData;
        parentPageUI = pageUI;

        if (npcData == null)
        {
            Debug.LogWarning("[ArbeitBookPageUI] npcData가 null입니다.");
            gameObject.SetActive(false);
            return;
        }

        UpdateUI();
    }

    /// <summary>
    /// UI 업데이트
    /// </summary>
    private void UpdateUI()
    {
        if (currentNpc == null) return;

        // 텍스트 정보 업데이트
        if (nameText != null)
            nameText.text = $"이름: {currentNpc.part_timer_name}";
        if (raceText != null)
        {
            // 종족에 따라 적절한 이름 가져옴
            string raceKorean = "";
            switch (currentNpc.race)
            {
                case "Human":
                    raceKorean = "인간";
                    break;
                case "Oak":
                    raceKorean = "오크";
                    break;
                case "Vampire":
                    raceKorean = "뱀파이어";
                    break;
                default:
                    Debug.LogWarning($"[ArbeitRepository] 알 수 없는 종족: {currentNpc.race}");
                    break;
            }
            raceText.text = $"종족: {raceKorean}";
        }
        if (personalityText != null)
            personalityText.text = $"성격: {currentNpc.personality_name}";
        if (specificityText != null)
            specificityText.text = $"특징: {currentNpc.specificity}";

        // 초상화 이미지 업데이트
        if (portraitImage != null && currentNpc.portraitSprite != null)
        {
            portraitImage.sprite = ArbeitRepository.Instance.GetPortraitByPrefabName(currentNpc);
            portraitImage.gameObject.SetActive(true);
        }
        else if (portraitImage != null)
        {
            portraitImage.gameObject.SetActive(false);
            Debug.LogWarning($"[ArbeitBookPageUI] '{currentNpc.part_timer_name}'의 portraitSprite가 null입니다.");
        }

        // 능력치 슬롯 업데이트
        UpdateAbilitySlots(servingSlotContainer, currentNpc.serving_ability, servingColor);
        UpdateAbilitySlots(cookingSlotContainer, currentNpc.cooking_ability, cookingColor);
        UpdateAbilitySlots(cleaningSlotContainer, currentNpc.cleaning_ability, cleaningColor);
    }

    /// <summary>
    /// 능력치 슬롯 업데이트
    /// </summary>
    private void UpdateAbilitySlots(Transform container, int abilityValue, Color slotColor)
    {
        if (container == null || abilitySlotPrefab == null)
        {
            Debug.LogWarning("[ArbeitBookPageUI] Container 또는 Prefab이 null입니다.");
            return;
        }

        // 기존 슬롯 초기화
        foreach (Transform child in container)
        {
            Destroy(child.gameObject);
        }

        // 능력치 값 제한 (0~5)
        int displayAbility = Mathf.Clamp(abilityValue, 0, 5);
        // 최대 5개 슬롯 생성
        for (int i = 0; i < 5; i++)
        {
            GameObject slot = Instantiate(abilitySlotPrefab, container);

            // 슬롯 크기 설정 (패널을 벗어나지 않도록 크기 축소)
            RectTransform slotRect = slot.GetComponent<RectTransform>();
            if (slotRect != null)
            {
                slotRect.sizeDelta = new Vector2(10, 10);
            }

            Image slotImage = slot.GetComponent<Image>();
            if (slotImage != null)
            {
                if (i < displayAbility)
                {
                    // 슬롯 채우기
                    slotImage.enabled = true;
                    slotImage.color = slotColor;
                }
                else
                {
                    // 빈 슬롯
                    // TODO : 필요 시 색깔 수정 예정
                    slotImage.enabled = true;
                    slotImage.color = new Color(0.5f, 0.5f, 0.5f, 0.3f);
                }
            }
        }

        // Layout Group 강제 재계산
        // Layout Group 에 맞게 slot을 재배치시키기 위해 필요!!
        UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(container.GetComponent<RectTransform>());
    }

    /// <summary>
    /// 배치 버튼 클릭 시 호출
    /// </summary>
    private void OnDeployButtonClicked()
    {
        if (parentPageUI != null && currentNpc != null)
        {
            parentPageUI.OnBookPageDeployClicked(currentNpc);
        }
    }
}
