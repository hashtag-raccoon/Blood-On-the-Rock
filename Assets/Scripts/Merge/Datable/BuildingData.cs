using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEditor.ShaderKeywordFilter;
using UnityEditor;

public enum CameraPositionOffset
{
    Left,
    Center,
    Right
}

public enum BuildingType
{
    None,
    Production,
    Decoration,
    Utility
}

[CreateAssetMenu(fileName = "Building", menuName = "Building/BuildingData", order = 0)]
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
    public Sprite building_sprite; // 건물 스프라이트 (배치용)
    public Vector2Int tileSize; // 건물 크기 (타일 단위, 기본값 1x1)
    public BuildingType buildingType; // 건물의 타입 ex)생성형,유틸형(대형 시계탑 등)
    public CameraPositionOffset cameraPositionOffset; // 건물 클릭 시 카메라 오프셋 위치
    public float MarkerPositionOffset; // 건물 배치 시 프리뷰 마커 오프셋 높이
    public float CameraOrthographicSize; // 건물 클릭 시 카메라 줌 크기(ZoomIn Size)
}

/// <summary>
/// 생산 슬롯의 저장 데이터
/// </summary>
[Serializable]
public class ProductionSlotData
{
    public int slot_index;              // 슬롯 인덱스 (0-3)
    public int resource_id;             // 생산 중인 자원 ID
    public string building_type;        // 건물 타입 (BuildingProductionInfo 조회용)
    public float time_remaining;        // 남은 시간 (초)
    public float total_production_time; // 전체 생산 시간 (초)
}

/// <summary>
/// CONSTRUCTED_BUILDING_PRODUCTION 테이블: 건설된 건물의 생산 현황을 관리하는 클래스
/// </summary>
[Serializable]
public class ConstructedBuildingProduction
{
    public long instance_id; // 건물 인스턴스 ID(PK, FK)
    public DateTime last_production_time; // 마지막 생산 시간
    public DateTime next_production_time; // 다음 생산 완료 시간
    public bool is_producing; // 생산 중 여부
    public List<ProductionSlotData> production_slots; // 생산 슬롯 정보 (최대 4개)

    // 기본 생성자 (기존 JSON과의 호환성을 위해 production_slots를 빈 리스트로 초기화)
    public ConstructedBuildingProduction()
    {
        production_slots = new List<ProductionSlotData>();
    }
}

[Serializable]
public class ConstructedBuildingPos
{
    public long instance_id; // 건물 인스턴스 ID
    public Vector3Int pos;
    public float rotation;
}


/// <summary>
/// 게임 내에 실제로 건설된 건물의 모든 정보를 통합하여 관리하는 클래스입니다.
/// </summary>
[Serializable]
public class ConstructedBuilding : IScrollItemData
{
    // BuildingData에서 가져온 정보
    public int Id { get; private set; } // 건물 타입 ID (BuildingData의 building_id)
    public long InstanceId { get; private set; } // 건물 인스턴스 ID (고유 식별자)
    public string Name { get; private set; }
    public string Type { get; private set; }
    public int Level { get; set; }
    public Sprite Icon { get; private set; }

    // BuildingProductionInfo에서 가져온 정보
    public int ProductionResourceId { get; private set; }
    public int ProductionOutputAmount { get; private set; }
    public float BaseProductionTimeMinutes { get; private set; }
    public int ConsumeAmount { get; private set; } // save & load 해야함
    public string ConsumeResourceType { get; private set; } // save & load 해야함

    // ConstructedBuildingProduction에서 가져온 정보
    public DateTime LastProductionTime { get; set; }
    public DateTime NextProductionTime { get; set; }
    public bool IsProducing { get; set; }

    // constructedBuilding 에서만
    public Vector3Int Position { get; set; }
    public float Rotation { get; set; }
    public bool IsEditInventory { get; set; } = false; // 인벤토리에 있는지 여부
    // 생성자: 여러 데이터 소스를 조합하여 하나의 완전한 객체를 생성.
    public ConstructedBuilding(BuildingData buildingData, BuildingProductionInfo productionInfo, ConstructedBuildingProduction productionStatus, ConstructedBuildingPos constructedBuildingPos)
    {
        // 기본 정보
        Id = buildingData.building_id; // 건물 타입 ID
        InstanceId = productionStatus.instance_id; // 건물 인스턴스 ID (고유 식별자)
        Name = buildingData.Building_Name;
        Type = buildingData.building_Type;
        Level = buildingData.level;
        Icon = buildingData.icon;
        Position = constructedBuildingPos.pos;
        Rotation = constructedBuildingPos.rotation;

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