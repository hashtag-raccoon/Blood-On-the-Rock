using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 레시피에 들어가는 재료
/// </summary>
/// <remarks>
/// Volume: type == Alchol,Drink -> ml(15ml,30ml..) / type == Ice, Garnish -> count(1,2,3..)
/// </remarks>
[Serializable]
[CreateAssetMenu(fileName = "Ingridiant", menuName = "Cocktail/Ingridiants", order = 0)]
public class Ingridiant : ScriptableObject
{
    public int Ingridiant_id;
    public string Ingridiant_name;
    public string Ingridiant_type;
    public string Description;
    public int? Volume; // type == Alchol,Drink -> ml(15ml,30ml..) / type == Ice, Garnish -> count(1,2,3..)
    public Sprite Icon;
}