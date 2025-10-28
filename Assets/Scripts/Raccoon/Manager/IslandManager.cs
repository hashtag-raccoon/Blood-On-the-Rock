using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IslandManager : MonoBehaviour
{
    [Header("하루 시간 설정(시)")]
    [SerializeField] private float SetDayTime = 3; // 시간 기준으로 설정할 것
    private float convertedDayTime = 0;

    [Header("바 현재 선호도")]
    public float storeFavor = 100f;

    public int barLevel = 1;
    public int wood = 0;
    public int money = 0;

    void Start()
    {
        convertedDayTime = SetDayTime * 60 * 60f; // 초 단위로 변환

        // 낮 -> 밤 코루틴 시작
        StartCoroutine(DayCoroutine());
    }

    void Update()
    {
        if (storeFavor <= 0)
        {
            // 게임종료
            Debug.Log("가게 호감도가 0이 되어 게임이 종료됩니다.");
        }
    }

    // 낮 -> 밤 코루틴
    IEnumerator DayCoroutine()
    {
        yield return new WaitForSeconds(convertedDayTime);
        Debug.Log("하루가 지났습니다.");
    }
}
