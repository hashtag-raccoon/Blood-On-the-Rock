using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;

public class JobCenterButtonUI : MonoBehaviour, IScrollItemUI
{
    [Header("UI 설정")]
    [SerializeField] private Image ArbeitImage;
    [SerializeField] private TextMeshProUGUI ArbeitNameText;
    [SerializeField] private TextMeshProUGUI ArbeitPersonalityText;
    [SerializeField] private TextMeshProUGUI ArbeitMoneyText;
    
    [Header("능력치 Grid Layout (슬롯 방식)")]
    [SerializeField] private Transform ServingSlotContainer;
    [SerializeField] private Transform CookingSlotContainer;
    [SerializeField] private Transform CleaningSlotContainer;
    [SerializeField] private GameObject ServingSlotPrefab; // Serving 슬롯 전용 프리팹
    [SerializeField] private GameObject CookingSlotPrefab; // Cooking 슬롯 전용 프리팹
    [SerializeField] private GameObject CleaningSlotPrefab; // Cleaning 슬롯 전용 프리팹
    [SerializeField] private Color normalColor = Color.white; // 기본 색상
    [SerializeField] private Color bonusColor = new Color(1f, 0.5f, 0f); // 주황색 (보너스)
    
    [Header("버튼")]
    [SerializeField] private Button OfferButton;
    
    [Header("고용 UI")]
    [SerializeField] private GameObject OfferUI;
    [SerializeField] private Button OfferAcceptButton;
    [SerializeField] private Button OfferCancelButton;

    private TempNpcData currentTempData;
    private Action<IScrollItemUI> clickCallback;

    private void Awake()
    {
        // 버튼 리스너 설정
        if (OfferButton != null)
        {
            OfferButton.onClick.AddListener(OnOfferButtonClicked);
        }
        
        if (OfferAcceptButton != null)
        {
            OfferAcceptButton.onClick.AddListener(OnOfferAccept);
        }
        
        if (OfferCancelButton != null)
        {
            OfferCancelButton.onClick.AddListener(OnOfferCancel);
        }
    }

    public void SetData<T>(T data, Action<IScrollItemUI> onClickCallback) where T : IScrollItemData
    {
        currentTempData = data as TempNpcData;
        clickCallback = onClickCallback;

        if (currentTempData == null)
        {
            Debug.LogError("JobCenterButtonUI: TempNpcData가 null입니다!");
            return;
        }

        // UI 업데이트
        UpdateUI();
    }

    private void UpdateUI()
    {
        ArbeitNameText.text = currentTempData.part_timer_name;
        ArbeitPersonalityText.text = currentTempData.personality_name;
        ArbeitMoneyText.text = currentTempData.estimated_daily_wage.ToString();

        // 능력치 슬롯 업데이트 (각 능력치별 전용 프리팹 사용)
        UpdateAbilitySlots(ServingSlotContainer, ServingSlotPrefab, currentTempData.base_serving_ability, currentTempData.FinalServingAbility);
        UpdateAbilitySlots(CookingSlotContainer, CookingSlotPrefab, currentTempData.base_cooking_ability, currentTempData.FinalCookingAbility);
        UpdateAbilitySlots(CleaningSlotContainer, CleaningSlotPrefab, currentTempData.base_cleaning_ability, currentTempData.FinalCleaningAbility);
    }

    /// <summary>
    /// 능력치 슬롯 업데이트 (Grid Layout 방식)
    /// </summary>
    private void UpdateAbilitySlots(Transform container, GameObject slotPrefab, int baseAbility, int finalAbility)
    {
        if (container == null || slotPrefab == null) return;

        // 기존 슬롯 제거
        foreach (Transform child in container)
        {
            Destroy(child.gameObject);
        }

        // 최대 5개 슬롯 생성
        for (int i = 0; i < 5; i++)
        {
            GameObject slot = Instantiate(slotPrefab, container);
            Image slotImage = slot.GetComponent<Image>();
            
            if (slotImage != null)
            {
                if (i < finalAbility)
                {
                    // 채워진 슬롯
                    slotImage.enabled = true;
                    
                    // 기본 능력치(3)를 초과하면 주황색
                    if (i >= 3 || i >= baseAbility)
                    {
                        slotImage.color = bonusColor;
                    }
                    else
                    {
                        slotImage.color = normalColor;
                    }
                }
                else
                {
                    // 빈 슬롯
                    slotImage.enabled = false;
                }
            }
        }
    }

    private void OnOfferButtonClicked()
    {
        // 고용 팝업 열기
        if (OfferUI != null)
        {
            OfferUI.SetActive(true);
        }
        
        clickCallback?.Invoke(this);
    }

    private void OnOfferAccept()
    {
        // TempNpcData -> Real NpcData 변환
        if (currentTempData != null && !currentTempData.is_hired)
        {
            npc newNpc = ArbeitRepository.Instance.ConvertTempToRealNpc(currentTempData);
            currentTempData.is_hired = true;
            
            // ArbeitRepository에서 해당 후보 제거
            ArbeitRepository.Instance.tempCandidateList.Remove(currentTempData);
            
            Debug.Log($"고용 완료: {newNpc.part_timer_name}");
            
            // UI 갱신 (JobCenterScrollUI에서 처리)
            // TODO: JobCenterScrollUI에 이벤트 전달하여 리스트 갱신
        }
        
        // 팝업 닫기
        if (OfferUI != null)
        {
            OfferUI.SetActive(false);
        }
    }

    private void OnOfferCancel()
    {
        // 팝업 닫기
        if (OfferUI != null)
        {
            OfferUI.SetActive(false);
        }
    }

    public T GetData<T>() where T : IScrollItemData
    {
        return (T)(object)currentTempData;
    }
}
