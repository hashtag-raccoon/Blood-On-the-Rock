using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

public class EditScrollUI : BaseScrollUI<ConstructedBuilding, EditBuildingButtonUI>
{
    private DataManager dataManager;
    [SerializeField] private IslandManager islandManager;
    [SerializeField] private DragDropController dragDropController;
    [SerializeField] private float duration = 0.5f;// UI 팝업, 종료 애니메이션 지속 시간
    protected override void Awake()
    {
        base.Awake();
    }

    private void Start()
    {
        dataManager = DataManager.Instance;
        StartCoroutine(WaitForDataAndInitialize());
    }

    private IEnumerator WaitForDataAndInitialize()
    {
        // DataManager의 ConstructedBuildings가 BuildingRepository에 의해 채워질 때까지 대기
        yield return new WaitUntil(() => dataManager != null && dataManager.ConstructedBuildings != null && dataManager.ConstructedBuildings.Count > 0);

        Debug.Log($"EditScrollUI: {dataManager.ConstructedBuildings.Count}개의 건물로 UI 생성 시작");
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
        dragDropController.onEdit = true;
    }

    protected override void OnCloseButtonClicked()
    {
        StartCoroutine(CloseSlideCoroutine());
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

        base.OnCloseButtonClicked();
    }
}
