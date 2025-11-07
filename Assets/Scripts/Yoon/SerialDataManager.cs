using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class SerialDataManager : MonoBehaviour
{
    #region Singlton
    public static SerialDataManager Instance { get; private set; }

    /*
    // 예시: 해금된 칵테일 레시피 목록 (레시피 ID, 해금 여부)
    [SerializeField]
    
    // 예시: 플레이어 보유 자원 (자원 이름, 수량)
    [SerializeField]
    */

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            // TODO: 게임 시작 시 저장된 데이터 불러오기 (Load)

        }
        else
        {
            Destroy(gameObject);
        }
    }
    #endregion

    // TODO: 게임 저장 (Save), 데이터 접근 및 수정 관련 메서드들 추가
}
