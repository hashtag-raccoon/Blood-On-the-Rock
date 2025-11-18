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

    private IEnumerator WaitForDataAndInitialize()
    {
        // DataManager의 ConstructedBuildings가 BuildingRepository에 의해 채워질 때까지 대기
        yield return new WaitUntil(() => dataManager != null && dataManager.ConstructedBuildings != null && dataManager.ConstructedBuildings.Count > 0);

        Debug.Log($"EditScrollUI: {dataManager.ConstructedBuildings.Count}개의 건물로 UI 생성 시작"); // 당분간은 놔둘 것
        GenerateItems(dataManager.ConstructedBuildings);
    }

    protected override void OnItemClicked(IScrollItemUI clickedItem)
    {
        ConstructedBuilding data = clickedItem.GetData<ConstructedBuilding>();

        // 추후 구현 예정  
    }
    protected override void OnOpenButtonClicked()
    {
        base.OnOpenButtonClicked();
        StartCoroutine(OpenSlideCoroutine());
        StartCoroutine(OpenIsEditModeUI());
        dragDropController.onEdit = true;
    }

    protected override void OnCloseButtonClicked()
    {
        StartCoroutine(CloseSlideCoroutine());
        base.OnCloseButtonClicked();
        StartCoroutine(CloseIsEditModeUI());
        dragDropController.onEdit = false;
    }

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
    }
    
    public IEnumerator CloseIsEditModeUI()
    {
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
    }
}
