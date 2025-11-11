using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class GenerativeBuildingScrollUI : MonoBehaviour
{
    [Header("UI 설정")]
    [SerializeField] protected ScrollRect scrollRect;
    [SerializeField] protected Transform contentParent;
    [SerializeField] protected GameObject buttonPrefab;
    
    [Header("생산 정보 버튼")]
    [SerializeField] protected GameObject productionStatusButtonPrefab;
    [SerializeField] protected Transform productionStatusParent;

    protected GenerativeBuilding building;
    protected List<GenerativeBuildingButtonUI> spawnedButtons = new List<GenerativeBuildingButtonUI>();
    protected List<ProductionStatusButtonUI> productionStatusButtonUIs = new List<ProductionStatusButtonUI>();

    protected virtual void Awake()
    {
        // 생산 상태 버튼은 Initialize에서 생성
    }

    protected virtual void Update()
    {
        UpdateProductionStatusButtons();
    }

    public virtual void Initialize(GenerativeBuilding generativeBuilding)
    {
        building = generativeBuilding;
        ClearButtons();
        
        // 생산 상태 버튼 생성
        if (building != null)
        {
            SetupProductionStatusButtons(building.GetMaxProductionSlots());
        }
        
        SetupUI();
    }

    protected virtual void SetupUI()
    {
        List<BuildingProductionData> productionList = GetProductionDataList();
        
        foreach (var data in productionList)
        {
            SpawnButton(data);
        }
    }

    protected virtual void SetupProductionStatusButtons(int slotCount)
    {
        ClearProductionStatusButtons();
        
        for (int i = 0; i < slotCount; i++)
        {
            GameObject btnObj = Instantiate(productionStatusButtonPrefab, productionStatusParent);
            ProductionStatusButtonUI statusBtn = btnObj.GetComponent<ProductionStatusButtonUI>();
            
            if (statusBtn != null)
            {
                int index = i;
                
                if (statusBtn.button != null)
                {
                    statusBtn.button.onClick.AddListener(() => OnProductionStatusButtonClick(index));
                }
                
                statusBtn.ClearData();
                productionStatusButtonUIs.Add(statusBtn);
            }
        }
    }

    protected virtual void OnProductionStatusButtonClick(int index)
    {
        if (building == null || index < 0 || index >= productionStatusButtonUIs.Count)
            return;
            
        ProductionStatusButtonUI statusBtn = productionStatusButtonUIs[index];
        
        // 활성화된 슬롯을 클릭하면 생산 취소
        if (statusBtn != null && statusBtn.isActive)
        {
            building.CancelProduction(index);
            statusBtn.ClearData();
        }
    }

    protected virtual List<BuildingProductionData> GetProductionDataList()
    {
        // 추후 구현 예정
        return DataManager.instance.GetBuildingProductionDataList();
    }

    protected virtual void SpawnButton(BuildingProductionData data)
    {
        GameObject buttonObj = Instantiate(buttonPrefab, contentParent);
        GenerativeBuildingButtonUI button = buttonObj.GetComponent<GenerativeBuildingButtonUI>();
        button.Initialize(data, this);
        spawnedButtons.Add(button);
    }

    protected virtual void ClearButtons()
    {
        foreach (var button in spawnedButtons)
        {
            if (button != null)
                Destroy(button.gameObject);
        }
        spawnedButtons.Clear();
    }

    protected virtual void ClearProductionStatusButtons()
    {
        foreach (var btn in productionStatusButtonUIs)
        {
            if (btn != null)
                Destroy(btn.gameObject);
        }
        productionStatusButtonUIs.Clear();
    }

    // 제작 버튼 클릭 시
    public virtual void OnProductionSelected(BuildingProductionData productionData)
    {
        if (building == null)
            return;

        int emptySlot = building.FindEmptySlot();
        
        if (emptySlot >= 0)
        {
            bool success = building.StartProduction(productionData, emptySlot);
            
            if (success)
            {
                SetProductionStatusButton(emptySlot, productionData);
            }
            else
            {
                Debug.LogWarning("생산 시작 실패 (재화 부족)");
                // 추후 구현 예정
            }
        }
        else
        {
            Debug.LogWarning("사용 가능한 생산 슬롯이 없습니다.");
            // 추후 구현 예정
        }
    }

    protected virtual void SetProductionStatusButton(int index, BuildingProductionData productionData)
    {
        if (index < 0 || index >= productionStatusButtonUIs.Count)
            return;
            
        ProductionStatusButtonUI statusBtn = productionStatusButtonUIs[index];
        if (statusBtn != null && productionData != null)
        {
            goodsData resource = DataManager.instance.GetResourceById(productionData.resource_id);
            statusBtn.SetData(productionData, resource);
        }
    }

    // 생산 상태 버튼 업데이트
    protected virtual void UpdateProductionStatusButtons()
    {
        if (building == null)
            return;

        for (int i = 0; i < productionStatusButtonUIs.Count; i++)
        {
            ProductionStatusButtonUI statusBtn = productionStatusButtonUIs[i];
            
            if (statusBtn != null)
            {
                bool isSlotActive = building.IsSlotActive(i);
                
                if (isSlotActive)
                {
                    BuildingProductionData productionData = building.GetProductionData(i);
                    
                    if (productionData != null)
                    {
                        // 진행도와 남은 시간 업데이트
                        float progress = building.GetProductionProgress(i);
                        float remainingTime = building.GetRemainingTime(i);
                        statusBtn.UpdateProgress(progress, remainingTime);
                    }
                }
                else if (statusBtn.isActive)
                {
                    // 슬롯은 비활성이지만 UI는 활성 상태면 초기화
                    statusBtn.ClearData();
                }
            }
        }
    }

    // 생산 완료 콜백
    public virtual void OnProductionCompleted(int slotIndex)
    {
        if (slotIndex >= 0 && slotIndex < productionStatusButtonUIs.Count)
        {
            productionStatusButtonUIs[slotIndex].ClearData();
        }
    }

    public virtual void CloseUI()
    {
        gameObject.SetActive(false);
    }
}