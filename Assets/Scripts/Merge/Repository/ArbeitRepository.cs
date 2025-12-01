using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

public class ArbeitRepository : MonoBehaviour, IRepository
{
    private static ArbeitRepository _instance;
    public static ArbeitRepository Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<ArbeitRepository>();
            }
            return _instance;
        }
    }
    public bool IsInitialized { get; private set; } = false;

    private JsonDataHandler _jsonDataHandler;

    [Header("데이터 에셋 (SO)")]
    [Tooltip("NPC의 고정 특성(성격, 기본 능력치 등)을 담고 있는 ScriptableObject")]
    [SerializeField] private PersonalityDataSO personalityDataSO;

    [Header("NPC 프리팹")]
    [Tooltip("NPC 프리팹 리스트 (프리팹 이름이 prefab_name과 매핑됩니다)")]
    [SerializeField] private List<GameObject> npcPrefabs = new List<GameObject>();

    [Header("NPC 스폰 설정")]
    [Tooltip("NPC가 스폰될 기본 위치")]
    [SerializeField] private Transform defaultSpawnPoint;

    private List<ArbeitData> _arbeitDatas = new List<ArbeitData>();
    private List<npc> _npcs = new List<npc>();
    private readonly Dictionary<int, Personality> _personalityDict = new Dictionary<int, Personality>();
    private readonly Dictionary<string, GameObject> _npcPrefabDict = new Dictionary<string, GameObject>();
    private readonly List<GameObject> _spawnedNpcs = new List<GameObject>();

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
        _jsonDataHandler = new JsonDataHandler();
    }

    private void Start()
    {
        // DataManager가 초기화를 요청할 때까지 대기합니다.
        DataManager.Instance.RegisterRepository(this);
    }

    public void Initialize()
    {
        LoadArbeitData();
        LoadPersonalityData();

        // 데이터가 준비되면, 딕셔너리를 초기화하고 NPC 리스트를 생성합니다.
        InitializeDictionaries();
        InitializeNpcPrefabDictionary();
        CreateNpcList();
        IsInitialized = true;
        Debug.Log("ArbeitRepository 초기화 완료.");
    }

    /// <summary>
    /// NPC 프리팹 리스트를 딕셔너리로 변환합니다.
    /// 프리팹 이름(예: Human1)을 키로 사용하여 빠른 조회를 가능하게 합니다.
    /// </summary>
    private void InitializeNpcPrefabDictionary()
    {
        _npcPrefabDict.Clear();
        
        if (npcPrefabs == null || npcPrefabs.Count == 0)
        {
            Debug.LogWarning("npcPrefabs 리스트가 비어있거나 null입니다. Inspector에서 프리팹을 할당해주세요.");
            return;
        }

        foreach (var prefab in npcPrefabs)
        {
            if (prefab == null)
            {
                Debug.LogWarning("npcPrefabs 리스트에 null 프리팹이 포함되어 있습니다.");
                continue;
            }

            if (!_npcPrefabDict.ContainsKey(prefab.name))
            {
                _npcPrefabDict.Add(prefab.name, prefab);
            }
            else
            {
                Debug.LogWarning($"[Data Duplication] NPC Prefab '{prefab.name}'이 중복됩니다.");
            }
        }
        Debug.Log($"NPC Prefab Dictionary 초기화 완료: {_npcPrefabDict.Count}개");
    }

    private void LoadArbeitData()
    {
        _arbeitDatas = _jsonDataHandler.LoadArbeitData();
    }

    private void LoadPersonalityData()
    {
        // PersonalityDataSO에서 직접 로드
    }

    /// <summary>
    /// DataManager로부터 받은 Personality 데이터를 딕셔너리로 변환하여 빠른 조회를 가능하게 합니다.
    /// </summary>
    private void InitializeDictionaries()
    {
        if (personalityDataSO != null && personalityDataSO.personalities != null)
        {
            var personalities = personalityDataSO.personalities;
            _personalityDict.Clear();
            foreach (var personality in personalities)
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
    private void CreateNpcList()
    {
        _npcs.Clear();

        if (_arbeitDatas == null || _personalityDict == null)
        {
            Debug.LogError("Arbeit data or Personality data is not loaded yet.");
            return;
        }

        foreach (var arbeitData in _arbeitDatas)
        {
            if (arbeitData.employment_state) // 고용 상태가 true인 경우에만 npc 리스트에 추가
            {
                // arbeitData의 personality_id를 사용하여 딕셔너리에서 해당 Personality 정보를 찾습니다.
                if (_personalityDict.TryGetValue(arbeitData.personality_id, out Personality personality))
                {
                    _npcs.Add(new npc(arbeitData, personality));
                }
                else
                {
                    Debug.LogWarning($"ArbeitData '{arbeitData.part_timer_name}'(ID: {arbeitData.part_timer_id})에 대한 Personality ID '{arbeitData.personality_id}'를 찾을 수 없습니다.");
                }
            }
        }
        Debug.Log($"고용된 npc {_npcs.Count}명을 생성했습니다.");
    }

    public List<npc> GetNpcs()
    {
        return _npcs;
    }

    #region NPC Spawn Methods
    /// <summary>
    /// DataManager.npcs 리스트를 기반으로 고용된 NPC를 씬에 스폰합니다.
    /// BuildingRepository.SpawnConstructedBuildings()와 유사한 방식으로 동작합니다.
    /// </summary>
    public void SpawnNpcs()
    {
        try
        {
            // DataManager의 조합 데이터(npcs)를 사용
            var npcsToSpawn = DataManager.Instance.npcs;

            if (npcsToSpawn == null || npcsToSpawn.Count == 0)
            {
                Debug.LogWarning("스폰할 NPC가 없습니다.");
                return;
            }

            foreach (var npcData in npcsToSpawn)
            {
                SpawnSingleNpc(npcData);
            }

            Debug.Log($"NPC {_spawnedNpcs.Count}명 스폰 완료.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"NPC 스폰 중 오류 발생: {ex}");
        }
    }

    /// <summary>
    /// 개별 NPC를 스폰합니다.
    /// </summary>
    /// <param name="npcData">스폰할 NPC의 데이터</param>
    /// <returns>스폰된 GameObject (실패 시 null)</returns>
    private GameObject SpawnSingleNpc(npc npcData)
    {
        if (npcData == null)
        {
            Debug.LogWarning("NPC 데이터가 null입니다.");
            return null;
        }

        if (string.IsNullOrEmpty(npcData.prefab_name))
        {
            Debug.LogWarning($"NPC '{npcData.part_timer_name}'(ID: {npcData.part_timer_id})의 prefab_name이 비어있습니다.");
            return null;
        }

        // prefab_name에서 실제 프리팹 이름으로 변환 (예: "Human_1" -> "Human1")
        string actualPrefabName = ConvertPrefabName(npcData.prefab_name);
        GameObject prefab = GetNpcPrefab(actualPrefabName);

        if (prefab == null)
        {
            Debug.LogWarning($"NPC '{npcData.part_timer_name}'(ID: {npcData.part_timer_id})의 프리팹 '{actualPrefabName}'을 찾을 수 없습니다.");
            return null;
        }

        // 스폰 위치 결정 (각 NPC마다 약간씩 오프셋을 주어 겹치지 않도록)
        Vector3 baseSpawnPosition = defaultSpawnPoint != null ? defaultSpawnPoint.position : Vector3.zero;
        Vector3 spawnPosition = baseSpawnPosition + new Vector3(_spawnedNpcs.Count * 1.5f, 0, 0); // X축으로 1.5씩 간격

        // 프리팹 인스턴스화
        GameObject spawnedNpc = Instantiate(prefab, spawnPosition, Quaternion.identity);
        spawnedNpc.name = $"{npcData.part_timer_name}_{npcData.part_timer_id}";

        // ArbeitController 초기화 (이미 프리팹에 있다면 가져오고, 없으면 추가)
        ArbeitController controller = spawnedNpc.GetComponent<ArbeitController>();
        if (controller == null)
        {
            controller = spawnedNpc.AddComponent<ArbeitController>();
        }
        controller.Initialize(npcData);

        _spawnedNpcs.Add(spawnedNpc);

        return spawnedNpc;
    }

    /// <summary>
    /// prefab_name을 실제 프리팹 파일 이름으로 변환합니다.
    /// 예: "Human_1" -> "Human1", "Vampire_3" -> "Vam3"
    /// </summary>
    private string ConvertPrefabName(string prefabName)
    {
        if (string.IsNullOrEmpty(prefabName)) return string.Empty;

        // 종족 이름 매핑 (JSON의 race/prefab_name -> 실제 프리팹 이름)
        // 예: "Vampire" -> "Vam", "Elf" -> "Oak" 등
        Dictionary<string, string> raceMapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "Vampire", "Vam" },
            { "Elf", "Oak" },
            { "Human", "Human" },
            { "Oak", "Oak" },
            { "Vam", "Vam" } // 이미 줄여진 경우도 지원
        };

        // prefab_name을 '_' 기준으로 분리 (예: "Vampire_3" -> ["Vampire", "3"])
        string[] parts = prefabName.Split('_');
        if (parts.Length != 2)
        {
            // '_'가 없거나 형식이 맞지 않으면 그대로 반환 (기존 동작 유지)
            return prefabName.Replace("_", "");
        }

        string race = parts[0];
        string id = parts[1];

        // 종족 이름 매핑 적용
        if (raceMapping.TryGetValue(race, out string mappedRace))
        {
            // 매핑된 종족 이름 + ID 조합 (예: "Vam" + "3" -> "Vam3")
            return mappedRace + id;
        }
        else
        {
            // 매핑이 없으면 원래 종족 이름 사용 (예: "Human_1" -> "Human1")
            return race + id;
        }
    }

    /// <summary>
    /// 프리팹 이름으로 NPC 프리팹을 조회합니다.
    /// </summary>
    private GameObject GetNpcPrefab(string prefabName)
    {
        if (_npcPrefabDict.TryGetValue(prefabName, out GameObject prefab))
        {
            return prefab;
        }
        return null;
    }

    /// <summary>
    /// prefab_name과 npc 데이터를 비교하여 매칭되는 NPC를 찾습니다.
    /// prefab_name 형식: "[종족]_[npc_id]" (예: "Human_1", "Oak_2")
    /// </summary>
    /// <param name="prefabName">검색할 prefab_name</param>
    /// <returns>매칭되는 npc 데이터 (없으면 null)</returns>
    public npc FindNpcByPrefabName(string prefabName)
    {
        if (string.IsNullOrEmpty(prefabName)) return null;

        string[] parts = prefabName.Split('_');
        if (parts.Length != 2) return null;

        string race = parts[0];
        if (!int.TryParse(parts[1], out int npcId)) return null;

        // npcs 리스트에서 race와 part_timer_id가 일치하는 NPC 찾기
        return _npcs.Find(n =>
            n.race.Equals(race, StringComparison.OrdinalIgnoreCase) &&
            n.part_timer_id == npcId);
    }

    /// <summary>
    /// 스폰된 모든 NPC GameObject를 반환합니다.
    /// </summary>
    public List<GameObject> GetSpawnedNpcs()
    {
        return _spawnedNpcs;
    }

    /// <summary>
    /// 특정 NPC의 GameObject를 반환합니다.
    /// </summary>
    public GameObject GetSpawnedNpcByPartTimerId(int partTimerId)
    {
        return _spawnedNpcs.Find(go =>
        {
            var controller = go.GetComponent<ArbeitController>();
            if (controller == null) return false;

            // ArbeitController의 npc 데이터에서 part_timer_id 확인
            // 참고: ArbeitController에 part_timer_id를 반환하는 프로퍼티가 필요할 수 있습니다.
            return go.name.EndsWith($"_{partTimerId}");
        });
    }
    #endregion

}
