using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class ResourceRepository : MonoBehaviour, IRepository
{
    private static ResourceRepository _instance;
    public static ResourceRepository Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<ResourceRepository>();
            }
            return _instance;
        }
    }
    public bool IsInitialized { get; private set; } = false;

    private const string GoodsPath = "Data/Resource";
    [SerializeField] private List<ResourceData> _resourceDatas = new List<ResourceData>();
    private Dictionary<string, ResourceData> _resourceDataByName = new Dictionary<string, ResourceData>();
    private Dictionary<int, ResourceData> _resourceDataById = new Dictionary<int, ResourceData>();

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // DataManager가 초기화를 요청할 때까지 대기합니다.
        DataManager.Instance.RegisterRepository(this);
    }

    public void Initialize()
    {
        LoadGoodsData();
        InitializeDictionaries();
        IsInitialized = true;
        Debug.Log("ResourceRepository 초기화 완료.");
    }

    private void LoadGoodsData()
    {
        ResourceData[] loadedGoods = Resources.LoadAll<ResourceData>(GoodsPath);
        _resourceDatas = new List<ResourceData>(loadedGoods);
    }

    private void InitializeDictionaries()
    {
        _resourceDataByName.Clear();
        _resourceDataById.Clear();
        foreach (var resource in _resourceDatas)
        {
            if (!_resourceDataByName.ContainsKey(resource.resource_name))
            {
                _resourceDataByName.Add(resource.resource_name, resource);
            }
            if (!_resourceDataById.ContainsKey(resource.resource_id))
            {
                _resourceDataById.Add(resource.resource_id, resource);
            }
        }
    }

    public ResourceData GetResourceByName(string name)
    {
        _resourceDataByName.TryGetValue(name, out var resource);
        return resource;
    }

    public ResourceData GetResourceById(int id)
    {
        _resourceDataById.TryGetValue(id, out var resource);
        return resource;
    }
}