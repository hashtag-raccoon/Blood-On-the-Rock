using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class OrderingManager : MonoBehaviour
{
    private static OrderingManager _instance;

    [Header("외곽선 시스템 설정")]
    [Tooltip("외곽선 전용 카메라")]
    [SerializeField] private Camera outlineCamera;

    [Tooltip("외곽선 렌더텍스쳐")]
    [SerializeField] private RenderTexture outlineRenderTexture;

    [Tooltip("외곽선 마테리얼")]
    [SerializeField] private Material outlineMaterial;

    [Tooltip("렌더텍스쳐 해상도")]
    [SerializeField] private int renderTextureSize = 512;

    [Tooltip("외곽선 여유 공간")]
    [Range(0f, 1f)]
    [SerializeField] private float outlinePadding = 0.2f;

    [Tooltip("외곽선 색상")]
    [SerializeField] private Color outlineColor = Color.white;

    [Tooltip("외곽선 두께")]
    [Range(1f, 100f)]
    [SerializeField] private float outlineWidth = 10f;

    [Header("선택 시 외곽선/기본 마테리얼 할당")]
    [SerializeField] private Material selectedOutlineMaterial;
    [SerializeField] private Material DefaultMaterial;

    [Header("업무 UI 관리")]
    [Tooltip("업무 UI 프리팹")]
    [SerializeField] private GameObject TaskUIPrefab;

    [Tooltip("업무 타입별 아이콘")]
    [SerializeField] private Sprite takeOrderIcon;
    [SerializeField] private Sprite serveOrderIcon;
    [SerializeField] private Sprite cleanTableIcon;

    [Tooltip("업무 UI 패널 크기")]
    [SerializeField] private Vector2 taskPanelSize;

    [Tooltip("알바 리스트")]
    public ArbeitController[] Arbiets;

    [Header("칵테일 주문 리스트")]
    public List<CocktailRecipeScript> CocktailOrders = new List<CocktailRecipeScript>();

    [Header("칵테일 주문창 UI 크기")]
    public Vector2 orderDialogPanelSize = new Vector2(1200, 800);

    public List<CocktailRecipeScript> CompletedCocktails = new List<CocktailRecipeScript>();

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

    [Header("업무 관리")]
    [SerializeField] private List<TaskInfo> allTasks = new List<TaskInfo>();

    private GameObject currentOutlineDisplay = null;
    private OutlineCameraFollower cameraFollower;

    //[Header("대화창 상태")]
    [HideInInspector]
    public bool isDialogOpen = false;
    [HideInInspector]
    public GameObject dialogOwner = null;

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
            return;
        }

        for (int i = 0; i < Arbiets.Length; i++)
        {
            Arbiets[i].myNpcData = DataManager.Instance.npcs[i];
        }

        InitializeOutlineSystem();
    }

    public void Update()
    {
        if (Input.GetMouseButtonDown(1))
        {
            SelectCancel();
        }

        if (Input.GetKeyDown(KeyCode.Escape) && isDialogOpen)
        {
            ForceCloseDialog();
        }
    }

    #region 알바생 선택 관리
    /// <summary>
    /// 알바생 선택 토글, 다른 알바생 선택 시 이전 선택 해제
    /// </summary>
    public void ToggleSelected(GameObject SelectedArbeit)
    {
        if (SelectedArbeit == null)
        {
            Debug.Log("선택된 알바가 없습니다.");
            return;
        }

        if (isDialogOpen && dialogOwner != SelectedArbeit)
        {
            Debug.LogWarning("대화창 열린 중에는 알바생 선택 불가");
            return;
        }

        if (CurrentSelected != null)
        {
            CurrentSelected.GetComponent<ArbeitController>().isSelected = false;
            SpriteRenderer currentRenderer = CurrentSelected.GetComponent<SpriteRenderer>();
            if (currentRenderer != null)
            {
                currentRenderer.material = DefaultMaterial;
            }
        }

        CurrentSelected = SelectedArbeit;
        CurrentSelected.GetComponent<ArbeitController>().isSelected = true;
        SpriteRenderer selectedRenderer = CurrentSelected.GetComponent<SpriteRenderer>();
        if (selectedRenderer != null)
        {
            selectedRenderer.material = selectedOutlineMaterial;
        }
        // 선택한 알바생에 외곽선 디스플레이 생성
        CreateOutlineDisplayForArbeit(SelectedArbeit);
    }
    /// <summary>
    /// 알바생 선택 해제하는 메소드
    /// 알바생 선택이 되있지 않을때는 작동 X
    /// 되어있을때는 전에 선택한 알바생의 isSelected를 false로 바꾸고 외곽선 제거
    /// </summary>
    public void SelectCancel()
    {
        // 대화창이 열려있다면 함께 닫아 선택 제한이 남지 않도록 처리
        if (isDialogOpen)
        {
            ForceCloseDialog();
        }

        if (CurrentSelected != null)
        {
            CurrentSelected.GetComponent<ArbeitController>().isSelected = false;
            SpriteRenderer renderer = CurrentSelected.GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                renderer.material = DefaultMaterial;
            }
            CurrentSelected = null;
        }
        // 외곽선 제거
        DestroyOutlineDisplay();
    }
    #endregion

    #region 업무 아이콘 관리
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
    /// 업무 생성 메소드, 타겟에 업무 할당
    /// </summary>
    public TaskInfo CreateTask(GameObject TargetObj, TaskType taskType)
    {
        TaskInfo newTask = null;

        switch (taskType)
        {
            case TaskType.TakeOrder:
                if (CocktailRepository.Instance._cocktailRecipeDict.Count > 0)
                {
                    int randomIndex = UnityEngine.Random.Range(0, CocktailRepository.Instance._cocktailRecipeDict.Count);
                    CocktailRecipeScript randomCocktail = CocktailRepository.Instance._cocktailRecipeDict[randomIndex];

                    newTask = new TaskInfo(TaskType.TakeOrder, TargetObj, randomCocktail);
                    allTasks.Add(newTask);
                    TaskUIInstantiate(TargetObj, newTask);
                }
                else
                {
                    Debug.LogError("칵테일 데이터가 없습니다!");
                }
                break;

            case TaskType.ServeOrder:
                newTask = new TaskInfo(TaskType.ServeOrder, TargetObj);
                allTasks.Add(newTask);
                break;

            case TaskType.CleanTable:
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
    /// 업무 UI 생성 메소드
    /// </summary>
    /// <param name="TargetObj"></param>
    /// <param name="taskInfo"></param>
    public void TaskUIInstantiate(GameObject TargetObj, TaskInfo taskInfo)
    {
        GameObject TaskUIObj = Instantiate(TaskUIPrefab);

        TaskUIController uiController = TaskUIObj.GetComponent<TaskUIController>();
        if (uiController == null)
        {
            uiController = TaskUIObj.AddComponent<TaskUIController>();
        }

        Vector3 offset = new Vector3(0, 1.0f, 0);
        Vector2 uiSize = taskPanelSize;
        Vector3 scale = new Vector3(0.01f, 0.01f, 0.01f);

        uiController.InitializeTargetUI(TargetObj, taskInfo, offset, uiSize, scale);
    }
    /// <summary>
    /// 업무 UI 생성 메소드 (알바생용), 알바생이 담당한 업무를 알바생 위에 생성함
    /// </summary>
    public GameObject TaskUIInstantiate(GameObject arbeitObj, TaskInfo task, float yOffset, int index)
    {
        GameObject TaskUIObj = Instantiate(TaskUIPrefab);

        TaskUIController uiController = TaskUIObj.GetComponent<TaskUIController>();
        if (uiController == null)
        {
            uiController = TaskUIObj.AddComponent<TaskUIController>();
        }

        uiController.InitializeArbeitUI(arbeitObj, task, taskPanelSize, yOffset, index);

        return TaskUIObj;
    }
    // 알바생 업무 UI 클릭 시 업무 제거
    public void OnArbeitTaskUIClicked(GameObject arbeitObj, TaskInfo task)
    {
        var arbeitController = arbeitObj.GetComponent<ArbeitController>();
        arbeitController.RemoveTaskFromQueue(task);
    }
    // 업무 UI 클릭 시 알바생에게 업무 추가
    public void OnTargetTaskUIClicked(GameObject taskUI, GameObject TargetObj)
    {
        if (isDialogOpen)
        {
            Debug.LogWarning("[업무 UI] 대화창이 열려있을 때는 업무를 삭제할 수 없습니다.");
            return;
        }

        if (CurrentSelected == null)
        {
            Debug.LogWarning("[업무 UI] 알바생이 선택되지 않았습니다.");
            return;
        }

        var selectedArbeit = CurrentSelected.GetComponent<ArbeitController>();

        // 업무를 추가하지 못하는 상태면 리턴
        if (!selectedArbeit.CanAddTask(taskUI))
        {
            return;
        }

        TaskInfo task = GetTaskByTarget(TargetObj);

        if (task != null)
        {
            selectedArbeit.AddTask(task);
        }
        else
        {
            Debug.LogWarning("해당 타겟에 대한 업무를 찾을 수 없습니다.");
        }
    }
    #endregion

    #region 업무 제거 메소드
    public void RemoveTask(TaskInfo task)
    {
        if (task != null && allTasks.Contains(task))
        {
            task.CompleteTask();
            allTasks.Remove(task);
            NotifyTaskCompleted(task);
        }
    }
    #endregion

    #region 업무 완료 시
    private void NotifyTaskCompleted(TaskInfo completedTask)
    {
        ArbeitController[] allArbeits = FindObjectsOfType<ArbeitController>();

        foreach (var arbeit in allArbeits)
        {
            arbeit.RemoveTaskIfMatch(completedTask);
        }
    }
    #endregion

    #region 조회용 메소드
    public TaskInfo GetTaskByTarget(GameObject target)
    {
        TaskInfo result = allTasks.Find(task => task.targetObject == target && !task.isCompleted);
        return result;
    }

    public GameObject GetTartgetByTask(TaskInfo task)
    {
        return task.targetObject;
    }

    public GameObject GetUIByTask(TaskInfo task)
    {
        return task.targetUI;
    }

    public List<TaskInfo> GetAllTasks()
    {
        return allTasks;
    }

    public bool HasTask(TaskInfo task)
    {
        return GetAllTasks().Contains(task);
    }
    #endregion

    #region 칵테일 주문 처리
    public void AcceptOrder(GameObject arbeit, TaskInfo task)
    {
        if (task == null || task.orderedCocktail == null)
        {
            Debug.LogError("task 또는 orderedCocktail이 null");
            return;
        }

        CocktailOrders.Add(task.orderedCocktail);
        CloseDialog();

        var arbeitController = arbeit.GetComponent<ArbeitController>();
        if (arbeitController != null)
        {
            arbeitController.EndTakeOrder();
        }
    }
    #endregion

    #region 대화창 열기
    /// <summary>
    /// 대화창을 열기 (초상화 이름 버전)
    /// </summary>
    public void OpenDialog(GameObject arbeitObj, TaskInfo task, Vector2? panelSize = null, int startIndex = 0,
    string replacementName = null, string portraitName = null)
    {
        if (arbeitObj == null || task == null)
        {
            Debug.LogError("arbeitObj 또는 task가 null");
            return;
        }

        if (isDialogOpen)
        {
            var arbeitController = arbeitObj.GetComponent<ArbeitController>();
            if (arbeitController != null)
            {
                arbeitController.AddToDialogQueue(task, panelSize, startIndex, replacementName, portraitName);
            }
            return;
        }

        isDialogOpen = true;
        dialogOwner = arbeitObj;

        if (DialogueManager.Instance != null && DialogueManager.Instance.dialogueUI != null)
        {
            System.Action onDialogueEnd = () => AcceptOrder(arbeitObj, task);

            string cocktailName = null;
            if (task.orderedCocktail != null)
            {
                cocktailName = task.orderedCocktail.CocktailName;
            }

            DialogueManager.Instance.dialogueUI.StartOrderDialogue(startIndex, panelSize, onDialogueEnd, replacementName, cocktailName, portraitName);
        }
    }
    /// <summary>
    /// 대화창을 열기 (초상화 스프라이트 버전)
    /// </summary>
    public void OpenDialog(GameObject arbeitObj, TaskInfo task, Vector2? panelSize, int startIndex,
    string replacementName, Sprite portraitSprite)
    {
        if (arbeitObj == null || task == null)
        {
            Debug.LogError("arbeitObj 또는 task가 null");
            return;
        }

        if (isDialogOpen)
        {
            var arbeitController = arbeitObj.GetComponent<ArbeitController>();
            if (arbeitController != null)
            {
                arbeitController.AddToDialogQueue(task, panelSize, startIndex, replacementName, portraitSprite);
            }
            return;
        }

        isDialogOpen = true;
        dialogOwner = arbeitObj;

        if (DialogueManager.Instance != null && DialogueManager.Instance.dialogueUI != null)
        {
            System.Action onDialogueEnd = () => AcceptOrder(arbeitObj, task);

            string cocktailName = null;
            if (task.orderedCocktail != null)
            {
                cocktailName = task.orderedCocktail.CocktailName;
            }

            DialogueManager.Instance.dialogueUI.StartOrderDialogue(startIndex, panelSize, onDialogueEnd, replacementName, cocktailName, portraitSprite);
        }
    }
    /// <summary>
    /// 대화창을 닫음
    /// </summary>
    public void CloseDialog()
    {
        if (isDialogOpen)
        {
            isDialogOpen = false;
            dialogOwner = null;

            if (DialogueManager.Instance != null && DialogueManager.Instance.dialogueUI != null)
            {
                DialogueManager.Instance.dialogueUI.gameObject.SetActive(false);
            }

            ProcessNextDialogInQueue();
        }
    }
    /// <summary>
    /// 대화창을 강제로 닫음
    /// </summary>
    public void ForceCloseDialog()
    {
        if (isDialogOpen)
        {
            isDialogOpen = false;
            GameObject currentDialogOwner = dialogOwner;
            dialogOwner = null;

            if (DialogueManager.Instance != null && DialogueManager.Instance.dialogueUI != null)
            {
                DialogueManager.Instance.dialogueUI.ForceEndDialogue();
            }

            if (currentDialogOwner != null)
            {
                var arbeitController = currentDialogOwner.GetComponent<ArbeitController>();
                if (arbeitController != null)
                {
                    arbeitController.CancelCurrentTask();
                }
            }

            ProcessNextDialogInQueue();
        }
    }
    /// <summary>
    /// 대화창 큐에 있는 다음 대화창을 처리
    /// </summary>
    private void ProcessNextDialogInQueue()
    {
        ArbeitController[] allArbeits = FindObjectsOfType<ArbeitController>();
        foreach (var arbeit in allArbeits)
        {
            if (arbeit.HasDialogInQueue())
            {
                arbeit.ProcessNextDialog();
                return;
            }
        }
    }
    #endregion

    #region 칵테일 제작 완료 및 제거
    public void MarkCocktailAsCompleted(CocktailRecipeScript recipe)
    {
        if (CocktailOrders.Contains(recipe) && !CompletedCocktails.Contains(recipe))
        {
            CompletedCocktails.Add(recipe);
            Debug.Log($"칵테일 제작 완료: {recipe.CocktailName}");
        }
    }

    public void RemoveCocktailOrder(CocktailRecipeScript recipe)
    {
        CocktailOrders.Remove(recipe);
        CompletedCocktails.Remove(recipe);
        Debug.Log($"칵테일 주문 제거: {recipe.CocktailName}");
    }
    #endregion

    #region 외곽선 시스템
    /// <summary>
    /// 외곽선 시스템 초기화
    /// </summary>
    private void InitializeOutlineSystem()
    {
        // RenderTexture 생성 또는 설정
        if (outlineRenderTexture == null)
        {
            outlineRenderTexture = new RenderTexture(renderTextureSize, renderTextureSize, 16, RenderTextureFormat.ARGB32);
            outlineRenderTexture.name = "OutlineRenderTexture";
            outlineRenderTexture.antiAliasing = 1;
            outlineRenderTexture.filterMode = FilterMode.Bilinear;
        }

        if (outlineCamera != null)
        {
            // OutlineCamera에 RenderTexture 설정
            outlineCamera.targetTexture = outlineRenderTexture;
            outlineCamera.clearFlags = CameraClearFlags.SolidColor;
            outlineCamera.backgroundColor = new Color(0, 0, 0, 0); // 완전 투명 배경

            // 투명도 렌더링 설정
            outlineCamera.depthTextureMode = DepthTextureMode.None;
            outlineCamera.allowHDR = false;
            outlineCamera.allowMSAA = false;

            // OutlineCameraFollower 컴포넌트 설정
            cameraFollower = outlineCamera.GetComponent<OutlineCameraFollower>();
            if (cameraFollower == null)
            {
                cameraFollower = outlineCamera.gameObject.AddComponent<OutlineCameraFollower>();
            }

            cameraFollower.outlineCam = outlineCamera;
            cameraFollower.padding = outlinePadding;

            // 카메라 초기 비활성화
            outlineCamera.gameObject.SetActive(false);
        }
        else
        {
            Debug.LogError("[Outline] OutlineCamera가 할당되지 않았습니다!");
        }

        // Outline Material 설정
        if (outlineMaterial != null)
        {
            outlineMaterial.SetFloat("_OutlineEnabled", 1f);
            outlineMaterial.SetFloat("_Thickness", outlineWidth);
            outlineMaterial.SetColor("_SolidOutline", outlineColor);
            outlineMaterial.SetFloat("_OutlineMode", 0f); // Solid
            outlineMaterial.SetFloat("_OutlineShape", 0f); // Contour
            outlineMaterial.SetFloat("_OutlinePosition", 2f); // Outside
        }
        else
        {
            Debug.LogError("[Outline] OutlineMaterial이 할당되지 않았습니다!");
        }
    }
    /// <summary>
    /// 외곽선 디스플레이 생성
    /// - 알바생 오브젝트를 받아 해당 오브젝트에 맞는 외곽선 디스플레이를 생성
    /// - 기존에 생성된 외곽선 디스플레이가 있으면 제거 후 새로 생성
    /// - 외곽선 카메라를 활성화하고 타겟 설정
    /// - 외곽선 디스플레이는 알바생의 Bounds 중심에 위치
    /// - 외곽선 디스플레이는 알바생을 따라다니도록 설정
    /// - 외곽선 디스플레이는 Canvas + RawImage로 구성되며, 외곽선 렌더텍스쳐를 사용
    /// - 외곽선 디스플레이의 크기는 외곽선 카메라의 Orthographic Size에 맞춤
    /// - 외곽선 디스플레이의 Sorting Order는 100으로 설정하여 캐릭터보다 앞에 렌더링
    /// - 외곽선 마테리얼의 색상과 두께는 현재 설정값을 사용
    /// - 외곽선 카메라의 패딩 설정값을 사용
    /// - 외곽선 카메라는 알바생과 모든 자식을 외곽선 레이어로 임시 변경하여 렌더링
    /// - 외곽선 디스플레이 생성 시점에 외곽선 카메라의 위치와 크기를 즉시 업데이트
    /// 등등의 기능을 제공함..
    /// 1줄 요약하면 "알바생 오브젝트 받아서 외곽선 디스플레이 생성하고 카메라 설정"
    /// </summary>
    private void CreateOutlineDisplayForArbeit(GameObject arbeit)
    {
        if (arbeit == null)
        {
            Debug.LogWarning("[Outline] Arbeit가 null입니다.");
            return;
        }

        if (outlineRenderTexture == null || outlineMaterial == null)
        {
            Debug.LogError("[Outline] RenderTexture 또는 Material이 없습니다!");
            return;
        }

        // 기존 OutlineDisplay 제거
        DestroyOutlineDisplay();

        // 알바생 레이어 확인
        int arbeitLayer = arbeit.layer;
        string layerName = LayerMask.LayerToName(arbeitLayer);

        // 알바생의 원래 레이어 저장 (자식 포함)
        var layerRestoreList = new List<LayerRestoreData>();
        int originalLayer = arbeitLayer;

        // OutlineCamera가 렌더링하는 레이어 확인 (cullingMask의 첫 번째 활성 레이어)
        int outlineLayer = -1;
        if (outlineCamera != null)
        {
            for (int i = 0; i < 32; i++)
            {
                if ((outlineCamera.cullingMask & (1 << i)) != 0)
                {
                    outlineLayer = i;
                    break;
                }
            }

            if (outlineLayer == -1)
            {
                Debug.LogError($"[Outline] OutlineCamera의 Culling Mask가 비어있습니다!");
                return;
            }

            // 알바생과 모든 자식을 Outline 레이어로 임시 변경 (원본 레이어는 리스트에 보관)
            SetLayerRecursively(arbeit, outlineLayer, layerRestoreList);
        }

        // OutlineCamera 활성화 및 타겟 설정
        if (outlineCamera != null && cameraFollower != null)
        {
            // 타겟 설정
            cameraFollower.target = arbeit.transform;

            // 카메라 활성화
            outlineCamera.gameObject.SetActive(true);

            // 즉시 위치와 크기 업데이트
            cameraFollower.UpdateCameraPosition();
        }

        // 카메라가 렌더링하는 중심점 계산 (알바생의 Bounds 중심)
        Bounds arbeitBounds = CalculateBounds(arbeit.transform);
        Vector3 displayPosition = arbeitBounds.center;

        // Canvas 오브젝트 생성 (알바생 Bounds 중심에 위치)
        currentOutlineDisplay = new GameObject($"{arbeit.name}_OutlineDisplay");
        currentOutlineDisplay.transform.position = displayPosition;
        currentOutlineDisplay.transform.rotation = Quaternion.identity;

        // Canvas 설정 (World Space)
        Canvas canvas = currentOutlineDisplay.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        // CanvasScaler 추가
        CanvasScaler scaler = currentOutlineDisplay.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 100;

        // RectTransform 설정 (OutlineCamera의 orthographicSize에 맞춤)
        RectTransform canvasRect = currentOutlineDisplay.GetComponent<RectTransform>();
        float cameraSize = outlineCamera.orthographicSize * 2f; // orthographicSize는 높이의 절반
        canvasRect.sizeDelta = new Vector2(cameraSize, cameraSize);

        // 피벗을 중앙으로 설정 (0.5, 0.5)
        canvasRect.pivot = new Vector2(0.5f, 0.5f);
        canvasRect.anchorMin = new Vector2(0.5f, 0.5f);
        canvasRect.anchorMax = new Vector2(0.5f, 0.5f);

        // RawImage 오브젝트 생성
        GameObject rawImageObject = new GameObject("RawImage");
        rawImageObject.transform.SetParent(currentOutlineDisplay.transform, false);

        // RawImage 설정
        RawImage rawImage = rawImageObject.AddComponent<RawImage>();
        rawImage.texture = outlineRenderTexture;
        rawImage.color = Color.white;

        // Material 인스턴스 생성 및 할당
        Material instanceMaterial = new Material(outlineMaterial);
        instanceMaterial.SetFloat("_OutlineEnabled", 1f);
        instanceMaterial.SetFloat("_Thickness", outlineWidth);
        instanceMaterial.SetColor("_SolidOutline", outlineColor);
        rawImage.material = instanceMaterial;

        // RectTransform 설정
        RectTransform imageRect = rawImageObject.GetComponent<RectTransform>();
        imageRect.anchorMin = Vector2.zero;
        imageRect.anchorMax = Vector2.one;
        imageRect.sizeDelta = Vector2.zero;
        imageRect.anchoredPosition = Vector2.zero;

        // Sorting Order 설정 (캐릭터보다 앞에 배치하여 외곽선 보이게)
        canvas.sortingLayerName = "Default";
        canvas.sortingOrder = 100;

        // OutlineDisplay가 알바생을 따라다니도록 LateUpdate에서 위치 동기화
        var follower = currentOutlineDisplay.AddComponent<OutlineDisplayFollower>();
        follower.target = arbeit.transform;
        follower.arbeitObject = arbeit;
        follower.originalLayer = originalLayer;
        follower.originalLayers = layerRestoreList;
    }

    /// <summary>
    /// 대상 오브젝트와 모든 자식의 레이어를 재귀적으로 변경
    /// - 변경 전 레이어는 restoreList에 저장
    /// 재귀함수로 구현함, 깊이 우선 탐색을 위해 Stack(나중에 들어간 데이터가 가장 나중에 빠지는 거)을 사용하지 않
    /// </summary>
    private void SetLayerRecursively(GameObject obj, int layer, List<LayerRestoreData> restoreList)
    {
        if (restoreList != null)
        {
            restoreList.Add(new LayerRestoreData
            {
                gameObject = obj,
                layer = obj.layer
            });
        }
        obj.layer = layer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, layer, restoreList);
        }
    }

    /// <summary>
    /// Transform의 모든 Renderer를 고려한 Bounds 계산
    /// </summary>
    private Bounds CalculateBounds(Transform target)
    {
        Renderer[] renderers = target.GetComponentsInChildren<Renderer>();

        if (renderers.Length == 0)
        {
            // Renderer가 없으면 기본 크기 반환
            return new Bounds(target.position, Vector3.one);
        }

        Bounds bounds = renderers[0].bounds;

        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        return bounds;
    }

    /// <summary>
    /// 외곽선 디스플레이 제거
    /// </summary>
    private void DestroyOutlineDisplay()
    {
        if (currentOutlineDisplay != null)
        {
            Destroy(currentOutlineDisplay);
            currentOutlineDisplay = null;
        }

        // OutlineCamera 비활성화
        if (outlineCamera != null)
        {
            outlineCamera.gameObject.SetActive(false);
        }
    }
    /// <summary>
    /// 외곽선 색상 설정
    /// </summary>
    public void SetOutlineColor(Color color)
    {
        outlineColor = color;

        if (outlineMaterial != null)
        {
            outlineMaterial.SetColor("_SolidOutline", color);
        }
    }
    /// <summary>
    /// 외곽선 두께 설정
    /// </summary>
    public void SetOutlineWidth(float width)
    {
        outlineWidth = width;

        if (outlineMaterial != null)
        {
            outlineMaterial.SetFloat("_Thickness", width);
        }
    }
    #endregion
}