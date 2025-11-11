using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class ResourceBuildingBase : BuildingBase
{
    [Header("생산 데이터")]
    public List<BuildingProductionData> productionDatas = new List<BuildingProductionData>();
    [Header("스크롤 UI 세팅")]
    public GameObject itemPrefab;
    public Transform contentParent;
    public ScrollRect scrollRect;
    [Header("레이아웃 설정")]
    public float itemWidth = 100f;
    public float itemHeight = 100f;
    public float spacing = 10f;
    public int paddingLeft = 10;
    public int paddingRight = 10;
    public int paddingTop = 10;
    public int paddingBottom = 10;

    private RectOffset padding;

    private void Awake()
    {
        InitializeLayout();
        SetupScrollView();
    }

    protected override void Start()
    {
        productionDatas = DataManager.Instance.GetBuildingProductionDataByType(Buildingdata.BuildingName);
    }

    protected virtual void InitializeLayout()
    {
        if (itemPrefab != null)
        {
            itemWidth = itemPrefab.GetComponent<RectTransform>().sizeDelta.x;
            itemHeight = itemPrefab.GetComponent<RectTransform>().sizeDelta.y;
        }

        padding = new RectOffset(paddingLeft, paddingRight, paddingTop, paddingBottom);
    }

    protected virtual void SetupScrollView()
    {
        if (scrollRect != null)
        {
            scrollRect.horizontal = true;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Elastic;
        }

        SetupLayoutGroup();
        SetupContentSizeFitter();
    }

    protected virtual void SetupLayoutGroup()
    {
        VerticalLayoutGroup layoutGroup = contentParent.GetComponent<VerticalLayoutGroup>();
        if (layoutGroup == null)
        {
            layoutGroup = contentParent.gameObject.AddComponent<VerticalLayoutGroup>();
        }

        layoutGroup.padding = padding;
        layoutGroup.spacing = spacing;
        layoutGroup.childAlignment = TextAnchor.UpperLeft;
        layoutGroup.childForceExpandWidth = false;
        layoutGroup.childForceExpandHeight = false;
    }

    protected virtual void SetupContentSizeFitter()
    {
        ContentSizeFitter sizeFitter = contentParent.GetComponent<ContentSizeFitter>();
        if (sizeFitter == null)
        {
            sizeFitter = contentParent.gameObject.AddComponent<ContentSizeFitter>();
        }

        sizeFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    public void GenerateItems()
    {
        ClearItems();
        foreach (var data in productionDatas)
        {
            CreateItem(data);
        }
    }

    public void ClearItems()
    {
        foreach (Transform child in contentParent)
        {
            Destroy(child.gameObject);
        }
    }

    public void CreateItem(BuildingProductionData data)
    {
        GameObject itemObj = Instantiate(itemPrefab, contentParent);
        LayoutElement layoutElement = itemObj.GetComponent<LayoutElement>();
        if (layoutElement == null)
        {
            layoutElement = itemObj.AddComponent<LayoutElement>();
        }
        layoutElement.preferredWidth = itemWidth;
        layoutElement.preferredHeight = itemHeight;
        layoutElement.flexibleHeight = 1f;
    }
}
