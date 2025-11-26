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
    public int Ingridiant_id { get; private set; }
    public string Ingridiant_name { get; private set; }
    public string Ingridiant_type { get; private set; }
    public string Description { get; private set; }
    public int? Volume { get; set; } // type == Alchol,Drink -> ml(15ml,30ml..) / type == Ice, Garnish -> count(1,2,3..)
    public Sprite Icon { get; private set; }
}