using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEditor.ShaderKeywordFilter;

[Serializable]
public class ArbeitData
{
    public int part_timer_id;
    public string part_timer_name;
    public string race;
    public int personality_id;
    public int level;
    public int exp;
    public DateTime hire_date;
    public DateTime employeement_stae;
    public int serving_ability;
    public int cooking_ability;
    public int cleaning_ability;
    public int tatal_ability;
    public int faigue;
    public int daily_wage;
    public bool need_rest;
}

[Serializable]
public class Personality
{
    public int personality_id;
    public string personality_name;
    public string destination;
    public string specificity;
    public int serving_ability;
    public int cooking_ability;
    public int cleaning_ability;
}

[Serializable]
public class npc
{

}