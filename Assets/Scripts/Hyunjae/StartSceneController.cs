using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class StartSceneController : MonoBehaviour
{
    [Header("UI Components")]
    public Text gameTitle;          // 게임 제목 텍스트
    public Button startButton;      // 게임 시작 버튼
    public Button exitButton;       // 게임 종료 버튼
    public Button settingButton;    // 설정 버튼 (추가 가능)
    

    [Header("Settings")]
    public string gameSceneName = "SampleScene";    // 다음 화면 씬 이름름
    public string settingSceneName = "SettingScene"; // 설정 씬 이름

    void Start()
    {
        if (startButton != null)
        {
            startButton.onClick.AddListener(StartGame);
        }
        
        if (exitButton != null)
        {
            exitButton.onClick.AddListener(ExitGame);
        }
        
        if (settingButton != null)
        {
            settingButton.onClick.AddListener(OpenSettings);
        }
        
        // 게임 제목 설정
        if (gameTitle != null)
        {
            gameTitle.text = "Blood On The Rock";
        }
    }

    
    public void StartGame()
    {
        Debug.Log("게임을 시작합니다!");
        
        // 게임 씬으로 전환
        SceneManager.LoadScene(gameSceneName);
    }

    
    public void ExitGame()
    {
        Debug.Log("게임을 종료합니다!");
        
        
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }

    public void OpenSettings()
    {
        Debug.Log("설정 화면을 엽니다!");
        
        // 설정 씬으로 전환
        SceneManager.LoadScene(settingSceneName);
    }
}
