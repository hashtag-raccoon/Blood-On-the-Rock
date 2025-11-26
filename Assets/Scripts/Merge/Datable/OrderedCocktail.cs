using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 주문 상태를 나타내는 enum
/// </summary>
public enum OrderStatus
{
    Ordered,    // 주문됨
    Preparing,  // 제조 중
    Ready,      // 제조 완료
    Served,     // 서빙 완료
    Cancelled   // 취소됨
}

/// <summary>
/// 손님이 주문한 칵테일의 모든 정보를 통합하여 관리하는 조합 데이터 클래스입니다.
/// CocktailData, CocktailRecipeScript, Glass 정보를 조합하고, 주문 관련 런타임 정보를 포함합니다.
/// </summary>
[Serializable]
public class OrderedCocktail
{
    #region CocktailData 정보
    /// <summary>칵테일 ID (기본키)</summary>
    public int CocktailId { get; private set; }

    /// <summary>칵테일 명</summary>
    public string CocktailName { get; private set; }

    /// <summary>제조 기법 (0: Build, 1: Floating, 2: Shaking)</summary>
    public int Technique { get; private set; }

    /// <summary>등급 (기본/고급/전설)</summary>
    public string Grade { get; private set; }

    /// <summary>난이도 (1-5)</summary>
    public int Difficulty { get; private set; }

    /// <summary>유사성 비교 기준 (기본값: 80.0)</summary>
    public float SimilarityThreshold { get; private set; }

    /// <summary>판매 가격</summary>
    public int SellPrice { get; private set; }

    /// <summary>잔 ID (외래키)</summary>
    public int GlassId { get; private set; }

    /// <summary>칵테일 아이콘 (UI 표시용)</summary>
    public Sprite Icon { get; private set; }
    #endregion

    #region CocktailRecipeScript 정보
    /// <summary>레시피 (재료 ID → 재료 정보)</summary>
    public Dictionary<int, Ingridiant> Recipe { get; private set; }

    /// <summary>레시피 순서 (제조 단계)</summary>
    public string RecipeOrder { get; private set; }
    #endregion

    #region Glass 정보
    /// <summary>잔 이름</summary>
    public string GlassName { get; private set; }

    /// <summary>잔 아이콘</summary>
    public Sprite GlassIcon { get; private set; }
    #endregion

    #region 주문 런타임 정보
    /// <summary>주문한 손님 GameObject</summary>
    public GameObject OrderedByGuest { get; set; }

    /// <summary>손님이 앉은 테이블 GameObject</summary>
    public GameObject AssignedTable { get; set; }

    /// <summary>주문 시간</summary>
    public DateTime OrderTime { get; set; }

    /// <summary>주문 상태</summary>
    public OrderStatus Status { get; set; }

    /// <summary>주문 고유 인스턴스 ID</summary>
    public long OrderInstanceId { get; private set; }
    #endregion

    /// <summary>
    /// OrderedCocktail 생성자
    /// 3가지 데이터 소스(CocktailData, CocktailRecipeScript, Glass)와 주문 정보를 조합하여
    /// 하나의 완전한 주문 객체를 생성합니다.
    /// </summary>
    /// <param name="cocktailData">칵테일 기본 정보</param>
    /// <param name="recipe">칵테일 레시피 정보</param>
    /// <param name="glass">잔 정보 (null 가능)</param>
    /// <param name="orderedByGuest">주문한 손님</param>
    /// <param name="assignedTable">손님이 앉은 테이블</param>
    /// <param name="orderTime">주문 시간</param>
    /// <param name="orderInstanceId">주문 고유 ID</param>
    public OrderedCocktail(
        CocktailData cocktailData,
        CocktailRecipeScript recipe,
        Glass glass,
        GameObject orderedByGuest,
        GameObject assignedTable,
        DateTime orderTime,
        long orderInstanceId)
    {
        // CocktailData 정보 복사
        if (cocktailData != null)
        {
            CocktailId = cocktailData.Cocktail_ID;
            CocktailName = cocktailData.CocktailName;
            Technique = cocktailData.technique;
            Grade = cocktailData.grade;
            Difficulty = cocktailData.difficulty;
            SimilarityThreshold = cocktailData.similarity_threadhold;
            SellPrice = cocktailData.sell_price;
            GlassId = cocktailData.glass_id;
            Icon = cocktailData.Icon;
        }
        else
        {
            Debug.LogError("CocktailData가 null입니다. OrderedCocktail을 생성할 수 없습니다.");
        }

        // CocktailRecipeScript 정보 복사
        if (recipe != null)
        {
            Recipe = recipe.Recipedict;
            RecipeOrder = recipe.RecipeOrder;
        }
        else
        {
            Debug.LogWarning($"CocktailRecipeScript가 null입니다. (칵테일 ID: {CocktailId})");
            Recipe = new Dictionary<int, Ingridiant>();
            RecipeOrder = string.Empty;
        }

        // Glass 정보 복사 (null 허용)
        if (glass != null)
        {
            GlassName = glass.Glass_name;
            GlassIcon = glass.Icon;
        }
        else
        {
            Debug.LogWarning($"Glass 정보가 null입니다. (Glass ID: {GlassId})");
            GlassName = "Unknown Glass";
            GlassIcon = null;
        }

        // 주문 런타임 정보
        OrderedByGuest = orderedByGuest;
        AssignedTable = assignedTable;
        OrderTime = orderTime;
        Status = OrderStatus.Ordered;
        OrderInstanceId = orderInstanceId;
    }

    /// <summary>
    /// 주문 정보를 문자열로 반환합니다. (디버깅용)
    /// </summary>
    public override string ToString()
    {
        return $"[주문 ID: {OrderInstanceId}] {CocktailName} (손님: {OrderedByGuest?.name ?? "None"}, 테이블: {AssignedTable?.name ?? "None"}, 상태: {Status}, 주문 시간: {OrderTime:yyyy-MM-dd HH:mm:ss})";
    }
}
