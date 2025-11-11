using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System.Linq;
using Newtonsoft.Json;

public class DataManager : MonoBehaviour
{
    #region Singleton
    public static DataManager instance;
    public static DataManager Instance => instance;
    #endregion

    #region Constants
    private const string GoodsPath = "Data/Goods";
    private const string BuildingUpgradePath = "Data/Building/BuildingUpgrade";
    #endregion

    #region Scriptable Object References
    [Header("데이터 에셋")]
    public PersonalityDataSO personalityDataSO;
    public BuildingDataSO buildingDataSO;
    public BuildingProductionInfoSO buildingProductionInfoSO;
    #endregion

    #region Goods Data
    public List<goodsData> goodsDatas = new List<goodsData>();
    #endregion

    #region Building Data
    // ScriptableObject로 관리되는 모든 건물 정의
    public List<BuildingData> BuildingDatas = new List<BuildingData>();
    public List<BuildingProductionInfo> BuildingProductionInfos = new List<BuildingProductionInfo>();
    public List<BuildingUpgradeData> BuildingUpgradeDatas = new List<BuildingUpgradeData>();
    
    // 현재 건설된 건물의 생산 상태 (플레이어 세이브 파일에서 로드)
    public List<ConstructedBuildingProduction> ConstructedBuildingProductions = new List<ConstructedBuildingProduction>();
    
    // BuildingRepository에서 가져온 통합된 건설 완료 건물 데이터
    public List<ConstructedBuilding> ConstructedBuildings { get; private set; }
    #endregion

