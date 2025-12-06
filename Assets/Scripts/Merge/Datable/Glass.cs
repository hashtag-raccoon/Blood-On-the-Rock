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
    [SerializeField] public int Glass_id;
    [SerializeField] public string Glass_name;
    [SerializeField] public string Description;
    [SerializeField] public Sprite Icon;
}
