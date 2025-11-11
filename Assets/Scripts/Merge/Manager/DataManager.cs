using System.Collections.Generic;
using UnityEngine;

public class DataManager : MonoBehaviour
{
    public static DataManager instance;
    public static DataManager Instance => instance;
    
    private const string BuildingPath = "Data/Building";
    private const string GoodsPath = "Data/Goods";
    private const string BuildingProductionPath = "Data/Building/BuildingProduction";
    private const string BuildingUpgradePath = "Data/Building/BuildingUpgrade";

    public List<goodsData> goodsDatas = new List<goodsData>();
    public List<BuildingData> BuildingDatas = new List<BuildingData>();
    public List<BuildingProductionData> BuildingProductionDatas = new List<BuildingProductionData>();
    public List<BuildingUpgradeData> BuildingUpgradeDatas = new List<BuildingUpgradeData>();

    [Header("가게 상태")]
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
            return;
        }
        
        BuildingDatas.Clear();
        goodsDatas.Clear();
        BuildingProductionDatas.Clear();

        // BuildingData 로드
        BuildingData[] loadedBuildings = Resources.LoadAll<BuildingData>(BuildingPath);
        foreach (var building in loadedBuildings)
        {
            BuildingDatas.Add(building);
        }
        
        // goodsData 로드
        goodsData[] loadedGoods = Resources.LoadAll<goodsData>(GoodsPath);
        foreach (var goods in loadedGoods)
        {
            goodsDatas.Add(goods);
        }
        
        // BuildingProductionData 로드
        BuildingProductionData[] loadedProductions = Resources.LoadAll<BuildingProductionData>(BuildingProductionPath);
        foreach (var production in loadedProductions)
        {
            BuildingProductionDatas.Add(production);
        }

        // BuildingUpgradeData 로드
        BuildingUpgradeData[] loadedUpgrades = Resources.LoadAll<BuildingUpgradeData>(BuildingUpgradePath);
        foreach (var upgrade in loadedUpgrades)
        {
            BuildingUpgradeDatas.Add(upgrade);
        }
    }

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

    private void OnDestroy()
    {
        if (instance == this)
        {
            BuildingDatas.Clear();
            goodsDatas.Clear();
            BuildingProductionDatas.Clear();
            BuildingDatas = null;
            goodsDatas = null;
            BuildingProductionDatas = null;
            instance = null;
        }
    }
}

