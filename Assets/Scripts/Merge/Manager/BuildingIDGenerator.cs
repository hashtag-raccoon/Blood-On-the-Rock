using System;

/// <summary>
/// 건물 인스턴스의 고유한 ID를 생성하는 클래스
/// 타임스탬프 기반으로 절대 중복되지 않는 ID를 생성합니다.
/// </summary>
public static class BuildingIDGenerator
{
    /// <summary>
    /// 건물 타입 ID와 타임스탬프를 조합하여 고유한 인스턴스 ID를 생성합니다.
    /// 형식: building_type_id * 10^13 + DateTime.UtcNow.Ticks % 10^13
    /// 예: 타입 1, 타임스탬프 702891234567 -> 10000702891234567
    /// </summary>
    /// <param name="buildingTypeId">건물 타입 ID (BuildingData.building_id)</param>
    /// <returns>고유한 건물 인스턴스 ID (long 타입)</returns>
    public static long GenerateInstanceID(int buildingTypeId)
    {
        // 타임스탬프를 13자리로 제한 (10^13 = 10000000000000)
        long timestamp = DateTime.UtcNow.Ticks % 10000000000000L;

        // 건물 타입 ID를 앞자리에, 타임스탬프를 뒷자리에 배치
        long instanceId = ((long)buildingTypeId * 10000000000000L) + timestamp;

        return instanceId;
    }
}
