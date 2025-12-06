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
    private List<int> _ownedGlassIds = new List<int>();

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

    /// <summary>
    /// IRepository 인터페이스 구현 - 기본 초기화 (빈 소유 목록)
    /// </summary>
    public void Initialize()
    {
        Initialize(new List<int>());
    }

    /// <summary>
    /// GlassRepository 초기화 (DataManager로부터 소유 잔 데이터 전달받음)
    /// </summary>
    /// <param name="ownedGlassIds">DataManager가 JSON에서 로드한 소유 잔 ID 목록</param>
    public void Initialize(List<int> ownedGlassIds)
    {
        if (glassSO == null || glassSO.glasses == null)
        {
            Debug.LogError("GlassSO가 할당되지 않았습니다.");
            IsInitialized = true;
            return;
        }

        InitializeDictionaries();

        // DataManager로부터 전달받은 소유 잔 데이터 사용
        _ownedGlassIds.Clear();
        if (ownedGlassIds != null && ownedGlassIds.Count > 0)
        {
            _ownedGlassIds.AddRange(ownedGlassIds);
            Debug.Log($"저장된 잔 {_ownedGlassIds.Count}개를 로드했습니다.");
        }
        else
        {
            // JSON이 없거나 비어있으면 기본 잔 3개 자동 소유
            LoadDefaultGlasses();
        }

        IsInitialized = true;
        Debug.Log($"GlassRepository 초기화 완료. 총 {_glassDict.Count}개 잔 등록, {_ownedGlassIds.Count}개 소유.");
    }

    /// <summary>
    /// 기본 잔 3개를 자동으로 소유합니다.
    /// </summary>
    private void LoadDefaultGlasses()
    {
        if (_glassDict.Count > 0)
        {
            int count = 0;
            foreach (var kvp in _glassDict)
            {
                if (count >= 3) break;
                _ownedGlassIds.Add(kvp.Key);
                count++;
            }
            Debug.Log($"기본 잔 {_ownedGlassIds.Count}개 자동 소유.");
        }
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

    /// <summary>
    /// 잔을 획득합니다.
    /// </summary>
    /// <param name="glassId">획득할 잔 ID</param>
    public void OwnGlass(int glassId)
    {
        if (!_ownedGlassIds.Contains(glassId))
        {
            _ownedGlassIds.Add(glassId);
            var glass = GetGlassById(glassId);
            Debug.Log($"잔 획득: {glass?.Glass_name ?? "Unknown"} (ID: {glassId})");
        }
        else
        {
            Debug.LogWarning($"잔 ID {glassId}는 이미 소유하고 있습니다.");
        }
    }

    /// <summary>
    /// 여러 잔을 한 번에 획득합니다.
    /// </summary>
    /// <param name="glassIds">획득할 잔 ID 목록</param>
    public void OwnGlasses(List<int> glassIds)
    {
        foreach (int id in glassIds)
        {
            OwnGlass(id);
        }
    }

    /// <summary>
    /// 잔을 소유하고 있는지 확인합니다.
    /// </summary>
    /// <param name="glassId">확인할 잔 ID</param>
    /// <returns>소유 여부</returns>
    public bool IsGlassOwned(int glassId)
    {
        return _ownedGlassIds.Contains(glassId);
    }

    /// <summary>
    /// 소유한 잔 ID 목록을 반환합니다.
    /// </summary>
    /// <returns>소유 잔 ID 목록</returns>
    public List<int> GetOwnedGlassIds()
    {
        return new List<int>(_ownedGlassIds);
    }

    /// <summary>
    /// 소유한 잔 객체 목록을 반환합니다.
    /// </summary>
    /// <returns>소유 잔 Glass 객체 목록</returns>
    public List<Glass> GetOwnedGlasses()
    {
        List<Glass> ownedGlasses = new List<Glass>();
        foreach (int glassId in _ownedGlassIds)
        {
            var glass = GetGlassById(glassId);
            if (glass != null)
            {
                ownedGlasses.Add(glass);
            }
        }
        return ownedGlasses;
    }
}
