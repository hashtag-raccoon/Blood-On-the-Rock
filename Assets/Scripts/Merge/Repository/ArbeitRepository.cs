using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Unity.VisualScripting;
using System;

public class ArbeitRepository : MonoBehaviour
{
    public static ArbeitRepository Instance { get; private set; }

    private DataManager _dataManager;
    private readonly Dictionary<int, Personality> _personalityDict = new Dictionary<int, Personality>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // DataManager 인스턴스를 가져옵니다.
        _dataManager = DataManager.Instance;
        StartCoroutine(WaitForDataAndInitialize());
    }

    private IEnumerator WaitForDataAndInitialize()
    {
        // DataManager가 NPC 생성에 필요한 모든 데이터를 로드할 때까지 대기합니다.
        yield return new WaitUntil(() =>
            _dataManager != null &&
            _dataManager.personalities != null && _dataManager.personalities.Count > 0 &&
            _dataManager.arbeitDatas != null
        );

        // 데이터가 준비되면, 딕셔너리를 초기화하고 NPC 리스트를 생성합니다.
        InitializeDictionaries();
        PopulateNpcList();
        
        // NPC Prefab 매핑 초기화
        if (_dataManager != null)
        {
            _dataManager.MapNpcPrefabs();
        }
    }

    /// <summary>
    /// DataManager로부터 받은 Personality 데이터를 딕셔너리로 변환하여 빠른 조회를 가능하게 합니다.
    /// </summary>
    public void InitializeDictionaries()
    {
        if (_dataManager.personalities != null && _dataManager.personalities.Count > 0)
        {
            _personalityDict.Clear();
            foreach (var personality in _dataManager.personalities)
            {
                if (personality == null) continue;

                // 중복된 personality_id가 있는지 확인하고, 없으면 딕셔너리에 추가합니다.
                if (!_personalityDict.ContainsKey(personality.personality_id))
                {
                    _personalityDict.Add(personality.personality_id, personality);
                }
                else
                {
                    // 중복 ID가 있을 경우, 경고 로그를 출력하여 데이터 오류를 쉽게 찾을 수 있도록 합니다.
                    Debug.LogWarning($"[Data Duplication] Personality ID '{personality.personality_id}'가 중복됩니다. 에셋: '{personality.name}'");
                }
            }
            Debug.Log($"Personality Dictionary 초기화 완료: {_personalityDict.Count}개");
        }
    }

    /// <summary>
    /// 로드된 원본 데이터(ArbeitData, Personality)를 조합하여 실제 게임에서 사용될 npc 객체 리스트를 생성하고,
    /// DataManager에 저장하여 다른 시스템에서 사용할 수 있도록 합니다.
    /// </summary>
    private void PopulateNpcList()
    {
        _dataManager.npcs = GetNpcs();
        Debug.Log($"고용된 npc {_dataManager.npcs.Count}명을 생성했습니다.");
    }

    public List<npc> GetNpcs()
    {
        List<npc> npcList = new List<npc>();

        if (_dataManager.arbeitDatas == null || _personalityDict == null)
        {
            Debug.LogError("Arbeit data or Personality data is not loaded yet.");
            return npcList;
        }

        foreach (var arbeitData in _dataManager.arbeitDatas)
        {
            if (arbeitData.employment_state) // 고용 상태가 true인 경우에만 npc 리스트에 추가
            {
                // arbeitData의 personality_id를 사용하여 딕셔너리에서 해당 Personality 정보를 찾습니다.
                if (_personalityDict.TryGetValue(arbeitData.personality_id, out Personality personality))
                {
                    npcList.Add(new npc(arbeitData, personality));
                }
                else
                {
                    Debug.LogWarning($"ArbeitData '{arbeitData.part_timer_name}'(ID: {arbeitData.part_timer_id})에 대한 Personality ID '{arbeitData.personality_id}'를 찾을 수 없습니다.");
                }
            }
        }
        return npcList;

    }

    #region NPC Spawn Methods

    /// <summary>
    /// prefab_name을 기준으로 NPC를 스폰합니다.
    /// BuildingFactory.CreateBuilding 패턴을 참고하여 구현되었습니다.
    /// </summary>
    /// <param name="prefabName">prefab_name (예: "오크_1", "인간_5")</param>
    /// <param name="position">스폰 위치</param>
    /// <param name="parent">부모 Transform (선택사항)</param>
    /// <returns>생성된 NPC GameObject</returns>
    public GameObject SpawnNpc(string prefabName, Vector3 position, Transform parent = null)
    {
        if (_dataManager == null)
        {
            Debug.LogError("DataManager가 초기화되지 않았습니다.");
            return null;
        }

        // DataManager에서 prefab 가져오기
        GameObject prefab = _dataManager.GetNpcPrefabByPrefabName(prefabName);
        if (prefab == null)
        {
            Debug.LogError($"NPC Prefab을 찾을 수 없습니다: {prefabName}");
            return null;
        }

        // prefab 인스턴스화
        GameObject npcInstance = Instantiate(prefab, position, Quaternion.identity);
        
        if (parent != null)
        {
            npcInstance.transform.SetParent(parent);
        }

        // prefab_name에서 정보 추출하여 GameObject 이름 설정
        npcInstance.name = $"{prefabName}_Instance";

        // 스프라이트 및 애니메이션 확인
        ValidateNpcComponents(npcInstance, prefabName);

        Debug.Log($"NPC 스폰 완료: {prefabName} at {position}");
        return npcInstance;
    }

    /// <summary>
    /// part_timer_id를 기준으로 NPC를 스폰합니다.
    /// </summary>
    /// <param name="partTimerId">part_timer_id</param>
    /// <param name="position">스폰 위치</param>
    /// <param name="parent">부모 Transform (선택사항)</param>
    /// <returns>생성된 NPC GameObject</returns>
    public GameObject SpawnNpcById(int partTimerId, Vector3 position, Transform parent = null)
    {
        if (_dataManager == null)
        {
            Debug.LogError("DataManager가 초기화되지 않았습니다.");
            return null;
        }

        var arbeitData = _dataManager.arbeitDatas.Find(a => a.part_timer_id == partTimerId);
        if (arbeitData == null || string.IsNullOrEmpty(arbeitData.prefab_name))
        {
            Debug.LogError($"part_timer_id {partTimerId}에 해당하는 ArbeitData를 찾을 수 없거나 prefab_name이 없습니다.");
            return null;
        }

        return SpawnNpc(arbeitData.prefab_name, position, parent);
    }

    /// <summary>
    /// race와 part_timer_id를 기준으로 NPC를 스폰합니다.
    /// </summary>
    /// <param name="race">종족</param>
    /// <param name="partTimerId">part_timer_id</param>
    /// <param name="position">스폰 위치</param>
    /// <param name="parent">부모 Transform (선택사항)</param>
    /// <returns>생성된 NPC GameObject</returns>
    public GameObject SpawnNpcByRaceAndId(string race, int partTimerId, Vector3 position, Transform parent = null)
    {
        if (_dataManager == null)
        {
            Debug.LogError("DataManager가 초기화되지 않았습니다.");
            return null;
        }

        var arbeitData = _dataManager.arbeitDatas.Find(a => a.race == race && a.part_timer_id == partTimerId);
        if (arbeitData == null || string.IsNullOrEmpty(arbeitData.prefab_name))
        {
            Debug.LogError($"race '{race}', part_timer_id {partTimerId}에 해당하는 ArbeitData를 찾을 수 없거나 prefab_name이 없습니다.");
            return null;
        }

        return SpawnNpc(arbeitData.prefab_name, position, parent);
    }

    /// <summary>
    /// NPC GameObject의 스프라이트 및 애니메이션 컴포넌트를 확인합니다.
    /// </summary>
    private void ValidateNpcComponents(GameObject npcInstance, string prefabName)
    {
        if (npcInstance == null) return;

        // SpriteRenderer 확인
        SpriteRenderer spriteRenderer = npcInstance.GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            if (spriteRenderer.sprite != null)
            {
                Debug.Log($"[{prefabName}] 스프라이트 적용 확인: {spriteRenderer.sprite.name}");
            }
            else
            {
                Debug.LogWarning($"[{prefabName}] SpriteRenderer는 있지만 스프라이트가 할당되지 않았습니다.");
            }
        }
        else
        {
            Debug.LogWarning($"[{prefabName}] SpriteRenderer 컴포넌트를 찾을 수 없습니다.");
        }

        // Animator 확인
        Animator animator = npcInstance.GetComponentInChildren<Animator>();
        if (animator != null)
        {
            if (animator.runtimeAnimatorController != null)
            {
                Debug.Log($"[{prefabName}] 애니메이션 컨트롤러 적용 확인: {animator.runtimeAnimatorController.name}");
            }
            else
            {
                Debug.LogWarning($"[{prefabName}] Animator는 있지만 AnimatorController가 할당되지 않았습니다.");
            }
        }
        else
        {
            Debug.LogWarning($"[{prefabName}] Animator 컴포넌트를 찾을 수 없습니다.");
        }
    }

    /// <summary>
    /// employment_state가 true인 모든 NPC를 자동으로 스폰합니다.
    /// </summary>
    /// <param name="startPosition">시작 위치</param>
    /// <param name="spacing">NPC 간 간격</param>
    /// <param name="parent">부모 Transform (선택사항)</param>
    /// <returns>스폰된 NPC GameObject 리스트</returns>
    public List<GameObject> AutoSpawnEmployedNpcs(Vector3 startPosition, float spacing = 2f, Transform parent = null)
    {
        List<GameObject> spawnedNpcs = new List<GameObject>();
        
        if (_dataManager == null || _dataManager.arbeitDatas == null)
        {
            Debug.LogError("DataManager가 초기화되지 않았습니다.");
            return spawnedNpcs;
        }
        
        List<ArbeitData> employedNpcs = _dataManager.arbeitDatas
            .FindAll(a => a.employment_state == true && !string.IsNullOrEmpty(a.prefab_name));
        
        if (employedNpcs.Count == 0)
        {
            Debug.LogWarning("스폰할 고용된 NPC가 없습니다.");
            return spawnedNpcs;
        }
        
        Debug.Log($"고용된 NPC {employedNpcs.Count}명을 자동 스폰 시작...");
        
        for (int i = 0; i < employedNpcs.Count; i++)
        {
            var arbeitData = employedNpcs[i];
            Vector3 position = startPosition + new Vector3(i * spacing, 0, 0);
            
            GameObject npc = SpawnNpc(arbeitData.prefab_name, position, parent);
            if (npc != null)
            {
                spawnedNpcs.Add(npc);
            }
        }
        
        Debug.Log($"자동 스폰 완료: 총 {spawnedNpcs.Count}명의 NPC가 스폰되었습니다.");
        return spawnedNpcs;
    }

    #endregion

}
