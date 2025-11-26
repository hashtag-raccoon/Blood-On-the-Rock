using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum IngredientType
{
    Base,      // ±‚¡÷
    Mixer     // πÕº≠
}

[CreateAssetMenu(fileName = "New Ingredient", menuName = "Cocktail/Ingredient")]
public class IngredientData : ScriptableObject
{
    public string ingredientName;
    public IngredientType type;
    public Sprite icon;
}