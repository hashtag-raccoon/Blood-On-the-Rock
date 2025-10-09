using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class SettingManager : MonoBehaviour
{
    [Header("UI Components")]
    public Button backButton;      // 뒤로가기 버튼
    
    [Header("Settings")]
    public string startSceneName = "StartSceneHJ";  // 시작 씬 이름
    
    void Start()
    {
        // 뒤로가기 버튼 이벤트 연결
        if (backButton != null)
        {
            backButton.onClick.AddListener(GoBackToStart);
        }
    }

    // 시작 화면으로 돌아가는 함수
    public void GoBackToStart()
    {
        Debug.Log("시작 화면으로 돌아갑니다!");
        
        // 시작 씬으로 전환
        SceneManager.LoadScene(startSceneName);
    }
}
