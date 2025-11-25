using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArbeitController : MonoBehaviour
{
    [Header("할당")]
    public IsometricPathfinder pathfinder;
    [SerializeField] private float moveSpeed = 5f;

    [Header("데이터 확인용")]
    [SerializeField] private npc myNpcData;
    private Transform currentTarget;
    private List<Vector3Int> currentPath;
    private int currentPathIndex;
    private bool isMoving = false;
    [HideInInspector]
    public bool isSelected = false; // 선택되었는지, OrderingManager가 바꿈, 알바생 중 단 한명만 True 가능

    [Header("업무 관리")]
    [SerializeField] private List<TaskInfo> taskQueue = new List<TaskInfo>(); // 업무 큐 (최대 3개)
    private const int MAX_TASKS = 3; // 최대 업무 갯수, 상수인데 필요 시 스크립트에서 설정
    // 기획단계에서 바꾸는거 아니면 절대 건들면 안됨 !!
    private TaskInfo currentTask = null; // 현재 수행 중인 업무
    [SerializeField] private float taskUI_Y_Offset = 1.5f; // 알바생 위 UI 오프셋
    private List<GameObject> taskUIObjects = new List<GameObject>(); // 알바생에게 표시되는 업무 UI 리스트

    private Camera mainCamera;

    private void Awake()
    {
        mainCamera = Camera.main;
    }

    #region Initialization
    /// <summary>
    /// 초기화
    /// </summary>
    /// <param name="알바 데이터"></param> <summary>
    public void Initialize(npc npcData)
    {
        this.myNpcData = npcData;
        // 후에 이동 속도 보정 등, 필요한 경우 여기에 추가할 것
        // ex: moveSpeed += npcData.serving_ability * 0.1f;
    }
    #endregion

    #region 이동 및 길찾기
    /// <summary>
    /// Target 설정, IsometricPathfinder를 통해 경로 계산 시작
    /// 만약 IsometricPathfinder가 null이면 작동을 하지않으니 참고바람
    /// </summary>
    public void SetTarget(Transform newTarget)
    {   
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
            
            // ignoreLayerMask를 반전시켜서 해당 레이어를 제외하고 Raycast
            int layerMask = ~CameraManager.instance.ignoreLayerMask.value;
            RaycastHit2D hitCollider = Physics2D.Raycast(pos, Vector2.zero, 0, layerMask);

            if (hitCollider.collider != null)
            {
                if (hitCollider.transform == this.transform)
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
        // 우선 이렇게 작업을 하나, 만약 문제가 생긴다면,,, 이거 만든 놈한테 연락주길 바람,,,,,,,
        
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
        // 현재 업무가 있으면
        if (currentTask != null)
        {
            ProcessTask(currentTask); // 해당 업무 진행
        }
    }
    #endregion

    #region 업무 관리
    /// <summary>
    /// 업무 추가 (최대 3개까지)
    /// </summary>
    public bool AddTask(TaskInfo task)
    {
        if (taskQueue.Count >= MAX_TASKS) // 가득차면 업무 추가 불가능
        {
            return false;
        }

        taskQueue.Add(task); // 업무 큐에 추가
        
        UpdateTaskUI(); // UI 업데이트
        
        // 현재 수행 중인 업무가 없으면
        if (currentTask == null && !isMoving)
        {
            StartNextTask(); // 현재 있는 첫번째 업무를 바로 수행함
        }
        
        return true;
    }

    /// <summary>
    /// 다음 업무 시작하는 메소드
    /// </summary>
    private void StartNextTask()
    {
        if (taskQueue.Count == 0)
        {
            currentTask = null;
            return;
        }

        // 알바생의 첫 번째 업무를 가져옴
        currentTask = taskQueue[0];
        taskQueue.RemoveAt(0);
        
        Debug.Log($"{myNpcData?.part_timer_name} 업무 시작: {currentTask.taskType}");
        
        // 업무 대상 위치로 이동
        if (currentTask.targetObject != null)
        {
            SetTarget(currentTask.targetObject.transform);
        }
        
        UpdateTaskUI(); // UI 업데이트
    }

    /// <summary>
    /// 업무 처리
    /// </summary>
    private void ProcessTask(TaskInfo task)
    {
        if (task == null)
        {
            Debug.Log("현재 처리할 업무가 없음");
            return;
        }

        switch (task.taskType) // 업무 유형에 따라 업무 처리해야 함
        {
            case TaskType.TakeOrder:
                // 칵테일 주문 받기 업무 처리
                ProcessTakeOrder(task);
                break;
                
            case TaskType.ServeOrder:
                // 서빙 업무 처리
                ProcessServeOrder(task);
                break;
                
            case TaskType.CleanTable:
                // 테이블 청소 업무 처리
                ProcessCleanTable(task);
                break;
        }
    }

    /// <summary>
    /// 주문 받기 업무 처리
    /// </summary>
    private void ProcessTakeOrder(TaskInfo task)
    {
        // 업무 UI 제거 (도착 시 UI 사라짐)
        if (task.taskUI != null)
        {
            Destroy(task.taskUI);
        }
        
        // 추후 대화창 여는 로직 구현해야 함
    }

    /// <summary>
    /// 서빙 업무 처리
    /// </summary>
    private void ProcessServeOrder(TaskInfo task)
    {
        // 여기서 추가로 서빙할 로직을 구현해야함
        // ex) 서빙할 칵테일 확인
        // 서빙 중일때 애니메이션 재생 등등
        // 우선 로직만 해놓고... 후에 해보겠음
        
        // 서빙 완료 처리
        CompleteCurrentTask();
    }

    /// <summary>
    /// 청소 업무 처리
    /// </summary>
    private void ProcessCleanTable(TaskInfo task)
    {
        // 여기서 추가할 청소를 하는 로직을 구현해야함
        // ex) 청소 애니메이션 재생 등등
        // 우선 로직만 해놓고... 후에 해보겠음
        CompleteCurrentTask();
    }

    /// <summary>
    /// 현재 업무 완료 처리
    /// </summary>
    public void CompleteCurrentTask()
    {
        if (currentTask != null)
        {
            // OrderingManager에서 업무 제거
            OrderingManager.Instance.RemoveTask(currentTask);
            
            currentTask = null;
            isMoving = false;

            // 다음 업무 시작
            StartNextTask();
        }
    }

    /// <summary>
    /// 현재 업무 취소 처리
    /// </summary>
    public void CancelCurrentTask()
    {
        if (currentTask != null)
        {
            // OrderingManager에서 업무 제거
            OrderingManager.Instance.RemoveTask(currentTask);
            
            currentTask = null;
            isMoving = false;
            
            // 다음 업무 시작
            StartNextTask();
        }
    }

    /// <summary>
    /// 특정 업무 큐에서 제거
    /// </summary>
    public void RemoveTaskFromQueue(TaskInfo task)
    {
        if (taskQueue.Contains(task))
        {
            taskQueue.Remove(task);
            UpdateTaskUI();
        }
    }

    /// <summary>
    /// 다른 알바생이 먼저 처리한 경우
    /// 완료된 업무와 일치하는 업무를 큐에서 제거 
    /// </summary>
    public void RemoveTaskIfMatch(TaskInfo completedTask)
    {
        if (completedTask == null) return;

        // 현재 수행 중인 업무가 완료된 업무와 동일한 타겟이면 취소
        if (currentTask != null && 
            currentTask.targetObject == completedTask.targetObject &&
            currentTask.taskType == completedTask.taskType)
        {
            CancelCurrentTask();
            return;
        }

        // 큐에 있는 업무 중 일치하는 것 제거
        TaskInfo matchingTask = taskQueue.Find(t => 
            t.targetObject == completedTask.targetObject && 
            t.taskType == completedTask.taskType);
        
        if (matchingTask != null)
        {
            taskQueue.Remove(matchingTask);
            UpdateTaskUI();
        }
    }
    #endregion

    #region 업무 UI 관리
    /// <summary>
    /// 업무 UI 업데이트 (ㅁ) => (ㅁ ㅁ) => (ㅁ ㅁ ㅁ) 이렇게 가로로 확장됨
    /// </summary>
    private void UpdateTaskUI()
    {
        // 기존 UI 모두 제거
        foreach (var uiObj in taskUIObjects)
        {
            if (uiObj != null)
            {
                Destroy(uiObj);
            }
        }
        taskUIObjects.Clear();
        
        int totalTasks = taskQueue.Count;
        if (currentTask != null) totalTasks++;

        // 업무 개수만큼 UI 생성
        for (int i = 0; i < totalTasks; i++)
        {
            // 첫 번째는 현재 업무
            TaskInfo task;
            if (i == 0 && currentTask != null)
            {
                task = currentTask; // 현재 수행 중인 업무
            }
            else // 나머지는 큐에서 2,3번째 업무를 가져옴
            {
                int queueIndex = (currentTask != null) ? i - 1 : i;
                task = taskQueue[queueIndex]; // 큐의 대기 업무
            }
            
            // 알바생 머리 위에 업무 UI 생성
            GameObject taskUI = OrderingManager.Instance.TaskUIInstantiate(this.gameObject, task, taskUI_Y_Offset, i);
            if (taskUI != null)
            {
                taskUIObjects.Add(taskUI);
            }
        }
    }

    #endregion

    #region 업무 상태 확인 메소드
    /// <summary>
    /// 현재 업무 큐 상태 확인, OrderingManager에서 사용
    /// </summary>
    public bool CanAddTask()
    {
        return taskQueue.Count < MAX_TASKS;
    }

    /// <summary>
    /// 현재 업무 개수 반환
    /// </summary>
    public int GetTaskCount()
    {
        int count = taskQueue.Count;
        if (currentTask != null) count++;
        return count;
    }

    /// <summary>
    /// 현재 수행 중인 업무 반환
    /// </summary>
    public TaskInfo GetCurrentTask()
    {
        return currentTask;
    }

    #endregion
}
