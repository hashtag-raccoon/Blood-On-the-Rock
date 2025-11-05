using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[CreateAssetMenu(fileName = "Building", menuName = "Building")]
public class BuildingData : ScriptableObject, IScrollItemData
{
    // BUILDING 테이블에 해당하는 데이터
    public int building_id; // 건물 ID(PK)
    public string Building_Name; // 건물명
    public string building_Type; // 건물타입
    public int island_id; // 외부 섬 or 내부 섬 ID(FK)
    public int level; // 레벨(1-10)
    public int construction_cost_gold; // 건설비용(골드)
    public int construction_cost_wood; // 건설비용(목재)
    public float construction_time_minutes; // 건설시간(분)

    // ScriptableObject에서만 사용하는 데이터
    public Sprite icon; // icon sprite
}

/// <summary>
/// BUILDING_PRODUCTION_INFO 테이블: 건물 타입별 생산 정보를 정의하는 클래스
/// </summary>
[Serializable]
public class BuildingProductionInfo
{
    public string building_type; // 건물 타입(PK)
    public int resource_id; // 생산자원 id(FK)
    public int output_amount; // 생산량
    public float base_production_time_minutes; // 기본 생산시간(분)
}

/// <summary>
/// CONSTRUCTED_BUILDING_PRODUCTION 테이블: 건설된 건물의 생산 현황을 관리하는 클래스
/// </summary>
[Serializable]
public class ConstructedBuildingProduction
{
    public int building_id; // 건물 ID(PK, FK)
    public DateTime last_production_time; // 마지막 생산 시간
    public DateTime next_production_time; // 다음 생산 완료 시간
    public bool is_producing; // 생산 중 여부
}

/// <summary>
/// 게임 내에 실제로 건설된 건물의 모든 정보를 통합하여 관리하는 클래스입니다.
/// </summary>
public class ConstructedBuilding
{
    // BuildingData에서 가져온 정보
    public int Id { get; private set; }
    public string Name { get; private set; }
    public string Type { get; private set; }
    public int Level { get; private set; }
    public Sprite Icon { get; private set; }

    // BuildingProductionInfo에서 가져온 정보
    public int ProductionResourceId { get; private set; }
    public int ProductionOutputAmount { get; private set; }
    public float BaseProductionTimeMinutes { get; private set; }

    // ConstructedBuildingProduction에서 가져온 정보
    public DateTime LastProductionTime { get; set; }
    public DateTime NextProductionTime { get; set; }
    public bool IsProducing { get; set; }

    // 생성자: 여러 데이터 소스를 조합하여 하나의 완전한 객체를 생성.
    public ConstructedBuilding(BuildingData buildingData, BuildingProductionInfo productionInfo, ConstructedBuildingProduction productionStatus)
    {
        // 기본 정보
        Id = buildingData.building_id;
        Name = buildingData.Building_Name;
        Type = buildingData.building_Type;
        Level = buildingData.level;
        Icon = buildingData.icon;

        // 생산 정의 정보 (생산 건물이 아닌 경우 null일 수 있음)
        if (productionInfo != null)
        {
            ProductionResourceId = productionInfo.resource_id;
            ProductionOutputAmount = productionInfo.output_amount;
            BaseProductionTimeMinutes = productionInfo.base_production_time_minutes;
        }

        // 실시간 생산 상태 정보 (생산 건물이 아닌 경우 null일 수 있음)
        if (productionStatus != null)
        {
            LastProductionTime = productionStatus.last_production_time;
            NextProductionTime = productionStatus.next_production_time;
            IsProducing = productionStatus.is_producing;
        }
    }
}