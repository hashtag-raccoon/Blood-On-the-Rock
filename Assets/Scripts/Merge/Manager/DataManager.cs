using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System.Linq;
using Newtonsoft.Json;
using System;


public class DataManager : MonoBehaviour
{
    #region Singleton

    public static DataManager Instance { get; private set; }
    #endregion

    #region Constants
    private const string GoodsPath = "Data/Resource";
    private const string BuildingUpgradePath = "Data/Building/BuildingUpgradeData";
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

    #region Raw Data Lists (원본 데이터)
    // --- 건물 관련 원본 데이터 ---
    [Header("건물 데이터")]
    public List<BuildingData> BuildingDatas = new List<BuildingData>();
    public List<BuildingProductionInfo> BuildingProductionInfos = new List<BuildingProductionInfo>();
    public List<BuildingUpgradeData> BuildingUpgradeDatas = new List<BuildingUpgradeData>();

    // 현재 건설된 건물의 생산 상태 (플레이어 세이브 파일에서 로드)
    public List<ConstructedBuildingProduction> ConstructedBuildingProductions = new List<ConstructedBuildingProduction>();

    // --- NPC 관련 원본 데이터 ---
    [Header("NPC 데이터")]
    public List<ArbeitData> arbeitDatas = new List<ArbeitData>();
    public List<Personality> personalities = new List<Personality>();

    // -- Cocktail 관련 원본 데이터 --
    [Header("칵테일 데이터")]
    public List<CocktailRecipeJson> recipes = new List<CocktailRecipeJson>();

    // --- 기타 데이터 ---
    [Header("기타 데이터")]
    public List<ResourceData> goodsDatas = new List<ResourceData>();
    
    // --- 인테리어 관련 원본 데이터 ---
    [Header("인테리어 데이터")]
    public List<InteriorData> InteriorDatas = new List<InteriorData>();
    #endregion

    #region Runtime Data Lists (가공된 런타임 데이터)
    [Header("조합 데이터")]
    // Repositories에서 원본 데이터를 조합하여 생성한, 실제 게임 로직에서 사용될 데이터 리스트입니다.

    [SerializeField] public List<npc> npcs = new List<npc>();
    [SerializeField] public List<ConstructedBuilding> ConstructedBuildings = new List<ConstructedBuilding>();
    [SerializeField] public List<CocktailData> cocktails = new List<CocktailData>();
    
    // NPC Prefab 매핑: prefab_name -> GameObject prefab
    [Header("NPC Prefab 매핑")]
    public Dictionary<string, GameObject> npcPrefabDict = new Dictionary<string, GameObject>();
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

