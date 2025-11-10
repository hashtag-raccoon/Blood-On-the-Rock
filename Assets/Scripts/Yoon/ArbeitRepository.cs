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
    [SerializeField] private Dictionary<int, Personality> _PersonalityDict;

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
        InitializeDictionaries();
    }

    public void InitializeDictionaries()
    {
        _PersonalityDict = _dataManager.personalities.ToDictionary(p => p.personality_id);

    }

    public List<npc> GetNpcs()
    {
        List<npc> npcList = new List<npc>();

        

        return npcList;

    }

}
