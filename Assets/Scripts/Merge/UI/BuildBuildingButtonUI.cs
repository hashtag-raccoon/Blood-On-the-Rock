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

    private object BuildingData;

    public void SetData<T>(T data, Action<IScrollItemUI> onClickCallback) where T : IScrollItemData
    {
        BuildingData = data;

        var building = data as BuildingData;

        BuildingNameText.text = building.BuildingName;

        BuildingAmountText.text = building.amount.ToString();

        BuildingPriceMoneyText.text = building.construction_cost_gold.ToString();

        BuildingPriceWoodText.text = building.construction_cost_wood.ToString();

        BuildingiconImage.sprite = building.icon;

        BuyButton.onClick.AddListener(() =>
        {
            try
            {
                onClickCallback(this);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error invoking onClickCallback: {ex}");
            }
        });
    }

    public T GetData<T>() where T : IScrollItemData
    {
        return (T)BuildingData;
    }
}
