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

    private List<ArbeitData> _arbeitDatas = new List<ArbeitData>();
    private List<npc> _npcs = new List<npc>();
    private readonly Dictionary<int, Personality> _personalityDict = new Dictionary<int, Personality>();

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

}
