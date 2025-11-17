using UnityEngine;

public enum resourceType
{
    vegetable,
    blood,
    meet,
    wood,
    money
}

[CreateAssetMenu(fileName = "resource", menuName = "resource")]
public class ResourceData : ScriptableObject, IScrollItemData
{
    public int resource_id;
    public string resource_name;
    public resourceType type;
    public Sprite icon;
    public int current_amount;
    public int max_storage;
}
