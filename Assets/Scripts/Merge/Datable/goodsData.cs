using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum goodsType
{
    vegetable,    // 채소
    blood,       // 피
    meet,       // 고기
    wood,      // 나무
    money     // 돈
}

[CreateAssetMenu(fileName = "goods", menuName = "goods")]
public class goodsData : ScriptableObject, IScrollItemData
{
    public int id;
    public string goodsName;
    public goodsType type;
    public Sprite icon;
    public int amount;
    public int price;
}