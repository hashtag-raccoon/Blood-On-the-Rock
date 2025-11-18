using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;

public class BuildBuildingButtonUI : MonoBehaviour, IScrollItemUI
{
    [Header("UI 설정")]
    [SerializeField] private Image BuildingiconImage;
    [SerializeField] private TextMeshProUGUI BuildingNameText;
    [SerializeField] private TextMeshProUGUI BuildingAmountText;
    [SerializeField] private TextMeshProUGUI BuildingPriceMoneyText;
    //[SerializeField] private Image PriceMoneyIconImage;
    [SerializeField] private TextMeshProUGUI BuildingPriceWoodText;
    //[SerializeField] private Image PriceWoodIconImage;
    [SerializeField] private Button BuyButton;

    private object buildingData;
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
        buildingData = data;

        var building = data as BuildingData;

        BuildingNameText.text = building.Building_Name;

        //BuildingAmountText.text = building.amount.ToString();

        BuildingPriceMoneyText.text = building.construction_cost_gold.ToString();

        BuildingPriceWoodText.text = building.construction_cost_wood.ToString();

        BuildingiconImage.sprite = building.icon;

        BuyButton.onClick.AddListener(() =>
        {
            BuyBuilding();
        });
    }

    public T GetData<T>() where T : IScrollItemData
    {
        return (T)buildingData;
    }

    /// <summary>
    /// 건물 구매 처리:
    /// 1. 자원 확인 및 차감
    /// 2. BuildScrollUI 비활성화 (애니메이션 포함)
    /// 3. 편집 모드(배치 모드) 진입
    /// </summary>
    public void BuyBuilding()
    {
        var building = buildingData as BuildingData;

        if (building == null || building.building_sprite == null)
        {
            Debug.LogError("건물 데이터가 올바르지 않습니다.");
            return;
        }


        // 자원 확인 및 차감
        if (MoneyData.current_amount < building.construction_cost_gold || 
            WoodData.current_amount < building.construction_cost_wood)
        {
            return;
        }

        MoneyData.current_amount -= building.construction_cost_gold;
        WoodData.current_amount -= building.construction_cost_wood;

        // BuildScrollUI 찾아서 비활성화 (애니메이션 포함)
        BuildScrollUI buildScrollUI = FindObjectOfType<BuildScrollUI>();
        if (buildScrollUI != null)
        {
            buildScrollUI.CloseUI();
        }

        // 편집 모드(배치 모드) 진입 - BuildingData만 전달
        DragDropController dragDropController = FindObjectOfType<DragDropController>();
        if (dragDropController != null)
        {
            dragDropController.StartNewBuildingPlacement(building);
        }
    }

    // 향후 구현: 건설된 건물 데이터 저장
    // private void SaveNewConstructedBuilding(BuildingData building)
    // {
    //     var newProduction = new ConstructedBuildingProduction
    //     {
    //         building_id = building.building_id,
    //         last_production_time = System.DateTime.Now,
    //         next_production_time = System.DateTime.Now,
    //         is_producing = false
    //     };
    //     
    //     dataManager.ConstructedBuildingProductions.Add(newProduction);
    //     // JSON 저장 호출
    // }
}
