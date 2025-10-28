using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BuildingButtonUI : MonoBehaviour
{
    [SerializeField] private Image BuildingiconImage;
    [SerializeField] private TextMeshProUGUI BuildingNameText;
    [SerializeField] private TextMeshProUGUI BuildingAmountText;
    //private Image selectionBorder;
    [SerializeField] private Button BuildingButton;

    //private bool isClicked = false;

    private object BuildingData;

    private void Awake()
    {
        
    }

    public void SetData(object data, System.Action<BuildingButtonUI> onClickCallBack)
    {
        BuildingData = data;
        var buildingData = (BuildingData)data;
        BuildingiconImage.sprite = buildingData.icon;
        BuildingNameText.text = buildingData.BuildingName;
        BuildingAmountText.text = buildingData.amount.ToString();

        BuildingButton.onClick.RemoveAllListeners();
        BuildingButton.onClick.AddListener(() => onClickCallBack?.Invoke(this));
    }

    public T GetData<T>()
    {
        return (T)BuildingData;
    }
}
