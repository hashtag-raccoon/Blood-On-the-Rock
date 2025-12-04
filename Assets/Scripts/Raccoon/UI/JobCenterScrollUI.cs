using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class JobCenterScrollUI : BaseScrollUI<TempNpcData, JobCenterButtonUI>
{
    [Header("성격 부여 확률 설정")]
    public int Chance = 5; // 성격 부여 확률 (%)
    public static int PersonalityChance = 5; // 다른데서 참조될 성격 부여 확률 (%)
    private DataManager dataManager;
    private BuildingRepository buildingRepository;
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

    // 현재 선택된 JobCenterButtonUI를 추적 (공유 버튼 클릭 시 사용)
    private JobCenterButtonUI currentSelectedItem = null;

    protected override void Awake()
    {
        InitializeButtons();
        InitializeLayout();
        SetupScrollView();
        PersonalityChance = Chance; // static 변수에 값 할당

        // 공유 버튼 리스너를 Awake에서 한 번만 등록
        if (ReferenceOfferAcceptButton != null)
        {
            ReferenceOfferAcceptButton.onClick.RemoveAllListeners();
            ReferenceOfferAcceptButton.onClick.AddListener(OnSharedOfferAccept);
        }

        if (ReferenceOfferCancelButton != null)
        {
            ReferenceOfferCancelButton.onClick.RemoveAllListeners();
            ReferenceOfferCancelButton.onClick.AddListener(OnSharedOfferCancel);
        }
    }

    private void Start()
    {
        dataManager = DataManager.Instance;
        buildingRepository = BuildingRepository.Instance;

        // UI 초기 상태 비활성화
        if (scrollUI != null)
        {
            scrollUI.SetActive(false);
        }
    }

    /// <summary>
    /// UI 오픈 시 후보 리스트로 갱신
    /// </summary>
    public void RefreshCandidateList()
    {
        List<TempNpcData> availableCandidates = ArbeitManager.Instance.GetAvailableCandidates();
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
    /// base.CreateItem을 오버라이드하여 직접구현, SetData 호출 전에 프리팹을 전달하여 오류 방지함
    /// </summary>
    protected override void CreateItem(TempNpcData data)
    {
        // 1. 프리팹 인스턴스화
        GameObject itemObj = Instantiate(itemPrefab, content);
        itemObj.transform.SetSiblingIndex(0);

        // 2. 프리팹들 정렬
        LayoutElement layoutElement = itemObj.GetComponent<LayoutElement>();
        if (layoutElement == null)
        {
            layoutElement = itemObj.AddComponent<LayoutElement>();
        }
        layoutElement.preferredWidth = itemWidth;
        layoutElement.preferredHeight = itemHeight;
        layoutElement.flexibleHeight = 1f;

        // 3. JobCenterButtonUI 스크립트 가져오기
        JobCenterButtonUI buttonUIScript = itemObj.GetComponent<JobCenterButtonUI>();
        if (buttonUIScript != null)
        {
            // 4. SetData 호출 전에 레퍼런스 전달(없으면 사고남!!!!!)
            if (ReferenceOfferUI != null)
                buttonUIScript.OfferUI = ReferenceOfferUI;

            if (ReferenceOfferAcceptButton != null)
                buttonUIScript.OfferAcceptButton = ReferenceOfferAcceptButton;

            if (ReferenceOfferCancelButton != null)
                buttonUIScript.OfferCancelButton = ReferenceOfferCancelButton;
            // 공통 슬롯 프리팹 전달
            buttonUIScript.SetAbilitySlotPrefab(AbilitySlotPrefab);

            // 5. 이제 SetData 호출 (이 시점에는 abilitySlotPrefab이 이미 설정되어 오류 없을거임)
            buttonUIScript.SetData(data, OnItemClicked);

            // 6. itemUIList에 추가
            itemUIList.Add(buttonUIScript);
        }
        else
        {
            Debug.LogError($"[JobCenterScrollUI] buttonUIScript를 찾을 수 없음!");
        }
    }

    /// <summary>
    /// 버튼 UI 정렬
    /// </summary>
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

    /// <summary>
    /// JobCenterButton 클릭 시 호출될 메소드
    /// 클릭된 아이템을 currentSelectedItem에 저장
    /// </summary>
    protected override void OnItemClicked(IScrollItemUI clickedItem)
    {
        currentSelectedItem = clickedItem as JobCenterButtonUI;
        if (currentSelectedItem == null)
        {
            Debug.LogWarning("[JobCenterScrollUI] OnItemClicked - clickedItem을 JobCenterButtonUI로 캐스팅 실패");
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

    /// <summary>
    /// 공유 버튼 - 고용 수락 핸들러
    /// 현재 선택된 JobCenterButtonUI의 OnOfferAccept를 호출
    /// </summary>
    private void OnSharedOfferAccept()
    {
        if (currentSelectedItem != null)
        {
            currentSelectedItem.OnOfferAccept();
        }
        else
        {
            Debug.LogWarning("[JobCenterScrollUI] OnSharedOfferAccept - currentSelectedItem이 null");
        }
    }

    /// <summary>
    /// 공유 버튼 - 고용 취소 핸들러
    /// 현재 선택된 JobCenterButtonUI의 OnOfferCancel을 호출
    /// </summary>
    private void OnSharedOfferCancel()
    {
        if (currentSelectedItem != null)
        {
            currentSelectedItem.OnOfferCancel();
        }
        else
        {
            Debug.LogWarning("[JobCenterScrollUI] OnSharedOfferCancel - currentSelectedItem이 null");
        }
    }
}
