using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CocktailSystem : MonoBehaviour
{
    private DataManager _dataManager;

    private CocktailData cocktailData;
    private CocktailRecipeJson recipeData;
    private Dictionary<int, int> Ingridiants = new Dictionary<int, int>();

    public void Awake()
    {
    }


    private void Start()
    {
        // DataManager 인스턴스를 가져옵니다.
        _dataManager = DataManager.Instance;
    }
    #region CheckSum
    public void CheckCocktailToRecipe()
    {
        float Percent = 0.0f;
        cocktailData = _dataManager.GetCocktailDataById(1);
        recipeData = _dataManager.GetCocktailRecipeByCocktailId(1);
    }
    #endregion

}