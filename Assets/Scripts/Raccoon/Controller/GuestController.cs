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
    public int desiredPartySize = 1; // 원하는 테이블 크기 (1인 or 2인 등)

    private Transform Target;
    private List<Vector3Int> currentPath;
    private int currentPathIndex;
    private bool isMoving = false;
    private GameObject assignedTable;
    private bool isWaiting = false;
    private int myWaitingPosition = -1;
    private GameObject waitingTargetObject;
    private bool isSeated = false;
    private Transform assignedSeat; // 배정된 좌석 (겹치지 않게)

    void Start()
    {
        if (pathfinder == null)
        {
            return;
        }
        DetermineTarget();
    }

    void Update()
    {
        if (pathfinder == null || isSeated)
        {
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
        if (Target == null)
        {
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
        }

        if (isMoving && currentPath != null)
        {
            MoveAlongPath();
        }
    }

    void OnDestroy()
    {
        // 좌석 배정 해제
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
        // 1번: 부분 점유 테이블 확인 (원하는 크기와 일치하는지 확인)
        GameObject partialTable = tableManager.GetPartiallyOccupiedTable(desiredPartySize);
        if (partialTable != null)
        {
            TableClass tableComp = partialTable.GetComponent<TableClass>();
            int seatedCount = tableComp.Seated_Customer.Count;
            int reservedCount = tableManager.tableReservations.ContainsKey(partialTable) ?
                                tableManager.tableReservations[partialTable].Count : 0;

            if (seatedCount + reservedCount < tableComp.MAX_Capacity)
            {
                // 빈 좌석 확인
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

        // 2번: 빈 테이블 확인 (원하는 크기와 일치하는지 확인)
        GameObject availableTable = tableManager.GetAvailableTable(desiredPartySize);
        if (availableTable != null)
        {
            TableClass tableComp = availableTable.GetComponent<TableClass>();
            int reservedCount = tableManager.tableReservations.ContainsKey(availableTable) ?
                                tableManager.tableReservations[availableTable].Count : 0;

            if (reservedCount < tableComp.MAX_Capacity)
            {
                // 빈 좌석 확인
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
        // 1번: 부분 점유 테이블 (원하는 크기 체크)
        GameObject partialTable = tableManager.GetPartiallyOccupiedTable(desiredPartySize);
        if (partialTable != null)
        {
            TableClass tableComp = partialTable.GetComponent<TableClass>();
            int seatedCount = tableComp.Seated_Customer.Count;
            int reservedCount = tableManager.tableReservations.ContainsKey(partialTable) ?
                                tableManager.tableReservations[partialTable].Count : 0;

            if (seatedCount + reservedCount < tableComp.MAX_Capacity)
            {
                // 빈 좌석 확인
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

        // 2번: 빈 테이블 (원하는 크기 체크)
        GameObject availableTable = tableManager.GetAvailableTable(desiredPartySize);
        if (availableTable != null)
        {
            TableClass tableComp = availableTable.GetComponent<TableClass>();
            int reservedCount = tableManager.tableReservations.ContainsKey(availableTable) ?
                                tableManager.tableReservations[availableTable].Count : 0;

            if (reservedCount < tableComp.MAX_Capacity)
            {
                // 빈 좌석 확인
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

        // 3번: 대기 위치
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
            OnReachedDestination();
            return;
        }

        Vector3 targetPos = pathfinder.CellToWorld(currentPath[currentPathIndex]);
        this.transform.position = Vector3.MoveTowards(this.transform.position, targetPos, moveSpeed * Time.deltaTime);

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

            // MAX_Capacity 최종 체크
            if (tableComp.Seated_Customer.Count >= tableComp.MAX_Capacity)
            {
                Debug.LogWarning($"⚠️ 테이블이 이미 가득 찼습니다!");

                // 좌석 배정 해제
                tableComp.ReleaseSeat(this.gameObject);

                // 예약 취소
                tableManager.CancelReservation(assignedTable, this.gameObject);

                assignedTable = null;
                assignedSeat = null;
                Target = null;
                isMoving = false;
                DetermineTarget();
                return;
            }

            // 좌석이 내게 배정되었는지 확인
            if (!tableComp.IsSeatAssignedToGuest(assignedSeat, this.gameObject))
            {
                Debug.LogWarning($"⚠️ 좌석이 다른 손님에게 배정되었습니다!");

                // 예약 취소
                tableManager.CancelReservation(assignedTable, this.gameObject);

                assignedTable = null;
                assignedSeat = null;
                Target = null;
                isMoving = false;
                DetermineTarget();
                return;
            }

            // 중복 체크 후 손님 추가
            if (!tableComp.Seated_Customer.Contains(this.gameObject))
            {
                tableComp.Seated_Customer.Add(this.gameObject);
                tableComp.isCustomerSeated = true;

                // 예약 취소 (실제 착석 완료)
                tableManager.CancelReservation(assignedTable, this.gameObject);

                // 좌석에 정확히 위치
                transform.position = assignedSeat.position;

                // 착석 완료
                isSeated = true;
                Debug.Log($"✅ 손님이 좌석에 착석 완료: {assignedTable.name}");
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