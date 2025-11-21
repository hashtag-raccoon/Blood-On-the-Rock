using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System.Linq;
using Newtonsoft.Json;
using System;
using System.Data;


public class DataManager : MonoBehaviour
{
    #region Singleton

    private static DataManager _instance;
    public static DataManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<DataManager>();
            }
            return _instance;
        }
    }
    #endregion

    #region Constants
    #endregion

    #region Data Sources (ScriptableObject)
    [Header("데이터 에셋")]
    [Tooltip("NPC의 고정 특성(성격, 기본 능력치 등)을 담고 있는 ScriptableObject")]
    [SerializeField] private PersonalityDataSO personalityDataSO;
    [Tooltip("건물의 고정 정보(ID, 이름, 레벨, 아이콘 등)를 담고 있는 ScriptableObject")]
    [SerializeField] private BuildingDataSO buildingDataSO;
    [Tooltip("건물의 생산 관련 고정 정보(생산품, 시간 등)를 담고 있는 ScriptableObject")]
    [SerializeField] private BuildingProductionInfoSO buildingProductionInfoSO;
    #endregion

    #region Raw Data Lists (원본 데이터 - 일부는 Repository로 이전)
    // --- 건물 관련 데이터 (저장/로드) ---
    [Header("저장/로드 데이터 (Player-specific)")]
    // 현재 건설된 건물의 생산 상태 (플레이어 세이브 파일에서 로드)
    public List<ConstructedBuildingProduction> ConstructedBuildingProductions = new List<ConstructedBuildingProduction>();
    #endregion

    #region Runtime Data Lists (가공된 런타임 데이터)
    [Header("조합 데이터")]
    // Repositories에서 원본 데이터를 조합하여 생성한, 실제 게임 로직에서 사용될 데이터 리스트입니다.

    [SerializeField] public List<npc> npcs = new List<npc>();
    [SerializeField] public List<ConstructedBuilding> ConstructedBuildings = new List<ConstructedBuilding>();
    #endregion

    #region Game Resources
    [Space(2)]
    [Header("섬/자원 현황")]
    public int wood = 0;
    public int money = 0;

    [Header("바 현재 선호도/바 현재 레벨")]
    public float storeFavor = 100f;
    public int barLevel = 1;
    #endregion

    #region JSON Handler
    private JsonDataHandler jsonDataHandler;
    #endregion

    private readonly List<IRepository> _repositories = new List<IRepository>();

    #region Unity Lifecycle
    private void Awake()
    {
        InitializeSingleton();
        jsonDataHandler = new JsonDataHandler();
        InitializeDataFiles();
        LoadAllData();
        Debug.Log("DataManager 초기화 및 모든 데이터 로딩 완료.");
    }

    private IEnumerator Start()
    {
        // 모든 Repository가 DataManager에 등록될 때까지 잠시 대기합니다.
        // (Awake 순서는 보장되지 않으므로, 한 프레임 대기하는 것이 안전합니다.)
        yield return null;

        // Repository 초기화
        foreach (var repo in _repositories)
        {
            // BuildingRepository와 같이 특별한 데이터가 필요한 경우
            if (repo is BuildingRepository buildingRepo)
            {
                buildingRepo.Initialize(ConstructedBuildingProductions);
            }
            else // 그 외 일반적인 Repository
            {
                repo.Initialize();
            }
        }

        // 초기화가 끝날 때까지 대기
        yield return new WaitUntil(() => _repositories.All(r => r.IsInitialized));

        // Repository로부터 가공된 런타임 데이터를 받아옵니다.
        ConstructedBuildings = BuildingRepository.Instance.GetConstructedBuildings();
        npcs = ArbeitRepository.Instance.GetNpcs();

        // 저장된 건물들을 씬에 생성합니다.
        BuildingRepository.Instance.SpawnSavedBuildings();
    }

    private void OnApplicationQuit()
    {
        // 게임 종료 직전, 변경된 런타임 데이터의 '상태'를 원본 '상태' 데이터에 반영 후 저장합니다.
        UpdateConstructedBuildingProductionsFromConstructedBuildings();
        UpdateAndSaveArbeitData();
        SaveConstructedBuildingProductions();
    }

    private void OnDestroy()
    {
        //CleanupResources();
    }
    #endregion

    #region Initialization
    private void InitializeSingleton()
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
    }

    /// <summary>
    /// 각 Repository가 Awake 시점에 자신을 등록하기 위해 호출하는 메서드입니다.
    /// </summary>
    /// <param name="repository">등록할 Repository 인스턴스</param>
    public void RegisterRepository(IRepository repository)
    {
        _repositories.Add(repository);
    }

    private void InitializeDataFiles()
    {
        jsonDataHandler.InitializeFiles();
    }

    private void LoadAllData()
    {
        // 게임 시작에 필요한 모든 데이터를 로드합니다.
        ConstructedBuildingProductions = jsonDataHandler.LoadConstructedBuildingProductions();
        Debug.Log($"ConstructedBuildingProduction {ConstructedBuildingProductions.Count}개를 JSON에서 로드했습니다.");
    }
    #endregion

    #region Data Saving Methods
    /// <summary>
    /// 현재 건설된 건물 생산 상태를 JSON 파일에 저장합니다.
    /// </summary>
    public void SaveConstructedBuildingProductions()
    {
        jsonDataHandler.SaveConstructedBuildingProductions(ConstructedBuildingProductions);
    }

    /// <summary>
    /// 게임 플레이 중 변경된 'ConstructedBuilding' 런타임 객체의 상태를
    /// 저장을 위한 원본 상태 데이터인 'ConstructedBuildingProductions' 리스트에 다시 반영합니다.
    /// </summary>
    private void UpdateConstructedBuildingProductionsFromConstructedBuildings()
    {
        if (ConstructedBuildings == null || ConstructedBuildingProductions == null) return;

        var productionDict = new Dictionary<int, ConstructedBuildingProduction>();
        foreach (var production in ConstructedBuildingProductions)
        {
            if (!productionDict.ContainsKey(production.building_id))
            {
                productionDict.Add(production.building_id, production);
            }
            else
            {
                Debug.LogWarning($"[Data Duplication] ConstructedBuildingProductions에 중복된 building_id '{production.building_id}'가 있습니다. 첫 번째 항목만 사용됩니다.");
            }
        }

        foreach (var building in ConstructedBuildings)
        {
            if (productionDict.TryGetValue(building.Id, out var production))
            // 딕셔너리에서 ID로 해당 건물의 상태 데이터를 찾습니다.
            {
                production.is_producing = building.IsProducing;
                production.last_production_time = building.LastProductionTime;
                production.next_production_time = building.NextProductionTime;
            }
        }



    }

    /// <summary>
    /// 게임 플레이 중 변경된 'npc' 런타임 객체의 상태를
    /// 저장을 위한 원본 상태 데이터인 'arbeitDatas' 리스트에 다시 반영하고, 그 결과를 JSON 파일에 저장합니다.
    /// </summary>
    private void UpdateAndSaveArbeitData()
    {
        if (npcs == null) return;

        var arbeitDict = jsonDataHandler.LoadArbeitData().ToDictionary(a => a.part_timer_id);

        foreach (var npc in this.npcs)
        {
            if (arbeitDict.TryGetValue(npc.part_timer_id, out var arbeitData))
            {
                // npc 객체의 변경 가능한 상태(레벨, 경험치 등)를 arbeitData에 덮어씁니다. 
                arbeitData.level = npc.level;
                arbeitData.exp = npc.exp;
                arbeitData.employment_state = npc.employment_state;
                arbeitData.fatigue = npc.fatigue;
                arbeitData.need_rest = npc.need_rest;
            }
        }

        // 최신 상태가 반영된 arbeitDatas 리스트를 파일에 저장합니다.
        jsonDataHandler.SaveArbeitData(arbeitDict.Values.ToList());
    }
    #endregion

    public void UpgradeBuildingLevel(int buildingId)
    {
        ConstructedBuilding building = GetConstructedBuildingById(buildingId);
        if (building != null)
        {
            building.Level += 1;
            Debug.Log($"건물 ID:{buildingId} '{building.Name}' 레벨 업그레이드: {building.Level}");
        }
        else
        {
            Debug.LogError($"건물 ID:{buildingId}를 찾을 수 없습니다.");
        }
    }

    #region  ConstructedBuilding Methods

    public ConstructedBuilding GetConstructedBuildingName(string buildingType)
    {
        return ConstructedBuildings.Find(data => data.Name == buildingType);
    }

    public ConstructedBuilding GetConstructedBuildingById(int buildingId)
    {
        return ConstructedBuildings.Find(data => data.Id == buildingId);
    }

    /// <summary>
    /// Main_Island에 건설된 모든 건물의 통합 데이터를 가져옴
    /// </summary>
    /// <param name="mainIslandId">메인 섬의 ID</param>
    /// <returns>건설된 건물 정보 리스트</returns>
    public List<ConstructedBuilding> GetConstructedBuildingsOnMainIsland(int mainIslandId = 1)
    {
        if (ConstructedBuildings == null)
        {
            Debug.LogWarning("ConstructedBuildings가 초기화되지 않았습니다.");
            return new List<ConstructedBuilding>();
        }

        // 특정 섬에 속한 건물만 필터링
        return ConstructedBuildings.Where(b =>
            BuildingRepository.Instance.GetBuildingDataById(b.Id)?.island_id == mainIslandId
        ).ToList();
    }

    /// <summary>
    /// 특정 타입의 건설된 건물들을 찾습니다.
    /// </summary>
    public List<ConstructedBuilding> GetConstructedBuildingsByType(string buildingType)
    {
        return ConstructedBuildings.FindAll(b => b.Type == buildingType);
    }

    /// <summary>
    /// 현재 생산 중인 건물들을 찾습니다.
    /// </summary>
    public List<ConstructedBuilding> GetProducingBuildings()
    {
        return ConstructedBuildings.FindAll(b => b.IsProducing);
    }
    #endregion
}
