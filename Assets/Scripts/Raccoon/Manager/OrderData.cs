using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 주문 데이터 정보를 저장하는 클래스
/// </summary>
[System.Serializable]
public class OrderData
{
    public int orderId;                 // 주문 고유 ID
    public GameObject targetTable;      // 주문한 테이블/손님 오브젝트
    public CocktailData orderedCocktail; // 주문한 칵테일 정보
    public int quantity;                // 주문 수량
    public float orderTime;             // 주문 시간 (Time.time 기준)
    public bool isCompleted;            // 주문 완료 여부

    /// <summary>
    /// 주문 데이터 생성자
    /// </summary>
    /// <param name="id">주문 고유 ID</param>
    /// <param name="table">주문한 테이블/손님 오브젝트</param>
    /// <param name="cocktail">주문한 칵테일</param>
    /// <param name="qty">주문 수량</param>
    public OrderData(int id, GameObject table, CocktailData cocktail, int qty = 1)
    {
        orderId = id;
        targetTable = table;
        orderedCocktail = cocktail;
        quantity = qty;
        orderTime = Time.time;
        isCompleted = false;
    }
}
