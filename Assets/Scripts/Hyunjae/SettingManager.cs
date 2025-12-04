using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// 설정 화면 스크롤 UI
/// InteriorScrollUI와 유사한 구조로 설정 항목 목록을 표시하는 스크롤 UI
/// </summary>
public class SettingManager : BaseScrollUI<SettingData, SettingButtonUI>
{
    [Header("설정 데이터")]
    [SerializeField] private List<SettingData> settingDatas = new List<SettingData>();
    
    [Header("씬 전환 설정")]
    [SerializeField] private Button backButton;      // 뒤로가기 버튼
    [SerializeField] private string startSceneName = "StartSceneHJ";  // 시작 씬 이름

    protected override void Awake()
    {
        base.Awake();
    }

    private void Start()
    {
        // 뒤로가기 버튼 이벤트 연결
        if (backButton != null)
        {
            backButton.onClick.AddListener(GoBackToStart);
        }
        
        // 설정 데이터가 있다면 사용
        if (settingDatas != null && settingDatas.Count > 0)
        {
            GenerateItems(settingDatas);
        }
        else
        {
            Debug.LogWarning("SettingDatas가 없습니다. Inspector에서 SettingData 리스트를 할당하세요.");
        }
    }

    protected override void OnItemClicked(IScrollItemUI clickedItem)
    {
        SettingData data = clickedItem.GetData<SettingData>();
        Debug.Log($"설정 항목 클릭: {data.Setting_Name}");
        
        // 설정 항목에 따른 처리
        switch (data.Setting_Name)
        {
            case "Display":
                OnDisplaySettingClicked();
                break;
            case "Audio":
                OnAudioSettingClicked();
                break;
            case "Control":
                OnControlSettingClicked();
                break;
            default:
                Debug.LogWarning($"알 수 없는 설정 항목: {data.Setting_Name}");
                break;
        }
    }

    protected override void OnOpenButtonClicked()
    {
        base.OnOpenButtonClicked();
    }

    protected override void OnCloseButtonClicked()
    {
        base.OnCloseButtonClicked();
    }

    /// <summary>
    /// Display 설정 클릭 시 호출
    /// </summary>
    private void OnDisplaySettingClicked()
    {
        Debug.Log("Display 설정을 엽니다.");
        // Display 설정 UI 열기 로직 구현
    }

    /// <summary>
    /// Audio 설정 클릭 시 호출
    /// </summary>
    private void OnAudioSettingClicked()
    {
        Debug.Log("Audio 설정을 엽니다.");
        // Audio 설정 UI 열기 로직 구현
    }

    /// <summary>
    /// Control 설정 클릭 시 호출
    /// </summary>
    private void OnControlSettingClicked()
    {
        Debug.Log("Control 설정을 엽니다.");
        // Control 설정 UI 열기 로직 구현
    }

    /// <summary>
    /// 시작 화면으로 돌아가는 함수
    /// </summary>
    public void GoBackToStart()
    {
        Debug.Log("시작 화면으로 돌아갑니다!");
        
        // 시작 씬으로 전환
        SceneManager.LoadScene(startSceneName);
    }
}