    #region Unity Lifecycle
    private void Awake()
    {
        InitializeSingleton();
        jsonDataHandler = new JsonDataHandler();
        InitializeDataFiles();
        LoadAllData();
        Debug.Log("DataManager 초기화 및 모든 데이터 로딩 완료.");
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

    private void InitializeDataFiles()
    {
        jsonDataHandler.InitializeFiles();
    }

    private void LoadAllData()
    {
        // 게임 시작에 필요한 모든 데이터를 로드합니다.
        LoadArbeitData();
        LoadPersonalityData();
        LoadBuildingData();
        LoadBuildingProductionData();
        LoadGoodsData();
        LoadBuildingUpgradeData();
        LoadConstructedBuildingProductions();
    }
    #endregion

    #region Data Loading Methods
    private void LoadArbeitData()
    // NPC의 상태 데이터(레벨, 경험치 등)를 JSON 파일에서 로드합니다.
    {
        arbeitDatas = jsonDataHandler.LoadArbeitData();
        Debug.Log($"ArbeitData 로드 완료: {arbeitDatas.Count}개");
    }

    private void LoadPersonalityData()
    {
        if (personalityDataSO != null)
        // NPC의 정의 데이터(성격, 고유 능력치 등)를 ScriptableObject에서 로드합니다.
        {
            personalities = personalityDataSO.personalities;
        }
        else
        {
            Debug.LogWarning("PersonalityDataSO가 DataManager에 할당되지 않았습니다. 빈 리스트로 초기화합니다.");
            personalities = new List<Personality>();
        }
    }

    private void LoadBuildingData()
    {
        // 건물의 기본 정의 데이터(이름, 타입, 레벨 등)를 ScriptableObject에서 로드합니다.
        if (buildingDataSO != null && buildingDataSO.buildings != null)
        {
            BuildingDatas = buildingDataSO.buildings;
            Debug.Log($"BuildingData {BuildingDatas.Count}개를 ScriptableObject에서 로드했습니다.");
        }
        else
        {
            Debug.LogWarning("BuildingDataSO가 DataManager에 할당되지 않았거나 비어있습니다. 빈 리스트로 초기화합니다.");
            BuildingDatas = new List<BuildingData>();
        }
    }

    private void LoadBuildingProductionData()
    {
        // 건물의 생산 정의 데이터(생산품, 생산 시간 등)를 ScriptableObject에서 로드합니다.
        if (buildingProductionInfoSO != null && buildingProductionInfoSO.productionInfos != null)
        {
            BuildingProductionInfos = buildingProductionInfoSO.productionInfos;
            Debug.Log($"BuildingProductionInfo {BuildingProductionInfos.Count}개를 ScriptableObject에서 로드했습니다.");
        }
        else
        {
            Debug.LogWarning("BuildingProductionInfoSO가 DataManager에 할당되지 않았거나 비어있습니다. 빈 리스트로 초기화합니다.");
            BuildingProductionInfos = new List<BuildingProductionInfo>();
        }
    }

    private void LoadGoodsData()
    {
        // 재화(Goods) 데이터를 Resources 폴더에서 로드합니다.
        ResourceData[] loadedGoods = Resources.LoadAll<ResourceData>(GoodsPath);
        goodsDatas.Clear();
        foreach (var goods in loadedGoods)
        {
            goodsDatas.Add(goods);
        }
    }

    private void LoadBuildingUpgradeData()
    {
        // 건물 업그레이드 데이터를 Resources 폴더에서 로드합니다.
        BuildingUpgradeData[] loadedUpgrades = Resources.LoadAll<BuildingUpgradeData>(BuildingUpgradePath);
        BuildingUpgradeDatas.Clear();
        foreach (var upgrade in loadedUpgrades)
        {
            BuildingUpgradeDatas.Add(upgrade);
        }
    }

    private void LoadConstructedBuildingProductions()
    {
        // 건설된 건물의 상태 데이터(생산 상태, 시간 등)를 JSON 파일에서 로드합니다.
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

        var productionDict = ConstructedBuildingProductions.ToDictionary(p => p.building_id);

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
        if (npcs == null || arbeitDatas == null) return;

        var arbeitDict = arbeitDatas.ToDictionary(a => a.part_timer_id);

        foreach (var npc in npcs)
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
        jsonDataHandler.SaveArbeitData(arbeitDatas);
    }
    #endregion

    #region Resource Query Methods
    public ResourceData GetResourceById(int id)
    {
        return goodsDatas.Find(r => r.resource_id == id);
    }

    public ResourceData GetResourceByName(string name)
    {
        return goodsDatas.Find(r => r.resource_name == name);
    }
    #endregion



    #region Building Production Query Methods
    public List<BuildingProductionInfo> GetBuildingProductionInfoList()
    {
        return BuildingProductionInfos;
    }

    /// <summary>
    /// 해당 건물 이름의 건물 생산 데이터 가져옴
    /// </summary>

    public List<BuildingProductionInfo> GetBuildingProductionInfoByType(string buildingType)
    {
        return BuildingProductionInfos.FindAll(data => data.building_type == buildingType);
    }
    #endregion

    #region Building Upgrade Methods

    public List<BuildingUpgradeData> GetBuildingUpgradeDataByType(string buildingType)
    {
        return BuildingUpgradeDatas.FindAll(data => data.building_type == buildingType);
    }

    public BuildingUpgradeData GetBuildingUpgradeDataByLevel(List<BuildingUpgradeData> upgradeDataList, int level)
    {
        return upgradeDataList.Find(data => data.level == level);
    }

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

    #endregion

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

    #region Cocktail Methods

    public CocktailData GetCocktailDataById(int cocktailId)
    {
        return cocktails.Find(data => data.Cocktail_ID == cocktailId);

    }

    public CocktailRecipeJson GetCocktailRecipeByCocktailId(int cocktailId)
    {
        return recipes.Find(data => data.CocktailId == cocktailId);
    }

    #endregion

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

    #region Cleanup
    private void CleanupResources()
    {
        if (Instance == this)
        {
            // Clear and nullify lists to free up memory
            BuildingDatas?.Clear();
            goodsDatas?.Clear();
            BuildingProductionInfos?.Clear();
            BuildingUpgradeDatas?.Clear();
            ConstructedBuildingProductions?.Clear();
            arbeitDatas?.Clear();
            personalities?.Clear();
            npcs?.Clear();

            BuildingDatas = null;
            goodsDatas = null;
            BuildingProductionInfos = null;
            BuildingUpgradeDatas = null;
            ConstructedBuildingProductions = null;
            ConstructedBuildings = null;
            arbeitDatas = null;
            personalities = null;
            npcs = null;

            Instance = null;
        }
    }
    #endregion
}
