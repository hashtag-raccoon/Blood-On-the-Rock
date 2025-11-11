using UnityEngine;

[CreateAssetMenu(fileName = "Building", menuName = "Building/Building")]
public class BuildingData : ScriptableObject, IScrollItemData
{
    public int id;
    public int level;
    public string BuildingName;
    public Sprite icon;
    public int amount;
    public int price;
    public goodsData priceType;
    public int demandLevel;
    public int construction_cost_gold;
    public int construction_cost_wood;
    public int construction_time_minutes;
}

