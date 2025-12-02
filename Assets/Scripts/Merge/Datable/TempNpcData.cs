using System;
using UnityEngine;

/// <summary>
/// 구인소 후보용 임시 알바생 데이터 (저장 없이 런타임에만 사용)
/// </summary>
[Serializable]
public class TempNpcData : IScrollItemData
{
    public int temp_id; // 임시 Arbeit ID
    public string part_timer_name;
    public string race; // 종족
    public int personality_id; // -1이면 성격 없음
    public string personality_name; // 성격 이름

    // 기본 능력치 (1~3)
    public int base_serving_ability;
    public int base_cooking_ability;
    public int base_cleaning_ability;

    // 성격 보너스 (personality_id가 있을 때만)
    public int personality_serving_bonus;
    public int personality_cooking_bonus;
    public int personality_cleaning_bonus;

    public int estimated_daily_wage; // 예상 일당
    public bool is_hired; // 고용 여부

    // UI 표시용 최종 능력치 (최대 5)
    public int FinalServingAbility => Mathf.Min(base_serving_ability + personality_serving_bonus, 5);
    public int FinalCookingAbility => Mathf.Min(base_cooking_ability + personality_cooking_bonus, 5);
    public int FinalCleaningAbility => Mathf.Min(base_cleaning_ability + personality_cleaning_bonus, 5);

    public TempNpcData() // 기본 생성자
    {
        temp_id = 0;
        part_timer_name = string.Empty;
        race = string.Empty;
        personality_id = -1;
        personality_name = "없음";
        base_serving_ability = 1;
        base_cooking_ability = 1;
        base_cleaning_ability = 1;
        personality_serving_bonus = 0;
        personality_cooking_bonus = 0;
        personality_cleaning_bonus = 0;
        estimated_daily_wage = 0;
        is_hired = false;
    }

    public TempNpcData(int tempId, string name, string raceType) // 매개변수 생성자, 왠만하면 해당 생성자로 생성 부탁!
    {
        temp_id = tempId;
        part_timer_name = name;
        race = raceType;
        personality_id = -1;
        personality_name = "없음";
        base_serving_ability = 1;
        base_cooking_ability = 1;
        base_cleaning_ability = 1;
        personality_serving_bonus = 0;
        personality_cooking_bonus = 0;
        personality_cleaning_bonus = 0;
        estimated_daily_wage = 0;
        is_hired = false;
    }
}
