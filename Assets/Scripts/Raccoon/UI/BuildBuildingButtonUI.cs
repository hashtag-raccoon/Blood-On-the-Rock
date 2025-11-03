using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;

public class BuildBuildingButtonUI : MonoBehaviour, IScrollItemUI
{
    [Header("UI ¼³Á¤")]
    [SerializeField] private Image BuildingiconImage;
    [SerializeField] private TextMeshProUGUI BuildingNameText;
    [SerializeField] private TextMeshProUGUI BuildingAmountText;
    [SerializeField] private TextMeshProUGUI BuildingPriceText;
    [SerializeField] private Image PriceIconImage;
    [SerializeField] private Button BuyButton;

    private object BuildingData;

    public void SetData<T>(T data, Action<IScrollItemUI> onClickCallback) where T : IScrollItemData
    {
        BuildingData = data;
        var buildingData = data as BuildingData;
        BuildingiconImage.sprite = buildingData.icon;
        BuildingNameText.text = buildingData.BuildingName;
        BuildingAmountText.text = buildingData.amount.ToString();
        BuildingPriceText.text = buildingData.price.ToString();
        PriceIconImage.sprite = buildingData.priceType.icon;

        BuyButton.onClick.RemoveAllListeners();
        BuyButton.onClick.AddListener(() => onClickCallback?.Invoke(this));
    }


    public T GetData<T>() where T : IScrollItemData
    {
        return (T)BuildingData;
    }
}
