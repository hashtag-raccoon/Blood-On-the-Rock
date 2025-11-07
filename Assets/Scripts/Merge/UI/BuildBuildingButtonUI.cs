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
    [SerializeField] private Image PriceMoneyIconImage;
    [SerializeField] private TextMeshProUGUI BuildingPriceWoodText;
    [SerializeField] private Image PriceWoodIconImage;
    [SerializeField] private Button BuyButton;

    private object BuildingData;

    public void SetData<T>(T data, Action<IScrollItemUI> onClickCallback) where T : IScrollItemData
    {
        BuildingData = data;
        var buildingData = data as BuildingData;
        BuildingiconImage.sprite = buildingData.icon;
        BuildingNameText.text = buildingData.BuildingName;
        BuildingAmountText.text = buildingData.amount.ToString();
        
        BuildingPriceMoneyText.text = buildingData.construction_cost_gold.ToString();
        //PriceMoneyIconImage.sprite = GetGoodsData().icon;
        BuildingPriceWoodText.text = buildingData.construction_cost_wood.ToString();
        PriceWoodIconImage.sprite = buildingData.priceType.icon;

        BuyButton.onClick.RemoveAllListeners();
        BuyButton.onClick.AddListener(() => onClickCallback?.Invoke(this));
    }


    public T GetData<T>() where T : IScrollItemData
    {
        return (T)BuildingData;
    }
}
