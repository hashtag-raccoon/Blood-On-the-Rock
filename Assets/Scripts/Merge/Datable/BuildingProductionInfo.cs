using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEditor.ShaderKeywordFilter;

[CreateAssetMenu(fileName = "Building_Production_Info", menuName = "Building/Building_Production_Info")]
public class BuildingProductionInfo : ScriptableObject
{
    public string building_type;
    public int resource_id;
    public int output_amount;
    public float base_production_time_minutes;
    public int consume_amount;
    public string consume_resource_type;

    public bool isInInventory;
    
}