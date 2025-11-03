using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

public class EditScrollUI : BaseScrollUI<BuildingData, EditBuildingButtonUI>
{
    private DataManager dataManager;
    [SerializeField] private IslandManager islandManager;
    [SerializeField] private float duration = 0.5f;// UI 팝업, 종료 애니메이션 지속 시간
    protected override void Awake()
    {
        base.Awake();
    }

    private void Start()
    {
        dataManager = DataManager.instance;

        GenerateItems(dataManager.BuildingDatas);
    }

    protected override void OnItemClicked(IScrollItemUI clickedItem)
    {
        BuildingData data = clickedItem.GetData<BuildingData>();

        // 그 후 추가 구현 예정
    }
    protected override void OnOpenButtonClicked()
    {
        base.OnOpenButtonClicked();
        StartCoroutine(OpenSlideCoroutine());
    }

    protected override void OnCloseButtonClicked()
    {
        StartCoroutine(CloseSlideCoroutine());
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
