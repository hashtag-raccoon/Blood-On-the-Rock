using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 칵테일에 사용되는 잔
/// </summary>
[Serializable]
[CreateAssetMenu(fileName = "Glass", menuName = "Cocktail/Glass", order = 1)]
public class Glass : ScriptableObject
{
    public int Glass_id { get; private set; }
    public string Glass_name { get; private set; }
    public string Description { get; private set; }
    public Sprite Icon { get; private set; }
}
