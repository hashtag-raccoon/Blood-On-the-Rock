using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEditor.ShaderKeywordFilter;

[CreateAssetMenu(fileName = "Arbeit", menuName = "Arbeit/Personality")]
[Serializable]
public class Personality : ScriptableObject
{
    public int personality_id;
    public string personality_name;
    public string description;
    public string specificity;
    public int serving_ability;
    public int cooking_ability;
    public int cleaning_ability;
}