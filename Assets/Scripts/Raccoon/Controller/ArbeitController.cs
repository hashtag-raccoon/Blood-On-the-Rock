using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArbeitController : MonoBehaviour
{
    [Header("할당")]
    public IsometricPathfinder pathfinder;
    [SerializeField] private float moveSpeed = 5f;

    [Header("데이터 확인용")]
    public npc myNpcData;
    private Transform currentTarget;
    private List<Vector3Int> currentPath;
    private int currentPathIndex;
    private bool isMoving = false;
    [HideInInspector]
    public bool isSelected = false;

    [Header("대기열 관리")]
    private ArbeitPoint arbeitPoint;
    private int myWaitingPosition = -1;
    private GameObject waitingTargetObject;
    private bool isWaiting = false;
    public bool IsWaiting => isWaiting;

    [Header("업무 관리")]
    private const int MAX_TASKS = 3; // 최대 업무 개수
    private List<TaskInfo> taskQueue = new List<TaskInfo>(); // 업무 큐
    private TaskInfo currentTask = null; // 현재 수행 중인 업무
    [SerializeField] private float taskUI_Y_Offset = 1.5f; // 업무 UI Y축 오프셋
    private List<GameObject> taskUIObjects = new List<GameObject>(); // 업무 UI 오브젝트 리스트

    // 대화 대기열
    private struct DialogQueueItem
    {
        public TaskInfo task;
        public Vector2? panelSize;
        public int startIndex;
        public string replacementName;
        public string portraitName;
        public Sprite portraitSprite;
        public bool useSprite;
    }
    private Queue<DialogQueueItem> dialogQueue = new Queue<DialogQueueItem>();

    private Camera mainCamera;

    [Header("클릭 감지 레이어")]
    [SerializeField] private LayerMask clickLayer;

    public LayerMask ClickLayerMask
    {
        get => clickLayer;
        set => clickLayer = value;
    }

    private void Awake()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("Main Camera를 찾을 수 없습니다. 카메라에 'MainCamera' 태그가 있는지 확인해주세요.");
        }
    }

    public void SetClickLayer(LayerMask layer)
    {
        clickLayer = layer;
    }

    #region Initialization
    /// <summary>
    /// 초기화
    /// </summary>
    /// <param name="알바 데이터"></param> <summary>
    public void Initialize(npc npcData)
    {
        this.myNpcData = npcData;
        // 데이터를 통해 이동 속도 보정 등이 필요할 경우 여기에 추가할 것
        // ex: moveSpeed += npcData.serving_ability * 0.1f;
    }

    /// <summary>
    /// ArbeitPoint 설정 및 대기열 추가
    /// </summary>
    public void SetArbeitPoint(ArbeitPoint point)
    {
        arbeitPoint = point;
        if (arbeitPoint != null)
        {
            myWaitingPosition = arbeitPoint.AddToWaitingLine(this.gameObject);
            SetWaitingTarget();
        }
    }
    #endregion

    #region 이동 및 길찾기
    /// <summary>
    /// Target 설정, IsometricPathfinder를 통해 경로 계산 시작
    /// 만약 IsometricPathfinder가 null이면 작동을 하지않으니 참고바람
    /// </summary>
    public void SetTarget(Transform newTarget)
    {
        if (pathfinder == null)
        {
            Debug.LogError("IsometricPathfinder가 할당되지 않았습니다.");
            return;
        }
        currentTarget = newTarget;
        CalculatePath();
    }

    /// <summary>
    /// 경로 계산
    /// </summary>
    private void CalculatePath()
    {
        if (currentTarget == null) return;

        Vector3 targetPos = new Vector3(currentTarget.position.x, currentTarget.position.y, 0);
        Vector3 startPos = new Vector3(this.transform.position.x, this.transform.position.y, 0);

        Vector3Int targetCell = pathfinder.WorldToCell(targetPos);
        Vector3Int startCell = pathfinder.WorldToCell(startPos);

        // 시작점에서 목표점까지의 경로 계산
        currentPath = pathfinder.FindPath(startCell, targetCell, 1, 1);

        // 경로가 유효한지 확인 후 이동 시작
        // A* 알고리즘을 통해 경로를 반환하고, 최소 이동 가능한 노드값이 0 이상이면 이동시작
        if (currentPath != null && currentPath.Count > 0)
        {
            pathfinder.DrawPath(currentPath); // 디버그용 경로 그리기
            currentPathIndex = 0;
            isMoving = true;
        }
        else
        {
            isMoving = false;
            Debug.Log("경로를 찾을 수 없습니다.");
        }
    }

    void Update()
    {
        /// <summary>
        /// 마우스 클릭 감지 및 알바 선택 처리
        /// </summary>
        if (Input.GetMouseButtonDown(0))
        {
            Vector2 pos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            int layerMask = clickLayer != 0 ? clickLayer : ~0;
            if (clickLayer == 0 && CameraManager.instance != null)
            {
                layerMask = ~CameraManager.instance.ignoreLayerMask.value;
            }

            RaycastHit2D hitCollider = Physics2D.Raycast(pos, Vector2.zero, 0, layerMask);

            if (hitCollider.collider != null)
            {
                Transform hitTransform = hitCollider.transform;
                if (hitTransform == this.transform || hitTransform.IsChildOf(this.transform))
                {
                    OrderingManager.Instance.ToggleSelected(this.gameObject);
                }
            }
        }

        // 매 프레임마다 이동 가능한 노드값이 Null 이 아니고, 이동 중이면 이동 처리
        if (isMoving && currentPath != null)
        {
            MoveAlongPath();
        }
    }

    /// <summary>
    /// 현재 경로를 따라 이동 처리함
    /// </summary>
    void MoveAlongPath()
    {
        // 경로의 끝에 도달했는지 확인
        if (currentPathIndex >= currentPath.Count)
        {
            // 경로 끝에 도달
            isMoving = false;
            OnReachedDestination(); // 목적지 도착 시 호출
            return;
        }

        Vector3 targetPos = pathfinder.CellToWorld(currentPath[currentPathIndex]);
        // IsometricPathfinder의 CellToWorld가 타일 중심을 반환한다면 정상 작동하는 코드
        // 만약 아니라면 오프셋 조정이 필요하거나 다른 방식으로 목표 위치를 계산해야 할 수 있음

        // 현재 위치에서 목표 위치로 이동
        this.transform.position = Vector3.MoveTowards(this.transform.position, targetPos, moveSpeed * Time.deltaTime);

        // 목표 위치에 근접했거나 도달해했는지 확인
        if (Vector3.Distance(this.transform.position, targetPos) < 0.01f)
        {
            // 다음 경로 노드로 이동
            currentPathIndex++;
        }
    }
    #endregion

    #region 목적지 도착
    /// <summary>
    /// 목적지에 도착했을 때 호출되는 메소드
    /// </summary>
    void OnReachedDestination()
    {
        // 대기 위치에 도착한 경우
        if (myWaitingPosition != -1 && waitingTargetObject != null)
        {
            Vector3 waitingPos = arbeitPoint.CalculateWaitingPosition(myWaitingPosition);
            if (Vector3.Distance(transform.position, waitingPos) < 0.1f)
            {
                isWaiting = true;
                return;
            }
        }

        if (currentTask != null && currentTask.taskType == TaskType.TakeOrder)
        {
            GuestController guest = currentTask.targetObject.GetComponent<GuestController>();
            string csvName = "Human_OrderDialogue";
            int dialogueIndex = 0;
            string[] prefixes = null;

            if (guest != null && guest.customerData != null)
            {
                // 종족에 따라 다른 CSV 파일과 접두사 배열 설정
                // 현재는 하드 코딩 했지만 추후에 다른 방식으로 관리할 예정
                switch (guest.customerData.race_id)
                {
                    case 0: // Human
                        csvName = "Human_OrderDialogue";
                        prefixes = new string[] { "교섭관", "농부", "기사단장", "계약중개인", "무역감시관", "일반" };
                        break;
                    case 1: // Orc
                        csvName = "Oak_OrderDialogue";
                        prefixes = new string[] { "전투 우두머리", "고기 사냥꾼", "혈투 전사", "부족 수호자", "전투 요리사", "일반" };
                        break;
                    case 2: // Vampire
                        csvName = "Vampire_OrderDialogue";
                        prefixes = new string[] { "혈맹 장군", "순혈 집행관", "가문 감시자", "전통 심판자", "고문헌 수호자", "일반" };
                        break;
                }

                // 대화 CSV 로드
                DialogueManager.Instance.LoadDialogue(csvName);

                // 손님 이름에서 접두사 추출하여 대화 인덱스 찾기
                if (prefixes != null)
                {
                    string customerName = guest.customerData.customer_name;
                    List<int> matchingIndices = new List<int>();

                    // 해당 접두사로 시작하는 모든 대화 인덱스를 찾음
                    for (int i = 0; i < DialogueManager.Instance.dialogueDic.Count; i++)
                    {
                        DialogueData data = DialogueManager.Instance.GetDialogue(i);
                        if (data != null)
                        {
                            // 대화 데이터의 Name이 손님 이름의 접두사와 일치하는지 확인
                            foreach (string prefix in prefixes)
                            {
                                if (customerName.StartsWith(prefix) && data.Name == prefix)
                                {
                                    matchingIndices.Add(i);
                                    break;
                                }
                            }
                        }
                    }

                    // 매칭된 대화 중 랜덤 선택
                    if (matchingIndices.Count > 0)
                    {
                        dialogueIndex = matchingIndices[Random.Range(0, matchingIndices.Count)];
                    }
                    else
                    {
                        Debug.LogWarning($"손님 이름 '{customerName}'의 접두사를 찾을 수 없음, 기본 인덱스 0 사용.");
                    }
                }
            }

            // 종족에 맞는 타이핑 속도 설정 (대화 시작 전에 먼저 설정)
            DialogueManager.Instance.RaceToTyping(guest.customerData.race_id);

            // CustomerData에 있는 Portrait 스프라이트 사용
            Sprite portraitSprite = guest.customerData.portraitSprite;

            // Portrait 스프라이트가 없으면 경고
            if (portraitSprite == null)
            {
                Debug.LogWarning($"손님 '{guest.customerData.customer_name}'의 portraitSprite가 없습니다.");
            }

            // OrderingManager를 통해 대화 시작 (스프라이트를 직접 전달하는 방식)
            OrderingManager.Instance.OpenDialog(this.gameObject, currentTask, OrderingManager.Instance.orderDialogPanelSize,
            dialogueIndex, guest.customerData.customer_name, portraitSprite);

        }
    }
    #endregion

    #region 업무 관리
    /// <summary>
    /// 업무 추가
    /// </summary>
    public bool AddTask(TaskInfo task)
    {
        if (task == null || taskQueue.Count >= MAX_TASKS)
        {
            return false;
        }

        taskQueue.Add(task); // 업무 큐에 추가
        UpdateTaskUI(); // 큐에 추가 후 TaskUI 업데이트하여 표시함

        // 현재 업무가 없으면 바로 시작
        if (currentTask == null)
        {
            StartNextTask();
        }

        return true;
    }

    /// <summary>
    /// 다음 업무 시작, 현재 업무가 없을 때 호출됨
    /// </summary>
    private void StartNextTask()
    {
        if (taskQueue.Count > 0)
        {
            currentTask = taskQueue[0];
            taskQueue.RemoveAt(0);
            UpdateTaskUI();
            ProcessTask(currentTask);
        }
        else
        {
            currentTask = null;
            UpdateTaskUI();
        }
    }

    /// <summary>
    /// 업무 처리, 업무 타입에 따라 바로 일을 수행함
    /// </summary>
    private void ProcessTask(TaskInfo task)
    {
        if (task == null) return;

        switch (task.taskType)
        {
            case TaskType.TakeOrder:
                ProcessTakeOrder(task);
                break;
            case TaskType.ServeOrder:
                ProcessServeOrder(task);
                break;
            case TaskType.CleanTable:
                ProcessCleanTable(task);
                break;
        }
    }

    /// <summary>
    /// 주문 받기 업무 처리
    /// </summary>
    private void ProcessTakeOrder(TaskInfo task)
    {
        SetTarget(task.targetObject.transform); // Target을 향해 이동 시작함
    }

    // 대화창 종료 후 호출할 예정
    public void EndTakeOrder()
    {
        CompleteCurrentTask(); // 현재 업무 완료 처리, 완료한 업무는 모든 알바생도 제거됨
    }

    /// <summary>
    /// 서빙 업무 처리
    /// </summary>
    private void ProcessServeOrder(TaskInfo task)
    {
        // 추후 추가할 일: 칵테일 제조 후 테이블로 이동
        CompleteCurrentTask();
    }

    /// <summary>
    /// 청소 업무 처리
    /// </summary>
    private void ProcessCleanTable(TaskInfo task)
    {
        // 추후 추가할 일: 테이블로 이동 후 청소
        CompleteCurrentTask();
    }

    /// <summary>
    /// 현재 업무 완료
    /// </summary>
    public void CompleteCurrentTask()
    {
        if (currentTask != null)
        {
            // OrderingManager에서 업무 제거 (RemoveTask 내부에서 CompleteTask 호출됨)
            if (OrderingManager.Instance != null)
            {
                OrderingManager.Instance.RemoveTask(currentTask);
            }
            else
            {
                Debug.LogError("OrderingManager.Instance가 null입니다!");
            }

            currentTask = null;

            // 대기 위치로 복귀
            ReturnToWaitingPosition();

            // 다음 업무 시작
            StartNextTask();
        }
        else
        {
            Debug.LogWarning("currentTask가 null입니다!");
        }
    }

    /// <summary>
    /// 현재 업무 취소
    /// </summary>
    public void CancelCurrentTask()
    {
        if (currentTask != null)
        {
            currentTask = null;

            // 대기 위치로 복귀
            ReturnToWaitingPosition();

            StartNextTask();
        }
    }

    /// <summary>
    /// 큐에서 특정 업무 제거
    /// </summary>
    public void RemoveTaskFromQueue(TaskInfo task)
    {
        // 현재 실행 중인 업무인지 확인
        if (currentTask != null && currentTask == task)
        {
            CancelCurrentTask();
            return;
        }

        // 큐에 있는 업무인지 확인
        if (taskQueue.Contains(task))
        {
            taskQueue.Remove(task);
            UpdateTaskUI();
        }
    }

    /// <summary>
    /// 다른 알바생이 완료한 업무와 동일한 업무 제거
    /// </summary>
    public void RemoveTaskIfMatch(TaskInfo completedTask)
    {
        // 현재 수행 중인 업무가 동일한 타겟인지 확인
        if (currentTask != null && currentTask.targetObject == completedTask.targetObject)
        {
            CancelCurrentTask();
            return;
        }

        // 큐에 있는 업무 중 동일한 타겟 제거
        TaskInfo matchingTask = taskQueue.Find(t => t.targetObject == completedTask.targetObject);
        if (matchingTask != null)
        {
            taskQueue.Remove(matchingTask);
            UpdateTaskUI();
        }
    }

    /// <summary>
    /// 업무 UI 업데이트
    /// </summary>
    private void UpdateTaskUI()
    {
        // 업무 큐가 비어있으면 모든 UI 제거, 그 후 리턴
        if (taskQueue == null)
        {
            if (taskUIObjects.Count > 0)
            {
                for (int i = 0; i < taskUIObjects.Count; i++)
                {
                    Destroy(taskUIObjects[i]);
                }
            }
            return;
        }
        // 기존 UI 제거
        foreach (var uiObj in taskUIObjects)
        {
            if (uiObj != null)
            {
                Destroy(uiObj);
            }
        }
        taskUIObjects.Clear();

        // 현재 업무 UI 생성
        if (currentTask != null)
        {
            GameObject uiObj = OrderingManager.Instance.TaskUIInstantiate(this.gameObject, currentTask, taskUI_Y_Offset, 0);
            taskUIObjects.Add(uiObj);
        }

        // 큐의 업무들 UI 생성
        for (int i = 0; i < taskQueue.Count; i++)
        {
            GameObject uiObj = OrderingManager.Instance.TaskUIInstantiate(this.gameObject, taskQueue[i], taskUI_Y_Offset, i + 1);
            taskUIObjects.Add(uiObj);
        }
    }

    /// <summary>
    /// 현재 업무 가져오기
    /// </summary>
    public TaskInfo GetCurrentTask()
    {
        return currentTask;
    }

    /// <summary>
    /// 업무 개수 가져오기
    /// </summary>
    public int GetTaskCount()
    {
        int count = taskQueue.Count;
        if (currentTask != null)
        {
            count++;
        }
        return count;
    }

    /// <summary>
    /// 업무 추가 가능 여부 확인
    /// 현재 수행중인 업무를 추가하려고 할때, 최대 업무 수에 도달하였을때
    /// 이미 업무 큐에 있는 업무를 추가하려고 할 때 업무 추가를 막음
    /// </summary>
    public bool CanAddTask(GameObject taskUI)
    {
        // 최대 업무 수를 초과하면 추가 불가
        if (GetTaskCount() >= MAX_TASKS)
        {
            return false;
        }

        // 이미 동일한 업무가 있는지 확인
        TaskInfo newTask = taskUI.GetComponent<TaskUIController>().assignedTask;
        if (newTask != null)
        {
            // 현재 수행 중인 업무와 동일한지 확인
            if (currentTask != null && currentTask == newTask)
            {
                Debug.LogWarning($"[ArbeitController] 이미 수행 중인 동일한 업무입니다: {newTask.taskType}");
                return false;
            }

            // 대기 중인 업무 큐에 동일한 업무가 있는지 확인
            if (taskQueue.Contains(newTask))
            {
                Debug.LogWarning($"[ArbeitController] 이미 대기 중인 동일한 업무입니다: {newTask.taskType}");
                return false;
            }
        }

        return true;
    }
    #endregion

    #region 대기열 관리
    /// <summary>
    /// 대기 위치 타겟 설정
    /// </summary>
    private void SetWaitingTarget()
    {
        if (arbeitPoint == null || myWaitingPosition == -1) return;

        Vector3 waitingPos = arbeitPoint.CalculateWaitingPosition(myWaitingPosition);

        if (waitingTargetObject == null)
        {
            waitingTargetObject = new GameObject($"WaitingTarget_{this.gameObject.name}");
        }

        waitingTargetObject.transform.position = waitingPos;
        currentTarget = waitingTargetObject.transform;
        isMoving = false;
    }

    /// <summary>
    /// 대기 위치로 복귀
    /// </summary>
    public void ReturnToWaitingPosition()
    {
        if (arbeitPoint != null && myWaitingPosition != -1)
        {
            isWaiting = false;
            SetWaitingTarget();
            SetTarget(waitingTargetObject.transform);
        }
    }

    /// <summary>
    /// 대기 위치 업데이트 (다른 알바가 제거되었을 때 호출됨)
    /// </summary>
    public void UpdateWaitingPosition(int newPosition)
    {
        if (myWaitingPosition != -1)
        {
            myWaitingPosition = newPosition;

            // 현재 업무가 없고 대기 중일 때만 위치 업데이트
            if (currentTask == null && isWaiting)
            {
                SetWaitingTarget();
            }
        }
    }

    #endregion

    #region 대화 대기열 관리
    /// <summary>
    /// 대화 대기열에 추가 (String 버전)
    /// </summary>
    public void AddToDialogQueue(TaskInfo task, Vector2? panelSize, int startIndex, string replacementName, string portraitName)
    {
        dialogQueue.Enqueue(new DialogQueueItem
        {
            task = task,
            panelSize = panelSize,
            startIndex = startIndex,
            replacementName = replacementName,
            portraitName = portraitName,
            useSprite = false
        });
    }

    /// <summary>
    /// 대화 대기열에 추가 (Sprite 버전)
    /// </summary>
    public void AddToDialogQueue(TaskInfo task, Vector2? panelSize, int startIndex, string replacementName, Sprite portraitSprite)
    {
        dialogQueue.Enqueue(new DialogQueueItem
        {
            task = task,
            panelSize = panelSize,
            startIndex = startIndex,
            replacementName = replacementName,
            portraitSprite = portraitSprite,
            useSprite = true
        });
    }

    /// <summary>
    /// 대화 대기열에 항목이 있는지 확인
    /// </summary>
    public bool HasDialogInQueue()
    {
        return dialogQueue.Count > 0;
    }

    /// <summary>
    /// 다음 대화 처리
    /// </summary>
    public void ProcessNextDialog()
    {
        if (dialogQueue.Count == 0)
        {
            return;
        }

        DialogQueueItem item = dialogQueue.Dequeue();

        if (item.useSprite)
        {
            OrderingManager.Instance.OpenDialog(this.gameObject, item.task, item.panelSize, item.startIndex, item.replacementName, item.portraitSprite);
        }
        else
        {
            OrderingManager.Instance.OpenDialog(this.gameObject, item.task, item.panelSize, item.startIndex, item.replacementName, item.portraitName);
        }
    }
    #endregion

    private void OnDestroy()
    {
        // 대기 타겟 오브젝트 제거
        if (waitingTargetObject != null)
        {
            DestroyImmediate(waitingTargetObject);
        }

        // 대기열에서 제거
        if (arbeitPoint != null)
        {
            arbeitPoint.RemoveFromWaitingLine(this.gameObject);
        }
    }
}
