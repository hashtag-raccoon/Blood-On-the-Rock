using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Building", menuName = "Building")]
public class BuildingData : ScriptableObject, IScrollItemData
{
    public int id;
    public string BuildingName;
    public Sprite icon;
    public int amount;
    public int price;
    public goodsData priceType; // 상품의 가격 유형
    public int demandLevel; // 요구 레벨

    public int construction_cost_gold;
    public int construction_cost_wood;
    public int construction_time_minutes;
}