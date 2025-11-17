using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ResourceBuildingController : BuildingBase
{
    [System.Serializable]
    public class ProductionInfo
    {
        public BuildingProductionInfo productionData;
        public goodsData resourceData;
        public float totalProductionTime;
        public float timeRemaining;
        public int slotIndex;

        public ProductionInfo(BuildingProductionInfo prodData, goodsData resData, int index)
        {
            productionData = prodData;
            resourceData = resData;
            totalProductionTime = prodData.base_production_time_minutes * 60f;
            timeRemaining = totalProductionTime;
            slotIndex = index;
        }
    }

    [Header("생산 설정")]
    [SerializeField] private int maxProductionSlots = 4;
    [Header("생산 완료 프리팹 (Canvas 포함)")]
    [SerializeField] private GameObject completeResourceUIPrefab;
    [Header("업그레이드 불가 UI 프리팹")]
    public GameObject LimitUpgradeUIObject;
    public Vector2 limitBuildingImageSize = new Vector2(100, 100);
    private static GameObject ActiveLimitUpgradeUI;

    private GameObject activeCompleteUI; // 생산완료 UI (Canvas 포함)

    private List<ProductionInfo> activeProductions = new List<ProductionInfo>();

    protected override void Start()
    {
        base.Start();
    }

    protected override IEnumerator WaitForDataAndInitialize()
    {
        // 부모 클래스의 초기화 대기
        yield return StartCoroutine(base.WaitForDataAndInitialize());

        // ResourceBuildingController 고유 초기화
        InitializeProductionSlots();
    }

    protected override void Update()
    {
        if (GetActiveProductionCount() <= 0)
        {
            if (activeCompleteUI != null)
            {
                Destroy(activeCompleteUI);
                activeCompleteUI = null;
            }
            return;
        }
        UpdateAllProductions();
    }

    protected override void OnUpgradeUI()
    {
        // 생산 중이면 제한 UI 표시
        if (this.gameObject.GetComponent<ResourceBuildingController>().GetActiveProductionCount() > 0)
        {
            BlurOnOff();
            ShowLimitUpgradeUI();
            return;
        }

        // 생산 중이 아니면 일반 업그레이드 UI 표시
        base.OnUpgradeUI();
    }

    private void ShowLimitUpgradeUI()
    {
        // 기존 UI가 있으면 먼저 제거하고, 후에 새로 생성함
        // 만약 제거를 하지 않을 경우 기존 UI가 남아있게 되어 UI가 중복 생성되는 문제가 발생함
        if (ActiveLimitUpgradeUI != null)
        {
            Destroy(ActiveLimitUpgradeUI);
            ActiveLimitUpgradeUI = null;
        }

        if (activeUpgradeUI != null)
        {
            Destroy(activeUpgradeUI);
            activeUpgradeUI = null;
        }

        ActiveLimitUpgradeUI = Instantiate(LimitUpgradeUIObject);

        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas != null)
        {
            ActiveLimitUpgradeUI.transform.SetParent(canvas.transform, false);
        }

        Transform buildingImageTransform = ActiveLimitUpgradeUI.transform.Find("BuildingImage");
        if (buildingImageTransform != null && Buildingdata != null && Buildingdata.icon != null)
        {
            UnityEngine.UI.Image img = buildingImageTransform.GetComponent<UnityEngine.UI.Image>();
            if (img != null)
            {
                img.sprite = Buildingdata.icon;

                RectTransform imgRect = buildingImageTransform.GetComponent<RectTransform>();
                if (imgRect != null)
                {
                    imgRect.sizeDelta = limitBuildingImageSize;
                }
            }
        }

        Transform buildingNameTransform = ActiveLimitUpgradeUI.transform.Find("BuildingName");
        if (buildingNameTransform != null && Buildingdata != null)
        {
            TMPro.TextMeshProUGUI nameText = buildingNameTransform.GetComponent<TMPro.TextMeshProUGUI>();
            if (nameText != null)
            {
                nameText.text = Buildingdata.Building_Name;
            }
        }

        Transform exitButtonTransform = ActiveLimitUpgradeUI.transform.Find("ExitButton");
        if (exitButtonTransform != null)
        {
            Button exitButton = exitButtonTransform.GetComponent<Button>();
            if (exitButton != null)
            {
                exitButton.onClick.AddListener(() =>
                {
                    if (ActiveLimitUpgradeUI != null)
                    {
                        BlurOnOff();
                        Destroy(ActiveLimitUpgradeUI);
                        ActiveLimitUpgradeUI = null;
                    }
                });
            }
        }
    }


    private void InitializeProductionSlots()
    {
        for (int i = 0; i < maxProductionSlots; i++)
        {
            activeProductions.Add(null);
        }
    }

    private void UpdateAllProductions()
    {
        bool hasCompletedProduction = false;

        for (int i = 0; i < activeProductions.Count; i++)
        {
            if (activeProductions[i] != null)
            {
                activeProductions[i].timeRemaining -= Time.deltaTime;

                if (activeProductions[i].timeRemaining <= 0)
                {
                    activeProductions[i].timeRemaining = 0;
                    hasCompletedProduction = true;
                }
            }
        }

        if (hasCompletedProduction && activeCompleteUI == null)
        {
            CompleteProductionUI();
        }
    }

    private void CompleteProductionUI()
    {
        if (activeCompleteUI != null || completeResourceUIPrefab == null)
        {
            return;
        }

        activeCompleteUI = Instantiate(completeResourceUIPrefab);

        Canvas canvas = activeCompleteUI.GetComponent<Canvas>();
        if (canvas != null)
        {
            canvas.renderMode = RenderMode.WorldSpace;

            Vector3 worldPos = transform.position;
            activeCompleteUI.transform.position = worldPos + new Vector3(0, 0.5f, 0);

            RectTransform canvasRect = activeCompleteUI.GetComponent<RectTransform>();
            if (canvasRect != null)
            {
                canvasRect.sizeDelta = new Vector2(200, 100);
            }
        }

        Button completeButton = activeCompleteUI.GetComponentInChildren<Button>();
        if (completeButton != null)
        {
            completeButton.onClick.AddListener(() =>
            {
                ALLCompleteProduction();

                if (activeCompleteUI != null)
                {
                    Destroy(activeCompleteUI);
                    activeCompleteUI = null;
                }
            });
        }
    }

    public bool StartProduction(BuildingProductionInfo productionData, goodsData resourceData)
    {
        int emptySlotIndex = FindEmptySlotIndex();

        if (emptySlotIndex == -1)
        {
            return false;
        }

        // 재화 소비
        goodsData consumeResource = DataManager.Instance.GetResourceByName(productionData.consume_resource_type);
        if (consumeResource.amount < productionData.consume_amount)
        {
            return false;
        }

        consumeResource.amount -= productionData.consume_amount;

        // 생산 시작
        ProductionInfo newProduction = new ProductionInfo(productionData, resourceData, emptySlotIndex);
        activeProductions[emptySlotIndex] = newProduction;

        // UI 업데이트
        if (ResourceBuildingUIManager.Instance != null)
        {
            ResourceBuildingUIManager.Instance.RefreshProductionSlots(this);
        }

        return true;
    }

    public void CancelProduction(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= activeProductions.Count)
            return;

        if (activeProductions[slotIndex] == null)
            return;

        // 재화 반환
        ProductionInfo production = activeProductions[slotIndex];
        goodsData consumeResource = DataManager.Instance.GetResourceByName(production.productionData.consume_resource_type);
        consumeResource.amount += production.productionData.consume_amount;

        activeProductions[slotIndex] = null;

        // 빈 슬롯 뒤로 보내서 정렬
        CompactProductionSlots();

        // UI 업데이트
        if (ResourceBuildingUIManager.Instance != null)
        {
            ResourceBuildingUIManager.Instance.RefreshProductionSlots(this);
        }
    }

    /*
    public void AllCancleProduction()
    {
        for(int i = 0 ; i < activeProductions.Count ; i++)
        {
            if(activeProductions[i] != null)
            {
                // 재화 반환
                ProductionInfo production = activeProductions[i];
                goodsData consumeResource = DataManager.Instance.GetResourceByName(production.productionData.consume_resource_type);
                consumeResource.amount += production.productionData.consume_amount;

                activeProductions[i] = null;
            }
        }
    }
    */

    public void CompleteProduction(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= activeProductions.Count)
            return;

        ProductionInfo production = activeProductions[slotIndex];
        if (production == null)
            return;

        // 생산물 지급
        production.resourceData.amount += production.productionData.output_amount;

        activeProductions[slotIndex] = null;

        // 슬롯 재정렬 (빈 슬롯을 뒤로 이동)
        CompactProductionSlots();

        // UI 업데이트
        if (ResourceBuildingUIManager.Instance != null)
        {
            ResourceBuildingUIManager.Instance.RefreshProductionSlots(this);
        }
    }

    private void ALLCompleteProduction()
    {
        int completedCount = 0;

        for (int i = 0; i < activeProductions.Count; i++)
        {
            // null이 아니고, 시간이 0 이하인 경우만 처리
            if (activeProductions[i] != null && activeProductions[i].timeRemaining <= 0)
            {
                ProductionInfo production = activeProductions[i];

                // 생산물 지급
                production.resourceData.amount += production.productionData.output_amount;

                // 슬롯 비우기
                activeProductions[i] = null;
                completedCount++;
            }
        }

        // 슬롯 재정렬 (빈 슬롯을 뒤로 이동)
        CompactProductionSlots();

        // UI 업데이트
        if (ResourceBuildingUIManager.Instance != null)
        {
            ResourceBuildingUIManager.Instance.RefreshProductionSlots(this);
        }
    }

    private void CompactProductionSlots()
    {
        List<ProductionInfo> compactedList = new List<ProductionInfo>();

        for (int i = 0; i < activeProductions.Count; i++)
        {
            if (activeProductions[i] != null)
            {

                activeProductions[i].slotIndex = compactedList.Count;
                compactedList.Add(activeProductions[i]);
            }
        }

        while (compactedList.Count < maxProductionSlots)
        {
            compactedList.Add(null);
        }

        activeProductions = compactedList;
    }

    // 생산 중인지 확인
    public int GetActiveProductionCount()
    {
        int count = 0;
        foreach (var production in activeProductions)
        {
            if (production != null)
                count++;
        }
        return count;
    }

    private int FindEmptySlotIndex()
    {
        for (int i = 0; i < activeProductions.Count; i++)
        {
            if (activeProductions[i] == null)
                return i;
        }
        return -1;
    }

    public List<ProductionInfo> GetActiveProductions()
    {
        return activeProductions;
    }

    public ProductionInfo GetProductionInfo(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= activeProductions.Count)
            return null;

        return activeProductions[slotIndex];
    }

    public int GetMaxProductionSlots()
    {
        return maxProductionSlots;
    }

    public override void OpenBuildingUI()
    {
        base.OpenBuildingUI();

        if (ResourceBuildingUIManager.Instance != null && Buildingdata != null)
        {
            ResourceBuildingUIManager.Instance.OpenBuildingUI(this, Buildingdata, Buildingdata.level);
        }
    }

    public override void CloseBuildingUI()
    {
        if (ResourceBuildingUIManager.Instance != null)
        {
            ResourceBuildingUIManager.Instance.CloseBuildingUI();
        }

        base.CloseBuildingUI();
    }
}