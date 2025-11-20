using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CocktailRecipeSO", menuName = "ScriptableObject/CocktailRecipeSO")]
public class CocktailRecipeSO : ScriptableObject
{
    public List<CocktailRecipeJson> recipes;
}