using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

public class EditScrollUI : BaseScrollUI<ConstructedBuilding, EditBuildingButtonUI>
{
    private DataManager dataManager;
    [Header("IslandManager, DragDropController 할당/연결")]
    [SerializeField] private IslandManager islandManager;
    [SerializeField] private DragDropController dragDropController;
    [Header("UI 애니메이션 설정")]
    [SerializeField] private float duration = 0.5f;// UI 팝업, 종료 애니메이션 지속 시간
    [SerializeField] private GameObject IsEditModeUI; // 슬라이드 애니메이션 대상 UI
    private Vector2 IsEdit_targetPosition;
    private Vector2 IsEdit_closedPosition;
    private Coroutine openIsEditModeCoroutine;
    private Coroutine closeIsEditModeCoroutine;
    protected override void Awake()
    {
        base.Awake();
    }

    private void Start()
    {
        dataManager = DataManager.Instance;
        StartCoroutine(WaitForDataAndInitialize());
        IsEdit_targetPosition = IsEditModeUI.GetComponent<RectTransform>().anchoredPosition;
        IsEdit_closedPosition = new Vector2(-Screen.width, IsEdit_targetPosition.y);
    }

    /// <summary>
    /// 데이터 매니저의 편집 모드 인벤토리 건물 데이터가 로드될 때까지 대기한 후, UI를 초기화
    /// </summary>
    private IEnumerator WaitForDataAndInitialize()
    {
        // DataManager의 EditMode_InventoryBuildings가 초기화될 때까지 대기
        yield return new WaitUntil(() => dataManager != null && dataManager.EditMode_InventoryBuildings != null);

        Debug.Log($"EditScrollUI: {dataManager.EditMode_InventoryBuildings.Count}개의 건물이 편집 인벤토리에 들어감");
        GenerateItems(dataManager.EditMode_InventoryBuildings);
    }

    // 아이템 클릭 시 인벤토리에서 건물을 꺼내 배치 시작
    protected override void OnItemClicked(IScrollItemUI clickedItem)
    {
        ConstructedBuilding data = clickedItem.GetData<ConstructedBuilding>();

        // 인벤토리에서 건물을 꺼내 배치 시작
        if (data != null && dragDropController != null)
        {
            StartBuildingPlacementFromInventory(data); // 인벤토리에서 건물을 꺼내 건물 배치 시작
        }
    }

    /// <summary>
    /// 인벤토리 UI를 갱신합니다.
    /// </summary>
    public void RefreshInventoryUI()
    {
        if (dataManager != null && dataManager.EditMode_InventoryBuildings != null)
        {
            GenerateItems(dataManager.EditMode_InventoryBuildings);
            Debug.Log($"EditScrollUI 갱신: {dataManager.EditMode_InventoryBuildings.Count}개의 인벤토리 건물");
        }
    }

    /// <summary>
    /// 인벤토리에서 건물을 꺼내 배치를 시작합니다.
    /// </summary>
    private void StartBuildingPlacementFromInventory(ConstructedBuilding building)
    {
        // BuildingData 찾기
        BuildingData buildingData = dataManager.BuildingDatas.Find(b => b.building_id == building.Id);
        if (buildingData == null)
        {
            Debug.LogError($"BuildingData를 찾을 수 없음 ID: {building.Id}");
            return;
        }

        // TempBuilding 배치 시작
        dragDropController.StartInventoryBuildingPlacement(buildingData, building.Id);
    }
    // 인벤토리 UI 열기 버튼 클릭 시 호출
    protected override void OnOpenButtonClicked()
    {
        base.OnOpenButtonClicked();
        StartCoroutine(OpenSlideCoroutine());
        if (openIsEditModeCoroutine == null)
        {
            openIsEditModeCoroutine = StartCoroutine(OpenIsEditModeUI());
        }
        dragDropController.onEdit = true;
    }
    // 인벤토리 UI 닫기 버튼 클릭 시 호출
    protected override void OnCloseButtonClicked()
    {
        StartCoroutine(CloseSlideCoroutine());
        base.OnCloseButtonClicked();
        if (closeIsEditModeCoroutine == null)
        {
            closeIsEditModeCoroutine = StartCoroutine(CloseIsEditModeUI());
        }
        dragDropController.onEdit = false;
    }

