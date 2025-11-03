using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ItemData", menuName = "ScriptableObjects/ItemData")]
[Serializable]
public class ItemData : ScriptableObject
{
    public string itemName;
    public Sprite itemIcon;
    public int itemID;
    public string itemDescription;
}
