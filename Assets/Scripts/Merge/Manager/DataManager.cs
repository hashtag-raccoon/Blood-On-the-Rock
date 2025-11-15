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
    private const string GoodsPath = "Data/Goods";
    private const string BuildingUpgradePath = "Data/Building/BuildingUpgradeData";
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

    // --- 기타 데이터 ---
    [Header("기타 데이터")]
    public List<goodsData> goodsDatas = new List<goodsData>();
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
    private Json DataFile = new Json();
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        InitializeSingleton();
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
        DataFile.ExistsFile();
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
        arbeitDatas = DataFile.loadArbeitData();
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
        goodsData[] loadedGoods = Resources.LoadAll<goodsData>(GoodsPath);
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
        ConstructedBuildingProductions = DataFile.loadConstructedBuildingProductions();
        Debug.Log($"ConstructedBuildingProduction {ConstructedBuildingProductions.Count}개를 JSON에서 로드했습니다.");
    }
    #endregion

    #region Data Saving Methods
    /// <summary>
    /// 현재 건설된 건물 생산 상태를 JSON 파일에 저장합니다.
    /// </summary>
    public void SaveConstructedBuildingProductions()
    {
        DataFile.saveConstructedBuildingProductions(ConstructedBuildingProductions);
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
        DataFile.saveArbeitData(arbeitDatas);
    }
    #endregion

    #region Resource Query Methods
    public goodsData GetResourceById(int id)
    {
        return goodsDatas.Find(r => r.id == id);
    }

    public goodsData GetResourceByName(string name)
    {
        return goodsDatas.Find(r => r.goodsName == name);
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

#region JSON Handler Class
/// <summary>
/// 아이템 및 플레이어 데이터를 JSON 파일로 저장하고 불러오는 기능을 담당
/// </summary>
class Json
{
    private string ArbeitDataPass = "Assets/Scripts/Merge/Datable/Json/ArbeitData.json";
    private string ConstructedBuildingProductionPass = "Assets/Scripts/Merge/Datable/Json/ConstructedBuildingProduction.json";

    public void ExistsFile()
    {
        if (!File.Exists(ArbeitDataPass))
        {
            File.WriteAllText(ArbeitDataPass, "[]");
        }

        if (!File.Exists(ConstructedBuildingProductionPass))
        {
            File.WriteAllText(ConstructedBuildingProductionPass, "[]");
        }
    }

    #region Constructed Building Production Methods
    public void saveConstructedBuildingProductions(List<ConstructedBuildingProduction> productions)
    {
        string jsonData = JsonConvert.SerializeObject(productions, Formatting.Indented);
        File.WriteAllText(ConstructedBuildingProductionPass, jsonData);
        Debug.Log($"ConstructedBuildingProduction {productions.Count}개를 저장했습니다.");
    }

    public List<ConstructedBuildingProduction> loadConstructedBuildingProductions()
    {
        if (!File.Exists(ConstructedBuildingProductionPass))
        {
            Debug.LogWarning("ConstructedBuildingProduction 파일이 존재하지 않습니다. 빈 리스트를 반환합니다.");
            return new List<ConstructedBuildingProduction>();
        }

        string jsonData = File.ReadAllText(ConstructedBuildingProductionPass);
        List<ConstructedBuildingProduction> productions = JsonConvert.DeserializeObject<List<ConstructedBuildingProduction>>(jsonData);
        return productions ?? new List<ConstructedBuildingProduction>();
    }
    #endregion

    #region Arbeit Data Methods
    public List<ArbeitData> loadArbeitData()
    {
        if (!File.Exists(ArbeitDataPass))
        {
            Debug.LogWarning("ArbeitData 파일이 존재하지 않습니다. 빈 리스트를 반환합니다.");
            return new List<ArbeitData>();
        }

        string jsonData = File.ReadAllText(ArbeitDataPass);
        List<ArbeitData> arbeitDataList = JsonConvert.DeserializeObject<List<ArbeitData>>(jsonData);
        return arbeitDataList ?? new List<ArbeitData>();
    }

    /// <summary>
    /// ArbeitData 리스트를 JSON 파일로 저장합니다.
    /// 이 메서드는 NPC의 레벨, 경험치 등 게임 플레이 중에 변경된 '상태' 데이터를 저장하는 데 사용됩니다.
    /// </summary>
    /// <param name="arbeitData">저장할 ArbeitData 리스트</param>
    public void saveArbeitData(List<ArbeitData> arbeitData)
    {
        string jsonData = JsonConvert.SerializeObject(arbeitData, Formatting.Indented);
        File.WriteAllText(ArbeitDataPass, jsonData);
        Debug.Log($"ArbeitData {arbeitData.Count}개를 저장했습니다.");
    }
    #endregion

}
#endregion

