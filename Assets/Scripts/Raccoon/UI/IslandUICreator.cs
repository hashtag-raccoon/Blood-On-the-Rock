using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.Antlr3.Runtime.Tree;
using UnityEngine;
using UnityEngine.UI;

public class IslandUICreator : MonoBehaviour
{
    [Header("섬 UI 세팅")]
    [SerializeField] private GameObject EditUI;
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private Transform content;
    [SerializeField] private GameObject buildingButtonPrefab;

    [Header("매니저 할당(데이터/카메라)")]
    [SerializeField] private DataManager dataManager;
    [SerializeField] private CameraManager cameraManager;

    [Header("레이아웃 세팅")]
    [SerializeField] private float spacing = 10f;
    [SerializeField] private int paddingLeft = 10;
    [SerializeField] private int paddingRight = 10;
    [SerializeField] private int paddingTop = 10;
    [SerializeField] private int paddingBottom = 10;
    [SerializeField] private float buttonWidth;

    [Header("Edit 버튼")]
    [SerializeField] private Button editButton;

    private List<BuildingButtonUI> buttonuiList = new List<BuildingButtonUI>();
    private RectOffset padding;

    private void Awake()
    {
        editButton.onClick.AddListener(ActiveEditButton);
        buttonWidth = buildingButtonPrefab.GetComponent<RectTransform>().sizeDelta.x;
        padding = new RectOffset(paddingLeft, paddingRight, paddingTop, paddingBottom);

        SetupScrollView();
        GenerateButtons();
    }

    private void SetupScrollView()
    {
        if (scrollRect != null)
        {
            scrollRect.horizontal = true;
            scrollRect.vertical = false;
            scrollRect.movementType = ScrollRect.MovementType.Elastic;
        }

        HorizontalLayoutGroup layoutGroup = content.GetComponent<HorizontalLayoutGroup>();
        if (layoutGroup == null)
        {
            layoutGroup = content.gameObject.AddComponent<HorizontalLayoutGroup>();
        }

        layoutGroup.spacing = spacing;
        layoutGroup.padding = padding;
        layoutGroup.childAlignment = TextAnchor.MiddleLeft; 
        layoutGroup.childControlWidth = true;
        layoutGroup.childControlHeight = true;
        layoutGroup.childForceExpandWidth = false;
        layoutGroup.childForceExpandHeight = true;

        ContentSizeFitter fitter = content.GetComponent<ContentSizeFitter>();
        if (fitter == null)
        {
            fitter = content.gameObject.AddComponent<ContentSizeFitter>();
        }
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
    }

    private void GenerateButtons()
    {
        ClearButtons();

        foreach (var buildingData in dataManager.BuildingDatas)
        {
            CreateBuildingButton(buildingData);
        }
    }

    private void CreateBuildingButton(BuildingData buildingData)
    {
        GameObject buttonObj = Instantiate(buildingButtonPrefab, content);

        LayoutElement layoutElement = buttonObj.GetComponent<LayoutElement>();
        if (layoutElement == null)
        {
            layoutElement = buttonObj.AddComponent<LayoutElement>();
        }
        layoutElement.preferredWidth = buttonWidth;
        layoutElement.flexibleHeight = 1f;

        BuildingButtonUI buttonUI = buttonObj.GetComponent<BuildingButtonUI>();
        if (buttonUI != null)
        {
            buttonUI.SetData(buildingData, OnBuildingButtonClicked);
            buttonuiList.Add(buttonUI);
        }
    }

    private void OnBuildingButtonClicked(BuildingButtonUI clickedButton)
    {
        BuildingData data = clickedButton.GetData<BuildingData>();
        // 추가로 나중에 구현할 예정
    }

    private void ClearButtons()
    {
        foreach (Transform child in content)
        {
            Destroy(child.gameObject);
        }
        buttonuiList.Clear();
    }
    public void AddButton(BuildingData buildingData)
    {
        CreateBuildingButton(buildingData);
    }

    public void RefreshButtons()
    {
        GenerateButtons();
    }

    private void ActiveEditButton()
    {
        EditUI.SetActive(true);
    }
}