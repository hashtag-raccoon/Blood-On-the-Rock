using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CocktailMakingManager : MonoBehaviour
{
    [Header("Settings")]
    public bool isMaking = false;

    private SelectionPanel selectionPanel;
    private Button barSpoonButton;
    private Button shakerButton;
    private GameObject toolButtonArea;
    private Canvas canvas;

    private Color inactiveButtonColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
    private Color activeButtonColor = new Color(0.4f, 0.7f, 0.3f, 1f);

    private CocktailRecipe currentRecipe;
    private bool wasUICreated = false;

    private void Start()
    {
        if (isMaking)
        {
            CreateUI();
        }
    }

    private void Update()
    {
        // isMaking 상태 변화 감지
        if (isMaking && !wasUICreated)
        {
            CreateUI();
        }
        else if (!isMaking && wasUICreated)
        {
            DestroyUI();
        }
    }

    private void CreateUI()
    {
        if (wasUICreated) return;

        BuildUI();
        UpdateToolButtons(false);

        if (selectionPanel != null)
        {
            selectionPanel.OnRecipeChanged += OnRecipeUpdated;
        }

        wasUICreated = true;
        Debug.Log("Cocktail Making UI Created");
    }

    private void DestroyUI()
    {
        if (!wasUICreated) return;

        if (selectionPanel != null)
        {
            selectionPanel.OnRecipeChanged -= OnRecipeUpdated;
            Destroy(selectionPanel.gameObject);
            selectionPanel = null;
        }

        if (toolButtonArea != null)
        {
            Destroy(toolButtonArea);
            toolButtonArea = null;
        }

        barSpoonButton = null;
        shakerButton = null;
        currentRecipe = null;

        wasUICreated = false;
        Debug.Log("Cocktail Making UI Destroyed");
    }

    public void ToggleUI(bool enable)
    {
        isMaking = enable;
    }

    private void BuildUI()
    {
        // Canvas 찾기 또는 생성
        canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        // SelectionPanel 생성 (우측)
        CreateSelectionPanel(canvas.transform);

        // Tool Buttons 생성 (좌측 하단)
        CreateToolButtons(canvas.transform);
    }

    private void CreateSelectionPanel(Transform parent)
    {
        GameObject panelObj = new GameObject("SelectionPanel");
        panelObj.transform.SetParent(parent, false);
        selectionPanel = panelObj.AddComponent<SelectionPanel>();
    }

    private void CreateToolButtons(Transform parent)
    {
        // 버튼 컨테이너
        toolButtonArea = new GameObject("ToolButtonArea");
        toolButtonArea.transform.SetParent(parent, false);
        RectTransform areaRect = toolButtonArea.AddComponent<RectTransform>();
        areaRect.anchorMin = new Vector2(0, 0);
        areaRect.anchorMax = new Vector2(0, 0);
        areaRect.pivot = new Vector2(0, 0);
        areaRect.anchoredPosition = new Vector2(20, 20);
        areaRect.sizeDelta = new Vector2(200, 400);

        VerticalLayoutGroup layoutGroup = toolButtonArea.AddComponent<VerticalLayoutGroup>();
        layoutGroup.spacing = 20;
        layoutGroup.padding = new RectOffset(0, 0, 0, 0);
        layoutGroup.childForceExpandWidth = true;
        layoutGroup.childForceExpandHeight = false;
        layoutGroup.childControlHeight = false;

        // 바스푼 버튼
        barSpoonButton = CreateToolButton(toolButtonArea.transform, "Bar Spoon", "Build/Float");

        // 쉐이커 버튼
        shakerButton = CreateToolButton(toolButtonArea.transform, "Shaker", "Shake");

        // 버튼 클릭 이벤트
        barSpoonButton.onClick.AddListener(OnBarSpoonClicked);
        shakerButton.onClick.AddListener(OnShakerClicked);
    }

    private Button CreateToolButton(Transform parent, string title, string subtitle)
    {
        GameObject btnObj = new GameObject($"Btn_{title}");
        btnObj.transform.SetParent(parent, false);

        RectTransform btnRect = btnObj.AddComponent<RectTransform>();
        btnRect.sizeDelta = new Vector2(0, 180);

        Button btn = btnObj.AddComponent<Button>();
        Image btnImage = btnObj.AddComponent<Image>();
        btnImage.color = inactiveButtonColor;

        // 배경 패널
        GameObject bgPanel = new GameObject("Background");
        bgPanel.transform.SetParent(btnObj.transform, false);
        RectTransform bgRect = bgPanel.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        Image bgImage = bgPanel.AddComponent<Image>();
        bgImage.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);

        // 아이콘 영역
        GameObject iconObj = new GameObject("Icon");
        iconObj.transform.SetParent(btnObj.transform, false);
        RectTransform iconRect = iconObj.AddComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0.5f, 0.5f);
        iconRect.anchorMax = new Vector2(0.5f, 0.5f);
        iconRect.anchoredPosition = new Vector2(0, 25);
        iconRect.sizeDelta = new Vector2(100, 100);
        Image iconImage = iconObj.AddComponent<Image>();

        if (CocktailDataManager.Instance != null)
        {
            if (title == "Bar Spoon")
                iconImage.sprite = CocktailDataManager.Instance.barSpoonIcon;
            else if (title == "Shaker")
                iconImage.sprite = CocktailDataManager.Instance.shakerIcon;
        }
        iconImage.preserveAspect = true;

        // 타이틀 텍스트
        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(btnObj.transform, false);
        RectTransform titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0, 0);
        titleRect.anchorMax = new Vector2(1, 0);
        titleRect.anchoredPosition = new Vector2(0, 40);
        titleRect.sizeDelta = new Vector2(-20, 30);
        TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = title;
        titleText.fontSize = 22;
        titleText.fontStyle = FontStyles.Bold;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.color = Color.white;

        // 서브타이틀 텍스트
        GameObject subtitleObj = new GameObject("Subtitle");
        subtitleObj.transform.SetParent(btnObj.transform, false);
        RectTransform subtitleRect = subtitleObj.AddComponent<RectTransform>();
        subtitleRect.anchorMin = new Vector2(0, 0);
        subtitleRect.anchorMax = new Vector2(1, 0);
        subtitleRect.anchoredPosition = new Vector2(0, 15);
        subtitleRect.sizeDelta = new Vector2(-20, 20);
        TextMeshProUGUI subtitleText = subtitleObj.AddComponent<TextMeshProUGUI>();
        subtitleText.text = subtitle;
        subtitleText.fontSize = 16;
        subtitleText.alignment = TextAlignmentOptions.Center;
        subtitleText.color = new Color(0.7f, 0.7f, 0.7f, 1f);

        return btn;
    }

    private void OnRecipeUpdated(CocktailRecipe recipe)
    {
        currentRecipe = recipe;
        bool canActivate = recipe.IsComplete();
        UpdateToolButtons(canActivate);
    }

    private void UpdateToolButtons(bool active)
    {
        barSpoonButton.interactable = active;
        shakerButton.interactable = active;

        var barSpoonImage = barSpoonButton.GetComponent<Image>();
        var shakerImage = shakerButton.GetComponent<Image>();

        if (barSpoonImage != null)
            barSpoonImage.color = active ? activeButtonColor : inactiveButtonColor;

        if (shakerImage != null)
            shakerImage.color = active ? activeButtonColor : inactiveButtonColor;
    }

    private void OnBarSpoonClicked()
    {
        if (currentRecipe == null || !currentRecipe.IsComplete())
            return;

        Debug.Log("Bar Spoon Button Clicked - Build/Float Event Start");
    }

    private void OnShakerClicked()
    {
        if (currentRecipe == null || !currentRecipe.IsComplete())
            return;

        Debug.Log("Shaker Button Clicked - Shaking Event Start");
    }

    public void ResetAll()
    {
        if (selectionPanel != null)
        {
            selectionPanel.ResetSelection();
        }

        currentRecipe = null;
        UpdateToolButtons(false);
    }

    private void OnDestroy()
    {
        if (selectionPanel != null)
        {
            selectionPanel.OnRecipeChanged -= OnRecipeUpdated;
        }
    }
}