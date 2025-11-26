using UnityEngine;

/// <summary>
/// 인테리어 오브젝트의 데이터를 저장하는 ScriptableObject
/// BuildingData와 유사한 구조이지만 인테리어 전용
/// </summary>
[CreateAssetMenu(fileName = "Interior", menuName = "Interior/InteriorData", order = 0)]
public class InteriorData : ScriptableObject, IScrollItemData
{
    [Header("인테리어 기본 정보")]
    public int interior_id; // 인테리어 ID(PK)
    public string Interior_Name; // 인테리어명
    public string interior_Type; // 인테리어 타입 (예: "Furniture", "Decoration", "Wall" 등)
    
    [Header("비용 및 배치")]
    public int purchase_cost_gold; // 구매 비용(골드)
    public int purchase_cost_wood; // 구매 비용(목재)
    public Vector2Int tileSize; // 인테리어 크기 (타일 단위, 기본값 1x1)
    public float MarkerPositionOffset; // 인테리어 배치 시 프리뷰 마커 오프셋 높이
    
    [Header("스프라이트")]
    public Sprite icon; // icon sprite
    public Sprite interior_sprite; // 인테리어 스프라이트 (배치용)
}

