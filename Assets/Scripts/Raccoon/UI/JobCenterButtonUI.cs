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
    [SerializeField] private Color personalityIncreaseColor = Color.green; // 성격으로 인한 증가일 때의 색상
    [SerializeField] private Color personalityDecreaseColor = new Color(0.5f, 0f, 0.5f); // 성격으로 인한 감소일 때의 색상
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
        OfferButton.interactable = !currentTempData.is_hired;

        ArbeitImage.sprite = currentTempData.Portrait;
        ArbeitNameText.text = "이름 : " + currentTempData.part_timer_name;
        ArbeitPersonalityText.text = "성격 : " + currentTempData.personality_name;
        ArbeitMoneyText.text = "월급 : " + currentTempData.estimated_daily_wage.ToString() + "G";

        // 능력치별 슬롯 업데이트 (공통 프리팹 사용, 타입별 색상 지정)
        UpdateAbilitySlots(ServingSlotContainer, abilitySlotPrefab, currentTempData.base_serving_ability, currentTempData.FinalServingAbility, servingColor);
        UpdateAbilitySlots(CookingSlotContainer, abilitySlotPrefab, currentTempData.base_cooking_ability, currentTempData.FinalCookingAbility, cookingColor);
        UpdateAbilitySlots(CleaningSlotContainer, abilitySlotPrefab, currentTempData.base_cleaning_ability, currentTempData.FinalCleaningAbility, cleaningColor);
    }

    /// <summary>
    /// 능력치 슬롯 업데이트 (Grid Layout 방식)
    /// 성격 능력치 변화에 따른 색상:
    /// 최종 - 기본 >= 3일 경우 personalityIncreaseColor 로 색깔 지정
    /// 최종 - 기본 < 0 일 경우 personalityDecreaseColor 로 색깔 지정
    /// 최종 < 0은 감소로 인해 능력치 이하가 0으로 된 경우로, 무조건 1칸이고 색깔은 personalityDecreaseColor로 고정, 최종능력치 1로 고정
    /// </summary>
    private void UpdateAbilitySlots(Transform container, GameObject slotPrefab, int baseAbility, int finalAbility, Color baseColor)
    {
        if (container == null || slotPrefab == null)
        {
            Debug.LogWarning($"UpdateAbilitySlots - container 또는 slotPrefab이 null! Container: {container}, Prefab: {slotPrefab}");
            return;
        }

        // 기존 슬롯 제거(초기화)
        foreach (Transform child in container)
        {
            Destroy(child.gameObject);
        }

        // 성격 능력치 차이 계산 (최종 - 기본)
        int personalityDiff = finalAbility - baseAbility;

        // 감소로 인해 능력치가 0 이하가 되었을 경우, 최소 1로 고정
        bool isDecreasedToOne = false;
        if (finalAbility < 1)
        {
            finalAbility = 1;
            isDecreasedToOne = true; // 감소로 인해 1칸이 됨
        }

        // 표시할 능력치 계산 (최소 1칸)
        int displayAbility = Mathf.Max(finalAbility, 1);

        // 성격으로 인한 색상 결정
        Color personalityColor;

        // 감소로 인해 1칸이 된 경우: 무조건 감소 색상
        if (isDecreasedToOne)
        {
            personalityColor = personalityDecreaseColor;
        }
        // 능력치가 성격으로 인해 올라갔을 경우 (차이 > 0)
        else if (personalityDiff > 0)
        {
            personalityColor = personalityIncreaseColor; // 초록색
        }
        // 능력치가 성격으로 인해 내려갔을 경우 (차이 < 0)
        else if (personalityDiff < 0)
        {
            personalityColor = personalityDecreaseColor; // 보라색
        }
        else
        {
            // 변화 없음 (차이 = 0): 기본 타입 색상
            personalityColor = baseColor;
        }

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

                    // 감소로 인해 1칸이 된 경우: 무조건 감소 색상
                    if (isDecreasedToOne)
                    {
                        slotImage.color = personalityDecreaseColor;
                    }
                    // 기본 능력치를 초과하는 슬롯: 성격 색상 적용 (증가 또는 감소)
                    else if (i >= baseAbility)
                    {
                        slotImage.color = personalityColor;
                    }
                    // 기본 능력치 범위 내 슬롯은 타입별 기본 색상로
                    else
                    {
                        // 단, 성격으로 인해 감소한 경우 기본 능력치 범위도 감소 색상으로 표시
                        if (personalityDiff < 0)
                        {
                            slotImage.color = personalityDecreaseColor;
                        }
                        else
                        {
                            slotImage.color = baseColor; // (Serving=빨강, Cooking=노랑, Cleaning=파랑 으로 색깔이 나눠짐)
                        }
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
            Debug.LogWarning($"OfferUI가 null입니다!");
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
            newNpc.employment_state = true; // 고용 상태로 설정

            // ArbeitRepository에서 해당 임시 알바생 데이터 제거
            ArbeitRepository.Instance.tempCandidateList.Remove(currentTempData);

            // UI 갱신 => 갱신하면서 고용 버튼 비활성화
            UpdateUI();
        }
        else
        {
            Debug.LogWarning($"고용 실패 - Data: {currentTempData != null}, Hired: {currentTempData?.is_hired}");
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
