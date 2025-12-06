using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.VisualScripting;

/// <summary>
/// 칵테일 제작 UI 관리 클래스
/// P키를 누르면 열리는 UI로, 두 가지 화면을 전환하며 관리합니다:
/// 1) 주문 목록 화면: 접수된 칵테일 주문들을 보여줌
/// 2) 칵테일 제작 화면: 재료/기법/잔을 선택하여 칵테일을 만드는 화면
/// </summary>
public class CocktailMakingUI : MonoBehaviour
{
    // ========== Unity Inspector에서 할당할 UI 참조들 ==========

    [Header("Panel References")]
    [SerializeField] private GameObject orderListPanel; // 주문 목록 화면 패널
    [SerializeField] private GameObject craftingPanel;   // 칵테일 제작 화면 패널

    [Header("Order List UI")]
    [SerializeField] private Transform orderListContent; // 주문 아이템들이 들어갈 ScrollView의 Content
    [SerializeField] private GameObject orderItemPrefab; // 주문 아이템 프리팹 (칵테일 아이콘+이름 표시)
    [SerializeField] private GameObject emptyStateMessage; // 주문이 없을 때 표시할 메시지
    [SerializeField] private TextMeshProUGUI titleText; // "주문 목록 (N)" 제목 텍스트

    [Header("Crafting UI")]
    [SerializeField] private TextMeshProUGUI craftingCocktailNameText; // 제작 중인 칵테일 이름 표시
    [SerializeField] private Button backButton; // 주문 목록으로 돌아가기 버튼
    [SerializeField] private Button nextStepButton; // 다음 단계 버튼
    [SerializeField] private Button completeButton; // 제작 완료 버튼
    [SerializeField] private Image selectedGlassDisplayImage; // 선택된 잔을 표시할 이미지 (Table 위)

    [Header("Crafting Sub-UIs")]
    [SerializeField] private IngredientSelectionUI ingredientSelectionUI; // 재료 선택 UI
    [SerializeField] private TechniqueSelectionUI techniqueSelectionUI; // 기법 선택 UI (Build/Floating/Shaking)
    [SerializeField] private GlassSelectionUI glassSelectionUI; // 잔 선택 UI
    [SerializeField] private ToolSelectionUI toolSelectionUI; // 도구 선택 UI (쉐이커/바스푼)

    [Header("Toast Message UI")]
    [SerializeField] private ToastMessageUI toastMessageUI; // 토스트 메시지 UI

    [Header("Table Ingredient Display")]
    [SerializeField] private Transform tableIngredientArea; // 테이블 위 재료 표시 영역
    [SerializeField] private GameObject draggableIngredientPrefab; // 드래그 가능 재료 프리팹

    // ========== 내부 변수들 ==========

    private List<GameObject> instantiatedOrderItems = new List<GameObject>(); // 생성된 주문 아이템 UI 목록 (메모리 관리용)
    private List<GameObject> instantiatedTableIngredients = new List<GameObject>(); // 테이블 위 재료 UI 목록
    private CocktailRecipeScript currentCraftingRecipe; // 현재 제작 중인 칵테일 레시피
    private CocktailSystem cocktailSystem; // 칵테일 검증 시스템 (점수 계산)

    // 제작 단계 관리
    private enum CraftingStep
    {
        ToolSelection = 0,    // 1단계: 도구 선택
        GlassSelection = 1,   // 2단계: 잔 선택
        IngredientSelection = 2 // 3단계: 재료 선택
    }
    private CraftingStep currentStep = CraftingStep.ToolSelection;

