using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEditor.ShaderKeywordFilter;

[Serializable]
public class ArbeitData
{
    public int part_timer_id;
    public string prefab_name;
    public string part_timer_name;
    public string race;
    public int personality_id;
    public int level;
    public int exp;
    public DateTime hire_date;
    public bool employment_state;
    public int serving_ability;
    public int cooking_ability;
    public int cleaning_ability;
    public int total_ability;
    public int fatigue;
    public int daily_wage;
    public bool need_rest;
}

[Serializable]
public class npc : IScrollItemData
{
    public int part_timer_id { get; private set; }
    public string part_timer_name { get; private set; }
    public string race { get; private set; }
    public int level { get; set; }
    public int exp { get; set; }
    public bool employment_state { get; set; }
    public int fatigue { get; set; }
    public int daily_wage { get; private set; }
    public bool need_rest { get; set; }
    public int total_ability { get; private set; }
    public string prefab_name { get; private set; }

    public int personality_id { get; private set; }
    public string personality_name { get; private set; }
    public string description { get; private set; }
    public string specificity { get; private set; }
    public int serving_ability { get; private set; }
    public int cooking_ability { get; private set; }
    public int cleaning_ability { get; private set; }

    public npc(ArbeitData arbeitData, Personality personality)
    {
        part_timer_id = arbeitData.part_timer_id;
        part_timer_name = arbeitData.part_timer_name;
        race = arbeitData.race;
        level = arbeitData.level;
        exp = arbeitData.exp;
        employment_state = arbeitData.employment_state; // 추가
        daily_wage = arbeitData.daily_wage;
        need_rest = false;
        prefab_name = arbeitData.prefab_name;

        personality_id = personality.personality_id;
        personality_name = personality.personality_name;
        description = personality.description;
        specificity = personality.specificity;
        serving_ability = personality.serving_ability;
        cooking_ability = personality.cooking_ability;
        cleaning_ability = personality.cleaning_ability;

        total_ability = serving_ability + cooking_ability + cleaning_ability;
    }

    /// <summary>
    /// prefab_name에서 종족과 ID를 추출하여 비교합니다.
    /// prefab_name 형식: "[종족]_[npc_id]" (예: "Human_1", "Oak_2")
    /// </summary>
    public bool MatchesPrefabName(string targetPrefabName)
    {
        if (string.IsNullOrEmpty(targetPrefabName)) return false;

        string[] parts = targetPrefabName.Split('_');
        if (parts.Length != 2) return false;

        string prefabRace = parts[0];
        if (!int.TryParse(parts[1], out int prefabId)) return false;

        // 종족(race)과 part_timer_id를 비교
        return race.Equals(prefabRace, StringComparison.OrdinalIgnoreCase) && part_timer_id == prefabId;
    }
}