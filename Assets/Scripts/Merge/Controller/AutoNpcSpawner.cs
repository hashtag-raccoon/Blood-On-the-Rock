using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 게임 시작 시 NPC를 자동으로 스폰하는 컨트롤러
/// 예제 2번 스타일: 여러 NPC를 한 번에 자동 스폰
/// </summary>
public class AutoNpcSpawner : MonoBehaviour
{
    [Header("자동 스폰 설정")]
    [Tooltip("자동 스폰 활성화 여부")]
    public bool autoSpawnOnStart = true;
    
    [Tooltip("스폰 시작 위치")]
    public Vector3 startPosition = Vector3.zero;
    
    [Tooltip("NPC 간 간격")]
    public float spacing = 2f;
    
    [Tooltip("스폰 간 딜레이 (초)")]
    public float spawnDelay = 0.1f;
    
    [Header("스폰 모드 선택")]
    [Tooltip("true: employment_state가 true인 NPC만 스폰\nfalse: 수동으로 지정한 NPC ID 리스트 사용")]
    public bool spawnEmployedNpcsOnly = true;
    
    [Header("수동 스폰 설정 (spawnEmployedNpcsOnly가 false일 때 사용)")]
    [Tooltip("스폰할 NPC ID 리스트")]
    public List<int> npcIdsToSpawn = new List<int> { 1, 3, 4, 5 };
    
    [Header("부모 Transform (선택사항)")]
    [Tooltip("스폰된 NPC들의 부모가 될 Transform. null이면 씬 루트에 생성")]
    public Transform parentTransform;
    
    [Header("디버그")]
    [Tooltip("스폰 정보를 콘솔에 출력")]
    public bool debugLog = true;
    
    private List<GameObject> spawnedNpcs = new List<GameObject>();
    
    void Start()
    {
        if (autoSpawnOnStart)
        {
            StartCoroutine(SpawnAllNpcs());
        }
    }
    
    /// <summary>
    /// 모든 NPC를 자동으로 스폰합니다.
    /// </summary>
    IEnumerator SpawnAllNpcs()
    {
        if (debugLog)
        {
            Debug.Log("[AutoNpcSpawner] 스폰 프로세스 시작...");
        }
        
        // DataManager 초기화 대기
        yield return new WaitUntil(() => DataManager.Instance != null);
        if (debugLog)
        {
            Debug.Log("[AutoNpcSpawner] DataManager 초기화 확인");
        }
        
        // ArbeitRepository 초기화 대기
        yield return new WaitUntil(() => ArbeitRepository.Instance != null);
        if (debugLog)
        {
            Debug.Log("[AutoNpcSpawner] ArbeitRepository 초기화 확인");
        }
        
        // DataManager의 arbeitDatas가 로드될 때까지 대기
        yield return new WaitUntil(() => 
            DataManager.Instance.arbeitDatas != null && 
            DataManager.Instance.arbeitDatas.Count > 0);
        if (debugLog)
        {
            Debug.Log($"[AutoNpcSpawner] ArbeitData 로드 확인: {DataManager.Instance.arbeitDatas.Count}개");
        }
        
        // MapNpcPrefabs가 실행되었는지 확인하고, 실행되지 않았다면 실행
        int maxWaitTime = 30; // 최대 30프레임 대기
        int waitCount = 0;
        while (DataManager.Instance.npcPrefabDict == null || 
               DataManager.Instance.npcPrefabDict.Count == 0)
        {
            if (waitCount >= maxWaitTime)
            {
                if (debugLog)
                {
                    Debug.LogWarning("[AutoNpcSpawner] MapNpcPrefabs가 자동 실행되지 않았습니다. 수동으로 실행합니다.");
                }
                DataManager.Instance.MapNpcPrefabs();
                break;
            }
            waitCount++;
            yield return null;
        }
        
        if (debugLog)
        {
            Debug.Log($"[AutoNpcSpawner] NPC Prefab 매핑 확인: {DataManager.Instance.npcPrefabDict.Count}개");
        }
        
        // 추가 안전 대기
        yield return new WaitForSeconds(0.1f);
        
        if (spawnEmployedNpcsOnly)
        {
            // employment_state가 true인 NPC만 스폰
            yield return StartCoroutine(SpawnEmployedNpcs());
        }
        else
        {
            // 수동으로 지정한 NPC ID 리스트로 스폰
            yield return StartCoroutine(SpawnNpcsByIdList());
        }
    }
    
