using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ResourceBuildingUIManager : MonoBehaviour
{
    public static ResourceBuildingUIManager Instance { get; private set; }
    
    [Header("UI 패널")]
    [SerializeField] private GameObject resourceBuildingUIPanel;
    
    [Header("건물 정보")]
    [SerializeField] private TextMeshProUGUI buildingNameText;
    [SerializeField] private TextMeshProUGUI buildingLevelText;
    [SerializeField] private Button upgradeButton; 
    
    [Header("왼쪽 - 생산 슬롯 패널")]
    [SerializeField] private Transform creationListParent; 
    [SerializeField] private GameObject productionSlotPrefab;
    
    [Header("왼쪽 패널 레이아웃 설정")]
    [SerializeField] private float slotSpacing = 10f;
    
    [Header("오른쪽 - 제작 목록 스크롤")]
    [SerializeField] private Transform productionListContent; 
    [SerializeField] private GameObject productionItemPrefab;
    [SerializeField] private ScrollRect productionScrollRect;
    [SerializeField] private float listItemWidth = 200f;
    [SerializeField] private float listItemHeight = 80f;
    [SerializeField] private float listSpacing = 10f;
    [SerializeField] private int listPaddingLeft = 10;
    [SerializeField] private int listPaddingRight = 10;
    [SerializeField] private int listPaddingTop = 10;
    [SerializeField] private int listPaddingBottom = 10;
    
    private ResourceBuildingController currentBuilding;
    private BuildingData currentBuildingData;
    private int currentBuildingLevel;
    
    private List<ProductionSlot> productionSlots = new List<ProductionSlot>();
    private List<ProductionCreateButton> productionCreateButtons = new List<ProductionCreateButton>();
    private LayoutGroup listLayoutGroup;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        SetupSlotLayout();
        SetupListLayout();
    }
    
    private void Update()
    {
        // UI가 열려있을 때만 생산 슬롯 업데이트
        if (resourceBuildingUIPanel != null && resourceBuildingUIPanel.activeSelf && currentBuilding != null)
        {
            UpdateProductionSlotsDisplay();
        }
    }
    
    private void InitializeProductionSlots(int maxSlots)
    {
        foreach (Transform child in creationListParent)
        {
            Destroy(child.gameObject);
        }
        productionSlots.Clear();
        
        for (int i = 0; i < maxSlots; i++)
        {
            GameObject slotObj = Instantiate(productionSlotPrefab, creationListParent);
            ProductionSlot slot = slotObj.GetComponent<ProductionSlot>();
            
            if (slot == null)
            {
                slot = slotObj.AddComponent<ProductionSlot>();
            }
            
            slot.Initialize(i);
            productionSlots.Add(slot);
        }
    }
    
    public void OpenBuildingUI(ResourceBuildingController building, BuildingData buildingData, int level)
    {
        currentBuilding = building;
        currentBuildingData = buildingData;
        currentBuildingLevel = level;
        
        UpdateBuildingInfo();
        
        // 슬롯 초기화
        InitializeProductionSlots(building.GetMaxProductionSlots());
        
        // 현재 생산 중인 항목 복원
        RefreshProductionSlots(building);
        
        GenerateProductionList();
        
        resourceBuildingUIPanel?.SetActive(true);
    }
    
    public void CloseBuildingUI()
    {
        // 생산은 취소하지 않고 UI만 닫음
        ClearProductionList();
    
        currentBuilding = null;
        currentBuildingData = null;
        currentBuildingLevel = 0;
        
        resourceBuildingUIPanel?.SetActive(false);
    }
    
    private void UpdateBuildingInfo()
    {
        if (currentBuildingData == null) return;
        
        if (buildingNameText != null)
            buildingNameText.text = currentBuildingData.BuildingName;
        
        if (buildingLevelText != null)
            buildingLevelText.text = $"Lv.{currentBuildingLevel}";
    }
    
    private void GenerateProductionList()
    {
        ClearProductionList();
        
        if (currentBuildingData == null) return;
        
        List<BuildingProductionData> productionDatas = 
            DataManager.Instance.GetBuildingProductionDataByType(currentBuildingData.BuildingName);
        
        foreach (var productionData in productionDatas)
        {
            CreateProductionItem(productionData);
        }
    }
    
    private void CreateProductionItem(BuildingProductionData productionData)
    {
        GameObject itemObj = Instantiate(productionItemPrefab, productionListContent);
        ProductionCreateButton createButton = itemObj.GetComponent<ProductionCreateButton>();
        
        if (createButton == null)
        {
            createButton = itemObj.AddComponent<ProductionCreateButton>();
        }
        
        goodsData resourceData = DataManager.Instance.GetResourceById(productionData.resource_id);
        
        createButton.Initialize(productionData, resourceData, currentBuildingLevel);
        
        productionCreateButtons.Add(createButton);
    }
    
    private void ClearProductionList()
    {
        foreach (var button in productionCreateButtons)
        {
            if (button != null && button.gameObject != null)
            {
                Destroy(button.gameObject);
            }
        }
        productionCreateButtons.Clear();
    }

    public void OnProductionCreateButtonClicked(BuildingProductionData productionData, goodsData resourceData)
    {        
        currentBuilding.StartProduction(productionData, resourceData);
    }
    
    public void RefreshProductionSlots(ResourceBuildingController building)
    {
        if (building == null) return;
        
        // 각 슬롯에 건물 정보 설정
        foreach (var slot in productionSlots)
        {
            slot.SetBuilding(building);
        }
        
        UpdateProductionSlotsDisplay();
    }
    
    private void UpdateProductionSlotsDisplay()
    {
        foreach (var slot in productionSlots)
        {
            slot.UpdateSlotDisplay();
        }
    }

    private void SetupSlotLayout()
    {
        if (creationListParent == null) return;
        
        VerticalLayoutGroup verticalLayout = creationListParent.GetComponent<VerticalLayoutGroup>();
        if (verticalLayout == null)
        {
            verticalLayout = creationListParent.gameObject.AddComponent<VerticalLayoutGroup>();
        }
        
        verticalLayout.spacing = slotSpacing;
        verticalLayout.childAlignment = TextAnchor.UpperCenter;
        verticalLayout.childControlWidth = false;
        verticalLayout.childControlHeight = false;
        verticalLayout.childForceExpandWidth = false;
        verticalLayout.childForceExpandHeight = false;
        verticalLayout.padding = new RectOffset(0, 0, 0, 0);
        
        ContentSizeFitter existingSizeFitter = creationListParent.GetComponent<ContentSizeFitter>();
        if (existingSizeFitter != null)
        {
            DestroyImmediate(existingSizeFitter);
        }
    }
    
    private void SetupListLayout()
    {
        if (productionListContent == null) return;
        
        if (productionItemPrefab != null)
        {
            RectTransform prefabRect = productionItemPrefab.GetComponent<RectTransform>();
            if (prefabRect != null)
            {
                listItemWidth = prefabRect.sizeDelta.x;
                listItemHeight = prefabRect.sizeDelta.y;
            }
        }
        
        if (productionScrollRect != null)
        {
            productionScrollRect.horizontal = false;
            productionScrollRect.vertical = true;
            productionScrollRect.movementType = ScrollRect.MovementType.Elastic;
        }
        
        VerticalLayoutGroup verticalLayout = productionListContent.GetComponent<VerticalLayoutGroup>();
        if (verticalLayout == null)
        {
            verticalLayout = productionListContent.gameObject.AddComponent<VerticalLayoutGroup>();
        }
        
        verticalLayout.padding = new RectOffset(listPaddingLeft, listPaddingRight, listPaddingTop, listPaddingBottom);
        verticalLayout.spacing = listSpacing;
        verticalLayout.childAlignment = TextAnchor.UpperCenter;
        verticalLayout.childControlWidth = true;
        verticalLayout.childControlHeight = false;
        verticalLayout.childForceExpandWidth = true;
        verticalLayout.childForceExpandHeight = false;
        
        listLayoutGroup = verticalLayout;
        
        ContentSizeFitter listSizeFitter = productionListContent.GetComponent<ContentSizeFitter>();
        if (listSizeFitter == null)
        {
            listSizeFitter = productionListContent.gameObject.AddComponent<ContentSizeFitter>();
        }
        
        listSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        listSizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
    }
}