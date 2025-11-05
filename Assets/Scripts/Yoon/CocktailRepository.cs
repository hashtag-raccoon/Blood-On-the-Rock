using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.PlayerLoop;

public class CocktailRepository : MonoBehaviour
{
    #region Repository_element
    private static CocktailRepository _instance;
    public static CocktailRepository Instance => _instance;

    [SerializeField] private List<CocktailData> allCocktails;
    private Dictionary<string, CocktailData> _cocktailDict;
    #endregion

    #region Repository_function
    private void Awake()
    {
        _instance = this;
        InitializeDictionary();
    }

    private void InitializeDictionary()
    {
        _cocktailDict = new Dictionary<string, CocktailData>();
    }

    public CocktailData GetCocktail(string name)
    {
        return _cocktailDict.TryGetValue(name, out var cocktail) ? cocktail : null;
    }
    // [MermaidChart: b885a2ee-ae43-4965-bbcc-e08cc467610b]
    // [MermaidChart: b885a2ee-ae43-4965-bbcc-e08cc467610b]
    // [MermaidChart: b885a2ee-ae43-4965-bbcc-e08cc467610b]
    // [MermaidChart: b885a2ee-ae43-4965-bbcc-e08cc467610b]
    #endregion
}
