using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 모든 BuildingProductionInfo를 관리하는 ScriptableObject
/// </summary>
[CreateAssetMenu(fileName = "BuildingProductionInfoSO", menuName = "Game Data/Building Production Info SO")]
public class BuildingProductionInfoSO : ScriptableObject
{
    [Header("건물 생산 정보 목록")]
    public List<BuildingProductionInfo> productionInfos = new List<BuildingProductionInfo>();

    /// <summary>
    /// 건물 타입으로 생산 정보를 찾습니다.
    /// </summary>
    public BuildingProductionInfo GetProductionInfoByBuildingType(string buildingType)
    {
        return productionInfos.Find(p => p.building_type == buildingType);
    }

    /// <summary>
    /// 건물 타입으로 생산 정보 목록을 찾습니다.
    /// </summary>
    public List<BuildingProductionInfo> GetProductionInfosByBuildingType(string buildingType)
    {
        return productionInfos.FindAll(p => p.building_type == buildingType);
    }

    /// <summary>
    /// 생산 아이템 ID로 생산 정보 목록을 찾습니다.
    /// </summary>
    public List<BuildingProductionInfo> GetProductionInfosByResourceId(int resourceId)
    {
        return productionInfos.FindAll(p => p.resource_id == resourceId);
    }
}
