using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DialogueUI : MonoBehaviour
{
    [Header("UI 컴포넌트")]
    public GameObject dialoguePanel;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI contextText;
    public Image portraitImage;

    [Header("선택지 버튼")]
    public Button choiceA_Button;
    public TextMeshProUGUI choiceA_Text;
    public Button choiceB_Button;
    public TextMeshProUGUI choiceB_Text;
    public Button choiceC_Button;
    public TextMeshProUGUI choiceC_Text;

    private int currentDialogueID;
    private DialogueData currentData;
    private Queue<string> contextQueue = new Queue<string>();
    private bool isTyping = false;
    private bool isWaitingForInput = false;

    // 대화 종료 시 호출할 콜백 (주문 완료 처리용)
    private System.Action onDialogueEndCallback;

    private void Start()
    {
        // UI 안전성 체크
        if (dialoguePanel == null)
        {

            Debug.LogError("DialoguePanel이 할당되지 않았습니다!");
        }

        // 선택지 버튼들 비활성화
        if (choiceA_Button != null)
        {
            choiceA_Button.onClick.AddListener(() => OnChoiceSelected(0));
        }
        if (choiceB_Button != null)
        {
            choiceB_Button.onClick.AddListener(() => OnChoiceSelected(1));
        }
        if (choiceC_Button != null)
        {
            choiceC_Button.onClick.AddListener(() => OnChoiceSelected(2));
        }
    }

    private void Update()
    {
        // 선택지 버튼이 활성화되어 있지 않은 경우에만 입력 대기
        bool anyChoiceActive = (choiceA_Button != null && choiceA_Button.gameObject.activeSelf) ||
                               (choiceB_Button != null && choiceB_Button.gameObject.activeSelf) ||
                               (choiceC_Button != null && choiceC_Button.gameObject.activeSelf);

        // 입력 대기 중이고 선택지 버튼이 활성화되어 있지 않으면
        // => 즉, 선택지 있는 대화가 아닌 일반 대화 진행 중일 때 다음 대화로 진행
        if (isWaitingForInput && !anyChoiceActive)
        {
            if (Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetMouseButtonDown(0))
            {
                DisplayNextContext();
            }
        }
    }

    private string currentReplacementName;
    private string currentReplacementPortrait;

    /// <summary>
    /// Id에 해당하는 대화를 시작함 (일반 대화 시작)
    /// </summary>
    /// <param name="id">대화 딕셔너리 중 n번째의 키(Index)</param>
    /// <param name="panelSize">대화창 크기</param>
    /// <param name="onEndCallback">대화 종료 시 호출할 콜백 함수</param>
    public void StartDialogue(int id, Vector2? panelSize = null, System.Action onEndCallback = null)
    {
        if (this.gameObject.activeSelf == false)
        {
            this.gameObject.SetActive(true);
        }
        currentDialogueID = id;
        
        // 콜백이 있는 경우에만 업데이트
        if (onEndCallback != null)
        {
            onDialogueEndCallback = onEndCallback;
        }

        // 패널 설정 (크기가 제공되면 전체 설정, 아니면 위치만 설정)
        if (dialoguePanel != null)
        {
            RectTransform rectTransform = dialoguePanel.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                // 패널 크기 설정 시
                if (panelSize.HasValue)
                {
                    rectTransform.anchorMin = new Vector2(0, 0);
                    rectTransform.anchorMax = new Vector2(0, 0);
                    rectTransform.pivot = new Vector2(0, 0);
                    rectTransform.anchoredPosition = new Vector2(0, 0);

                    // Layout 컴포넌트가 크기를 변경하지 못하도록 비활성화
                    // 패널 크기로 조정 되는 것이 아닌, 자동으로 크기가 조정 되는 것을 방지하기 위함
                    var layoutElement = dialoguePanel.GetComponent<LayoutElement>();
                    if (layoutElement != null)
                    {
                        layoutElement.ignoreLayout = true;
                    }
                    // ContentSizeFitter 비활성화
                    // 패널 크기로 조정 되는 것이 아닌, 자동으로 크기가 조정 되는 것을 방지하기 위함
                    var contentSizeFitter = dialoguePanel.GetComponent<ContentSizeFitter>();
                    if (contentSizeFitter != null)
                    {
                        contentSizeFitter.enabled = false;
                    }
                    
                    // 크기 강제 설정
                    rectTransform.sizeDelta = panelSize.Value;

                    // 한 프레임 뒤에 다시 크기 확인
                    Canvas.ForceUpdateCanvases();
                }
                else
                {
                    // 크기가 없으면: 위치만 재설정
                    rectTransform.anchoredPosition = new Vector2(0, 0);
                }
            }
        }
        // 대화 데이터 로드
        LoadDialogueData(id);
    }

    /// <summary>
    /// 주문용 대화 시작 (이때, 텍스트랑 초상화 치환 가능)
    /// </summary>
    public void StartOrderDialogue(int id, Vector2? panelSize = null, System.Action onEndCallback = null, string replacementName = null, string replacementPortrait = null)
    {
        // 화자 이름 및 초상화 설정
        currentReplacementName = replacementName;
        currentReplacementPortrait = replacementPortrait;

        // 기본 대화 시작 로직 호출
        StartDialogue(id, panelSize, onEndCallback);
    }

    /// <summary>
    /// Index에 해당하는 대화 데이터를 로드함
    /// </summary>
    /// <param name="id">대화 인덱스</param>
    private void LoadDialogueData(int id)
    {
        currentData = DialogueManager.Instance.GetDialogue(id);
        // 대화 데이터가 없으면 경고 출력 후 종료
        if (currentData == null)
        {
            Debug.LogWarning("대화 데이터가 없습니다. ID: " + id);
            EndDialogue();
            return;
        }
        //  패널 할당 X 시 종료
        if (dialoguePanel == null)
        {
            Debug.LogError("DialoguePanel이 null입니다!");
            return;
        }

        // 이전 선택 버튼들 모두 비활성화
        if (choiceA_Button != null) choiceA_Button.gameObject.SetActive(false);
        if (choiceB_Button != null) choiceB_Button.gameObject.SetActive(false);
        if (choiceC_Button != null) choiceC_Button.gameObject.SetActive(false);

        // 모든 텍스트 컴포넌트를 AutoSize로 설정
        SetTextAutoSize();
        dialoguePanel.SetActive(true);
        if (nameText != null)
        {
            nameText.text = currentData.Name;
        }

        // 현재 인물 이미지는 Image 또는 Sprite로 저장이 되어 있지만
        // csv 파일 내에는 String으로 저장이 되어 있기 때문에
        // 인물 이미지를 Resources - Portraits 폴더 내에서 불러옴
        string portraitName = currentData.Portrait;

        // 치환할 초상화가 있다면 교체
        if (!string.IsNullOrEmpty(currentReplacementPortrait))
        {
            portraitName = currentReplacementPortrait;
        }

        if (!string.IsNullOrEmpty(portraitName))
        {
            Sprite portrait = Resources.Load<Sprite>($"Dialogue/Portraits/{portraitName}");
            if (portrait != null)
            {
                portraitImage.sprite = portrait;
                portraitImage.gameObject.SetActive(true);

                // Portrait 위치를 패널 왼쪽 하단에 맞춰 조정 (Ignore Layout이므로 수동 위치 조정)
                UpdatePortraitPosition();
            }
            else
            {
                Debug.LogWarning("초상화를 Resources/Dialogue/Portraits 폴더에서 찾을 수 없습니다: " + portraitName);
                portraitImage.gameObject.SetActive(false);
            }
        }
        else
        {
            //Debug.LogWarning("초상화 이름이 비어있습니다."); // 우선 필요없을거 같아서 비활성화 했는데,, 우선 필요하면 다시 살릴 것
            portraitImage.gameObject.SetActive(false); // 초상화가 없을 경우 이미지 비활성화
        }

        // 이벤트 트리거
        // 해당 Index에서 대화 시작 시 한 번만 호출
        if (!string.IsNullOrEmpty(currentData.EventName))
        {
            Debug.Log($"Triggering Event: {currentData.EventName}");
            // 메시지 또는 전용 이벤트 매니저를 사용할 수 있음
            // 후에 사용할 예정
        }

        // 대화 내용 분할 및 큐에 저장
        contextQueue.Clear();

        string context = currentData.Context;
        // 치환 텍스트가 있다면 { }를 치환 텍스트로 치환
        if (!string.IsNullOrEmpty(currentReplacementName))
        {
            context = context.Replace("{ }", currentReplacementName);
        }
        // '/' 기준으로 대화 내용 분할
        string[] parts = context.Split('/');
        foreach (string part in parts)
        {
            contextQueue.Enqueue(part.Trim()); // 공백 제거 후 큐에 추가
        }

        DisplayNextContext(); // 다음 대화 내용 표시
    }

    /// <summary>
    /// 다음 대화 내용을 표시함
    /// </summary> <summary>
    private void DisplayNextContext()
    {
        if (contextQueue.Count > 0) // 아직 대화 내용이 남아있으면
        {
            string text = contextQueue.Dequeue(); // 다음 대화 내용 가져오기
            contextText.text = text;
            isWaitingForInput = true;
        }
        else // 대화 내용이 모두 끝났으면
        {
            isWaitingForInput = false; // 입력 대기 종료

            // 해당 ID의 모든 대화 내용이 끝남
            // 선택지가 있는지 확인
            bool hasChoices = HasChoices();

            if (hasChoices)
            {
                // Context는 그대로 유지하고 선택지를 그 아래에 표시
                ShowChoices();
            }
            else // 선택지가 없으면
            {
                // 다음 Index로 진행
                if (currentData.NextIndex == -1)
                {
                    EndDialogue(); // 다음 대화가 없으면(다음 인덱스가 -1 이면) 종료
                }
                else
                {
                    StartDialogue(currentData.NextIndex); // 다음 대화로 진행
                }
            }
        }
    }

    /// <summary>
    /// 선택지가 있는지 확인하는 메소드
    /// </summary>
    private bool HasChoices() // 선택지가 있는지 확인
    {
        if (currentData == null) return false;

        // 선택지 텍스트가 비어있지 않은지 확인
        return !string.IsNullOrWhiteSpace(currentData.ChoiceA_Text) ||
               !string.IsNullOrWhiteSpace(currentData.ChoiceB_Text) ||
               !string.IsNullOrWhiteSpace(currentData.ChoiceC_Text);
    }

    /// <summary>
    /// 선택지 표시, Context는 질문으로 유지되고 그 아래에 선택지 버튼들이 표시됨
    /// </summary>
    private void ShowChoices()
    {
        isWaitingForInput = false;

        // 레이아웃 자동 조정
        if (dialoguePanel != null)
        {
            Canvas.ForceUpdateCanvases();
        }
        // 선택지 A 설정
        if (!string.IsNullOrWhiteSpace(currentData.ChoiceA_Text) && choiceA_Button != null && choiceA_Text != null)
        {
            choiceA_Button.gameObject.SetActive(true);
            choiceA_Text.text = currentData.ChoiceA_Text;
        }
        else if (choiceA_Button != null)
        {
            choiceA_Button.gameObject.SetActive(false);
        }

        // 선택지 B 설정
        if (!string.IsNullOrWhiteSpace(currentData.ChoiceB_Text) && choiceB_Button != null && choiceB_Text != null)
        {
            choiceB_Button.gameObject.SetActive(true);
            choiceB_Text.text = currentData.ChoiceB_Text;
        }
        else if (choiceB_Button != null)
        {
            choiceB_Button.gameObject.SetActive(false);
        }

        // 선택지 C 설정
        if (!string.IsNullOrWhiteSpace(currentData.ChoiceC_Text) && choiceC_Button != null && choiceC_Text != null)
        {
            choiceC_Button.gameObject.SetActive(true);
            choiceC_Text.text = currentData.ChoiceC_Text;
        }
        else if (choiceC_Button != null)
        {
            choiceC_Button.gameObject.SetActive(false);
        }

        // 레이아웃 갱신
        if (dialoguePanel != null)
        {
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(dialoguePanel.GetComponent<RectTransform>()); // 강제 레이아웃 갱신
        }
    }

    /// <summary>
    /// 선택지 선택 시 호출되는 메소드
    /// </summary>
    /// <param name="choiceIndex">다음으로 넘어갈 인덱스 번호</param>
    private void OnChoiceSelected(int choiceIndex)
    {
        // 모든 선택지 버튼 숨김
        if (choiceA_Button != null) choiceA_Button.gameObject.SetActive(false);
        if (choiceB_Button != null) choiceB_Button.gameObject.SetActive(false);
        if (choiceC_Button != null) choiceC_Button.gameObject.SetActive(false);

        int nextID = -1; // 다음 대화 ID, 기본값을 -1로 해놓고 선택지의 다음 인덱스 번호가 없으면 대화가 종료되게 함

        switch (choiceIndex)
        {
            case 0: // A
                nextID = currentData.ChoiceA_Next; // 선택지 A의 다음 대화 ID
                break;
            case 1: // B
                nextID = currentData.ChoiceB_Next; // 선택지 B의 다음 대화 ID
                break;
            case 2: // C
                nextID = currentData.ChoiceC_Next; // 선택지 C의 다음 대화 ID
                break;
        }

        if (nextID != -1) // 다음 대화 ID가 -1이 아니면 다음 대화로 진행
        {
            StartDialogue(nextID); // 다음 대화로 진행
        }
        else
        {
            EndDialogue(); // 대화 종료
        }
    }

    /// <summary>
    /// Portrait 위치 정렬
    /// </summary>
    private void UpdatePortraitPosition()
    {
        if (portraitImage != null && dialoguePanel != null)
        {
            RectTransform portraitRect = portraitImage.GetComponent<RectTransform>();
            RectTransform panelRect = dialoguePanel.GetComponent<RectTransform>();

            if (portraitRect != null && panelRect != null)
            {
                portraitRect.anchorMin = new Vector2(1, 1);
                portraitRect.anchorMax = new Vector2(1, 1);
                portraitRect.pivot = new Vector2(1, 0); // Portrait 하단이 패널 상단에 닿도록 위치 잡음
                portraitRect.anchoredPosition = new Vector2(-10, 10); // 패널 오른쪽 끝에서 10px 안쪽, 상단에서 10px 위로
            }
        }
    }

    /// <summary>
    /// 모든 텍스트를 AutoSize로 설정하고 패널 밖으로 나가지 않게 설정
    /// </summary>
    private void SetTextAutoSize()
    {
        // 이름, 내용, 선택지 텍스트들에 대해 AutoSize 설정
        // Overflow 모드 사용 => 내용이 패널 내에서 줄바꿈되도록 설정
        if (nameText != null)
        {
            nameText.enableAutoSizing = true;
            nameText.fontSizeMin = 10;
            nameText.fontSizeMax = 50;
            nameText.overflowMode = TextOverflowModes.Ellipsis;
            
            // RectTransform 크기 제한
            RectTransform nameRect = nameText.GetComponent<RectTransform>();
            if (nameRect != null)
            {
                nameRect.sizeDelta = new Vector2(300, 50);
            }
            
            // Margin(여백) 설정
            nameText.margin = new Vector4(10, 10, 10, 10);
        }

        if (contextText != null)
        {
            contextText.enableAutoSizing = true;
            contextText.fontSizeMin = 8;
            contextText.fontSizeMax = 40;
            contextText.overflowMode = TextOverflowModes.Overflow;
            
            // RectTransform 크기 제한
            // TODO : 추후 패널 크기에 맞게 동적으로 조정하는 기능 추가 고려
            RectTransform contextRect = contextText.GetComponent<RectTransform>();
            if (contextRect != null)
            {
                contextRect.sizeDelta = new Vector2(450, 200);
            }
            
            // Margin(여백) 설정
            contextText.margin = new Vector4(15, 10, 15, 10);
        }

        if (choiceA_Text != null)
        {
            choiceA_Text.enableAutoSizing = true;
            choiceA_Text.fontSizeMin = 8;
            choiceA_Text.fontSizeMax = 35;
            choiceA_Text.overflowMode = TextOverflowModes.Overflow;
            
            RectTransform choiceARect = choiceA_Text.GetComponent<RectTransform>();
            if (choiceARect != null)
            {
                choiceARect.sizeDelta = new Vector2(400, 80);
            }
            
            choiceA_Text.margin = new Vector4(10, 5, 10, 5);
        }

        if (choiceB_Text != null)
        {
            choiceB_Text.enableAutoSizing = true;
            choiceB_Text.fontSizeMin = 8;
            choiceB_Text.fontSizeMax = 35;
            choiceB_Text.overflowMode = TextOverflowModes.Overflow;
            
            RectTransform choiceBRect = choiceB_Text.GetComponent<RectTransform>();
            if (choiceBRect != null)
            {
                choiceBRect.sizeDelta = new Vector2(400, 80);
            }
            
            choiceB_Text.margin = new Vector4(10, 5, 10, 5);
        }

        if (choiceC_Text != null)
        {
            choiceC_Text.enableAutoSizing = true;
            choiceC_Text.fontSizeMin = 8;
            choiceC_Text.fontSizeMax = 35;
            choiceC_Text.overflowMode = TextOverflowModes.Overflow;
            
            RectTransform choiceCRect = choiceC_Text.GetComponent<RectTransform>();
            if (choiceCRect != null)
            {
                choiceCRect.sizeDelta = new Vector2(400, 80);
            }
            
            choiceC_Text.margin = new Vector4(10, 5, 10, 5);
        }
    }

    /// <summary>
    /// 대화 종료 메소드
    /// </summary>
    private void EndDialogue()
    {
        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(false);
        }

        // 모든 선택지 버튼 숨김
        if (choiceA_Button != null) choiceA_Button.gameObject.SetActive(false);
        if (choiceB_Button != null) choiceB_Button.gameObject.SetActive(false);
        if (choiceC_Button != null) choiceC_Button.gameObject.SetActive(false);

        isWaitingForInput = false;

        // 대화 종료 콜백 호출
        if (onDialogueEndCallback != null)
        {
            onDialogueEndCallback.Invoke();
            onDialogueEndCallback = null; // 콜백 초기화
        }
        
        currentReplacementName = null; // 치환 텍스트 초기화
        currentReplacementPortrait = null; // 치환 초상화 초기화
    }
}