    /// <summary>
    /// 초기화: CocktailSystem 찾기, 버튼 이벤트 등록, 서브 UI 초기화
    /// </summary>
    private void Awake()
    {
        // CocktailSystem 찾기 또는 생성
        // CocktailSystem은 사용자가 선택한 재료/기법/잔을 저장하고 검증하는 역할
        cocktailSystem = FindObjectOfType<CocktailSystem>();
        if (cocktailSystem == null)
        {
            GameObject systemObj = new GameObject("CocktailSystem");
            cocktailSystem = systemObj.AddComponent<CocktailSystem>();
        }

        // "← 목록으로" 버튼 클릭 시 주문 목록 화면으로 전환
        if (backButton != null)
        {
            backButton.onClick.AddListener(ShowOrderListView);
        }

        // "다음 단계" 버튼 클릭 시 다음 제작 단계로 이동
        if (nextStepButton != null)
        {
            nextStepButton.onClick.AddListener(OnNextStepClicked);
        }

        // "제작 완료" 버튼 클릭 시 칵테일 검증 및 완료 처리
        if (completeButton != null)
        {
            completeButton.onClick.AddListener(OnCompleteCraftingClicked);
        }

        // 서브 UI들에게 CocktailSystem 전달 (재료/기법/잔 선택 시 CocktailSystem에 저장)
        if (ingredientSelectionUI != null)
        {
            ingredientSelectionUI.Initialize(cocktailSystem);
            ingredientSelectionUI.OnIngredientSelectedEvent += OnIngredientSelectedInUI;
        }

        if (techniqueSelectionUI != null)
            techniqueSelectionUI.Initialize(cocktailSystem);

        if (glassSelectionUI != null)
        {
            glassSelectionUI.Initialize(cocktailSystem);
            glassSelectionUI.OnGlassSelectedEvent += OnGlassSelectedInUI;
        }

        if (toolSelectionUI != null)
            toolSelectionUI.Initialize(cocktailSystem, techniqueSelectionUI);

        // [FIX] 주문 목록 UI 겹침 문제 해결을 위한 레이아웃 컴포넌트 자동 설정
        if (orderListContent != null)
        {
            // HorizontalLayoutGroup 확인 및 추가 (가로 배치로 변경)
            HorizontalLayoutGroup hlg = orderListContent.GetComponent<HorizontalLayoutGroup>();
            if (hlg == null)
            {
                // 혹시 VerticalLayoutGroup이 있다면 제거 (충돌 방지)
                VerticalLayoutGroup vlg = orderListContent.GetComponent<VerticalLayoutGroup>();
                if (vlg != null) DestroyImmediate(vlg);

                hlg = orderListContent.gameObject.AddComponent<HorizontalLayoutGroup>();
                hlg.childAlignment = TextAnchor.MiddleLeft;
                hlg.childControlHeight = false; // 아이템 자체 높이 사용
                hlg.childControlWidth = false;  // 아이템 자체 너비 사용
                hlg.childForceExpandHeight = false;
                hlg.childForceExpandWidth = false;
                hlg.spacing = 20f; // 적절한 간격
            }

            // ContentSizeFitter 확인 및 추가
            ContentSizeFitter csf = orderListContent.GetComponent<ContentSizeFitter>();
            if (csf == null)
            {
                csf = orderListContent.gameObject.AddComponent<ContentSizeFitter>();
                // 가로 방향으로 늘어나도록 설정
                csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
                csf.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
            }

            // [FIX] 세로 스크롤 비활성화 (가로 스크롤만 허용)
            ScrollRect scrollRect = orderListContent.GetComponentInParent<ScrollRect>();
            if (scrollRect != null)
            {
                scrollRect.vertical = false;
                scrollRect.horizontal = true; // 가로 스크롤은 확실하게 켜둠
            }
        }
    }

