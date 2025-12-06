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

    private List<ArbeitData> _arbeitDatas = new List<ArbeitData>();
    private List<npc> _npcs = new List<npc>();
    private readonly Dictionary<int, Personality> _personalityDict = new Dictionary<int, Personality>();

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
        CreateNpcList();
        IsInitialized = true;
        Debug.Log("ArbeitRepository 초기화 완료.");
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
                npc newNpc = null;
                if (_personalityDict.TryGetValue(arbeitData.personality_id, out Personality personality))
                {
                    newNpc = new npc(arbeitData, personality);
                }
                else
                {
                    //Debug.LogWarning($"ArbeitData '{arbeitData.part_timer_name}'(ID: {arbeitData.part_timer_id})에 대한 Personality ID '{arbeitData.personality_id}'를 찾을 수 없습니다.");
                    // Personality가 없는 경우에도 npc 객체를 생성함
                    newNpc = new npc(arbeitData, new Personality
                    {
                        personality_id = -1,
                        personality_name = "없음",
                        description = "",
                        specificity = "",
                        serving_ability = 0,
                        cooking_ability = 0,
                        cleaning_ability = 0
                    });
                }

                // ArbeitSpriteReference 사용해서 게임을 껐다켜도
                // 기존 NPC의 portraitSprite와 prefab_name 복구
                if (ArbeitManager.Instance != null && ArbeitManager.Instance.arbeitSpriteReference != null)
                {
                    RestoreNpcVisualData(newNpc, arbeitData);
                }

                _npcs.Add(newNpc);
            }
        }
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

    #region NPC Data Query Methods
    /// <summary>
    /// 프리팹 이름을 기반으로 NPC의 초상화 스프라이트를 반환함
    /// </summary>
    public Sprite GetPortraitByPrefabName(npc npcData)
    {
        if (npcData == null || string.IsNullOrEmpty(npcData.prefab_name))
            return null;

        var prefabRef = ArbeitManager.Instance.arbeitSpriteReference;
        List<ArbeitPrefabToSpritePair> pairs = null;

        // 종족에 따라 적절한 리스트 선택
        switch (npcData.race)
        {
            case "Human":
                pairs = prefabRef.Human_Pairs;
                break;
            case "Oak":
                pairs = prefabRef.Oak_Pairs;
                break;
            case "Vampire":
                pairs = prefabRef.Vampire_Pairs;
                break;
            default:
                Debug.LogWarning($"[ArbeitRepository] 알 수 없는 종족: {npcData.race}");
                return null;
        }
        // prefab_name으로 매칭되는 초상화 찾기
        if (pairs != null)
        {
            foreach (var pair in pairs)
            {
                if (pair.PairPrefab != null && pair.PairPrefab.name == npcData.prefab_name)
                {
                    return pair.PairPortrait;
                }
            }
        }
        Debug.Log($"[ArbeitRepository] 매칭되는 초상화를 찾을 수 없습니다: {npcData.prefab_name}");
        return null; // 만약 매칭되는 초상화가 없으면 null 반환
    }

    /// <summary>
    /// NPC의 초상화 스프라이트와 매칭되는 프리팹을 찾습니다.
    /// </summary>
    public GameObject GetMatchingPrefabByPortrait(npc npcData)
    {
        if (npcData.portraitSprite == null) return null;

        var prefabRef = ArbeitManager.Instance.arbeitSpriteReference;
        List<ArbeitPrefabToSpritePair> pairs = null;

        // 종족에 따라 적절한 리스트 선택
        switch (npcData.race)
        {
            case "Human":
                pairs = prefabRef.Human_Pairs;
                break;
            case "Oak":
                pairs = prefabRef.Oak_Pairs;
                break;
            case "Vampire":
                pairs = prefabRef.Vampire_Pairs;
                break;
            default:
                Debug.LogWarning($"[ArbeitRepository] 알 수 없는 종족: {npcData.race}");
                return null;
        }

        // 초상화 스프라이트로 매칭되는 프리팹 찾기
        if (pairs != null)
        {
            foreach (var pair in pairs)
            {
                if (pair.PairPortrait == npcData.portraitSprite && pair.PairPrefab != null)
                {
                    return pair.PairPrefab;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// 기존 NPC의 portraitSprite를 ArbeitPrefabReference를 사용하여 복구
    /// prefab_name이 null일 경우 "[종족]_[part_timer_id]" 형식으로 생성함
    /// </summary>
    private void RestoreNpcVisualData(npc npcData, ArbeitData arbeitData)
    {
        // prefab_name이 null이면 생성
        if (string.IsNullOrEmpty(npcData.prefab_name))
        {
            npcData.prefab_name = $"{npcData.race}_{npcData.part_timer_id}";
            arbeitData.prefab_name = npcData.prefab_name;
        }

        // portraitSprite가 null이면 ArbeitPrefabReference에서 복구 시도
        if (npcData.portraitSprite == null)
        {
            var prefabRef = ArbeitManager.Instance.arbeitSpriteReference;
            List<ArbeitPrefabToSpritePair> pairs = null;

            switch (npcData.race)
            {
                case "Human":
                    pairs = prefabRef.Human_Pairs;
                    break;
                case "Oak":
                case "Orc":
                    pairs = prefabRef.Oak_Pairs;
                    break;
                case "Vampire":
                    pairs = prefabRef.Vampire_Pairs;
                    break;
            }

            // prefab_name이나 part_timer_id로 매칭되는 초상화 찾기
            // 예: "Human_1" -> Human Pairs에서 첨 번째 초상화 사용 (part_timer_id 기반)
            if (pairs != null && pairs.Count > 0)
            {
                // part_timer_id를 인덱스로 사용 (순환 방지)
                int index = (npcData.part_timer_id - 1) % pairs.Count;
                if (index >= 0 && index < pairs.Count && pairs[index].PairPortrait != null)
                {
                    npcData.portraitSprite = pairs[index].PairPortrait;
                }
            }
        }
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

        string[] races = { "Human", "Oak", "Vampire" };

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

            // 종족에 맞는 초상화 스프라이트 할당 (ArbeitSpriteReference 사용)
            if (ArbeitManager.Instance != null && ArbeitManager.Instance.arbeitSpriteReference != null)
            {
                var prefabRef = ArbeitManager.Instance.arbeitSpriteReference;
                switch (candidate.race)
                {
                    case "Human":
                        if (prefabRef.Human_portraits.Count > 0)
                            candidate.Portrait = prefabRef.Human_portraits[UnityEngine.Random.Range(0, prefabRef.Human_portraits.Count)];
                        break;
                    case "Oak":
                        if (prefabRef.Oak_portraits.Count > 0)
                            candidate.Portrait = prefabRef.Oak_portraits[UnityEngine.Random.Range(0, prefabRef.Oak_portraits.Count)];
                        break;
                    case "Vampire":
                        if (prefabRef.Vampire_portraits.Count > 0)
                            candidate.Portrait = prefabRef.Vampire_portraits[UnityEngine.Random.Range(0, prefabRef.Vampire_portraits.Count)];
                        break;
                    default:
                        Debug.LogWarning($"[ArbeitRepository] 알 수 없는 종족: {candidate.race}");
                        candidate.Portrait = null;
                        break;
                }
            }
            else
            {
                Debug.LogWarning("[ArbeitRepository] ArbeitManager 또는 arbeitPrefabReference가 null입니다.");
                candidate.Portrait = null;
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

            int SumAbility = candidate.base_serving_ability + candidate.base_cooking_ability + candidate.base_cleaning_ability
            + candidate.personality_serving_bonus + candidate.personality_cooking_bonus + candidate.personality_cleaning_bonus;

            candidate.estimated_daily_wage = Mathf.RoundToInt((SumAbility - 3) * (100f / 12f) + 50);

            candidate.is_hired = false;

            candidates.Add(candidate);
        }

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

        // prefab_name을 "[종족]_[part_timer_id]" 형식으로 생성
        string generatedPrefabName = $"{tempData.race}_{newId}";

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
            daily_wage = tempData.estimated_daily_wage,
            prefab_name = generatedPrefabName,
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

        // ArbeitSpriteReference를 사용하여 초상화와 프리팹 이름 설정
        if (ArbeitManager.Instance == null)
        {
            Debug.LogError("[ArbeitRepository] ArbeitManager.Instance가 null입니다!");
        }
        else if (ArbeitManager.Instance.arbeitSpriteReference == null)
        {
            Debug.LogError($"[ArbeitRepository] ArbeitManager.Instance.arbeitSpriteReference가 null입니다! (ArbeitManager: {ArbeitManager.Instance.gameObject.name})");
        }

        if (ArbeitManager.Instance != null && ArbeitManager.Instance.arbeitSpriteReference != null)
        {
            var prefabRef = ArbeitManager.Instance.arbeitSpriteReference;
            List<ArbeitPrefabToSpritePair> pairs = null;

            switch (newNpc.race)
            {
                case "Human":
                    pairs = prefabRef.Human_Pairs;
                    break;
                case "Oak":
                    pairs = prefabRef.Oak_Pairs;
                    break;
                case "Vampire":
                    pairs = prefabRef.Vampire_Pairs;
                    break;
                default:
                    Debug.LogWarning($"[ArbeitRepository] '{newNpc.part_timer_name}' - 알 수 없는 종족: {newNpc.race}");
                    break;
            }

            // prefab_name 형식으로 매칭 (예: "Vampire_1" -> pairs[0], "Oak_3" -> pairs[2])
            if (pairs != null && pairs.Count > 0)
            {
                // generatedPrefabName은 "종족_ID" 형식 (예: "Vampire_1")
                // ID를 추출하여 인덱스로 사용 (1부터 시작하므로 -1)
                string[] parts = generatedPrefabName.Split('_');
                if (parts.Length == 2 && int.TryParse(parts[1], out int id))
                {
                    int index = (id - 1) % pairs.Count; // 순환 방지

                    if (index >= 0 && index < pairs.Count)
                    {
                        var pair = pairs[index];

                        // 초상화 설정
                        if (pair.PairPortrait != null)
                        {
                            newNpc.portraitSprite = pair.PairPortrait;
                        }

                        // 프리팹 이름 설정 (실제 프리팹 이름 사용)
                        if (pair.PairPrefab != null)
                        {
                            string actualPrefabName = pair.PairPrefab.name;
                            newArbeitData.prefab_name = actualPrefabName;
                            newNpc.prefab_name = actualPrefabName;
                        }
                        else
                        {
                            Debug.LogWarning($"[ArbeitRepository] '{newNpc.part_timer_name}' ({newNpc.race}) - pairs[{index}]의 PairPrefab이 null입니다.");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"[ArbeitRepository] '{newNpc.part_timer_name}' - 계산된 인덱스 {index}가 범위를 벗어났습니다. (pairs.Count={pairs.Count})");
                    }
                }
            }
            else
            {
                Debug.LogWarning($"[ArbeitRepository] '{newNpc.part_timer_name}' ({newNpc.race}) - pairs가 null이거나 비어있습니다.");
            }
        }
        else
        {
            Debug.LogWarning($"[ArbeitRepository] ArbeitManager 또는 ArbeitSpriteReference가 없어 비주얼 데이터 설정을 건너뜁니다.");
        }

        _npcs.Add(newNpc);

        // JSON 에 알바 데이터 저장
        _jsonDataHandler.SaveArbeitData(_arbeitDatas);

        return newNpc;
    }
    #endregion

}
