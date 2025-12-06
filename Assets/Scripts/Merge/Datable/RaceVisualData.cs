using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AYellowpaper.SerializedCollections;

/// <summary>
/// 비주얼 세트 (프리팹 1개 + 초상화 1개) 1개씩 있는 세트
/// </summary>
[Serializable]
public class PrefixVisualSet
{
    [Tooltip("손님 프리팹")]
    public GameObject customerPrefab;

    [Tooltip("대화 초상화 스프라이트")]
    public Sprite portraitSprite;
}

/// <summary>
/// 종족별 비주얼 데이터를 관리하는 스크립터블 오브젝트
/// </summary>
[CreateAssetMenu(fileName = "RaceVisualData", menuName = "Game Data/Race Visual Data")]
public class RaceVisualData : ScriptableObject
{
    [Header("종족 ID 및 이름")]
    [Tooltip("ID (0=Human, 1=Oak, 2=Vampire)")]
    public int raceId;

    [Tooltip("종족 이름")]
    public string raceName;

    [Header("접두사별 비주얼 데이터")]
    [SerializedDictionary("접두사", "비주얼 세트 리스트")]
    public SerializedDictionary<string, List<PrefixVisualSet>> prefixVisualSets = new SerializedDictionary<string, List<PrefixVisualSet>>();

    /// <summary>
    /// 접두사로 랜덤 PrefixVisualSet 가져오기
    /// </summary>
    public PrefixVisualSet GetRandomVisualSetByPrefix(string prefix)
    {
        if (string.IsNullOrEmpty(prefix))
        {
            Debug.LogWarning($"[RaceVisualData] 접두사가 비어있음");
            return null;
        }

        // 디버깅: 딕셔너리 상태 확인
        if (prefixVisualSets == null)
        {
            Debug.LogError($"[RaceVisualData] prefixVisualSets 딕셔너리가 null입니다!");
            return null;
        }

        if (prefixVisualSets.Count == 0)
        {
            Debug.LogError($"[RaceVisualData] prefixVisualSets 딕셔너리가 비어있습니다! 인스펙터에서 데이터를 설정했는지 확인하세요.");
            return null;
        }

        if (!prefixVisualSets.ContainsKey(prefix))
        {
            // 디버깅: 사용 가능한 키 목록 출력
            string availableKeys = string.Join(", ", prefixVisualSets.Keys);
            Debug.LogWarning($"[RaceVisualData/{raceName}] 접두사 '{prefix}'를 찾을 수 없음. 사용 가능한 접두사: [{availableKeys}]");
            return null;
        }

        List<PrefixVisualSet> visualSetList = prefixVisualSets[prefix];

        if (visualSetList == null || visualSetList.Count == 0)
        {
            Debug.LogWarning($"[RaceVisualData] 접두사 '{prefix}'의 비주얼 세트가 비어있음");
            return null;
        }

        // 리스트에서 랜덤으로 하나 선택
        return visualSetList[UnityEngine.Random.Range(0, visualSetList.Count)];
    }

    /// <summary>
    /// 접두사로 랜덤 프리팹 가져오기
    /// </summary>
    public GameObject GetRandomCustomerPrefab(string prefix)
    {
        PrefixVisualSet visualSet = GetRandomVisualSetByPrefix(prefix);

        if (visualSet == null || visualSet.customerPrefab == null)
        {
            Debug.LogWarning($"[RaceVisualData] 접두사 '{prefix}'의 프리팹을 찾을 수 없음");
            return null;
        }

        return visualSet.customerPrefab;
    }

    /// <summary>
    /// 접두사로 랜덤 초상화 스프라이트 가져오기
    /// </summary>
    public Sprite GetRandomPortraitSprite(string prefix)
    {
        PrefixVisualSet visualSet = GetRandomVisualSetByPrefix(prefix);

        if (visualSet == null || visualSet.portraitSprite == null)
        {
            Debug.LogWarning($"[RaceVisualData] 접두사 '{prefix}'의 초상화를 찾을 수 없음.");
            return null;
        }

        return visualSet.portraitSprite;
    }

    /// <summary>
    /// 프리팹으로 해당하는 PrefixVisualSet 찾기 (대화창 UI용)
    /// </summary>
    public PrefixVisualSet GetVisualSetByPrefab(GameObject prefab)
    {
        if (prefab == null)
        {
            Debug.LogWarning($"[RaceVisualData] 프리팹이 null임");
            return null;
        }

        foreach (var kvp in prefixVisualSets)
        {
            if (kvp.Value != null && kvp.Value.Count > 0)
            {
                foreach (var visualSet in kvp.Value)
                {
                    if (visualSet.customerPrefab == prefab)
                    {
                        return visualSet;
                    }
                }
            }
        }

        Debug.LogWarning($"[RaceVisualData] 프리팹 '{prefab.name}'에 해당하는 비주얼 세트를 찾을 수 없음");
        return null;
    }

    /// <summary>
    /// 프리팹 이름으로 해당하는 PrefixVisualSet 찾기 (대화창 UI용)
    /// </summary>
    public PrefixVisualSet GetVisualSetByPrefabName(string prefabName)
    {
        if (string.IsNullOrEmpty(prefabName))
        {
            Debug.LogWarning($"[RaceVisualData] 프리팹 이름이 비어있음");
            return null;
        }

        foreach (var kvp in prefixVisualSets)
        {
            if (kvp.Value != null && kvp.Value.Count > 0)
            {
                foreach (var visualSet in kvp.Value)
                {
                    if (visualSet.customerPrefab != null && visualSet.customerPrefab.name == prefabName)
                    {
                        return visualSet;
                    }
                }
            }
        }

        Debug.LogWarning($"[RaceVisualData] 프리팹 이름 '{prefabName}'에 해당하는 비주얼 세트를 찾을 수 없음");
        return null;
    }

    /// <summary>
    /// PrefixVisualSet에서 초상화 가져오기 (대화창 UI용)
    /// </summary>
    public Sprite GetPortraitFromVisualSet(PrefixVisualSet visualSet)
    {
        if (visualSet == null)
        {
            Debug.LogWarning($"[RaceVisualData] PrefixVisualSet X");
            return null;
        }

        if (visualSet.portraitSprite == null)
        {
            Debug.LogWarning($"[RaceVisualData] 비주얼 세트 초상화 X");
            return null;
        }

        return visualSet.portraitSprite;
    }

    /// <summary>
    /// 접두사 목록 가져오기
    /// </summary>
    public List<string> GetAllPrefixes()
    {
        return new List<string>(prefixVisualSets.Keys);
    }

    /// <summary>
    /// 특정 접두사의 모든 비주얼 세트 가져오기
    /// </summary>
    public List<PrefixVisualSet> GetAllVisualSetsByPrefix(string prefix)
    {
        if (string.IsNullOrEmpty(prefix))
        {
            Debug.LogWarning($"[RaceVisualData] 접두사가 비어있음");
            return null;
        }

        if (!prefixVisualSets.ContainsKey(prefix))
        {
            Debug.LogWarning($"[RaceVisualData] 접두사 '{prefix}'를 찾을 수 없음");
            return null;
        }

        return prefixVisualSets[prefix];
    }
}