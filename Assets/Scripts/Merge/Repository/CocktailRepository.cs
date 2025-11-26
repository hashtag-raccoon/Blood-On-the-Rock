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
    private readonly Dictionary<int, CocktailRecipeScript> _cocktailRecipeDict = new Dictionary<int, CocktailRecipeScript>();

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

    public int GetTotalCocktailCount()
    {
        return _cocktailDataDict.Count;
    }

    public CocktailRecipeScript GetCocktailRecipeByCocktailId(int cocktailId)
    {
        _cocktailRecipeDict.TryGetValue(cocktailId, out var recipe);
        return recipe;
    }

    // 당분간 임시로 쓸 메소드, 전체 레시피를 리스트로 반환
    public List<CocktailRecipeScript> GetAllCocktailRecipe()
    {
        return cocktailRecipeSO.recipes.ToList();
    }
}