    // Animations
    private IEnumerator OpenSlideCoroutine()
    {
        float elapsedTime = 0f;
        Vector2 closedPosition = new Vector2(-scrollUI.GetComponent<RectTransform>().rect.width, scrollUI.GetComponent<RectTransform>().rect.height / 2);
        Vector2 openPosition = new Vector2(0, scrollUI.GetComponent<RectTransform>().rect.height / 2);

        scrollUI.GetComponent<RectTransform>().anchoredPosition = closedPosition;
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / duration);
            scrollUI.GetComponent<RectTransform>().anchoredPosition = Vector2.Lerp(closedPosition, openPosition, t);
            yield return null;
        }
        scrollUI.GetComponent<RectTransform>().anchoredPosition = openPosition;
    }

    private IEnumerator CloseSlideCoroutine()
    {
        float elapsedTime = 0f;
        Vector2 closedPosition = new Vector2(-scrollUI.GetComponent<RectTransform>().rect.width, scrollUI.GetComponent<RectTransform>().rect.height / 2);
        Vector2 openPosition = new Vector2(0, scrollUI.GetComponent<RectTransform>().rect.height / 2);
        scrollUI.GetComponent<RectTransform>().anchoredPosition = openPosition;
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / duration);
            scrollUI.GetComponent<RectTransform>().anchoredPosition = Vector2.Lerp(openPosition, closedPosition, t);
            yield return null;
        }
        scrollUI.GetComponent<RectTransform>().anchoredPosition = closedPosition;
    }

    public IEnumerator OpenIsEditModeUI()
    {
        // 이미 실행 중인 닫기 코루틴이 있으면 중지
        if (closeIsEditModeCoroutine != null)
        {
            StopCoroutine(closeIsEditModeCoroutine);
            closeIsEditModeCoroutine = null;
        }
        
        // 이미 열려있으면 실행하지 않음
        if (IsEditModeUI.activeSelf)
        {
            yield break;
        }
        
        IsEditModeUI.SetActive(true);
        
        RectTransform rectTransform = IsEditModeUI.GetComponent<RectTransform>();
        float elapsedTime = 0f;
        
        rectTransform.anchoredPosition = IsEdit_closedPosition;
        
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / duration);
            rectTransform.anchoredPosition = Vector2.Lerp(IsEdit_closedPosition, IsEdit_targetPosition, t);
            yield return null;
        }
        
        rectTransform.anchoredPosition = IsEdit_targetPosition;
        openIsEditModeCoroutine = null;
    }
    
    public IEnumerator CloseIsEditModeUI()
    {
        // 이미 실행 중인 열기 코루틴이 있으면 중지
        if (openIsEditModeCoroutine != null)
        {
            StopCoroutine(openIsEditModeCoroutine);
            openIsEditModeCoroutine = null;
        }
        
        // 이미 닫혀있으면 실행하지 않음
        if (!IsEditModeUI.activeSelf)
        {
            yield break;
        }
        
        RectTransform rectTransform = IsEditModeUI.GetComponent<RectTransform>();
        float elapsedTime = 0f;
        
        rectTransform.anchoredPosition = IsEdit_targetPosition;
        
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / duration);
            rectTransform.anchoredPosition = Vector2.Lerp(IsEdit_targetPosition, IsEdit_closedPosition, t);
            yield return null;
        }
        
        rectTransform.anchoredPosition = IsEdit_closedPosition;
        IsEditModeUI.SetActive(false);
        closeIsEditModeCoroutine = null;
    }

    public void ToggleScrollUI()
    {
        if (!dragDropController.onEdit)
        {
            OnCloseButtonClicked();
        }
        else
        {
            OnOpenButtonClicked();
        }
    }
}
