using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.VisualScripting;

public class GenerativeBuildingButtonUI : MonoBehaviour
{
    [Header("UI 할당")]
    [SerializeField] protected TextMeshProUGUI nameText;
    [SerializeField] protected TextMeshProUGUI amountText;
    [SerializeField] protected TextMeshProUGUI timeText;
    [SerializeField] protected Image resourceIcon;

    protected BuildingProductionData productionData;
    protected GenerativeBuildingScrollUI parentScrollUI;
    protected goodsData GeneratedResourceData;

    protected virtual void Awake()
    {
        this.GetComponent<Button>().onClick.AddListener(OnButtonClick);
    }

    public virtual void Initialize(BuildingProductionData data, GenerativeBuildingScrollUI parent)
    {
        productionData = data;
        parentScrollUI = parent;
        GeneratedResourceData = DataManager.instance.GetResourceById(productionData.resource_id);
        UpdateUI();
    }

    protected virtual void UpdateUI()
    {
        if (nameText != null)
            nameText.text = GeneratedResourceData.goodsName;
            
        if (amountText != null)
            amountText.text = $"+{productionData.output_amount}";
            
        if (timeText != null)
            timeText.text = $"{productionData.base_production_time_minutes}min";
            
        if (resourceIcon != null)
            resourceIcon.sprite = GeneratedResourceData.icon;
    }

    protected virtual void OnButtonClick()
    {
        if (parentScrollUI != null)
        {
            parentScrollUI.OnProductionSelected(productionData);
        }
    }
}
