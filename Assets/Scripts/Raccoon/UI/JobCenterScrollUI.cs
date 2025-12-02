using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class JobCenterScrollUI : BaseScrollUI<npc, JobCenterButtonUI>
{
    private DataManager dataManager;
    private BuildingRepository Buildinginstance;
    [Header("IslandManager 할당/연결")]
    [SerializeField] private IslandManager islandManager;
    [Header("UI 애니메이션 설정")]
    [SerializeField] float duration = 1f; // UI 팝업/종료 애니메이션 지속 시간
    [Header("UI 레퍼런스 설정")]
    [SerializeField] private GameObject ReferenceOfferUI;
    [SerializeField] private Button ReferenceOfferAcceptButton;
    [SerializeField] private Button ReferenceOfferCancelButton;
    [Header("능력치 슬롯 프리팹 레퍼런스")]
    [SerializeField] private GameObject AbilitySlotPrefab; // 공통 능력치 슬롯 프리팹
    // UI 애니메이션용 딕셔너리임
    // ui들 원래 위치 담을 딕셔너리, 키: ui 오브젝트, 값: 원래 위치
    private Dictionary<GameObject, Vector2> UIoriginPos = new Dictionary<GameObject, Vector2>();

    private bool isUIOpen = false; // UI 활성화 상태

    protected override void Awake() // 버튼 및 정렬상태, 스크롤 초기화
    {
        base.Awake();

    }

    private void Start()
    {
        dataManager = DataManager.Instance;
        buildingRepository = BuildingRepository.Instance;

        // 초기 임시 알바생 데이터들 생성
        GenerateInitialCandidates();

        // UI 초기 상태 비활성화
        if (scrollUI != null)
        {
            scrollUI.SetActive(false);
        }
    }

    /// <summary>
    /// 초기 후보 3명 생성 (씬 로드 시 호출)
    /// </summary>
    private void GenerateInitialCandidates()
    {
        if (ArbeitRepository.Instance.tempCandidateList.Count == 0)
        {
            List<TempNpcData> candidates = ArbeitRepository.Instance.CreateRandomTempCandidates(3);
            ArbeitRepository.Instance.tempCandidateList.AddRange(candidates);
        }
    }

    /// <summary>
    /// UI 오픈 시 알바생 리스트로 갱신
    /// </summary>
    public void RefreshCandidateList()
    {
        // 고용되지 않은 후보자들만 따로 담음
        // GenerateInitialCandidates()에서 이미 3명 생성되어 있으므로 사실상 중복 생성 방지임
        List<TempNpcData> availableCandidates = ArbeitRepository.Instance.tempCandidateList.FindAll(c => !c.is_hired);
        GenerateItems(availableCandidates);
    }

    protected override void InitializeButtons()
    {
        // JobCenter 건물을 클릭하여 호출되므로 버튼 불필요함. 그래서 null 처리하였음.
        openButton = null;
        closeButton = null;
    }

    /// <summary>
    /// JobCenterButtonUI 생성 시 레퍼런스 할당
    /// </summary>
    protected override void OnItemCreated(JobCenterButtonUI itemUI, GameObject itemObj)
    {
        base.OnItemCreated(itemUI, itemObj);

        // Inspector에서 할당받은 레퍼런스를 생성한 JobCenterButtonUI에 전달
        var buttonUIScript = itemObj.GetComponent<JobCenterButtonUI>();
        if (buttonUIScript != null)
        {
            if (ReferenceOfferUI != null)
                buttonUIScript.OfferUI = ReferenceOfferUI;

            if (ReferenceOfferAcceptButton != null)
                buttonUIScript.OfferAcceptButton = ReferenceOfferAcceptButton;

            if (ReferenceOfferCancelButton != null)
                buttonUIScript.OfferCancelButton = ReferenceOfferCancelButton;

            // 공통 슬롯 프리팹 전달
            buttonUIScript.SetAbilitySlotPrefab(AbilitySlotPrefab);
        }
        else
        {
            Debug.LogError($"[JobCenterScrollUI] buttonUIScript를 찾을 수 없음!");
        }
    }



    /// <summary>
    /// JobCenter(건물)에서 호출하여 UI 열기
    /// </summary>
    public void OpenUI()
    {
        if (isUIOpen) return;

        isUIOpen = true;
        if (scrollUI != null)
        {
            scrollUI.SetActive(true);
        }
        RefreshCandidateList();
        StartCoroutine(OpenSlideCoroutine());
    }

    /// <summary>
    /// UI 닫기 (ESC 키)
    /// </summary>
    public override void CloseUI()
    {
        if (!isUIOpen) return;

        isUIOpen = false;
        StartCoroutine(CloseSlideCoroutine());
    }

    protected override void SetupLayoutGroup()
    {
        VerticalLayoutGroup layoutGroup = content.GetComponent<VerticalLayoutGroup>();
        if (layoutGroup == null)
        {
            layoutGroup = content.gameObject.AddComponent<VerticalLayoutGroup>();
        }

        layoutGroup.spacing = spacing;
        layoutGroup.padding = padding;
        layoutGroup.childAlignment = TextAnchor.MiddleLeft;
        layoutGroup.childControlWidth = true;
        layoutGroup.childControlHeight = true;
        layoutGroup.childForceExpandWidth = false;
        layoutGroup.childForceExpandHeight = true;
    }

    protected override void OnOpenButtonClicked()
    {
        // 버튼을 사용하지 않으므로 OpenUI()로 대체
        OpenUI();
    }

    protected override void OnCloseButtonClicked()
    {
        // 버튼을 사용하지 않으므로 CloseUI()로 대체
        CloseUI();
    }

    // UI 애니메이션
    private IEnumerator OpenSlideCoroutine()
    {
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

    // UI 애니메이션
    private IEnumerator CloseSlideCoroutine()
    {
        foreach (var ui in islandManager.leftUI)
        {
            StartCoroutine(SlideUICoroutine(ui, true, false));
        }

        foreach (var ui in islandManager.rightUI)
        {
            StartCoroutine(SlideUICoroutine(ui, false, false));
        }

        yield return new WaitForSeconds(duration);

        // 애니메이션 완료 후 UI 비활성화
        if (scrollUI != null)
        {
            scrollUI.SetActive(false);
        }
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
