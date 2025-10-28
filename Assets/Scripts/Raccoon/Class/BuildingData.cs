using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Building", menuName = "Building")]
public class BuildingData : ScriptableObject
{
    public int id;
    public string BuildingName;
    public Sprite icon;
    public int amount;
}