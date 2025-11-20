using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Unity.VisualScripting;
using System;

[Serializable]
public class BuildingRepository : MonoBehaviour
{
    public static BuildingRepository Instance { get; private set; }

    private DataManager _dataManager;
    private readonly Dictionary<int, BuildingData> _buildingDataDict = new Dictionary<int, BuildingData>();
    private readonly Dictionary<string, BuildingProductionInfo> _productionInfoDict = new Dictionary<string, BuildingProductionInfo>();
    
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
        // DataManager 인스턴스를 가져옵니다.
        _dataManager = DataManager.Instance;
        StartCoroutine(WaitForDataAndInitialize());
    }

    private IEnumerator WaitForDataAndInitialize()
    {
        // DataManager가 건물 생성에 필요한 모든 데이터를 로드할 때까지 대기합니다.
        yield return new WaitUntil(() =>
            _dataManager != null &&
            _dataManager.BuildingDatas != null &&
            _dataManager.BuildingDatas.Count > 0 &&
            _dataManager.BuildingProductionInfos != null &&
            _dataManager.BuildingProductionInfos.Count > 0 &&
            _dataManager.ConstructedBuildingProductions != null
        );

        // 데이터가 준비되면, 딕셔너리를 초기화하고 건물 리스트를 생성합니다.
        InitializeDictionaries();
        PopulateConstructedBuildingsList();
    }

    /// <summary>
    /// BuildingData와 BuildingProductionInfo를 Dictionary로 변환하여 빠른 검색을 가능하게 합니다.
    /// </summary>
    public void InitializeDictionaries()
    {
        if (_dataManager.BuildingDatas != null && _dataManager.BuildingDatas.Count > 0)
        {
            // 딕셔너리를 초기화하기 전에 비워줍니다.
            _buildingDataDict.Clear();
            // ToDictionary()는 중복 키가 있을 때 예외를 발생시키므로, foreach를 사용하여 수동으로 딕셔너리를 채웁니다.
            // 이를 통해 데이터 중복 문제를 방어하고, 어떤 데이터가 문제인지 로그로 남길 수 있습니다.
            foreach (var buildingData in _dataManager.BuildingDatas)
            {
                if (buildingData == null) continue;

                // 딕셔너리에 이미 해당 building_id가 키로 존재하는지 확인합니다.
                if (!_buildingDataDict.ContainsKey(buildingData.building_id))
                {
                    // 키가 존재하지 않으면 딕셔너리에 추가합니다.
                    _buildingDataDict.Add(buildingData.building_id, buildingData);
                }
                else
                {
                    // 이미 키가 존재한다면, 이는 데이터가 중복되었다는 의미입니다.
                    // 게임을 멈추는 대신, 어떤 에셋의 ID가 중복되었는지 경고 로그를 출력하여 개발자가 문제를 인지하고 수정할 수 있도록 돕습니다.
                    Debug.LogWarning($"[Data Duplication] BuildingData ID '{buildingData.building_id}'가 중복됩니다. 에셋: '{buildingData.name}'");
                }
            }
            Debug.Log($"BuildingData Dictionary 초기화 완료: {_buildingDataDict.Count}개");
        }

        if (_dataManager.BuildingProductionInfos != null && _dataManager.BuildingProductionInfos.Count > 0)
        {
            _productionInfoDict.Clear(); // 딕셔너리 초기화
            // BuildingData와 마찬가지로 BuildingProductionInfo의 중복 키 문제도 방어합니다.
            _productionInfoDict.Clear();
            foreach (var productionInfo in _dataManager.BuildingProductionInfos)
            {
                if (productionInfo == null || string.IsNullOrEmpty(productionInfo.building_type)) continue;

                // building_type을 키로 사용하며 중복을 확인합니다.
                if (!_productionInfoDict.ContainsKey(productionInfo.building_type))
                {
                    // 키가 없으면 추가합니다.
                    _productionInfoDict.Add(productionInfo.building_type, productionInfo);
                }
                else
                {
                    // 중복된 building_type이 있을 경우 경고를 출력합니다.
                    Debug.LogWarning($"[Data Duplication] BuildingProductionInfo building_type '{productionInfo.building_type}'이 중복됩니다. 에셋: '{productionInfo.name}'");
                }
            }
            Debug.Log($"BuildingProductionInfo Dictionary 초기화 완료: {_productionInfoDict.Count}개");
        }
    }

    /// <summary>
    /// 로드된 원본 데이터(BuildingData, BuildingProductionInfo, ConstructedBuildingProduction)를 조합하여
    /// 실제 게임에서 사용될 ConstructedBuilding 객체 리스트를 생성하고, DataManager에 저장합니다.
    /// </summary>
    private void PopulateConstructedBuildingsList()
    {
        _dataManager.ConstructedBuildings = GetConstructedBuildings();
        Debug.Log($"건설된 건물 {_dataManager.ConstructedBuildings.Count}개를 생성했습니다.");
    }

    /// <summary>
    /// 저장된 건물 상태(ConstructedBuildingProduction)를 기반으로,
    /// 건물의 정의 데이터(BuildingData, BuildingProductionInfo)를 조합하여
    /// 완전한 'ConstructedBuilding' 런타임 객체 리스트를 생성하여 반환합니다.
    /// </summary>
    public List<ConstructedBuilding> GetConstructedBuildings()
    {
        List<ConstructedBuilding> constructedBuildings = new List<ConstructedBuilding>();

        if (_dataManager.ConstructedBuildingProductions == null ||
            _buildingDataDict == null ||
            _productionInfoDict == null)
        {
            Debug.LogError("Building data or Production data is not loaded yet.");
            return constructedBuildings;
        }

        foreach (var production in _dataManager.ConstructedBuildingProductions)
        {
            // 1. 건물의 상태(production) 데이터에서 ID를 가져와, 건물의 기본 정의(BuildingData)를 딕셔너리에서 찾습니다.
            if (_buildingDataDict.TryGetValue(production.building_id, out BuildingData buildingData))
            {
                // 2. 건물의 타입(building_Type)을 사용하여, 건물의 생산 정의(BuildingProductionInfo)를 딕셔너리에서 찾습니다.
                //    생산 기능이 없는 건물일 경우 productionInfo는 null이 될 수 있습니다.
                BuildingProductionInfo productionInfo = null;
                _productionInfoDict.TryGetValue(buildingData.building_Type, out productionInfo);

                // 3. 세 종류의 데이터를 모두 조합하여 완전한 ConstructedBuilding 객체를 생성합니다.
                var constructedBuilding = new ConstructedBuilding(
                    buildingData,
                    productionInfo,
                    production
                );

                constructedBuildings.Add(constructedBuilding);
            }
            else
            {
                Debug.LogWarning($"ConstructedBuildingProduction의 building_id '{production.building_id}'에 해당하는 BuildingData를 찾을 수 없습니다.");
            }
        }

        return constructedBuildings;
    }

    /// <summary>
    /// Main_Island에 건설된 모든 건물의 통합 데이터를 가져옴 (기존 호환성 유지)
    /// </summary>
    /// <param name="mainIslandId">메인 섬의 ID</param>
    /// <returns>건설된 건물 정보 리스트</returns>
    public List<ConstructedBuilding> GetConstructedBuildingsOnMainIsland(int mainIslandId = 1)
    {
        if (_dataManager.ConstructedBuildings == null)
        {
            Debug.LogWarning("ConstructedBuildings가 초기화되지 않았습니다.");
            return new List<ConstructedBuilding>();
        }

        // 특정 섬에 속한 건물만 필터링
        return _dataManager.ConstructedBuildings.Where(b =>
            _buildingDataDict.ContainsKey(b.Id) &&
            _buildingDataDict[b.Id].island_id == mainIslandId
        ).ToList();
    }

    /// <summary>
    /// 특정 ID의 건설된 건물을 찾습니다.
    /// </summary>
    public ConstructedBuilding GetConstructedBuildingById(int buildingId)
    {
        if (_dataManager.ConstructedBuildings == null)
        {
            Debug.LogWarning("ConstructedBuildings가 초기화되지 않았습니다.");
            return null;
        }

        return _dataManager.ConstructedBuildings.Find(b => b.Id == buildingId);
    }

    /// <summary>
    /// 특정 타입의 건설된 건물들을 찾습니다.
    /// </summary>
    public List<ConstructedBuilding> GetConstructedBuildingsByType(string buildingType)
    {
        if (_dataManager.ConstructedBuildings == null)
        {
            Debug.LogWarning("ConstructedBuildings가 초기화되지 않았습니다.");
            return new List<ConstructedBuilding>();
        }

        return _dataManager.ConstructedBuildings.FindAll(b => b.Type == buildingType);
    }

    /// <summary>
    /// 현재 생산 중인 건물들을 찾습니다.
    /// </summary>
    public List<ConstructedBuilding> GetProducingBuildings()
    {
        if (_dataManager.ConstructedBuildings == null)
        {
            Debug.LogWarning("ConstructedBuildings가 초기화되지 않았습니다.");
            return new List<ConstructedBuilding>();
        }

        return _dataManager.ConstructedBuildings.FindAll(b => b.IsProducing);
    }

    /// <summary>
    /// 새로운 건물을 건설 목록에 추가합니다.
    /// </summary>
    public void AddConstructedBuilding(int buildingId)
    {
        // 1. BuildingData 확인
        if (!_buildingDataDict.TryGetValue(buildingId, out BuildingData buildingData))
        {
            Debug.LogError($"BuildingData ID '{buildingId}'를 찾을 수 없습니다.");
            return;
        }

        // 2. 새로운 ConstructedBuildingProduction 생성
        ConstructedBuildingProduction newProduction = new ConstructedBuildingProduction
        {
            building_id = buildingId,
            last_production_time = System.DateTime.Now,
            next_production_time = System.DateTime.Now,
            is_producing = false
        };

        // 3. DataManager의 ConstructedBuildingProductions에 추가
        _dataManager.ConstructedBuildingProductions.Add(newProduction);

        // 4. ConstructedBuildings 리스트 갱신
        PopulateConstructedBuildingsList();

        Debug.Log($"건물 '{buildingData.Building_Name}' (ID: {buildingId})을 건설 목록에 추가했습니다.");
    }

    /// <summary>
    /// 건물을 건설 목록에서 제거합니다.
    /// </summary>
    public void RemoveConstructedBuilding(int buildingId)
    {
        // 1. ConstructedBuildingProductions에서 제거
        var production = _dataManager.ConstructedBuildingProductions.Find(p => p.building_id == buildingId);
        if (production != null)
        {
            _dataManager.ConstructedBuildingProductions.Remove(production);

            // 2. ConstructedBuildings 리스트 갱신
            PopulateConstructedBuildingsList();

            Debug.Log($"건물 ID '{buildingId}'를 건설 목록에서 제거했습니다.");
        }
        else
        {
            Debug.LogWarning($"건물 ID '{buildingId}'를 건설 목록에서 찾을 수 없습니다.");
        }
    }

    /// <summary>
    /// 건물의 생산 상태를 업데이트합니다.
    /// </summary>
    public void UpdateBuildingProductionStatus(int buildingId, bool isProducing, System.DateTime? nextProductionTime = null)
    {
        var production = _dataManager.ConstructedBuildingProductions.Find(p => p.building_id == buildingId);
        if (production != null)
        {
            production.is_producing = isProducing;
            production.last_production_time = System.DateTime.Now;

            if (nextProductionTime.HasValue)
            {
                production.next_production_time = nextProductionTime.Value;
            }

            // ConstructedBuildings 리스트 갱신
            PopulateConstructedBuildingsList();

            Debug.Log($"건물 ID '{buildingId}'의 생산 상태를 업데이트했습니다. 생산 중: {isProducing}");
        }
        else
        {
            Debug.LogWarning($"건물 ID '{buildingId}'를 찾을 수 없습니다.");
        }
    }

    /// <summary>
    /// 모든 건설된 건물 정보를 로그로 출력합니다. (디버깅용)
    /// </summary>
    public void LogConstructedBuildings()
    {
        if (_dataManager.ConstructedBuildings == null || _dataManager.ConstructedBuildings.Count == 0)
        {
            Debug.Log("건설된 건물이 없습니다.");
            return;
        }

        foreach (var building in _dataManager.ConstructedBuildings)
        {
            Debug.Log($"ID: {building.Id}, Name: {building.Name}, Type: {building.Type}, " +
                     $"Level: {building.Level}, IsProducing: {building.IsProducing}, " +
                     $"ResourceId: {building.ProductionResourceId}, OutputAmount: {building.ProductionOutputAmount}, " +
                     $"ProductionTime: {building.BaseProductionTimeMinutes}min, " +
                     $"LastProduction: {building.LastProductionTime}, NextProduction: {building.NextProductionTime}");
        }
    }
}
