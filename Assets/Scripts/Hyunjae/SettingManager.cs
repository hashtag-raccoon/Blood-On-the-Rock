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
    
    [Header("탭 버튼 설정")]
    [SerializeField] private Button displayTabButton;    // Display 탭 버튼
    [SerializeField] private Button audioTabButton;      // Audio 탭 버튼
    [SerializeField] private Button controlTabButton;    // Control 탭 버튼
    
    [Header("설정 패널")]
    [SerializeField] private GameObject displayPanel;    // Display 설정 패널
    [SerializeField] private GameObject audioPanel;      // Audio 설정 패널
    [SerializeField] private GameObject controlPanel;   // Control 설정 패널
    
    [Header("탭 시각적 피드백")]
    [SerializeField] private Color selectedTabColor = new Color(1f, 1f, 1f, 1f);      // 선택된 탭 색상
    [SerializeField] private Color normalTabColor = new Color(0.7f, 0.7f, 0.7f, 1f);  // 일반 탭 색상
    
    [Header("씬 전환 설정")]
    [SerializeField] private Button backButton;      // 뒤로가기 버튼
    [SerializeField] private string startSceneName = "StartSceneHJ";  // 시작 씬 이름
    
    private string currentActiveTab = "Display";  // 현재 활성화된 탭

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
        
        // 탭 버튼 이벤트 연결
        InitializeTabButtons();
        
        // 초기 탭 설정 (Display 탭을 기본으로 활성화)
        SwitchTab("Display");
        
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
    
    /// <summary>
    /// 탭 버튼 초기화 및 이벤트 연결
    /// </summary>
    private void InitializeTabButtons()
    {
        if (displayTabButton != null)
        {
            displayTabButton.onClick.RemoveAllListeners();
            displayTabButton.onClick.AddListener(() => SwitchTab("Display"));
        }
        
        if (audioTabButton != null)
        {
            audioTabButton.onClick.RemoveAllListeners();
            audioTabButton.onClick.AddListener(() => SwitchTab("Audio"));
        }
        
        if (controlTabButton != null)
        {
            controlTabButton.onClick.RemoveAllListeners();
            controlTabButton.onClick.AddListener(() => SwitchTab("Control"));
        }
    }
    
    /// <summary>
    /// 탭 전환 함수
    /// </summary>
    /// <param name="tabName">전환할 탭 이름 (Display, Audio, Control)</param>
    public void SwitchTab(string tabName)
    {
        // 모든 패널 비활성화
        if (displayPanel != null) displayPanel.SetActive(false);
        if (audioPanel != null) audioPanel.SetActive(false);
        if (controlPanel != null) controlPanel.SetActive(false);
        
        // 모든 탭 버튼 색상 초기화
        ResetTabButtonColors();
        
        // 선택된 탭에 따라 패널 활성화 및 버튼 색상 변경
        switch (tabName)
        {
            case "Display":
                if (displayPanel != null) displayPanel.SetActive(true);
                SetTabButtonColor(displayTabButton, selectedTabColor);
                currentActiveTab = "Display";
                break;
                
            case "Audio":
                if (audioPanel != null) audioPanel.SetActive(true);
                SetTabButtonColor(audioTabButton, selectedTabColor);
                currentActiveTab = "Audio";
                break;
                
            case "Control":
                if (controlPanel != null) controlPanel.SetActive(true);
                SetTabButtonColor(controlTabButton, selectedTabColor);
                currentActiveTab = "Control";
                break;
                
            default:
                Debug.LogWarning($"알 수 없는 탭 이름: {tabName}");
                return;
        }
        
        Debug.Log($"탭 전환: {tabName}");
    }
    
    /// <summary>
    /// 모든 탭 버튼 색상을 기본 색상으로 초기화
    /// </summary>
    private void ResetTabButtonColors()
    {
        SetTabButtonColor(displayTabButton, normalTabColor);
        SetTabButtonColor(audioTabButton, normalTabColor);
        SetTabButtonColor(controlTabButton, normalTabColor);
    }
    
    /// <summary>
    /// 탭 버튼의 색상 설정
    /// </summary>
    private void SetTabButtonColor(Button button, Color color)
    {
        if (button != null)
        {
            var colors = button.colors;
            colors.normalColor = color;
            button.colors = colors;
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
        SwitchTab("Display");
    }

    /// <summary>
    /// Audio 설정 클릭 시 호출
    /// </summary>
    private void OnAudioSettingClicked()
    {
        Debug.Log("Audio 설정을 엽니다.");
        SwitchTab("Audio");
    }

    /// <summary>
    /// Control 설정 클릭 시 호출
    /// </summary>
    private void OnControlSettingClicked()
    {
        Debug.Log("Control 설정을 엽니다.");
        SwitchTab("Control");
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
