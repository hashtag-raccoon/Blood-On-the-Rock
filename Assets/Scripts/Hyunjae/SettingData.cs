using UnityEngine;

/// <summary>
/// 설정 항목의 데이터를 저장하는 ScriptableObject
/// InteriorData와 유사한 구조이지만 설정 전용
/// </summary>
[CreateAssetMenu(fileName = "Setting", menuName = "Setting/SettingData", order = 0)]
public class SettingData : ScriptableObject, IScrollItemData
{
    [Header("설정 기본 정보")]
    public int setting_id; // 설정 ID(PK)
    public string Setting_Name; // 설정명 (Display, Audio, Control)
    public string setting_Type; // 설정 타입
    
    [Header("스프라이트")]
    public Sprite icon; // icon sprite
}