    #region NPC/Arbeit Data
    public List<ArbeitData> arbeitDatas = new List<ArbeitData>();
    public List<Personality> personalities = new List<Personality>();
    public List<npc> npcs = new List<npc>();
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
    }

    public void Start()
    {
        // 게임 시작 시 건설된 건물 데이터 로드
        LoadConstructedBuildings();
    }

    private void OnApplicationQuit()
    {
        SaveNpcData();
        SaveConstructedBuildingProductions();
    }

    private void OnDestroy()
    {
        CleanupResources();
    }
    #endregion

    #region Initialization
    private void InitializeSingleton()
    {
        if (instance == null)
        {
            instance = this;
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
    {
        arbeitDatas = DataFile.loadArbeitData();
    }

    private void LoadPersonalityData()
    {
        if (personalityDataSO != null)
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
        goodsData[] loadedGoods = Resources.LoadAll<goodsData>(GoodsPath);
        goodsDatas.Clear();
        foreach (var goods in loadedGoods)
        {
            goodsDatas.Add(goods);
        }
    }

    private void LoadBuildingUpgradeData()
    {
        BuildingUpgradeData[] loadedUpgrades = Resources.LoadAll<BuildingUpgradeData>(BuildingUpgradePath);
        BuildingUpgradeDatas.Clear();
        foreach (var upgrade in loadedUpgrades)
        {
            BuildingUpgradeDatas.Add(upgrade);
        }
    }

    private void LoadConstructedBuildingProductions()
    {
        ConstructedBuildingProductions = DataFile.loadConstructedBuildingProductions();
        Debug.Log($"ConstructedBuildingProduction {ConstructedBuildingProductions.Count}개를 JSON에서 로드했습니다.");
    }
    #endregion

    #region NPC Management
    /// <summary>
    /// 현재 NPC 리스트를 JSON 파일에 저장합니다.
    /// </summary>
    public void SaveNpcData()
    {
        DataFile.saveNpcData(npcs);
    }

    public void LoadNPC()
    {
        foreach (var npc in npcs)
        {
            Debug.Log($"id: {npc.part_timer_id} name: {npc.part_timer_name} race: {npc.race} " +
                     $"level: {npc.level} exp: {npc.exp} employment_state: {npc.employment_state}, " +
                     $"fatigue: {npc.fatigue} daily_wage: {npc.daily_wage} need_rest: {npc.need_rest} " +
                     $"total_ability: {npc.total_ability} personality_id: {npc.personality_id} " +
                     $"personality_name: {npc.personality_name} description: {npc.description} " +
                     $"specificity: {npc.specificity} serving_ability: {npc.serving_ability} " +
                     $"cooking_ability: {npc.cooking_ability} cleaning_ability: {npc.cleaning_ability}");
        }
    }
    #endregion

    #region Building Management
    /// <summary>
    /// BuildingRepository를 통해 현재 건설된 모든 건물 데이터를 가져와 리스트를 초기화
    /// </summary>
    public void LoadConstructedBuildings()
    {
        // BuildingRepository 인스턴스에서 건설된 건물 목록을 가져옵니다.
        // TODO: BuildingRepository 구현 후 주석 해제
        // ConstructedBuildings = BuildingRepository.Instance.GetConstructedBuildingsOnMainIsland();
    }

    /// <summary>
    /// 현재 건설된 건물 생산 상태를 JSON 파일에 저장합니다.
    /// </summary>
    public void SaveConstructedBuildingProductions()
    {
        DataFile.saveConstructedBuildingProductions(ConstructedBuildingProductions);
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

    #region Cleanup
    private void CleanupResources()
    {
        if (instance == this)
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

            instance = null;
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
    private string NpcDataPass = "Assets/Scripts/Merge/Datable/Json/NpcData.json";
    private string BuildingPass = "Assets/Scripts/Merge/Datable/Json/BuildingData.json";
    private string ConstructedBuildingProductionPass = "Assets/Scripts/Merge/Datable/Json/ConstructedBuildingProduction.json";

    public void ExistsFile()
    {
        // 파일이 없으면 빈 배열로 초기화
        if (!File.Exists(BuildingPass))
        {
            File.WriteAllText(BuildingPass, "[]");
        }

        if (!File.Exists(ArbeitDataPass))
        {
            File.WriteAllText(ArbeitDataPass, "[]");
        }

        if (!File.Exists(NpcDataPass))
        {
            File.WriteAllText(NpcDataPass, "[]");
        }

        if (!File.Exists(ConstructedBuildingProductionPass))
        {
            File.WriteAllText(ConstructedBuildingProductionPass, "[]");
        }
    }

    #region Building Data Methods
    public void saveBuildingData(List<ConstructedBuilding> constructedBuildings)
    {
        string JsonData = JsonConvert.SerializeObject(constructedBuildings, Formatting.Indented);
        File.WriteAllText(BuildingPass, JsonData);
    }

    public List<ConstructedBuilding> loadBuildingData()
    {
        if (!File.Exists(BuildingPass))
        {
            Debug.LogWarning("BuildingData 파일이 존재하지 않습니다. 빈 리스트를 반환합니다.");
            return new List<ConstructedBuilding>();
        }

        string JsonData = File.ReadAllText(BuildingPass);
        List<ConstructedBuilding> buildingsFromData = JsonConvert.DeserializeObject<List<ConstructedBuilding>>(JsonData);
        return buildingsFromData ?? new List<ConstructedBuilding>();
    }

    public void updateBuildingData(List<ConstructedBuilding> constructedBuildings)
    {
        // 1. JSON 파일에서 기존 건물 데이터를 읽어옴
        List<ConstructedBuilding> buildingsFromData = loadBuildingData();

        // 2. 기존 데이터를 ID를 키로 하는 Dictionary로 변환
        var buildingsDict = buildingsFromData.ToDictionary(b => b.Id);

        // 3. 현재 게임 내 건물 데이터를 순회하며 업데이트/추가
        foreach (var currentBuilding in constructedBuildings)
        {
            if (buildingsDict.ContainsKey(currentBuilding.Id))
            {
                // 3-1. ID가 존재하면 기존 건물 정보를 현재 정보로 업데이트
                buildingsDict[currentBuilding.Id] = currentBuilding;
            }
            else
            {
                // 3-2. ID가 없으면 새로 건설된 건물이므로 Dictionary에 추가
                buildingsDict.Add(currentBuilding.Id, currentBuilding);
            }
        }

        // 4. 현재 게임 데이터에 없는 건물(삭제된 건물)을 찾아 제거
        var currentBuildingIds = new HashSet<int>(constructedBuildings.Select(b => b.Id));
        var buildingsToRemove = buildingsDict.Keys.Where(id => !currentBuildingIds.Contains(id)).ToList();
        foreach (var id in buildingsToRemove)
        {
            buildingsDict.Remove(id);
        }

        // 5. 업데이트된 데이터를 다시 JSON 형식으로 변환하여 파일에 저장
        saveBuildingData(buildingsDict.Values.ToList());
    }
    #endregion

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
    #endregion

    #region NPC Data Methods
    public void saveNpcData(List<npc> npcs)
    {
        string jsonData = JsonConvert.SerializeObject(npcs, Formatting.Indented);
        File.WriteAllText(NpcDataPass, jsonData);
    }
    #endregion
}
#endregion
