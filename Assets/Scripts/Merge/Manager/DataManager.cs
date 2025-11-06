using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DataManager : MonoBehaviour
{
    public static DataManager instance;
    private const string BuildingPath = "Data/Building";
    private const string GoodsPath = "Data/Goods";
    public List<goodsData> goodsDatas = new List<goodsData>();
    public List<BuildingData> BuildingDatas = new List<BuildingData>();

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
        BuildingDatas.Clear();
        goodsDatas.Clear();

        BuildingDatas.AddRange(Resources.LoadAll<BuildingData>(BuildingPath));
        goodsDatas.AddRange(Resources.LoadAll<goodsData>(GoodsPath));
    }

    public void GetGoodsData()
    {

    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            BuildingDatas.Clear();
            goodsDatas.Clear();
            BuildingDatas = null;
            goodsDatas = null;
            instance = null;
        }
    }
}
