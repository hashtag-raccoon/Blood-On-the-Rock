using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Tilemaps;

public class OrderingManager : MonoBehaviour
{
    private static OrderingManager _instance;
    [Header("선택 시 외곽선/기본 마테리얼 할당")]
    [SerializeField] private Material selectedOutlineMaterial;
    [SerializeField] private Material DefaultMaterial;
    [Header("업무 UI 프리펜")]
    [SerializeField] private GameObject TaskUIPrefab;

    [Header("업무 타입별 아이콘")]
    [SerializeField] private Sprite takeOrderIcon; // 주문 받기 아이콘
    [SerializeField] private Sprite serveOrderIcon; // 서빙 아이콘
    [SerializeField] private Sprite cleanTableIcon; // 청소 아이콘
    [Header("업무 UI 패널 크기")]
    [SerializeField] private Vector2 taskPanelSize; // 업무 UI 패널 크기

    public ArbeitController[] Arbiets;


    public static OrderingManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<OrderingManager>();
            }
            return _instance;
        }
    }

    [HideInInspector]
    public GameObject CurrentSelected = null; // 현재 선택한 알바생, 우클릭 시 null 됨

    [Header("업무 관리")]
    [SerializeField] private List<TaskInfo> allTasks = new List<TaskInfo>(); // 모든 업무 리스트


    /// <summary>
    /// 추후 구현할 대화창 전용
    /// </summary>
    [Header("대화창 상태")]
    [HideInInspector]
    public bool isDialogOpen = false; // 대화창이 열려있는지 여부
    [HideInInspector]
    public GameObject dialogOwner = null; // 대화창을 연 알바생

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        for (int i = 0; i < Arbiets.Length; i++)
        {
            Arbiets[i].myNpcData = DataManager.Instance.npcs[i];
        }

    }

    public void Update()
    {
        // 우클릭 시 알바생 선택 취소
        if (Input.GetMouseButtonDown(1))
        {
            SelectCancel();
        }
    }

    #region 알바생 선택 관리
    /// <summary>
    /// 알바 선택 토글
    /// </summary>
    public void ToggleSelected(GameObject SelectedArbeit)
    {
        if (SelectedArbeit == null)
        {
            Debug.Log("선택된 알바가 없습니다. || 매개변수를 제대로 넣지않음");
            return;
        }

        // 대화창이 열려있는 동안에는 다른 알바생 선택 불가
        if (isDialogOpen && dialogOwner != SelectedArbeit)
        {
            Debug.LogWarning("대화창 열린 중에는 알바생 선택 불가, ESC - 대화창 종료 or 주문 완료할 것");
            return;
        }

        /// <summary>
        /// 중복 선택 못하게, 알바를 클릭할때마다 호출하게 해서
        /// 이미 이전에 선택한 알바가 있다면 선택 활성화 끄고,
        /// 새로 선택한 알바를 활성화하고, 이전 선택한 알바를 갱신
        /// </summary>
        if (CurrentSelected != null)
        {
            CurrentSelected.GetComponent<ArbeitController>().isSelected = false;
            CurrentSelected.GetComponent<SpriteRenderer>().material = DefaultMaterial;
        }
        CurrentSelected = SelectedArbeit;
        CurrentSelected.GetComponent<ArbeitController>().isSelected = true;
        CurrentSelected.GetComponent<SpriteRenderer>().material = selectedOutlineMaterial;
    }

    /// <summary>
    /// 선택 취소, 우클릭 시 호출
    /// </summary>
    public void SelectCancel()
    {
        if (CurrentSelected != null)
        {
            CurrentSelected.GetComponent<ArbeitController>().isSelected = false;
            CurrentSelected.GetComponent<SpriteRenderer>().material = DefaultMaterial;
            CurrentSelected = null;
        }
    }
    #endregion

    #region 업무 아이콘 관리

    /// <summary>
    /// TaskType에 따라 아이콘 스프라이트 반환
    /// 만약 할당 안되면 흰색 아이콘만 나오니 주의
    /// </summary>
    public Sprite GetTaskIconSprite(TaskType taskType)
    {
        switch (taskType)
        {
            case TaskType.TakeOrder:
                return takeOrderIcon;
            case TaskType.ServeOrder:
                return serveOrderIcon;
            case TaskType.CleanTable:
                return cleanTableIcon;
            default:
                return null;
        }
    }
    #endregion

    #region 업무 생성
    /// <summary>
    /// 업무 생성 메소드
    /// </summary>
    public TaskInfo CreateTask(GameObject TargetObj, TaskType taskType)
    {
        TaskInfo newTask = null;

        switch (taskType)
        {
            case TaskType.TakeOrder:
                // 칵테일 업무 생성 로직
                // DataManager의 칵테일 리스트에서 랜덤하게 선택
                if (CocktailRepository.Instance._cocktailRecipeDict.Count > 0)
                {
                    //int randomIndex = UnityEngine.Random.Range(0, DataManager.Instance.cocktails.Count);
                    //CocktailData randomCocktail = DataManager.Instance.cocktails[randomIndex];

                    int randomIndex = UnityEngine.Random.Range(0, CocktailRepository.Instance._cocktailRecipeDict.Count);
                    CocktailRecipeScript randomCocktail = CocktailRepository.Instance._cocktailRecipeDict[randomIndex];

                    newTask = new TaskInfo(TaskType.TakeOrder, TargetObj, randomCocktail);
                    allTasks.Add(newTask);

                    // 업무 UI 생성
                    TaskUIInstantiate(TargetObj);
                }
                else
                {
                    Debug.LogError("칵테일 데이터가 없습니다!");
                }
                break;

            case TaskType.ServeOrder:
                // 서빙 업무 생성 로직
                newTask = new TaskInfo(TaskType.ServeOrder, TargetObj);
                allTasks.Add(newTask);
                break;

            case TaskType.CleanTable:
                // 테이블 청소 업무 생성 로직
                newTask = new TaskInfo(TaskType.CleanTable, TargetObj);
                allTasks.Add(newTask);
                break;

            default:
                Debug.LogWarning("알 수 없는 업무 유형입니다.");
                break;
        }

        return newTask;
    }
    #endregion

    #region 업무 UI 관리
    /// <summary>
    /// 업무 UI 생성 (손님/테이블 등 타겟 오브젝트용)
    /// ex) 손님 위에 주문 UI 띄우기
    /// ex) 테이블 위에 청소 UI 띄우기
    /// </summary>
    public void TaskUIInstantiate(GameObject TargetObj)
    {
        GameObject TaskUIObj = Instantiate(TaskUIPrefab);

        // TaskUIController로 UI 초기화 위임
        TaskUIController uiController = TaskUIObj.GetComponent<TaskUIController>();
        if (uiController == null)
        {
            uiController = TaskUIObj.AddComponent<TaskUIController>();
        }

        Vector3 offset = new Vector3(0, 1.0f, 0);
        Vector2 uiSize = taskPanelSize;
        Vector3 scale = new Vector3(0.01f, 0.01f, 0.01f);

        uiController.InitializeTargetUI(TargetObj, offset, uiSize, scale);
    }

    /// <summary>
    /// 업무 UI 생성 (알바생용)
    /// </summary>
    /// <param name="arbeitObj">알바생 게임오브젝트</param>
    /// <param name="task">할당된 업무</param>
    /// <param name="yOffset">Y축 오프셋</param>
    /// <param name="index">업무 인덱스</param>
    /// <returns>생성된 UI 오브젝트</returns>
    public GameObject TaskUIInstantiate(GameObject arbeitObj, TaskInfo task, float yOffset, int index)
    {
        GameObject TaskUIObj = Instantiate(TaskUIPrefab);

        // TaskUIController로 UI 초기화 위임
        TaskUIController uiController = TaskUIObj.GetComponent<TaskUIController>();
        if (uiController == null)
        {
            uiController = TaskUIObj.AddComponent<TaskUIController>();
        }

        uiController.InitializeArbeitUI(arbeitObj, task, taskPanelSize, yOffset, index);

        return TaskUIObj;
    }

    /// <summary>
    /// 알바생 업무 UI 클릭 시 처리 (업무 취소)
    /// </summary>
    public void OnArbeitTaskUIClicked(GameObject arbeitObj, TaskInfo task)
    {
        var arbeitController = arbeitObj.GetComponent<ArbeitController>();
        arbeitController.RemoveTaskFromQueue(task);
    }

    /// <summary>
    /// 업무 UI 클릭 시 호출되는 메서드
    /// </summary>
    public void OnTargetTaskUIClicked(GameObject taskUI, GameObject TargetObj)
    {
        var selectedArbeit = CurrentSelected.GetComponent<ArbeitController>();

        // 알바생이 업무를 추가할 수 있는지 확인
        if (!selectedArbeit.CanAddTask())
        {
            return;
        }

        // 해당 타겟에 대한 업무 찾기
        TaskInfo task = GetTaskByTarget(TargetObj);

        if (task != null)
        {
            // 알바생에게 업무 할당
            selectedArbeit.AddTask(task);
        }
        else
        {
            Debug.LogWarning("해당 타겟에 대한 업무를 찾을 수 없습니다."); // 없으면 곤란함
        }
    }
    #endregion

    #region 업무 제거 메소드
    /// <summary>
    /// 업무 제거 메소드
    /// </summary>
    public void RemoveTask(TaskInfo task)
    {
        if (task != null && allTasks.Contains(task))
        {
            task.CompleteTask();
            allTasks.Remove(task);

            // 동일한 타겟에 할당된 다른 알바생의 업무도 제거
            NotifyTaskCompleted(task);
        }
    }
    #endregion

    #region 업무 완료 시
    /// <summary>
    /// 업무 완료 시 => 동일한 업무를 가진 다른 알바생에게 전파
    /// </summary>
    private void NotifyTaskCompleted(TaskInfo completedTask)
    {
        // 모든 알바생 찾기
        ArbeitController[] allArbeits = FindObjectsOfType<ArbeitController>();

        foreach (var arbeit in allArbeits)
        {
            arbeit.RemoveTaskIfMatch(completedTask);
        }
    }
    #endregion

    #region 조회용 메소드
    /// <summary>
    /// 특정 타겟 오브젝트에 대한 업무 가져오기
    /// </summary>
    public TaskInfo GetTaskByTarget(GameObject target)
    {
        return allTasks.Find(task => task.targetObject == target && !task.isCompleted);
    }

    /// <summary>
    /// 모든 업무 리스트 가져오기
    /// </summary>
    public List<TaskInfo> GetAllTasks()
    {
        return allTasks;
    }
    #endregion

    #region 칵테일 주문 처리
    /// <summary>
    /// 주문 완료 처리 (대화창에서 주문 수락 시 호출)
    /// 추후 구현 예정
    /// </summary>
    public void AcceptOrder(GameObject arbeit, TaskInfo task)
    {
        if (task == null || task.orderedCocktail == null)
        {
            Debug.LogError("유효하지 않은 주문입니다.");
            return;
        }

        Debug.Log($"주문 수락: {task.orderedCocktail.CocktailName}");

        // 후에 해야할 일 : 주문 데이터를 저장하는 로직 추가
        // 예: 주문 테이블, 칵테일 정보, 개수 등을 별도 리스트나 딕셔너리에 저장

        // 대화창 닫기
        CloseDialog();

        // 알바생의 현재 업무 완료 처리
        var arbeitController = arbeit.GetComponent<ArbeitController>();
        if (arbeitController != null)
        {
            arbeitController.CompleteCurrentTask();
        }
    }

    /// <summary>
    /// 대화창 열기 (대화창 구현 시 호출)
    /// 추후 구현 예정
    /// </summary>
    public void OpenDialog(GameObject TargetObj)
    {
        isDialogOpen = true;
        //dialogOwner = ???;
    }

    /// <summary>
    /// 대화창 닫기 (ESC 또는 주문 완료 시 호출)
    /// 추후 구현 예정
    /// </summary>
    public void CloseDialog()
    {
        if (isDialogOpen)
        {
            Debug.Log($"{dialogOwner?.name}의 대화창이 닫혔습니다.");
            isDialogOpen = false;
            dialogOwner = null;
        }
    }
    #endregion
}
