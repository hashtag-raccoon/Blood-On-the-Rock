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

    public int maxOfferCount = 3; // 구인소 최대 알바 구인 수

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

    // [구인소] 임시 후보 NPC 리스트
    // 임시 알바생들임
    public List<TempNpcData> tempCandidateList = new List<TempNpcData>(3); // 최대 3명

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
                    //Debug.LogWarning($"ArbeitData '{arbeitData.part_timer_name}'(ID: {arbeitData.part_timer_id})에 대한 Personality ID '{arbeitData.personality_id}'를 찾을 수 없습니다.");
                    // Personality가 없는 경우에도 npc 객체를 생성함
                    _npcs.Add(new npc(arbeitData, new Personality
                    {
                        personality_id = -1,
                        personality_name = "없음",
                        description = "",
                        specificity = "",
                        serving_ability = 0,
                        cooking_ability = 0,
                        cleaning_ability = 0
                    }));
                }
            }
        }
        Debug.Log($"고용된 npc {_npcs.Count}명을 생성했습니다.");
    }

    public List<npc> GetNpcs()
    {
        return _npcs;
    }
    // 배치된 NPC 리스트 반환
    public List<npc> GetDeployedNpcs()
    {
        return _npcs.Where(n => n.is_deployed).ToList();
    }
    // 고용된 NPC 리스트 반환
    public List<npc> GethiredNpcs()
    {
        return _npcs.Where(n => n.employment_state).ToList();
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

    #region 알바생 후보 생성 및 관리
    /// <summary>
    /// 랜덤 임시 후보 NPC 생성 (구인소용)
    /// </summary>
    /// <param name="count">생성할 후보 수 (기본 3명)</param>
    /// <returns>생성된 TempNpcData 리스트</returns>
    public List<TempNpcData> CreateRandomTempCandidates(int count = 3)
    {
        List<TempNpcData> candidates = new List<TempNpcData>();

        string[] races = { "Human", "Orc", "Vampire" };

        for (int i = 0; i < count; i++)
        {
            TempNpcData candidate = new TempNpcData
            {
                temp_id = i + 1,
                race = races[UnityEngine.Random.Range(0, races.Length)]
            };

            // 종족에 맞는 랜덤 이름 선택
            candidate.part_timer_name = IntelligentNameGenerator.Generate(candidate.race);

            // 기본 능력치 (1~3)
            candidate.base_serving_ability = UnityEngine.Random.Range(1, 4);
            candidate.base_cooking_ability = UnityEngine.Random.Range(1, 4);
            candidate.base_cleaning_ability = UnityEngine.Random.Range(1, 4);

            // 종족에 맞는 초상화 스프라이트 할당
            switch (candidate.race)
            {
                case "Human":
                    candidate.Portrait = ArbeitManager.Instance.arbeitSpriteReference.
                    Human_portraits[UnityEngine.Random.Range(0, ArbeitManager.Instance.arbeitSpriteReference.Human_portraits.Count)];
                    break;
                case "Orc":
                    candidate.Portrait = ArbeitManager.Instance.arbeitSpriteReference.
                    Oak_portraits[UnityEngine.Random.Range(0, ArbeitManager.Instance.arbeitSpriteReference.Oak_portraits.Count)];
                    break;
                case "Vampire":
                    candidate.Portrait = ArbeitManager.Instance.arbeitSpriteReference.
                    Vampire_portraits[UnityEngine.Random.Range(0, ArbeitManager.Instance.arbeitSpriteReference.Vampire_portraits.Count)];
                    break;
                default:
                    candidate.Portrait = null;
                    break;
            }

            // 5% 확률로 성격 부여
            // 만약 성격 부여가 될 경우 PersoanlityDataSO에서 랜덤 성격 선택
            if (UnityEngine.Random.Range(0f, 1f) < (JobCenterScrollUI.PersonalityChance / 100) && personalityDataSO != null && personalityDataSO.personalities.Count > 0)
            {
                // PersonalitySO에서 랜덤 성격 선택
                Personality randomPersonality = personalityDataSO.personalities[UnityEngine.Random.Range(0, personalityDataSO.personalities.Count)];

                candidate.personality_id = randomPersonality.personality_id;
                candidate.personality_name = randomPersonality.personality_name;
                candidate.personality_serving_bonus = randomPersonality.serving_ability;
                candidate.personality_cooking_bonus = randomPersonality.cooking_ability;
                candidate.personality_cleaning_bonus = randomPersonality.cleaning_ability;
            }
            else
            {
                candidate.personality_id = -1; // 성격 없음
                candidate.personality_name = "없음";
                candidate.personality_serving_bonus = 0;
                candidate.personality_cooking_bonus = 0;
                candidate.personality_cleaning_bonus = 0;
            }

            // TODO : 예상 일급 계산 로직 필요
            // ex : candidate.estimated_daily_wage = ~~~
            candidate.estimated_daily_wage = 0;

            candidate.is_hired = false;

            candidates.Add(candidate);
        }

        Debug.Log($"[ArbeitRepository] 임시 후보 {candidates.Count}명 생성 완료");
        return candidates;
    }

    /// <summary>
    /// 임시 후보를 실제 NPC 데이터로 변환 및 저장
    /// 해당 메소드는 구인소에서 알바생을 고용할 때 호출됨
    /// </summary>
    /// <param name="tempData">임시 후보 데이터에서 실제 NPC 데이터로 변환할 대상</param>
    /// <returns>생성된 npc 객체</returns>
    public npc ConvertTempToRealNpc(TempNpcData tempData)
    {
        if (tempData == null)
        {
            Debug.LogError("TempNpcData가 null입니다.");
            return null;
        }

        // 새로운 part_timer_id 생성 (기존 최대값 + 1)
        int newId = _arbeitDatas.Count > 0 ? _arbeitDatas.Max(a => a.part_timer_id) + 1 : 1;

        // TempArbeitData를 토대로 ArbeitData 생성
        ArbeitData newArbeitData = new ArbeitData
        {
            part_timer_id = newId,
            part_timer_name = tempData.part_timer_name,
            race = tempData.race,
            personality_id = tempData.personality_id,
            serving_ability = tempData.FinalServingAbility,
            cooking_ability = tempData.FinalCookingAbility,
            cleaning_ability = tempData.FinalCleaningAbility,
            hire_date = DateTime.Now,
            employment_state = true,
            daily_wage = tempData.estimated_daily_wage
        };

        // ArbeitData 리스트에 추가
        _arbeitDatas.Add(newArbeitData);

        // npc 객체 생성
        // Personality > 0 => 즉 성격이 있을 때만 personality 할당
        Personality personality = null;
        if (tempData.personality_id > 0)
        {
            _personalityDict.TryGetValue(tempData.personality_id, out personality);
        }

        if (personality == null)
        {
            // 성격이 없을 경우 기본 성격 객체 생성
            personality = new Personality
            {
                personality_id = -1,
                personality_name = "없음",
                description = "",
                specificity = "",
                serving_ability = 0,
                cooking_ability = 0,
                cleaning_ability = 0
            };
        }
        // 만약 성격이 없으면 없는 npc로 생성, 있으면 해당 성격으로 npc 생성
        npc newNpc = new npc(newArbeitData, personality);
        _npcs.Add(newNpc);

        // JSON 에 알바 데이터 저장
        _jsonDataHandler.SaveArbeitData(_arbeitDatas);

        Debug.Log($"[ArbeitRepository] '{tempData.part_timer_name}' 고용 완료 (ID: {newId})");

        return newNpc;
    }
    #endregion

}
