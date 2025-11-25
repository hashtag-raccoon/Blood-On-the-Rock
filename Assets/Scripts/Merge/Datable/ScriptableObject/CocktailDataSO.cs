using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CocktailDataSO", menuName = "ScriptableObject/CocktailDataSO")]
public class CocktailDataSO : ScriptableObject
{
    public List<CocktailData> cocktails;
}