    /// <summary>
    /// employment_state가 true인 NPC들을 자동으로 스폰합니다.
    /// </summary>
    IEnumerator SpawnEmployedNpcs()
    {
        if (DataManager.Instance == null)
        {
            Debug.LogError("[AutoNpcSpawner] DataManager가 초기화되지 않았습니다.");
            yield break;
        }
        
        if (DataManager.Instance.arbeitDatas == null)
        {
            Debug.LogError("[AutoNpcSpawner] ArbeitDatas가 null입니다.");
            yield break;
        }
        
        if (debugLog)
        {
            Debug.Log($"[AutoNpcSpawner] 전체 ArbeitData 수: {DataManager.Instance.arbeitDatas.Count}");
            foreach (var data in DataManager.Instance.arbeitDatas)
            {
                Debug.Log($"  - ID: {data.part_timer_id}, Name: {data.part_timer_name}, Employed: {data.employment_state}, Prefab: {data.prefab_name}");
            }
        }
        
        List<ArbeitData> employedNpcs = DataManager.Instance.arbeitDatas
            .FindAll(a => a.employment_state == true && !string.IsNullOrEmpty(a.prefab_name));
        
        if (employedNpcs.Count == 0)
        {
            if (debugLog)
            {
                Debug.LogWarning("[AutoNpcSpawner] 스폰할 고용된 NPC가 없습니다. employment_state가 true인 NPC를 확인하세요.");
            }
            yield break;
        }
        
        if (debugLog)
        {
            Debug.Log($"[AutoNpcSpawner] 고용된 NPC {employedNpcs.Count}명을 자동 스폰 시작...");
            foreach (var npc in employedNpcs)
            {
                Debug.Log($"  - {npc.part_timer_name} (ID: {npc.part_timer_id}, Prefab: {npc.prefab_name})");
            }
        }
        
        for (int i = 0; i < employedNpcs.Count; i++)
        {
            var arbeitData = employedNpcs[i];
            Vector3 position = startPosition + new Vector3(i * spacing, 0, 0);
            
            if (debugLog)
            {
                Debug.Log($"[AutoNpcSpawner] NPC 스폰 시도: {arbeitData.prefab_name} at {position}");
            }
            
            // Prefab이 매핑되어 있는지 확인
            if (!DataManager.Instance.npcPrefabDict.ContainsKey(arbeitData.prefab_name))
            {
                Debug.LogError($"[AutoNpcSpawner] Prefab '{arbeitData.prefab_name}'이 매핑되지 않았습니다. Resources/Prefab/NPC/{arbeitData.prefab_name}.prefab 경로를 확인하세요.");
                yield return new WaitForSeconds(spawnDelay);
                continue;
            }
            
            GameObject npc = ArbeitRepository.Instance.SpawnNpc(
                arbeitData.prefab_name,
                position,
                parentTransform
            );
            
            if (npc != null)
            {
                spawnedNpcs.Add(npc);
                if (debugLog)
                {
                    Debug.Log($"[AutoNpcSpawner] ✓ NPC {arbeitData.part_timer_name} (ID: {arbeitData.part_timer_id}, {arbeitData.prefab_name}) 스폰 완료 at {position}");
                }
            }
            else
            {
                Debug.LogError($"[AutoNpcSpawner] ✗ NPC {arbeitData.prefab_name} 스폰 실패!");
            }
            
            yield return new WaitForSeconds(spawnDelay);
        }
        
        if (debugLog)
        {
            Debug.Log($"[AutoNpcSpawner] 자동 스폰 완료: 총 {spawnedNpcs.Count}명의 NPC가 스폰되었습니다.");
        }
    }
    
    /// <summary>
    /// 수동으로 지정한 NPC ID 리스트로 NPC들을 스폰합니다.
    /// </summary>
    IEnumerator SpawnNpcsByIdList()
    {
        if (npcIdsToSpawn == null || npcIdsToSpawn.Count == 0)
        {
            if (debugLog)
            {
                Debug.LogWarning("스폰할 NPC ID 리스트가 비어있습니다.");
            }
            yield break;
        }
        
        if (debugLog)
        {
            Debug.Log($"NPC {npcIdsToSpawn.Count}명을 자동 스폰 시작...");
        }
        
        for (int i = 0; i < npcIdsToSpawn.Count; i++)
        {
            int npcId = npcIdsToSpawn[i];
            Vector3 position = startPosition + new Vector3(i * spacing, 0, 0);
            
            GameObject npc = ArbeitRepository.Instance.SpawnNpcById(
                npcId,
                position,
                parentTransform
            );
            
            if (npc != null)
            {
                spawnedNpcs.Add(npc);
                if (debugLog)
                {
                    Debug.Log($"NPC ID {npcId} 스폰 완료 at {position}");
                }
            }
            else
            {
                if (debugLog)
                {
                    Debug.LogWarning($"NPC ID {npcId} 스폰 실패");
                }
            }
            
            yield return new WaitForSeconds(spawnDelay);
        }
        
        if (debugLog)
        {
            Debug.Log($"자동 스폰 완료: 총 {spawnedNpcs.Count}명의 NPC가 스폰되었습니다.");
        }
    }
    
    /// <summary>
    /// 수동으로 스폰을 트리거합니다.
    /// </summary>
    public void TriggerSpawn()
    {
        if (!autoSpawnOnStart)
        {
            StartCoroutine(SpawnAllNpcs());
        }
    }
    
    /// <summary>
    /// 스폰된 모든 NPC를 제거합니다.
    /// </summary>
    public void ClearSpawnedNpcs()
    {
        foreach (var npc in spawnedNpcs)
        {
            if (npc != null)
            {
                Destroy(npc);
            }
        }
        spawnedNpcs.Clear();
        
        if (debugLog)
        {
            Debug.Log("스폰된 모든 NPC가 제거되었습니다.");
        }
    }
    
    /// <summary>
    /// 현재 스폰된 NPC 리스트를 반환합니다.
    /// </summary>
    public List<GameObject> GetSpawnedNpcs()
    {
        return new List<GameObject>(spawnedNpcs);
    }
}

