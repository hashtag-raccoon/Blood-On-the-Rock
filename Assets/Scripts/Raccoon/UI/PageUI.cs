using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 알바 도감/편집 페이지 UI 관리 클래스
/// 도감 모드와 편집 모드를 전환하며 NPC(알바생) 배치와 정보 조회를 담당함
/// </summary>
public class PageUI : MonoBehaviour
{
    [Header("페이지 오브젝트")]
    public GameObject pageUIObject;

    [Header("Animator 설정")]
    [SerializeField] private Animator pageAnimator; // 페이지 애니메이션 컨트롤러
    [SerializeField] private string nextTriggerName = "Next"; // Next Trigger 파라미터 이름
    [SerializeField] private string prevTriggerName = "Prev"; // Prev Trigger 파라미터 이름
    [SerializeField] private string pageIdleStateName = "PageIdle"; // Page 애니메이션 Idle 상태 이름
    [SerializeField] private string pageOpenStateName = "PageOpen"; // Page 애니메이션 PageOpen 상태 이름
    [SerializeField] private string pageCloseStateName = "PageClose"; // Page 애니메이션 PageClose 상태 이름

    [Header("페이지 전환 버튼")]
    [SerializeField] private Button nextButton;
    [SerializeField] private Button prevButton;

    [Header("페이지 Transform")]
    [SerializeField] private Transform leftPageTransform; // 왼쪽 페이지 위치 지정
    [SerializeField] private Transform rightPageTransform; // 오른쪽 페이지 위치 지정

