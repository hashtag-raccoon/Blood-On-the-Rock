using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;

public class EditBuildingButtonUI : MonoBehaviour, IScrollItemUI
{
    [Header("UI 설정")]
    [SerializeField] private Image BuildingiconImage;
    [SerializeField] private TextMeshProUGUI BuildingNameText;
    [SerializeField] private TextMeshProUGUI BuildingAmountText;
    [SerializeField] private Button BuildingButton;

    private object BuildingData;

    public void SetData<T>(T data, Action<IScrollItemUI> onClickCallback) where T : IScrollItemData
    {
        BuildingData = data;
        var buildingData = data as BuildingData;
        BuildingiconImage.sprite = buildingData.icon;
        BuildingNameText.text = buildingData.Building_Name;
        //BuildingAmountText.text = buildingData.production_info.output_Amount.ToString();

        BuildingButton.onClick.RemoveAllListeners();
        BuildingButton.onClick.AddListener(() => onClickCallback?.Invoke(this));
    }


    public T GetData<T>() where T : IScrollItemData
    {
        return (T)BuildingData;
    }
}
