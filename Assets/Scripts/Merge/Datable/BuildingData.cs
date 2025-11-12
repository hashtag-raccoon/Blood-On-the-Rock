using UnityEngine;

[CreateAssetMenu(fileName = "Building", menuName = "Building/Building")]
public class BuildingData : ScriptableObject, IScrollItemData
{
    // 테스트 주석
    public int id;
    public int level;
    public string BuildingName;
    public string Building_type;
    public Sprite icon;
    public int amount;
    public int price;
    public goodsData priceType;
    public int demandLevel;
    public int construction_cost_gold;
    public int construction_cost_wood;
    public int construction_time_minutes;
}

