using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Newtonsoft.Json;
using System.Linq;


public class DataManager : MonoBehaviour
{
    public static DataManager instance;

    public List<goodsData> goodsDatas = new List<goodsData>();
    // ScriptableObject로 관리되는 모든 건물 정의
    public List<BuildingData> BuildingDatas = new List<BuildingData>();
    // 건물 타입별 생산 정보 (JSON 또는 다른 방식으로 로드)
    public List<BuildingProductionInfo> BuildingProductionInfos = new List<BuildingProductionInfo>();
    // 현재 건설된 건물의 생산 상태 (플레이어 세이브 파일에서 로드)
    public List<ConstructedBuildingProduction> ConstructedBuildingProductions = new List<ConstructedBuildingProduction>();

    // BuildingRepository에서 가져온 통합된 건설 완료 건물 데이터
    public List<ConstructedBuilding> ConstructedBuildings { get; private set; }

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
    }
    public void Start()
    {
        // 게임 시작 시 건설된 건물 데이터 로드
        LoadConstructedBuildings();
    }

    public void GetGoodsData()
    {

    }

    /// <summary>
    /// BuildingRepository를 통해 현재 건설된 모든 건물 데이터를 가져와 리스트를 초기화
    /// </summary>
    public void LoadConstructedBuildings()
    {
        // BuildingRepository 인스턴스에서 건설된 건물 목록을 가져옵니다.
        ConstructedBuildings = BuildingRepository.Instance.GetConstructedBuildingsOnMainIsland();
    }
}
class Json
// 아이템 및 플레이어 데이터를 JSON 파일로 저장하고 불러오는 기능을 담당
{
    private string BuildingPass = "../Merge/Json/Building.json";

    public void ExistsFile()
    {
        //아이템 파일 초기화
        if (!File.Exists(BuildingPass))
        {
            File.WriteAllText(BuildingPass, "[]");
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