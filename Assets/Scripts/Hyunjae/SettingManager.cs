using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

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
        
        // UI 이벤트 시스템 자동 설정
        EnsureUIEventSystem();
    }
    
    /// <summary>
    /// BaseScrollUI의 InitializeButtons를 오버라이드하여 openButton 연결을 방지
    /// SettingScene은 StartScene에서 설정 버튼을 눌러 들어오므로 openButton이 필요 없음
    /// </summary>
    protected override void InitializeButtons()
    {
        // openButton은 사용하지 않으므로 연결하지 않음
        // closeButton만 연결
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(OnCloseButtonClicked);
        }
    }

    private void Start()
    {
        // 뒤로가기 버튼 이벤트 연결
        if (backButton != null)
        {
            backButton.onClick.AddListener(GoBackToStart);
        }
        else
        {
            Debug.LogWarning("SettingManager: BackButton이 Inspector에 할당되지 않았습니다.");
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
    /// UI 이벤트 시스템이 제대로 설정되어 있는지 확인하고 없으면 생성
    /// </summary>
    private void EnsureUIEventSystem()
    {
        // EventSystem 확인 및 생성
        if (EventSystem.current == null)
        {
            Debug.LogWarning("SettingManager: EventSystem이 없습니다. 자동으로 생성합니다.");
            GameObject eventSystemObj = new GameObject("EventSystem");
            eventSystemObj.AddComponent<EventSystem>();
            eventSystemObj.AddComponent<StandaloneInputModule>();
            Debug.Log("SettingManager: EventSystem 생성 완료");
        }
        
        // Canvas 확인 및 GraphicRaycaster 추가
        Canvas[] canvases = FindObjectsOfType<Canvas>();
        foreach (Canvas canvas in canvases)
        {
            if (canvas.GetComponent<GraphicRaycaster>() == null)
            {
                Debug.LogWarning($"SettingManager: Canvas '{canvas.name}'에 GraphicRaycaster가 없습니다. 자동으로 추가합니다.");
                canvas.gameObject.AddComponent<GraphicRaycaster>();
                Debug.Log($"SettingManager: Canvas '{canvas.name}'에 GraphicRaycaster 추가 완료");
            }
        }
    }
    
    /// <summary>
    /// 탭 버튼 초기화 및 이벤트 연결
    /// </summary>
    private void InitializeTabButtons()
    {
        // Display 탭 버튼 설정
        if (displayTabButton != null)
        {
            // 버튼 상태 확인
            CheckButtonState(displayTabButton, "Display");
            
            displayTabButton.onClick.RemoveAllListeners();
            displayTabButton.onClick.AddListener(() => {
                Debug.Log("SettingManager: Display 버튼 클릭됨!");
                SwitchTab("Display");
            });
            Debug.Log("SettingManager: Display 탭 버튼 이벤트 연결 완료");
        }
        else
        {
            Debug.LogError("SettingManager: DisplayTabButton이 Inspector에 할당되지 않았습니다. 버튼이 동작하지 않습니다!");
        }
        
        // Audio 탭 버튼 설정
        if (audioTabButton != null)
        {
            // 버튼 상태 확인
            CheckButtonState(audioTabButton, "Audio");
            
            audioTabButton.onClick.RemoveAllListeners();
            audioTabButton.onClick.AddListener(() => {
                Debug.Log("SettingManager: Audio 버튼 클릭됨!");
                SwitchTab("Audio");
            });
            Debug.Log("SettingManager: Audio 탭 버튼 이벤트 연결 완료");
        }
        else
        {
            Debug.LogError("SettingManager: AudioTabButton이 Inspector에 할당되지 않았습니다. 버튼이 동작하지 않습니다!");
        }
        
        // Control 탭 버튼 설정
        if (controlTabButton != null)
        {
            // 버튼 상태 확인
            CheckButtonState(controlTabButton, "Control");
            
            controlTabButton.onClick.RemoveAllListeners();
            controlTabButton.onClick.AddListener(() => {
                Debug.Log("SettingManager: Control 버튼 클릭됨!");
                SwitchTab("Control");
            });
            Debug.Log("SettingManager: Control 탭 버튼 이벤트 연결 완료");
        }
        else
        {
            Debug.LogError("SettingManager: ControlTabButton이 Inspector에 할당되지 않았습니다. 버튼이 동작하지 않습니다!");
        }
    }
    
    /// <summary>
    /// 버튼 상태를 확인하고 문제가 있으면 경고를 출력하고 수정 시도
    /// </summary>
    private void CheckButtonState(Button button, string buttonName)
    {
        if (button == null) return;
        
        // 버튼이 활성화되어 있는지 확인
        if (!button.gameObject.activeInHierarchy)
        {
            Debug.LogWarning($"SettingManager: {buttonName} 버튼의 GameObject가 비활성화되어 있습니다!");
            button.gameObject.SetActive(true);
            Debug.Log($"SettingManager: {buttonName} 버튼 GameObject 활성화 완료");
        }
        
        // 버튼이 상호작용 가능한지 확인
        if (!button.interactable)
        {
            Debug.LogWarning($"SettingManager: {buttonName} 버튼의 Interactable이 false입니다!");
            button.interactable = true;
            Debug.Log($"SettingManager: {buttonName} 버튼 Interactable 활성화 완료");
        }
        
        // 버튼의 Image 컴포넌트에서 Raycast Target 확인
        Image buttonImage = button.GetComponent<Image>();
        if (buttonImage != null && !buttonImage.raycastTarget)
        {
            Debug.LogWarning($"SettingManager: {buttonName} 버튼의 Image Raycast Target이 false입니다!");
            buttonImage.raycastTarget = true;
            Debug.Log($"SettingManager: {buttonName} 버튼 Image Raycast Target 활성화 완료");
        }
        
        // 버튼에 GraphicRaycaster가 필요한지 확인 (Canvas에 있어야 함)
        Canvas canvas = button.GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            GraphicRaycaster raycaster = canvas.GetComponent<GraphicRaycaster>();
            if (raycaster == null)
            {
                Debug.LogWarning($"SettingManager: Canvas에 GraphicRaycaster가 없습니다! 자동으로 추가합니다.");
                canvas.gameObject.AddComponent<GraphicRaycaster>();
                Debug.Log($"SettingManager: Canvas에 GraphicRaycaster 추가 완료");
            }
        }
        else
        {
            Debug.LogWarning($"SettingManager: {buttonName} 버튼이 Canvas 하위에 없습니다!");
        }
        
        // EventSystem 확인
        if (EventSystem.current == null)
        {
            Debug.LogError("SettingManager: EventSystem이 씬에 없습니다! 자동으로 생성합니다.");
            EnsureUIEventSystem();
        }
        
        Debug.Log($"SettingManager: {buttonName} 버튼 상태 - Active: {button.gameObject.activeInHierarchy}, Interactable: {button.interactable}, Enabled: {button.enabled}, RaycastTarget: {(buttonImage != null ? buttonImage.raycastTarget.ToString() : "N/A")}");
    }
    
    /// <summary>
    /// 탭 전환 함수
    /// </summary>
    /// <param name="tabName">전환할 탭 이름 (Display, Audio, Control)</param>
    public void SwitchTab(string tabName)
    {
        Debug.Log($"SettingManager: SwitchTab 호출됨 - {tabName}");
        
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
                if (displayPanel != null)
                {
                    displayPanel.SetActive(true);
                    Debug.Log("SettingManager: Display 패널 활성화됨");
                }
                else
                {
                    Debug.LogWarning("SettingManager: DisplayPanel이 할당되지 않았습니다.");
                }
                SetTabButtonColor(displayTabButton, selectedTabColor);
                currentActiveTab = "Display";
                break;
                
            case "Audio":
                if (audioPanel != null)
                {
                    audioPanel.SetActive(true);
                    Debug.Log("SettingManager: Audio 패널 활성화됨");
                }
                else
                {
                    Debug.LogWarning("SettingManager: AudioPanel이 할당되지 않았습니다.");
                }
                SetTabButtonColor(audioTabButton, selectedTabColor);
                currentActiveTab = "Audio";
                break;
                
            case "Control":
                if (controlPanel != null)
                {
                    controlPanel.SetActive(true);
                    Debug.Log("SettingManager: Control 패널 활성화됨");
                }
                else
                {
                    Debug.LogWarning("SettingManager: ControlPanel이 할당되지 않았습니다.");
                }
                SetTabButtonColor(controlTabButton, selectedTabColor);
                currentActiveTab = "Control";
                break;
                
            default:
                Debug.LogWarning($"SettingManager: 알 수 없는 탭 이름: {tabName}");
                return;
        }
        
        Debug.Log($"SettingManager: 탭 전환 완료 - {tabName}");
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
