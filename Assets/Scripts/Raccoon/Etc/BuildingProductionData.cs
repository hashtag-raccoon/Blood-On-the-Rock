using UnityEngine;

[CreateAssetMenu(fileName = "Building_Production", menuName = "Building/Building_Production")]
public class BuildingProductionData : ScriptableObject
{
    public string building_type;
    public int resource_id;
    public int output_amount;
    public float base_production_time_minutes;
    public int consume_amount;
    public string consume_resource_type;
}
