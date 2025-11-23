using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class GuestController : MonoBehaviour
{
    public IsometricPathfinder pathfinder;
    [SerializeField] private float moveSpeed = 5f;

    [HideInInspector]
    public TableManager tableManager;

    [Header("손님 그룹 설정")]
    public int desiredPartySize = 1;

    private Transform Target;
    private List<Vector3Int> currentPath;
    private int currentPathIndex;
    private bool isMoving = false;
    
    #region 애니메이션 제어를 위한 Public 프로퍼티 (추가됨)
    /// <summary>
    /// [수정됨] NPC의 현재 이동 상태를 반환하는 public 프로퍼티
    /// CharacterAnimationController에서 IsWalking 파라미터를 제어하기 위해 추가됨
    /// </summary>
    public bool IsMoving => isMoving;
    
    /// <summary>
    /// [수정됨] NPC의 현재 이동 속도 벡터를 반환하는 public 프로퍼티
    /// CharacterAnimationController에서 MoveY 파라미터를 제어하기 위해 추가됨
    /// 매 프레임마다 MoveAlongPath()에서 계산되어 업데이트됨
    /// </summary>
    public Vector3 CurrentVelocity { get; private set; }
    #endregion
    private GameObject assignedTable;
    private bool isWaiting = false;
    private int myWaitingPosition = -1;
    private GameObject waitingTargetObject;
    private bool isSeated = false;
    private Transform assignedSeat; 

    void Start()
    {
        // [수정됨] CurrentVelocity 초기화
        // 게임 시작 시 속도를 0으로 설정하여 애니메이션이 올바르게 시작되도록 함
        CurrentVelocity = Vector3.zero;
        if (pathfinder == null)
        {
            return;
        }
        DetermineTarget();
    }

    void Update()
    {
        // [수정됨] pathfinder가 없거나 앉아있는 상태일 때 속도를 0으로 설정
        // CharacterAnimationController가 정지 상태를 올바르게 감지할 수 있도록 함
        if (pathfinder == null || isSeated)
        {
            CurrentVelocity = Vector3.zero;
            return;
        }

        if (isWaiting)
        {
            CheckForAvailableTable();
        }

        if (Target == null)
        {
            DetermineTarget();
        }
        // [수정됨] Target이 없을 때 속도를 0으로 설정
        // 목표가 없으면 이동하지 않으므로 애니메이션도 정지 상태로 설정
        if (Target == null)
        {
            CurrentVelocity = Vector3.zero;
            return;
        }

        if (!isMoving)
        {
            Vector3 targetPos = new Vector3(Target.position.x, Target.position.y, 0);
            Vector3 startPos = new Vector3(this.transform.position.x, this.transform.position.y, 0);
            Vector3Int targetCell = pathfinder.WorldToCell(targetPos);
            Vector3Int startCell = pathfinder.WorldToCell(startPos);
            currentPath = pathfinder.FindPath(startCell, targetCell);

            if (currentPath != null)
            {
                pathfinder.DrawPath(currentPath);
                currentPathIndex = 0;
                isMoving = true;
            }
            else
            {
                // [수정됨] 경로를 찾을 수 없을 때 속도를 0으로 설정
                // 경로가 없으면 이동할 수 없으므로 애니메이션도 정지 상태로 설정
                CurrentVelocity = Vector3.zero;
            }
        }

        if (isMoving && currentPath != null)
        {
            MoveAlongPath();
        }
        else if (!isMoving)
        {
            // [수정됨] 이동하지 않을 때 속도를 0으로 설정
            // isMoving이 false일 때 애니메이션이 정지 상태로 전환되도록 함
            CurrentVelocity = Vector3.zero;
        }
    }

    void OnDestroy()
    {
        if (assignedTable != null && assignedSeat != null)
        {
            TableClass tableComp = assignedTable.GetComponent<TableClass>();
            if (tableComp != null)
            {
                tableComp.ReleaseSeat(this.gameObject);
            }
        }

        if (waitingTargetObject != null)
        {
            DestroyImmediate(waitingTargetObject);
        }
    }

    void CheckForAvailableTable()
    {
        GameObject partialTable = tableManager.GetPartiallyOccupiedTable(desiredPartySize);
        if (partialTable != null)
        {
            TableClass tableComp = partialTable.GetComponent<TableClass>();
            int seatedCount = tableComp.Seated_Customer.Count;
            int reservedCount = tableManager.tableReservations.ContainsKey(partialTable) ?
                                tableManager.tableReservations[partialTable].Count : 0;

            if (seatedCount + reservedCount < tableComp.MAX_Capacity)
            {
                Transform availableSeat = tableComp.GetAvailableSeatForGuest(this.gameObject);
                if (availableSeat != null)
                {
                    tableManager.ReserveTable(partialTable, this.gameObject);

                    if (tableManager.tableReservations.ContainsKey(partialTable) &&
                        tableManager.tableReservations[partialTable].Contains(this.gameObject))
                    {
                        RemoveFromWaitingLine();
                        assignedTable = partialTable;
                        assignedSeat = availableSeat;
                        Target = assignedSeat;
                        isMoving = false;
                        return;
                    }
                }
            }
        }

        GameObject availableTable = tableManager.GetAvailableTable(desiredPartySize);
        if (availableTable != null)
        {
            TableClass tableComp = availableTable.GetComponent<TableClass>();
            int reservedCount = tableManager.tableReservations.ContainsKey(availableTable) ?
                                tableManager.tableReservations[availableTable].Count : 0;

            if (reservedCount < tableComp.MAX_Capacity)
            {
                Transform availableSeat = tableComp.GetAvailableSeatForGuest(this.gameObject);
                if (availableSeat != null)
                {
                    tableManager.ReserveTable(availableTable, this.gameObject);

                    if (tableManager.tableReservations.ContainsKey(availableTable) &&
                        tableManager.tableReservations[availableTable].Contains(this.gameObject))
                    {
                        RemoveFromWaitingLine();
                        assignedTable = availableTable;
                        assignedSeat = availableSeat;
                        Target = assignedSeat;
                        isMoving = false;
                    }
                }
            }
        }
    }

    void DetermineTarget()
    {
        GameObject partialTable = tableManager.GetPartiallyOccupiedTable(desiredPartySize);
        if (partialTable != null)
        {
            TableClass tableComp = partialTable.GetComponent<TableClass>();
            int seatedCount = tableComp.Seated_Customer.Count;
            int reservedCount = tableManager.tableReservations.ContainsKey(partialTable) ?
                                tableManager.tableReservations[partialTable].Count : 0;

            if (seatedCount + reservedCount < tableComp.MAX_Capacity)
            {
                Transform availableSeat = tableComp.GetAvailableSeatForGuest(this.gameObject);
                if (availableSeat != null)
                {
                    tableManager.ReserveTable(partialTable, this.gameObject);

                    if (tableManager.tableReservations.ContainsKey(partialTable) &&
                        tableManager.tableReservations[partialTable].Contains(this.gameObject))
                    {
                        isWaiting = false;
                        assignedTable = partialTable;
                        assignedSeat = availableSeat;
                        Target = assignedSeat;
                        return;
                    }
                }
            }
        }

        GameObject availableTable = tableManager.GetAvailableTable(desiredPartySize);
        if (availableTable != null)
        {
            TableClass tableComp = availableTable.GetComponent<TableClass>();
            int reservedCount = tableManager.tableReservations.ContainsKey(availableTable) ?
                                tableManager.tableReservations[availableTable].Count : 0;

            if (reservedCount < tableComp.MAX_Capacity)
            {
                Transform availableSeat = tableComp.GetAvailableSeatForGuest(this.gameObject);
                if (availableSeat != null)
                {
                    tableManager.ReserveTable(availableTable, this.gameObject);

                    if (tableManager.tableReservations.ContainsKey(availableTable) &&
                        tableManager.tableReservations[availableTable].Contains(this.gameObject))
                    {
                        isWaiting = false;
                        assignedTable = availableTable;
                        assignedSeat = availableSeat;
                        Target = assignedSeat;
                        return;
                    }
                }
            }
        }

        if (tableManager.CustomerWaitingTransform != null)
        {
            AddToWaitingLine();
            assignedTable = null;
            assignedSeat = null;
        }
    }

    void AddToWaitingLine()
    {
        if (myWaitingPosition == -1)
        {
            myWaitingPosition = tableManager.AddToWaitingLine(this.gameObject);
            SetIsometricWaitingTarget();
        }
    }

    void RemoveFromWaitingLine()
    {
        if (myWaitingPosition != -1)
        {
            tableManager.RemoveFromWaitingLine(this.gameObject);
            myWaitingPosition = -1;
            isWaiting = false;

            if (waitingTargetObject != null)
            {
                DestroyImmediate(waitingTargetObject);
                waitingTargetObject = null;
            }
        }
    }

    void SetIsometricWaitingTarget()
    {
        Vector3 waitingPos = tableManager.CalculateIsometricWaitingPosition(myWaitingPosition);

        if (waitingTargetObject == null)
        {
            waitingTargetObject = new GameObject($"WaitingTarget_{this.gameObject.name}");
            Target = waitingTargetObject.transform;
        }

        Target.position = waitingPos;
        isMoving = false;
    }

    public void UpdateWaitingPosition(int newPosition)
    {
        if (isWaiting && myWaitingPosition != -1)
        {
            myWaitingPosition = newPosition;
            SetIsometricWaitingTarget();
        }
    }

    void MoveAlongPath()
    {
        if (currentPathIndex >= currentPath.Count)
        {
            isMoving = false;
            // [수정됨] 목적지에 도달했을 때 속도를 0으로 설정
            // 도착 시 애니메이션이 정지 상태로 전환되도록 함
            CurrentVelocity = Vector3.zero;
            OnReachedDestination();
            return;
        }

        // [수정됨] 이동 전 위치를 저장 (속도 계산을 위해)
        // 이전 프레임의 위치와 현재 프레임의 위치 차이로 속도를 계산
        Vector3 previousPosition = this.transform.position;
        Vector3 targetPos = pathfinder.CellToWorld(currentPath[currentPathIndex]);
        this.transform.position = Vector3.MoveTowards(this.transform.position, targetPos, moveSpeed * Time.deltaTime);
        
        // [수정됨] 현재 속도 계산 (애니메이션 제어용)
        // CharacterAnimationController에서 MoveY 파라미터를 제어하기 위해 필요
        // 위치 변화량을 Time.deltaTime으로 나누어 초당 이동 속도 벡터를 계산
        // 이 값의 Y 성분이 MoveY 파라미터로 사용되어 위/아래 이동 애니메이션을 제어함
        CurrentVelocity = (this.transform.position - previousPosition) / Time.deltaTime;

        if (Vector3.Distance(this.transform.position, targetPos) < 0.01f)
        {
            currentPathIndex++;
        }
    }

    void OnReachedDestination()
    {
        if (assignedTable != null && assignedSeat != null)
        {
            TableClass tableComp = assignedTable.GetComponent<TableClass>();
            if (tableComp.Seated_Customer.Count >= tableComp.MAX_Capacity)
            {
                Debug.LogWarning($" 테이블이 이미 가득 참");

                tableComp.ReleaseSeat(this.gameObject);

                tableManager.CancelReservation(assignedTable, this.gameObject);

                assignedTable = null;
                assignedSeat = null;
                Target = null;
                isMoving = false;
                DetermineTarget();
                return;
            }

            if (!tableComp.IsSeatAssignedToGuest(assignedSeat, this.gameObject))
            {
                Debug.LogWarning($"좌석이 다른 손님에게 배정됨");

                tableManager.CancelReservation(assignedTable, this.gameObject);

                assignedTable = null;
                assignedSeat = null;
                Target = null;
                isMoving = false;
                DetermineTarget();
                return;
            }

            if (!tableComp.Seated_Customer.Contains(this.gameObject))
            {
                tableComp.Seated_Customer.Add(this.gameObject);
                tableComp.isCustomerSeated = true;

                tableManager.CancelReservation(assignedTable, this.gameObject);

                transform.position = assignedSeat.position;

                isSeated = true;
            }
        }
        else if (myWaitingPosition != -1)
        {
            if (!isWaiting)
            {
                isWaiting = true;
            }
        }
    }
}