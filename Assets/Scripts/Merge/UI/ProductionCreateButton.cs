using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class ProductionCreateButton : MonoBehaviour
{
    [Header("UI 컴포넌트")]
    private Button createButton;
    [SerializeField] private Image goodsImageProductionList;
    [SerializeField] private TextMeshProUGUI goodsCreateTimeText;
    [SerializeField] private Image goodsConsumeImage;
    [SerializeField] private TextMeshProUGUI goodsConsumeText;
    [SerializeField] private TextMeshProUGUI goodsNameText;
    [SerializeField] private TextMeshProUGUI goodsAmountText;

    // 추후 구현
    //[SerializeField] private GameObject lockedOverlay;
    //[SerializeField] private TextMeshProUGUI lockedLevelText;

    private BuildingProductionInfo productionData;
    private ResourceData resourceData;
    private bool isUnlocked = true;

    public void Initialize(BuildingProductionInfo production, ResourceData resource, int currentBuildingLevel)
    {
        createButton = GetComponent<Button>();
        productionData = production;
        resourceData = resource;

        isUnlocked = true;

        UpdateUI(currentBuildingLevel);

        if (createButton != null)
        {
            createButton.onClick.RemoveAllListeners();
            createButton.onClick.AddListener(OnCreateButtonClicked);
        }
    }

    private void UpdateUI(int currentBuildingLevel)
    {
        if (resourceData == null || productionData == null) return;

        if (goodsImageProductionList != null)
        {
            goodsImageProductionList.sprite = resourceData.icon;
        }

        if (goodsNameText != null)
        {
            goodsNameText.text = resourceData.resource_name;
        }

        if (goodsAmountText != null)
        {
            goodsAmountText.text = $"+{productionData.output_amount}";
        }

        if (goodsCreateTimeText != null)
        {
            float totalMinutes = productionData.base_production_time_minutes;

            int minutes = Mathf.FloorToInt(totalMinutes);
            int seconds = Mathf.FloorToInt((totalMinutes - minutes) * 60f);

            goodsCreateTimeText.text = $"{minutes}분 {seconds}초";
        }

        if (goodsConsumeText != null)
        {
            goodsConsumeText.text = $"{productionData.consume_amount}";
        }

        goodsConsumeImage.sprite = ResourceRepository.Instance.GetResourceByName(productionData.consume_resource_type).icon;

        UpdateLockedUI(currentBuildingLevel);

        if (createButton != null)
        {
            createButton.interactable = isUnlocked;
        }
    }
    private void UpdateLockedUI(int currentBuildingLevel)
    {
        //if (lockedOverlay == null) return;

        //lockedOverlay.SetActive(false);
        isUnlocked = true;
    }

    private void OnCreateButtonClicked()
    {
        if (!isUnlocked)
        {
            Debug.LogWarning("[ProductionCreateButton] 버튼이 잠금 상태입니다.");
            return;
        }

        if (ResourceBuildingUIManager.Instance != null)
        {
            ResourceBuildingUIManager.Instance.OnProductionCreateButtonClicked(productionData, resourceData);
        }
    }
}