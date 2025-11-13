using System;
using UnityEngine;
using System.Collections.Generic;


[CreateAssetMenu(fileName = "Building_Upgrade", menuName = "Building/Building_UpgradeData")]
public class BuildingUpgradeData : ScriptableObject
{
    public string building_type;
    public int level;
    public int upgrade_price;
    public float base_upgrade_time_minutes;
    public List<upgrade_requirements> requirements = new List<upgrade_requirements>();
}

[System.Serializable]
public class upgrade_requirements // 요구 조건
{
    public string requirement_type; // 소비재화 이름
    public int requirement_value; // 소비재화 량
}