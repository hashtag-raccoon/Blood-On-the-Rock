using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// cocktail 전체 데이터
/// </summary>
[Serializable]
public class CocktailData
{
    public int Cocktail_ID { get; private set; }//칵테일ID(기본키)
    public string CocktailName { get; private set; }//칵테일 명(고유)
    public int technique { get; private set; } // 제조기법(빌드/플로팅/쉐이킹)
    public string grade { get; private set; } // 등급(기본/고급/전설)
    public string taste { get; private set; } // 맛특성(단맛/쓴맛/신맛/복합)
    public int difficulty { get; private set; } // 난이도(1-5)
    public int production_time { get; private set; } // 제조시간(분)
    public string description { get; private set; } // 설명
    public string production_steps { get; private set; } // 제조단계 순서[Json]
    public float similarity_threadhold { get; private set; } = 80.0f; // 유사성 비교기준(80% 고정)
    public string unlock_condition { get; private set; } // 해금조건[Json]
    public int sell_price { get; private set; } // 판매가격
    public int production_cost { get; private set; } // 제작원가
    public int glass_id { get; private set; } // 잔 종류(외래키)
    public Sprite Icon { get; private set; } // 칵테일 아이콘 (UI 표시용)
}