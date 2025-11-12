using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ProductionSlot : MonoBehaviour
{
    [Header("UI 컴포넌트")]
    private Button slotButton;
    [SerializeField] private Image productionImage;
    [SerializeField] private TextMeshProUGUI productionTimeText; 
    
    private int slotIndex;
    private ResourceBuildingController currentBuilding;
    
    public void Initialize(int index)
    {
        slotIndex = index;

        slotButton = GetComponent<Button>();
        productionImage = transform.Find("ProductionImage")?.GetComponent<Image>();
        productionTimeText = transform.Find("ProductionTime")?.GetComponent<TextMeshProUGUI>();

        if (slotButton != null)
        {
            slotButton.onClick.RemoveAllListeners();
            slotButton.onClick.AddListener(OnSlotClicked);
        }
        
        ResetSlot();
    }
    
    public void SetBuilding(ResourceBuildingController building)
    {
        currentBuilding = building;
    }
    
    public void UpdateSlotDisplay()
    {
        if (currentBuilding == null)
        {
            ResetSlot();
            return;
        }
        
        ResourceBuildingController.ProductionInfo productionInfo = currentBuilding.GetProductionInfo(slotIndex);
        
        if (productionInfo == null)
        {
            ResetSlot();
        }
        else
        {
            DisplayProduction(productionInfo);
        }
    }
    
    private void DisplayProduction(ResourceBuildingController.ProductionInfo productionInfo)
    {
        // 아이콘 표시
        if (productionImage != null && productionInfo.resourceData != null)
        {
            productionImage.sprite = productionInfo.resourceData.icon;
            productionImage.enabled = true;
            productionImage.color = Color.white;
        }
        
        // 남은 시간 표시
        if (productionTimeText != null)
        {
            float timeRemaining = productionInfo.timeRemaining;
            int minutes = Mathf.FloorToInt(timeRemaining / 60f);
            int seconds = Mathf.FloorToInt(timeRemaining % 60f);
            productionTimeText.text = $"{minutes:00}분 {seconds:00}초";
        }
    }
    
    private void OnSlotClicked()
    {
        if (currentBuilding != null)
        {
            ResourceBuildingController.ProductionInfo productionInfo = currentBuilding.GetProductionInfo(slotIndex);
            
            if (productionInfo != null)
            {
                if (productionInfo.timeRemaining > 0)
                {
                    // 생산 중이면 취소
                    currentBuilding.CancelProduction(slotIndex);
                }
                else
                {
                    currentBuilding.CompleteProduction(slotIndex);
                }
            }
            else
            {

            }
        }
    }
    
    public void ResetSlot()
    {
        // UI 초기화
        if (productionImage != null)
        {
            productionImage.sprite = null;
            productionImage.enabled = false;
        }
        
        if (productionTimeText != null)
        {
            productionTimeText.text = "";
        }
    }
}