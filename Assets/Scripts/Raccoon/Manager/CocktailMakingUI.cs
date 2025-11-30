using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CocktailMakingUI : MonoBehaviour
{
    //ex) 아래는 칵테일 제작 UI 패널을 여닫는 간단한 예시 스크립트
    //[Header("칵테일 제작 UI 패널")]
    //public GameObject cocktailMakingPanel; // 칵테일 제작 UI 패널 할당

    private void Start()
    {
        if (this.gameObject.activeSelf == true)
        {
            this.gameObject.SetActive(false); // 시작 시 UI 비활성화
        }
    }

    /// <summary>
    /// 칵테일 제작 UI 열기 함수
    /// </summary>
    public void OpenCocktailMakingUI()
    {
        this.gameObject.SetActive(true);
    }

    /// <summary>
    /// 칵테일 제작 UI 닫기 함수
    /// </summary>
    public void CloseCocktailMakingUI()
    {
        this.gameObject.SetActive(false);
    }
}
