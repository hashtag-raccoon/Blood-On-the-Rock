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

}