    [Header("UI 애니메이션 효과용")]
    [SerializeField] private CanvasGroup leftPageCanvasGroup;   // 왼쪽 페이지 내용물 그룹
    [SerializeField] private CanvasGroup rightPageCanvasGroup;  // 오른쪽 페이지 내용물 그룹
    [SerializeField] private AnimationCurve turnCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f); // 스케일/알파 전환 곡선

    [Header("도감 모드 페이지")]
    [SerializeField] private Button bookModeButton;
    [SerializeField] private GameObject bookPagePrefab; // 도감 페이지 프리팹 (ArbeitBookPageUI)

    [Header("편집 모드 페이지")]
    [SerializeField] private Button editModeButton;
    [SerializeField] private GameObject editPanelPrefab; // 편집 패널 프리팹 (ArbeitEditPanelUI)
    [SerializeField] private int maxEditPanels = 4; // 최대 편집 패널 수, 필요 시 조정 가능

    [Header("디버깅 옵션")]
    [SerializeField] private bool showPanelGizmos = true; // 편집 패널 위치 기즈모 표시
    [SerializeField] private float gizmoRadius = 50f; // 기즈모 크기 (UI 픽셀 단위)

    // 왼쪽, 오른쪽 페이지에 생성되는 UI 오브젝트들
    private List<ArbeitBookPageUI> bookPages = new List<ArbeitBookPageUI>();
    private List<ArbeitEditPanelUI> editPanels = new List<ArbeitEditPanelUI>();

    public bool isBook = false; // 도감 모드 여부
    private int currentPageIndex = 0; // 현재 페이지 인덱스 (도감 모드일 때 2개씩 조회)
    private npc currentEditingNPC; // 현재 배치 대상인 알바생
    private int currentEditSlotIndex = -1; // 현재 편집 중인 슬롯 인덱스 (-1이면 새 슬롯)
    private bool isAnimating = false; // 애니메이션 중 여부, 애니메이션 중일때는 행동 불가

    private DataManager dataManager;
    private ArbeitManager arbeitManager;

    // Animator 파라미터 해시 (최적화용)
    private int nextTriggerHash;
    private int prevTriggerHash;

    private void Awake()
    {
        dataManager = DataManager.Instance;
        arbeitManager = ArbeitManager.Instance;

        // Animator 파라미터 해시 캐싱 (최적화용)
        nextTriggerHash = Animator.StringToHash(nextTriggerName);
        prevTriggerHash = Animator.StringToHash(prevTriggerName);
    }

    private void Start()
    {
        // Animator 초기화
        ValidateAnimator();

        // 버튼 리스너 등록
        if (nextButton != null)
            nextButton.onClick.AddListener(OnNextButtonClicked);
        if (prevButton != null)
            prevButton.onClick.AddListener(OnPrevButtonClicked);
        if (bookModeButton != null)
            bookModeButton.onClick.AddListener(OnBookModeButtonClicked);
        if (editModeButton != null)
            editModeButton.onClick.AddListener(OnEditModeButtonClicked);

        if (pageUIObject.activeSelf == true)
        {
            pageUIObject.SetActive(false);
        }
    }

    /// <summary>
    /// Animator 설정 초기화
    /// </summary>
    private void ValidateAnimator()
    {
        if (pageAnimator == null)
        {
            // pageUIObject에서 Animator 컴포넌트 찾기
            if (pageUIObject != null)
            {
                pageAnimator = pageUIObject.GetComponent<Animator>();
            }

            if (pageAnimator == null)
            {
                Debug.LogWarning("[PageUI] pageAnimator가 할당되지 않았습니다. Inspector에서 Animator를 할당해주세요.");
                return;
            }
        }

        if (pageAnimator.runtimeAnimatorController == null)
        {
            Debug.LogWarning("[PageUI] Animator에 RuntimeAnimatorController가 없습니다.");
            return;
        }
    }

    private void Update()
    {
        // ESC 키로 PageUI 비활성화
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ClosePageUI();
            return;
        }

        // 키보드 입력 처리 (도감 모드일 때만), 키보드 입력으로 페이지 전환 가능
        if (isBook && !isAnimating)
        {
            if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                OnNextButtonClicked();
            }
            else if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                OnPrevButtonClicked();
            }
        }
    }

    #region PaeUI 오픈/닫기
    /// <summary>
    /// PageUI 시작할때 도감모드로 시작하는 메소드
    /// </summary>
    public void OpenPageUI()
    {
        // 코루틴 시작 전에 먼저 게임 오브젝트 활성화
        if (pageUIObject.activeSelf == false)
        {
            pageUIObject.SetActive(true);
        }
        if (gameObject.activeSelf == false)
        {
            gameObject.SetActive(true);
        }

        // Animator 활성화
        if (pageAnimator != null)
        {
            pageAnimator.enabled = true;
        }

        SwitchToBookMode(); // 도감 모드로 시작
    }

    /// <summary>
    /// PageUI 종료 메소드
    /// </summary>
    public void ClosePageUI()
    {
        if (pageUIObject.activeSelf == true)
        {
            pageUIObject.SetActive(false);
        }
        if (gameObject.activeSelf == true)
        {
            gameObject.SetActive(false);
        }
    }
    #endregion

    #region 도감/배치 모드 전환
    /// <summary>
    /// 도감 모드로 전환
    /// </summary>
    public void SwitchToBookMode()
    {
        isBook = true;
        currentPageIndex = 0;

        // 기존 배치 패널 제거
        ClearEditPanels();

        // 버튼 활성화
        if (nextButton != null)
            nextButton.gameObject.SetActive(true);
        if (prevButton != null)
            prevButton.gameObject.SetActive(true);

        // 페이지 로드
        LoadBookPages();
    }

    /// <summary>
    /// 배치 모드로 전환
    /// </summary>
    public void SwitchToEditMode()
    {
        isBook = false;

        // 기존 도감 페이지 제거
        ClearBookPages();

        // 버튼 비활성화
        if (nextButton != null)
            nextButton.gameObject.SetActive(false);
        if (prevButton != null)
            prevButton.gameObject.SetActive(false);

        // 편집 패널 로드
        LoadEditPanels();
    }

    /// <summary>
    /// 도감 모드 버튼 클릭
    /// </summary>
    private void OnBookModeButtonClicked()
    {
        if (isBook) return; // 이미 도감 모드면 무시

        // 도감 모드로 전환
        SwitchToBookMode();
    }

    /// <summary>
    /// 편집 모드 버튼 클릭
    /// </summary>
    private void OnEditModeButtonClicked()
    {
        if (!isBook) return; // 이미 편집 모드면 무시

        // 편집 모드로 전환
        SwitchToEditMode();
    }
    #endregion

    #region 도감 모드 페이지 처리
    /// <summary>
    /// 도감 페이지 로드 (현재 인덱스 기준 2페이지씩)
    /// </summary>
    private void LoadBookPages()
    {
        if (dataManager == null || dataManager.npcs == null)
        {
            Debug.LogWarning("[PageUI] DataManager 또는 npcs 리스트가 null임");
            return;
        }

        if (leftPageTransform == null || rightPageTransform == null || bookPagePrefab == null)
        {
            Debug.LogWarning("[PageUI] leftPageTransform, rightPageTransform 또는 bookPagePrefab이 null임");
            return;
        }

        EnsurePageContentGroups();

        // 기존 페이지 제거
        ClearBookPages();

        List<npc> npcList = dataManager.GetAllHiredNpc(); // 전체 고용된 NPC 리스트를 불러옴

        // 왼쪽 페이지 (currentPageIndex)
        if (currentPageIndex < npcList.Count)
        {
            GameObject leftPage = Instantiate(bookPagePrefab, leftPageTransform);
            RectTransform leftRect = leftPage.GetComponent<RectTransform>();
            if (leftRect != null)
            {
                leftRect.localPosition = Vector3.zero;
                leftRect.localScale = Vector3.one;
                leftRect.localRotation = Quaternion.identity;
            }

            ArbeitBookPageUI leftPageUI = leftPage.GetComponent<ArbeitBookPageUI>();
            if (leftPageUI != null)
            {
                leftPageUI.SetNpcData(npcList[currentPageIndex], this);
                bookPages.Add(leftPageUI);
            }
        }

        // 오른쪽 페이지 (currentPageIndex + 1)
        if (currentPageIndex + 1 < npcList.Count)
        {
            GameObject rightPage = Instantiate(bookPagePrefab, rightPageTransform);
            RectTransform rightRect = rightPage.GetComponent<RectTransform>();
            if (rightRect != null)
            {
                rightRect.localPosition = Vector3.zero;
                rightRect.localScale = Vector3.one;
                rightRect.localRotation = Quaternion.identity;
            }

            ArbeitBookPageUI rightPageUI = rightPage.GetComponent<ArbeitBookPageUI>();
            if (rightPageUI != null)
            {
                rightPageUI.SetNpcData(npcList[currentPageIndex + 1], this);
                bookPages.Add(rightPageUI);
            }
        }

        // 버튼 상태 업데이트
        UpdateNavigationButtons();
    }

    /// <summary>
    /// 도감 페이지 제거
    /// </summary>
    private void ClearBookPages()
    {
        foreach (var page in bookPages)
        {
            if (page != null)
                Destroy(page.gameObject);
        }
        bookPages.Clear();
    }

    /// <summary>
    /// 화살표 버튼 활성화/비활성화 처리
    /// </summary>
    private void UpdateNavigationButtons()
    {
        if (dataManager == null || dataManager.npcs == null) return;

        int npcCount = dataManager.npcs.Count;

        // Prev 버튼: 첫 페이지가 아니면 활성화
        if (prevButton != null)
            prevButton.interactable = currentPageIndex > 0;

        // Next 버튼: 다음 페이지가 있으면 활성화
        if (nextButton != null)
            nextButton.interactable = currentPageIndex + 2 < npcCount;
    }

    /// <summary>
    /// 다음 페이지로 이동 (Next Trigger 호출해서 PageOpen -> PageIdle 애니메이션 진행됨)
    /// </summary>
    private void OnNextButtonClicked()
    {
        if (isAnimating)
        {
            Debug.LogWarning("[PageUI] 애니메이션 진행 중이라 무시됨");
            return;
        }

        StartCoroutine(PlayPageAnimation(true));
    }

    /// <summary>
    /// 이전 페이지로 이동 (Prev Trigger 호출해서 PageClose -> PageIdle 애니메이션 진행됨)
    /// </summary>
    private void OnPrevButtonClicked()
    {
        if (isAnimating)
        {
            Debug.LogWarning("[PageUI] 애니메이션 진행 중이라 무시됨");
            return;
        }

        StartCoroutine(PlayPageAnimation(false));
    }
    #endregion

    #region 애니메이션

    /// <summary>
    /// 페이지 내용물 컨테이너에 CanvasGroup을 보장하고 필드에 재할당
    /// (계층에서 CanvasGroup이 빠져 있으면 동적으로 추가)
    /// </summary>
    private void EnsurePageContentGroups()
    {
        leftPageCanvasGroup = GetOrAddCanvasGroup(leftPageTransform, leftPageCanvasGroup);
        rightPageCanvasGroup = GetOrAddCanvasGroup(rightPageTransform, rightPageCanvasGroup);
    }

    private CanvasGroup GetOrAddCanvasGroup(Transform target, CanvasGroup cached)
    {
        if (target == null) return cached;

        CanvasGroup cg = cached;
        if (cg == null || cg.transform != target)
        {
            cg = target.GetComponent<CanvasGroup>();
            if (cg == null)
            {
                cg = target.gameObject.AddComponent<CanvasGroup>();
            }
        }
        return cg;
    }

    /// <summary>
    /// Animator Trigger를 사용한 페이지 전환 애니메이션
    /// 배경 스프라이트는 그대로 두고, 내용물 컨테이너(left/right)를 접었다 펴며 교체
    /// </summary>
    private IEnumerator PlayPageAnimation(bool isNext)
    {
        isAnimating = true;

        if (pageAnimator == null)
        {
            isAnimating = false;
            yield break;
        }

        EnsurePageContentGroups();

        int triggerHash = isNext ? nextTriggerHash : prevTriggerHash;
        string targetStateName = isNext ? pageOpenStateName : pageCloseStateName;

        // 1. 애니메이션 트리거 발동
        pageAnimator.SetTrigger(triggerHash);
        yield return null; // 트리거 적용 대기

        // 2. 애니메이션 상태 진입 대기
        yield return StartCoroutine(WaitForAnimatorState(targetStateName));

        // 3. 전반부: 넘어가는 페이지 접기 (Scale X 1 -> 0, Alpha 1 -> 0)
        Transform foldingPage = isNext ? rightPageTransform : leftPageTransform;
        CanvasGroup foldingGroup = isNext ? rightPageCanvasGroup : leftPageCanvasGroup;
        yield return StartCoroutine(AnimatePageFold(foldingPage, foldingGroup, 1f, 0f, 0.5f));

        // 4. 중간지점: 데이터 교체 (UI가 접힌 상태라 전환이 가려짐)
        ChangePageData(isNext);

        // 5. 펼칠 페이지 초기화 (Scale X 0, Alpha 0)
        Transform unfoldingPage = isNext ? leftPageTransform : rightPageTransform;
        CanvasGroup unfoldingGroup = isNext ? leftPageCanvasGroup : rightPageCanvasGroup;
        if (unfoldingPage != null) unfoldingPage.localScale = new Vector3(0f, 1f, 1f);
        if (unfoldingGroup != null) unfoldingGroup.alpha = 0f;

        // 6. 후반부: 도착 페이지 펴기 (Scale X 0 -> 1, Alpha 0 -> 1)
        yield return StartCoroutine(AnimatePageFold(unfoldingPage, unfoldingGroup, 0f, 1f, 0.95f));

        // 7. 상태 보정
        ResetPageTransform(leftPageTransform, leftPageCanvasGroup);
        ResetPageTransform(rightPageTransform, rightPageCanvasGroup);

        // 8. Idle 애니메이션 상태 복귀 대기
        yield return StartCoroutine(WaitForAnimatorState(pageIdleStateName));

        isAnimating = false;
    }

    /// <summary>
    /// 페이지 인덱스 변경 및 UI 리로드
    /// </summary>
    private void ChangePageData(bool isNext)
    {
        if (dataManager == null || dataManager.npcs == null) return;

        int npcCount = dataManager.npcs.Count;

        if (isNext)
        {
            if (currentPageIndex + 2 < npcCount)
            {
                currentPageIndex += 2;
            }
        }
        else
        {
            if (currentPageIndex > 0)
            {
                currentPageIndex -= 2;
            }
        }

        LoadBookPages();
    }

    /// <summary>
    /// 특정 Animator 상태에 진입할 때까지 대기하는 코루틴
    /// </summary>
    private IEnumerator WaitForAnimatorState(string stateName)
    {
        if (pageAnimator == null) yield break;

        float timeout = 3f; // 타임아웃 (무한 루프 방지)
        float elapsedTime = 0f;

        // 상태에 진입할 때까지 대기
        while (!IsInAnimatorState(stateName) && elapsedTime < timeout)
        {
            elapsedTime += Time.unscaledDeltaTime;
            yield return null;
        }

        if (elapsedTime >= timeout)
        {
            Debug.LogWarning($"[PageUI] WaitForAnimatorState 타임아웃: {stateName}");
        }
    }

    /// <summary>
    /// 현재 애니메이션이 완료될 때까지 대기하는 코루틴
    /// </summary>
    private IEnumerator WaitForCurrentAnimationComplete()
    {
        if (pageAnimator == null) yield break;

        float timeout = 5f; // 타임아웃 (무한 루프 방지)
        float elapsedTime = 0f;

        // 애니메이션이 재생 중인 동안 대기
        while (elapsedTime < timeout)
        {
            AnimatorStateInfo stateInfo = pageAnimator.GetCurrentAnimatorStateInfo(0); // 0은 기본 레이어 인덱스(책 안 펴진 상태)

            // 애니메이션이 완료되었거나, 이미 다음 상태로 전환된 경우
            if (stateInfo.normalizedTime >= 0.95f || IsInAnimatorState(pageIdleStateName))
            {
                break;
            }

            elapsedTime += Time.unscaledDeltaTime;
            yield return null;
        }

        if (elapsedTime >= timeout) // 타임아웃 처리, 해당 시간 지나면 경고 로그 출력
        {
            Debug.LogWarning("[PageUI] WaitForCurrentAnimationComplete 타임아웃");
        }
    }

    /// <summary>
    /// 현재 애니메이션이 지정된 비율까지 진행될 때까지 대기
    /// </summary>
    private IEnumerator WaitForAnimationRatio(float targetRatio)
    {
        if (pageAnimator == null) yield break;

        float timeout = 2f;
        float elapsedTime = 0f;

        while (elapsedTime < timeout)
        {
            AnimatorStateInfo stateInfo = pageAnimator.GetCurrentAnimatorStateInfo(0);

            if (stateInfo.normalizedTime >= targetRatio)
            {
                break;
            }

            elapsedTime += Time.unscaledDeltaTime;
            yield return null;
        }

        if (elapsedTime >= timeout)
        {
            Debug.LogWarning($"[PageUI] WaitForAnimationRatio 타임아웃: target {targetRatio}");
        }
    }

    /// <summary>
    /// 페이지 UI를 접거나 펴는 연출 (Scale X와 Alpha 동시 조절)
    /// targetNormalizedTime: 애니메이션 클립 진행 비율(0~1)에서 언제까지 진행할지
    /// </summary>
    private IEnumerator AnimatePageFold(Transform targetTrans, CanvasGroup targetGroup, float startVal, float endVal, float targetNormalizedTime)
    {
        if (targetTrans == null || pageAnimator == null) yield break;

        float sectionStartTime = targetNormalizedTime <= 0.5f ? 0f : 0.5f;
        float sectionDuration = targetNormalizedTime - sectionStartTime;
        if (sectionDuration <= 0f) sectionDuration = 0.01f;

        while (true)
        {
            AnimatorStateInfo stateInfo = pageAnimator.GetCurrentAnimatorStateInfo(0);
            float currentAnimTime = stateInfo.normalizedTime;

            if (currentAnimTime >= targetNormalizedTime)
                break;

            float t = (currentAnimTime - sectionStartTime) / sectionDuration;
            t = Mathf.Clamp01(t);

            float curvedT = turnCurve != null ? turnCurve.Evaluate(t) : t;
            float currentVal = Mathf.Lerp(startVal, endVal, curvedT);

            targetTrans.localScale = new Vector3(currentVal, 1f, 1f);
            if (targetGroup != null)
                targetGroup.alpha = currentVal;

            yield return null;
        }

        targetTrans.localScale = new Vector3(endVal, 1f, 1f);
        if (targetGroup != null)
            targetGroup.alpha = endVal;
    }

    /// <summary>
    /// 페이지 Transform과 CanvasGroup 상태를 기본값으로 복구
    /// </summary>
    private void ResetPageTransform(Transform t, CanvasGroup g)
    {
        if (t != null) t.localScale = Vector3.one;
        if (g != null) g.alpha = 1f;
    }

    /// <summary>
    /// 특정 Animator 상태에 있는지 확인
    /// </summary>
    private bool IsInAnimatorState(string stateName)
    {
        if (pageAnimator == null) return false;

        AnimatorStateInfo stateInfo = pageAnimator.GetCurrentAnimatorStateInfo(0);
        return stateInfo.IsName(stateName);
    }
    #endregion

    #region 배치 버튼 클릭 시 처리
    /// <summary>
    /// 도감에서 배치 버튼 클릭 시 호출
    /// </summary>
    public void OnBookPageDeployClicked(npc selectedNpc)
    {
        if (selectedNpc == null) return;

        currentEditingNPC = selectedNpc;
        SwitchToEditMode();
    }
    #endregion

    #region 배치 모드 패널 처리
    /// <summary>
    /// 배치 모드 패널들 로드
    /// </summary>
    private void LoadEditPanels()
    {
        if (leftPageTransform == null || rightPageTransform == null || editPanelPrefab == null)
        {
            Debug.LogWarning("[PageUI] leftPageTransform, rightPageTransform 또는 editPanelPrefab이 null입니다.");
            return;
        }

        EnsurePageContentGroups();

        // 기존 패널 제거
        ClearEditPanels();

        // 배치된 알바 정보 가져오기 (ArbeitManager에서 가져옴)
        List<npc> deployedNpcs = GetDeployedNpcs();

        // 최대 패널 수만큼 생성 (왼쪽 2개, 오른쪽 2개)
        for (int i = 0; i < maxEditPanels; i++)
        {
            // 왼쪽 페이지에 0, 1번 패널, 오른쪽 페이지에 2, 3번 패널
            bool isLeftSide = (i < maxEditPanels / 2);
            Transform parentTransform = isLeftSide ? leftPageTransform : rightPageTransform;

            GameObject panelObj = Instantiate(editPanelPrefab, parentTransform);

            // RectTransform 초기화해서 위치 조정
            RectTransform panelRect = panelObj.GetComponent<RectTransform>();
            if (panelRect != null)
            {
                panelRect.localPosition = Vector3.zero;
                panelRect.localScale = Vector3.one;
                panelRect.localRotation = Quaternion.identity;

                // 같은 페이지 내에서 위치 조정 (가로로 2개씩, ㅁ[왼쪽페이지트랜스폼]ㅁ, ㅁ[오른쪽페이지트랜스폼]ㅁ 형태로)
                // 왼쪽: 0(왼쪽), 1(오른쪽) / 오른쪽: 2(왼쪽), 3(오른쪽)
                int indexInPage = isLeftSide ? i : (i - maxEditPanels / 2);

                // 왼쪽 패널과 오른쪽 패널 배치
                // 해당 위치가 딱 맞아서 인스펙터에서 조정하지않고, 스크립트에서만 조정함 !!
                // TODO : 추후 필요 시 수정 예정
                float xOffset = 0f;
                if (indexInPage == 0) // 왼쪽 페이지일때
                {
                    xOffset = -120f; // 왼쪽으로 오프셋
                }
                else if (indexInPage == 1) // 오른쪽 페이지일때
                {
                    xOffset = 120f; // 오른쪽으로 오프셋
                }

                panelRect.anchoredPosition = new Vector2(xOffset, 0); // 위치 조정
            }

            ArbeitEditPanelUI panelUI = panelObj.GetComponent<ArbeitEditPanelUI>();

            if (panelUI != null)
            {
                // 최대 배치 수를 가져옴
                int maxDeployed = arbeitManager != null ? arbeitManager.maxDeployedArbeiters : maxEditPanels;

                // 교체 편집 중인 슬롯인지 확인 (기존 알바 교체)
                if (currentEditingNPC != null && currentEditSlotIndex >= 0 && i == currentEditSlotIndex)
                {
                    // 교체할 슬롯을 편집 중인 상태로 표시
                    panelUI.SetEditingNpc(currentEditingNPC, this);
                }
                // 패널에 배치된 NPC가 있는지 확인
                else if (i < deployedNpcs.Count)
                {
                    panelUI.SetDeployedNpc(deployedNpcs[i], this, i); // 슬롯 인덱스 전달
                }
                else if (i >= maxDeployed)
                {
                    // 최대 배치 수 초과 패널은 비활성화
                    panelUI.SetDisabledPanel(this);
                }
                else if (currentEditingNPC != null && i == deployedNpcs.Count) // 새 알바 추가
                {
                    // 현재 편집 중인 알바생이 있으면 첫 빈 패널에 표시
                    panelUI.SetEditingNpc(currentEditingNPC, this);
                }
                else // 배치된 알바생도 없고, 배치 편집 중인 알바생도 없으면 빈 패널로 설정
                {
                    // 빈 패널
                    panelUI.SetEmptyPanel(this);
                }

                editPanels.Add(panelUI);
            }
        }
    }

    /// <summary>
    /// 편집 패널 제거
    /// </summary>
    private void ClearEditPanels()
    {
        foreach (var panel in editPanels)
        {
            if (panel != null)
                Destroy(panel.gameObject);
        }
        editPanels.Clear();
    }

    /// <summary>
    /// 배치된 알바생 목록 가져오기
    /// - ArbeitManager의 deployedArbeiters에서 npc 데이터 추출
    /// - null 체크 포함
    /// - 반환값: 배치된 npc 리스트
    /// </summary>
    private List<npc> GetDeployedNpcs()
    {
        List<npc> deployed = new List<npc>();

        if (arbeitManager != null && arbeitManager.deployedArbeiters != null)
        {
            foreach (var arbeiterObj in arbeitManager.deployedArbeiters)
            {
                if (arbeiterObj == null) continue;

                ArbeitController controller = arbeiterObj.GetComponent<ArbeitController>();
                if (controller != null && controller.myNpcData != null)
                {
                    deployed.Add(controller.myNpcData);
                }
            }
        }

        return deployed;
    }

    /// <summary>
    /// 편집 패널에서 확인 버튼 클릭 시 호출
    /// - 배치 또는 교체 확정
    /// - currentEditingNPC가 null이면 무시
    /// - currentEditSlotIndex가 -1이면 새 배치, 0 이상이면 해당 슬롯 교체
    /// 등의 기능 포함
    /// </summary>
    public void OnEditPanelConfirmClicked(npc npcToConfirm)
    {
        if (npcToConfirm == null) return;

        // ArbeitManager를 통해 실제 배치 수행
        bool success = false;

        if (arbeitManager != null)
        {
            success = arbeitManager.DeployArbeiterFromUI(npcToConfirm, currentEditSlotIndex);
        }
        else
        {
            Debug.LogError("[PageUI] ArbeitManager가 null");
        }

        if (!success)
        {
            Debug.LogWarning($"[PageUI] '{npcToConfirm.part_timer_name}' 배치 실패");
        }

        // 상태 초기화
        currentEditingNPC = null;
        currentEditSlotIndex = -1;

        // 패널 리로드
        LoadEditPanels();
    }

    /// <summary>
    /// 편집 패널에서 취소 버튼 클릭 시 호출
    /// </summary>
    public void OnEditPanelCancelClicked()
    {
        currentEditingNPC = null;
        LoadEditPanels();
    }

    /// <summary>
    /// 편집 패널에서 배치 버튼 클릭 시 호출 (빈 패널)
    /// </summary>
    public void OnEditPanelDeployClicked()
    {
        OnEditPanelDeployClicked(-1);
    }

    /// <summary>
    /// 편집 패널에서 배치 버튼 클릭 시 호출 (슬롯 인덱스 지정)
    /// </summary>
    public void OnEditPanelDeployClicked(int slotIndex)
    {
        currentEditSlotIndex = slotIndex;
        SwitchToBookMode();
    }
    #endregion

    #region 디버깅 기즈모
    private void OnDrawGizmos()
    {
        if (!showPanelGizmos) return;

        // 편집 모드일 때만 표시
        if (isBook) return;

        if (leftPageTransform == null || rightPageTransform == null) return;

        // 왼쪽 페이지 2개 슬롯 표시
        DrawPanelSlotGizmo(leftPageTransform, 0, Color.cyan);
        DrawPanelSlotGizmo(leftPageTransform, 1, Color.blue);

        // 오른쪽 페이지 2개 슬롯 표시
        DrawPanelSlotGizmo(rightPageTransform, 2, Color.green);
        DrawPanelSlotGizmo(rightPageTransform, 3, Color.yellow);
    }

    private void DrawPanelSlotGizmo(Transform parent, int slotIndex, Color color)
    {
        if (parent == null) return;

        // 부모 Transform의 세계 좌표 가져오기
        Vector3 worldPos = parent.position;

        // 기즈모 색상 설정
        Gizmos.color = color;

        // 와이어 구체로 슬롯 위치 표시
        Gizmos.DrawWireSphere(worldPos, gizmoRadius);

        // 슬롯 번호 표시
#if UNITY_EDITOR
        UnityEditor.Handles.color = color;
        UnityEditor.Handles.Label(worldPos + Vector3.up * (gizmoRadius + 20f), $"Panel Slot {slotIndex}");
#endif
    }
    #endregion
}
