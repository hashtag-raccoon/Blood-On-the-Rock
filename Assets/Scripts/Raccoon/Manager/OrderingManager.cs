using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Tilemaps;

public enum TaskType
{
    None,
    TakeOrder,
    ServeOrder,
    CleanTable
}

public class OrderingManager : MonoBehaviour
{
    private static OrderingManager _instance;
    [Header("선택 시 외곽선/기본 마테리얼 할당")]
    [SerializeField] private Material selectedOutlineMaterial;
    [SerializeField] private Material DefaultMaterial;
    [Header("업무 UI 프리팹")]
    [SerializeField] private GameObject TaskUIPrefab;

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
    public GameObject CurrentSelected = null;
    
    public class TaskInfo
    {
        public TaskType taskType;
    }

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
    }

    public void Update()
    {
        // 우클릭 시 알바생 선택 취소
        if (Input.GetMouseButtonDown(1))
        {
            SelectCancel();
        }
    }

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

    /// <summary>
    /// 업무 UI 생성
    /// </summary>
    public void TaskUIInstantiate(GameObject GuestObj)
    {
        if (TaskUIPrefab == null)
        {
            Debug.LogError("OrderingManager: TaskUIPrefab가 할당되지 않았습니다.");
            return;
        }

        GameObject TaskUIObj = Instantiate(TaskUIPrefab);

        Canvas canvas = TaskUIObj.GetComponent<Canvas>();
        if (canvas != null)
        {
            canvas.renderMode = RenderMode.WorldSpace;

            // 테이블 위치 기준으로 UI 위치 설정
            Vector3 guestPos = GuestObj.transform.position;
            TaskUIObj.transform.position = guestPos + new Vector3(0, 1.0f, 0); // 테이블 위 1유닛
            TaskUIObj.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f); // UI 크기 조정
            RectTransform canvasRect = TaskUIObj.GetComponent<RectTransform>();
            if (canvasRect != null)
            {
                canvasRect.sizeDelta = new Vector2(50, 50); // UI 크기 설정
            }
        }

        // TaskUIController가 있다면 타겟 설정 (있을 경우에만)
        var uiController = TaskUIObj.GetComponent(typeof(MonoBehaviour));
        if (uiController != null)
        {
            var setMethod = uiController.GetType().GetMethod("SetTargetGuest");
            if (setMethod != null)
            {
                setMethod.Invoke(uiController, new object[] { GuestObj });
            }
        }

        // 클릭 감지
        UnityEngine.EventSystems.EventTrigger eventTrigger = TaskUIObj.GetComponent<UnityEngine.EventSystems.EventTrigger>();
        if (eventTrigger == null)
        {
            eventTrigger = TaskUIObj.AddComponent<UnityEngine.EventSystems.EventTrigger>();
        }

        UnityEngine.EventSystems.EventTrigger.Entry entry = new UnityEngine.EventSystems.EventTrigger.Entry();
        entry.eventID = UnityEngine.EventSystems.EventTriggerType.PointerClick;
        entry.callback.AddListener((data) => { OnTaskUIClicked(TaskUIObj, GuestObj); }); // TaskUI에 클릭 시 호출될 메서드 연결
        eventTrigger.triggers.Add(entry); // 이벤트 트리거에 추가
    }

    /// <summary>
    /// 업무 UI 클릭 시 처리
    /// </summary>
    private void OnTaskUIClicked(GameObject taskUI, GameObject GuestObj)
    {
        // 여기에 클릭 시 처리할 로직 추가
        // 예: 알바를 테이블로 이동시키기, 주문 UI 표시 등
        var selectedArbeit = CurrentSelected.GetComponent<ArbeitController>();
        if (selectedArbeit != null)
        {
            selectedArbeit.SetTarget(GuestObj.transform);
        }
    }

    public void CreateTask(GameObject TargetObj, TaskType taskType)
    {
        switch(taskType)
        {
            case TaskType.TakeOrder:
                // 칵테일 주문 받기 업무 생성 로직
                Random.Range(1, CocktailRepository.Instance.GetTotalCocktailCount());
                break;
            case TaskType.ServeOrder:
                // 서빙 업무 생성 로직
                break;
            case TaskType.CleanTable:
                // 테이블 청소 업무 생성 로직
                break;
            default:
                Debug.LogWarning("알 수 없는 업무 유형입니다.");
                break;
        }
    }
}
