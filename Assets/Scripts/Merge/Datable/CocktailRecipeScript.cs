using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 칵테일 레시피 데이터
/// </summary>

[Serializable]
[CreateAssetMenu(fileName = "CocktailRecipe", menuName = "Cocktail/CocktailRecipeScript")]
public class CocktailRecipeScript : ScriptableObject
{
    public int RecipeId;
    public int CocktailId;
    public int Technique; // (0/1/2) : (build, floating, shaking)
    public string CocktailName;
    public string Description;
    public Dictionary<int, Ingridiant> Recipedict = new Dictionary<int, Ingridiant>();
    public string RecipeOrder; // 당분간은 건드리지 말 것(허동윤 피셜)
    
    public bool isUnlock = false; // 레시피 잠금 해제 여부, 당분간 임시로 쓸 예정(주문에 씀)
}