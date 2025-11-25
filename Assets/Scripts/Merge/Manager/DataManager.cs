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
    [Header("인테리어 데이터")]
    public List<InteriorData> InteriorDatas = new List<InteriorData>();
    
    // NPC Prefab 매핑: prefab_name -> GameObject prefab
    [Header("NPC Prefab 매핑")]
    public Dictionary<string, GameObject> npcPrefabDict = new Dictionary<string, GameObject>();
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

        BuildingRepository.Instance.SpawnConstructedBuildings();

        // 초기화가 끝날 때까지 대기
        yield return new WaitUntil(() => _repositories.All(r => r.IsInitialized));

        // Repository로부터 가공된 런타임 데이터를 받아옵니다.
        ConstructedBuildings = BuildingRepository.Instance.GetConstructedBuildings();
        npcs = ArbeitRepository.Instance.GetNpcs();
    }

    private void OnApplicationQuit()
    {
        // 게임 종료 직전, 변경된 런타임 데이터의 '상태'를 원본 '상태' 데이터에 반영 후 저장합니다.
        UpdateConstructedBuildingProductionsFromConstructedBuildings();
        UpdateConstructedBuildingPositionsFromConstructedBuildings();
        UpdateAndSaveArbeitData();
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

        int updateCount = 0;
        foreach (var building in ConstructedBuildings)
        {
            if (productionDict.TryGetValue(building.Id, out var production))
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

        var positionDict = new Dictionary<int, ConstructedBuildingPos>();
        foreach (var position in ConstructedBuildingPositions)
        {
            if (!positionDict.ContainsKey(position.building_id))
            {
                positionDict.Add(position.building_id, position);
            }
            else
            {
                Debug.LogWarning($"[Data Duplication] ConstructedBuildingPositions에 중복된 building_id '{position.building_id}'가 있습니다. 첫 번째 항목만 사용됩니다.");
            }
        }

        int updateCount = 0;
        foreach (var building in ConstructedBuildings)
        {
            if (positionDict.TryGetValue(building.Id, out var position))
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
                    Debug.Log($"건물 ID {building.Id}의 위치 데이터를 업데이트했습니다. Position: {building.Position}, Rotation: {building.Rotation}");
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

    #endregion

    #region Constructed Building_Inventory Methods

    public List<ConstructedBuilding> GetInventoryBuildings()
    {
        return ConstructedBuildings.FindAll(data => data.IsEditInventory);
    }

    /// <summary>
    /// 건물의 인벤토리 상태를 업데이트합니다.
    /// </summary>
    public void UpdateBuildingInventoryStatus(int buildingId, bool isInInventory)
    {
        var building = ConstructedBuildings.Find(b => b.Id == buildingId);
        if (building != null)
        {
            building.IsEditInventory = isInInventory;
            Debug.Log($"건물 ID '{buildingId}'의 인벤토리 상태를 업데이트했습니다: {isInInventory}");
        }
        else
        {
            Debug.LogWarning($"건물 ID '{buildingId}'를 찾을 수 없습니다.");
        }
    }


    /// <summary>
    /// 편집 모드 인벤토리 리스트를 갱신합니다.
    /// </summary>
    public void RefreshEditModeInventory()
    {
        EditMode_InventoryBuildings = GetInventoryBuildings();
        Debug.Log($"EditMode_InventoryBuildings 갱신: {EditMode_InventoryBuildings.Count}개의 건물");
    }

    #endregion

    public ResourceData GetResourceByName(string resourceName)
    {
        return ResourceRepository.Instance.GetResourceByName(resourceName);
    }

    #region NPC Prefab Mapping Methods

    /// <summary>
    /// prefab_name을 기준으로 prefab과 npcs 리스트를 매핑합니다.
    /// prefab_name 형식: "[종족]_[npc_id]" (예: "오크_1", "인간_5")
    /// </summary>
    public void MapNpcPrefabs()
    {
        if (arbeitDatas == null)
        {
            Debug.LogWarning("arbeitDatas가 초기화되지 않았습니다.");
            return;
        }
        
        // npcs는 null일 수 있음 (손님 데이터의 경우 employment_state가 false)
        // npcs가 null이어도 prefab은 로드할 수 있어야 함

        npcPrefabDict.Clear();
        
        Debug.Log($"[MapNpcPrefabs] 시작: arbeitDatas {arbeitDatas.Count}개, useResourcesFolder: {useResourcesFolder}, 경로: {npcPrefabPath}");

        foreach (var arbeitData in arbeitDatas)
        {
            if (string.IsNullOrEmpty(arbeitData.prefab_name))
            {
                Debug.LogWarning($"ArbeitData ID {arbeitData.part_timer_id}의 prefab_name이 비어있습니다.");
                continue;
            }

            // prefab_name을 "_"로 분리하여 race와 part_timer_id 추출
            string[] parts = arbeitData.prefab_name.Split('_');
            if (parts.Length < 2)
            {
                Debug.LogWarning($"ArbeitData ID {arbeitData.part_timer_id}의 prefab_name 형식이 올바르지 않습니다: {arbeitData.prefab_name}");
                continue;
            }

            string raceFromPrefabName = parts[0];
            if (!int.TryParse(parts[1], out int npcIdFromPrefabName))
            {
                Debug.LogWarning($"ArbeitData ID {arbeitData.part_timer_id}의 prefab_name에서 ID를 파싱할 수 없습니다: {arbeitData.prefab_name}");
                continue;
            }

            // race와 part_timer_id로 npc 찾기
            npc matchedNpc = npcs.Find(n => n.race == raceFromPrefabName && n.part_timer_id == npcIdFromPrefabName);
            if (matchedNpc == null)
            {
                // npcs 리스트에 없어도 prefab은 로드할 수 있음 (손님 데이터의 경우)
                Debug.Log($"NPC (race: {raceFromPrefabName}, id: {npcIdFromPrefabName})를 npcs 리스트에서 찾을 수 없습니다. prefab만 로드합니다.");
            }

            // Prefab 로드
            GameObject prefab = null;
            string prefabPath = "";
            string actualPrefabName = "";
            
            // prefab_name을 실제 파일명으로 변환 (Human_1 -> Human1, Vampire_3 -> Vam3)
            actualPrefabName = ConvertPrefabNameToFileName(arbeitData.prefab_name);
            Debug.Log($"[MapNpcPrefabs] ID {arbeitData.part_timer_id}: prefab_name '{arbeitData.prefab_name}' -> 파일명 '{actualPrefabName}' 변환");
            
            if (useResourcesFolder)
            {
                // Resources 폴더에서 로드
                prefabPath = $"{npcPrefabPath}/{actualPrefabName}";
                prefab = Resources.Load<GameObject>(prefabPath);
                Debug.Log($"[MapNpcPrefabs] Resources.Load 시도: {prefabPath}");
            }
            else
            {
                // Assets 폴더 기준 절대 경로에서 로드 (에디터 전용)
                #if UNITY_EDITOR
                prefabPath = $"Assets/{npcPrefabPath}/{actualPrefabName}.prefab";
                prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                Debug.Log($"[MapNpcPrefabs] AssetDatabase.LoadAssetAtPath 시도: {prefabPath}");
                #else
                Debug.LogError("useResourcesFolder가 false일 때는 에디터에서만 작동합니다. 빌드 후에는 Resources 폴더를 사용해야 합니다.");
                #endif
            }

            if (prefab == null)
            {
                Debug.LogWarning($"[MapNpcPrefabs] ✗ Prefab을 찾을 수 없습니다: {prefabPath} (원본 prefab_name: {arbeitData.prefab_name}, 변환된 파일명: {actualPrefabName})");
                continue;
            }
            
            Debug.Log($"[MapNpcPrefabs] ✓ Prefab 로드 성공: {prefabPath} -> {prefab.name}");

            // 딕셔너리에 추가
            if (!npcPrefabDict.ContainsKey(arbeitData.prefab_name))
            {
                npcPrefabDict.Add(arbeitData.prefab_name, prefab);
                Debug.Log($"NPC Prefab 매핑 완료: {arbeitData.prefab_name} -> {prefab.name}");
            }
            else
            {
                Debug.LogWarning($"중복된 prefab_name이 발견되었습니다: {arbeitData.prefab_name}");
            }
        }

        Debug.Log($"NPC Prefab 매핑 완료: 총 {npcPrefabDict.Count}개");
    }

    /// <summary>
    /// prefab_name을 실제 파일명으로 변환합니다.
    /// 예: Human_1 -> Human1, Vampire_3 -> Vam3, Oak_1 -> Oak1
    /// </summary>
    private string ConvertPrefabNameToFileName(string prefabName)
    {
        if (string.IsNullOrEmpty(prefabName))
            return prefabName;
        
        // 언더스코어로 분리
        string[] parts = prefabName.Split('_');
        if (parts.Length < 2)
            return prefabName; // 변환 불가능하면 원본 반환
        
        string racePart = parts[0];
        string idPart = parts[1];
        
        // 종족명 변환 규칙
        // Human -> Human (그대로)
        // Vampire -> Vam
        // Oak -> Oak (그대로)
        if (racePart.Equals("Vampire", StringComparison.OrdinalIgnoreCase))
        {
            racePart = "Vam";
        }
        
        // 숫자 부분 처리 (앞의 0 제거 등)
        if (int.TryParse(idPart, out int id))
        {
            // 파일명 형식: {race}{id} (예: Human1, Vam3, Oak1)
            return $"{racePart}{id}";
        }
        
        return prefabName; // 변환 실패 시 원본 반환
    }

    /// <summary>
    /// prefab_name으로 NPC prefab을 가져옵니다.
    /// </summary>
    public GameObject GetNpcPrefabByPrefabName(string prefabName)
    {
        if (npcPrefabDict.TryGetValue(prefabName, out GameObject prefab))
        {
            return prefab;
        }
        return null;
    }

    /// <summary>
    /// part_timer_id로 NPC prefab을 가져옵니다.
    /// </summary>
    public GameObject GetNpcPrefabById(int partTimerId)
    {
        var arbeitData = arbeitDatas.Find(a => a.part_timer_id == partTimerId);
        if (arbeitData != null && !string.IsNullOrEmpty(arbeitData.prefab_name))
        {
            return GetNpcPrefabByPrefabName(arbeitData.prefab_name);
        }
        return null;
    }

    /// <summary>
    /// race와 part_timer_id로 NPC prefab을 가져옵니다.
    /// </summary>
    public GameObject GetNpcPrefabByRaceAndId(string race, int partTimerId)
    {
        var arbeitData = arbeitDatas.Find(a => a.race == race && a.part_timer_id == partTimerId);
        if (arbeitData != null && !string.IsNullOrEmpty(arbeitData.prefab_name))
        {
            return GetNpcPrefabByPrefabName(arbeitData.prefab_name);
        }
        return null;
    }

    #endregion

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
}
