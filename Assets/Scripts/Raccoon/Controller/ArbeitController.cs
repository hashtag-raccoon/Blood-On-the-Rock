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
    public bool isSelected = false;

    private Camera mainCamera;

    private void Awake()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("Main Camera를 찾을 수 없습니다. 카메라에 'MainCamera' 태그가 있는지 확인해주세요.");
        }
    }

    #region Initialization
    /// <summary>
    /// 초기화
    /// </summary>
    /// <param name="알바 데이터"></param> <summary>
    public void Initialize(npc npcData)
    {
        this.myNpcData = npcData;
        // 이동 속도 보정 등이 필요할 경우 여기에 추가할 것
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
        // 목적지 도착 시 로직
        // 목적지 도착 시 처리할 추가 로직이 있으면 여기에 작성할 것
        Debug.Log($"Arbeit {myNpcData?.part_timer_name} reached destination.");
    }
    #endregion
}
