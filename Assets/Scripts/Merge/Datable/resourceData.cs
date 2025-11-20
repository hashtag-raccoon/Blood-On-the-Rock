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

    // 인스펙터에서 값이 변경될 때마다 유효성 검사 수행
    private void OnValidate()
    {
        if (max_storage < 0) max_storage = 0;
        if (current_amount > max_storage) current_amount = max_storage;
        if (current_amount < 0) current_amount = 0;
    }
}
