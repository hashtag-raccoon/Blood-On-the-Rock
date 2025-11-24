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
    private Dictionary<int, Ingridiant> Ingridiants = new Dictionary<int, Ingridiant>(); // 제작 시 user가 선택한 재료

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
        int order_id = 1;
        cocktailData = CocktailRepository.Instance.GetCocktailDataById(order_id);
        recipeData = CocktailRepository.Instance.GetCocktailRecipeByCocktailId(1);
        foreach (var item in recipeData.Recipedict)
        {
            if (Ingridiants.ContainsKey(item.Key))
            {

            }
        }

    }
    #endregion
}