using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;

/// <summary>
/// 인테리어 배치 버튼 UI
/// BuildBuildingButtonUI와 유사한 구조로 인테리어 배치를 위한 버튼
/// </summary>
public class BuildInteriorButtonUI : MonoBehaviour, IScrollItemUI
{
    [Header("UI 설정")]
    [SerializeField] private Image InteriorIconImage;
    [SerializeField] private TextMeshProUGUI InteriorNameText;
    [SerializeField] private TextMeshProUGUI InteriorAmountText;
    [SerializeField] private TextMeshProUGUI InteriorPriceMoneyText;
    [SerializeField] private TextMeshProUGUI InteriorPriceWoodText;
    [SerializeField] private Button BuyButton;

    private object interiorData;
    private DataManager dataManager;
    private ResourceData MoneyData;
    private ResourceData WoodData;

    private void Awake()
    {
        dataManager = DataManager.Instance;
        MoneyData = dataManager.GetResourceByName("Money");
        WoodData = dataManager.GetResourceByName("Wood");
    }

    public void SetData<T>(T data, Action<IScrollItemUI> onClickCallback) where T : IScrollItemData
    {
        interiorData = data;

        var interior = data as InteriorData;

        InteriorNameText.text = interior.Interior_Name;

        //InteriorAmountText.text = interior.amount.ToString(); // 필요시 구현

        InteriorPriceMoneyText.text = interior.purchase_cost_gold.ToString();

        InteriorPriceWoodText.text = interior.purchase_cost_wood.ToString();

        InteriorIconImage.sprite = interior.icon;

        BuyButton.onClick.RemoveAllListeners();
        BuyButton.onClick.AddListener(() =>
        {
            BuyInterior();
        });
    }

    public T GetData<T>() where T : IScrollItemData
    {
        return (T)interiorData;
    }

    /// <summary>
    /// 인테리어 구매 처리:
    /// 1. 자원 확인 및 차감
    /// 2. InteriorScrollUI 비활성화 (애니메이션 포함)
    /// 3. 편집 모드(배치 모드) 진입
    /// </summary>
    public void BuyInterior()
    {
        var interior = interiorData as InteriorData;

        if (interior == null || interior.interior_sprite == null)
        {
            Debug.LogError("인테리어 데이터가 올바르지 않습니다.");
            return;
        }

        // 자원 확인 및 차감
        if (MoneyData != null && WoodData != null)
        {
            if (MoneyData.current_amount < interior.purchase_cost_gold ||
                WoodData.current_amount < interior.purchase_cost_wood)
            {
                Debug.LogWarning("자원이 부족합니다.");
                return;
            }

            // 자원 차감
            MoneyData.current_amount -= interior.purchase_cost_gold;
            WoodData.current_amount -= interior.purchase_cost_wood;
        }

        // InteriorScrollUI 찾아서 비활성화 (애니메이션 포함)
        InteriorScrollUI interiorScrollUI = FindObjectOfType<InteriorScrollUI>();
        if (interiorScrollUI != null)
        {
            interiorScrollUI.CloseUI();
        }

        // 편집 모드(배치 모드) 진입 - InteriorData만 전달
        DragDropController dragDropController = FindObjectOfType<DragDropController>();
        if (dragDropController != null)
        {
            dragDropController.StartNewInteriorPlacement(interior);
        }
    }
}

