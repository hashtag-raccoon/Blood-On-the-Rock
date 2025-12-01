using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class GlassRepository : MonoBehaviour, IRepository
{
    private static GlassRepository _instance;
    public static GlassRepository Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<GlassRepository>();
            }
            return _instance;
        }
    }
    public bool IsInitialized { get; private set; } = false;

    [Header("데이터 에셋 (SO)")]
    [Tooltip("칵테일 잔의 고정 정보(ID, 이름, 아이콘 등)를 담고 있는 ScriptableObject")]
    [SerializeField] private GlassSO glassSO;

    private readonly Dictionary<int, Glass> _glassDict = new Dictionary<int, Glass>();

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
        InitializeDictionaries();
        IsInitialized = true;
        Debug.Log("GlassRepository 초기화 완료.");
    }

    private void InitializeDictionaries()
    {
        if (glassSO != null && glassSO.glasses != null)
        {
            _glassDict.Clear();
            foreach (var glass in glassSO.glasses)
            {
                if (glass != null && !_glassDict.ContainsKey(glass.Glass_id))
                {
                    _glassDict.Add(glass.Glass_id, glass);
                }
                else if (glass != null)
                {
                    Debug.LogWarning($"[Data Duplication] Glass ID '{glass.Glass_id}'가 중복됩니다. 에셋: '{glass.name}'");
                }
            }
            Debug.Log($"Glass Dictionary 초기화 완료: {_glassDict.Count}개");
        }
    }

    /// <summary>
    /// ID를 사용하여 특정 Glass를 가져옵니다.
    /// </summary>
    /// <param name="glassId">가져올 잔의 ID</param>
    /// <returns>찾은 Glass. 없으면 null을 반환합니다.</returns>
    public Glass GetGlassById(int glassId)
    {
        _glassDict.TryGetValue(glassId, out var glass);
        if (glass == null) Debug.LogWarning($"Glass ID '{glassId}'를 찾을 수 없습니다.");
        return glass;
    }

    /// <summary>
    /// 모든 Glass 리스트를 반환합니다.
    /// </summary>
    /// <returns>모든 Glass의 리스트</returns>
    public List<Glass> GetAllGlasses()
    {
        if (glassSO != null && glassSO.glasses != null)
        {
            return glassSO.glasses;
        }
        return new List<Glass>();
    }
}
