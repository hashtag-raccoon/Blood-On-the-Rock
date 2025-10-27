using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class RecipeIngredient
{
    public IngredientData ingredient;
    public float amount; // 재료의 양 (예: ml, g 등)
}

[CreateAssetMenu(fileName = "New Recipe", menuName = "Cocktail/Recipe")]
public class CocktailScriptable : ScriptableObject
{
    [SerializeField] private string cocktailName; // 칵테일 이름
    [SerializeField] private Sprite cocktailImage; // 칵테일 이미지
    [SerializeField] private List<RecipeIngredient> bases; // 칵테일 재료 목록(기주와 믹서)
    [SerializeField] private List<RecipeIngredient> mixers; // 칵테일 재료 목록(기주와 믹서)
    [SerializeField] private string description; // 칵테일 설명
}