using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum TaskType // 업무 유형
{
    None,
    TakeOrder, // 칵테일 주문
    ServeOrder, // 서빙
    CleanTable // 테이블 청소
}

/// <summary>
/// 업무 정보의 클래스
/// </summary>
[System.Serializable]
public class TaskInfo
{
    public TaskType taskType;           // 업무 타입
    public GameObject targetObject;     // 업무 대상 오브젝트 (손님, 테이블 등)
    public CocktailData orderedCocktail; // 주문한 칵테일 (업무 - 칵테일 주문일 경우)
    public bool isCompleted = false;    // 업무 완료 여부
    public GameObject taskUI;           // 업무 UI 오브젝트

    /// <summary>
    /// 업무 - 칵테일 주문용 생성자
    /// </summary>
    public TaskInfo(TaskType type, GameObject target, CocktailData cocktail = null)
    {
        taskType = type;
        targetObject = target;
        orderedCocktail = cocktail;
        isCompleted = false;
    }

    // 아래로 추가할 업무들 있으면 첨부

    /// <summary>
    /// 업무 완료 처리
    /// </summary>
    public void CompleteTask()
    {
        isCompleted = true;
        // UI 제거
        if (taskUI != null)
        {
            Object.Destroy(taskUI);
        }
    }
}
