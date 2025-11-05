using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class BuildingRepository : MonoBehaviour
{
    public static BuildingRepository Instance { get; private set; }

    private DataManager _dataManager;
    private Dictionary<string, BuildingProductionInfo> _productionInfoDict;
    private Dictionary<int, ConstructedBuildingProduction> _productionStatusDict;

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
        // DataManager 인스턴스를 찾아서 캐싱합니다.
        _dataManager = DataManager.instance;
        InitializeDictionaries();
    }

    /// <summary>
    /// 빠른 조회를 위해 데이터를 딕셔너리 형태로 변환합니다.
    /// 이 함수는 데이터가 로드된 후 호출되어야 합니다.
    /// </summary>
    public void InitializeDictionaries()
    {
        _productionInfoDict = _dataManager.BuildingProductionInfos.ToDictionary(info => info.building_type);
        _productionStatusDict = _dataManager.ConstructedBuildingProductions.ToDictionary(status => status.building_id);
    }

    /// <summary>
    /// Main_Island에 건설된 모든 건물의 통합 데이터를 가져옵니다.
    /// </summary>
    /// <param name="mainIslandId">메인 섬의 ID</param>
    /// <returns>건설된 건물 정보 리스트</returns>
    public List<ConstructedBuilding> GetConstructedBuildingsOnMainIsland(int mainIslandId = 1) // 메인 섬 ID를 1로 가정
    {
        List<ConstructedBuilding> constructedBuildings = new List<ConstructedBuilding>();

        // 1. DataManager에서 Main_Island에 속한 건물(BuildingData)만 필터링합니다.
        var buildingsOnIsland = _dataManager.BuildingDatas.Where(b => b.island_id == mainIslandId);

        foreach (var buildingData in buildingsOnIsland)
        {
            // 2. 각 건물의 타입과 ID를 사용해 나머지 정보들을 딕셔너리에서 찾습니다.
            _productionInfoDict.TryGetValue(buildingData.building_Type, out var productionInfo);
            _productionStatusDict.TryGetValue(buildingData.building_id, out var productionStatus);

            // 3. 모든 정보를 취합하여 ConstructedBuilding 객체를 생성하고 리스트에 추가합니다.
            constructedBuildings.Add(new ConstructedBuilding(buildingData, productionInfo, productionStatus));
        }

        return constructedBuildings;
    }
}
