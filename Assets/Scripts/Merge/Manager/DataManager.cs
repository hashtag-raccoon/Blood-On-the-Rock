using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DataManager : MonoBehaviour
{
    public static DataManager instance;

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
    }

    public void GetGoodsData()
    {

    }
}
