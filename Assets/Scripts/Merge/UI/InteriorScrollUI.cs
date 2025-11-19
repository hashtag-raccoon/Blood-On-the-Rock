using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 인테리어 배치 스크롤 UI
/// BuildScrollUI와 유사한 구조로 인테리어 목록을 표시하는 스크롤 UI
/// </summary>
public class InteriorScrollUI : BaseScrollUI<InteriorData, BuildInteriorButtonUI>
{
    private DataManager dataManager;
    [Header("IslandManager 할당/연결")]
    [SerializeField] private IslandManager islandManager;
    [Header("UI 애니메이션 설정")]
    [SerializeField] float duration = 1f; // UI 팝업/종료 애니메이션 지속 시간

    // ui들 원래 위치 담을 딕셔너리, 키: ui 오브젝트, 값: 원래 위치, OpenButton 누를 시 저장 및 초기화
    private Dictionary<GameObject, Vector2> UIoriginPos = new Dictionary<GameObject, Vector2>();

    protected override void Awake()
    {
        base.Awake();
    }

    private void Start()
    {
        dataManager = DataManager.Instance;
        
        // DataManager에 InteriorDatas가 있다면 사용, 없다면 빈 리스트
        if (dataManager.InteriorDatas != null && dataManager.InteriorDatas.Count > 0)
        {
            GenerateItems(dataManager.InteriorDatas);
        }
        else
        {
            Debug.LogWarning("InteriorDatas가 DataManager에 없습니다. DataManager에 InteriorDatas 리스트를 추가하거나, 직접 InteriorData 리스트를 할당하세요.");
        }
    }

    protected override void OnItemClicked(IScrollItemUI clickedItem)
    {
        InteriorData data = clickedItem.GetData<InteriorData>();
        // 그 후 추가 구현 예정
    }

    protected override void OnOpenButtonClicked()
    {
        StartCoroutine(OpenSlideCoroutine());
        base.OnOpenButtonClicked();
        if (islandManager != null)
        {
            islandManager.BlurUI.SetActive(true);
        }
    }

    protected override void OnCloseButtonClicked()
    {
        base.OnCloseButtonClicked();
        StartCoroutine(CloseSlideCoroutine());
        if (islandManager != null)
        {
            islandManager.BlurUI.SetActive(false);
        }
    }
    
    private IEnumerator OpenSlideCoroutine()
    {
        if (islandManager == null) yield break;
        
        foreach (var ui in islandManager.leftUI)
        {
            if (!UIoriginPos.ContainsKey(ui))
            {
                // 딕셔너리에 ui 원래 위치 저장
                UIoriginPos[ui] = ui.GetComponent<RectTransform>().anchoredPosition;
            }
            StartCoroutine(SlideUICoroutine(ui, true, true));
        }

        foreach (var ui in islandManager.rightUI)
        {
            if (!UIoriginPos.ContainsKey(ui))
            {
                // 딕셔너리에 ui 원래 위치 저장
                UIoriginPos[ui] = ui.GetComponent<RectTransform>().anchoredPosition;
            }
            StartCoroutine(SlideUICoroutine(ui, false, true));
        }

        yield return new WaitForSeconds(duration);
    }

    private IEnumerator CloseSlideCoroutine()
    {
        if (islandManager == null) yield break;
        
        foreach (var ui in islandManager.leftUI)
        {
            StartCoroutine(SlideUICoroutine(ui, true, false));
        }

        foreach (var ui in islandManager.rightUI)
        {
            StartCoroutine(SlideUICoroutine(ui, false, false));
        }

        yield return new WaitForSeconds(duration);
    }
    
    // isOpen이 true면<열기 애니메이션>, false면<닫기 애니메이션>
    // <열기 애니메이션> - isLeft가 true면 왼쪽에서 화면 밖으로 슬라이드, false면 오른쪽에서 화면 밖으로 슬라이드,
    // <닫기 애니메이션> - isLeft가 true면 화면 밖에서 오른쪽으로 슬라이드, false면 화면 밖에서 왼쪽으로 슬라이드
    private IEnumerator SlideUICoroutine(GameObject ui, bool isLeft, bool isOpen)
    {
        float elapsedTime = 0f;
        RectTransform rect = ui.GetComponent<RectTransform>();

        Vector2 startPosition;
        Vector2 endPosition;

        // 원래 위치
        Vector2 originalPosition = UIoriginPos[ui];

        // 화면 밖 위치, isLeft면 왼쪽 밖, 아니면 오른쪽 밖
        Vector2 offScreenPosition = new Vector2(
            isLeft ? -Screen.width : Screen.width,
            originalPosition.y
        );

        if (isOpen)
        {
            startPosition = originalPosition;
            endPosition = offScreenPosition;
        }
        else
        {
            startPosition = offScreenPosition;
            endPosition = originalPosition;
        }

        rect.anchoredPosition = startPosition;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / duration);
            rect.anchoredPosition = Vector2.Lerp(startPosition, endPosition, t);
            yield return null;
        }

        rect.anchoredPosition = endPosition;
    }
}

