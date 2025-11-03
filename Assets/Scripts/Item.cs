using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ItemType
{
    Equipment,
    Consumable,
    Etc
}

[System.Serializable]
public class Item
{
    public ItemType itemType;
    public string ItemName;
    public Sprite ItemImange;

    public bool Use()
    {
        return false;
    }
}
