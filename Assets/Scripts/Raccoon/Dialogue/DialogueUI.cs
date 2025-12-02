using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DialogueUI : MonoBehaviour
{
    [Header("UI 컴포넌트")]
    public GameObject dialoguePanel;
    public RectTransform namePanel;
    public RectTransform safeArea; // 텍스트가 들어갈 안전 영역, 해당 영역이 없으면 텍스트 난리남 !!!
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI contextText;
    public Image portraitImage;
    public Transform portraitTransform;

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
        else
        {
            // Safe Area가 있으면 해당 영역으로 텍스트/버튼 이동 및 레이아웃 설정
            if (safeArea != null)
            {
                // 부모 변경 (Reparenting)
                if (contextText != null) contextText.transform.SetParent(safeArea, false);
                if (choiceA_Button != null) choiceA_Button.transform.SetParent(safeArea, false);
                if (choiceB_Button != null) choiceB_Button.transform.SetParent(safeArea, false);
                if (choiceC_Button != null) choiceC_Button.transform.SetParent(safeArea, false);

                // SafeArea에 VerticalLayoutGroup 가져옴
                // VerticalLayoutGroup 없으면 조금 곤란함
                // 왜냐? 안 그러면 Choice가 있을때 Text가 정렬이 안됨
                // 또는 UI가 작아질때 Text가 난리가 남 <= 경험담임
                VerticalLayoutGroup layoutGroup = safeArea.GetComponent<VerticalLayoutGroup>();
                if (layoutGroup == null)
                {
                    layoutGroup = safeArea.gameObject.AddComponent<VerticalLayoutGroup>(); // 없다면 추가
                    layoutGroup.padding = new RectOffset(0, 0, 0, 0);
                    layoutGroup.spacing = 10;
                    layoutGroup.childAlignment = TextAnchor.UpperLeft;
                    layoutGroup.childControlHeight = false; // ContextText가 위에서 시작하도록 false로 변경
                    layoutGroup.childControlWidth = true;
                    layoutGroup.childForceExpandHeight = false; // ContextText가 필요한 만큼만 차지하도록 false로 변경
                    layoutGroup.childForceExpandWidth = true;
                }

                // ContextText를 왼쪽 위에서 시작하도록 정렬 설정
                if (contextText != null)
                {
                    contextText.alignment = TextAlignmentOptions.TopLeft;

                    // LayoutElement 추가하여 높이를 콘텐츠 크기에 맞춤
                    LayoutElement contextLayout = contextText.GetComponent<LayoutElement>();
                    if (contextLayout == null)
                    {
                        contextLayout = contextText.gameObject.AddComponent<LayoutElement>();
                    }
                    contextLayout.flexibleHeight = 0; // 유연한 높이 비활성화
                    contextLayout.preferredHeight = -1; // 자동 높이
                }
            }
            else // 사실 없어도 되는데 없으면 진짜 난리나서 경고 출력
            {
                Debug.LogWarning("SafeArea가 할당되지 않았습니다. 텍스트와 버튼이 패널 밖으로 나갈 수 있습니다.");
                return;
            }
        }

        namePanel = nameText.transform.parent.GetComponent<RectTransform>();

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

    private void LateUpdate()
    {
        UpdateNamePanelPosition();
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

        // 패널 설정
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

        // $가 있을때 해당 텍스트 줄바꿈
        if(context.Contains("$"))
        {
            context = context.Replace("$", "\n");
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
            StopAllCoroutines(); // 이전 타이핑 애니메이션 중지
            StartCoroutine(TypeText(text));
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
    /// 텍스트를 한 글자씩 출력하는 타이핑 코루틴
    /// 애니메이션 효과를 주고싶어 만듦, 추후 조정 가능
    /// </summary>
    private IEnumerator TypeText(string text)
    {
        isTyping = true;
        contextText.text = "";

        float typingSpeed = DialogueManager.Instance != null ? DialogueManager.Instance.typingSpeed : 0.05f;

        foreach (char letter in text.ToCharArray())
        {
            contextText.text += letter;
            yield return new WaitForSeconds(typingSpeed);
        }

        isTyping = false;
        isWaitingForInput = true;
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
    /// Portrait 위치를 SafeArea 기준으로 정렬
    /// </summary>
    private void UpdatePortraitPosition()
    {
        if (portraitImage != null && safeArea != null)
        {
            RectTransform portraitRect = portraitImage.GetComponent<RectTransform>();

            if (portraitRect != null)
            {
                // SafeArea의 월드 좌표를 가져옴
                Vector3 safeAreaPosition = safeArea.position;

                // Portrait를 SafeArea 위치 기준으로 설정
                // 오프셋을 조절하여 항상 Portrait가 원하는 위치에 있도록함
                Vector3 offset = Vector3.zero;
                if (DialogueManager.Instance != null)
                {
                    offset = DialogueManager.Instance.portraitOffset; // DialogueManager에서 오프셋 받음
                }

                portraitRect.position = safeAreaPosition + offset;
            }
        }
    }

    /// <summary>
    /// NamePanel 위치를 DialoguePanel의 오른쪽 상단에 고정하는 메소드
    /// </summary>
    private void UpdateNamePanelPosition()
    {
        if (namePanel != null && dialoguePanel != null)
        {
            RectTransform panelRect = dialoguePanel.GetComponent<RectTransform>();
            if (panelRect != null)
            {
                // DialoguePanel의 월드 좌표 기준, 코너 구하기
                Vector3[] corners = new Vector3[4];
                panelRect.GetWorldCorners(corners);
                // corners[2] = Top-Right
                Vector3 topRight = corners[2];

                // NamePanel의 피벗을 (1, 0) [Bottom-Right]으로 설정하여
                // NamePanel의 오른쪽 아래가 DialoguePanel의 오른쪽 위에 딱 붙게 함
                if (namePanel.pivot != new Vector2(1, 0))
                {
                    namePanel.pivot = new Vector2(1, 0);
                }

                // 오프셋 적용
                Vector3 offset = Vector3.zero;
                if (DialogueManager.Instance != null)
                {
                    offset = DialogueManager.Instance.namePanelOffset;
                }

                namePanel.position = topRight + offset;
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
            if (DialogueManager.Instance != null)
            {
                nameText.fontSizeMax = DialogueManager.Instance.nameTextMaxSize;
            }
            else
            {
                nameText.fontSizeMax = 50;
            }
            nameText.overflowMode = TextOverflowModes.Ellipsis;

            // RectTransform 크기 제한 (NamePanel 내부이므로 유지)
            RectTransform nameRect = nameText.GetComponent<RectTransform>();
            if (nameRect != null)
            {
                nameRect.sizeDelta = new Vector2(300, 50);
            }

            // Margin(여백) 설정
            nameText.margin = new Vector4(5, 2, 5, 2);
        }

        if (contextText != null)
        {
            contextText.enableAutoSizing = true;
            contextText.fontSizeMin = 18;
            if (DialogueManager.Instance != null)
            {
                contextText.fontSizeMax = DialogueManager.Instance.contextTextMaxSize;
            }
            else
            {
                contextText.fontSizeMax = 34;
            }
            contextText.enableWordWrapping = true;
            contextText.overflowMode = TextOverflowModes.Ellipsis;

            // Margin(여백) 설정
            contextText.margin = new Vector4(15, 10, 15, 10);
        }

        if (choiceA_Text != null)
        {
            choiceA_Text.enableAutoSizing = false;
            choiceA_Text.fontSize = 20;
            choiceA_Text.enableWordWrapping = true;
            choiceA_Text.overflowMode = TextOverflowModes.Overflow;

            choiceA_Text.margin = new Vector4(10, 5, 10, 5);
        }

        if (choiceB_Text != null)
        {
            choiceB_Text.enableAutoSizing = false;
            choiceB_Text.fontSize = 20;
            choiceB_Text.enableWordWrapping = true;
            choiceB_Text.overflowMode = TextOverflowModes.Overflow;

            choiceB_Text.margin = new Vector4(10, 5, 10, 5);
        }

        if (choiceC_Text != null)
        {
            choiceC_Text.enableAutoSizing = false;
            choiceC_Text.fontSize = 20;
            choiceC_Text.enableWordWrapping = true;
            choiceC_Text.overflowMode = TextOverflowModes.Overflow;

            choiceC_Text.margin = new Vector4(10, 5, 10, 5);
        }

        // 레이아웃 갱신
        if (safeArea != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(safeArea);
        }
        else if (dialoguePanel != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(dialoguePanel.GetComponent<RectTransform>());
        }
    }

    /// <summary>
    /// 대화 종료 메소드
    /// </summary>
    private void EndDialogue()
    {
        this.gameObject.SetActive(false);

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
