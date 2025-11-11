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
    [SerializeField] private Dictionary<int, Personality> _PersonalityDict = new Dictionary<int, Personality>();

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
        _dataManager = DataManager.instance;
        StartCoroutine(WaitForDataAndInitialize());
    }

    private IEnumerator WaitForDataAndInitialize()
    {
        // DataManager의 personalities 리스트가 채워질 때까지 기다립니다.
        yield return new WaitUntil(() => _dataManager != null && _dataManager.personalities != null && _dataManager.personalities.Count > 0 && _dataManager.arbeitDatas != null && _dataManager.arbeitDatas.Count > 0);
        InitializeDictionaries();
        PopulateNpcList();
    }

    public void InitializeDictionaries()
    {
        if (_dataManager.personalities != null && _dataManager.personalities.Count > 0)
        {
            _PersonalityDict = _dataManager.personalities.ToDictionary(p => p.personality_id);
        }
    }

    private void PopulateNpcList()
    {
        _dataManager.npcs = GetNpcs();
        _dataManager.LoadNPC();
    }

    public List<npc> GetNpcs()
    {
        List<npc> npcList = new List<npc>();


        if (_dataManager.arbeitDatas == null || _PersonalityDict == null)
        {
            Debug.LogError("Arbeit data or Personality data is not loaded yet.");
            return npcList;
        }

        foreach (var arbeitData in _dataManager.arbeitDatas)
        {
            if (arbeitData.employment_state) // 고용 상태가 true인 경우에만 npc 리스트에 추가
            {
                if (_PersonalityDict.TryGetValue(arbeitData.personality_id, out Personality personality))
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