    /// <summary>
    /// 게임 시작 시 UI 비활성화 (P키를 눌러야 열림)
    /// </summary>
    private void Start()
    {
        if (this.gameObject.activeSelf)
        {
            this.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// P키를 누르면 호출됨 (PlayerableController에서 호출)
    /// UI를 활성화하고 주문 목록 화면을 표시
    /// </summary>
    public void OpenCocktailMakingUI()
    {
        this.gameObject.SetActive(true);
        ShowOrderListView(); // 기본적으로 주문 목록 화면 표시
    }

    /// <summary>
    /// P키를 다시 누르면 호출됨 (UI 닫기)
    /// 생성된 주문 아이템들을 모두 삭제하고 UI 비활성화
    /// </summary>
    public void CloseCocktailMakingUI()
    {
        ClearOrderList(); // 메모리 정리
        this.gameObject.SetActive(false);
    }

    /// <summary>
    /// UI가 현재 활성화되어 있는지 확인 (P키 토글용)
    /// </summary>
    public bool GestActive()
    {
        return this.gameObject.activeSelf;
    }

    // ========== 화면 전환 ==========

    /// <summary>
    /// 주문 목록 화면을 표시 (제작 화면 숨김)
    /// OrderingManager에서 주문 목록을 가져와서 UI에 표시
    /// </summary>
    private void ShowOrderListView()
    {
        // 주문 목록 패널 보이기, 제작 패널 숨기기
        if (orderListPanel != null)
            orderListPanel.SetActive(true);

        if (craftingPanel != null)
            craftingPanel.SetActive(false);

        // 주문 목록 새로고침 (OrderingManager.CocktailOrders 읽어서 UI 생성)
        RefreshOrderList();
    }

    /// <summary>
    /// 칵테일 제작 화면을 표시 (주문 목록 숨김)
    /// 사용자가 주문 아이템을 클릭하면 호출됨
    /// </summary>
    /// <param name="recipe">제작할 칵테일 레시피</param>
    private void ShowCraftingView(CocktailRecipeScript recipe)
    {
        currentCraftingRecipe = recipe; // 현재 제작 중인 레시피 저장

        // 제작 패널 보이기, 주문 목록 패널 숨기기
        if (orderListPanel != null)
            orderListPanel.gameObject.SetActive(false);

        if (craftingPanel != null)
            craftingPanel.gameObject.SetActive(true);


        // 제작 화면 초기화 (칵테일 이름 표시, 재료/기법/잔 선택 UI 초기화)
        InitializeCraftingView();
    }

    // ========== 주문 목록 화면 ==========

    /// <summary>
    /// 주문 목록 UI를 새로고침
    /// OrderingManager.CocktailOrders에서 주문 목록을 가져와서 UI 생성
    /// </summary>
    private void RefreshOrderList()
    {
        // 기존에 생성된 주문 아이템 UI들 모두 삭제
        ClearOrderList();

        // OrderingManager에서 현재 주문 목록 가져오기
        var orders = OrderingManager.Instance.CocktailOrders;

        // 주문이 없으면 "주문이 없습니다" 메시지 표시
        if (orders.Count == 0)
        {
            if (emptyStateMessage != null)
                emptyStateMessage.SetActive(true);

            if (titleText != null)
                titleText.text = "주문 목록 (0)";

            return;
        }

        // 주문이 있으면 메시지 숨기고 개수 표시
        if (emptyStateMessage != null)
            emptyStateMessage.SetActive(false);

        if (titleText != null)
            titleText.text = $"주문 목록 ({orders.Count})";

        // 각 주문마다 UI 아이템 생성
        foreach (var recipe in orders)
        {
            CreateOrderItem(recipe);
        }

        // [FIX] 레이아웃 강제 갱신 (프리팹 생성 직후 위치가 꼬이는 문제 방지)
        if (orderListContent != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(orderListContent as RectTransform);
        }
    }

    /// <summary>
    /// 주문 아이템 UI 하나 생성 (프리팹 인스턴스화)
    /// 칵테일 이름, 아이콘, 완료 체크마크를 표시하는 UI
    /// </summary>
    /// <param name="recipe">표시할 칵테일 레시피</param>
    private void CreateOrderItem(CocktailRecipeScript recipe)
    {
        if (orderItemPrefab == null || orderListContent == null) return;

        // 프리팹 인스턴스화 (ScrollView의 Content 하위에 생성)
        GameObject orderItem = Instantiate(orderItemPrefab, orderListContent);
        instantiatedOrderItems.Add(orderItem); // 나중에 삭제하기 위해 리스트에 추가

        // OrderItemUI 컴포넌트에 데이터 전달
        OrderItemUI itemUI = orderItem.GetComponent<OrderItemUI>();
        if (itemUI != null)
        {
            // 레시피 정보와 클릭 콜백 전달 (클릭하면 OnOrderItemClicked 호출)
            itemUI.SetData(recipe, OnOrderItemClicked);
        }
    }

    /// <summary>
    /// 생성된 모든 주문 아이템 UI 삭제 (메모리 정리)
    /// </summary>
    private void ClearOrderList()
    {
        foreach (GameObject item in instantiatedOrderItems)
        {
            Destroy(item);
        }
        instantiatedOrderItems.Clear();
    }

    /// <summary>
    /// 주문 아이템을 클릭했을 때 호출되는 콜백
    /// 제작 화면으로 전환하여 해당 칵테일을 만들 수 있게 함
    /// </summary>
    /// <param name="recipe">클릭한 주문의 칵테일 레시피</param>
    private void OnOrderItemClicked(CocktailRecipeScript recipe)
    {
        // 제작 화면으로 전환
        ShowCraftingView(recipe);
    }

    // ========== 칵테일 제작 화면 ==========

    /// <summary>
    /// 제작 화면 초기화
    /// - 제작할 칵테일 이름 표시
    /// - 이전에 선택한 재료/기법/잔 초기화
    /// - 1단계(도구 선택)부터 시작
    /// </summary>
    private void InitializeCraftingView()
    {
        if (currentCraftingRecipe == null) return;

        // 제작 중인 칵테일 이름 표시 (예: "모히토")
        if (craftingCocktailNameText != null)
        {
            craftingCocktailNameText.text = currentCraftingRecipe.CocktailName;
        }

        // CocktailSystem 초기화 (이전에 선택한 재료/기법/잔 모두 제거)
        cocktailSystem.ClearIngredients();

        // 테이블 위 재료 초기화 (이전 제작 시 추가된 재료 제거)
        ClearTableIngredients();

        // 재료 선택 UI 초기화 (사용 가능한 재료 목록 표시)
        if (ingredientSelectionUI != null)
            ingredientSelectionUI.ShowAvailableIngredients();

        // 기법 선택 UI 초기화 (Build/Floating/Shaking 버튼 선택 해제)
        if (techniqueSelectionUI != null)
            techniqueSelectionUI.ResetSelection();

        // 잔 선택 UI 초기화 (사용 가능한 잔 목록 표시, 선택 해제)
        if (glassSelectionUI != null)
        {
            glassSelectionUI.ShowAvailableGlasses();
            glassSelectionUI.ResetSelection();
        }

        // 선택된 잔 이미지 초기화 (숨김)
        if (selectedGlassDisplayImage != null)
        {
            selectedGlassDisplayImage.gameObject.SetActive(false);
            selectedGlassDisplayImage.sprite = null;
        }

        // 도구 선택 UI 초기화 (쉐이커/바스푼 선택 해제)
        if (toolSelectionUI != null)
        {
            toolSelectionUI.SetCurrentRecipe(currentCraftingRecipe);
            toolSelectionUI.ResetSelection();
        }

        // 1단계(도구 선택)부터 시작
        currentStep = CraftingStep.ToolSelection;
        ShowCraftingStep(currentStep);
    }

    /// <summary>
    /// 현재 제작 단계에 맞는 패널을 표시합니다.
    /// </summary>
    private void ShowCraftingStep(CraftingStep step)
    {
        // 모든 단계 패널 비활성화
        if (toolSelectionUI != null) toolSelectionUI.gameObject.SetActive(false);
        if (glassSelectionUI != null) glassSelectionUI.gameObject.SetActive(false);
        if (ingredientSelectionUI != null) ingredientSelectionUI.gameObject.SetActive(false);

        // 현재 단계 패널만 활성화
        switch (step)
        {
            case CraftingStep.ToolSelection:
                if (toolSelectionUI != null) toolSelectionUI.gameObject.SetActive(true);
                // 다음 단계 버튼 표시, 완료 버튼 숨김
                if (nextStepButton != null) nextStepButton.gameObject.SetActive(true);
                if (completeButton != null) completeButton.gameObject.SetActive(false);
                break;

            case CraftingStep.GlassSelection:
                if (glassSelectionUI != null) glassSelectionUI.gameObject.SetActive(true);
                // 다음 단계 버튼 표시, 완료 버튼 숨김
                if (nextStepButton != null) nextStepButton.gameObject.SetActive(true);
                if (completeButton != null) completeButton.gameObject.SetActive(false);
                break;

            case CraftingStep.IngredientSelection:
                if (ingredientSelectionUI != null) ingredientSelectionUI.gameObject.SetActive(true);
                // 다음 단계 버튼 숨김, 완료 버튼 표시
                if (nextStepButton != null) nextStepButton.gameObject.SetActive(false);
                if (completeButton != null) completeButton.gameObject.SetActive(true);
                break;
        }

        Debug.Log($"제작 단계 전환: {step}");
    }

    /// <summary>
    /// "다음 단계" 버튼 클릭 시 호출
    /// </summary>
    private void OnNextStepClicked()
    {
        // 다음 단계로 이동
        switch (currentStep)
        {
            case CraftingStep.ToolSelection:
                currentStep = CraftingStep.GlassSelection;
                break;

            case CraftingStep.GlassSelection:
                currentStep = CraftingStep.IngredientSelection;
                break;
        }

        ShowCraftingStep(currentStep);
    }

    /// <summary>
    /// "제작 완료" 버튼 클릭 시 호출
    /// 1. CocktailSystem으로 사용자가 만든 칵테일 검증 (점수 계산)
    /// 2. 성공 시: OrderingManager에 완료 표시, 주문 목록으로 돌아감
    /// 3. 실패 시: 실패 메시지 표시 (TODO: UI 피드백)
    /// </summary>
    private void OnCompleteCraftingClicked()
    {
        if (currentCraftingRecipe == null) return;

        // 도구 검증: 선택한 도구가 레시피의 기법과 호환되는지 확인
        if (!cocktailSystem.IsToolCompatibleWithTechnique(currentCraftingRecipe.Technique))
        {
            if (toastMessageUI != null)
            {
                toastMessageUI.ShowToast("해당 레시피와 맞지 않는 기법입니다");
            }
            else
            {
                Debug.LogWarning("ToastMessageUI가 할당되지 않았습니다.");
            }
            return; // 검증 실패 시 제작 중단
        }

        // CocktailRepository에서 칵테일 데이터 가져오기 (성공 기준 점수 확인용)
        CocktailData cocktailData = CocktailRepository.Instance.GetCocktailDataById(currentCraftingRecipe.CocktailId);

        // CocktailSystem으로 검증: 사용자가 선택한 재료/기법/잔과 레시피 비교하여 점수 계산
        float score = cocktailSystem.CheckCocktailToRecipe(currentCraftingRecipe.CocktailId);

        // 성공 여부 판단 (점수가 similarity_threadhold 이상이면 성공)
        if (cocktailData != null && score >= cocktailData.similarity_threadhold)
        {
            // 제작 성공!
            // OrderingManager에 완료 표시 (주문 목록에서 체크마크 표시됨)
            OrderingManager.Instance.MarkCocktailAsCompleted(currentCraftingRecipe);
            Debug.Log($"{currentCraftingRecipe.CocktailName} 제작 완료!");

            // 주문 목록 화면으로 돌아가기 (완료된 칵테일에 체크마크 표시됨)
            ShowOrderListView();
        }
        else
        {
            // 제작 실패
            Debug.Log($"칵테일 제작 실패. 점수: {score}");
            // TODO: 실패 피드백 UI 표시 (예: "재료가 부족합니다", "점수가 낮습니다" 등)
        }
    }

    /// <summary>
    /// GlassSelectionUI에서 잔이 선택되었을 때 호출됨
    /// </summary>
    /// <param name="glass">선택된 잔 데이터</param>
    private void OnGlassSelectedInUI(Glass glass)
    {
        if (selectedGlassDisplayImage != null && glass != null)
        {
            selectedGlassDisplayImage.sprite = glass.Icon;
            selectedGlassDisplayImage.gameObject.SetActive(true);

            // Native Size로 설정하여 비율 유지 (필요 시)
            selectedGlassDisplayImage.SetNativeSize();
        }
    }

    /// <summary>
    /// IngredientSelectionUI에서 재료가 선택되었을 때 호출됨
    /// </summary>
    /// <param name="ingredient">선택된 재료 데이터</param>
    private void OnIngredientSelectedInUI(Ingridiant ingredient)
    {
        if (ingredient != null)
        {
            CreateDraggableIngredient(ingredient);
        }
    }

    /// <summary>
    /// 테이블 위에 드래그 가능한 재료 아이콘을 생성합니다.
    /// </summary>
    /// <param name="ingredient">표시할 재료 데이터</param>
    private void CreateDraggableIngredient(Ingridiant ingredient)
    {
        if (draggableIngredientPrefab == null || tableIngredientArea == null) return;

        GameObject ingredientObj = Instantiate(draggableIngredientPrefab, tableIngredientArea);
        instantiatedTableIngredients.Add(ingredientObj);

        // DraggableIngredient 컴포넌트에 재료 데이터 전달
        var draggable = ingredientObj.GetComponent<DraggableIngredient>();
        if (draggable != null)
        {
            draggable.SetIngredient(ingredient);
        }

        Debug.Log($"테이블에 재료 추가: {ingredient.Ingridiant_name}");
    }

    /// <summary>
    /// 테이블 위의 모든 재료를 제거합니다. (새 칵테일 제작 시 호출)
    /// </summary>
    private void ClearTableIngredients()
    {
        foreach (GameObject ingredientObj in instantiatedTableIngredients)
        {
            Destroy(ingredientObj);
        }
        instantiatedTableIngredients.Clear();
    }

    /// <summary>
    /// 컴포넌트 파괴 시 이벤트 리스너 정리 (메모리 누수 방지)
    /// </summary>
    private void OnDestroy()
    {
        if (backButton != null)
        {
            backButton.onClick.RemoveListener(ShowOrderListView);
        }

        if (nextStepButton != null)
        {
            nextStepButton.onClick.RemoveListener(OnNextStepClicked);
        }

        if (completeButton != null)
        {
            completeButton.onClick.RemoveListener(OnCompleteCraftingClicked);
        }

        if (glassSelectionUI != null)
        {
            glassSelectionUI.OnGlassSelectedEvent -= OnGlassSelectedInUI;
        }

        if (ingredientSelectionUI != null)
        {
            ingredientSelectionUI.OnIngredientSelectedEvent -= OnIngredientSelectedInUI;
        }
    }
}
