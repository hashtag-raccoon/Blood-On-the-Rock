using UnityEngine;

public enum goodsType
{
    vegetable,
    blood,
    meet,
    wood,
    money
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
