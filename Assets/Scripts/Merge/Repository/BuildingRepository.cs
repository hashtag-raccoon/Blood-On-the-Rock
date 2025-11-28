using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

[Serializable]
public class BuildingRepository : MonoBehaviour, IRepository
{
    private static BuildingRepository _instance;
    public static BuildingRepository Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<BuildingRepository>();
            }
            return _instance;
        }
    }
    public bool IsInitialized { get; private set; } = false;

    private const string BuildingUpgradePath = "Data/Building/BuildingUpgradeData";


    [Header("데이터 에셋 (SO)")]
    [Tooltip("건물의 고정 정보(ID, 이름, 레벨, 아이콘 등)를 담고 있는 ScriptableObject")]
    [SerializeField] private BuildingDataSO buildingDataSO;
    [Tooltip("건물의 생산 관련 고정 정보(생산품, 시간 등)를 담고 있는 ScriptableObject")]
    [SerializeField] private BuildingProductionInfoSO buildingProductionInfoSO;

    private List<ConstructedBuilding> _constructedBuildings = new List<ConstructedBuilding>();
    private List<BuildingUpgradeData> _buildingUpgradeDatas = new List<BuildingUpgradeData>();

    private readonly Dictionary<int, BuildingData> _buildingDataDict = new Dictionary<int, BuildingData>();
    private readonly Dictionary<string, BuildingProductionInfo> _productionInfoDict = new Dictionary<string, BuildingProductionInfo>();

    [SerializeField] private Grid grid;
    private void Awake()
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

    private void Start()
    {
        // DataManager가 초기화를 요청할 때까지 대기합니다.
        DataManager.Instance.RegisterRepository(this);

        grid = FindObjectOfType<Grid>();
        if (grid == null)
        {
            Debug.Log("Scene에서 Grid를 찾을 수 없음.");
        }
    }

    public void Initialize()
    {
        // DataManager가 데이터를 전달하는 Initialize(data)를 호출하므로 이 메서드는 비워둡니다.
    }

    public void Initialize(List<ConstructedBuildingProduction> constructedBuildingProductions, List<ConstructedBuildingPos> constructedBuildingPositions) // 오버로딩된 메서드
    {
        LoadBuildingUpgradeData();
        InitializeDictionaries();
        CreateConstructedBuildingsList(constructedBuildingProductions, constructedBuildingPositions);
        IsInitialized = true;
        Debug.Log("BuildingRepository 초기화 완료.");
    }

    /// <summary>
    /// 건물 업그레이드 데이터를 Resources 폴더에서 로드합니다.
    /// </summary>
    private void LoadBuildingUpgradeData()
    {
        BuildingUpgradeData[] loadedUpgrades = Resources.LoadAll<BuildingUpgradeData>(BuildingUpgradePath);
        _buildingUpgradeDatas.Clear();
        foreach (var upgrade in loadedUpgrades)
        {
            _buildingUpgradeDatas.Add(upgrade);
        }
    }
    /// <summary>
    /// BuildingData와 BuildingProductionInfo를 Dictionary로 변환하여 빠른 검색을 가능하게 합니다.
    /// </summary>
    private void InitializeDictionaries()
    {
        if (buildingDataSO != null && buildingDataSO.buildings != null)
        {
            var buildingDatas = buildingDataSO.buildings;
            // 딕셔너리를 초기화하기 전에 비워줍니다.
            _buildingDataDict.Clear();
            // ToDictionary()는 중복 키가 있을 때 예외를 발생시키므로, foreach를 사용하여 수동으로 딕셔너리를 채웁니다.
            // 이를 통해 데이터 중복 문제를 방어하고, 어떤 데이터가 문제인지 로그로 남길 수 있습니다.
            foreach (var buildingData in buildingDatas)
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

        if (buildingProductionInfoSO != null && buildingProductionInfoSO.productionInfos != null)
        {
            var productionInfos = buildingProductionInfoSO.productionInfos;
            _productionInfoDict.Clear(); // 딕셔너리 초기화
            // BuildingData와 마찬가지로 BuildingProductionInfo의 중복 키 문제도 방어합니다.
            _productionInfoDict.Clear();
            foreach (var productionInfo in productionInfos)
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
    private void CreateConstructedBuildingsList(List<ConstructedBuildingProduction> constructedBuildingProductions, List<ConstructedBuildingPos> constructedBuildingPositions)
    {
        _constructedBuildings.Clear();

        if (constructedBuildingProductions == null || _buildingDataDict == null || _productionInfoDict == null)
        {
            Debug.LogError("Building data or Production data is not loaded yet.");
            return;
        }
        //
        foreach (var item in constructedBuildingProductions.Zip(constructedBuildingPositions, (Production, Pos) => new { production = Production, pos = Pos }))
        {
            // instance_id가 일치하는지 확인
            if (item.production.instance_id != item.pos.instance_id)
            {
                Debug.LogWarning($"ConstructedBuildingProduction과 ConstructedBuildingPos의 instance_id가 일치하지 않습니다. Production: {item.production.instance_id}, Pos: {item.pos.instance_id}");
                continue;
            }

            // instance_id에서 building_type_id 추출 (앞자리)
            int buildingTypeId = (int)(item.production.instance_id / 10000000000000L);

            // 1. 건물 타입 ID로 건물의 기본 정의(BuildingData)를 딕셔너리에서 찾습니다.
            if (_buildingDataDict.TryGetValue(buildingTypeId, out BuildingData buildingData))
            {
                // 2. 건물의 타입(building_Type)을 사용하여, 건물의 생산 정의(BuildingProductionInfo)를 딕셔너리에서 찾습니다.
                //    생산 기능이 없는 건물일 경우 productionInfo는 null이 될 수 있습니다.
                BuildingProductionInfo productionInfo = null;
                _productionInfoDict.TryGetValue(buildingData.building_Type, out productionInfo);

                // 3. 세 종류의 데이터를 모두 조합하여 완전한 ConstructedBuilding 객체를 생성합니다.
                var constructedBuilding = new ConstructedBuilding(
                    buildingData,
                    productionInfo,
                    item.production,
                    item.pos
                );

                _constructedBuildings.Add(constructedBuilding);
            }
            else
            {
                Debug.LogWarning($"building_type_id '{buildingTypeId}' (instance_id: {item.production.instance_id})에 해당하는 BuildingData를 찾을 수 없습니다.");
            }
        }
        Debug.Log($"건설된 건물 {_constructedBuildings.Count}개를 생성했습니다.");
    }

    /// <summary>
    /// Repository가 생성한 'ConstructedBuilding' 런타임 객체 리스트를 반환합니다.
    /// </summary>
    public List<ConstructedBuilding> GetConstructedBuildings()
    {
        return _constructedBuildings;
    }
    /// <summary>
    /// 모든 건물 원본 데이터(BuildingData) 리스트를 반환합니다. (UI 생성용)
    /// </summary>
    /// <returns>모든 BuildingData의 리스트</returns>
    public List<BuildingData> GetAllBuildingData()
    {
        if (buildingDataSO != null && buildingDataSO.buildings != null)
        {
            return buildingDataSO.buildings;
        }
        return new List<BuildingData>();
    }

    /// <summary>
    /// 지정된 건물 타입에 대한 모든 생산 정보(BuildingProductionInfo) 리스트를 반환합니다.
    /// </summary>
    /// <param name="buildingType">정보를 가져올 건물의 타입</param>
    /// <returns>해당 건물 타입의 생산 정보 리스트</returns>
    public List<BuildingProductionInfo> GetProductionInfosForBuildingType(string buildingType)
    {
        if (buildingProductionInfoSO == null || buildingProductionInfoSO.productionInfos == null)
        {
            return new List<BuildingProductionInfo>();
        }
        Debug.Log(buildingProductionInfoSO.productionInfos.Where(info => info.building_type == buildingType).ToList().Count);
        return buildingProductionInfoSO.productionInfos.Where(info => info.building_type == buildingType).ToList();
    }
    /// <summary>
    /// 건물 타입 ID로 BuildingData를 조회합니다.
    /// </summary>
    /// <param name="buildingTypeId">건물 타입 ID</param>
    /// <returns>찾은 BuildingData. 없으면 null을 반환합니다.</returns>
    public BuildingData GetBuildingDataByTypeId(int buildingTypeId)
    {
        _buildingDataDict.TryGetValue(buildingTypeId, out var buildingData);
        if (buildingData == null) Debug.LogWarning($"BuildingData 타입 ID '{buildingTypeId}'를 찾을 수 없습니다.");
        return buildingData;
    }

    /// <summary>
    /// 건물 인스턴스 ID로 ConstructedBuilding을 조회합니다.
    /// </summary>
    /// <param name="instanceId">건물 인스턴스 ID</param>
    /// <returns>찾은 ConstructedBuilding. 없으면 null을 반환합니다.</returns>
    public ConstructedBuilding GetConstructedBuildingByInstanceId(long instanceId)
    {
        return _constructedBuildings.Find(b => b.InstanceId == instanceId);
    }

    /// <summary>
    /// 건물 타입에 해당하는 업그레이드 데이터를 가져옵니다.
    /// </summary>
    public List<BuildingUpgradeData> GetBuildingUpgradeDataByType(string buildingType)
    {
        return _buildingUpgradeDatas.FindAll(data => data.building_type == buildingType);
    }

    /// <summary>
    /// 업그레이드 데이터 리스트에서 특정 레벨의 데이터를 찾습니다.
    /// </summary>
    public BuildingUpgradeData GetBuildingUpgradeDataByLevel(List<BuildingUpgradeData> upgradeDataList, int level)
    {
        return upgradeDataList.Find(data => data.level == level);
    }

    /// <summary>
    /// 새로운 건물을 건설 목록에 추가하거나, 이미 존재하면 위치만 업데이트합니다.
    /// </summary>
    /// <param name="buildingTypeId">건물 타입 ID</param>
    /// <param name="position">건물 위치</param>
    /// <param name="instanceId">건물 인스턴스 ID (이미 생성된 경우). 없으면 자동 생성</param>
    public void AddConstructedBuilding(int buildingTypeId, Vector3Int position, long instanceId = -1)
    {
        // 1. BuildingData 확인
        if (!_buildingDataDict.TryGetValue(buildingTypeId, out BuildingData buildingData))
        {
            Debug.LogError($"BuildingData 타입 ID '{buildingTypeId}'를 찾을 수 없습니다.");
            return;
        }

        // 2. instance_id가 제공되지 않았으면 새로 생성
        if (instanceId == -1)
        {
            instanceId = BuildingIDGenerator.GenerateInstanceID(buildingTypeId);
        }

        // 3. 이미 존재하는 건물인지 확인 (instance_id로)
        var existingProduction = DataManager.Instance.ConstructedBuildingProductions
            .Find(p => p.instance_id == instanceId);
        var existingPosition = DataManager.Instance.ConstructedBuildingPositions
            .Find(p => p.instance_id == instanceId);

        if (existingProduction != null && existingPosition != null)
        {
            // 이미 존재하는 경우 - 위치만 업데이트 (변경사항이 있는 경우에만)
            if (existingPosition.pos != position)
            {
                existingPosition.pos = position;
                Debug.Log($"건물 '{buildingData.Building_Name}' (인스턴스 ID: {instanceId})의 위치를 {position}로 업데이트했습니다.");
            }
        }
        else
        {
            // 새로운 건물인 경우 - 데이터 추가
            // Production 데이터 생성
            ConstructedBuildingProduction newProduction = new ConstructedBuildingProduction
            {
                instance_id = instanceId,
                last_production_time = System.DateTime.Now,
                next_production_time = System.DateTime.Now,
                is_producing = false
            };
            DataManager.Instance.ConstructedBuildingProductions.Add(newProduction);

            // Position 데이터 생성
            ConstructedBuildingPos newPosition = new ConstructedBuildingPos
            {
                instance_id = instanceId,
                pos = position,
                rotation = 0f
            };
            DataManager.Instance.ConstructedBuildingPositions.Add(newPosition);

            Debug.Log($"건물 '{buildingData.Building_Name}' (타입 ID: {buildingTypeId}, 인스턴스 ID: {instanceId})을 위치 {position}에 건설 목록에 추가했습니다.");
        }

        // ConstructedBuildings 리스트 갱신
        CreateConstructedBuildingsList(DataManager.Instance.ConstructedBuildingProductions, DataManager.Instance.ConstructedBuildingPositions);
    }

    /// <summary>
    /// 건물을 건설 목록에서 제거합니다.
    /// </summary>
    /// <param name="instanceId">건물 인스턴스 ID</param>
    public void RemoveConstructedBuilding(long instanceId)
    {
        // 1. ConstructedBuildingProductions에서 제거
        var production = DataManager.Instance.ConstructedBuildingProductions.Find(p => p.instance_id == instanceId);
        var position = DataManager.Instance.ConstructedBuildingPositions.Find(p => p.instance_id == instanceId);

        if (production != null)
        {
            DataManager.Instance.ConstructedBuildingProductions.Remove(production);
            if (position != null)
            {
                DataManager.Instance.ConstructedBuildingPositions.Remove(position);
            }

            // 2. ConstructedBuildings 리스트 갱신
            CreateConstructedBuildingsList(DataManager.Instance.ConstructedBuildingProductions, DataManager.Instance.ConstructedBuildingPositions);

            Debug.Log($"건물 인스턴스 ID '{instanceId}'를 건설 목록에서 제거했습니다.");
        }
        else
        {
            Debug.LogWarning($"건물 인스턴스 ID '{instanceId}'를 건설 목록에서 찾을 수 없습니다.");
        }
    }

    /// <summary>
    /// 건물의 생산 상태를 업데이트합니다.
    /// </summary>
    /// <param name="instanceId">건물 인스턴스 ID</param>
    public void UpdateBuildingProductionStatus(long instanceId, bool isProducing, DateTime? nextProductionTime = null)
    {
        var production = DataManager.Instance.ConstructedBuildingProductions.Find(p => p.instance_id == instanceId);
        if (production != null)
        {
            production.is_producing = isProducing;
            production.last_production_time = System.DateTime.Now;

            if (nextProductionTime.HasValue)
            {
                production.next_production_time = nextProductionTime.Value;
            }

            // ConstructedBuildings 리스트 갱신
            CreateConstructedBuildingsList(DataManager.Instance.ConstructedBuildingProductions, DataManager.Instance.ConstructedBuildingPositions);

            Debug.Log($"건물 인스턴스 ID '{instanceId}'의 생산 상태를 업데이트했습니다. 생산 중: {isProducing}");
        }
        else
        {
            Debug.LogWarning($"건물 인스턴스 ID '{instanceId}'를 찾을 수 없습니다.");
        }
    }

    public void SpawnConstructedBuildings()
    {
        try
        {
            foreach (var building in _constructedBuildings)
            {
                Vector3Int gridpos = building.Position;
                Vector3 worldPos = grid.CellToWorld(gridpos);
                //Vector3Int worldPos = grid.CellToWorld
                //float rot = building.Rotation;
                BuildingData buildingData = GetBuildingDataByTypeId(building.Id); // building.Id는 building_type_id
                GameObject constructedbuilding = BuildingFactory.CreateBuilding(buildingData, worldPos, building.InstanceId); // instanceId 전달
                DragDropController.Instance.PlaceTilemapMarkers(gridpos, buildingData.tileSize, buildingData.MarkerPositionOffset);
            }
        }
        catch (Exception ex)
        {
            Debug.Log(ex);
            return;
        }
    }

    /// <summary>
    /// 모든 건설된 건물 정보를 로그로 출력합니다. (디버깅용)
    /// </summary>
    public void LogConstructedBuildings()
    {
        if (_constructedBuildings == null || _constructedBuildings.Count == 0)
        {
            Debug.Log("건설된 건물이 없습니다.");
            return;
        }

        foreach (var building in _constructedBuildings)
        {
            Debug.Log($"ID: {building.Id}, Name: {building.Name}, Type: {building.Type}, " +
                     $"Level: {building.Level}, IsProducing: {building.IsProducing}, " +
                     $"ResourceId: {building.ProductionResourceId}, OutputAmount: {building.ProductionOutputAmount}, " +
                     $"ProductionTime: {building.BaseProductionTimeMinutes}min, " +
                     $"LastProduction: {building.LastProductionTime}, NextProduction: {building.NextProductionTime}");
        }
    }
}
