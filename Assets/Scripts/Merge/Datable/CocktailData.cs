using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(fileName = "CocktailData", menuName = "ScriptableObjects/CocktailData")]
[Serializable]
public class CocktailData : ScriptableObject
{
    public int Cocktail_ID; //칵테일ID(기본키)
    public string CocktailName; //칵테일 명(고유)
    public string technique; // 제조기법(빌드/플로팅/쉐이킹)
    public string grade; // 등급(기본/고급/전설)
    public string taste; // 맛특성(단맛/쓴맛/신맛/복합)
    public int difficulty; // 난이도(1-5)
    public int production_time; // 제조시간(분)
    public string description; // 설명
    public string production_steps; // 제조단계 순서[Json]
    public float similarity_threadhold; // 유사성 비교기준(70% 고정)
    public string unlock_condition; // 해금조건[Json]
    public int sell_price; // 판매가격
    public int production_cost; // 제작원가
    public int glass_id; // 잔 종류(외래키)
}
