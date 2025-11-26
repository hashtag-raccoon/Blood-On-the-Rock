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

    #region NPC Prefab Path Settings
    [Header("NPC Prefab 경로 설정")]
    [Tooltip("Resources 폴더 사용 여부 (false면 Assets/Prefab/Character 같은 절대 경로 사용)")]
    [SerializeField] private bool useResourcesFolder = true;

    [Tooltip("Resources 폴더 기준 NPC Prefab 경로 (예: 'Prefab/Character')\n또는 Assets 폴더 기준 절대 경로 (예: 'Prefab/Character')")]
    [SerializeField] private string npcPrefabPath = "Prefab/Character";
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
    public List<ConstructedBuildingPos> ConstructedBuildingPositions = new List<ConstructedBuildingPos>();
    #endregion

    #region Runtime Data Lists (가공된 런타임 데이터)
    [Header("조합 데이터")]
    // Repositories에서 원본 데이터를 조합하여 생성한, 실제 게임 로직에서 사용될 데이터 리스트입니다.

    [SerializeField] public List<npc> npcs = new List<npc>();
    [SerializeField] public List<ConstructedBuilding> ConstructedBuildings = new List<ConstructedBuilding>();
    [SerializeField] public List<CocktailData> cocktails = new List<CocktailData>();
    [SerializeField] public List<OrderedCocktail> OrderedCocktails = new List<OrderedCocktail>();
    [SerializeField] public List<ConstructedBuilding> EditMode_InventoryBuildings = new List<ConstructedBuilding>();
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
        EditMode_InventoryBuildings = GetInventoryBuildings();
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
                buildingRepo.Initialize(ConstructedBuildingProductions, ConstructedBuildingPositions);
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
        OrderedCocktails = CocktailRepository.Instance.GetOrderedCocktails();
        BuildingRepository.Instance.SpawnConstructedBuildings();
    }

    private void OnApplicationQuit()
    {
        // 게임 종료 직전, 변경된 런타임 데이터의 '상태'를 원본 '상태' 데이터에 반영 후 저장합니다.
        UpdateConstructedBuildingProductionsFromConstructedBuildings();
        UpdateConstructedBuildingPositionsFromConstructedBuildings();
        UpdateAndSaveArbeitData();
        SaveCocktailProgress();
        SaveConstructedBuildingProductions();
        SaveConstructedBuidlingPositions();
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

        ConstructedBuildingPositions = jsonDataHandler.LoadBuildingPositions();
        Debug.Log($"ConstructedBuildingPositions {ConstructedBuildingPositions.Count}개를 JSON에서 로드했습니다.");
    }
    #endregion

    #region Data Saving Methods
    /// <summary>
    /// 현재 건설된 건물 위치를 JSON 파일에 저장합니다.
    /// </summary>
    public void SaveConstructedBuidlingPositions()
    {
        jsonDataHandler.SaveBuildingPosition(ConstructedBuildingPositions);
    }

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

        var productionDict = new Dictionary<long, ConstructedBuildingProduction>();
        foreach (var production in ConstructedBuildingProductions)
        {
            if (!productionDict.ContainsKey(production.instance_id))
            {
                productionDict.Add(production.instance_id, production);
            }
            else
            {
                Debug.LogWarning($"[Data Duplication] ConstructedBuildingProductions에 중복된 instance_id '{production.instance_id}'가 있습니다. 첫 번째 항목만 사용됩니다.");
            }
        }

        int updateCount = 0;
        foreach (var building in ConstructedBuildings)
        {
            if (productionDict.TryGetValue(building.InstanceId, out var production))
            {
                // 변경사항이 있는지 확인하고 업데이트
                bool hasChanges = false;

                if (production.is_producing != building.IsProducing)
                {
                    production.is_producing = building.IsProducing;
                    hasChanges = true;
                }

                if (production.last_production_time != building.LastProductionTime)
                {
                    production.last_production_time = building.LastProductionTime;
                    hasChanges = true;
                }

                if (production.next_production_time != building.NextProductionTime)
                {
                    production.next_production_time = building.NextProductionTime;
                    hasChanges = true;
                }

                // 생산 슬롯 정보 저장
                if (building.IsProducing)
                {
                    ResourceBuildingController controller = FindResourceBuildingControllerByInstanceId(building.InstanceId);
                    if (controller != null)
                    {
                        List<ProductionSlotData> slotDataList = new List<ProductionSlotData>();
                        List<ResourceBuildingController.ProductionInfo> activeProds = controller.GetActiveProductions();

                        foreach (var prod in activeProds)
                        {
                            if (prod != null)
                            {
                                ProductionSlotData slotData = new ProductionSlotData
                                {
                                    slot_index = prod.slotIndex,
                                    resource_id = prod.productionData.resource_id,
                                    building_type = prod.productionData.building_type,
                                    time_remaining = prod.timeRemaining,
                                    total_production_time = prod.totalProductionTime
                                };
                                slotDataList.Add(slotData);
                            }
                        }

                        production.production_slots = slotDataList;
                        hasChanges = true;
                    }
                }
                else
                {
                    // 생산 중이 아니면 슬롯 정보 클리어
                    if (production.production_slots != null && production.production_slots.Count > 0)
                    {
                        production.production_slots.Clear();
                        hasChanges = true;
                    }
                }

                if (hasChanges)
                {
                    updateCount++;
                }
            }
        }

        if (updateCount > 0)
        {
            Debug.Log($"ConstructedBuildingProduction {updateCount}개의 데이터를 업데이트했습니다.");
        }
    }

    /// <summary>
    /// 게임 플레이 중 변경된 'ConstructedBuilding' 런타임 객체의 위치를
    /// 저장을 위한 원본 위치 데이터인 'ConstructedBuildingPositions' 리스트에 다시 반영합니다.
    /// </summary>
    private void UpdateConstructedBuildingPositionsFromConstructedBuildings()
    {
        if (ConstructedBuildings == null || ConstructedBuildingPositions == null) return;

        var positionDict = new Dictionary<long, ConstructedBuildingPos>();
        foreach (var position in ConstructedBuildingPositions)
        {
            if (!positionDict.ContainsKey(position.instance_id))
            {
                positionDict.Add(position.instance_id, position);
            }
            else
            {
                Debug.LogWarning($"[Data Duplication] ConstructedBuildingPositions에 중복된 instance_id '{position.instance_id}'가 있습니다. 첫 번째 항목만 사용됩니다.");
            }
        }

        int updateCount = 0;
        foreach (var building in ConstructedBuildings)
        {
            if (positionDict.TryGetValue(building.InstanceId, out var position))
            {
                // 변경사항이 있는지 확인하고 업데이트
                bool hasChanges = false;

                if (position.pos != building.Position)
                {
                    position.pos = building.Position;
                    hasChanges = true;
                }

                if (position.rotation != building.Rotation)
                {
                    position.rotation = building.Rotation;
                    hasChanges = true;
                }

                if (hasChanges)
                {
                    updateCount++;
                    Debug.Log($"건물 인스턴스 ID {building.InstanceId}의 위치 데이터를 업데이트했습니다. Position: {building.Position}, Rotation: {building.Rotation}");
                }
            }
        }

        if (updateCount > 0)
        {
            Debug.Log($"ConstructedBuildingPositions {updateCount}개의 데이터를 업데이트했습니다.");
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

    /// <summary>
    /// 칵테일 진행 정보(해금된 레시피)를 저장
    /// </summary>
    private void SaveCocktailProgress()
    {
        if (CocktailRepository.Instance == null) return;

        List<int> unlockedRecipeIds = CocktailRepository.Instance.GetUnlockedRecipeIds();
        jsonDataHandler.SaveCocktailProgress(unlockedRecipeIds);
    }
    #endregion

    public void UpgradeBuildingLevel(long instanceId)
    {
        ConstructedBuilding building = GetConstructedBuildingByInstanceId(instanceId);
        if (building != null)
        {
            building.Level += 1;
            Debug.Log($"건물 인스턴스 ID:{instanceId} '{building.Name}' 레벨 업그레이드: {building.Level}");
        }
        else
        {
            Debug.LogError($"건물 인스턴스 ID:{instanceId}를 찾을 수 없습니다.");
        }
    }

    #region  ConstructedBuilding Methods

    public ConstructedBuilding GetConstructedBuildingName(string buildingType)
    {
        return ConstructedBuildings.Find(data => data.Name == buildingType);
    }

    public ConstructedBuilding GetConstructedBuildingByInstanceId(long instanceId)
    {
        return ConstructedBuildings.Find(data => data.InstanceId == instanceId);
    }

    #endregion

    #region Constructed Building_Inventory Methods

    public List<ConstructedBuilding> GetInventoryBuildings()
    {
        return ConstructedBuildings.FindAll(data => data.IsEditInventory);
    }

    /// <summary>
    /// 편집 모드 인벤토리 리스트를 갱신합니다.
    /// </summary>
    public void RefreshEditModeInventory()
    {
        EditMode_InventoryBuildings = GetInventoryBuildings();
        Debug.Log($"EditMode_InventoryBuildings 갱신: {EditMode_InventoryBuildings.Count}개의 건물");
    }

    /// <summary>
    /// 건물의 인벤토리 상태를 업데이트합니다.
    /// </summary>
    public void UpdateBuildingInventoryStatus(long instanceId, bool isInInventory)
    {
        var building = ConstructedBuildings.Find(b => b.InstanceId == instanceId);
        if (building != null)
        {
            building.IsEditInventory = isInInventory;
            Debug.Log($"건물 인스턴스 ID '{instanceId}'의 인벤토리 상태를 업데이트했습니다: {isInInventory}");
        }
        else
        {
            Debug.LogWarning($"건물 인스턴스 ID '{instanceId}'를 찾을 수 없습니다.");
        }
    }



    #endregion

    public ResourceData GetResourceByName(string resourceName)
    {
        return ResourceRepository.Instance.GetResourceByName(resourceName);
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
            BuildingRepository.Instance.GetBuildingDataByTypeId(b.Id)?.island_id == mainIslandId
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

    /// <summary>
    /// Scene에서 특정 instance_id를 가진 ResourceBuildingController를 찾습니다.
    /// </summary>
    private ResourceBuildingController FindResourceBuildingControllerByInstanceId(long instanceId)
    {
        ResourceBuildingController[] controllers = FindObjectsOfType<ResourceBuildingController>();
        foreach (var controller in controllers)
        {
            if (controller.GetConstructedBuilding() != null &&
                controller.GetConstructedBuilding().InstanceId == instanceId)
            {
                return controller;
            }
        }
        return null;
    }

}
