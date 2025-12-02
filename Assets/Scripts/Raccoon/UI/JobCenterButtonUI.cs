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

    [Header("능력치 칸 (Grid Layout 방식)")]
    [SerializeField] private Transform ServingSlotContainer;
    [SerializeField] private Transform CookingSlotContainer;
    [SerializeField] private Transform CleaningSlotContainer;
    [SerializeField] private Color servingColor = Color.red; // 서빙 능력치 색상 (빨강)
    [SerializeField] private Color cookingColor = Color.yellow; // 요리 능력치 색상 (노랑)
    [SerializeField] private Color cleaningColor = Color.blue; // 청소 능력치 색상 (파랑)
    [SerializeField] private Color bonusColor = new Color(1f, 0.5f, 0f); // 주황색, 보너스 능력치 색상

    // JobCenterScrollUI에서 전달받을 공통 슬롯 프리팹
    public GameObject abilitySlotPrefab;

    [Header("버튼")]
    [SerializeField] private Button OfferButton;

    [Header("고용 수락/거절 UI")]
    public GameObject OfferUI;

    private Button offerAcceptButton;
    public Button OfferAcceptButton
    {
        get => offerAcceptButton;
        set
        {
            offerAcceptButton = value;
            // 버튼이 할당될 때 리스너 연결
            if (offerAcceptButton != null)
            {
                offerAcceptButton.onClick.RemoveAllListeners();
                offerAcceptButton.onClick.AddListener(OnOfferAccept);
            }
        }
    }

    private Button offerCancelButton;
    public Button OfferCancelButton
    {
        get => offerCancelButton;
        set
        {
            offerCancelButton = value;
            // 버튼이 할당될 때 리스너 연결
            if (offerCancelButton != null)
            {
                offerCancelButton.onClick.RemoveAllListeners();
                offerCancelButton.onClick.AddListener(OnOfferCancel);
            }
        }
    }

    private TempNpcData currentTempData;
    private Action<IScrollItemUI> clickCallback;

    /// <summary>
    /// 외부(JobCenterScrollUI)에서 공통 슬롯 프리팹 설정
    /// </summary>
    public void SetAbilitySlotPrefab(GameObject slotPrefab)
    {
        abilitySlotPrefab = slotPrefab;
    }

    private void Awake()
    {
        // OfferButton 리스너 설정 (SerializeField로 할당되므로 Awake에서 처리)
        if (OfferButton != null)
        {
            OfferButton.onClick.AddListener(OnOfferButtonClicked);
        }
        // OfferAcceptButton과 OfferCancelButton은 property setter에서 처리
    }

    public void SetData<T>(T data, Action<IScrollItemUI> onClickCallback) where T : IScrollItemData
    {
        currentTempData = data as TempNpcData;
        clickCallback = onClickCallback;

        if (currentTempData == null)
        {
            Debug.LogError("JobCenterButtonUI: TempNpcData가 null");
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

        // 능력치별 슬롯 업데이트 (공통 프리팹 사용, 타입별 색상 지정)
        UpdateAbilitySlots(ServingSlotContainer, abilitySlotPrefab, currentTempData.base_serving_ability, currentTempData.FinalServingAbility, servingColor);
        UpdateAbilitySlots(CookingSlotContainer, abilitySlotPrefab, currentTempData.base_cooking_ability, currentTempData.FinalCookingAbility, cookingColor);
        UpdateAbilitySlots(CleaningSlotContainer, abilitySlotPrefab, currentTempData.base_cleaning_ability, currentTempData.FinalCleaningAbility, cleaningColor);
    }

    /// <summary>
    /// 능력치 슬롯 업데이트 (Grid Layout 방식)
    /// </summary>
    private void UpdateAbilitySlots(Transform container, GameObject slotPrefab, int baseAbility, int finalAbility, Color baseColor)
    {
        if (container == null || slotPrefab == null)
        {
            Debug.LogWarning($"[JobCenterButtonUI] UpdateAbilitySlots - container 또는 slotPrefab이 null! Container: {container}, Prefab: {slotPrefab}");
            return;
        }

        // 기존 슬롯 제거(초기화)
        foreach (Transform child in container)
        {
            Destroy(child.gameObject);
        }

        // 최소 표시 능력치 계산 (baseAbility와 finalAbility 중 큰 값, 최소 1)
        int displayAbility = Mathf.Max(baseAbility, finalAbility, 1);

        // 최대 5개 슬롯 생성
        for (int i = 0; i < 5; i++)
        {
            GameObject slot = Instantiate(slotPrefab, container);

            // 슬롯 크기 설정 (프리팹의 SizeDelta가 0이므로 명시적으로 설정)
            RectTransform slotRect = slot.GetComponent<RectTransform>();
            if (slotRect != null)
            {
                slotRect.sizeDelta = new Vector2(30, 30); // 슬롯 크기 30x30
            }

            Image slotImage = slot.GetComponent<Image>();

            if (slotImage != null)
            {
                // i가 표시 능력치 미만일때 (최소 1칸은 표시)
                if (i < displayAbility)
                {
                    // 채워진 슬롯
                    slotImage.enabled = true;

                    // 기본 능력치를 초과하거나 3 이상이면 주황색 (보너스), 아니면 타입별 기본 색상
                    if (i >= baseAbility || i >= 3)
                    {
                        slotImage.color = bonusColor; // 보너스 슬롯은 주황색
                    }
                    else
                    {
                        slotImage.color = baseColor; // 기본 능력치 슬롯은 타입별 색상 (Serving=빨강, Cooking=노랑, Cleaning=파랑)
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
        else
        {
            Debug.LogWarning($"[JobCenterButtonUI] OfferUI가 null입니다!");
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

            // ArbeitRepository에서 해당 임시 알바생 데이터 제거
            ArbeitRepository.Instance.tempCandidateList.Remove(currentTempData);

            // UI 갱신 (JobCenterScrollUI에서 처리)
            // TODO: 해당 버튼은 회색으로 변함과 동시에 구인되었다는 표시로 변환
        }
        else
        {
            Debug.LogWarning($"[JobCenterButtonUI] 고용 실패 - Data: {currentTempData != null}, Hired: {currentTempData?.is_hired}");
        }

        // Offer 팝업 닫기
        OnOfferCancel();
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
