using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class CocktailRepository : MonoBehaviour, IRepository
{
    private static CocktailRepository _instance;
    public static CocktailRepository Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<CocktailRepository>();
            }
            return _instance;
        }
    }
    public bool IsInitialized { get; private set; } = false;

    [Header("데이터 에셋 (SO)")]
    [Tooltip("칵테일의 고정 정보(ID, 이름, 아이콘 등)를 담고 있는 ScriptableObject")]
    [SerializeField] private CocktailDataSO cocktailDataSO;
    [Tooltip("칵테일 레시피 정보를 담고 있는 ScriptableObject")]
    [SerializeField] private CocktailRecipeSO cocktailRecipeSO;

    private readonly Dictionary<int, CocktailData> _cocktailDataDict = new Dictionary<int, CocktailData>();
    public readonly Dictionary<int, CocktailRecipeScript> _cocktailRecipeDict = new Dictionary<int, CocktailRecipeScript>();

    // 조합 데이터
    private List<OrderedCocktail> _orderedCocktails = new List<OrderedCocktail>();

    // 레시피 해금 시스템
    private List<int> _unlockedRecipeIds = new List<int>();

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
        // DataManager가 초기화를 요청할 때까지 대기
        DataManager.Instance.RegisterRepository(this);
    }

    public void Initialize()
    {
        InitializeDictionaries();
        CreateOrderedCocktailsList();
        LoadUnlockedRecipes();
        IsInitialized = true;
        Debug.Log("CocktailRepository 초기화 완료.");
    }

    private void InitializeDictionaries()
    {
        if (cocktailDataSO != null && cocktailDataSO.cocktails != null)
        {
            _cocktailDataDict.Clear();
            foreach (var cocktail in cocktailDataSO.cocktails)
            {
                if (cocktail != null && !_cocktailDataDict.ContainsKey(cocktail.Cocktail_ID))
                {
                    _cocktailDataDict.Add(cocktail.Cocktail_ID, cocktail);
                }
            }
        }

        if (cocktailRecipeSO != null && cocktailRecipeSO.recipes != null)
        {
            _cocktailRecipeDict.Clear();
            foreach (var recipe in cocktailRecipeSO.recipes)
            {
                if (recipe != null && !_cocktailRecipeDict.ContainsKey(recipe.CocktailId))
                {
                    _cocktailRecipeDict.Add(recipe.CocktailId, recipe);
                }
            }
        }
    }

    public CocktailData GetCocktailDataById(int cocktailId)
    {
        _cocktailDataDict.TryGetValue(cocktailId, out var data);
        return data;
    }

    public CocktailRecipeScript GetCocktailRecipeByCocktailId(int cocktailId)
    {
        _cocktailRecipeDict.TryGetValue(cocktailId, out var recipe);
        return recipe;
    }

    #region 조합 데이터 생성

    /// <summary>
    /// 주문된 칵테일 리스트를 초기화
    /// 게임 시작 시 빈 리스트로 시작하며, 주문이 들어올 때마다 AddOrderedCocktail을 통해 추가
    /// </summary>
    private void CreateOrderedCocktailsList()
    {
        _orderedCocktails.Clear();
        Debug.Log("OrderedCocktails 리스트 초기화 완료.");
    }

    /// <summary>
    /// 주문 인스턴스 ID를 생성
    /// 형식: YYYYMMDDHHMMSS + 랜덤 4자리
    /// </summary>
    private long GenerateOrderInstanceId()
    {
        System.DateTime now = System.DateTime.Now;
        long timestamp = long.Parse(now.ToString("yyyyMMddHHmmss"));
        long randomPart = UnityEngine.Random.Range(1000, 9999);
        return timestamp * 10000 + randomPart;
    }

    #endregion

    #region 레시피 해금 시스템

    /// <summary>
    /// 저장된 해금 레시피 정보를 로드합니다.
    /// JSON 파일이 없으면 기본 레시피 3개를 자동으로 해금합니다.
    /// </summary>
    private void LoadUnlockedRecipes()
    {
        _unlockedRecipeIds.Clear();

        // JsonDataHandler를 통해 저장된 해금 정보 로드 (Phase 3에서 구현)
        // 현재는 기본 레시피 자동 해금
        if (_cocktailDataDict.Count > 0)
        {
            int count = 0;
            foreach (var kvp in _cocktailDataDict)
            {
                if (count >= 3) break;
                _unlockedRecipeIds.Add(kvp.Key);
                count++;
            }
            Debug.Log($"기본 레시피 {_unlockedRecipeIds.Count}개 자동 해금.");
        }
    }

    /// <summary>
    /// 레시피를 해금
    /// </summary>
    /// <param name="cocktailId">해금할 칵테일 ID</param>
    public void UnlockRecipe(int cocktailId)
    {
        if (!_unlockedRecipeIds.Contains(cocktailId))
        {
            _unlockedRecipeIds.Add(cocktailId);
            var cocktailData = GetCocktailDataById(cocktailId);
            Debug.Log($"레시피 해금: {cocktailData?.CocktailName ?? "Unknown"} (ID: {cocktailId})");
        }
        else
        {
            Debug.LogWarning($"레시피 ID {cocktailId}는 이미 해금되어 있습니다.");
        }
    }

    /// <summary>
    /// 여러 레시피를 한번에 해금
    /// </summary>
    public void UnlockRecipes(List<int> cocktailIds)
    {
        foreach (int id in cocktailIds)
        {
            UnlockRecipe(id);
        }
    }

    /// <summary>
    /// 레시피가 해금되었는지 확인
    /// </summary>
    public bool IsRecipeUnlocked(int cocktailId)
    {
        return _unlockedRecipeIds.Contains(cocktailId);
    }

    /// <summary>
    /// 해금된 레시피 중에서 랜덤으로 칵테일 ID를 반환
    /// 주문 시스템에서 사용 될 function
    /// </summary>
    public int GetRandomUnlockedCocktailId()
    {
        if (_unlockedRecipeIds.Count == 0)
        {
            Debug.LogWarning("해금된 레시피가 없습니다.");
            return -1;
        }

        int randomIndex = UnityEngine.Random.Range(0, _unlockedRecipeIds.Count);
        return _unlockedRecipeIds[randomIndex];
    }

    /// <summary>
    /// 해금된 레시피 ID 리스트를 반환
    /// </summary>
    public List<int> GetUnlockedRecipeIds()
    {
        return new List<int>(_unlockedRecipeIds);
    }

    /// <summary>
    /// 해금된 칵테일 데이터만 반환
    /// </summary>
    public List<CocktailData> GetUnlockedCocktailData()
    {
        List<CocktailData> unlockedCocktails = new List<CocktailData>();
        foreach (int recipeId in _unlockedRecipeIds)
        {
            var cocktailData = GetCocktailDataById(recipeId);
            if (cocktailData != null)
            {
                unlockedCocktails.Add(cocktailData);
            }
        }
        return unlockedCocktails;
    }

    #endregion

    #region 주문 CRUD

    /// <summary>
    /// 해당 메서드는 claude로 만들었으며, 데이터 입출력을 위해 임시 제작됨.
    /// 머지 시 이미 해당 메서드들이 만들어졌으면 삭제해도 됨.
    /// 새로운 칵테일 주문을 추가합니다.
    /// 주문 시스템에서 호출합니다.
    /// </summary>
    /// <param name="cocktailId">주문할 칵테일 ID</param>
    /// <param name="guest">주문한 손님 GameObject</param>
    /// <param name="table">손님이 앉은 테이블 GameObject</param>
    /// <returns>생성된 OrderedCocktail 객체. 실패 시 null</returns>
    public OrderedCocktail AddOrderedCocktail(int cocktailId, GameObject guest, GameObject table)
    {
        // 1. 해금된 레시피인지 확인
        if (!_unlockedRecipeIds.Contains(cocktailId))
        {
            Debug.LogWarning($"칵테일 ID '{cocktailId}'는 아직 해금되지 않았습니다.");
            return null;
        }

        // 2. 원본 데이터 조회
        CocktailData cocktailData = GetCocktailDataById(cocktailId);
        CocktailRecipeScript recipe = GetCocktailRecipeByCocktailId(cocktailId);

        if (cocktailData == null || recipe == null)
        {
            Debug.LogError($"칵테일 ID '{cocktailId}'에 대한 데이터를 찾을 수 없습니다.");
            return null;
        }

        // 3. Glass 정보 조회 (GlassRepository 사용)
        Glass glass = null;
        if (GlassRepository.Instance != null)
        {
            glass = GlassRepository.Instance.GetGlassById(cocktailData.glass_id);
            if (glass == null)
            {
                Debug.LogWarning($"Glass ID '{cocktailData.glass_id}'를 찾을 수 없습니다.");
            }
        }
        else
        {
            Debug.LogWarning("GlassRepository가 초기화되지 않았습니다.");
        }

        // 4. 주문 인스턴스 ID 생성
        long orderInstanceId = GenerateOrderInstanceId();

        // 5. OrderedCocktail 생성
        OrderedCocktail orderedCocktail = new OrderedCocktail(
            cocktailData,
            recipe,
            glass,
            guest,
            table,
            System.DateTime.Now,
            orderInstanceId
        );

        // 6. 리스트에 추가
        _orderedCocktails.Add(orderedCocktail);

        Debug.Log($"주문 추가: {cocktailData.CocktailName} (주문 ID: {orderInstanceId}, 손님: {guest?.name ?? "Unknown"})");

        return orderedCocktail;
    }

    /// <summary>
    /// 칵테일 주문을 제거합니다.
    /// </summary>
    /// <param name="orderInstanceId">주문 인스턴스 ID</param>
    public void RemoveOrderedCocktail(long orderInstanceId)
    {
        var orderedCocktail = _orderedCocktails.Find(o => o.OrderInstanceId == orderInstanceId);
        if (orderedCocktail != null)
        {
            _orderedCocktails.Remove(orderedCocktail);
            Debug.Log($"주문 제거: {orderedCocktail.CocktailName} (주문 ID: {orderInstanceId})");
        }
        else
        {
            Debug.LogWarning($"주문 ID '{orderInstanceId}'를 찾을 수 없습니다.");
        }
    }

    /// <summary>
    /// 손님의 주문을 제거합니다. (손님이 떠날 때 호출)
    /// </summary>
    public void RemoveOrderedCocktailByGuest(GameObject guest)
    {
        var orderedCocktail = GetOrderedCocktailByGuest(guest);
        if (orderedCocktail != null)
        {
            RemoveOrderedCocktail(orderedCocktail.OrderInstanceId);
        }
        else
        {
            Debug.LogWarning($"손님 '{guest?.name ?? "Unknown"}'의 주문을 찾을 수 없습니다.");
        }
    }

    /// <summary>
    /// 주문 상태를 업데이트합니다.
    /// </summary>
    /// <param name="orderInstanceId">주문 인스턴스 ID</param>
    /// <param name="newStatus">새로운 상태</param>
    public void UpdateOrderStatus(long orderInstanceId, OrderStatus newStatus)
    {
        var orderedCocktail = GetOrderedCocktailByInstanceId(orderInstanceId);
        if (orderedCocktail != null)
        {
            orderedCocktail.Status = newStatus;
            Debug.Log($"주문 상태 업데이트: {orderedCocktail.CocktailName} - {newStatus}");
        }
        else
        {
            Debug.LogWarning($"주문 ID '{orderInstanceId}'를 찾을 수 없습니다.");
        }
    }

    #endregion

    #region 조회 메서드

    /// <summary>
    /// 현재 주문된 모든 칵테일 리스트를 반환
    /// </summary>
    public List<OrderedCocktail> GetOrderedCocktails()
    {
        return _orderedCocktails;
    }

    /// <summary>
    /// 특정 손님이 주문한 칵테일을 반환
    /// </summary>
    public OrderedCocktail GetOrderedCocktailByGuest(GameObject guest)
    {
        return _orderedCocktails.Find(o => o.OrderedByGuest == guest);
    }

    /// <summary>
    /// 특정 테이블의 모든 주문을 반환
    /// </summary>
    public List<OrderedCocktail> GetOrderedCocktailsByTable(GameObject table)
    {
        return _orderedCocktails.FindAll(o => o.AssignedTable == table);
    }

    /// <summary>
    /// 주문 인스턴스 ID로 주문을 조회
    /// </summary>
    public OrderedCocktail GetOrderedCocktailByInstanceId(long orderInstanceId)
    {
        return _orderedCocktails.Find(o => o.OrderInstanceId == orderInstanceId);
    }

    /// <summary>
    /// 특정 상태의 주문들을 반환합
    /// </summary>
    public List<OrderedCocktail> GetOrderedCocktailsByStatus(OrderStatus status)
    {
        return _orderedCocktails.FindAll(o => o.Status == status);
    }

    /// <summary>
    /// 모든 CocktailData 리스트를 반환 (UI 생성용)
    /// </summary>
    public List<CocktailData> GetAllCocktailData()
    {
        if (cocktailDataSO != null && cocktailDataSO.cocktails != null)
        {
            return cocktailDataSO.cocktails;
        }
        return new List<CocktailData>();
    }

    #endregion
}
