using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SelectionPanel : MonoBehaviour
{
    private GameObject glassPanel;
    private GameObject spiritPanel;
    private GameObject mixerPanel;
    private GameObject garnishPanel;

    private Transform glassContent;
    private Transform spiritContent;
    private Transform mixerContent;
    private Transform garnishContent;

    private Button glassTabButton;
    private Button spiritTabButton;
    private Button mixerTabButton;
    private Button garnishTabButton;

    private Button nextButton;
    private Button prevButton;

    private TextMeshProUGUI categoryTitle;
    private TextMeshProUGUI selectionCountText;

    private enum SelectionCategory { Glass, Spirit, Mixer, Garnish }
    private SelectionCategory currentCategory = SelectionCategory.Glass;

    private IngredientButton selectedGlassButton;
    private List<IngredientButton> selectedSpiritButtons = new List<IngredientButton>();
    private List<IngredientButton> selectedMixerButtons = new List<IngredientButton>();
    private IngredientButton selectedGarnishButton;

    private CocktailRecipe currentRecipe = new CocktailRecipe();

    public System.Action<CocktailRecipe> OnRecipeChanged;

    private void Start()
    {
        BuildUI();
        SetupButtons();
        ShowCategory(SelectionCategory.Glass);
        PopulateAllCategories();
        UpdateNavigationButtons();
    }

    private void BuildUI()
    {
        // 메인 패널
        RectTransform rect = gameObject.GetComponent<RectTransform>();
        if (rect == null)
            rect = gameObject.AddComponent<RectTransform>();

        rect.anchorMin = new Vector2(1, 0);
        rect.anchorMax = new Vector2(1, 1);
        rect.pivot = new Vector2(1, 0.5f);
        rect.anchoredPosition = new Vector2(-20, 0);
        rect.sizeDelta = new Vector2(400, 0);

        Image panelBg = gameObject.GetComponent<Image>();
        if (panelBg == null)
            panelBg = gameObject.AddComponent<Image>();
        panelBg.color = new Color(0.15f, 0.15f, 0.15f, 0.95f);

        // 상단 타이틀 영역
        CreateTitleArea();

        // 탭 버튼 영역
        CreateTabButtons();

        // 카테고리 패널들
        CreateCategoryPanels();

        // 하단 네비게이션
        CreateNavigationButtons();
    }

    private void CreateTitleArea()
    {
        GameObject titleArea = new GameObject("TitleArea");
        titleArea.transform.SetParent(transform, false);
        RectTransform titleRect = titleArea.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0, 1);
        titleRect.anchorMax = new Vector2(1, 1);
        titleRect.pivot = new Vector2(0.5f, 1);
        titleRect.anchoredPosition = Vector2.zero;
        titleRect.sizeDelta = new Vector2(0, 80);

        // 카테고리 제목
        GameObject titleObj = new GameObject("CategoryTitle");
        titleObj.transform.SetParent(titleArea.transform, false);
        RectTransform titleTextRect = titleObj.AddComponent<RectTransform>();
        titleTextRect.anchorMin = new Vector2(0, 0.5f);
        titleTextRect.anchorMax = new Vector2(0.6f, 1);
        titleTextRect.offsetMin = new Vector2(20, 0);
        titleTextRect.offsetMax = new Vector2(0, -10);
        categoryTitle = titleObj.AddComponent<TextMeshProUGUI>();
        categoryTitle.text = "Ingredient Selection";
        categoryTitle.fontSize = 24;
        categoryTitle.fontStyle = FontStyles.Bold;
        categoryTitle.color = Color.white;

        // 선택 카운트
        GameObject countObj = new GameObject("SelectionCount");
        countObj.transform.SetParent(titleArea.transform, false);
        RectTransform countRect = countObj.AddComponent<RectTransform>();
        countRect.anchorMin = new Vector2(0.6f, 0.5f);
        countRect.anchorMax = new Vector2(1, 1);
        countRect.offsetMin = Vector2.zero;
        countRect.offsetMax = new Vector2(-20, -10);
        selectionCountText = countObj.AddComponent<TextMeshProUGUI>();
        selectionCountText.text = "0/1";
        selectionCountText.fontSize = 20;
        selectionCountText.alignment = TextAlignmentOptions.Right;
        selectionCountText.color = Color.yellow;
    }

    private void CreateTabButtons()
    {
        GameObject tabArea = new GameObject("TabArea");
        tabArea.transform.SetParent(transform, false);
        RectTransform tabRect = tabArea.AddComponent<RectTransform>();
        tabRect.anchorMin = new Vector2(0, 1);
        tabRect.anchorMax = new Vector2(1, 1);
        tabRect.pivot = new Vector2(0.5f, 1);
        tabRect.anchoredPosition = new Vector2(0, -80);
        tabRect.sizeDelta = new Vector2(0, 60);

        HorizontalLayoutGroup layoutGroup = tabArea.AddComponent<HorizontalLayoutGroup>();
        layoutGroup.spacing = 5;
        layoutGroup.padding = new RectOffset(10, 10, 5, 5);
        layoutGroup.childForceExpandWidth = true;
        layoutGroup.childForceExpandHeight = true;

        glassTabButton = CreateTabButton(tabArea.transform, "Glass");
        spiritTabButton = CreateTabButton(tabArea.transform, "Spirit");
        mixerTabButton = CreateTabButton(tabArea.transform, "Mixer");
        garnishTabButton = CreateTabButton(tabArea.transform, "Garnish");
    }

    private Button CreateTabButton(Transform parent, string label)
    {
        GameObject btnObj = new GameObject($"Tab_{label}");
        btnObj.transform.SetParent(parent, false);

        Button btn = btnObj.AddComponent<Button>();
        Image btnImage = btnObj.AddComponent<Image>();
        btnImage.color = new Color(0.3f, 0.3f, 0.3f, 1f);

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(btnObj.transform, false);
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
        text.text = label;
        text.fontSize = 18;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;

        return btn;
    }

    private void CreateCategoryPanels()
    {
        glassPanel = CreateCategoryPanel("GlassPanel", -140);
        spiritPanel = CreateCategoryPanel("SpiritPanel", -140);
        mixerPanel = CreateCategoryPanel("MixerPanel", -140);
        garnishPanel = CreateCategoryPanel("GarnishPanel", -140);

        glassContent = CreateScrollView(glassPanel);
        spiritContent = CreateScrollView(spiritPanel);
        mixerContent = CreateScrollView(mixerPanel);
        garnishContent = CreateScrollView(garnishPanel);
    }

    private GameObject CreateCategoryPanel(string name, float yOffset)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(transform, false);
        RectTransform rect = panel.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 0);
        rect.anchorMax = new Vector2(1, 1);
        rect.offsetMin = new Vector2(10, 70);
        rect.offsetMax = new Vector2(-10, yOffset);

        return panel;
    }

    private Transform CreateScrollView(GameObject panel)
    {
        // ScrollView
        GameObject scrollView = new GameObject("ScrollView");
        scrollView.transform.SetParent(panel.transform, false);
        RectTransform scrollRect = scrollView.AddComponent<RectTransform>();
        scrollRect.anchorMin = Vector2.zero;
        scrollRect.anchorMax = Vector2.one;
        scrollRect.offsetMin = Vector2.zero;
        scrollRect.offsetMax = Vector2.zero;

        Image scrollBg = scrollView.AddComponent<Image>();
        scrollBg.color = new Color(0.1f, 0.1f, 0.1f, 0.5f);

        ScrollRect scroll = scrollView.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical = true;

        // Viewport
        GameObject viewport = new GameObject("Viewport");
        viewport.transform.SetParent(scrollView.transform, false);
        RectTransform viewportRect = viewport.AddComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = Vector2.zero;
        viewportRect.offsetMax = Vector2.zero;
        viewport.AddComponent<Mask>();
        Image viewportImage = viewport.AddComponent<Image>();
        viewportImage.color = Color.clear;

        // Content
        GameObject content = new GameObject("Content");
        content.transform.SetParent(viewport.transform, false);
        RectTransform contentRect = content.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 1);
        contentRect.anchorMax = new Vector2(1, 1);
        contentRect.pivot = new Vector2(0.5f, 1);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = new Vector2(0, 0);

        GridLayoutGroup grid = content.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(170, 200);
        grid.spacing = new Vector2(10, 10);
        grid.padding = new RectOffset(10, 10, 10, 10);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 2;

        ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scroll.viewport = viewportRect;
        scroll.content = contentRect;

        return content.transform;
    }

    private void CreateNavigationButtons()
    {
        GameObject navArea = new GameObject("NavigationArea");
        navArea.transform.SetParent(transform, false);
        RectTransform navRect = navArea.AddComponent<RectTransform>();
        navRect.anchorMin = new Vector2(0, 0);
        navRect.anchorMax = new Vector2(1, 0);
        navRect.pivot = new Vector2(0.5f, 0);
        navRect.anchoredPosition = Vector2.zero;
        navRect.sizeDelta = new Vector2(0, 60);

        HorizontalLayoutGroup layoutGroup = navArea.AddComponent<HorizontalLayoutGroup>();
        layoutGroup.spacing = 10;
        layoutGroup.padding = new RectOffset(10, 10, 10, 10);
        layoutGroup.childForceExpandWidth = true;
        layoutGroup.childForceExpandHeight = true;

        prevButton = CreateNavButton(navArea.transform, "Previous");
        nextButton = CreateNavButton(navArea.transform, "Next");
    }

    private Button CreateNavButton(Transform parent, string label)
    {
        GameObject btnObj = new GameObject($"Btn_{label}");
        btnObj.transform.SetParent(parent, false);

        Button btn = btnObj.AddComponent<Button>();
        Image btnImage = btnObj.AddComponent<Image>();
        btnImage.color = new Color(0.2f, 0.5f, 0.8f, 1f);

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(btnObj.transform, false);
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
        text.text = label;
        text.fontSize = 20;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;

        return btn;
    }

    private void SetupButtons()
    {
        glassTabButton.onClick.AddListener(() => ShowCategory(SelectionCategory.Glass));
        spiritTabButton.onClick.AddListener(() => ShowCategory(SelectionCategory.Spirit));
        mixerTabButton.onClick.AddListener(() => ShowCategory(SelectionCategory.Mixer));
        garnishTabButton.onClick.AddListener(() => ShowCategory(SelectionCategory.Garnish));

        nextButton.onClick.AddListener(NextCategory);
        prevButton.onClick.AddListener(PrevCategory);
    }

    private void PopulateAllCategories()
    {
        if (CocktailDataManager.Instance == null)
        {
            Debug.LogError("CocktailDataManager가 씬에 없습니다!");
            return;
        }

        PopulateGlasses();
        PopulateSpirits();
        PopulateMixers();
        PopulateGarnishes();
    }

    private void PopulateGlasses()
    {
        foreach (var glass in CocktailDataManager.Instance.glasses)
        {
            IngredientButton btn = IngredientButton.Create(glassContent);
            btn.SetupGlass(glass, OnGlassSelected);
        }
    }

    private void PopulateSpirits()
    {
        foreach (var spirit in CocktailDataManager.Instance.baseSpirits)
        {
            IngredientButton btn = IngredientButton.Create(spiritContent);
            btn.SetupSpirit(spirit, OnSpiritSelected);
        }
    }

    private void PopulateMixers()
    {
        foreach (var mixer in CocktailDataManager.Instance.mixers)
        {
            IngredientButton btn = IngredientButton.Create(mixerContent);
            btn.SetupMixer(mixer, OnMixerSelected);
        }
    }

    private void PopulateGarnishes()
    {
        foreach (var garnish in CocktailDataManager.Instance.garnishes)
        {
            IngredientButton btn = IngredientButton.Create(garnishContent);
            btn.SetupGarnish(garnish, OnGarnishSelected);
        }
    }

    private void OnGlassSelected(IngredientButton button)
    {
        if (selectedGlassButton != null)
            selectedGlassButton.SetSelected(false);

        selectedGlassButton = button;
        button.SetSelected(true);
        currentRecipe.selectedGlass = button.GetData<GlassData>();

        UpdateSelectionCount();
        OnRecipeChanged?.Invoke(currentRecipe);
    }

    private void OnSpiritSelected(IngredientButton button)
    {
        if (selectedSpiritButtons.Contains(button))
        {
            selectedSpiritButtons.Remove(button);
            button.SetSelected(false);
            currentRecipe.selectedSpirits.Remove(button.GetData<BaseSpiritData>());
        }
        else if (selectedSpiritButtons.Count < 3)
        {
            selectedSpiritButtons.Add(button);
            button.SetSelected(true);
            currentRecipe.selectedSpirits.Add(button.GetData<BaseSpiritData>());
        }

        UpdateSelectionCount();
        OnRecipeChanged?.Invoke(currentRecipe);
    }

    private void OnMixerSelected(IngredientButton button)
    {
        if (selectedMixerButtons.Contains(button))
        {
            selectedMixerButtons.Remove(button);
            button.SetSelected(false);
            currentRecipe.selectedMixers.Remove(button.GetData<MixerData>());
        }
        else if (selectedMixerButtons.Count < 3)
        {
            selectedMixerButtons.Add(button);
            button.SetSelected(true);
            currentRecipe.selectedMixers.Add(button.GetData<MixerData>());
        }

        UpdateSelectionCount();
        OnRecipeChanged?.Invoke(currentRecipe);
    }

    private void OnGarnishSelected(IngredientButton button)
    {
        if (selectedGarnishButton != null)
            selectedGarnishButton.SetSelected(false);

        selectedGarnishButton = button;
        button.SetSelected(true);
        currentRecipe.selectedGarnish = button.GetData<GarnishData>();

        UpdateSelectionCount();
        OnRecipeChanged?.Invoke(currentRecipe);
    }

    private void ShowCategory(SelectionCategory category)
    {
        currentCategory = category;

        glassPanel.SetActive(category == SelectionCategory.Glass);
        spiritPanel.SetActive(category == SelectionCategory.Spirit);
        mixerPanel.SetActive(category == SelectionCategory.Mixer);
        garnishPanel.SetActive(category == SelectionCategory.Garnish);

        UpdateCategoryTitle();
        UpdateSelectionCount();
        UpdateNavigationButtons();
    }

    private void UpdateCategoryTitle()
    {
        switch (currentCategory)
        {
            case SelectionCategory.Glass:
                categoryTitle.text = "Select Glass";
                break;
            case SelectionCategory.Spirit:
                categoryTitle.text = "Select Base Spirit";
                break;
            case SelectionCategory.Mixer:
                categoryTitle.text = "Select Mixer";
                break;
            case SelectionCategory.Garnish:
                categoryTitle.text = "Select Garnish";
                break;
        }
    }

    private void UpdateSelectionCount()
    {
        string count = "";
        switch (currentCategory)
        {
            case SelectionCategory.Glass:
                count = selectedGlassButton != null ? "1/1" : "0/1";
                break;
            case SelectionCategory.Spirit:
                count = $"{selectedSpiritButtons.Count}/3";
                break;
            case SelectionCategory.Mixer:
                count = $"{selectedMixerButtons.Count}/3";
                break;
            case SelectionCategory.Garnish:
                count = selectedGarnishButton != null ? "1/1" : "0/1";
                break;
        }
        selectionCountText.text = $"Selected: {count}";
    }

    private void UpdateNavigationButtons()
    {
        prevButton.interactable = currentCategory != SelectionCategory.Glass;
        nextButton.interactable = currentCategory != SelectionCategory.Garnish;
    }

    private void NextCategory()
    {
        if (currentCategory < SelectionCategory.Garnish)
        {
            ShowCategory(currentCategory + 1);
        }
    }

    private void PrevCategory()
    {
        if (currentCategory > SelectionCategory.Glass)
        {
            ShowCategory(currentCategory - 1);
        }
    }

    public CocktailRecipe GetCurrentRecipe()
    {
        return currentRecipe;
    }

    public void ResetSelection()
    {
        if (selectedGlassButton != null)
            selectedGlassButton.SetSelected(false);

        foreach (var btn in selectedSpiritButtons)
            btn.SetSelected(false);

        foreach (var btn in selectedMixerButtons)
            btn.SetSelected(false);

        if (selectedGarnishButton != null)
            selectedGarnishButton.SetSelected(false);

        selectedGlassButton = null;
        selectedSpiritButtons.Clear();
        selectedMixerButtons.Clear();
        selectedGarnishButton = null;

        currentRecipe = new CocktailRecipe();
        OnRecipeChanged?.Invoke(currentRecipe);
        UpdateSelectionCount();
    }
} 