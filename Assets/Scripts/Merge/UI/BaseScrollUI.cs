using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public interface IScrollItemData
{

}

public interface IScrollItemUI
{
    void SetData<T>(T data, System.Action<IScrollItemUI> onClickCallback) where T : IScrollItemData;
    T GetData<T>() where T : IScrollItemData;
}

public abstract class BaseScrollUI<TData, TItemUI> : MonoBehaviour
    where TData : IScrollItemData
    where TItemUI : MonoBehaviour, IScrollItemUI
{
    [Header("스크롤 세팅")]
    [SerializeField] protected GameObject scrollUI;
    [SerializeField] protected ScrollRect scrollRect;
    [SerializeField] protected Transform content;
    [SerializeField] protected GameObject itemPrefab;

    [Header("레이아웃 설정")]
    [SerializeField] protected float itemWidth = 100f;
    [SerializeField] protected float itemHeight = 100f;
    [SerializeField] protected float spacing = 10f;
    [SerializeField] protected int paddingLeft = 10;
    [SerializeField] protected int paddingRight = 10;
    [SerializeField] protected int paddingTop = 10;
    [SerializeField] protected int paddingBottom = 10;

    [Header("버튼 할당")]
    [SerializeField] protected Button openButton;
    [SerializeField] protected Button closeButton;

    protected List<TItemUI> itemUIList = new List<TItemUI>();
    protected RectOffset padding;

    protected virtual void Awake()
    {
        InitializeButtons();
        InitializeLayout();
        SetupScrollView();
        closeButton.transform.SetAsLastSibling();
    }

    protected virtual void InitializeButtons()
    {
        if (openButton != null)
        { openButton.onClick.AddListener(OnOpenButtonClicked); }

        if (closeButton != null)
        { closeButton.onClick.AddListener(OnCloseButtonClicked); }
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
        HorizontalLayoutGroup layoutGroup = content.GetComponent<HorizontalLayoutGroup>();
        if (layoutGroup == null)
        { layoutGroup = content.gameObject.AddComponent<HorizontalLayoutGroup>(); }

        layoutGroup.spacing = spacing;
        layoutGroup.padding = padding;
        layoutGroup.childAlignment = TextAnchor.MiddleLeft;
        layoutGroup.childControlWidth = true;
        layoutGroup.childControlHeight = true;
        layoutGroup.childForceExpandWidth = false;
        layoutGroup.childForceExpandHeight = true;
    }

    protected virtual void SetupContentSizeFitter()
    {
        ContentSizeFitter fitter = content.GetComponent<ContentSizeFitter>();
        if (fitter == null)
        { fitter = content.gameObject.AddComponent<ContentSizeFitter>(); }

        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    public virtual void GenerateItems(List<TData> dataList)
    {
        ClearItems();
        foreach (var data in dataList)
        {
            CreateItem(data);
        }
    }

    public virtual void AddItem(TData data)
    {
        CreateItem(data);
    }

    protected virtual void CreateItem(TData data)
    {
        GameObject itemObj = Instantiate(itemPrefab, content);
        itemObj.transform.SetSiblingIndex(0);
        LayoutElement layoutElement = itemObj.GetComponent<LayoutElement>();
        if (layoutElement == null)
        { layoutElement = itemObj.AddComponent<LayoutElement>(); }

        layoutElement.preferredWidth = itemWidth;
        layoutElement.preferredHeight = itemHeight;
        layoutElement.flexibleHeight = 1f;

        TItemUI itemUI = itemObj.GetComponent<TItemUI>();
        if (itemUI != null)
        {
            itemUI.SetData(data, OnItemClicked);
            itemUIList.Add(itemUI);
        }
    }

    public virtual void ClearItems()
    {
        foreach (Transform child in content)
        {
            Destroy(child.gameObject);
        }
        itemUIList.Clear();
    }

    public virtual void RefreshItems(List<TData> dataList)
    {
        GenerateItems(dataList);
    }

    protected virtual void OnItemClicked(IScrollItemUI clickedItem)
    {
        // 오버라이드할 것
    }

    protected virtual void OnOpenButtonClicked()
    {
        if (scrollUI != null)
        {
            scrollUI.SetActive(true);
        }
    }

    protected virtual void OnCloseButtonClicked()
    {
        if (scrollUI != null)
        {
            scrollUI.SetActive(false);
        }
    }
}