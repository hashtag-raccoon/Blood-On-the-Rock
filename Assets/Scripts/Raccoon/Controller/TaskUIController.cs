using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 업무 UI 관리 컨트롤러
/// 손님/테이블 위 업무 UI, 알바생 머리 위 업무 UI 모두 관리
/// </summary>
public class TaskUIController : MonoBehaviour
{
    [Header("UI 설정")]
    [SerializeField] private Canvas canvas;
    [SerializeField] private RectTransform rectTransform;
    [SerializeField] private UnityEngine.UI.Image taskIcon; // 업무 아이콘 Image

    [SerializeField] private float customXOffset = 0.9f; // 커스텀 X 오프셋 (필요시 사용)
    private GameObject targetObject; // 타겟 오브젝트 (손님, 테이블, 알바생 등)
    private TaskInfo assignedTask; // 할당된 업무
    private bool isArbeitUI = false; // 알바생 UI인지 여부

    private void Awake()
    {
        if (canvas == null)
            canvas = GetComponent<Canvas>();
        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();
        if (taskIcon == null)
            taskIcon = GetComponentInChildren<UnityEngine.UI.Image>().transform.Find("TaskIcon")
            .GetComponent<UnityEngine.UI.Image>();
    }

    /// <summary>
    /// 손님/테이블용 업무 UI 초기화
    /// </summary>
    public void InitializeTargetUI(GameObject target, Vector3 offset, Vector2 uiSize, Vector3 scale)
    {
        targetObject = target;
        isArbeitUI = false;

        if (canvas != null)
        {
            canvas.renderMode = RenderMode.WorldSpace;
        }

        // 타겟 위치 기준으로 UI 위치 설정
        transform.position = target.transform.position + offset;
        transform.localScale = scale;

        if (rectTransform != null)
        {
            rectTransform.sizeDelta = uiSize;
        }

        // 클릭 이벤트 설정 (알바생에게 업무 할당)
        SetupClickEvent(() => OrderingManager.Instance.OnTargetTaskUIClicked(this.gameObject, targetObject));

        // 타겟의 TaskInfo에서 TaskType을 가져와서 아이콘 업데이트
        TaskInfo targetTask = OrderingManager.Instance.GetTaskByTarget(target);
        if (targetTask != null)
        {
            UpdateTaskIcon(targetTask.taskType);
        }
    }

    /// <summary>
    /// 알바생용 업무 UI 초기화
    /// </summary>
    public void InitializeArbeitUI(GameObject arbeit, TaskInfo task, Vector2 uiSize, float yOffset, int index)
    {
        targetObject = arbeit;
        assignedTask = task;
        isArbeitUI = true;

        // 알바생의 자식으로 설정 (이동 시 따라다니게)
        transform.SetParent(arbeit.transform);

        if (canvas != null)
        {
            canvas.renderMode = RenderMode.WorldSpace;
        }

        // 로컬 좌표로 UI 위치 설정
        float xOffset = (index - 1) * customXOffset; // 중앙 정렬
        transform.localPosition = new Vector3(xOffset, yOffset, 0);
        transform.localScale = new Vector3(0.008f, 0.008f, 0.008f); // 크기 조정

        if (rectTransform != null)
        {
            rectTransform.sizeDelta = uiSize;
        }

        // 클릭 이벤트 설정 (업무 취소)
        SetupClickEvent(() => OrderingManager.Instance.OnArbeitTaskUIClicked(arbeit, task));

        // TaskType에 따라 아이콘 업데이트
        UpdateTaskIcon(task.taskType);
    }

    /// <summary>
    /// 클릭 이벤트 설정
    /// </summary>
    private void SetupClickEvent(System.Action onClick)
    {
        EventTrigger eventTrigger = GetComponent<EventTrigger>();
        if (eventTrigger == null)
        {
            eventTrigger = gameObject.AddComponent<EventTrigger>();
        }

        // 기존 이벤트 제거
        eventTrigger.triggers.Clear();

        EventTrigger.Entry entry = new EventTrigger.Entry();
        entry.eventID = EventTriggerType.PointerClick;
        entry.callback.AddListener((data) => onClick?.Invoke());
        eventTrigger.triggers.Add(entry);
    }

    /// <summary>
    /// 타겟 오브젝트 설정 (기존 코드와 호환성 유지)
    /// </summary>
    public void SetTargetGuest(GameObject target)
    {
        targetObject = target;
    }

    /// <summary>
    /// 할당된 업무 반환
    /// </summary>
    public TaskInfo GetAssignedTask()
    {
        return assignedTask;
    }

    /// <summary>
    /// 타겟 오브젝트 반환
    /// </summary>
    public GameObject GetTargetObject()
    {
        return targetObject;
    }

    /// <summary>
    /// 알바생 UI인지 확인
    /// </summary>
    public bool IsArbeitUI()
    {
        return isArbeitUI;
    }

    /// <summary>
    /// TaskType에 따라 아이콘 업데이트
    /// </summary>
    private void UpdateTaskIcon(TaskType taskType)
    {
        if (taskIcon != null)
        {
            Sprite iconSprite = OrderingManager.Instance.GetTaskIconSprite(taskType);
            if (iconSprite != null)
            {
                taskIcon.sprite = iconSprite;
            }
        }
    }
}
