using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 종족별 비주얼 데이터 (Sprite, Portrait)
/// 각 접두사(직업 느낌, 이름 대체)별로 여러 스프라이트/초상화를 보유
/// </summary>
[System.Serializable]
public class PrefixVisualSet
{
    [Tooltip("접두사 이름 (ex: 교섭관, 농부 등)")]
    public string prefixName;

    [Tooltip("해당 접두사의 손님 프리팹들")]
    public List<GameObject> customerPrefabs = new List<GameObject>();

    [Tooltip("해당 접두사의 대화 초상화 스프라이트들")]
    public List<Sprite> portraitSprites = new List<Sprite>();
}

/// <summary>
/// 종족별 비주얼 데이터를 관리하는 스크립터블 오브젝트
/// </summary>
[CreateAssetMenu(fileName = "RaceVisualData", menuName = "Game Data/Race Visual Data")]
public class RaceVisualData : ScriptableObject
{
    [Header("종족 ID 및 이름")]
    [Tooltip("ID (0=Human, 1=Orc, 2=Vampire)")]
    public int raceId;

    [Tooltip("이름")]
    public string raceName;

    [Tooltip("각 접두사별 비주얼(프리팹), 초상화) 세트")]
    public List<PrefixVisualSet> prefixVisualSets = new List<PrefixVisualSet>();

    /// <summary>
    /// 접두사에 해당하는 랜덤 프리팹 가져오는 메소드
    /// </summary>
    public GameObject GetRandomCustomerPrefab(string prefix)
    {
        PrefixVisualSet visualSet = prefixVisualSets.Find(set => set.prefixName == prefix);
        if (visualSet != null && visualSet.customerPrefabs.Count > 0)
        {
            // 랜덤으로 스프라이트 선택
            return visualSet.customerPrefabs[Random.Range(0, visualSet.customerPrefabs.Count)];
        }
        // 디버그 메시지 출력 후 null 반환
        Debug.LogWarning($"[RaceVisualData] 접두사 '{prefix}'의 customerPrefabs를 찾을 수 없음");
        return null;
    }

    /// <summary>
    /// 접두사에 해당하는 랜덤 초상화 스프라이트 가져오는 메소드
    /// </summary>
    public Sprite GetRandomPortraitSprite(string prefix)
    {
        PrefixVisualSet visualSet = prefixVisualSets.Find(set => set.prefixName == prefix);
        if (visualSet != null && visualSet.portraitSprites.Count > 0)
        {
            // 랜덤으로 초상화 스프라이트 선택
            return visualSet.portraitSprites[Random.Range(0, visualSet.portraitSprites.Count)];
        }
        // 디버그 메시지 출력 후 null 반환
        Debug.LogWarning($"[RaceVisualData] 접두사 '{prefix}'의 portraitPrefabs를 찾을 수 없음.");
        return null;
    }
}