using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System.Linq;
using Newtonsoft.Json;


public class DataManager : MonoBehaviour
{
    public static DataManager instance;
    public static DataManager Instance => instance; // Added from ex.cs

    [Header("데이터 에셋")]
    public PersonalityDataSO personalityDataSO; // 인스펙터에서 할당

    // Constants for loading from Resources (from ex.cs)
    private const string BuildingPath = "Data/Building";
    private const string GoodsPath = "Data/Goods";
    private const string BuildingProductionPath = "Data/Building/BuildingProduction";
    private const string BuildingUpgradePath = "Data/Building/BuildingUpgrade";

    public List<goodsData> goodsDatas = new List<goodsData>();
    // ScriptableObject로 관리되는 모든 건물 정의
    public List<BuildingData> BuildingDatas = new List<BuildingData>();
    public List<BuildingProductionInfo> BuildingProductionInfos = new List<BuildingProductionInfo>();
    public List<BuildingProductionData> BuildingProductionDatas = new List<BuildingProductionData>(); // Replaced BuildingProductionInfos
    public List<BuildingUpgradeData> BuildingUpgradeDatas = new List<BuildingUpgradeData>(); // Added from ex.cs
    // 현재 건설된 건물의 생산 상태 (플레이어 세이브 파일에서 로드)
    public List<ConstructedBuildingProduction> ConstructedBuildingProductions = new List<ConstructedBuildingProduction>();

    // BuildingRepository에서 가져온 통합된 건설 완료 건물 데이터
    public List<ConstructedBuilding> ConstructedBuildings { get; private set; }

    public List<ArbeitData> arbeitDatas = new List<ArbeitData>();
    public List<Personality> personalities = new List<Personality>();

    public List<npc> npcs = new List<npc>();

    private Json DataFile = new Json();

    [Space(2)]
    [Header("섬/자원 현황")]
    public int wood = 0;
    public int money = 0;
    [Header("바 현재 선호도/바 현재 레벨")]
    public float storeFavor = 100f;
    public int barLevel = 1;

    private void Awake()
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
        DataFile.ExistsFile();
        LoadArbeitData();
        LoadPersonalityData();
    }
    public void Start()
    {
        // 게임 시작 시 건설된 건물 데이터 로드
        LoadConstructedBuildings();
    }

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
            Debug.LogError("PersonalityDataSO가 DataManager에 할당되지 않았습니다.");
        }
    }

    /// <summary>
    /// 현재 NPC 리스트를 JSON 파일에 저장합니다.
    /// </summary>
    public void SaveNpcData()
    {
        DataFile.saveNpcData(npcs);
    }

    public void GetGoodsData()
    {
        //npcs = ArbeitRepository.Instance.
    }

    private void OnApplicationQuit()
    {
        SaveNpcData();
    }
    /// <summary>
    /// BuildingRepository를 통해 현재 건설된 모든 건물 데이터를 가져와 리스트를 초기화
    /// </summary>
    public void LoadConstructedBuildings()
    {
        // BuildingRepository 인스턴스에서 건설된 건물 목록을 가져옵니다.
        //ConstructedBuildings = BuildingRepository.Instance.GetConstructedBuildingsOnMainIsland();
    }

    public void LoadNPC()
    {
        foreach (var npc in npcs)
        {
            Debug.Log($"id: {npc.part_timer_id} name: {npc.part_timer_name} race: {npc.race} level: {npc.level} exp: {npc.exp} employment_state: {npc.employment_state}, fatigue: {npc.fatigue} daily_wage: {npc.daily_wage} need_rest: {npc.need_rest} total_ability: {npc.total_ability} personality_id: {npc.personality_id} personality_name: {npc.personality_name} description: {npc.description} specificity: {npc.specificity} serving_ability: {npc.serving_ability} cooking_ability: {npc.cooking_ability} cleaning_ability: {npc.cleaning_ability}");
        }
    }

    // Methods from ex.cs
    public goodsData GetResourceById(int id)
    {
        return goodsDatas.Find(r => r.id == id);
    }

    public List<BuildingProductionData> GetBuildingProductionDataList()
    {
        return BuildingProductionDatas;
    }

    public goodsData GetResourceByName(string name)
    {
        return goodsDatas.Find(r => r.goodsName == name);
    }

    // 해당 건물 이름의 건물 생산 데이터 가져옴
    public List<BuildingProductionData> GetBuildingProductionDataByType(string buildingType)
    {
        return BuildingProductionDatas.FindAll(data => data.building_type == buildingType);
    }

    private void OnDestroy() // From ex.cs
    {
        if (instance == this)
        {
            // Clear and nullify lists to free up memory
            BuildingDatas.Clear();
            goodsDatas.Clear();
            BuildingProductionDatas.Clear();
            BuildingUpgradeDatas.Clear(); // Clear new list

            BuildingDatas = null;
            goodsDatas = null;
            BuildingProductionDatas = null;
            BuildingUpgradeDatas = null; // Nullify new list

            // Also clear other lists if they are not managed by other systems
            ConstructedBuildingProductions.Clear();
            ConstructedBuildings = null; // Assuming this is managed elsewhere or needs explicit nulling
            arbeitDatas.Clear();
            personalities.Clear();
            npcs.Clear();

            ConstructedBuildingProductions = null;
            arbeitDatas = null;
            personalities = null;
            npcs = null;

            instance = null;
        }
    }
}
class Json
// 아이템 및 플레이어 데이터를 JSON 파일로 저장하고 불러오는 기능을 담당
{
    private string ArbeitDataPass = "Assets/Scripts/Merge/Datable/Json/ArbeitData.json";
    private string NpcDataPass = "Assets/Scripts/Merge/Datable/Json/NpcData.json";
    private string BuildingPass = "Assets/Scripts/Merge/Datable/Json/BuildingData.json";

    public void ExistsFile()
    {
        //아이템 파일 초기화
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

    }

    public void saveBuildingData(List<ConstructedBuilding> constructedBuildings)
    {
        string JsonData = JsonConvert.SerializeObject(constructedBuildings);
        File.WriteAllText(BuildingPass, JsonData);
    }

    public List<ConstructedBuilding> loadBuildingData()
    {
        string JsonData = File.ReadAllText(BuildingPass);
        List<ConstructedBuilding> buildinsFromData = JsonConvert.DeserializeObject<List<ConstructedBuilding>>(JsonData);
        return buildinsFromData;
    }

    public List<ArbeitData> loadArbeitData()
    {
        string jsonData = File.ReadAllText(ArbeitDataPass);
        List<ArbeitData> arbeitDataList = JsonConvert.DeserializeObject<List<ArbeitData>>(jsonData);
        return arbeitDataList ?? new List<ArbeitData>();
    }

    public void saveNpcData(List<npc> npcs)
    {
        string jsonData = JsonConvert.SerializeObject(npcs, Formatting.Indented);
        File.WriteAllText(NpcDataPass, jsonData);
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
}