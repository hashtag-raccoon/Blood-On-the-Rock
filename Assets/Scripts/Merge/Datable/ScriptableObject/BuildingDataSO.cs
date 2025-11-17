using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 모든 BuildingData를 관리하는 ScriptableObject
/// </summary>
[CreateAssetMenu(fileName = "BuildingDataSO", menuName = "Game Data/Building Data SO")]
public class BuildingDataSO : ScriptableObject
{
    [Header("건물 데이터 목록")]
    public List<BuildingData> buildings = new List<BuildingData>();

    /// <summary>
    /// ID로 건물 데이터를 찾습니다.
    /// </summary>
    public BuildingData GetBuildingById(int id)
    {
        return buildings.Find(b => b.building_id == id);
    }

    /// <summary>
    /// 이름으로 건물 데이터를 찾습니다.
    /// </summary>
    public BuildingData GetBuildingByName(string name)
    {
        return buildings.Find(b => b.Building_Name == name);
    }

    /// <summary>
    /// 타입으로 건물 데이터 목록을 찾습니다.
    /// </summary>
    public List<BuildingData> GetBuildingsByType(string type)
    {
        return buildings.FindAll(b => b.building_Type == type);
    }
}
