using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GuestController : MonoBehaviour
{
    // 아이소메트릭 경로탐색기 스크립트
    public IsometricPathfinder pathfinder;
    // 이동속도
    [SerializeField] private float moveSpeed = 5f;

    [HideInInspector]
    public TableManager tableManager; // 테이블 매니저 스크립트

    private Transform Target; // 최종 목표 타겟 (테이블 또는 대기 위치)
    private List<Vector3Int> currentPath; // 현재 경로
    private int currentPathIndex; // 현재 경로 인덱스
    private bool isMoving = false; // 이동 중인지 여부
    private GameObject assignedTable; // 예약된 테이블 오브젝트
    private bool isWaiting = false; // 대기 위치에 도착했는지 여부
    private int myWaitingPosition = -1; // 대기열에서의 위치 (-1은 대기열에 없음을 의미)

    // 아이소메트릭 대기열을 위한 임시 타겟 오브젝트
    private GameObject waitingTargetObject;

    void Start()
    {
        // pathfinder가 없으면 경고
        if (pathfinder == null)
        {
            //Debug.LogError("IsometricPathfinder를 찾을 수 없습니다! GuestController에 할당해주세요.");
            return;
        }

        // 초기 타겟 결정
        DetermineTarget();
    }

    private bool isSeated = false; // 착석 완료 여부

    void Update()
    {
        // pathfinder가 없으면 실행하지 않음
        if (pathfinder == null)
        {
            return;
        }

        // 이미 착석했으면 더 이상 이동하지 않음
        if (isSeated)
        {
            return;
        }

        // 대기 중일 때, 테이블이 비었는지 확인
        if (isWaiting)
        {
            CheckForAvailableTable();
        }

        // 타겟이 없으면 재결정
        if (Target == null)
        {
            DetermineTarget();
        }
        // 타겟이 여전히 없으면 실행하지 않음
        if (Target == null)
        {
            return;
        }

        // 이동 중이 아닐 때만 경로 재계산
        if (!isMoving)
        {
            // Z 좌표를 0으로 고정하여 2D 타일맵 좌표로 변환
            Vector3 targetPos = new Vector3(Target.position.x, Target.position.y, 0);
            Vector3 startPos = new Vector3(this.transform.position.x, this.transform.position.y, 0);
            // 타일맵 셀 좌표로 변환
            Vector3Int targetCell = pathfinder.WorldToCell(targetPos);
            Vector3Int startCell = pathfinder.WorldToCell(startPos);
            // 경로 찾기
            currentPath = pathfinder.FindPath(startCell, targetCell);

            if (currentPath != null)
            {
                // 경로 시각화 (디버그용)
                pathfinder.DrawPath(currentPath);
                // 경로 인덱스 초기화 및 이동 시작
                currentPathIndex = 0;
                isMoving = true;
            }
        }

        // 경로를 따라 이동
        if (isMoving && currentPath != null)
        {
            MoveAlongPath(); // 경로를 따라 이동
        }
    }

    void OnDestroy()
    {
        // 임시 타겟 오브젝트 정리
        if (waitingTargetObject != null)
        {
            DestroyImmediate(waitingTargetObject); // 파괴
        }
    }

    // 대기 중일 때 사용 가능한 테이블 확인
    void CheckForAvailableTable()
    {
        // 1번 조건: 부분 점유 테이블 확인
        GameObject partialTable = tableManager.GetPartiallyOccupiedTable(); // 부분 점유 테이블 가져오기
        if (partialTable != null)
        {
            TableClass tableComp = partialTable.GetComponent<TableClass>();
            int seatedCount = tableComp.Seated_Customer.Count; // 앉아있는 손님 수
            // 예약된 손님 수 , 예약된 손님 수가 없으면 0 있으면 그 수
            int reservedCount = tableManager.tableReservations.ContainsKey(partialTable) ?
                                tableManager.tableReservations[partialTable].Count : 0;

            // 실제로 자리가 있는지 확인
            if (seatedCount + reservedCount < tableComp.MAX_Capacity)
            {
                // 예약 시도
                tableManager.ReserveTable(partialTable, this.gameObject);

                // 예약 성공 여부 확인
                if (tableManager.tableReservations.ContainsKey(partialTable) &&
                    tableManager.tableReservations[partialTable].Contains(this.gameObject))
                {
                    // 대기 상태 해제 및 대기열에서 제거
                    RemoveFromWaitingLine();

                    assignedTable = partialTable; // 예약된 테이블 설정
                    Target = partialTable.transform; // 타겟 설정
                    isMoving = false; // 새로운 경로 계산을 위해
                    //Debug.Log("🍽️ 대기 중 부분 점유 테이블 발견! 이동 시작");
                    return;
                }
            }
        }

        // 2번 조건: 빈 테이블 확인
        GameObject availableTable = tableManager.GetAvailableTable(); // 빈 테이블 가져오기
        if (availableTable != null) // 빈 테이블이 있으면
        {
            TableClass tableComp = availableTable.GetComponent<TableClass>();
            // 예약된 손님 수 , 예약된 손님 수가 없으면 0 있으면 그 수
            int reservedCount = tableManager.tableReservations.ContainsKey(availableTable) ?
                                tableManager.tableReservations[availableTable].Count : 0;

            // 실제로 자리가 있는지 확인
            if (reservedCount < tableComp.MAX_Capacity)
            {
                // 예약 시도
                tableManager.ReserveTable(availableTable, this.gameObject);

                // 예약 성공 여부 확인
                if (tableManager.tableReservations.ContainsKey(availableTable) &&
                    tableManager.tableReservations[availableTable].Contains(this.gameObject))
                {
                    // 대기 상태 해제 및 대기열에서 제거
                    RemoveFromWaitingLine();

                    assignedTable = availableTable; // 예약된 테이블 설정
                    Target = availableTable.transform; // 타겟 설정
                    isMoving = false; // 새로운 경로 계산을 위해
                    //Debug.Log("🍽️ 대기 중 빈 테이블 발견! 이동 시작");
                }
            }
        }
    }

    void DetermineTarget() // 최종 타겟 결정 메서드
    {
        // 1번 조건: 예약된 테이블 중 앉은 손님 수가 1명 이상이고 자리가 남은 테이블
        GameObject partialTable = tableManager.GetPartiallyOccupiedTable();
        if (partialTable != null)
        {
            TableClass tableComp = partialTable.GetComponent<TableClass>();
            int seatedCount = tableComp.Seated_Customer.Count; // 앉아있는 손님 수
            // 예약된 손님 수 , 예약된 손님 수가 없으면 0 있으면 그 수
            int reservedCount = tableManager.tableReservations.ContainsKey(partialTable) ?
                                tableManager.tableReservations[partialTable].Count : 0;

            // 실제로 예약 가능한지 확인
            if (seatedCount + reservedCount < tableComp.MAX_Capacity)
            {
                // 예약 시도
                tableManager.ReserveTable(partialTable, this.gameObject);

                // 예약 성공 여부 확인
                if (tableManager.tableReservations.ContainsKey(partialTable) &&
                    tableManager.tableReservations[partialTable].Contains(this.gameObject))
                {
                    isWaiting = false;
                    assignedTable = partialTable; // 예약된 테이블 설정
                    Target = partialTable.transform; // 타겟 설정
                    //Debug.Log("🎯 타겟: 부분 점유 테이블 (예약 완료)");
                    return;
                }
            }
        }

        // 2번 조건: 예약되지 않은 테이블
        GameObject availableTable = tableManager.GetAvailableTable();
        if (availableTable != null)
        {
            TableClass tableComp = availableTable.GetComponent<TableClass>();
            // 예약된 손님 수 , 예약된 손님 수가 없으면 0 있으면 그 수
            int reservedCount = tableManager.tableReservations.ContainsKey(availableTable) ?
                                tableManager.tableReservations[availableTable].Count : 0;

            // 실제로 예약 가능한지 확인
            if (reservedCount < tableComp.MAX_Capacity)
            {
                // 예약 시도
                tableManager.ReserveTable(availableTable, this.gameObject);

                // 예약 성공 여부 확인
                if (tableManager.tableReservations.ContainsKey(availableTable) &&
                    tableManager.tableReservations[availableTable].Contains(this.gameObject))
                {
                    isWaiting = false;
                    assignedTable = availableTable; // 예약된 테이블 설정
                    Target = availableTable.transform; // 타겟 설정
                    //Debug.Log("🎯 타겟: 빈 테이블 (예약 완료)");
                    return;
                }
            }
        }

        // 3번 조건: 아이소메트릭 대기 위치 (대기열 추가)
        if (tableManager.CustomerWaitingTransform != null)
        {
            AddToWaitingLine();
            assignedTable = null;
            //Debug.Log("🚶 타겟: 아이소메트릭 대기 위치 (모든 테이블 만석)");
        }
    }

    // === 아이소메트릭 대기열 관리 메서드들 ===

    // 대기열에 추가하는 메서드
    void AddToWaitingLine()
    {
        if (myWaitingPosition == -1) // 아직 대기열에 없는 경우만
        {
            myWaitingPosition = tableManager.AddToWaitingLine(this.gameObject);
            SetIsometricWaitingTarget();
        }
    }

    // 대기열에서 제거하는 메서드
    void RemoveFromWaitingLine()
    {
        if (myWaitingPosition != -1)
        {
            tableManager.RemoveFromWaitingLine(this.gameObject);
            myWaitingPosition = -1;
            isWaiting = false;

            // 임시 타겟 오브젝트 정리
            if (waitingTargetObject != null)
            {
                DestroyImmediate(waitingTargetObject);
                waitingTargetObject = null;
            }
        }
    }
    // 아이소메트릭 대기 위치를 타겟으로 설정
    void SetIsometricWaitingTarget()
    {
        Vector3 waitingPos = tableManager.CalculateIsometricWaitingPosition(myWaitingPosition);

        // 임시 빈 오브젝트를 생성하여 타겟으로 사용
        if (waitingTargetObject == null)
        {
            waitingTargetObject = new GameObject($"WaitingTarget_{this.gameObject.name}");
            Target = waitingTargetObject.transform;
        }

        Target.position = waitingPos;
        isMoving = false; // 새로운 경로 계산을 위해

        //Debug.Log($"🎮 아이소메트릭 대기 위치 설정: {myWaitingPosition}번째, 좌표: {waitingPos}");
    }

    // 대기 위치를 업데이트하는 메서드 (TableManager에서 호출)
    // <param name="newPosition">새로운 대기열 위치</param>
    public void UpdateWaitingPosition(int newPosition)
    {
        if (isWaiting && myWaitingPosition != -1)
        {
            myWaitingPosition = newPosition;
            SetIsometricWaitingTarget();
            //Debug.Log($"⬆️ 아이소메트릭 대기 위치 업데이트: {myWaitingPosition}번째");
        }
    }

    void MoveAlongPath()
    {
        if (currentPathIndex >= currentPath.Count) // 경로 끝에 도달
        {
            isMoving = false; // 이동 종료
            OnReachedDestination(); // 도착 처리
            return;
        }

        Vector3 targetPos = pathfinder.CellToWorld(currentPath[currentPathIndex]); // 다음 경로 지점의 월드 좌표
        this.transform.position = Vector3.MoveTowards(this.transform.position, targetPos, moveSpeed * Time.deltaTime); // 이동

        if (Vector3.Distance(this.transform.position, targetPos) < 0.01f) // 다음 지점에 거의 도달했으면
        {
            currentPathIndex++; // 다음 경로 지점으로 이동
        }
    }

    void OnReachedDestination() // 도착 처리 메서드
    {
        // 테이블에 도착한 경우
        if (assignedTable != null)
        {
            TableClass tableComp = assignedTable.GetComponent<TableClass>();

            // MAX_Capacity 최종 체크
            if (tableComp.Seated_Customer.Count >= tableComp.MAX_Capacity)
            {
                //Debug.LogWarning($"⚠️ 테이블이 이미 가득 참! 현재: {tableComp.Seated_Customer.Count}명, 최대: {tableComp.MAX_Capacity}명");

                // 예약 취소
                tableManager.CancelReservation(assignedTable, this.gameObject);

                // 다시 타겟 결정 (대기 위치로)
                assignedTable = null;
                Target = null;
                isMoving = false;
                DetermineTarget(); // 대기 위치로 이동 시도
                return;
            }

            // 중복 체크 후 손님 추가 및 테이블 상태 변경
            if (!tableComp.Seated_Customer.Contains(this.gameObject))
            {
                tableComp.Seated_Customer.Add(this.gameObject);
                tableComp.isCustomerSeated = true; // 손님이 앉아있는 상태로 변경

                // 예약 취소 (실제 착석 완료)
                tableManager.CancelReservation(assignedTable, this.gameObject);

                // 착석 완료 상태로 변경
                isSeated = true;
                //Debug.Log($"🍽️ 손님이 테이블에 착석. 현재 인원: {tableComp.Seated_Customer.Count}/{tableComp.MAX_Capacity}");
            }
        }
        // 아이소메트릭 대기 위치에 도착한 경우
        else if (myWaitingPosition != -1)
        {
            if (!isWaiting) // 처음 대기 위치에 도착했을 때만
            {
                isWaiting = true;
                Vector3 currentPos = tableManager.CalculateIsometricWaitingPosition(myWaitingPosition); // 현재 대기 위치 좌표
                //Debug.Log($"🎮 손님이 아이소메트릭 대기열 {myWaitingPosition}번째 위치에 도착 - 좌표: {currentPos}");
            }
        }
    }
}